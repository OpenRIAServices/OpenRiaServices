using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenRiaServices.Hosting.AspNetCore.Serialization;

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
        /// <param name="configure">An optional callback to configure <see cref="XmlSerializationOptions"/>.</param>
        /// <param name="defaultProvider">If <see langword="true"/> the Xml provider will be the default for responses (when content type is not specified)</param>
        public OpenRiaServicesOptionsBuilder AddXmlSerialization(Action<XmlSerializationOptions>? configure, bool defaultProvider = false)
        {
            if (configure is not null)
                Services.Configure(configure);

            Services.AddOptions<OpenRiaServicesOptions>()
                .Configure((OpenRiaServicesOptions options, IOptions<XmlSerializationOptions> serializationOptions) =>
                {
                    options.AddSerializationProvider(new TextXmlSerializationProvider(serializationOptions.Value), defaultProvider);
                });

            return this;
        }

        /// <summary>
        /// Enables MessagePack wire format (<c>application/vnd.msgpack</c>) in addition to built in binary Xml.
        /// </summary>
        /// <remarks>
        /// Request should specify mime-type <c>application/vnd.msgpack</c> using <c>Content-Type</c> or <c>Accept</c> HTTP-headers.
        /// </remarks>
        /// <param name="defaultProvider">If <see langword="true"/> the MessagePack provider will be the default for responses (when content type is not specified)</param>
        public OpenRiaServicesOptionsBuilder AddMessagePackSerialization(bool defaultProvider = false)
        {
            return AddMessagePackSerialization(configure: null, defaultProvider);
        }

        /// <summary>
        /// Enables MessagePack wire format (<c>application/vnd.msgpack</c>) in addition to built in binary Xml,
        /// with options configurable via a callback.
        /// </summary>
        /// <remarks>
        /// Request should specify mime-type <c>application/vnd.msgpack</c> using <c>Content-Type</c> or <c>Accept</c> HTTP-headers.
        /// </remarks>
        /// <param name="configure">An optional callback to configure <see cref="MessagePackSerializationOptions"/>.</param>
        /// <param name="defaultProvider">If <see langword="true"/> the MessagePack provider will be the default for responses (when content type is not specified)</param>
        public OpenRiaServicesOptionsBuilder AddMessagePackSerialization(Action<MessagePackSerializationOptions>? configure, bool defaultProvider = false)
        {
            if (configure is not null)
                Services.Configure(configure);

            Services.AddOptions<OpenRiaServicesOptions>()
                .Configure((OpenRiaServicesOptions options, IOptions<MessagePackSerializationOptions> serializationOptions) =>
                {
                    options.AddSerializationProvider(new MessagePackSerializationProvider(serializationOptions.Value), defaultProvider);
                });

            return this;
        }

        /// <summary>
        /// Configures the default binary XML (<c>application/msbin1</c>) serialization provider.
        /// </summary>
        /// <remarks>
        /// Use this to restrict reader quotas and mitigate denial-of-service risks for binary requests.
        /// </remarks>
        /// <param name="configure">A callback to configure <see cref="BinarySerializationOptions"/>.</param>
        public OpenRiaServicesOptionsBuilder ConfigureBinarySerialization(Action<BinarySerializationOptions> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);

            Services.Configure<BinarySerializationOptions>(configure);

            return this;
        }

        /// <summary>
        /// Removes all registered <see cref="ISerializationProvider" />s.
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
    }
}
