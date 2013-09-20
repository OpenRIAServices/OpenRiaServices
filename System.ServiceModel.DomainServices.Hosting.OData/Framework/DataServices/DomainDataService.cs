namespace System.ServiceModel.DomainServices.Hosting.OData
{
    #region Namespaces
    using System.ComponentModel;
    using System.Data.Services;
    using System.Data.Services.Providers;
    #endregion

    /// <summary>
    /// Data service on top of a domain service.
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class DomainDataService : DataService<object>, IServiceProvider
    {
        /// <summary>
        /// Domain data service IDataServiceProvider implementations.
        /// </summary>
        private DomainDataServiceProvider provider;

        /// <summary>
        /// Creates a new domain data service instance.
        /// </summary>
        /// <param name="domainServiceDataServiceMetadata">Metadata for the domain data service.</param>
        /// <param name="result">Result of current request operation invocation.</param>
        internal DomainDataService(DomainDataServiceMetadata domainServiceDataServiceMetadata, object result)
        {
            this.provider = new DomainDataServiceProvider(domainServiceDataServiceMetadata, result);
        }

        /// <summary>Initializes configuration for the service instance.</summary>
        /// <param name="config">Configuration settings for the service.</param>
        public static void InitializeService(DataServiceConfiguration config)
        {
            // DEVNOTE(wbasheer): Currently exposing everyting.
            config.SetEntitySetAccessRule("*", EntitySetRights.AllRead);
            config.SetServiceOperationAccessRule("*", ServiceOperationRights.AllRead);
        }

        /// <summary>
        /// Gets the service object of the specified type.
        /// </summary>
        /// <param name="serviceType">An object that specifies the type of service object to get. </param>
        /// <returns>A service object of type serviceType.</returns>
        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(IDataServiceMetadataProvider))
            {
                return this.provider;
            }
            else
            if (serviceType == typeof(IDataServiceQueryProvider))
            {
                return this.provider;
            }

            return null;
        }
    }
}