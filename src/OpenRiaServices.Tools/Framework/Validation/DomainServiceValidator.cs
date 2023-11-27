using System;

namespace OpenRiaServices.Tools.Validation
{
    /// <summary>
    /// Validator used by the <see cref="ValidateDomainServicesTask"/> to validate the
    /// integrity of the <see cref="OpenRiaServices.Server.DomainService"/>s exposed by the target Web Application
    /// </summary>
    /// <remarks>
    /// This class is <see cref="MarshalByRefObject"/> so that it can be invoked across
    /// AppDomain boundaries.
    /// </remarks>
    internal sealed class DomainServiceValidator : MarshalByRefObject, IDisposable
#if NETFRAMEWORK
        , System.Web.Hosting.IRegisteredObject
#endif
    {
        public void Validate(string[] assemblies, ILoggingService logger)
        {
            if (assemblies == null)
            {
                throw new ArgumentNullException(nameof(assemblies));
            }
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            // Just creating an instance of the DomainServiceCatalog will locate all the DomainServices in the specified
            // assemblies and use their corresponding DomainServiceDescriptions to identify and report errors. These
            // errors will be reported using the logger we provide.
            _ = new DomainServiceCatalog(assemblies, logger);
        }

#if NETFRAMEWORK
        void System.Web.Hosting.IRegisteredObject.Stop(bool immediate)
        {
            // Do nothing	
        }
#endif

        void IDisposable.Dispose()
        {
            // Do nothing
        }
    }
}
