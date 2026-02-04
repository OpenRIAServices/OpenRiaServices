#nullable enable

using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace OpenRiaServices.Hosting.AspNetCore
{
    /// <summary>
    /// A possible future extension point for configuring OpenRia Services
    /// Similar to <see cref="IMvcBuilder"/>.
    /// </summary>
    public sealed class OpenRiaServicesOptionsBuilder
    {
        internal OpenRiaServicesOptionsBuilder(IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            Services = services;
        }

        internal IServiceCollection Services { get; }
        // Add Options ? 

        /// <summary>
        /// Enables Text based XML wire format (application/xml) in addition to built in binary XML (application/msbin1).
        /// It does not change the default format.
        /// </summary>
        /// <remarks>Request should specify mime-type <c>application/xml</c> using <c>Content-Type</c> or <c>Accept</c> headers
        /// <summary>
        /// Enables text-based XML wire-format serialization (application/xml) alongside the existing binary XML format.
        /// </summary>
        /// <remarks>
        /// Appends a TextXmlSerializationProvider to the configured OpenRiaServicesOptions.SerializationProviders and, when a BinaryXmlSerializationProvider exists, reuses its per-domain service data contract cache. The default provider (position 0) is not changed.
        /// </remarks>
        /// <returns>The same <see cref="OpenRiaServicesOptionsBuilder"/> instance for fluent chaining.</returns>
        public OpenRiaServicesOptionsBuilder WithTextXmlSerialization()
        {
            Services.Configure<OpenRiaServicesOptions>(options =>
            {
                Serialization.TextXmlSerializationProvider provider = new Serialization.TextXmlSerializationProvider();

                // use same cache for data contracts
                if (options.SerializationProviders.OfType<Serialization.BinaryXmlSerializationProvider>().FirstOrDefault() is { } binaryProvider)
                    provider._perDomainServiceDataContractCache = binaryProvider._perDomainServiceDataContractCache;

                // Add new provider last, position 0 is default
                options.SerializationProviders = [.. options.SerializationProviders, provider];
            });

            return this;
        }
    }
}