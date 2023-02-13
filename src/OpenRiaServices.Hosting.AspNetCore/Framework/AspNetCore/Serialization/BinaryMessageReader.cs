using System;
using System.Buffers;
using System.Xml;

namespace OpenRiaServices.Hosting.AspNetCore.Serialization
{
    /// <summary>
    /// Helper class to cache <see cref="XmlDictionaryReader"/>
    /// </summary>
    internal sealed class BinaryMessageReader : IDisposable
    {
        private readonly XmlDictionaryReader _binaryReader;
        private XmlDictionaryReader _textReader;
        private XmlDictionaryReader _currentReader;

        // Cache at most one writer per thread
        [ThreadStatic]
        private static BinaryMessageReader s_threadInstance;

        /// <summary>
        ///  Prevent creation from outside of this class
        /// </summary>
        private BinaryMessageReader()
        {
            _binaryReader = XmlDictionaryReader.CreateBinaryReader(Array.Empty<byte>(), XmlDictionaryReaderQuotas.Max);
        }

        public static BinaryMessageReader Rent(ArraySegment<byte> bytes, bool isBinary)
        {
            var messageReader = s_threadInstance ?? new BinaryMessageReader();

            // Reentrancy is not expected, but if the operation throws we dont
            // want to reuse the current messageWriter since XmlWriter might not be in starting state
            s_threadInstance = null;

            if (isBinary)
            {
                ((IXmlBinaryReaderInitializer)messageReader._binaryReader).SetInput(bytes.Array, bytes.Offset, bytes.Count, dictionary: null, XmlDictionaryReaderQuotas.Max, null, null);
                messageReader._currentReader = messageReader._binaryReader;
            }
            else
            {
                if (messageReader._textReader is IXmlTextReaderInitializer textReader)
                    textReader.SetInput(bytes.Array, bytes.Offset, bytes.Count, encoding: null, XmlDictionaryReaderQuotas.Max, null);
                else
                    messageReader._textReader = XmlDictionaryReader.CreateTextReader(bytes.Array, bytes.Offset, bytes.Count, XmlDictionaryReaderQuotas.Max);

                messageReader._currentReader = messageReader._textReader;
            }


            return messageReader;
        }

        public static void Return(BinaryMessageReader binaryMessageReader)
        {
            binaryMessageReader._currentReader.Close();
            s_threadInstance = binaryMessageReader;
        }

        public void Dispose()
        {
            Return(this);
        }

        public XmlDictionaryReader XmlDictionaryReader => _currentReader;
    }
}
