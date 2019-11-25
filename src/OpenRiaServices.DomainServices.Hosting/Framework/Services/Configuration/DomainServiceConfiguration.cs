using System;
using System.Collections.Generic;
using System.Configuration;

namespace OpenRiaServices.DomainServices.Hosting
{
    namespace Internal
    {
        public class DomainServiceConfiguration
        {
            private static Lazy<DomainServiceConfiguration> s_domainServiceConfiguration = new Lazy<DomainServiceConfiguration>(CreateConfiguration, System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);
            private DomainServiceConfiguration() { }

            static DomainServiceConfiguration CreateConfiguration()
            {
                var instance = new DomainServiceConfiguration();
                foreach(ProviderSettings provider in DomainServicesSection.Current.Endpoints)
                {
                    DomainServiceEndpointFactory endpointFactory = CreateEndpointFactoryInstance(provider);
                    instance.Endpoints.Add(endpointFactory.Name, endpointFactory);
                }
                return instance;
            }

            public static DomainServiceConfiguration Instance => s_domainServiceConfiguration.Value;

            // TODO: not dictionary ..
            public IDictionary<string, DomainServiceEndpointFactory> Endpoints { get; } = new Dictionary<string, DomainServiceEndpointFactory>();

            /// <summary>
            /// Creates a <see cref="DomainServiceEndpointFactory"/> from a <see cref="ProviderSettings"/> object.
            /// </summary>
            /// <param name="provider">The <see cref="ProviderSettings"/> object.</param>
            /// <returns>A <see cref="DomainServiceEndpointFactory"/>.</returns>
            private static DomainServiceEndpointFactory CreateEndpointFactoryInstance(ProviderSettings provider)
            {
                Type endpointFactoryType = Type.GetType(provider.Type, /* throwOnError */ true);
                DomainServiceEndpointFactory endpointFactory = (DomainServiceEndpointFactory)Activator.CreateInstance(endpointFactoryType);
                endpointFactory.Name = provider.Name;
                endpointFactory.Parameters = provider.Parameters;
                return endpointFactory;
            }
        }
    }
}
