// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OpenRiaServices.Server;

namespace OpenRiaServices.Hosting.AspNetCore
{
    public class OpenRiaServicesConfigurationBuilder
    {
        private readonly OpenRiaServicesEndpointDataSource _dataSource;
        private readonly IServiceProviderIsService _typeIsService;

        internal OpenRiaServicesConfigurationBuilder(OpenRiaServicesEndpointDataSource dataSource, IServiceProviderIsService typeIsService)
        {
            _dataSource = dataSource;
            _typeIsService = typeIsService;
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
                ?? EndpointRoutePattern.FullName;

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
