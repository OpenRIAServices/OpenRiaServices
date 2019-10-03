using System;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading;
using System.Xml;

#if SERVERFX
namespace OpenRiaServices.DomainServices.Hosting
#else
namespace OpenRiaServices.DomainServices.Client
#endif
{
    /// <summary>
    /// The binding element that specifies the .NET Binary Format for XML used to encode messages.
    /// </summary>
    internal class PoxBinaryMessageEncodingBindingElement : MessageEncodingBindingElement
    {
        private readonly XmlDictionaryReaderQuotas _readerQuotas;

        public PoxBinaryMessageEncodingBindingElement()
        {
#if SILVERLIGHT
            this._readerQuotas = XmlDictionaryReaderQuotas.Max;
#else
            this._readerQuotas = new XmlDictionaryReaderQuotas();
            XmlDictionaryReaderQuotas.Max.CopyTo(this._readerQuotas);
#endif
        }

        public override MessageVersion MessageVersion
        {
            get
            {
                return MessageVersion.None;
            }
            set
            {
                if (MessageVersion.None != value)
                {
                    throw new ArgumentException(Resource.PoxBinaryMessageEncoder_MessageVersionNotSupported);
                }
            }
        }

#if !SILVERLIGHT
        public XmlDictionaryReaderQuotas ReaderQuotas
        {
            get
            {
                return this._readerQuotas;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                else
                {
                    value.CopyTo(this._readerQuotas);
                }
            }
        }
#endif

        public override MessageEncoderFactory CreateMessageEncoderFactory()
        {
            return new PoxBinaryMessageEncoderFactory(this._readerQuotas);
        }

        public override BindingElement Clone()
        {
            return new PoxBinaryMessageEncodingBindingElement();
        }

        public override IChannelFactory<TChannel> BuildChannelFactory<TChannel>(BindingContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            context.BindingParameters.Add(this);
            return context.BuildInnerChannelFactory<TChannel>();
        }

        public override bool CanBuildChannelFactory<TChannel>(BindingContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return context.CanBuildInnerChannelFactory<TChannel>();
        }

#if !SILVERLIGHT
        public override IChannelListener<TChannel> BuildChannelListener<TChannel>(BindingContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            context.BindingParameters.Add(this);
            return context.BuildInnerChannelListener<TChannel>();
        }

        public override bool CanBuildChannelListener<TChannel>(BindingContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return context.CanBuildInnerChannelListener<TChannel>();
        }
#endif

        // Factory that produces message encoders.
        private class PoxBinaryMessageEncoderFactory : MessageEncoderFactory
        {
            private PoxBinaryMessageEncoder _encoder;
            private readonly XmlDictionaryReaderQuotas _readerQuotas;

            public PoxBinaryMessageEncoderFactory(XmlDictionaryReaderQuotas readerQuotas)
            {
                if (readerQuotas == null)
                {
                    throw new ArgumentNullException(nameof(readerQuotas));
                }
                this._readerQuotas = readerQuotas;
            }

            public override MessageEncoder Encoder
            {
                get
                {
                    if (this._encoder == null)
                    {
                        this._encoder = new PoxBinaryMessageEncoder(this.MessageVersion, this._readerQuotas);
                    }
                    return this._encoder;
                }
            }

            public override MessageVersion MessageVersion
            {
                get
                {
                    return MessageVersion.None;
                }
            }
        }

        // Message encoder for Binary XML.
        internal class PoxBinaryMessageEncoder : MessageEncoder
        {
            private const string Fault = "Fault";
            private const string Namespace = "http://schemas.microsoft.com/ws/2005/05/envelope/none";
            private const string PoxBinaryContentType = "application/msbin1";
            private readonly MessageVersion _messageVersion;
            private readonly XmlDictionaryReaderQuotas _readerQuotas;

            public PoxBinaryMessageEncoder(MessageVersion messageVersion, XmlDictionaryReaderQuotas readerQuotas)
            {
                if (messageVersion == null)
                {
                    throw new ArgumentNullException(nameof(messageVersion));
                }
                if (readerQuotas == null)
                {
                    throw new ArgumentNullException(nameof(readerQuotas));
                }
                this._readerQuotas = readerQuotas;
                this._messageVersion = messageVersion;
            }

            public override string ContentType
            {
                get
                {
                    return PoxBinaryContentType;
                }
            }

            public override string MediaType
            {
                get
                {
                    return PoxBinaryContentType;
                }
            }

            public override MessageVersion MessageVersion
            {
                get
                {
                    return this._messageVersion;
                }
            }

            public override Message ReadMessage(ArraySegment<byte> buffer, BufferManager bufferManager, string contentType)
            {
                if (bufferManager == null)
                {
                    throw new ArgumentNullException(nameof(bufferManager));
                }

                ThrowIfIncorrectContentType(contentType);
#if !SILVERLIGHT
                PoxBinaryBufferedMessageData data = new PoxBinaryBufferedMessageData(buffer, bufferManager, this);
#else
                PoxBinaryBufferedMessageData data = new PoxBinaryBufferedMessageData(buffer, this);
#endif
                return new PoxBinaryBufferedMessage(data);
            }

            public override Message ReadMessage(Stream stream, int maxSizeOfHeaders, string contentType)
            {
                if (stream == null)
                {
                    throw new ArgumentNullException(nameof(stream));
                }

                ThrowIfIncorrectContentType(contentType);

                XmlDictionaryReader reader = XmlDictionaryReader.CreateBinaryReader(stream, this._readerQuotas);
                Message message = Message.CreateMessage(reader, maxSizeOfHeaders, this.MessageVersion);
                message.Properties.Encoder = this;

                return message;
            }

            public override ArraySegment<byte> WriteMessage(Message message, int maxMessageSize, BufferManager bufferManager, int messageOffset)
            {
                if (message == null)
                {
                    throw new ArgumentNullException(nameof(message));
                }

                if (bufferManager == null)
                {
                    throw new ArgumentNullException(nameof(bufferManager));
                }

                if (maxMessageSize < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(maxMessageSize));
                }

                this.ThrowIfInvalidMessageVersion(message);

                message.Properties.Encoder = this;

                /// PERF: 
                /// For further imprioved perf we should look into adopting a similar behaviour as for the 
                /// in binary encoding which performs *size prediction* 
                /// https://referencesource.microsoft.com/#System.ServiceModel/System/ServiceModel/Channels/BufferedMessageWriter.cs,ed72c7ce79a15637
                /// that should allow us to skip memory copies and further improve performance.
                /// 
                /// We should be able to pool both the stream and the binary writer togheter with size data
                using (var stream = new BufferManagerStream(bufferManager, messageOffset, minAllocationSize: 2 * 1024, maxAllocationSize: maxMessageSize))
                {
                    using (XmlDictionaryWriter writer = XmlDictionaryWriter.CreateBinaryWriter(stream))
                    {
                        message.WriteMessage(writer);
                        writer.Flush();

                        return stream.GetArrayAndDispose();
                    }
                }
            }

            /// <summary>
            /// Stream optimized for usage by <see cref="XmlBinaryWriter"/> without unneccessary 
            /// allocations on LOH.
            /// It writes directly to memory pooled by a <see cref="BufferManager"/> in order to 
            /// avoid allocations and be able to return memory directly without additional copies 
            /// (for small messages).
            /// </summary>
            internal class BufferManagerStream : Stream
            {
                private static readonly bool Is64BitProcess = Environment.Is64BitProcess;
                private readonly BufferManager _bufferManager;
                private readonly int _maxSize;
                // The offset into the final byte array where our content should start
                private readonly int _offset;
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
                    _bufferManager = bufferManager;
                    _offset = offset;
                    _bufferWritten = offset;
                    _maxSize = maxAllocationSize;
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

                        // Write bufffer
                        if (count <= _buffer.Length - _bufferWritten)
                        {
                            FastCopy(buffer, offset, _buffer, _bufferWritten, count);
                            _position += count;
                            _bufferWritten += count;
                            break;
                        }
                        else
                        {
                            // Fill _buffer
                            int toCopy = _buffer.Length - _bufferWritten;
                            FastCopy(buffer, offset, _buffer, _bufferWritten, toCopy);
                            _position += toCopy;
                            _bufferWritten += toCopy;
                            offset += toCopy;
                            count -= toCopy;
                        }
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

                    _buffer = _bufferManager.TakeBuffer(Math.Min(_position, _maxSize));
                    _bufferWritten = 0;
                }

                protected override void Dispose(bool disposing)
                {
                    try
                    {
                        base.Dispose(disposing);
                    }
                    finally
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
                }

                /// <summary>
                /// Copies bytes from <paramref name="src"/> to <paramref name="dest"/> using fastes 
                /// copy based on process bitness (x86 / x64)
                /// </summary>
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                        // or similar in managed code for smaller counts.
                        // Luckily the binary WxmlWriter will buffer writes up around 512 bytes
                        // So we do not expect a large number of writes with small counts
                        Buffer.BlockCopy(src, srcOffset, dest, destOffset, count);
                    }
                }

                public ArraySegment<byte> GetArrayAndDispose()
                {
                    // We only have a single segment, return it directly with no copying
                    if (_bufferList == null)
                    {
                        var buffer = _buffer;
                        _buffer = null;

                        System.Diagnostics.Debug.Assert(_bufferWritten == _position + _offset);
                        Dispose();
                        return new ArraySegment<byte>(buffer, _offset, _position);
                    }
                    else
                    {
                        int totalSize = _offset + _position;
                        var buffer = _bufferManager.TakeBuffer(totalSize);

                        // Copy in reverse order from filled to utilize CPU caches better
                        // _buffer might only be partially filled
                        int destOffset = totalSize - _bufferWritten;
                        FastCopy(_buffer, 0, buffer, destOffset, _bufferWritten);

                        // Buffers in list are all full
                        for (int i = _bufferList.Count - 1; i > 0; --i)
                        {
                            destOffset -= _bufferList[i].Length;
                            FastCopy(_bufferList[i], 0, buffer, destOffset, _bufferList[i].Length);
                        }

                        // First buffer might have offset
                        FastCopy(_bufferList[0], _offset, buffer, _offset, _bufferList[0].Length - _offset);
                        System.Diagnostics.Debug.Assert(destOffset - (_bufferList[0].Length - _offset) == _offset);

                        Dispose();
                        return new ArraySegment<byte>(buffer, _offset, _position);
                    }
                }
            }

            public override void WriteMessage(Message message, Stream stream)
            {
                if (message == null)
                {
                    throw new ArgumentNullException(nameof(message));
                }

                if (stream == null)
                {
                    throw new ArgumentNullException(nameof(stream));
                }

                this.ThrowIfInvalidMessageVersion(message);

                message.Properties.Encoder = this;
                XmlDictionaryWriter xmlWriter = XmlDictionaryWriter.CreateBinaryWriter(stream);
                message.WriteMessage(xmlWriter);
                xmlWriter.Flush();
            }

            private void ThrowIfInvalidMessageVersion(Message message)
            {
                if (message.Version != this.MessageVersion)
                {
                    throw new ProtocolException(string.Format(CultureInfo.CurrentCulture, Resource.PoxBinaryMessageEncoder_InvalidMessageVersion, message.Version, this.MessageVersion));
                }
            }

            private static void ThrowIfIncorrectContentType(string contentType)
            {
                if (contentType != null && contentType != PoxBinaryContentType && !contentType.StartsWith(PoxBinaryContentType, StringComparison.OrdinalIgnoreCase))
                {
                    throw new ProtocolException(string.Format(CultureInfo.CurrentCulture, Resource.PoxBinaryMessageEncoder_InvalidContentType, PoxBinaryContentType));
                }
            }

            /// <summary>
            /// A buffered message created by a <see cref="PoxBinaryMessageEncoder"/>.
            /// </summary>
            /// <remarks>The Message.CreateMessage overloads do not provide a non-streamed message
            /// that is comparable to the messages produced by the WCF framework. This disparity
            /// leads to both functional and performance pitfalls. This class is an approximation
            /// of a buffered message used by WCF, specifically engineered for PoxBinary (i.e.
            /// MessageVersion.None only).</remarks>
            private class PoxBinaryBufferedMessage : Message
            {
                private readonly PoxBinaryBufferedMessageData _data;
                private readonly MessageHeaders _headers;
                private readonly bool _isFault;
                private readonly MessageProperties _properties;
                private XmlDictionaryReader _reader;

                /// <summary>
                /// Constructs a <see cref="PoxBinaryBufferedMessage"/>.
                /// </summary>
                /// <param name="data">The message data containing the message contents.</param>
                internal PoxBinaryBufferedMessage(PoxBinaryBufferedMessageData data)
                {
                    this._data = data;
#if !SILVERLIGHT
                    this._data.Open();
#endif
                    this._headers = new MessageHeaders(data.Encoder.MessageVersion);
                    this._properties = new MessageProperties();
                    this._reader = this._data.TakeReader();
                    this._isFault = this._reader.IsStartElement(PoxBinaryMessageEncoder.Fault,
                        PoxBinaryMessageEncoder.Namespace);
                }

                /// <summary>
                /// The <see cref="MessageHeaders"/> object that represents the headers of this message.
                /// </summary>
                /// <exception cref="ObjectDisposedException" />
                public override MessageHeaders Headers
                {
                    get
                    {
                        if (this.IsDisposed)
                        {
                            throw PoxBinaryBufferedMessage.CreateMessageDisposedException();
                        }

                        return this._headers;
                    }
                }

                /// <summary>
                /// Returns <c>true</c> if this message contains a fault.
                /// </summary>
                public override bool IsFault
                {
                    get
                    {
                        return this._isFault;
                    }
                }

                /// <summary>
                /// The <see cref="MessageProperties"/> instance that represents the properties of
                /// this message.
                /// </summary>
                /// <exception cref="ObjectDisposedException" />
                public override MessageProperties Properties
                {
                    get
                    {
                        if (this.IsDisposed)
                        {
                            throw PoxBinaryBufferedMessage.CreateMessageDisposedException();
                        }

                        return this._properties;
                    }
                }

                /// <summary>
                /// The version of this message.
                /// </summary>
                public override MessageVersion Version
                {
                    get
                    {
                        return this._data.Encoder.MessageVersion;
                    }
                }

                /// <summary>
                /// Converts the message body to a string.
                /// </summary>
                /// <param name="writer">The writer to which the message is written.</param>
                protected override void OnBodyToString(XmlDictionaryWriter writer)
                {
                    using (XmlDictionaryReader reader = this._data.TakeReader())
                    {
                        PoxBinaryBufferedMessage.WriteContents(reader, writer);
                    }
                }

                /// <summary>
                /// Disposes all message resources.
                /// </summary>
                protected override void OnClose()
                {
                    Exception ex = null;

                    try
                    {
                        base.OnClose();
                    }
                    catch (Exception e)
                    {
                        if (e.IsFatal())
                        {
                            throw;
                        }

                        ex = e;
                    }

                    try
                    {
                        this._properties.Dispose();
                    }
                    catch (Exception e)
                    {
                        if (e.IsFatal())
                        {
                            throw;
                        }

                        if (ex == null)
                        {
                            ex = e;
                        }
                    }

                    try
                    {
                        if (this._reader != null)
                        {
                            this._reader.Close();
                        }
                    }
                    catch (Exception e)
                    {
                        if (e.IsFatal())
                        {
                            throw;
                        }

                        if (ex == null)
                        {
                            ex = e;
                        }
                    }

#if !SILVERLIGHT
                    try
                    {
                        this._data.Close();
                    }
                    catch (Exception e)
                    {
                        if (e.IsFatal())
                        {
                            throw;
                        }

                        if (ex == null)
                        {
                            ex = e;
                        }
                    }
#endif

                    if (ex != null)
                    {
                        throw ex;
                    }
                }

                /// <summary>
                /// Creates a buffer to store this message.
                /// </summary>
                /// <param name="maxBufferSize">The maximum size of the buffer to be created.</param>
                /// <returns>A buffer containing the message.</returns>
                /// <remarks>The <paramref name="maxBufferSize"/> is ignored. Since the message
                /// contents are shared, no additional buffers are created.</remarks>
                protected override MessageBuffer OnCreateBufferedCopy(int maxBufferSize)
                {
                    return new PoxBinaryBufferedMessageBuffer(this._data, this._headers,
                        this._properties);
                }

                /// <summary>
                /// Returns an <see cref="XmlDictionaryReader"/> positioned at the message contents.
                /// </summary>
                /// <returns>An <see cref="XmlDictionaryReader"/>.</returns>
                protected override XmlDictionaryReader OnGetReaderAtBodyContents()
                {
                    XmlDictionaryReader reader = this._reader;
                    this._reader = null;
                    return reader;
                }

                /// <summary>
                /// Writes the message body contents to the specified writer.
                /// </summary>
                /// <param name="writer">The <see cref="XmlDictionaryWriter"/> that is used to
                /// write the message body contents.</param>
                protected override void OnWriteBodyContents(XmlDictionaryWriter writer)
                {
                    using (XmlDictionaryReader reader = this.OnGetReaderAtBodyContents())
                    {
                        PoxBinaryBufferedMessage.WriteContents(reader, writer);
                    }
                }

                private static Exception CreateMessageDisposedException()
                {
                    return new ObjectDisposedException(string.Empty);
                }

                private static void WriteContents(XmlDictionaryReader reader,
                    XmlDictionaryWriter writer)
                {
                    if (reader.NodeType != XmlNodeType.Element)
                    {
                        reader.MoveToContent();
                    }

                    while (!reader.EOF)
                    {
                        writer.WriteNode(reader, false);
                    }
                }

                /// <summary>
                /// Represents a buffer that stores the data required to create a
                /// <see cref="PoxBinaryBufferedMessage"/> or its contents for future consumption.
                /// </summary>
                private class PoxBinaryBufferedMessageBuffer : MessageBuffer
                {
                    private readonly PoxBinaryBufferedMessageData _data;
                    private readonly MessageHeaders _headers;
                    private bool isClosed;
                    private readonly MessageProperties _properties;

                    /// <summary>
                    /// Constructs a <see cref="PoxBinaryBufferedMessageBuffer"/>
                    /// </summary>
                    /// <param name="data">The message data.</param>
                    /// <param name="headers">The message headers.</param>
                    /// <param name="properties">The message properties.</param>
                    internal PoxBinaryBufferedMessageBuffer(PoxBinaryBufferedMessageData data,
                        MessageHeaders headers, MessageProperties properties)
                    {
                        this._data = data;
#if !SILVERLIGHT
                        this._data.Open();
#endif
                        this._headers = new MessageHeaders(headers);
                        this._properties = new MessageProperties(properties);

                        this.ThisLock = new object();
                    }

                    /// <summary>
                    /// Gets the number of bytes consumed by this buffer.
                    /// </summary>
                    /// <exception cref="ObjectDisposedException" />
                    public override int BufferSize
                    {
                        get
                        {
                            lock (this.ThisLock)
                            {
                                if (this.isClosed)
                                {
                                    throw PoxBinaryBufferedMessageBuffer.CreateBufferDisposedException();
                                }

                                return this._data.Buffer.Count;
                            }
                        }
                    }

                    /// <summary>
                    /// Gets the type of content stored in this buffer.
                    /// </summary>
                    /// <exception cref="ObjectDisposedException" />
                    public override string MessageContentType
                    {
                        get
                        {
                            lock (this.ThisLock)
                            {
                                if (this.isClosed)
                                {
                                    throw PoxBinaryBufferedMessageBuffer.CreateBufferDisposedException();
                                }

                                return this._data.Encoder.ContentType;
                            }
                        }
                    }

                    private object ThisLock
                    {
                        get;
                        set;
                    }

                    /// <summary>
                    /// Closes the buffer.
                    /// </summary>
                    public override void Close()
                    {
                        lock (this.ThisLock)
                        {
                            if (!this.isClosed)
                            {
                                this.isClosed = true;
#if !SILVERLIGHT
                                this._data.Close();
#endif
                            }
                        }
                    }

                    /// <summary>
                    /// Returns an identical copy of the original message from which this buffer was
                    /// created.
                    /// </summary>
                    /// <returns>A copy of the original message.</returns>
                    /// <exception cref="ObjectDisposedException" />
                    public override Message CreateMessage()
                    {
                        lock (this.ThisLock)
                        {
                            if (this.isClosed)
                            {
                                throw PoxBinaryBufferedMessageBuffer.CreateBufferDisposedException();
                            }

                            PoxBinaryBufferedMessage destinationMessage = new PoxBinaryBufferedMessage(this._data);
                            destinationMessage.Headers.CopyHeadersFrom(this._headers);
                            destinationMessage.Properties.CopyProperties(this._properties);

                            return destinationMessage;
                        }
                    }

                    /// <summary>
                    /// Writes the entire contents of this buffer into the specified stream.
                    /// </summary>
                    /// <param name="stream">The stream that the buffer contents will be written to.</param>
                    /// <exception cref="ArgumentNullException" /> 
                    /// <exception cref="ObjectDisposedException" />
                    public override void WriteMessage(Stream stream)
                    {
                        if (stream == null)
                        {
                            throw new ArgumentNullException(nameof(stream));
                        }

                        lock (this.ThisLock)
                        {
                            if (this.isClosed)
                            {
                                throw PoxBinaryBufferedMessageBuffer.CreateBufferDisposedException();
                            }

                            stream.Write(this._data.Buffer.Array, this._data.Buffer.Offset,
                                this._data.Buffer.Count);
                        }
                    }

                    private static Exception CreateBufferDisposedException()
                    {
                        return new ObjectDisposedException(string.Empty);
                    }
                }
            }

            /// <summary>
            /// The message data class that holds shared state between messages, buffers, and
            /// messages created from a buffer.
            /// </summary>
            /// <remarks>The desktop framework allows callers to keep track of reader lifetimes.
            /// With this ability, the state can be ref counted across all users, and the buffer
            /// can be returned to its manager when all users release their references.
            /// The Silverlight framework does not allow callers to keep track of reader lifetimes.
            /// Given this, the buffer is not returned to its manager, but is cleaned up by the GC.
            /// Note, without a custom message, we would use messages returned from
            /// Message.CreateMessage. These messages cannot reuse buffers.</remarks>
            private class PoxBinaryBufferedMessageData
            {
#if !SILVERLIGHT
                private BufferManager _bufferManager;
                private readonly OnXmlDictionaryReaderClose _onReaderClose;
                private int _refCount;

                /// <summary>
                /// Constructs a <see cref="PoxBinaryBufferedMessageData"/>.
                /// </summary>
                /// <param name="buffer">The message contents.</param>
                /// <param name="bufferManager">The buffer manager from which
                /// <paramref name="buffer"/> came.</param>
                /// <param name="encoder">The encoder producing the message contents.</param>
                internal PoxBinaryBufferedMessageData(ArraySegment<byte> buffer,
                    BufferManager bufferManager, PoxBinaryMessageEncoder encoder)
                    : this(buffer, encoder)
                {
                    this._bufferManager = bufferManager;
                    this._onReaderClose = new OnXmlDictionaryReaderClose(this.OnReaderClose);
                }
#endif
                /// <summary>
                /// Constructs a <see cref="PoxBinaryBufferedMessageData"/>.
                /// </summary>
                /// <param name="buffer">The message contents.</param>
                /// <param name="encoder">The encoder producing the message contents.</param>
                internal PoxBinaryBufferedMessageData(ArraySegment<byte> buffer,
                    PoxBinaryMessageEncoder encoder)
                {
                    this.Buffer = buffer;
                    this.Encoder = encoder;
                }

                /// <summary>
                /// The message contents.
                /// </summary>
                public ArraySegment<byte> Buffer
                {
                    get;
                    private set;
                }

                /// <summary>
                /// The encoder producing the message contents.
                /// </summary>
                public PoxBinaryMessageEncoder Encoder
                {
                    get;
                    private set;
                }

#if !SILVERLIGHT
                /// <summary>
                /// Releases a reference on the message data.
                /// </summary>
                public void Close()
                {
                    if (Interlocked.Decrement(ref this._refCount) == 0)
                    {
                        this._bufferManager.ReturnBuffer(this.Buffer.Array);
                        this.Buffer = default(ArraySegment<byte>);
                        this._bufferManager = null;
                    }
                }

                /// <summary>
                /// Adds a reference on the message data.
                /// </summary>
                public void Open()
                {
                    Interlocked.Increment(ref this._refCount);
                }

                /// <summary>
                /// Returns an <see cref="XmlDictionaryReader"/> containing the message contents.
                /// </summary>
                /// <returns>An <see cref="XmlDictionaryReader"/> containing the message
                /// contents.</returns>
                public XmlDictionaryReader TakeReader()
                {
                    XmlDictionaryReader reader = XmlDictionaryReader.CreateBinaryReader(
                        this.Buffer.Array, this.Buffer.Offset, this.Buffer.Count, null,
                        this.Encoder._readerQuotas, null, this._onReaderClose);

                    this.Open();
                    return reader;
                }

                /// <summary>
                /// The callback method that is called when a reader returned from this message
                /// data closes.
                /// </summary>
                /// <param name="reader">The closing reader.</param>
                private void OnReaderClose(XmlDictionaryReader reader)
                {
                    this.Close();
                }
#else
                /// <summary>
                /// Returns an <see cref="XmlDictionaryReader"/> containing the message contents.
                /// </summary>
                /// <returns>An <see cref="XmlDictionaryReader"/> containing the message
                /// contents.</returns>
                public XmlDictionaryReader TakeReader()
                {
                    return XmlDictionaryReader.CreateBinaryReader(this.Buffer.Array,
                        this.Buffer.Offset, this.Buffer.Count, this.Encoder._readerQuotas);
                }
#endif
            }
        }
    }
}
