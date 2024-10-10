using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using OpenRiaServices;
using OpenRiaServices.Hosting.AspNetCore;
using OpenRiaServices.Server;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class OpenRiaServicesServiceCollectionExtensions
    {
        public static OpenRiaServicesOptionsBuilder AddOpenRiaServices(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.AddSingleton<OpenRiaServicesEndpointDataSource>();
            services.AddSingleton(services);

            return new OpenRiaServicesOptionsBuilder(services);
        }

        public static OpenRiaServicesOptionsBuilder AddOpenRiaServices(this IServiceCollection services, Action<OpenRiaServicesOptions> configure)
        {
            ArgumentNullException.ThrowIfNull(services);

            AddOpenRiaServices(services);
            services.Configure(configure);

            return new OpenRiaServicesOptionsBuilder(services);
        }

        public static IServiceCollection AddDomainService<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this IServiceCollection services) where T : DomainService
        {
            return AddDomainService<T>(services, ServiceLifetime.Transient);
        }

        public static IServiceCollection AddDomainService<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this IServiceCollection services, ServiceLifetime serviceLifetime) where T : DomainService
        {
            services.Add(new ServiceDescriptor(typeof(T), typeof(T), serviceLifetime));
            return services;
        }

        // TODO:
        // - From or In Assemblies
        // [RequiresUnreferencedCode("DomainService types are loaded dynamically and may be trimmed.")]
        public static IServiceCollection AddDomainServicesFromAssemblies(this IServiceCollection services, IEnumerable<Assembly> assemblies, ServiceLifetime serviceLifetime)
        {
            ArgumentNullException.ThrowIfNull(assemblies);

            foreach (var assembly in assemblies)
            {
                if (TypeUtility.CanContainDomainServiceImplementations(assembly))
                    AddDomainServicesFromAssembly(services, assembly, serviceLifetime);
            }

            return services;
        }

        /// <summary>
        /// Registers public <see cref="DomainService"/>s that are marked with <see cref="EnableClientAccessAttribute"/> from <paramref name="assembly"/> 
        /// </summary>
        public static IServiceCollection AddDomainServicesFromAssembly(this IServiceCollection services, Assembly assembly, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(assembly);

            //if (serviceLifetime == ServiceLifetime.Singleton)
            //                throw new ArgumentOutOfRangeException(nameof(serviceLifetime), Resource.OpenRiaServicesServiceCollectionExtensions_SingletonNotAllowed);

            foreach (var exportedType in assembly.GetExportedTypes())
            {
                if (exportedType.IsAbstract || exportedType.IsInterface || !typeof(DomainService).IsAssignableFrom(exportedType))
                {
                    continue;
                }

                if (!TypeUtility.IsAttributeDefined(exportedType, typeof(EnableClientAccessAttribute), true))
                {
                    continue;
                }

                services.Add(new ServiceDescriptor(exportedType, exportedType, serviceLifetime));
            }

            return services;
        }

    }
}
