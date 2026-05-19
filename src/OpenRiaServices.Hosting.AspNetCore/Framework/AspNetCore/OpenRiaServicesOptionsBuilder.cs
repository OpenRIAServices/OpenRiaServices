using System;
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

        /// <summary>
        /// Allows access for extension methods to register application services
        /// </summary>
        public IServiceCollection Services { get; }

        // We could add "Options" property (implement IOptions<OpenRiaServicesOptions> and register "this" as singleton)
        // but direct access to the property could very well be done, before configuration paving the way for subtle errors

        /// <summary>
        /// Enables text based XML wire format (application/xml) in addition to built in binary Xml (application/msbin1).
        /// </summary>
        /// <remarks>Request should specify mime-type <c>application/xml</c> using <c>Content-Type</c> or <c>Accept</c> HTTP-headers
        /// </remarks>
        /// <param name="defaultProvider">If <see langword="true"/> the Xml provider will be the default for responses (when content type is not specified)</param>
        public OpenRiaServicesOptionsBuilder AddXmlSerialization(bool defaultProvider = false)
        {
            return AddXmlSerialization(configure: null, defaultProvider);
        }

        /// <summary>
        /// Enables text based XML wire format (application/xml) in addition to built in binary Xml (application/msbin1),
        /// with options configurable via a callback.
        /// </summary>
        /// <remarks>Request should specify mime-type <c>application/xml</c> using <c>Content-Type</c> or <c>Accept</c> HTTP-headers.
        /// </remarks>
        /// <param name="configure">An optional callback to configure <see cref="Serialization.XmlDataContractSerializerOptions"/>.</param>
        /// <param name="defaultProvider">If <see langword="true"/> the Xml provider will be the default for responses (when content type is not specified)</param>
        public OpenRiaServicesOptionsBuilder AddXmlSerialization(Action<Serialization.XmlDataContractSerializerOptions>? configure, bool defaultProvider = false)
        {
            var options = new Serialization.XmlDataContractSerializerOptions();
            configure?.Invoke(options);
            return AddSerializationProvider(new Serialization.TextXmlSerializationProvider(options), defaultProvider);
        }

        /// <summary>
        /// Configures the default binary XML (<c>application/msbin1</c>) serialization provider.
        /// </summary>
        /// <remarks>
        /// Use this to restrict reader quotas and mitigate denial-of-service risks for binary requests.
        /// </remarks>
        /// <param name="configure">A callback to configure <see cref="Serialization.BinaryDataContractSerializerOptions"/>.</param>
        public OpenRiaServicesOptionsBuilder ConfigureBinarySerialization(Action<Serialization.BinaryDataContractSerializerOptions> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);

            var options = new Serialization.BinaryDataContractSerializerOptions();
            configure(options);

            Services.Configure<OpenRiaServicesOptions>(openRiaOptions =>
            {
                var providers = openRiaOptions.SerializationProviders;
                var newProvider = new Serialization.BinaryXmlSerializationProvider(options);
                bool replaced = false;

                for (int i = 0; i < providers.Length; i++)
                {
                    if (providers[i] is Serialization.BinaryXmlSerializationProvider oldProvider)
                    {
                        // Transfer the shared DataContractCache so existing metadata is reused
                        newProvider._perDomainServiceDataContractCache = oldProvider._perDomainServiceDataContractCache;
                        providers[i] = newProvider;
                        replaced = true;
                        break;
                    }
                }

                if (!replaced)
                {
                    openRiaOptions.AddSerializationProvider(newProvider, defaultProvider: true);
                }
            });

            return this;
        }

        /// <summary>
        /// Removes all registered <see cref="Serialization.ISerializationProvider" />s.
        /// <para>Useful for removing default serialization formats (application/msbin1).</para>
        /// </summary>
        public OpenRiaServicesOptionsBuilder ClearSerializationProviders()
        {
            Services.Configure<OpenRiaServicesOptions>(options =>
            {
                options.ClearSerializationProviders();
            });

            return this;
        }

        private OpenRiaServicesOptionsBuilder AddSerializationProvider(Serialization.ISerializationProvider serializationProvider, bool defaultProvider)
        {
            // When adding options it might make sense to resolve the provider using DI so allowing default options configuration
            //Services.AddSingleton<Serialization.TextXmlSerializationProvider>();
            //Services.AddOptions<OpenRiaServicesOptions>().Configure((OpenRiaServicesOptions opts, Serialization.TextXmlSerializationProvider provider) => { });
            // OR
            // Services.Configure<XmlSerializationOptions>(callback)
            // Services.AddOptions<OpenRiaServicesOptions>().Configure((OpenRiaServicesOptions opts, IOptions<XmlSerializationOptions> options)
            Services.Configure<OpenRiaServicesOptions>(options =>
            {
                options.AddSerializationProvider(serializationProvider, defaultProvider);
            });

            return this;
        }
    }
}
