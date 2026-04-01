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
        public OpenRiaServicesOptionsBuilder AddXmlSerializer(bool defaultProvider = false)
        {
            // When adding options it might make sense to resolve the provider using DI so allowing default options configuration
            // Services.AddSingleton<Serialization.TextXmlSerializationProvider>();
            // Services.AddOptions<OpenRiaServicesOptions>().Configure((OpenRiaServicesOptions opts, Serialization.TextXmlSerializationProvider provider)
            // OR
            // Services.Configure<XmlSerializationOptions>(callback)
            // Services.AddOptions<OpenRiaServicesOptions>().Configure((OpenRiaServicesOptions opts, IOptions<XmlSerializationOptions> options)
            Services.Configure<OpenRiaServicesOptions>(options =>
            {
                options.AddSerializationProvider(new Serialization.TextXmlSerializationProvider(), defaultProvider);
            });

            return this;
        }

        /// <summary>
        /// Removes all registered <see cref="Serialization.ISerializationProvider" />s.
        /// <para>Useful for removing default serialization formats (application/msbin1).</para>
        /// </summary>
        public OpenRiaServicesOptionsBuilder ClearSerializers()
        {
            Services.Configure<OpenRiaServicesOptions>(options =>
            {
                options.ClearSerializationProviders();
            });
            
            return this;
        }
    }
}
