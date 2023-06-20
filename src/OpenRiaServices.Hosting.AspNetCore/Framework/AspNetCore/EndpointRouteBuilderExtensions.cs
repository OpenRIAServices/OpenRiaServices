using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OpenRiaServices.Server;

namespace OpenRiaServices.Hosting.AspNetCore
{
    public static class EndpointRouteBuilderExtensions
    {
        // TODO: Other return types and parameters?? 
        // Action<HostingOptions>
        // return builder ...
        // move this to separate collection extensions class
        public static void AddOpenRiaServices(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.AddSingleton<OpenRiaServicesEndpointDataSource>();
        }

        public static void AddOpenRiaServices<T>(this IServiceCollection services) where T : OpenRiaServices.Server.DomainService
        {
            ArgumentNullException.ThrowIfNull(services);

            services.AddSingleton<OpenRiaServicesEndpointDataSource>();

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
                    ?? new DymmyIsService(scope.ServiceProvider);

                var configurationBuilder = new OpenRiaServicesConfigurationBuilder(dataSource, isService);
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
            private readonly IServiceProvider _serviceProvider;

            public DymmyIsService(IServiceProvider serviceProvider)
            {
                _serviceProvider = serviceProvider;
            }

            public bool IsService(Type serviceType)
            {
                using var domainService = (DomainService)_serviceProvider.GetService(serviceType);
                return domainService != null;
            }
        }
    }
}
