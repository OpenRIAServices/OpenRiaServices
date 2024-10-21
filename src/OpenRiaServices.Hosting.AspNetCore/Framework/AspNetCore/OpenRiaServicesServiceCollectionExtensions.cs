using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using OpenRiaServices;
using OpenRiaServices.Hosting.AspNetCore;
using OpenRiaServices.Server;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for registering OpenRiaServices services in an <see cref="IServiceCollection"/>
    /// </summary>
    public static class OpenRiaServicesServiceCollectionExtensions
    {
        /// <summary>
        /// Adds services required for using OpenRiaServices
        /// </summary>
        public static OpenRiaServicesOptionsBuilder AddOpenRiaServices(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.AddSingleton<OpenRiaServicesEndpointDataSource>();
            services.AddSingleton(services);

            return new OpenRiaServicesOptionsBuilder(services);
        }

        /// <summary>
        /// Adds services required for using OpenRiaServices and configures the options
        /// </summary>
        public static OpenRiaServicesOptionsBuilder AddOpenRiaServices(this IServiceCollection services, Action<OpenRiaServicesOptions> configure)
        {
            ArgumentNullException.ThrowIfNull(services);

            AddOpenRiaServices(services);
            services.Configure(configure);

            return new OpenRiaServicesOptionsBuilder(services);
        }

        /// <summary>
        /// Registers <typeparamref name="TService"/> as a transient service in <paramref name="services"/>
        /// </summary>
        public static IServiceCollection AddDomainService<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicConstructors)] TService>(this IServiceCollection services) where TService : DomainService
        {
            return AddDomainService<TService>(services, ServiceLifetime.Transient);
        }

        /// <summary>
        /// Registers <typeparamref name="TService"/> as a service with <paramref name="serviceLifetime"/> in <paramref name="services"/>
        /// </summary>
        public static IServiceCollection AddDomainService<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicConstructors)] TService>(this IServiceCollection services, ServiceLifetime serviceLifetime) where TService : DomainService
        {
            services.Add(new ServiceDescriptor(typeof(TService), typeof(TService), serviceLifetime));
            return services;
        }

        // TODO:
        // - From or In Assemblies
        // [RequiresUnreferencedCode("DomainService types are loaded dynamically and may be trimmed.")]
        /// <summary>
        /// Registers public <see cref="DomainService"/>s marked with <see cref="EnableClientAccessAttribute"/> in <paramref name="assemblies"/> for dependency injection
        /// </summary>
        public static IServiceCollection AddDomainServicesFromAssemblies(this IServiceCollection services, IEnumerable<Assembly> assemblies, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
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
        /// Registers public <see cref="DomainService"/>s that are marked with <see cref="EnableClientAccessAttribute"/> from <paramref name="assembly"/> for dependency injection
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
