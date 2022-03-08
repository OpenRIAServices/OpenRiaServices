using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using OpenRiaServices.Hosting.Wcf;
using OpenRiaServices.Server;

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
        /// <param name="serviceLifetime">Lifetime of services</param>
        /// <param name="assemblies">assemblies to scan, or <c>null</c> to scan all referenced assemblies</param>
        /// <returns></returns>
        public static IServiceCollection AddDomainServices(this IServiceCollection serviceCollection, ServiceLifetime serviceLifetime, Assembly[] assemblies = null)
        {
            foreach (var type in DomainServiceAssemblyScanner.DiscoverDomainServices(assemblies))
                serviceCollection.Add(new ServiceDescriptor(type, type, serviceLifetime));

            return serviceCollection;
        }
    }
}
