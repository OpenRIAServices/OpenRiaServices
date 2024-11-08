#nullable enable

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenRiaServices.Server;

namespace OpenRiaServices.Hosting.AspNetCore
{
    public static class EndpointRouteBuilderExtensions
    {
        [Obsolete("Moved to OpenRiaServicesServiceCollectionExtensions")]
        public static void AddOpenRiaServices(IServiceCollection services)
        {
            services.AddOpenRiaServices();
        }

        [Obsolete("Use AddOpenRiaServices() instead and add domainServices using AddDomainServices.. ")]
        public static void AddOpenRiaServices<T>(this IServiceCollection services) where T : DomainService
        {
            services.AddOpenRiaServices();
            services.AddTransient<T>();
        }

        public static IEndpointConventionBuilder MapOpenRiaServices(this IEndpointRouteBuilder endpoints)
            => MapOpenRiaServices(endpoints, string.Empty);

        public static IEndpointConventionBuilder MapOpenRiaServices(this IEndpointRouteBuilder endpoints, Action<OpenRiaServicesConfigurationBuilder> configure)
            => MapOpenRiaServices(endpoints, string.Empty, configure);

        /// <summary>
        /// Map all registered <see cref="DomainService"/>, under the route prefix <paramref name="prefix"/> resulting in endpoint names on "prefix"+"DomainServiceNamePattern/MethodName" format
        /// 
        /// <para>this is a shorthand for <see cref="MapOpenRiaServices(IEndpointRouteBuilder, string, Action{OpenRiaServicesConfigurationBuilder})"/> with configure 
        /// set to <c>builder => { builder.AddRegisteredDomainServices(); }</c>
        /// </para>
        /// </summary>
        /// <see cref="OpenRiaServicesConfigurationBuilder.AddRegisteredDomainServices(bool)"/>
        /// <see cref="OpenRiaServices.Hosting.AspNetCore.DomainServiceEndpointRoutePatternAttribute"/>
        /// <seealso cref="MapOpenRiaServices(IEndpointRouteBuilder, string, Action{OpenRiaServicesConfigurationBuilder})"/>
        /// <param name="prefix">Route prefix to place all services under</param>
        /// <returns></returns>
        public static IEndpointConventionBuilder MapOpenRiaServices(this IEndpointRouteBuilder endpoints, string prefix)
            => MapOpenRiaServices(endpoints, prefix, builder => { builder.AddRegisteredDomainServices(); });

        /// <summary>
        /// Maps OpenRiaServices with the given route <paramref name="prefix"/> prefix.
        /// <para>Use the <paramref name="configure"/> action to Map individual DomainServices.</para>
        /// </summary>
        /// <param name="endpoints">The endpoint route builder.</param>
        /// <param name="prefix">The prefix for the OpenRiaServices routes.</param>
        /// <param name="configure">The configuration action for OpenRiaServices.</param>
        /// <returns>An endpoint convention builder that can be used to configure all routes created.</returns>
        public static IEndpointConventionBuilder MapOpenRiaServices(this IEndpointRouteBuilder endpoints, string prefix, Action<OpenRiaServicesConfigurationBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(endpoints);
            ArgumentNullException.ThrowIfNull(configure);

            var dataSource = endpoints.ServiceProvider.GetRequiredService<OpenRiaServicesEndpointDataSource>();
            if (dataSource.Prefix is not null)
                throw new InvalidOperationException("MapOpenRiaServices can only be called once");
            dataSource.Prefix = prefix;

            using (var scope = endpoints.ServiceProvider.CreateScope())
            {
                IServiceProviderIsService isService = scope.ServiceProvider.GetService<IServiceProviderIsService>()
                    ?? new DymmyIsService(scope);

                var logger = scope.ServiceProvider.GetService<ILogger<OpenRiaServicesConfigurationBuilder>>();
                var configurationBuilder = new OpenRiaServicesConfigurationBuilder(dataSource, isService, logger, scope);
                configure(configurationBuilder);

                if (!dataSource.HasMappedAnyDomainService())
                    throw new InvalidOperationException("No domain services were registered, use AddDomainService or AddDomainServices to register domain services");
            }

            endpoints.DataSources.Add(dataSource);

            return dataSource;
        }

        /// <summary>
        /// Simple implementation of IServiceProviderIsService in case a non conformant DI provider is used
        /// </summary>
        sealed class DymmyIsService : IServiceProviderIsService
        {
            private readonly IServiceScope _scope;

            public DymmyIsService(IServiceScope scope)
            {
                _scope = scope;
            }

            public bool IsService(Type serviceType)
            {
                // No need to dispose, the scope should dispose the instance
                return _scope.ServiceProvider.GetService(serviceType) is DomainService;
            }
        }
    }
}
