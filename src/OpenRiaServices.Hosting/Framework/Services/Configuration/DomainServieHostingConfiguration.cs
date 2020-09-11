﻿using System;
using System.Collections.Generic;
using System.Configuration;

namespace OpenRiaServices.Hosting.Configuration.Internal
{
    /// <summary>
    /// internal use, will probably change between minor releases
    /// </summary>
    public class DomainServieHostingConfiguration
    {
        private static readonly Lazy<DomainServieHostingConfiguration> s_domainServiceConfiguration = new Lazy<DomainServieHostingConfiguration>(CreateConfiguration, System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

        private DomainServieHostingConfiguration()
        {
            EndpointFactories = new HashSet<DomainServiceEndpointFactory>(new EndpointNameComparer());
        }

        /// <summary>
        /// Get the current global configuration
        /// </summary>
        public static DomainServieHostingConfiguration Current => s_domainServiceConfiguration.Value;

        /// <summary>
        /// List of all endpoints, name must be unique
        /// </summary>
        public ISet<DomainServiceEndpointFactory> EndpointFactories { get; }

        /// <summary>
        /// Initialize the singleton instance
        /// </summary>
        /// <returns></returns>
        static DomainServieHostingConfiguration CreateConfiguration()
        {
            var instance = new DomainServieHostingConfiguration();
            foreach (ProviderSettings provider in DomainServicesSection.Current.Endpoints)
            {
                DomainServiceEndpointFactory endpointFactory = CreateEndpointFactoryInstance(provider);
                instance.EndpointFactories.Add(endpointFactory);
            }
            return instance;
        }

        /// <summary>
        /// Creates a <see cref="DomainServiceEndpointFactory"/> from a <see cref="ProviderSettings"/> object.
        /// </summary>
        /// <param name="provider">The <see cref="ProviderSettings"/> object.</param>
        /// <returns>A <see cref="DomainServiceEndpointFactory"/>.</returns>
        private static DomainServiceEndpointFactory CreateEndpointFactoryInstance(ProviderSettings provider)
        {
            var endpointFactoryType = Type.GetType(provider.Type, /* throwOnError */ true);
            var endpointFactory = (DomainServiceEndpointFactory)Activator.CreateInstance(endpointFactoryType);
            endpointFactory.Name = provider.Name;
            endpointFactory.Parameters = provider.Parameters;
            return endpointFactory;
        }

        private class EndpointNameComparer : IEqualityComparer<DomainServiceEndpointFactory>
        {
            public bool Equals(DomainServiceEndpointFactory x, DomainServiceEndpointFactory y)
            {
                return string.Equals(x.Name, y.Name);
            }

            public int GetHashCode(DomainServiceEndpointFactory obj)
            {
                return obj.Name.GetHashCode();
            }
        }
    }
}

