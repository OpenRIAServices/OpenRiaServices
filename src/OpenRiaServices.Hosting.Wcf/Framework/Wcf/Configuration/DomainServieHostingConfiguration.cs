using System;
using System.Collections.Generic;
using System.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace OpenRiaServices.Hosting.Wcf.Configuration.Internal
{
    /// <summary>
    /// internal use, will probably change between minor releases
    /// </summary>
    public class DomainServiceHostingConfiguration
    {
        private static readonly Lazy<DomainServiceHostingConfiguration> s_domainServiceConfiguration = new Lazy<DomainServiceHostingConfiguration>(CreateConfiguration, System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);
        private static IServiceScopeFactory s_serviceScopeFactory = new DefaultDomainServicesServiceProvider();
        
        private IServiceProvider _serviceProvider;

        private DomainServiceHostingConfiguration()
        {
            EndpointFactories = new HashSet<DomainServiceEndpointFactory>(new EndpointNameComparer());

            // This might seem strange, s_serviceScopeProvider cannot be changed until after ctor is run
            // and since ctor is private only a single instance will ever be created
            _serviceProvider = (DefaultDomainServicesServiceProvider)s_serviceScopeFactory;
        }

        /// <summary>
        /// Allow internal access to creating scope
        /// </summary>
        internal static IServiceScopeFactory ServiceScopeFactory => s_serviceScopeFactory;

        /// <summary>
        /// Get the current global configuration
        /// </summary>
        public static DomainServiceHostingConfiguration Current => s_domainServiceConfiguration.Value;

        /// <summary>
        /// List of all endpoints, name must be unique
        /// </summary>
        public ISet<DomainServiceEndpointFactory> EndpointFactories { get; }

        /// <summary>
        /// Global service provider to use for creating domain services and method injection.
        /// <para>Must be set at startup before first request</para>
        /// </summary>
        /// <exception cref="ArgumentException">If provider does not support Scopes</exception>
        public IServiceProvider ServiceProvider
        {
            get => _serviceProvider;
            set
            {
                var scopeFactory = value?.GetService<IServiceScopeFactory>();
                if (scopeFactory == null)
                    throw new ArgumentException(Resource.DomainServiceHostingConfiguration_ServiceProvider_MustSupportScope, nameof(value));

                _serviceProvider = value;
                s_serviceScopeFactory = scopeFactory;
            }
        }

        /// <summary>
        /// Initialize the singleton instance
        /// </summary>
        /// <returns></returns>
        static DomainServiceHostingConfiguration CreateConfiguration()
        {
            var instance = new DomainServiceHostingConfiguration();
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

        private sealed class EndpointNameComparer : IEqualityComparer<DomainServiceEndpointFactory>
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

