#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using OpenRiaServices.Server;

namespace OpenRiaServices.Hosting.AspNetCore
{
    public sealed class OpenRiaServicesOptionsBuilder
    {
        internal OpenRiaServicesOptionsBuilder(IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            Services = services;
        }

        /*** MOVE UP ***/
        internal OpenRiaServicesOptionsBuilder AddDomainService<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicConstructors)] T>() where T : DomainService
        {
            return AddDomainService<T>(ServiceLifetime.Transient);
        }

        internal OpenRiaServicesOptionsBuilder AddDomainService<T>(ServiceLifetime serviceLifetime) where T : DomainService
        {
            Services.AddDomainService<T>(serviceLifetime);
            return this;
        }

        internal void AddDomainServicesFromAssemblies(IEnumerable<Assembly> assemblies, ServiceLifetime serviceLifetime)
        {
            Services.AddDomainServicesFromAssemblies(assemblies, serviceLifetime);
        }

        /// <summary>
        /// Registers public <see cref="DomainService"/>s that are marked with <see cref="EnableClientAccessAttribute"/> from <paramref name="assembly"/> 
        /// </summary>
        internal void AddDomainServicesFromAssembly(Assembly assembly, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        {
            Services.AddDomainServicesFromAssembly(assembly, serviceLifetime);
        }

        internal IServiceCollection Services { get; }
        // Add Options ? 
    }
}
