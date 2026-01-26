using System;
using OpenRiaServices.Server;

#nullable enable

namespace OpenRiaServices.Hosting.AspNetCore
{
    /// <summary>
    /// Provides context on which exception occurred and during what kind of request. 
    /// </summary>
    public readonly struct UnhandledExceptionContext
    {
        public UnhandledExceptionContext(Exception exception, DomainService domainService)
        {
            Exception = ExceptionHandlingUtility.GetUnwrappedException(exception);
            ServiceContext = domainService.ServiceContext;
        }

        /// <summary>
        /// The exception thrown
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// The context in which the exception occurred, can be used to resolve services
        /// </summary>
        public DomainServiceContext ServiceContext { get; }
    }
}
