using System;
using Microsoft.Extensions.DependencyInjection;
using OpenRiaServices.Server;

namespace OpenRiaServices.Hosting.Wcf.Configuration
{
    /// <summary>
    /// Default implementation for <see cref="Configuration.Internal.DomainServiceHostingConfiguration.ServiceProvider"/>
    /// which creates DomainServices using <see cref="Activator.CreateInstance(Type)"/>
    /// </summary>
    sealed class DefaultDomainServicesServiceProvider : IServiceProvider, IServiceScopeFactory
    {
        /// <summary>
        /// Create a new scope, if a non-default DomainService factory is set then use a Dummy 
        /// resulting so that <see cref="DomainService.Factory"/> is used instead.
        /// </summary>
        IServiceScope IServiceScopeFactory.CreateScope()
            => DomainService.IsDefaultFactory ? new ActivatorServiceProvider() : new DummyScope();

        public object GetService(Type serviceType)
        {
            // Create service scope
            if (serviceType == typeof(IServiceScopeFactory))
                return this;

            return null;
        }

        // A scope which will never resolve anything
        sealed class DummyScope : IServiceScope, IServiceProvider
        {
            IServiceProvider IServiceScope.ServiceProvider => this;

            void IDisposable.Dispose() { }

            object IServiceProvider.GetService(Type serviceType) => null;
        }

        /// <summary>
        /// Service provider, capable of resolving a single DomainService (once)
        /// </summary>
        sealed class ActivatorServiceProvider : IServiceScope, IServiceProvider, IDisposable
        {
            private DomainService _domainService;

            IServiceProvider IServiceScope.ServiceProvider => this;

            void IDisposable.Dispose() => _domainService?.Dispose();

            object IServiceProvider.GetService(Type serviceType)
            {
                try
                {
                    if (_domainService == null && typeof(DomainService).IsAssignableFrom(serviceType))
                    {
                        _domainService = (DomainService)Activator.CreateInstance(serviceType);
                        return _domainService;
                    }
                    return null;
                }
                catch (System.Reflection.TargetInvocationException tie) when (tie.InnerException != null)
                {
                    throw tie.InnerException;
                }
            }
        }
    }
}

