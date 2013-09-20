namespace System.ServiceModel.DomainServices.Hosting.OData
{
    #region Namespaces
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Dispatcher;
    using System.Text;
    using System.Xml;
    #endregion

    /// <summary>
    /// Error handler for domain data service exceptions. Uses the data service format 
    /// for serializing exceptions. Currently only uses the application/xml mime format.
    /// </summary>
    internal class DomainDataServiceErrorHandler : IErrorHandler
    {
        /// <summary>
        /// Enables error-related processing and returns a value that indicates whether the dispatcher 
        /// aborts the session and the instance context in certain cases. 
        /// </summary>
        /// <param name="error">The exception thrown during processing.</param>
        /// <returns>true if Windows Communication Foundation (WCF) should not abort the session (if there is one) and instance context 
        /// if the instance context is not Single; otherwise, false. The default is false.</returns>
        public bool HandleError(Exception error)
        {
            return error is DomainDataServiceException;
        }

        /// <summary>
        /// Enables the creation of a custom FaultException that is returned from an exception in the course of a service method.
        /// </summary>
        /// <param name="error">The Exception object thrown in the course of the service operation.</param>
        /// <param name="version">The SOAP version of the message.</param>
        /// <param name="fault">The Message object that is returned to the client, or service, in the duplex case.</param>
        public void ProvideFault(Exception error, MessageVersion version, ref Message fault)
        {
            Debug.Assert(error != null, "error != null");
            Debug.Assert(version != null, "version != null");

            HttpStatusCode statusCode;
            Action<Stream> exceptionWriter = DomainDataServiceErrorHandler.HandleException(error, out statusCode);

            Message message = null;
            try
            {
                message = Message.CreateMessage(MessageVersion.None, string.Empty, new DelegateBodyWriter(exceptionWriter));
                message.Properties.Add(WebBodyFormatMessageProperty.Name, new WebBodyFormatMessageProperty(WebContentFormat.Raw));

                HttpResponseMessageProperty response = new HttpResponseMessageProperty();
                response.Headers[HttpResponseHeader.ContentType] = ServiceUtils.MimeApplicationXml;
                response.Headers[ServiceUtils.HttpDataServiceVersion] = ServiceUtils.DataServiceVersion1Dot0 + ";";
                response.StatusCode = statusCode;

                if (statusCode == HttpStatusCode.MethodNotAllowed)
                {
                    DomainDataServiceException e = (error as DomainDataServiceException);
                    if (e != null)
                    {
                        response.Headers[HttpResponseHeader.Allow] = e.ResponseAllowHeader;
                    }
                }

                message.Properties.Add(HttpResponseMessageProperty.Name, response);

                fault = message;
                message = null;
            }
            finally
            {
                if (message != null)
                {
                    ((IDisposable)message).Dispose();
                }
            }
        }

        /// <summary>
        /// Creates a delegate used for serializing the exception to outgoing stream.
        /// </summary>
        /// <param name="exception">Exception to serialize.</param>
        /// <param name="statusCode">Http status code from <paramref name="exception"/>.</param>
        /// <returns>Delegate that serializes the <paramref name="exception"/>.</returns>
        private static Action<Stream> HandleException(Exception exception, out HttpStatusCode statusCode)
        {
            Debug.Assert(TypeUtils.IsCatchableExceptionType(exception), "WebUtil.IsCatchableExceptionType(exception)");
            Debug.Assert(exception != null, "exception != null");

            DomainDataServiceException e = exception as DomainDataServiceException;
            statusCode = e != null ? (HttpStatusCode)e.StatusCode : HttpStatusCode.InternalServerError;

            return new ErrorSerializer(exception).SerializeXmlErrorToStream;
        }

        /// <summary>Use this class to handle writing body contents using a callback.</summary>
        private class DelegateBodyWriter : BodyWriter
        {
            #region Fields.

            /// <summary>Writer action callback.</summary>
            private readonly Action<Stream> writerAction;

            #endregion Fields.

            /// <summary>Initializes a new <see cref="DelegateBodyWriter"/> instance.</summary>
            /// <param name="writer">Callback for writing.</param>
            internal DelegateBodyWriter(Action<Stream> writer)
                : base(false)
            {
                Debug.Assert(writer != null, "writer != null");
                this.writerAction = writer;
            }

            /// <summary>Called when the message body is written to an XML file.</summary>
            /// <param name="writer">
            /// An <see cref="XmlDictionaryWriter"/> that is used to write this 
            /// message body to an XML file.
            /// </param>
            protected override void OnWriteBodyContents(XmlDictionaryWriter writer)
            {
                Debug.Assert(writer != null, "writer != null");

                try
                {
                    writer.WriteStartElement(ServiceUtils.WcfBinaryElementName);
                    using (XmlWriterStream stream = new XmlWriterStream(writer))
                    {
                        this.writerAction(stream);
                    }

                    writer.WriteEndElement();
                }
                finally
                {
                    // We will always abort the channel in case a domain service exception occurs.
                    var ctx = System.ServiceModel.OperationContext.Current;
                    if (ctx != null)
                    {
                        ctx.Channel.Abort();
                    }
                }
            }

            /// <summary>Use this class to write to an <see cref="XmlDictionaryWriter"/>.</summary>
            private class XmlWriterStream : Stream
            {
                /// <summary>Target writer.</summary>
                private XmlDictionaryWriter innerWriter;

                /// <summary>Initializes a new <see cref="XmlWriterStream"/> instance.</summary>
                /// <param name="xmlWriter">Target writer.</param>
                internal XmlWriterStream(XmlDictionaryWriter xmlWriter)
                {
                    Debug.Assert(xmlWriter != null, "xmlWriter != null");
                    this.innerWriter = xmlWriter;
                }

                /// <summary>Gets a value indicating whether the current stream supports reading.</summary>
                public override bool CanRead
                {
                    get { return false; }
                }

                /// <summary>Gets a value indicating whether the current stream supports seeking.</summary>
                public override bool CanSeek
                {
                    get { return false; }
                }

                /// <summary>Gets a value indicating whether the current stream supports writing.</summary>
                public override bool CanWrite
                {
                    get { return true; }
                }

                /// <summary>Gets the length in bytes of the stream.</summary>
                public override long Length
                {
                    get { throw new NotSupportedException(); }
                }

                /// <summary>Gets or sets the position within the current stream.</summary>
                public override long Position
                {
                    get { throw new NotSupportedException(); }
                    set { throw new NotSupportedException(); }
                }

                /// <summary>
                /// Clears all buffers for this stream and causes any buffered 
                /// data to be written to the underlying device.
                /// </summary>
                public override void Flush()
                {
                    this.innerWriter.Flush();
                }

                /// <summary>
                /// Reads a sequence of bytes from the current stream and 
                /// advances the position within the stream by the number of bytes read.
                /// </summary>
                /// <param name="buffer">
                /// An array of bytes. When this method returns, the buffer contains 
                /// the specified byte array with the values between <paramref name="offset"/> 
                /// and (<paramref name="offset"/> + <paramref name="count"/> - 1) replaced 
                /// by the bytes read from the current source.
                /// </param>
                /// <param name="offset">
                /// The zero-based byte offset in <paramref name="buffer"/> at which to 
                /// begin storing the data read from the current stream.
                /// </param>
                /// <param name="count">
                /// The maximum number of bytes to be read from the current stream.
                /// </param>
                /// <returns>The total number of bytes read into the buffer.</returns>
                public override int Read(byte[] buffer, int offset, int count)
                {
                    throw new NotSupportedException();
                }

                /// <summary>Sets the position within the current stream.</summary>
                /// <param name="offset">
                /// A byte offset relative to the <paramref name="origin"/> parameter.
                /// </param>
                /// <param name="origin">
                /// A value of type <see cref="SeekOrigin"/> indicating the reference 
                /// point used to obtain the new position.
                /// </param>
                /// <returns>The new position within the current stream.</returns>
                public override long Seek(long offset, SeekOrigin origin)
                {
                    throw new NotSupportedException();
                }

                /// <summary>Sets the length of the current stream.</summary>
                /// <param name="value">New value for length.</param>
                public override void SetLength(long value)
                {
                    throw new NotSupportedException();
                }

                /// <summary>
                /// Writes a sequence of bytes to the current stream and advances 
                /// the current position within this stream by the number of 
                /// bytes written. 
                /// </summary>
                /// <param name="buffer">
                /// An array of bytes. This method copies <paramref name="count"/> 
                /// bytes from <paramref name="buffer"/> to the current stream.
                /// </param>
                /// <param name="offset">
                /// The zero-based byte offset in buffer at which to begin copying 
                /// bytes to the current stream.
                /// </param>
                /// <param name="count">
                /// The number of bytes to be written to the current stream.
                /// </param>
                public override void Write(byte[] buffer, int offset, int count)
                {
                    this.innerWriter.WriteBase64(buffer, offset, count);
                }
            }
        }

        /// <summary>
        /// Performs the actual task of exception serialization.
        /// </summary>
        private class ErrorSerializer
        {
            #region Private Fields.

            /// <summary>Default error response encoding.</summary>
            private static Encoding defaultEncoding = new UTF8Encoding(false, true);

            /// <summary>Exception to serialize.</summary>
            private Exception _exception;
            
            #endregion

            #region Constructors.

            /// <summary>Initializes a new <see cref="ErrorSerializer"/> instance.</summary>
            /// <param name="e">Exception to handle.</param>
            internal ErrorSerializer(Exception e)
            {
                Debug.Assert(e != null, "exception != null");
                this._exception = e;
            }

            #endregion Constructors.

            /// <summary>Serializes the current exception description to the specified <paramref name="stream"/>.</summary>
            /// <param name="stream">Stream to write to.</param>
            internal void SerializeXmlErrorToStream(Stream stream)
            {
                Debug.Assert(stream != null, "stream != null");
                using (XmlWriter writer = ServiceUtils.CreateXmlWriterAndWriteProcessingInstruction(stream, ErrorSerializer.defaultEncoding))
                {
                    Debug.Assert(writer != null, "writer != null");
                    writer.WriteStartElement(ServiceUtils.XmlErrorElementName, ServiceUtils.DataWebMetadataNamespace);
                    string errorCode, message, messageLang;
                    DomainDataServiceException dataException = ExtractErrorValues(this._exception, out errorCode, out message, out messageLang);

                    writer.WriteStartElement(ServiceUtils.XmlErrorCodeElementName, ServiceUtils.DataWebMetadataNamespace);
                    writer.WriteString(errorCode);
                    writer.WriteEndElement();   // </code>

                    writer.WriteStartElement(ServiceUtils.XmlErrorMessageElementName, ServiceUtils.DataWebMetadataNamespace);
                    writer.WriteAttributeString(
                        ServiceUtils.XmlNamespacePrefix,    // prefix
                        ServiceUtils.XmlLangAttributeName,  // localName
                        null,                               // ns
                        messageLang);                       // value
                    writer.WriteString(message);
                    writer.WriteEndElement();   // </message>

                    // Always assuming verbose errors.
                    Exception exception = (dataException == null) ? this._exception : dataException.InnerException;
                    
                    SerializeXmlException(writer, exception);

                    writer.WriteEndElement();   // </error>
                    writer.Flush();
                }
            }

            /// <summary>Serializes an exception in XML format.</summary>
            /// <param name='writer'>Writer to which error should be serialized.</param>
            /// <param name='exception'>Exception to serialize.</param>
            private static void SerializeXmlException(XmlWriter writer, Exception exception)
            {
                string elementName = ServiceUtils.XmlErrorInnerElementName;
                int nestingDepth = 0;
                while (exception != null)
                {
                    // Inner Error Tag namespace changed to DataWebMetadataNamespace
                    // Maybe DataWebNamespace should be used on all error tags? Up to debate...
                    // NOTE: this is a breaking change from V1
                    writer.WriteStartElement(elementName, ServiceUtils.DataWebMetadataNamespace);
                    nestingDepth++;

                    string exceptionMessage = exception.Message ?? String.Empty;
                    writer.WriteStartElement(ServiceUtils.XmlErrorMessageElementName, ServiceUtils.DataWebMetadataNamespace);
                    writer.WriteString(exceptionMessage);
                    writer.WriteEndElement();   // </message>

                    string exceptionType = exception.GetType().FullName;
                    writer.WriteStartElement(ServiceUtils.XmlErrorTypeElementName, ServiceUtils.DataWebMetadataNamespace);
                    writer.WriteString(exceptionType);
                    writer.WriteEndElement();   // </type>

                    string exceptionStackTrace = exception.StackTrace ?? String.Empty;
                    writer.WriteStartElement(ServiceUtils.XmlErrorStackTraceElementName, ServiceUtils.DataWebMetadataNamespace);
                    writer.WriteString(exceptionStackTrace);
                    writer.WriteEndElement();   // </stacktrace>

                    exception = exception.InnerException;
                    elementName = ServiceUtils.XmlErrorInternalExceptionElementName;
                }

                while (nestingDepth > 0)
                {
                    writer.WriteEndElement();   // </innererror>
                    nestingDepth--;
                }
            }

            /// <summary>
            /// Gets values describing the <paramref name='exception' /> if it's a DomainDataServiceException;
            /// defaults otherwise.
            /// </summary>
            /// <param name='exception'>Exception to extract value from.</param>
            /// <param name='errorCode'>Error code from the <paramref name='exception' />; blank if not available.</param>
            /// <param name='message'>Message from the <paramref name='exception' />; blank if not available.</param>
            /// <param name='messageLang'>Message language from the <paramref name='exception' />; current default if not available.</param>
            /// <returns>The cast DataServiceException; possibly null.</returns>
            private static DomainDataServiceException ExtractErrorValues(Exception exception, out string errorCode, out string message, out string messageLang)
            {
                DomainDataServiceException dataException = exception as DomainDataServiceException;
                if (dataException != null)
                {
                    errorCode = dataException.ErrorCode ?? string.Empty;
                    message = dataException.Message ?? string.Empty;
                    messageLang = dataException.MessageLanguage ?? CultureInfo.CurrentCulture.Name;
                    return dataException;
                }
                else
                {
                    errorCode = string.Empty;
                    message = Resource.DomainDataService_General_Error;
                    messageLang = CultureInfo.CurrentCulture.Name;
                    return null;
                }
            }
        }
    }
}