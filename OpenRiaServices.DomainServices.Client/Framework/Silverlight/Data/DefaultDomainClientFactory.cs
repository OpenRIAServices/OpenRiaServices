using System;

namespace OpenRiaServices.DomainServices.Client.Data
{
    /// <summary>
    /// Default DomainClientFactory which tries to create Domain clients using WebDomainClient.
    /// This class is only intended as a fallback to the old approach of creating domain clients, 
    /// in case the user references an old "OpenRiaServices.DomainServices.Client.Web" without a DomainClientFactory implementation.
    /// </summary>
    sealed class DefaultDomainClientFactory : DomainClientFactory
    {
        private readonly Type _webDomainClientType;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultDomainClientFactory"/> class.
        /// </summary>
        public DefaultDomainClientFactory()
        {
            // Look for the WebDomainClient in an assembly with the same version and with same signing key as this assembly
            var webDomainClientName = "OpenRiaServices.DomainServices.Client.WebDomainClient`1, "
                                    + typeof(DomainClient).Assembly.FullName.Replace("OpenRiaServices.DomainServices.Client", "OpenRiaServices.DomainServices.Client.Web");
            _webDomainClientType = Type.GetType(webDomainClientName);
        }

        /// <summary>
        /// Creates a WebDomainClient{serviceContract} using reflection.
        /// </summary>
        protected override DomainClient CreateDomainClientCore(Type serviceContract, Uri serviceUri, bool requiresSecureEndpoint)
        {
            // Try to create a WebDomainClient using reflection
            if (_webDomainClientType != null)
            {
                var domainClientType = _webDomainClientType.MakeGenericType(serviceContract);
                if (domainClientType == null)
                    throw new InvalidOperationException(Resource.DomainClientFactory_FaildToGetGenericDomainClient);

                return (DomainClient)Activator.CreateInstance(domainClientType, serviceUri, requiresSecureEndpoint);
            }

            throw new InvalidOperationException(Resource.DomainClientFactory_MissingClientWebReference);
        }
    }
}
