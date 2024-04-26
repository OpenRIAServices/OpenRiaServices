// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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

        public IEndpointConventionBuilder AddDomainService(Type type)
        {
            ArgumentNullException.ThrowIfNull(nameof(type));

            if (!_typeIsService.IsService(type))
                throw new InvalidOperationException($"Domainservice {type} cannot be resolved by container, register it before calling map");

            _dataSource.TryAddDomainService(type.Name, type);
            var longName = type.FullName.Replace('.', '-') + ".svc";
            return _dataSource.AddDomainService(longName + "/binary", type);
        }

        public IEndpointConventionBuilder AddDomainService<T>() where T : DomainService
        {
            return AddDomainService(typeof(T));
        }

        public IEndpointConventionBuilder AddDomainService(Type type, string path)
        {
            ArgumentNullException.ThrowIfNull(nameof(type));
#if NET7_0_OR_GREATER
            ArgumentNullException.ThrowIfNullOrEmpty(nameof(path));
#endif

            if (!_typeIsService.IsService(type))
                throw new InvalidOperationException($"Domainservice {type} cannot be resolved by container, register it before calling map");

            return _dataSource.AddDomainService(path, type);
        }

        public IEndpointConventionBuilder AddDomainService<T>(string path) where T : DomainService
        {
            return AddDomainService(typeof(T), path);
        }
    }
}
