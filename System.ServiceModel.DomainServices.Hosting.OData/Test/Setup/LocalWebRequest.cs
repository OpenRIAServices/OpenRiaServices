namespace OpenRiaServices.DomainServices.Hosting.OData.UnitTests
{
    #region Namespaces
    using System;
    using System.Collections.Generic;
    using System.Data.Test.Astoria;
    using System.IO;
    using System.Net;
    using System.Diagnostics;
    #endregion
    /// <summary>
    /// Provides a TestWebRequest subclass that can handle requests through
    /// HTTP requests.
    /// </summary>
    internal class HttpBasedWebRequest : TestWebRequest
    {
        #region Fields.

        /// <summary>Uri location to the service entry point.</summary>
        protected string serviceEntryPointLocation;

        /// <summary>Full request URI string, based on RequestUriString, including protocol and host name.</summary>
        private string fullRequestUri;

        /// <summary>Response to the last web server request sent.</summary>
        private WebResponse response;

        /// <summary>Response stream to the last web server request sent.</summary>
        private Stream responseStream;

        #endregion Fields.

        #region Properties.

        /// <summary>Uri location to the service entry point.</summary>
        public override string BaseUri
        {
            get
            {
                return this.serviceEntryPointLocation;
            }
        }

        /// <summary>Gets response headers for this request.</summary>
        public override Dictionary<string, string> ResponseHeaders
        {
            get
            {
                Dictionary<string, string> responseHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                WebHeaderCollection headers = this.response.Headers;
                foreach (string header in headers.AllKeys)
                {
                    responseHeaders[header] = string.Join(",", headers.GetValues(header));
                }

                return responseHeaders;
            }
        }

        /// <summary>Cache-Control header in response.</summary>
        public override string ResponseCacheControl
        {
            get { return this.response.Headers[HttpResponseHeader.CacheControl]; }
        }

        /// <summary>Location header for response.</summary>
        public override string ResponseLocation
        {
            get { return this.response.Headers[HttpResponseHeader.Location]; }
        }

        /// <summary>Value for the DataServiceVersion response header.</summary>
        public override string ResponseVersion
        {
            get { return this.response.Headers["DataServiceVersion"]; }
        }

        /// <summary>ContentType header for response.</summary>
        public override string ResponseContentType
        {
            get
            {
                return response.ContentType;
            }
        }

        public override string ResponseETag
        {
            get { return this.response.Headers[HttpResponseHeader.ETag]; }
        }

        public override int ResponseStatusCode
        {
            get { return (int)((HttpWebResponse)this.response).StatusCode; }
        }

        /// <summary>Full request URI string, based on RequestUriString, including protocol and host name.</summary>
        public override string FullRequestUriString
        {
            get
            {
                if (this.fullRequestUri == null)
                {
                    if (serviceEntryPointLocation == null)
                    {
                        this.StartService();
                    }

                    if (this.RequestUriString.StartsWith(serviceEntryPointLocation))
                    {
                        return this.RequestUriString;
                    }
                    else
                    {
                        return serviceEntryPointLocation + this.RequestUriString;
                    }
                }
                else
                {
                    return this.fullRequestUri;
                }
            }
            set
            {
                if (value.StartsWith(serviceEntryPointLocation))
                {
                    this.RequestUriString = value.Substring(serviceEntryPointLocation.Length);
                }
                else
                {
                    this.RequestUriString = null;
                }

                this.fullRequestUri = value;
            }
        }

        /// <summary>Gets or sets the original URI of the request.</summary>
        public override string RequestUriString
        {
            get { return base.RequestUriString; }
            set
            {
                base.RequestUriString = value;
                this.fullRequestUri = null;
            }
        }

        #endregion properties.

        #region Methods.

        /// <summary>Gets the response stream, as returned by the server.</summary>
        public override Stream GetResponseStream()
        {
            return responseStream;
        }

        /// <summary>
        /// Creates a WebRequest object.
        /// </summary>
        /// <param name="fullUri">Uri to service.</param>
        /// <returns>WebRequest object</returns>
        protected virtual HttpWebRequest CreateWebRequest(string fullUri)
        {
            return (HttpWebRequest)System.Net.WebRequest.Create(fullUri);
        }

        /// <summary>Sends the current request to the server.</summary>
        public override void SendRequest()
        {
            if (this.responseStream != null)
            {
                this.responseStream.Close();
                this.responseStream = null;
            }

            if (this.response != null)
            {
                this.response.Close();
                this.response = null;
            }

            string fullUri = this.FullRequestUriString;
            Debug.WriteLine("Sending request: {0} [{1}]", this.HttpMethod, fullUri);
            HttpWebRequest request = CreateWebRequest(fullUri);
            request.Method = this.HttpMethod;
            if (request.Method == "GET")
            {
                // Content-Length or Chunked Encoding cannot be set for an operation that does not write data.
                request.SendChunked = false;
            }

            if (this.RequestContentLength != -1)
            {
                request.ContentLength = this.RequestContentLength;
            }
            if (this.RequestContentType != null)
            {
                request.ContentType = this.RequestContentType;
            }
            if (this.Accept != null)
            {
                ((HttpWebRequest)request).Accept = this.Accept;
            }
            if (this.AcceptCharset != null)
            {
                request.Headers["Accept-Charset"] = this.AcceptCharset;
            }
            if (!String.IsNullOrEmpty(this.IfMatch))
            {
                request.Headers["If-Match"] = this.IfMatch;
            }
            if (!String.IsNullOrEmpty(this.IfNoneMatch))
            {
                request.Headers["If-None-Match"] = this.IfNoneMatch;
            }
            if (this.RequestMaxVersion != null)
            {
                request.Headers["MaxDataServiceVersion"] = this.RequestMaxVersion;
            }
            if (this.RequestVersion != null)
            {
                request.Headers["DataServiceVersion"] = this.RequestVersion;
            }

            foreach (var p in this.RequestHeaders)
            {
                request.Headers[p.Key] = p.Value;
            }

            if (this.RequestStream != null)
            {
                using (Stream requestStream = request.GetRequestStream())
                {
                    IOUtil.CopyStream(this.RequestStream, requestStream);
                    if (this.RequestStream.CanSeek)
                    {
                        this.RequestStream.Position = 0;
                    }
                }
            }

            try
            {
                this.response = request.GetResponse();
                this.responseStream = this.response.GetResponseStream();
            }
            catch (WebException webException)
            {
                Trace.WriteLine("WebException: " + webException.Message);
                System.Net.WebResponse exceptionResponse = webException.Response;
                if (exceptionResponse == null)
                {
                    Trace.WriteLine("  Response: null");
                }
                else
                {
                    this.response = exceptionResponse;
                    this.responseStream = TestUtil.EnsureStreamWithSeek(this.response.GetResponseStream());
                    Trace.WriteLine("  Response: " + new StreamReader(this.responseStream).ReadToEnd());
                    this.responseStream.Position = 0;
                }
                throw;
            }
        }

        #endregion Methods.
    }

    /// <summary>
    /// Provides a TestWebRequest subclass that can handle requests in a local debugging web server.
    /// </summary>
    internal class LocalWebRequest : HttpBasedWebRequest
    {
        /// <summary>Name of the service file under test, with no path information.</summary>
        private const string ServiceFileName = "service.svc";

        /// <summary>Last service type setup.</summary>
        private Type lastServiceType;

        /// <summary>
        /// Shuts down or kills the debugging web server.
        /// </summary>
        /// <param name="disposing">
        /// Whether the call is being made from an explicit call to 
        /// IDisposable.Dispose() rather than through the finalizer.
        /// </param>
        protected override void Dispose(bool disposing)
        {
            ShutdownLocalWeb();
            base.Dispose(disposing);
        }

        /// <summary>Starts the service.</summary>
        public override void StartService()
        {
            this.SetupLocalWeb();
        }

        /// <summary>Stops the service.</summary>
        public override void StopService()
        {
            this.ShutdownLocalWeb();
        }

        public void SetupLocalWeb()
        {
            if (lastServiceType != this.ServiceType)
            {
                ShutdownLocalWeb();
                SetupServiceFiles();
                LocalWebServerHelper.StartWebServer();
                this.lastServiceType = this.ServiceType;
            }
        }

        public void ShutdownLocalWeb()
        {
            LocalWebServerHelper.Cleanup();
            lastServiceType = null;
        }

        /// <summary>Sends the current request to the server.</summary>
        public override void SendRequest()
        {
            SetupLocalWeb();
            base.SendRequest();
        }

        /// <summary>
        /// Sets up the required files locally to test the web data service
        /// through the local web server.
        /// </summary>
        private void SetupServiceFiles()
        {
            serviceEntryPointLocation = LocalWebServerHelper.SetupServiceFiles(
                ServiceFileName,
                this.ServiceType);
        }
    }
}
