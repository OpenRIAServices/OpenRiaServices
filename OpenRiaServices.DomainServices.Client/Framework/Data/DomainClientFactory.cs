using System;

namespace OpenRiaServices.DomainServices.Client
{
    /// <summary>
    /// Provides a base upon which classes implementing <see cref="IDomainClientFactory"/> should derive.
    /// 
    /// It provides some basic support for specifying the applications base uri using <see cref="ServerBaseUri"/> as 
    /// well as converts http: into https: when requiresSecureEndpoint is specified.
    /// </summary>
    public abstract class DomainClientFactory : IDomainClientFactory
    {
        private Uri _serverBaseUri;

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainClientFactory"/> class.
        /// </summary>
        public DomainClientFactory()
        {
            // For silverlight we set the service base based on the application source
#if SILVERLIGHT && !PORTABLE
            var application = System.Windows.Application.Current;
            if ((application != null) && (application.Host != null) && (application.Host.Source != null))
                ServerBaseUri = application.Host.Source;
#endif
        }

        /// <summary>
        /// Gets or sets a absolute base URI.
        /// </summary>
        /// <value>
        /// The server base URI.
        /// </value>
        public Uri ServerBaseUri
        {
            get
            {
                return _serverBaseUri;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                if (!value.IsAbsoluteUri)
                    throw new ArgumentException("ServiceBaseUri must be absolute", nameof(value));

                _serverBaseUri = value;
            }
        }

        /// <summary>
        /// Entry point for creating a a new <see cref="DomainClient" /> instance.
        /// 
        /// Arguments are validated and the <paramref name="serviceUri"/> is transformed if necessary based on <see cref="ServerBaseUri"/> and <paramref name="requiresSecureEndpoint"/>
        /// The actual creation is deferred to the <see cref="CreateDomainClientCore(Type, Uri, bool)"/> method.
        /// </summary>
        /// <param name="serviceContract">The service contract (not null).</param>
        /// <param name="serviceUri">The service URI (not null).</param>
        /// <param name="requiresSecureEndpoint"><c>true</c> if DomainService has the RequiresSecureEndpoint attribute and encryption should be enabled.</param>
        /// <exception cref="ArgumentNullException"><paramref name="serviceContract"/> or <paramref name="serviceUri"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="serviceContract"/>  is not an interface</exception>
        /// <returns>A <see cref="DomainClient"/> to use when communicating with the service</returns>
        public DomainClient CreateDomainClient(Type serviceContract, Uri serviceUri, bool requiresSecureEndpoint)
        {
            if (serviceContract == null)
                throw new ArgumentNullException(nameof(serviceContract));
            if (serviceUri == null)
                throw new ArgumentNullException(nameof(serviceUri));
            if (!TypeUtility.IsInterface(serviceContract))
                throw new ArgumentException(Resource.DomainClientFactory_ServiceContractMustBeAnInterface, "serviceContract");

            // Compose absolute uri
            if (!serviceUri.IsAbsoluteUri)
            {
                serviceUri = ComposeAbsoluteUri(serviceUri);
            }

            // We want to replace a http scheme (everything before the ':' in a Uri) with https.
            // Doing this via UriBuilder loses the OriginalString. Unfortunately, this leads
            // the builder to include the original port in the output which is not what we want.
            // To stay as close to the original Uri as we can, we'll just do some simple string
            // replacement.
            //
            // Desired output: http://my.domain/mySite.aspx -> https://my.domain/mySite.aspx
            // Builder output: http://my.domain/mySite.aspx -> https://my.domain:80/mySite.aspx
            //   The actual port is probably 443, but including it increases the cross-domain complexity.
            if (requiresSecureEndpoint && serviceUri.OriginalString.StartsWith("http:", StringComparison.OrdinalIgnoreCase))
            {
                serviceUri = new Uri("https:" + serviceUri.OriginalString.Substring(5 /*("http:").Length*/));
            }

            return CreateDomainClientCore(serviceContract, serviceUri, requiresSecureEndpoint);
        }

        /// <summary>
        /// Called when the service Uri is relative in order to convert it to an absolute uri,
        ///  this method uses the <see cref="ServerBaseUri"/> source to create an absolute Uri.
        /// </summary>
        protected virtual Uri ComposeAbsoluteUri(Uri serviceUri)
        {
            if (ServerBaseUri == null)
            {
#if SILVERLIGHT && !PORTABLE
                // WPF: if ((bool)(DesignerProperties.IsInDesignModeProperty.GetMetadata(typeof(DependencyObject)).DefaultValue))
                if (System.ComponentModel.DesignerProperties.IsInDesignTool)
                {
                    return serviceUri;
                }
#endif

                throw new InvalidOperationException(Resource.DomainClientFactory_UnableToCreateAbsoluteUri);
            }
                

            return new Uri(ServerBaseUri, serviceUri);
        }

        /// <summary>
        /// Creates the actual <see cref="DomainClient" /> instance.
        ///         
        /// This methods is called by the <see cref="DomainClientFactory"/> base class with almost the same arguments as
        /// to <see cref="CreateDomainClient"/> but where the original serviceUri has been processed so that if it was relative
        /// it is now absolute (based on ServerBaseUri) and is the endpoint must be secure then http:// has been changed to https://.
        /// </summary>
        /// <param name="serviceContract">The service contract (not null).</param>
        /// <param name="serviceUri">The service URI (not null, exanded based on <see cref="ServerBaseUri"/>).</param>
        /// <param name="requiresSecureEndpoint"><c>true</c> if DomainService has the RequiresSecureEndpoint attribute and encryption should be enabled.</param>
        /// <returns>A <see cref="DomainClient"/> to use when communicating with the service</returns>
        abstract protected DomainClient CreateDomainClientCore(Type serviceContract, Uri serviceUri, bool requiresSecureEndpoint);
    }
}
