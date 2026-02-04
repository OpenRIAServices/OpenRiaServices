using System;
using System.Buffers;
using System.Text;
using System.Xml;

namespace OpenRiaServices.Hosting.AspNetCore.Serialization
{
    /// <summary>
    /// Helper class to cache <see cref="XmlDictionaryWriter"/> and stream in order
    /// to not have to allocated all memory used for the writer.
    /// It also adds some estimates of the buffer size needde
    /// </summary>
    internal sealed class BinaryMessageWriter
    {
        private readonly ArrayPoolStream _stream;
        private readonly XmlDictionaryWriter _binaryWriter;
        private readonly XmlDictionaryWriter _textWriter;
        private XmlDictionaryWriter _currentWriter;

        private const int MaxStreamAllocationSize = 4 * 1024 * 1024;
        // IMPORTANT: If this is changed then EstimateMessageSize should be changed as well
        private const int MessageLengthHistorySize = 4;
        private const int InitialBufferSize = 16 * 1024;
        private readonly int[] _lastMessageLengths = new int[MessageLengthHistorySize] { InitialBufferSize, InitialBufferSize, InitialBufferSize, InitialBufferSize };
        private int _messageLengthIndex;
        public static readonly Encoding UTF8Encoding = new UTF8Encoding(false);

        // Cache at most one writer per thread
        [ThreadStatic]
        private static BinaryMessageWriter? s_threadInstance;

        /// <summary>
        ///  Prevent creation from outside of this class
        /// <summary>
        /// Initializes a BinaryMessageWriter with a pooled underlying stream and creates both binary and text XmlDictionaryWriter instances, selecting the binary writer as the current writer.
        /// </summary>
        private BinaryMessageWriter()
        {
            _stream = new ArrayPoolStream(ArrayPool<byte>.Shared, MaxStreamAllocationSize);
            _currentWriter = _binaryWriter = XmlDictionaryWriter.CreateBinaryWriter(_stream);
            _textWriter = XmlDictionaryWriter.CreateTextWriter(_stream);
        }

        /// <summary>
        /// Obtains a per-thread BinaryMessageWriter instance prepared for writing binary or text XML.
        /// </summary>
        /// <param name="isBinary">If true, selects the binary XML writer; otherwise selects the text XML writer.</param>
        /// <returns>A BinaryMessageWriter instance with its internal stream reset to an estimated buffer size and the requested writer selected.</returns>
        public static BinaryMessageWriter Rent(bool isBinary)
        {
            var messageWriter = s_threadInstance ?? new BinaryMessageWriter();

            // Reentrancy is not expected, but if the operation throws we dont
            // want to reuse the current messageWriter since XmlWriter might not be in starting state
            s_threadInstance = null;

            // Allocate first buffer
            messageWriter._stream.Reset(messageWriter.EstimateMessageSize());
            messageWriter._currentWriter = isBinary ? messageWriter._binaryWriter : messageWriter._textWriter;
            return messageWriter;
        }

        /// <summary>
        /// Finalizes a writer's output, captures the produced buffer memory, and prepares the writer for thread-local reuse.
        /// </summary>
        /// <param name="binaryMessageWriter">The BinaryMessageWriter whose output will be finalized and captured.</param>
        /// <param name="reset">If true, reinitializes the active writer's output to the internal stream so the writer can be reused immediately.</param>
        /// <returns>The buffer memory containing the message produced by the writer; the writer's underlying stream is reset.</returns>
        public static ArrayPoolStream.BufferMemory Return(BinaryMessageWriter binaryMessageWriter, bool reset = false)
        {
            binaryMessageWriter._currentWriter.Flush();
            binaryMessageWriter.RecordMessageSize((int)binaryMessageWriter._stream.Length);
            var res = binaryMessageWriter._stream.GetBufferMemoryAndReset();

            if (reset)
            {
                if (binaryMessageWriter._currentWriter is IXmlBinaryWriterInitializer binaryWriter)
                {
                    binaryWriter.SetOutput(binaryMessageWriter._stream, null, null, false);
                }
                else if (binaryMessageWriter._currentWriter is IXmlTextWriterInitializer textWriter)
                {
                    textWriter.SetOutput(binaryMessageWriter._stream, UTF8Encoding, false);
                }
            }

            s_threadInstance = binaryMessageWriter;
            return res;
        }

        public void Clear()
        {
            //reset writer ?
            _stream.Clear();
        }

        private void RecordMessageSize(int count)
        {
            _lastMessageLengths[_messageLengthIndex] = count;
            _messageLengthIndex = (_messageLengthIndex + 1) % MessageLengthHistorySize;
        }

        /// <summary>
        /// Get estimate based on maximum buffer size of the last few messages.
        /// <summary>
        /// Estimates an initial buffer size for the next message using recent message length history.
        /// </summary>
        /// <returns>The estimated buffer size in bytes: the larger of the two most recent maxima plus 256.</returns>
        private int EstimateMessageSize()
        {
            int max1 = Math.Max(_lastMessageLengths[3], _lastMessageLengths[2]);
            int max2 = Math.Max(_lastMessageLengths[1], _lastMessageLengths[0]);

            return Math.Max(max2, max1) + 256;
        }

        public XmlDictionaryWriter XmlWriter => _currentWriter;
    }
}