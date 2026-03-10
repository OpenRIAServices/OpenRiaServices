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

        internal IServiceCollection Services { get; }
        // Add Options ? 

        /// <summary>
        /// Enables Text based XML wire format (application/xml) in addition to built in binary XML (application/msbin1).
        /// It does not change the default format.
        /// </summary>
        /// <remarks>Request should specify mime-type <c>application/xml</c> using <c>Content-Type</c> or <c>Accept</c> headers
        /// </remarks>
        public OpenRiaServicesOptionsBuilder WithTextXmlSerialization()
        {
            Services.Configure<OpenRiaServicesOptions>(options =>
            {
                options.AddTextXmlSerializer();
            });

            return this;
        }
    }
}
