﻿#nullable enable

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenRiaServices.Hosting.AspNetCore;
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

        public static IEndpointConventionBuilder MapOpenRiaServices(this IEndpointRouteBuilder endpoints, Action<OpenRiaServicesConfigurationBuilder> configure)
        {
            return endpoints.MapOpenRiaServices(string.Empty, configure);
        }

        public static IEndpointConventionBuilder MapOpenRiaServices(this IEndpointRouteBuilder endpoints, string prefix, Action<OpenRiaServicesConfigurationBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(endpoints);
            ArgumentNullException.ThrowIfNull(configure);

            // todo: Consider user friendyl exception instead
            var dataSource = endpoints.ServiceProvider.GetRequiredService<OpenRiaServicesEndpointDataSource>();
            dataSource.Prefix = prefix;

            // Scope used to validate that domainservices can be resolved
            using (var scope = endpoints.ServiceProvider.CreateScope())
            {
                // Allow OpenRiaServicesConfigurationBuilder to validate that types are registeredd corretly
                IServiceProviderIsService isService = scope.ServiceProvider.GetService<IServiceProviderIsService>() 
                    ?? new DymmyIsService(scope);

                var logger = scope.ServiceProvider.GetService<ILogger<OpenRiaServicesConfigurationBuilder>>();
                var configurationBuilder = new OpenRiaServicesConfigurationBuilder(dataSource, isService, logger, scope);
                configure(configurationBuilder);
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
