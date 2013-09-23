namespace OpenRiaServices.DomainServices.Hosting.OData
{
    #region Namespaces
    using OpenRiaServices.DomainServices.Server;

    #endregion

    /// <summary>Factory for creating data services on top of a domain service.</summary>
    internal class DomainDataServiceFactory
    {
        /// <summary>DomainService metadata object corresponding to domain service description.</summary>
        private DomainDataServiceMetadata domainDataServiceMetadata;

        /// <summary>Constructs factory for creating data services using the given domain service description.</summary>
        /// <param name="metadata">Data Service metadata object corresponding to domain service description.</param>
        internal DomainDataServiceFactory(DomainDataServiceMetadata metadata)
        {
            this.domainDataServiceMetadata = metadata;
        }

        /// <summary>Get the metadata useful for data service for the domain service.</summary>
        internal DomainDataServiceMetadata DomainDataServiceMetadata
        {
            get
            {
                return this.domainDataServiceMetadata;
            }
        }

        /// <summary>
        /// Creates a service instance where the <paramref name="result"/> represents the root query 
        /// result corresponding to current request.
        /// </summary>
        /// <param name="result">Root query result.</param>
        /// <returns>New instance of data service.</returns>
        internal DomainDataService CreateService(object result)
        {
            return new DomainDataService(this.DomainDataServiceMetadata, result);
        }
    }
}