using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using OpenRiaServices.Server;

namespace OpenRiaServices.Hosting.AspNetCore
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

            services.AddOpenRiaServices();
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

        /// <summary>
        /// Registers public <see cref="DomainService"/>s marked with <see cref="EnableClientAccessAttribute"/> in <paramref name="assemblies"/> as Transient services
        /// </summary>
        public static IServiceCollection AddDomainServices(this IServiceCollection services, params Assembly[] assemblies)
            => AddDomainServices(services, ServiceLifetime.Transient, assemblies);

        // [RequiresUnreferencedCode("DomainService types are loaded dynamically and may be trimmed.")]
        /// <summary>
        /// Registers public <see cref="DomainService"/>s marked with <see cref="EnableClientAccessAttribute"/> in <paramref name="assemblies"/> for dependency injection
        /// </summary>
        public static IServiceCollection AddDomainServices(this IServiceCollection services, ServiceLifetime serviceLifetime, params Assembly[] assemblies)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(assemblies);

            if (serviceLifetime == ServiceLifetime.Singleton)
                throw new ArgumentOutOfRangeException(nameof(serviceLifetime), "Singleton is not a valid serviceLifetime");

            foreach (var assembly in assemblies)
            {
                if (assembly is not null && TypeUtility.CanContainDomainServiceImplementations(assembly))
                    AddDomainServicesFromAssembly(services, assembly, serviceLifetime);
            }

            return services;
        }

        /// <summary>
        /// Registers public <see cref="DomainService"/>s that are marked with <see cref="EnableClientAccessAttribute"/> from <paramref name="assembly"/> for dependency injection
        /// </summary>
        private static void AddDomainServicesFromAssembly(this IServiceCollection services, Assembly assembly, ServiceLifetime serviceLifetime)
        {
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
        }

    }
}
