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
    /// Experimental SUBJECT TO CHANGE: 
    /// </summary>
    public static class OpenRiaServiceCollectionExtensions
    {
        /// <summary>
        /// PREVIEW and Subject to change: Registers DomainServices in the specified service collection with provided
        /// </summary>
        /// <param name="serviceCollection"></param>
        /// <param name="serviceLifetime"></param>
        /// <param name="assemblies">assemblies to scan, or null to scan all referenced assemblies</param>
        /// <returns></returns>
        public static IServiceCollection AddDomainServices(this IServiceCollection serviceCollection, ServiceLifetime serviceLifetime, Assembly[] assemblies = null)
        {
            foreach (var type in DomainServiceAssemblyScanner.DiscoverDomainServices(assemblies))
                serviceCollection.Add(new ServiceDescriptor(type, type, serviceLifetime));

            return serviceCollection;
        }
    }
}
