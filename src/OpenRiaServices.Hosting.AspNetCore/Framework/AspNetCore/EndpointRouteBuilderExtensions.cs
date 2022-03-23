using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

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

    public static IEndpointConventionBuilder MapOpenRiaServices(this IEndpointRouteBuilder endpoints, Action<OpenRiaServicesConfigurationBuilder> configure)
    {
        return MapOpenRiaServices(endpoints, string.Empty, configure);
    }

    public static IEndpointConventionBuilder MapOpenRiaServices(this IEndpointRouteBuilder endpoints, string prefix, Action<OpenRiaServicesConfigurationBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(configure);

        // todo: Consider user friendyl exception instead
        var dataSource = endpoints.ServiceProvider.GetRequiredService<OpenRiaServicesEndpointDataSource>();
        dataSource.Prefix = prefix;

        var configurationBuilder = new OpenRiaServicesConfigurationBuilder(dataSource);
        configure(configurationBuilder);

        // Validate that all domainservices are registered
        using (var scope = endpoints.ServiceProvider.CreateScope())
        {
            IServiceProviderIsService isService = scope.ServiceProvider.GetService<IServiceProviderIsService>();
            bool canResolve(Type type) => isService != null ? isService.IsService(type) : scope.ServiceProvider.GetService(type) is not null;

            foreach (var type in dataSource.DomainServices)
            {
                if (!canResolve(type.Value.DomainServiceType))
                    throw new InvalidOperationException($"Domainservice {type.Value.DomainServiceType} cannot be resolved by container, register it before calling map");
            }
        }

        endpoints.DataSources.Add(dataSource);

        return dataSource;
    }
}
