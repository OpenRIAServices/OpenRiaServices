using System;
using Microsoft.AspNetCore.Http;
using OpenRiaServices.Server;

#nullable enable

namespace OpenRiaServices.Hosting.AspNetCore
{
    public sealed class OpenRiaServicesOptions
    {
        // TODO: should it be a single parameter (Action<UnhandledExceptionParameters>), or Action<Exception, UnhandledExceptionParameters>
        public Action<UnhandledExceptionParameters>? ExceptionHandler { get; set; }
        // public OpenRiaServicesOptions OnUnhandledException(Action<UnhandledExceptionParameters>) { }

        /// <summary>
        /// Gets or sets a value indicating whether exception stack trace information should be included in error messages.
        /// <para>This is considered INSECURE since it gives an attacker to much information</para>
        /// </summary>
        public bool IncludeExceptionStackTraceInErrors { get; set; }

        /// <summary>
        /// UNSAFE: Gets or sets a value indicating whether exception message details (from exceptions other than <see cref="DomainException"/>) should be included in error messages.
        /// <para>This is generally considered INSECURE since it can provide an attacker with to much information</para>
        /// </summary>
        public bool IncludeExceptionMessageInErrors { get; set; }

        /* ************ SOME POSSIBLE FUTURE OPTIONS ************ 
         * 
         * int MaxReceiveSize / MaxRequestSize { get; set; }
         * int MaxResponseSize { get; set; }
         * 
         * int RouteOrder { get; set; }
         * */
    }

    // TODO: 1: Naming UnhandledExceptionParameters or UnhandledExceptionArguments, 2: Move to separate file?
    /// <summary>
    /// <see cref="UnhandledExceptionParameters"/> can be used to control how errors are passed to the client
    /// </summary>
    /// <param name="exception"></param>
    /// <param name="fault"></param>
    public sealed class UnhandledExceptionParameters(Exception exception, DomainServiceFault fault, DomainService domainService, Microsoft.AspNetCore.Http.HttpContext httpContext)
    {
        public Exception Exception { get; } = exception;
        //public DomainService? DomainService { get; } = domainService; // om enkelt att komma åt ananrs ServiceContext
        public DomainServiceContext ServiceContext { get; } = domainService.ServiceContext;

        // Consider exposing in the future for easier access to HTTP headers (user can use ServiceContext.GetService<IHttpContextAccessor>() )
        internal HttpContext HttpContext { get; } = httpContext;

        public string ErrorMessage { get => fault.ErrorMessage; set => fault.ErrorMessage = value; }
        public int ErrorCode { get => fault.ErrorCode; set => fault.ErrorCode = value; }
        public System.Net.HttpStatusCode HttpStatusCode { get => (System.Net.HttpStatusCode)HttpContext.Response.StatusCode; set => HttpContext.Response.StatusCode = (int)value; }

        // TO REVIEW: Use getter/seter or Set methods (which makes settable properties stand out a bit, but it is not as straightforward as setters)
        // SetErrorMessage()
        // SetErrorCode
        // SetHttpStatusCode


        internal DomainServiceFault DomainServiceFault { get => fault; }
        
        // Maybe
        // void SetFromException(DomainException domainException) { }
    };
}
