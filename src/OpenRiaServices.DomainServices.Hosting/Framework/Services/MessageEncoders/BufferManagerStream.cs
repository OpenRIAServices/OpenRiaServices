﻿using System;
using System.IO;
using System.ServiceModel.Channels;

#if SERVERFX
namespace OpenRiaServices.DomainServices.Hosting
#else
namespace OpenRiaServices.DomainServices.Client.Web
#endif
{
    /// <summary>
    /// Stream optimized for usage by <see cref="System.Xml.XmlDictionaryWriter"/> without unneccessary 
    /// allocations on LOH.
    /// It writes directly to memory pooled by a <see cref="BufferManager"/> in order to 
    /// avoid allocations and be able to return memory directly without additional copies 
    /// (for small messages).
    /// </summary>
    internal class BufferManagerStream : Stream
    {
        private static readonly bool Is64BitProcess = Environment.Is64BitProcess;
        private BufferManager _bufferManager;
        private readonly int _maxSize;
        // The offset into the final byte array where our content should start
        private int _offset;
        // number of bytes written to _buffer, used as offset into _buffer where we write next time
        private int _bufferWritten;
        // "Current" buffer where the next write should go
        private byte[] _buffer;
        // Any "previous" buffers already filled
        private System.Collections.Generic.List<byte[]> _bufferList;
        // String "position" (total size so far)
        private int _position;


        public BufferManagerStream(BufferManager bufferManager, int offset, int minAllocationSize, int maxAllocationSize)
        {
            _maxSize = maxAllocationSize;
            Reset(bufferManager, offset, minAllocationSize);
        }

        public void Reset(BufferManager bufferManager, int offset, int minAllocationSize)
        {
            _bufferManager = bufferManager;
            _offset = offset;
            _bufferWritten = offset;
            _position = 0;
            _buffer = bufferManager.TakeBuffer(minAllocationSize + offset);
        }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotImplementedException();

        public override long Position { get => _position; set => throw new NotImplementedException(); }

        public override void Flush()
        {
            // Nothing to do
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

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
                _bufferList = new System.Collections.Generic.List<byte[]>(capacity: 16);
            _bufferList.Add(_buffer);
            // Ensure we never return buffer twice in case TakeBuffer below throws
            _buffer = null;

            int nextSize = Math.Min(_position * 2, _maxSize);
            _buffer = _bufferManager.TakeBuffer(nextSize);
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
                _bufferManager.ReturnBuffer(_buffer);
                _buffer = null;
            }

            if (_bufferList != null)
            {
                foreach (var buffer in _bufferList)
                    _bufferManager.ReturnBuffer(buffer);
                _bufferList = null;
            }
        }

        /// <summary>
        /// Copies bytes from <paramref name="src"/> to <paramref name="dest"/> using fastes 
        /// copy based on process bitness (x86 / x64) tested on .Net Framework 4.8
        /// </summary>
        private static unsafe void FastCopy(byte[] src, int srcOffset, byte[] dest, int destOffset, int count)
        {
            if (count == 0)
                return;

            if (Is64BitProcess && count <= 1024)
            {
                fixed (byte* s = &src[srcOffset], d = &dest[destOffset])
                    Buffer.MemoryCopy(s, d, dest.Length - destOffset, count);
            }
            else
            {
                // For x86 it is significantly faster to do copying of int's and longs
                // or similar in managed code for smaller counts (below 100-200)
                // But we expect most copies to be larger since xml writer buffer around 500 bytes
                Buffer.BlockCopy(src, srcOffset, dest, destOffset, count);
            }
        }

        public ArraySegment<byte> GetArrayAndClear()
        {
            // We only have a single segment, return it directly with no copying
            if (_bufferList == null)
            {
                var buffer = _buffer;
                _buffer = null;

                System.Diagnostics.Debug.Assert(_bufferWritten == _position + _offset);
                Clear();
                return new ArraySegment<byte>(buffer, _offset, _position);
            }
            else
            {
                // Copy in reverse order from filled to utilize CPU caches better
                // _buffer might only be partially filled
                int totalSize = _offset + _position;
                int destOffset = totalSize - _bufferWritten;
                byte[] buffer = null;

                try
                {
                    // Reuse the "current" buffer if it is large enough
                    if (_position <= _buffer.Length)
                    {
                        buffer = _buffer;
                        FastCopy(_buffer, 0, buffer, destOffset, _bufferWritten);
                        _buffer = null;
                    }
                    else
                    {
                        buffer = _bufferManager.TakeBuffer(totalSize);
                        FastCopy(_buffer, 0, buffer, destOffset, _bufferWritten);
                    }

                    // Buffers in list are all full
                    for (int i = _bufferList.Count - 1; i > 0; --i)
                    {
                        destOffset -= _bufferList[i].Length;
                        FastCopy(_bufferList[i], 0, buffer, destOffset, _bufferList[i].Length);
                    }

                    // First buffer might have offset
                    FastCopy(_bufferList[0], _offset, buffer, _offset, _bufferList[0].Length - _offset);
                    System.Diagnostics.Debug.Assert(destOffset - (_bufferList[0].Length - _offset) == _offset);

                    Clear();

                    return new ArraySegment<byte>(buffer, _offset, _position);

                }
                catch
                {
                    if (buffer != null)
                        _bufferManager.ReturnBuffer(buffer);
                    throw;
                }
            }
        }
    }
}
