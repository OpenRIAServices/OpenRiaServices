using System.Configuration;
using System.Web.Configuration;
using OpenRiaServices.Server;

namespace OpenRiaServices.Hosting
{
    /// <summary>
    /// Configures the <see cref="DomainService"/>s in this application.
    /// </summary>
    public class DomainServicesSection : ConfigurationSection
    {
        private readonly ConfigurationProperty propEndpoints;
        private static DomainServicesSection s_Current;

        /// <summary>
        /// Get DomainServicesSection specified in web.config under "system.serviceModel/domainServices"
        /// or creates a new one with default settings.
        /// </summary>
        public static DomainServicesSection Current
        {
            get
            {
                if (s_Current != null)
                    return s_Current;

                DomainServicesSection config = (DomainServicesSection)WebConfigurationManager.GetSection("system.serviceModel/domainServices");
                if (config == null)
                {
                    // Make sure we have a config instance, as that's where we put our default configuration. If we don't do this, our 
                    // binary endpoint won't be used when someone doesn't have a <domainServices/> section in their web.config.
                    config = new DomainServicesSection();
                    config.InitializeDefaultInternal();
                }
                s_Current = config;
                return config;
            }
        }

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
