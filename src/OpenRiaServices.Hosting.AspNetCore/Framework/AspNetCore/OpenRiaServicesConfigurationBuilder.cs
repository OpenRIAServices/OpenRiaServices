// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
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
        private readonly ILogger? _logger;
        private readonly IServiceScope _scope;

        internal OpenRiaServicesConfigurationBuilder(OpenRiaServicesEndpointDataSource dataSource, IServiceProviderIsService typeIsService, ILogger? logger, IServiceScope scope)
        {
            _dataSource = dataSource;
            _typeIsService = typeIsService;
            _logger = logger;
            _scope = scope;
        }

        /// <summary>
        /// Map (add routes) for the specified <see cref="DomainService"/> <typeparamref name="T"/> so it can be accessed by clients.
        /// </summary>
        public IEndpointConventionBuilder AddDomainService<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>() where T : DomainService
        {
            return AddDomainService(typeof(T));
        }

        /// <summary>
        /// Map (add routes) for the specified <see cref="DomainService"/> <paramref name="type"/> so it can be accessed by clients.
        /// </summary>
        public IEndpointConventionBuilder AddDomainService([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
        {
            ArgumentNullException.ThrowIfNull(nameof(type));

            if (!_typeIsService.IsService(type))
                throw new InvalidOperationException($"Domainservice {type} cannot be resolved by container, register it before calling map");

            return _dataSource.AddDomainService(GetDomainServiceRoute(type), type);
        }

        /// <summary>
        /// Map (add routes) for the specified <see cref="DomainService"/> <paramref name="type"/> under the specified path so it can be accessed by clients.
        /// </summary>
        public IEndpointConventionBuilder AddDomainService([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type, string path)
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

        /// <summary>
        /// Map (add routes) for the specified <see cref="DomainService"/> type under the specified path so it can be accessed by clients.
        /// </summary>
        public IEndpointConventionBuilder AddDomainService<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(string path) where T : DomainService
        {
            return AddDomainService(typeof(T), path);
        }

        /// <summary>
        /// Map (add routes) for all <see cref="DomainService"/> implementations that has been registered in the <see cref="IServiceCollection"/>
        /// that was passed to <see cref="OpenRiaServicesServiceCollectionExtensions.AddOpenRiaServices(IServiceCollection)"/>.
        /// </summary>
        /// <param name="suppressAndLogErrors"><c>true</c> means that exceptions are supressed and logged, default <c>false</c></param>
        /// <returns></returns>
        public IEndpointConventionBuilder AddRegisteredDomainServices(bool suppressAndLogErrors = false)
        {
            CompositeEndpointConventionBuilder compositeEndpointConventionBuilder = new();

            foreach (ServiceDescriptor service in _dataSource.ServiceCollection)
            {
                Type serviceType = service.ServiceType;
                if (typeof(DomainService).IsAssignableFrom(serviceType))
                {
                    if (service.ServiceType != service.ImplementationType)
                    {
                        // Fallback to trying to resolve the actual implementation type using _scope
                        serviceType = _scope.ServiceProvider.GetRequiredService(serviceType).GetType();
                    }

                    if (!TypeUtility.IsAttributeDefined(serviceType, typeof(EnableClientAccessAttribute), true))
                    {
                        _logger?.NoEnableClientAccessAttributeSkipping(serviceType);
                        continue;
                    }

                    try
                    {
                        compositeEndpointConventionBuilder.AddBuilder(AddDomainService(serviceType));
                    }
                    catch (Exception ex) when (suppressAndLogErrors && ex is not MissingMethodException)
                    {
                        _logger?.NotMappingDomainServiceDueToException(serviceType, ex);
                    }
                }
            }

            return compositeEndpointConventionBuilder;
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
                ?? Assembly.GetEntryAssembly()?.GetCustomAttribute<DomainServiceEndpointRoutePatternAttribute>()?.EndpointRoutePattern
                ?? EndpointRoutePattern.WCF;

            return pattern switch
            {
                EndpointRoutePattern.Name => type.Name,
                EndpointRoutePattern.WCF => type.FullName!.Replace('.', '-') + ".svc/binary",
                EndpointRoutePattern.FullName => type.FullName?.Replace('.', '-') ?? throw new InvalidOperationException("Type.FullName must not be null"),
                _ => throw new NotImplementedException(),
            };
        }

        sealed class CompositeEndpointConventionBuilder : IEndpointConventionBuilder
        {
            private readonly List<IEndpointConventionBuilder> _builders = new();

            public CompositeEndpointConventionBuilder()
            {
            }

            public void AddBuilder(IEndpointConventionBuilder builder)
            {
                _builders.Add(builder);
            }

            void IEndpointConventionBuilder.Add(Action<EndpointBuilder> convention)
            {
                foreach (var builder in _builders)
                    builder.Add(convention);
            }

#if NET7_0_OR_GREATER
            /// <summary>
            /// Registers the specified convention for execution after conventions registered
            /// via <see cref="Add(Action{EndpointBuilder})"/>
            /// </summary>
            /// <param name="finallyConvention">The convention to add to the builder.</param>
            void IEndpointConventionBuilder.Finally(Action<EndpointBuilder> finallyConvention)
            {
                foreach (var builder in _builders)
                    builder.Finally(finallyConvention);
            }
#endif
        }
    }

}
