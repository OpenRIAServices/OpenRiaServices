namespace OpenRiaServices.DomainServices.Hosting.OData.UnitTests
{
    #region Namespaces
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net.Mime;
    using OpenRiaServices.DomainServices.Server;
    using System.Text;
    using System.Xml;
    using System.Xml.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    #endregion

    /// <summary>
    /// Provides support to make tests reusable across in-memory, local and remote web servers.
    /// </summary>
    [DebuggerDisplay("{HttpMethod} {FullRequestUriString}")]
    internal abstract class TestWebRequest : IDisposable
    {
        #region Fields.

        /// <summary>Set to true to get additional stack traces for troubleshooting.</summary>
        internal static bool SaveOriginalStackTrace = false;

        /// <summary>Actual type of service to use.</summary>
        protected Type serviceType;

        /// <summary>Headers used in this request.</summary>
        private Dictionary<string, string> requestHeaders;

        #endregion Fields.

        /// <summary>Creates a TestWebRequest that can run with a local, easy-to-debu server.</summary>
        /// <returns>A new TestWebRequest instance.</returns>
        public static TestWebRequest CreateForLocal()
        {
            return new LocalWebRequest();
        }

        /// <summary>
        /// Initializes a new TestWebRequest instance with simple defaults 
        /// that have no side-effects on querying.
        /// </summary>
        protected TestWebRequest()
        {
            this.HttpMethod = "GET";
            this.RequestContentLength = -1;
            this.requestHeaders = new Dictionary<string, string>();
        }

        /// <summary>Disposes object resources from the Finalizer thread.</summary>
        ~TestWebRequest()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Provides an opportunity to clean-up resources.
        /// </summary>
        /// <param name="disposing">
        /// Whether the call is being made from an explicit call to 
        /// IDisposable.Dispose() rather than through the finalizer.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
        }

        /// <summary>
        /// Releases resources held onto by this object.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        #region Properties.

        /// <summary>Uri location to the service entry point.</summary>
        public virtual string BaseUri
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public virtual Uri ServiceRoot
        {
            get
            {
                return new Uri(this.BaseUri, UriKind.Absolute);
            }
        }

        /// <summary>Value for the Accept header (MIME specification).</summary>
        public string Accept
        {
            get;
            set;
        }

        /// <summary>Value for the Accept-Charset header.</summary>
        public string AcceptCharset
        {
            get;
            set;
        }

        /// <summary>Full request URI string, based on RequestUriString, including protocol and host name.</summary>
        public virtual string FullRequestUriString
        {
            get { return this.RequestUriString; }
            set { throw new NotSupportedException(); }
        }

        /// <summary>Gets custom headers for this request.</summary>
        public Dictionary<string, string> RequestHeaders
        {
            get
            {
                return this.requestHeaders;
            }
        }

        /// <summary>Gets or sets the method for the request.</summary>
        public string HttpMethod
        {
            get;
            set;
        }

        /// <summary>Gets or sets the value for the MaxDataServiceVersion request header.</summary>
        public string RequestMaxVersion
        {
            get;
            set;
        }

        /// <summary>Gets or sets the method for the request.</summary>
        public Stream RequestStream
        {
            get;
            set;
        }

        /// <summary>Gets or sets the value for the DataServiceVersion request header.</summary>
        public string RequestVersion
        {
            get;
            set;
        }

        /// <summary>Gets or sets the original URI of the request.</summary>
        public virtual string RequestUriString
        {
            get;
            set;
        }

        /// <summary>Gets response headers for this request.</summary>
        public abstract Dictionary<string, string> ResponseHeaders
        {
            get;
        }

        /// <summary>Cache-Control header in response.</summary>
        public abstract string ResponseCacheControl
        {
            get;
        }

        /// <summary>ContentType header for response.</summary>
        public abstract string ResponseContentType
        {
            get;
        }

        /// <summary>Location header for response.</summary>
        public abstract string ResponseLocation
        {
            get;
        }

        /// <summary>ETag header in response, available after SendRequest.</summary>
        public abstract string ResponseETag
        {
            get;
        }

        /// <summary>Response status code, available after SendRequest.</summary>
        public abstract int ResponseStatusCode
        {
            get;
        }

        /// <summary>Gets or sets the value for the DataServiceVersion response header.</summary>
        public abstract string ResponseVersion
        {
            get;
        }

        /// <summary>Gets the HTTP MIME type of the input stream.</summary>
        public string RequestContentType
        {
            get;
            set;
        }

        /// <summary>Gets/Sets the length of the request content stream in bytes.</summary>
        public int RequestContentLength
        {
            get;
            set;
        }

        /// <summary>Actual type of service to run against, eg DataService&lt;NorthwindContext&gt;.</summary>
        public virtual Type ServiceType
        {
            get
            {
                return this.serviceType;
            }

            set
            {
                this.serviceType = value;

                if (!typeof(DomainService).IsAssignableFrom(this.serviceType))
                {
                    throw new Exception("ServiceType should be of type DomainService.");
                }
            }
        }

        /// <summary> Gets/Sets If-Match header value</summary>
        public string IfMatch
        {
            get;
            set;
        }

        /// <summary> Gets/Sets If-None-Match header value</summary>
        public string IfNoneMatch
        {
            get;
            set;
        }

        #endregion Properties.

        /// <summary>
        /// Returns the server response stream after a call to SendRequest.
        /// </summary>
        /// <returns>The server response stream after a call to SendRequest.</returns>
        public abstract Stream GetResponseStream();

        /// <summary>
        /// Returns the server response text after a call to SendRequest.
        /// </summary>
        /// <returns>The server response text after a call to SendRequest.</returns>
        public string GetResponseStreamAsText()
        {
            Stream stream = this.GetResponseStream();
            if (stream == null)
            {
                throw new InvalidOperationException("GetResponseStream() returned null - ensure SendRequest was called before.");
            }

            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        /// <summary>
        /// Returns the server response text after a call to SendRequest.
        /// </summary>
        /// <returns>The server response text after a call to SendRequest.</returns>
        public XmlDocument GetResponseStreamAsXmlDocument()
        {
            using (Stream stream = this.GetResponseStream())
            {
                if (stream == null)
                {
                    throw new InvalidOperationException("GetResponseStream() returned null - ensure SendRequest was called before.");
                }

                XmlDocument document = new XmlDocument(TestUtil.TestNameTable);
                document.Load(stream);
                return document;
            }
        }

        /// <summary>
        /// Returns the server response text after a call to SendRequest.
        /// </summary>
        /// <returns>The server response text after a call to SendRequest.</returns>
        public virtual XmlDocument GetResponseStreamAsXmlDocument(string responseFormat)
        {
            Assert.IsTrue(responseFormat == TestUtil.AtomFormat, "Expecting atom format.");
            return this.GetResponseStreamAsXmlDocument();
        }

        /// <summary>
        /// Returns the server response as XML after a call to SendRequest.
        /// </summary>
        /// <returns>The server response parsed as XML and returned as XDocument instance.</returns>
        public XDocument GetResponseStreamAsXDocument()
        {
            using (Stream stream = this.GetResponseStream())
            {
                if (stream == null)
                {
                    throw new InvalidOperationException("GetResponseStream() returned null - ensure SendRequest was called before.");
                }

                XDocument document = XDocument.Load(XmlReader.Create(stream));
                return document;
            }
        }

        /// <summary>Sets the <see cref="RequestStream"/> to the specified UTF-8 text.</summary>
        /// <param name="text">Text to set.</param>
        public void SetRequestStreamAsText(string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            this.RequestStream = new MemoryStream(bytes);
            this.RequestContentLength = (int)this.RequestStream.Length;
        }

        /// <summary>Sends the current request to the server.</summary>
        public abstract void SendRequest();

        /// <summary>
        /// Sends the current request to the server, and tries to throw an 
        /// exception if the response indicates an error occurred.
        /// </summary>
        public virtual void SendRequestAndCheckResponse()
        {
            this.SendRequest();
            string mediaType = new ContentType(this.ResponseContentType).MediaType;
            SerializationFormatData data = SerializationFormatData.Values
                                                                  .Single(format => format.MimeTypes.Any(m => String.Equals(m, mediaType, StringComparison.OrdinalIgnoreCase)));
            if (data.IsStructured)
            {
                Stream stream = TestUtil.EnsureStreamWithSeek(this.GetResponseStream());
                XmlReader reader = XmlReader.Create(stream);
                while (reader.Read())
                {
                    if (reader.LocalName == "error")
                    {
                        throw this.CreateExceptionFromError(XElement.Load(reader, LoadOptions.PreserveWhitespace));
                    }
                }

                stream.Position = 0;
            }
            else if (data.Name == "Text")
            {
                string text = this.GetResponseStreamAsText();
                if (text.Contains("<?xml"))
                {
                    throw new Exception(text);
                }
            }
            else
            {
                // TODO: implement.
            }
        }

        /// <summary>Starts the service.</summary>
        public virtual void StartService()
        {
        }

        /// <summary>Stops the service.</summary>
        public virtual void StopService()
        {
        }

        /// <summary>
        /// Generate Exception from xml response.
        /// </summary>
        public Exception CreateExceptionFromError(XmlElement element)
        {
            Debug.Assert(element != null, "element != null");
            string message = element.SelectSingleNode("./message", TestUtil.TestNamespaceManager).InnerText;
            Exception result = new Exception(message);
            return result;
        }

        /// <summary>
        /// Generate Exception from xml response.
        /// </summary>
        public Exception CreateExceptionFromError(XElement element)
        {
            Debug.Assert(element != null, "element != null");
            string message = element.Elements().Where(e => e.Name.LocalName == "message").Single().Value;
            Exception result = new Exception(message);
            return result;
        }
    }
}
