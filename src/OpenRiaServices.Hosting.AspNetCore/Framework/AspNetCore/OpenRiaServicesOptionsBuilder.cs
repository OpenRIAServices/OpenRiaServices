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

        public OpenRiaServicesOptionsBuilder WithTextXmlSerialization()
        {
            Services.Configure<OpenRiaServicesOptions>(options =>
            {
                Serialization.TextXmlSerializationProvider provider = new Serialization.TextXmlSerializationProvider();
                if (options.SerializationProviders.OfType<Serialization.BinaryXmlSerializationProvider>().FirstOrDefault() is { } binaryProvider)
                    provider._perDomainServiceDataContractCache = binaryProvider._perDomainServiceDataContractCache;
                // use same cache for data contracts

                // Add new provider last, position 0 is default
                options.SerializationProviders = [.. options.SerializationProviders, provider];
            });

            return this;
        }
    }
}
