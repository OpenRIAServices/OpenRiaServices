using System;
using System.Buffers;
using System.Xml;

namespace OpenRiaServices.Hosting.AspNetCore.Serialization
{
    /// <summary>
    /// Helper class to cache <see cref="XmlDictionaryReader"/>
    /// </summary>
    internal class BinaryMessageReader : IDisposable
    {
        private readonly XmlDictionaryReader _reader;

        // Cache at most one writer per thread
        [ThreadStatic]
        private static BinaryMessageReader s_threadInstance;

        /// <summary>
        ///  Prevent creation from outside of this class
        /// </summary>
        private BinaryMessageReader()
        {
            _reader = XmlDictionaryReader.CreateBinaryReader(Array.Empty<byte>(), XmlDictionaryReaderQuotas.Max);
        }

        public static BinaryMessageReader Rent(ArraySegment<byte> bytes)
        {
            var messageReader = s_threadInstance ?? new BinaryMessageReader();

            // Reentrancy is not expected, but if the operation throws we dont
            // want to reuse the current messageWriter since XmlWriter might not be in starting state
            s_threadInstance = null;

            ((IXmlBinaryReaderInitializer)messageReader._reader).SetInput(bytes.Array, bytes.Offset, bytes.Count, dictionary: null, XmlDictionaryReaderQuotas.Max, null, null);
            return messageReader;
        }

        public static void Return(BinaryMessageReader binaryMessageReader)
        {
            binaryMessageReader._reader.Close();
            s_threadInstance = binaryMessageReader;
        }

        public void Dispose()
        {
            Return(this);
        }

        public XmlDictionaryReader XmlDictionaryReader => _reader;
    }
}
