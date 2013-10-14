using System;

namespace OpenRiaServices.DomainController.Server
{
    /// <summary>
    /// Provides an interface for <see cref="DomainController"/> factory implementations.
    /// </summary>
    public interface IDomainControllerFactory
    {
        /// <summary>
        /// Creates a new <see cref="DomainController"/> instance.
        /// </summary>
        /// <param name="DomainControllerType">The <see cref="Type"/> of <see cref="DomainController"/> to create.</param>
        /// <param name="context">The current <see cref="DomainControllerContext"/>.</param>
        /// <returns>A <see cref="DomainController"/> instance.</returns>
        DomainController CreateDomainController(Type DomainControllerType, DomainControllerContext context);

        /// <summary>
        /// Releases an existing <see cref="DomainController"/> instance.
        /// </summary>
        /// <param name="DomainController">The <see cref="DomainController"/> instance to release.</param>
        void ReleaseDomainController(DomainController DomainController);
    }
}
