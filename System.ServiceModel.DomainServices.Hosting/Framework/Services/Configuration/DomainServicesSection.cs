using System.Configuration;
using System.ServiceModel.DomainServices.Server;

namespace System.ServiceModel.DomainServices.Hosting
{
    /// <summary>
    /// Configures the <see cref="DomainService"/>s in this application.
    /// </summary>
    public class DomainServicesSection : ConfigurationSection
    {
        private ConfigurationProperty propEndpoints;

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainServicesSection"/> class.
        /// </summary>
        public DomainServicesSection()
        {
            this.propEndpoints = new ConfigurationProperty("endpoints", typeof(ProviderSettingsCollection), null, ConfigurationPropertyOptions.None);
        }

        /// <summary>
        /// Gets the collection of endpoints that should be used by every <see cref="DomainService"/> in this application.
        /// </summary>
        [ConfigurationProperty("endpoints")]
        public ProviderSettingsCollection Endpoints
        {
            get
            {
                return (ProviderSettingsCollection)this[this.propEndpoints];
            }
        }

        /// <summary>
        /// Allows internal code to initialize this object's defaults.
        /// </summary>
        internal void InitializeDefaultInternal()
        {
            this.InitializeDefault();
        }

        /// <summary>
        /// Used to initialize a default set of values for the <see cref="ConfigurationElement"/> object.
        /// </summary>
        /// <remarks>
        /// Registers the default endpoints as well.
        /// </remarks>
        protected override void InitializeDefault()
        {
            base.InitializeDefault();
            this.Endpoints.Clear();
            this.Endpoints.Add(new ProviderSettings("binary", typeof(PoxBinaryEndpointFactory).AssemblyQualifiedName));
        }
    }
}
