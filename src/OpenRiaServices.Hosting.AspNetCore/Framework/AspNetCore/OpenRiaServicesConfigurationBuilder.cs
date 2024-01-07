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
            var longName = type.FullName.Replace('.', '-') + ".svc";

            if (!_typeIsService.IsService(type))
                throw new InvalidOperationException($"Domainservice {type} cannot be resolved by container, register it before calling map");

            return _dataSource.AddDomainService(longName + "/binary", type);
        }

        public IEndpointConventionBuilder AddDomainService<T>() where T : DomainService
        {
            return AddDomainService(typeof(T));
        }

    }
}
