using System;
using System.Diagnostics.CodeAnalysis;

namespace OpenRiaServices.Client
{
    /// <summary>
    /// Provides a way of creating <see cref="DomainClient"/>s.
    /// Use <see cref="DomainContext.DomainClientFactory"/> to change how <see cref="DomainClient"/>s are created.
    /// </summary>
    public interface IDomainClientFactory
    {
        /// <summary>
        /// Creates an <see cref="DomainClient" /> instance.
        /// </summary>
        /// <param name="serviceContract">The service contract (not null).</param>
        /// <param name="serviceUri">The service URI (not null).</param>
        /// <param name="requiresSecureEndpoint"><c>true</c> if DomainService has the RequiresSecureEndpoint attribute and encryption should be enabled.</param>
        /// <returns>A <see cref="DomainClient"/> to use when communicating with the service</returns>
        DomainClient CreateDomainClient([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type serviceContract, Uri serviceUri, bool requiresSecureEndpoint);
    }
}
