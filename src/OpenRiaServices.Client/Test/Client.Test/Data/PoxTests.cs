using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel.Channels;
using System.Xml;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.DomainServices.Client.Test;

namespace OpenRiaServices.DomainServices.Client.Web.Test
{
    [TestClass]
    public class PoxTests
    {
        private static string ObjectDisposedText = new ObjectDisposedException(string.Empty).Message;
        private PoxBufferManager _bufferManager;
        private DisposableProperty _property;
        private MessageEncoder _encoder;
        private readonly string _simpleMessageAction = "simpleAction";
        private readonly Uri _simpleMessageTo = new Uri("http://simple/To");
        private byte[] _simpleMessageArray;
        private readonly string _simpleMessagePropertyIndex = "simpleProperty";
        private readonly string _simpleMessageString = "<simple>\"msg\"</simple>";

        [TestInitialize]
        public void Initialize()
        {
            this._bufferManager = new PoxBufferManager();
            this._property = new DisposableProperty();
            PoxBinaryMessageEncodingBindingElement pox = new PoxBinaryMessageEncodingBindingElement();
            MessageEncoderFactory factory = pox.CreateMessageEncoderFactory();
            this._encoder = factory.Encoder;
            this._simpleMessageArray = this.GetMessageArray(this._simpleMessageString);
        }

        [TestMethod]
        [Description("Basic Message property tests.")]
        public void PoxBufferedMessage_Properties()
        {
            using (Message message = this.GetMessage())
            {
                // Do the basic property comparisons.
                this.CompareMessageProperties(message);
            }

            // Closing the message should dispose the property.
            Assert.IsTrue(this._property.IsDisposed, "The property is not disposed.");

            // Test that pox can interpret fault messages.
            using (Message fault = Message.CreateMessage(this._encoder.MessageVersion, null, new NoneFaultBodyWriter()))
            {
                byte[] messageArray = this.GetMessageArray(fault);
                using (Message message = this.GetMessage(messageArray))
                {
                    Assert.IsTrue(message.IsFault, "Message is not a fault.");
                }
            }
        }

        [TestMethod]
        [Description("Message negative tests.")]
        public void PoxBufferedMessage_Negative()
        {
            // After disposal, some properties should throw.
            Message message = this.GetMessage();
            message.Close();

            ExceptionHelper.ExpectException<ObjectDisposedException>(() =>
                {
                    MessageHeaders headers = message.Headers;
                }, ObjectDisposedText);

            ExceptionHelper.ExpectException<ObjectDisposedException>(() =>
            {
                MessageProperties properties = message.Properties;
            }, ObjectDisposedText);

            // To be consistent with the framework, some properties should not throw.
            Assert.IsFalse(message.IsFault);
            Assert.IsNotNull(message.Version);
        }

        [TestMethod]
        [Description("Test the various Message write functionality.")]
        public void PoxBufferedMessage_Write()
        {
            const string xmlheader = "<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n";
            using (Message message = this.GetMessage())
            {
                // Ensure Write produces comparable xml.
                Assert.AreEqual(this._simpleMessageString, this.GetString(message), "WriteMessage");

                // Ensure ToString produces comparable xml.
                string toString = message.ToString();
                if (toString.StartsWith(xmlheader))
                {
                    // Silverlight produces strange utf-16 artifacts.
                    toString = toString.Substring(xmlheader.Length);
                }
                Assert.AreEqual(this._simpleMessageString, toString, "ToString");
            }
        }

#if !SILVERLIGHT
        [TestMethod]
        [Description("Verify configured reader quotas are passed all the way to the reader.")]
        public void PoxBufferedMessage_GetReader()
        {
            PoxBinaryMessageEncodingBindingElement pox = new PoxBinaryMessageEncodingBindingElement();
            pox.ReaderQuotas.MaxArrayLength = 1;
            pox.ReaderQuotas.MaxBytesPerRead = 1;
            pox.ReaderQuotas.MaxDepth = 1;
            pox.ReaderQuotas.MaxNameTableCharCount = 1;
            pox.ReaderQuotas.MaxStringContentLength = 1;
            MessageEncoderFactory factory = pox.CreateMessageEncoderFactory();
            this._encoder = factory.Encoder;

            // Test that the XmlDictionaryReaderQuotas propagate to the XmlDictionaryReader.
            using (Message message = this.GetMessage())
            using (XmlDictionaryReader reader = message.GetReaderAtBodyContents())
            {
                Assert.AreEqual(1, reader.Quotas.MaxArrayLength, "MaxArrayLength");
                Assert.AreEqual(1, reader.Quotas.MaxBytesPerRead, "MaxBytesPerRead");
                Assert.AreEqual(1, reader.Quotas.MaxDepth, "MaxDepth");
                Assert.AreEqual(1, reader.Quotas.MaxNameTableCharCount, "MaxNameTableCharCount");
                Assert.AreEqual(1, reader.Quotas.MaxStringContentLength, "MaxStringContentLength");
            }
        }

        [TestMethod]
        [Description("Basic MessageBuffer property tests.")]
        public void PoxBufferedMessage_BufferedCopy_Properties()
        {
            using (Message message = this.GetMessage())
            {
                using (MessageBuffer buffer = message.CreateBufferedCopy(0))
                {
                    // Basic property verification
                    Assert.AreEqual(this._simpleMessageArray.Length, buffer.BufferSize, "BufferSize");
                    Assert.AreEqual(this._encoder.ContentType, buffer.MessageContentType, "MessageContentType");
                }

                // The behavior should match the framework. After close, the original property
                // should not be disposed.
                Assert.IsFalse(this._property.IsDisposed, "The property is disposed.");
            }
        }

        [TestMethod]
        [Description("MessageBuffer negative tests.")]
        public void PoxBufferedMessage_BufferedCopy_Negative()
        {
            // write message throws argument null
            // All props throw after close, create message, write message throw after close

            using (Message message = this.GetMessage())
            {
                MessageBuffer buffer = message.CreateBufferedCopy(0);
                // WriteMessage throws ArgumentNullException.
                ExceptionHelper.ExpectArgumentNullException(() =>
                    {
                        buffer.WriteMessage(null);
                    }, "stream");

                // All members throw ObjectDisposedException after Close.
                buffer.Close();
                ExceptionHelper.ExpectException<ObjectDisposedException>(() =>
                    {
                        int bufferSize = buffer.BufferSize;
                    }, ObjectDisposedText);

                ExceptionHelper.ExpectException<ObjectDisposedException>(() =>
                    {
                        string contentType = buffer.MessageContentType;
                    }, ObjectDisposedText);

                ExceptionHelper.ExpectException<ObjectDisposedException>(() =>
                    {
                        buffer.CreateMessage();
                    }, ObjectDisposedText);

                ExceptionHelper.ExpectException<ObjectDisposedException>(() =>
                    {
                        using (MemoryStream stream = new MemoryStream())
                        {
                            buffer.WriteMessage(stream);
                        }
                    }, ObjectDisposedText);
            }
        }

        [TestMethod]
        [Description("Test the various MessageBuffer write functionality.")]
        public void PoxBufferedMessage_BufferedCopy_Write()
        {
            using (Message message = this.GetMessage())
            using (MessageBuffer buffer = message.CreateBufferedCopy(0))
            {
                using (Message copy1 = buffer.CreateMessage())
                using (Message copy2 = buffer.CreateMessage())
                {
                    // Verify buffered copies compare to original.
                    this.CompareMessage(copy1);
                    this.CompareMessage(copy2);
                }

                using (MemoryStream stream = new MemoryStream())
                {
                    // Verify WriteMessage compares to original.
                    buffer.WriteMessage(stream);
                    stream.Position = 0;
                    Assert.AreEqual(this._simpleMessageString, this.GetString(stream), "WriteMessage does not compare to the original.");
                }
            }
        }

        [TestMethod]
        [Description("Verify the buffer is returned exactly once.")]
        public void PoxBufferedMessage_BufferManagement()
        {
            Message message;
            XmlDictionaryReader reader;
            MessageBuffer buffer;
            int expectedCount;

            // Common case 1, reader is used and closed, message is closed.
            expectedCount = 0;
            message = this.GetMessage();
            reader = message.GetReaderAtBodyContents();

            // Ensure double close has no effect.
            for (int i = 0; i < 2; i++)
            {
                reader.Close();
                Assert.AreEqual(expectedCount, this._bufferManager.Count, "BufferManager.Count");
                message.Close();
                expectedCount = 1;
                Assert.AreEqual(expectedCount, this._bufferManager.Count, "Array should have been returned.");
            }

            // Common case 2, a buffer is used to create 2 messages. A reader is created from one
            // of the copies.
            this._bufferManager.Clear();
            message = this.GetMessage();
            buffer = message.CreateBufferedCopy(0);
            Message copy1 = buffer.CreateMessage();
            Message copy2 = buffer.CreateMessage();
            reader = copy2.GetReaderAtBodyContents();

            // Ensure double close has no effect.
            expectedCount = 0;
            for (int i = 0; i < 2; i++)
            {
                reader.Close();
                copy2.Close();
                copy1.Close();
                buffer.Close();
                Assert.AreEqual(expectedCount, this._bufferManager.Count, "BufferManager.Count");
                message.Close();
                expectedCount = 1;
                Assert.AreEqual(expectedCount, this._bufferManager.Count, "Array should have been returned.");
            }
        }
#endif

        #region Helpers
        private void CompareMessage(Message message)
        {
            this.CompareMessageProperties(message);
            Assert.AreEqual(this._simpleMessageString, this.GetString(message));
        }

        private void CompareMessageProperties(Message message)
        {
            Assert.AreEqual(this._simpleMessageAction, message.Headers.Action);
            Assert.AreEqual(this._simpleMessageTo, message.Headers.To);
            Assert.AreEqual(this._property, message.Properties[this._simpleMessagePropertyIndex], "Got different property");
            Assert.AreEqual(this._encoder.MessageVersion, message.Version);
            Assert.IsFalse(message.IsFault, "Message is a fault when it should not be.");
        }

        private Message GetMessage()
        {
            return this.GetMessage(this._simpleMessageArray);
        }

        private Message GetMessage(byte[] source)
        {
            int length = source.Length;
            byte[] copiedMessageArray = new byte[length];
            Buffer.BlockCopy(source, 0, copiedMessageArray, 0, length);
            ArraySegment<byte> simpleMessageBuffer = new ArraySegment<byte>(copiedMessageArray);
            Message message = this._encoder.ReadMessage(simpleMessageBuffer, this._bufferManager);
            message.Headers.Action = this._simpleMessageAction;
            message.Headers.To = this._simpleMessageTo;
            message.Properties.Add(this._simpleMessagePropertyIndex, this._property);
            return message;
        }

        private byte[] GetMessageArray(string source)
        {
            using (XmlReader reader = XmlReader.Create(new StringReader(source)))
            using (Message message = Message.CreateMessage(this._encoder.MessageVersion, null, reader))
            {
                return GetMessageArray(message);
            }
        }
        
        private byte[] GetMessageArray(Message source)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                this._encoder.WriteMessage(source, stream);
                return stream.ToArray();
            }
        }

        private string GetString(Message message)
        {
            using (MemoryStream stream = new MemoryStream())
            using (XmlDictionaryWriter writer = XmlDictionaryWriter.CreateBinaryWriter(stream))
            {
                message.WriteBodyContents(writer);
                writer.Flush();
                stream.Position = 0;
                return this.GetString(stream);
            }
        }

        private string GetString(MemoryStream stream)
        {
            using (XmlDictionaryReader reader = XmlDictionaryReader.CreateBinaryReader(stream, XmlDictionaryReaderQuotas.Max))
            {
                XDocument xDocument = XDocument.Load(reader);
                return xDocument.ToString();
            }
        }

        private class DisposableProperty : IDisposable
        {
            public bool IsDisposed
            {
                get;
                private set;
            }

            public void Dispose()
            {
                this.IsDisposed = true;
            }
        }

        private class NoneFaultBodyWriter : BodyWriter
        {
            private const string Fault = "Fault";
            private const string Namespace = "http://schemas.microsoft.com/ws/2005/05/envelope/none";

            public NoneFaultBodyWriter()
                : base(true)
            {
            }

            protected override BodyWriter OnCreateBufferedCopy(int maxBufferSize)
            {
                return base.OnCreateBufferedCopy(maxBufferSize);
            }

            protected override void OnWriteBodyContents(XmlDictionaryWriter writer)
            {
                writer.WriteElementString(Fault, Namespace, "Sender");
            }
        }

        private class PoxBufferManager : BufferManager
        {
            private readonly HashSet<byte[]> _buffers;

            public PoxBufferManager()
            {
                this._buffers = new HashSet<byte[]>();
                this.ThisLock = new object();
            }

            public int Count
            {
                get
                {
                    return this._buffers.Count;
                }
            }

            private object ThisLock
            {
                get;
                set;
            }

            public override void Clear()
            {
                lock (this.ThisLock)
                {
                    this._buffers.Clear();
                }
            }

            public override void ReturnBuffer(byte[] buffer)
            {
                lock (this.ThisLock)
                {
                    if (!this._buffers.Add(buffer))
                    {
                        throw new ArgumentException("buffer");
                    }
                }
            }

            public override byte[] TakeBuffer(int bufferSize)
            {
                lock (this.ThisLock)
                {
                    byte[] buffer = this._buffers.FirstOrDefault(b => b.Length >= bufferSize);

                    if (buffer != null)
                    {
                        this._buffers.Remove(buffer);
                    }
                    else
                    {
                        buffer = new byte[bufferSize];
                    }

                    return buffer;
                }
            }
        }
        #endregion
    }
}
