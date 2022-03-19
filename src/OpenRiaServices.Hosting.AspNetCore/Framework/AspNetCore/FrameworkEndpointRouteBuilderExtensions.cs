
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

public static class FrameworkEndpointRouteBuilderExtensions
{
    // TODO: Other type ?? 
    public static void AddOpenRiaServices(this IServiceCollection services)
    {
        services.AddSingleton<FrameworkEndpointDataSource>();
    }

    public static IEndpointConventionBuilder MapOpenRiaServices(this IEndpointRouteBuilder endpoints, Action<FrameworkConfigurationBuilder> configure)
    {
        if (endpoints == null)
        {
            throw new ArgumentNullException(nameof(endpoints));
        }
        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var dataSource = endpoints.ServiceProvider.GetRequiredService<FrameworkEndpointDataSource>();

        var configurationBuilder = new FrameworkConfigurationBuilder(dataSource);
        configure(configurationBuilder);

        endpoints.DataSources.Add(dataSource);

        return dataSource;
    }
}
