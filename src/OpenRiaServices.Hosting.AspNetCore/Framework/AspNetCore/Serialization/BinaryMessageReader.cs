using System;
using System.Xml;

namespace OpenRiaServices.Hosting.AspNetCore.Serialization
{
    /// <summary>
    /// Helper class to cache <see cref="XmlDictionaryReader"/>
    /// </summary>
    internal sealed class BinaryMessageReader : IDisposable
    {
        private readonly XmlDictionaryReader _binaryReader;
        private XmlDictionaryReader? _textReader;
        private XmlDictionaryReader _currentReader;

        // Cache at most one writer per thread
        [ThreadStatic]
        private static BinaryMessageReader? s_threadInstance;

        /// <summary>
        ///  Prevent creation from outside of this class
        /// <summary>
        /// Initializes a new instance and prepares an empty binary XmlDictionaryReader with maximum quotas, setting it as the current reader.
        /// </summary>
        private BinaryMessageReader()
        {
            _currentReader = _binaryReader = XmlDictionaryReader.CreateBinaryReader(Array.Empty<byte>(), XmlDictionaryReaderQuotas.Max);
        }

        /// <summary>
        /// Obtains a per-thread BinaryMessageReader configured to read the provided byte segment.
        /// </summary>
        /// <param name="bytes">The byte segment containing the XML payload; the segment's offset and count are used.</param>
        /// <param name="isBinary">True to initialize the binary XML reader; false to initialize the text XML reader.</param>
        /// <returns>A BinaryMessageReader whose active XmlDictionaryReader is initialized to read from the given segment.</returns>
        public static BinaryMessageReader Rent(ArraySegment<byte> bytes, bool isBinary)
        {
            var messageReader = s_threadInstance ?? new BinaryMessageReader();

            // Reentrancy is not expected, but if the operation throws we dont
            // want to reuse the current messageWriter since XmlWriter might not be in starting state
            s_threadInstance = null;

            if (isBinary)
            {
                ((IXmlBinaryReaderInitializer)messageReader._binaryReader).SetInput(bytes.Array!, bytes.Offset, bytes.Count, dictionary: null, XmlDictionaryReaderQuotas.Max, null, null);
                messageReader._currentReader = messageReader._binaryReader;
            }
            else
            {
                if (messageReader._textReader is IXmlTextReaderInitializer textReader)
                    textReader.SetInput(bytes.Array!, bytes.Offset, bytes.Count, encoding: null, XmlDictionaryReaderQuotas.Max, null);
                else
                    messageReader._textReader = XmlDictionaryReader.CreateTextReader(bytes.Array!, bytes.Offset, bytes.Count, XmlDictionaryReaderQuotas.Max);

                messageReader._currentReader = messageReader._textReader;
            }


            return messageReader;
        }

        /// <summary>
        /// Releases a BinaryMessageReader to the current thread's cache and closes its active XmlDictionaryReader.
        /// </summary>
        /// <param name="binaryMessageReader">The instance to return to the per-thread cache; its currently active reader will be closed.</param>
        public static void Return(BinaryMessageReader binaryMessageReader)
        {
            binaryMessageReader._currentReader.Close();
            s_threadInstance = binaryMessageReader;
        }

        /// <summary>
        /// Closes the active XmlDictionaryReader and stores this instance in the per-thread cache for reuse.
        /// </summary>
        public void Dispose()
        {
            Return(this);
        }

        public XmlDictionaryReader XmlDictionaryReader => _currentReader;
    }
}