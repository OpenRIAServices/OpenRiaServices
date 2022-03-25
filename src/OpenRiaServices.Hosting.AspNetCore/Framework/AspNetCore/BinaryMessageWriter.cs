using System;
using System.Buffers;
using System.Xml;

#if SERVERFX
namespace OpenRiaServices.Hosting.Wcf.MessageEncoders
#else
namespace OpenRiaServices.Client.Web
#endif
{
    /// <summary>
    /// Helper class to cache <see cref="XmlDictionaryWriter"/> and stream in order
    /// to not have to allocated all memory used for the writer.
    /// It also adds some estimates of the buffer size needde
    /// </summary>
    internal class BinaryMessageWriter
    {
        private ArrayPoolStream _stream;
        private XmlDictionaryWriter _writer;
        private IXmlBinaryWriterInitializer _writerInitializer;

        private const int MaxStreamAllocationSize = 1024 * 1024;
        // IMPORTANT: If this is changed then EstimateMessageSize should be changed as well
        private const int MessageLengthHistorySize = 4;
        private const int InitialBufferSize = 16 * 1024;
        private readonly int[] _lastMessageLengths = new int[MessageLengthHistorySize] { InitialBufferSize, InitialBufferSize, InitialBufferSize, InitialBufferSize };
        private int _messageLengthIndex = 0;

        // Cache at most one writer per thread
        [ThreadStatic]
        private static BinaryMessageWriter s_threadInstance;

        /// <summary>
        ///  Prevent creation from outside of this class
        /// </summary>
        private BinaryMessageWriter()
        {
        }

        public static BinaryMessageWriter Rent()
        {
            var messageWriter = s_threadInstance ?? new BinaryMessageWriter();
            // Reentrancy is not expected, but if the operation throws we dont
            // want to reuse the current messageWriter since XmlWriter might not be in starting state
            s_threadInstance = null;
            return messageWriter;
        }

        public void Clear() => _stream.Clear();

        public static MessageEncoders.ArrayPoolStream.BufferMemory Return(BinaryMessageWriter binaryMessageWriter)
        {
            binaryMessageWriter._writer.Flush();
            binaryMessageWriter.RecordMessageSize((int)binaryMessageWriter._stream.Length);
            var res = binaryMessageWriter._stream.GetBufferMemoryAndClear();

            s_threadInstance = binaryMessageWriter;

            return res;
        }

        private void RecordMessageSize(int count)
        {
            _lastMessageLengths[_messageLengthIndex] = count;
            _messageLengthIndex = (_messageLengthIndex + 1) % MessageLengthHistorySize;
        }

        /// <summary>
        /// Get estimate based on maximum buffer size of the last few messages.
        /// </summary>
        private int EstimateMessageSize()
        {
            int max1 = Math.Max(_lastMessageLengths[3], _lastMessageLengths[2]);
            int max2 = Math.Max(_lastMessageLengths[1], _lastMessageLengths[0]);

            return Math.Max(max2, max1) + 256;
        }

        public XmlDictionaryWriter GetXmlWriter()
        {
            int startSize = EstimateMessageSize();
            // Reuse created writer and stream if possible, they are created on first call 
            if (_writer != null)
            {
                _stream.Reset(ArrayPool<byte>.Shared, 0, startSize);
            }
            else
            {
                _stream = new ArrayPoolStream(ArrayPool<byte>.Shared, 0 , startSize, MaxStreamAllocationSize);
                _writer = XmlDictionaryWriter.CreateBinaryWriter(_stream);
            }

            return _writer;
        }
    }
}
