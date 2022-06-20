using System;
using System.Reflection;
using OpenRiaServices.Hosting.Wcf;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for Microsoft.Extensions.DependencyInjection interop
    /// </summary>
    public static class OpenRiaServicesServiceCollectionExtensions
    {
        /// <summary>
        /// Registers DomainServices found in the specified assemblies to the <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="serviceCollection"><see cref="IServiceCollection"/> where services are registered</param>
        /// <param name="serviceLifetime">Lifetime of services: <see cref="ServiceLifetime.Transient"/> or <see cref="ServiceLifetime.Scoped"/></param>
        /// <param name="assemblies">assemblies to scan, or <c>null</c> to scan all referenced assemblies</param>
        /// <returns></returns>
        public static IServiceCollection AddDomainServices(this IServiceCollection serviceCollection, ServiceLifetime serviceLifetime, Assembly[] assemblies = null)
        {
            if (serviceLifetime == ServiceLifetime.Singleton)
                throw new ArgumentOutOfRangeException(nameof(serviceLifetime), Resource.OpenRiaServicesServiceCollectionExtensions_SingletonNotAllowed);

            foreach (var type in DomainServiceAssemblyScanner.DiscoverDomainServices(assemblies))
            {
                serviceCollection.Add(new ServiceDescriptor(type, type, serviceLifetime));
            }
                

            return serviceCollection;
        }
    }
}
