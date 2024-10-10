// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenRiaServices.Server;

namespace OpenRiaServices.Hosting.AspNetCore
{
    public class OpenRiaServicesConfigurationBuilder
    {
        private readonly OpenRiaServicesEndpointDataSource _dataSource;
        private readonly IServiceProviderIsService _typeIsService;
        private readonly ILogger<OpenRiaServicesConfigurationBuilder> _logger;
        private readonly IServiceScope _scope;

        internal OpenRiaServicesConfigurationBuilder(OpenRiaServicesEndpointDataSource dataSource, IServiceProviderIsService typeIsService, ILogger<OpenRiaServicesConfigurationBuilder> logger, IServiceScope scope)
        {
            _dataSource = dataSource;
            _typeIsService = typeIsService;
            _logger = logger;
            _scope = scope;
        }

        public IEndpointConventionBuilder AddDomainService<T>() where T : DomainService
        {
            return AddDomainService(typeof(T));
        }

        public IEndpointConventionBuilder AddDomainService(Type type)
        {
            ArgumentNullException.ThrowIfNull(nameof(type));

            if (!_typeIsService.IsService(type))
                throw new InvalidOperationException($"Domainservice {type} cannot be resolved by container, register it before calling map");

            return _dataSource.AddDomainService(GetDomainServiceRoute(type), type);
        }

        public IEndpointConventionBuilder AddDomainService(Type type, string path)
        {
            ArgumentNullException.ThrowIfNull(nameof(type));
#if NET7_0_OR_GREATER
            ArgumentNullException.ThrowIfNullOrEmpty(nameof(path));
#endif
            if (path.EndsWith('/'))
                throw new ArgumentException("Path should not end with /", nameof(path));

            if (!_typeIsService.IsService(type))
                throw new InvalidOperationException($"Domainservice {type} cannot be resolved by container, register it before calling map");

            return _dataSource.AddDomainService(path, type);
        }

        public IEndpointConventionBuilder AddDomainService<T>(string path) where T : DomainService
        {
            return AddDomainService(typeof(T), path);
        }

        public IEndpointConventionBuilder AddRegisteredDomainServices(bool suppressAndLogErrors = false)
        {
            foreach (ServiceDescriptor service in _dataSource.ServiceCollection)
            {
                Type serviceType = service.ServiceType;
                if (typeof(DomainService).IsAssignableFrom(serviceType))
                {
                    if (service.ServiceType != service.ImplementationType)
                    {
                        throw new InvalidOperationException($"ServiceDescriptor for '{serviceType}' has different ServiceType and ImplementationType '{service.ImplementationType}'");
                    }

                    if (!TypeUtility.IsAttributeDefined(serviceType, typeof(EnableClientAccessAttribute), true))
                    {
                        _logger.LogTrace("Skipping DomainService '{DomainServiceType}' since it is not marked with EnableClientAccessAttribute", serviceType);
                        continue;
                    }


                    try
                    {
                        AddDomainService(serviceType);
                    }
                    catch (Exception ex)
                    {
                        if (suppressAndLogErrors && ex is not MissingMethodException)
                        {
                            _logger.LogWarning(ex, "Skipped domain service '{DomainServiceType}' since it resulted in error: {Error}", serviceType, ex.Message);
                        }
                        else
                        {
                            _logger.LogError(ex, "Error adding DomainService '{DomainServiceType}'", serviceType);
                            throw;
                        }
                    }
                }
            }
            //composite convention builder ?
            // return this;
            return null;
        }

        private static string GetDomainServiceRoute(Type type)
        {
            // 1. TODO: Look at EnableClientAccessAttribute if we can set route there
            // 2. Lookup DomainServiceEndpointRoutePatternAttribute in same assembly as DomainService
            // 3. Lookup DomainServiceEndpointRoutePatternAttribute in startup assembly
            // 4. Fallback to default (FullName)
            // - Fallback to FullName
            EndpointRoutePattern pattern =
                type.Assembly.GetCustomAttribute<DomainServiceEndpointRoutePatternAttribute>()?.EndpointRoutePattern
                ?? Assembly.GetEntryAssembly().GetCustomAttribute<DomainServiceEndpointRoutePatternAttribute>()?.EndpointRoutePattern
                ?? EndpointRoutePattern.WCF;

            return pattern switch
            {
                EndpointRoutePattern.Name => type.Name,
                EndpointRoutePattern.WCF => type.FullName.Replace('.', '-') + ".svc/binary",
                EndpointRoutePattern.FullName => type.FullName.Replace('.', '-'),
                _ => throw new NotImplementedException(),
            };
        }
    }
}
