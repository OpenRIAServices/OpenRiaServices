using System;
using System.ServiceModel;

namespace OpenRiaServices.Hosting.Wcf.OData
{
    #region Namespaces
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.ServiceModel.Activation;
    using System.ServiceModel.Description;
    using System.ServiceModel.Web;
    using System.Text;
    using System.Xml;
    #endregion

    /// <summary>Utility methods used by host creation code.</summary>
    internal static class ServiceUtils
    {
        /// <summary>Empty object array.</summary>
        internal static readonly object[] EmptyObjectArray = Array.Empty<object>();

        /// <summary>Default contract namespace.</summary>
        internal const string DefaultNamespace = "http://tempuri.org/";

        /// <summary>Name/Configuration name for the contract.</summary>
        internal const string DomainDataServiceContractName = "DomainDataServiceContract";

        /// <summary>Name of the data service end point.</summary>
        internal const string DomainDataServiceEndPointName = "/dataservice";

        /// <summary>Operation name for service document request.</summary>
        internal const string ServiceDocumentOperationName = "ServiceDocument";

        /// <summary>Operation name for metadata request.</summary>
        internal const string MetadataOperationName = "$metadata";

        /// <summary>Name of URI template match results property for a message.</summary>
        internal const string UriTemplateMatchResultsPropertyName = "UriTemplateMatchResults";

        /// <summary>Wildcard pattern that matches everything.</summary>
        internal const string MatchAllWildCard = "*";

        /// <summary>Post fix for all resource sets.</summary>
        internal const string ResourceSetPostFix = "Set";

        /// <summary>HTTP GET method name.</summary>
        internal const string HttpGetMethodName = "GET";

        /// <summary>HTTP POST method name.</summary>
        internal const string HttpPostMethodName = "POST";

        /// <summary>"Binary" - WCF element name for binary content in XML-wrapping streams.</summary>
        internal const string WcfBinaryElementName = "Binary";

        /// <summary>MIME type for XML bodies.</summary>
        internal const string MimeApplicationXml = "application/xml";

        /// <summary>XML element name for an error.</summary>
        internal const string XmlErrorElementName = "error";

        /// <summary>XML element name for an error code.</summary>
        internal const string XmlErrorCodeElementName = "code";

        /// <summary>XML element name for the inner error details.</summary>
        internal const string XmlErrorInnerElementName = "innererror";

        /// <summary>XML element name for an internal exception.</summary>
        internal const string XmlErrorInternalExceptionElementName = "internalexception";

        /// <summary>XML element name for an exception type.</summary>
        internal const string XmlErrorTypeElementName = "type";

        /// <summary>XML element name for an exception stack trace.</summary>
        internal const string XmlErrorStackTraceElementName = "stacktrace";

        /// <summary>XML element name for an error message.</summary>
        internal const string XmlErrorMessageElementName = "message";

        /// <summary>XML namespace for data service annotations.</summary>
        internal const string DataWebMetadataNamespace = "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata";

        /// <summary> Schema Namespace prefix For xml.</summary>
        internal const string XmlNamespacePrefix = "xml";

        /// <summary>'lang' XML attribute name for annotation language.</summary>
        internal const string XmlLangAttributeName = "lang";

        /// <summary>'DataServiceVersion' - HTTP header name for data service version.</summary>
        internal const string HttpDataServiceVersion = "DataServiceVersion";

        /// <summary>'1.0' - the version 1.0 text for a data service.</summary>
        internal const string DataServiceVersion1Dot0 = "1.0";

        /// <summary>Extension to service file.</summary>
        private const string ServiceFileExtension = ".svc";

        /// <summary>Locks used detecting default authentication scheme, credential type used to be done only once.</summary>
        private static readonly object _inspectorLock = new object();

        /// <summary>Detector for default authentication scheme, credential type.</summary>
        private static WebServiceHostInspector _inspector;

        /// <summary>Gets the default authentication scheme supported by the server.</summary>
        internal static AuthenticationSchemes AuthenticationScheme
        {
            get
            {
                if (_inspector == null)
                {
                    lock (_inspectorLock)
                    {
                        if (_inspector == null)
                        {
                            WebServiceHostInspector inspector = new WebServiceHostInspector();
                            inspector.Inspect();
                            _inspector = inspector;
                        }
                    }
                }

                return _inspector.AuthenticationScheme;
            }
        }

        /// <summary>Gets the default credential type supported by the server.</summary>
        internal static HttpClientCredentialType CredentialType
        {
            get
            {
                if (_inspector == null)
                {
                    lock (_inspectorLock)
                    {
                        if (_inspector == null)
                        {
                            WebServiceHostInspector inspector = new WebServiceHostInspector();
                            inspector.Inspect();
                            _inspector = inspector;
                        }
                    }
                }
                return _inspector.CredentialType;
            }
        }

        /// <summary>
        /// Get the action representing the request.
        /// </summary>
        /// <param name="contractName">Contract name.</param>
        /// <param name="opname">Operation name.</param>
        /// <param name="action">Request action.</param>
        /// <returns>String representing the request action.</returns>
        internal static string GetRequestMessageAction(ContractDescription contractName, string opname, string action)
        {
            return ServiceUtils.GetMessageAction(contractName, opname, action, /* isResponse */ false);
        }

        /// <summary>
        /// Get the action representing the response.
        /// </summary>
        /// <param name="contractName">Contract name.</param>
        /// <param name="opname">Operation name.</param>
        /// <param name="action">Request action.</param>
        /// <returns>String representing the response action.</returns>
        internal static string GetResponseMessageAction(ContractDescription contractName, string opname, string action)
        {
            return ServiceUtils.GetMessageAction(contractName, opname, action, /* isResponse */ true);
        }

        /// <summary>
        /// Adds the behavior of given type to the contract description.
        /// </summary>
        /// <typeparam name="T">Type of behavior.</typeparam>
        /// <param name="contractDesc">Contract description.</param>
        /// <returns>Added behavior.</returns>
        internal static T EnsureBehavior<T>(ContractDescription contractDesc) where T : IContractBehavior, new()
        {
            T behavior = contractDesc.Behaviors.Find<T>();
            if (behavior == null)
            {
                behavior = new T();
                contractDesc.Behaviors.Insert(0, behavior);
            }

            return behavior;
        }

        /// <summary>
        /// Add the behavior to the operation description.
        /// </summary>
        /// <typeparam name="T">Type of behavior.</typeparam>
        /// <param name="operationDesc">Operation description.</param>
        /// <returns>Added behavior.</returns>
        internal static T EnsureBehavior<T>(OperationDescription operationDesc) where T : IOperationBehavior, new()
        {
            T behavior = operationDesc.Behaviors.Find<T>();
            if (behavior == null)
            {
                behavior = new T();
                operationDesc.Behaviors.Insert(0, behavior);
            }

            return behavior;
        }

        /// <summary>Set the read quotas for the end point.</summary>
        /// <param name="readerQuotas">Reader quotas for the end point.</param>
        internal static void SetReaderQuotas(XmlDictionaryReaderQuotas readerQuotas)
        {
            readerQuotas.MaxArrayLength = Int32.MaxValue;
            readerQuotas.MaxBytesPerRead = Int32.MaxValue;
            readerQuotas.MaxDepth = Int32.MaxValue;
            readerQuotas.MaxNameTableCharCount = Int32.MaxValue;
            readerQuotas.MaxStringContentLength = Int32.MaxValue;
        }

        /// <summary>
        /// Creates a new XmlWriter instance using the specified stream and writers the processing instruction
        /// with the given encoding value
        /// </summary>
        /// <param name="stream"> The stream to which you want to write</param>
        /// <param name="encoding"> Encoding that you want to specify in the reader settings as well as the processing instruction </param>
        /// <returns>XmlWriter with the appropriate xml settings and processing instruction</returns>
        internal static XmlWriter CreateXmlWriterAndWriteProcessingInstruction(Stream stream, Encoding encoding)
        {
            Debug.Assert(null != stream, "null != stream");
            Debug.Assert(null != encoding, "null != encoding");

            XmlWriterSettings settings = CreateXmlWriterSettings(encoding);
            XmlWriter writer = XmlWriter.Create(stream, settings);
            writer.WriteProcessingInstruction("xml", "version=\"1.0\" encoding=\"" + encoding.WebName + "\" standalone=\"yes\"");
            return writer;
        }

        /// <summary>
        /// Creates a new XmlWriterSettings instance using the encoding.
        /// </summary>
        /// <param name="encoding"> Encoding that you want to specify in the reader settings as well as the processing instruction </param>
        /// <returns>XmlWriterSettings instance.</returns>
        internal static XmlWriterSettings CreateXmlWriterSettings(Encoding encoding)
        {
            Debug.Assert(null != encoding, "null != encoding");

            // No need to close the underlying stream here for client,
            // since it always MemoryStream for writing i.e. it caches the response before processing.
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.CheckCharacters = false;
            settings.ConformanceLevel = ConformanceLevel.Fragment;
            settings.Encoding = encoding;
            settings.Indent = true;
            settings.NewLineHandling = NewLineHandling.Entitize;

            Debug.Assert(!settings.CloseOutput, "!settings.CloseOutput -- otherwise default changed?");

            return settings;
        }

        /// <summary>
        /// Obtains the action name based on contract, operation and direction of action.
        /// </summary>
        /// <param name="contractName">Contract name.</param>
        /// <param name="opname">Operation name.</param>
        /// <param name="action">Request or response action.</param>
        /// <param name="isResponse">true if response action, false if request action.</param>
        /// <returns>String representing the action.</returns>
        private static string GetMessageAction(ContractDescription contractName, string opname, string action, bool isResponse)
        {
            if (action != null)
            {
                return action;
            }
            StringBuilder builder = new StringBuilder(0x40);
            if (string.IsNullOrEmpty(contractName.Namespace))
            {
                builder.Append("urn:");
            }
            else
            {
                builder.Append(contractName.Namespace);
                if (!contractName.Namespace.EndsWith("/", StringComparison.Ordinal))
                {
                    builder.Append('/');
                }
            }
            builder.Append(contractName.Name);
            builder.Append('/');
            action = isResponse ? (opname + "Response") : opname;
            return UriUtils.CombineUriStrings(builder.ToString(), action);
        }

        #region WebServiceHostInspector

        /// <summary>
        /// Uses the WCF <see cref="WebServiceHost"/> to get the default authentication scheme
        /// and credential type for services on the current server.
        /// </summary>
        private class WebServiceHostInspector : WebServiceHost
        {
            private readonly CommunicationException _exception = new CommunicationException(Resource.Communication_NoChannelMustBeOpened);

            /// <summary>Default is no authentication scheme.</summary>
            private AuthenticationSchemes _authenticationScheme = AuthenticationSchemes.None;

            /// <summary>Default is no credential type.</summary>
            private HttpClientCredentialType _credentialType = HttpClientCredentialType.None;


            /// <summary>Constructor for host inspector.</summary>
            internal WebServiceHostInspector()
                : base(
                typeof(OpenRiaServices.Hosting.Wcf.OData.ServiceUtils.WebServiceHostInspector.Service),
                new Uri("http://OpenRiaServices.Hosting.Wcf.OData.ServiceUtils.WebServiceHostInspector.Service.svc"))
            {
            }

            /// <summary>Gets the default authentication scheme.</summary>
            internal AuthenticationSchemes AuthenticationScheme
            {
                get
                {
                    return this._authenticationScheme;
                }
            }

            /// <summary>Gets the default credential type.</summary>
            internal HttpClientCredentialType CredentialType
            {
                get
                {
                    return this._credentialType;
                }
            }

            /// <summary>Detects the default authentication scheme and credential type.</summary>
            public void Inspect()
            {
                try
                {
                    // The WebServerHost determines the default endpoint settings in OnOpening().
                    // We're calling into Open() to get the process started. We want to avoid
                    // actually opening the channel and initializing a socket so we'll abort the
                    // process once we get the endpoint information we need. There is no API for
                    // otherwise determing the authentication scheme.
                    this.Open();
                }
                catch (CommunicationException e)
                {
                    // We will always throw _exception from OnOpening if given the opportunity
                    if (e != this._exception)
                    {
                        throw;
                    }
                }
            }

            /// <summary>Perform the actual detection of authentication scheme and credential type in WCF event handler.</summary>
            protected override void OnOpening()
            {
                base.OnOpening();

                ServiceEndpoint se = this.Description.Endpoints.FirstOrDefault();

                if (se != null)
                {
                    WebHttpBinding whb = se.Binding as WebHttpBinding;

                    if (whb != null)
                    {
                        this._credentialType = whb.Security.Transport.ClientCredentialType;

                        switch (this._credentialType)
                        {
                            case HttpClientCredentialType.Basic:
                                this._authenticationScheme = AuthenticationSchemes.Basic;
                                break;
                            case HttpClientCredentialType.Digest:
                                this._authenticationScheme = AuthenticationSchemes.Digest;
                                break;
                            case HttpClientCredentialType.Ntlm:
                                this._authenticationScheme = AuthenticationSchemes.Ntlm;
                                break;
                            case HttpClientCredentialType.Windows:
                                this._authenticationScheme = AuthenticationSchemes.Negotiate;
                                break;
                            default:
                                break;
                        }

                        // We've successfully determined the authentication scheme, now we'll
                        // abort the open process to avoid touching system resources.
                        throw this._exception;
                    }
                }

                // base.OnOpening() should fail before we ever get here, but even when it doesn't
                // we want to alert users of problems creating the default endpoints.
                // DEVNOTE(wbasheer): Use localized error strings.
                throw new CommunicationException(Resource.Communication_NoDefaultAuthenticationScheme);
            }

            /// <summary>Dummy service contract used by the host inspector.</summary>
            [ServiceContract]
            private interface IService
            {
                [OperationContract]
                void Operation();
            }

            /// <summary>Dummy service used by host inspector.</summary>
            [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
            private class Service : IService
            {
                public void Operation()
                {
                }
            }
        }

        #endregion
    }
}