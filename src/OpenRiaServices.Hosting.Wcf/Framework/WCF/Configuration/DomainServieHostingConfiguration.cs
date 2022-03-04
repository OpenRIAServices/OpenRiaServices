﻿using System;
using System.Collections.Generic;
using System.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenRiaServices.Server;

namespace OpenRiaServices.Hosting.Wcf.Configuration.Internal
{
    /// <summary>
    /// internal use, will probably change between minor releases
    /// </summary>
    public class DomainServiceHostingConfiguration
    {
        private static readonly Lazy<DomainServiceHostingConfiguration> s_domainServiceConfiguration = new Lazy<DomainServiceHostingConfiguration>(CreateConfiguration, System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

        private DomainServiceHostingConfiguration()
        {
            EndpointFactories = new HashSet<DomainServiceEndpointFactory>(new EndpointNameComparer());
        }

        /// <summary>
        /// Get the current global configuration
        /// </summary>
        public static DomainServiceHostingConfiguration Current => s_domainServiceConfiguration.Value;

        /// <summary>
        /// List of all endpoints, name must be unique
        /// </summary>
        public ISet<DomainServiceEndpointFactory> EndpointFactories { get; }
        public IServiceProvider ServiceProvider { get; set; } = new DefaultServiceProvider();

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

        private class DefaultServiceProvider : IServiceProvider, IServiceScopeFactory
        {
            IServiceScope IServiceScopeFactory.CreateScope() 
                => new ServiceScope(DomainService.IsDefaultFactory ? new ActivatorServiceProvider() : this);

            public object GetService(Type serviceType)
            {
                // Create service scope
                if (serviceType == typeof(IServiceScopeFactory))
                    return this;

                return null;
            }
        }

        private class ServiceScope : IServiceScope
        {
            IServiceProvider _serviceProvider;

            public ServiceScope(IServiceProvider serviceProvider)
            {
                _serviceProvider = serviceProvider;
            }

            public IServiceProvider ServiceProvider => _serviceProvider;

            public void Dispose() => (_serviceProvider as IDisposable)?.Dispose();
        }

        private class ActivatorServiceProvider : IServiceProvider, IDisposable
        {
            private List<IDisposable> _disposables = new List<IDisposable>();

            public void Dispose()
            {
                foreach (var instance in _disposables)
                    instance.Dispose();
                _disposables.Clear();
            }

            public object GetService(Type serviceType)
            {
                try
                {
                    if (typeof(DomainService).IsAssignableFrom(serviceType))
                    {
                        var instance = (DomainService)Activator.CreateInstance(serviceType);
                        _disposables.Add(instance);
                        return instance;
                    }
                    return null;
                }
                catch (System.Reflection.TargetInvocationException tie)
                {
                    if (tie.InnerException is Exception inner)
                    {
                        throw inner;
                    }
                    throw;
                }
            }
        }
    
    }
}

