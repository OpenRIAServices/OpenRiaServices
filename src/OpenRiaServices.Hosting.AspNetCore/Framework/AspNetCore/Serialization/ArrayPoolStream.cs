using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace OpenRiaServices.Hosting.AspNetCore.Serialization
{
    /// <summary>
    /// Stream optimized for usage by <see cref="System.Xml.XmlDictionaryWriter"/> without unneccessary 
    /// allocations on LOH.
    /// It writes directly to memory pooled by a <see cref="BufferManager"/> in order to 
    /// avoid allocations and be able to return memory directly without additional copies 
    /// (for small messages).
    /// </summary>
    internal sealed class ArrayPoolStream : Stream
    {
        private ArrayPool<byte> _arrayPool;
        private readonly int _maxSize;
        // number of bytes written to _buffer, used as offset into _buffer where we write next time
        private int _bufferWritten;
        // "Current" buffer where the next write should go
        private byte[] _buffer;
        // Any "previous" buffers already filled
        private List<byte[]> _bufferList;
        // String "position" (total size so far)
        private int _position;

        public ArrayPoolStream(ArrayPool<byte> arrayPool, int maxBlockSize)
        {
            _arrayPool = arrayPool;
            _maxSize = maxBlockSize;
        }

        public void Reset(int size)
        {
            Debug.Assert(_buffer is null);

            _bufferWritten = 0;
            _position = 0;
            _buffer = _arrayPool.Rent(Math.Min(size, _maxSize));
        }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => _position;

        public override long Position { get => _position; set => throw new NotImplementedException(); }

        public override void Flush()
        {
            // Nothing to do
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotImplementedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();

        public override void SetLength(long value) => throw new NotImplementedException();

        public override void Write(byte[] buffer, int offset, int count)
        {
            // Argument validation is skipped since it is only used by 
            // BinaryXml writer which we trust to always give valid input

            // Note: BinaryXml buffers up to 512 bytesso we should expect most writes to be around 
            // 500+ bytes (smaller if the next write is a long string or byte array)
            do
            {
                EnsureBufferCapacity();

                // Write up to count bytes, but never more than the rest of the buffer
                int toCopy = Math.Min(count, _buffer.Length - _bufferWritten);
                FastCopy(buffer, offset, _buffer, _bufferWritten, toCopy);
                _position += toCopy;
                _bufferWritten += toCopy;
                offset += toCopy;
                count -= toCopy;
            } while (count > 0);
        }

        /// <summary>
        /// Copies bytes from <paramref name="src"/> to <paramref name="dest"/>
        /// </summary>
        private static unsafe void FastCopy(byte[] src, int srcOffset, byte[] dest, int destOffset, int count)
        {
            if (count == 0)
                return;

            Unsafe.CopyBlockUnaligned(destination: ref dest[destOffset], source: ref src[srcOffset], (uint)count);
        }

        /// <summary>
        /// Allocate more space if buffer is full.
        /// Ensures _buffer is non null and has space to write more bytes
        /// </summary>
        private void EnsureBufferCapacity()
        {
            // There is space left
            if (_bufferWritten < _buffer.Length)
                return;

            // Save current buffer in list before allocating a new buffer
            if (_bufferList == null)
                _bufferList = new List<byte[]>(capacity: 16);
            _bufferList.Add(_buffer);
            // Ensure we never return buffer twice in case TakeBuffer below throws
            _buffer = null;

            int nextSize = Math.Min(_position * 2, _maxSize);
            _buffer = _arrayPool.Rent(nextSize);
            _bufferWritten = 0;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Clear();
            }
            base.Dispose(disposing);
        }

        /// <summary>
        ///  Returns all mmmory to the current BufferManager
        /// </summary>
        public void Clear()
        {
            if (_buffer != null)
            {
                _arrayPool.Return(_buffer);
                _buffer = null;
            }

            if (_bufferList != null)
            {
                foreach (var buffer in _bufferList)
                    _arrayPool.Return(buffer);
                _bufferList = null;
            }
        }


        public BufferMemory GetBufferMemoryAndReset()
        {
            var res = new BufferMemory(_arrayPool, _buffer, _bufferList, _bufferWritten, _position);
            _buffer = null;
            _bufferList = null;
            _bufferWritten = 0;
            _position = 0;
            return res;
        }

        public ArraySegment<byte> GetRentedArrayAndClear()
        {
            ArraySegment<byte> result;

            // We only have a single segment, return it directly with no copying
            if (_bufferList is null)
            {
                result = new ArraySegment<byte>(_buffer, 0, _position);
                System.Diagnostics.Debug.Assert(_bufferWritten == _position);
                _buffer = null; // prevent buffer from beeing returned twice
            }
            else
            {
                // Copy in reverse order from filled to utilize CPU caches better
                // _buffer might only be partially filled
                int totalSize = _position;
                int destOffset = totalSize - _bufferWritten;
                result = default;

                try
                {
                    // Reuse the "current" buffer if it is large enough
                    if (_position <= _buffer.Length)
                    {
                        result = new ArraySegment<byte>(_buffer, 0, _position);

                        // Src and destination can overlapp so Unsafe cannot be used
                        // Using unsafe gives invalid result, but on x86 (not x64) so it is difficult to troubleshoot
                        Buffer.BlockCopy(src: _buffer, srcOffset: 0, _buffer, destOffset, _bufferWritten);
                        _buffer = null; // prevent buffer from beeing returned twice
                    }
                    else
                    {
                        result = new ArraySegment<byte>(_arrayPool.Rent(totalSize), 0, _position);
                        FastCopy(_buffer, 0, result.Array, destOffset, _bufferWritten);
                    }

                    // Buffers in list are all full
                    for (int i = _bufferList.Count - 1; i >= 0; --i)
                    {
                        destOffset -= _bufferList[i].Length;
                        FastCopy(_bufferList[i], 0, result.Array, destOffset, _bufferList[i].Length);
                    }
                }
                catch
                {
                    if (result.Array != default)
                        _arrayPool.Return(result.Array);
                    throw;
                }
            }
            Clear();
            _bufferWritten = 0;
            _position = 0;
            return result;
        }

        public struct BufferMemory : IDisposable
        {
            readonly ArrayPool<byte> _arrayPool;
            byte[] _buffer;
            readonly int _bufferWritten;
            private readonly int _length;
            List<byte[]> _bufferList;

            public int Length => _length;

            public BufferMemory(ArrayPool<byte> arrayPool, byte[] buffer, List<byte[]> bufferList, int bufferWritten, int length)
            {
                _arrayPool = arrayPool;
                _buffer = buffer;
                _bufferList = bufferList;
                _bufferWritten = bufferWritten;
                _length = length;
            }

            public async Task WriteTo(HttpResponse response, CancellationToken ct)
            {
                await response.StartAsync(ct);
                WriteTo(response.BodyWriter);
                //await response.BodyWriter.FlushAsync(ct);
                //await response.CompleteAsync(); //? needed ?? 
            }

            private void WriteTo(PipeWriter bodyWriter)
            {
                // It might make sense to call FlushAsync after each buffer is writter
                // to reduce memory usage
                var list = _bufferList;
                if (list != null)
                {
                    for (int i = 0; i < list.Count; ++i)
                    {
                        var buffer = list[i];
                        int len = buffer.Length;
                        var dest = bodyWriter.GetSpan(len);
                        Unsafe.CopyBlockUnaligned(ref dest[0], source: ref buffer[0], (uint)len);
                        bodyWriter.Advance(len);
                    }
                }

                if (_buffer != null)
                {
                    var dest = bodyWriter.GetSpan(_bufferWritten);
                    Unsafe.CopyBlockUnaligned(ref dest[0], source: ref _buffer[0], (uint)_bufferWritten);
                    bodyWriter.Advance(_bufferWritten);
                }
            }

            public void Dispose()
            {
                if (_buffer != null)
                {
                    _arrayPool.Return(_buffer);
                    _buffer = null;
                }

                if (_bufferList != null)
                {
                    foreach (var buffer in _bufferList)
                        if (buffer != null)
                            _arrayPool.Return(buffer);
                    _bufferList = null;
                }
            }
        }
    }
}
