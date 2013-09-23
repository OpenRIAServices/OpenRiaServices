using System;

namespace OpenRiaServices.DomainServices.Server
{
    /// <summary>
    /// Provides an interface for <see cref="DomainService"/> factory implementations.
    /// </summary>
    public interface IDomainServiceFactory
    {
        /// <summary>
        /// Creates a new <see cref="DomainService"/> instance.
        /// </summary>
        /// <param name="domainServiceType">The <see cref="Type"/> of <see cref="DomainService"/> to create.</param>
        /// <param name="context">The current <see cref="DomainServiceContext"/>.</param>
        /// <returns>A <see cref="DomainService"/> instance.</returns>
        DomainService CreateDomainService(Type domainServiceType, DomainServiceContext context);

        /// <summary>
        /// Releases an existing <see cref="DomainService"/> instance.
        /// </summary>
        /// <param name="domainService">The <see cref="DomainService"/> instance to release.</param>
        void ReleaseDomainService(DomainService domainService);
    }
}
