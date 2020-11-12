using System;
using System.ServiceModel.Channels;
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
        private BufferManagerStream _stream;
        private XmlDictionaryWriter _writer;

        private const int MaxStreamAllocationSize = 1024 * 1024;
        // IMPORTANT: If this is changed then EstimateMessageSize should be changed as well
        private const int MessageLengthHistorySize = 4;
        private const int InitialBufferSize = 2048;
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

        /// <summary>
        /// Writes the specified message to a byte array allocated by the specigied <paramref name="bufferManager"/>
        /// </summary>
        public static ArraySegment<byte> WriteMessage(Message message, BufferManager bufferManager, int messageOffset)
        {
            var messageWriter = s_threadInstance ?? new BinaryMessageWriter();
            // Reentrancy is not expected, but if the operation throws we dont
            // want to reuse the current messageWriter since XmlWriter might not be in starting state
            s_threadInstance = null;

            var result = messageWriter.WriteMessageCore(message, bufferManager, messageOffset);

            // Save for later reuse
            s_threadInstance = messageWriter;
            return result;
        }

        /// <summary>
        /// Writes the specified message to a byte array allocated by the specigied <paramref name="bufferManager"/>
        /// </summary>
        private ArraySegment<byte> WriteMessageCore(Message message, BufferManager bufferManager, int offset)
        {
            try
            {
                XmlDictionaryWriter writer = GetXmlWriter(bufferManager, offset);
                message.WriteMessage(writer);
                writer.Flush();

                var result = _stream.GetArrayAndClear();
                RecordMessageSize(result.Count);
                return result;
            }
            catch
            {
                _stream.Clear();
                throw;
            }
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

        private XmlDictionaryWriter GetXmlWriter(BufferManager bufferManager, int offset)
        {
            int startSize = EstimateMessageSize();
            // Reuse created writer and stream if possible, they are created on first call 
            if (_writer != null)
            {
                _stream.Reset(bufferManager, offset, startSize);
            }
            else
            {
                _stream = new BufferManagerStream(bufferManager, offset, startSize, MaxStreamAllocationSize);
                _writer = XmlDictionaryWriter.CreateBinaryWriter(_stream);
            }

            return _writer;
        }
    }
}
