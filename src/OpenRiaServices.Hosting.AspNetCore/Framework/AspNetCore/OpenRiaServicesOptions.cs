using System;
using OpenRiaServices.Server;

#nullable enable

namespace OpenRiaServices.Hosting.AspNetCore
{
    public sealed class OpenRiaServicesOptions
    {
        // TODO: Naming, should it be a single parameter, or should exception details (exception, ServiceContext) be passed as separate parameters?
        public Action<DomainServiceErrorInfo>? OnError { get; set; }
        // public void WithOnError(Action<Exception, DomainServiceErrorInfo>) { }

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

    // TODO: 1: Naming, 2: Move to separate file
    /// <summary>
    /// <see cref="DomainServiceErrorInfo"/> can be used to control how errors are passed to the client
    /// </summary>
    /// <param name="exception"></param>
    /// <param name="fault"></param>
    public sealed class DomainServiceErrorInfo(Exception exception, DomainServiceFault fault, DomainService domainService)
    {
        public Exception Exception { get; } = exception;
        //public DomainService? DomainService { get; } = domainService; // om enkelt att komma åt ananrs ServiceContext
        public DomainServiceContext ServiceContext { get; } = domainService.ServiceContext;

        public string ErrorMessage { get => fault.ErrorMessage; set => fault.ErrorMessage = value; }
        public int ErrorCode { get => fault.ErrorCode; set => fault.ErrorCode = value; }
        public System.Net.HttpStatusCode HttpStatusCode { get; set; }
        // TO REVIEW: Use getter/seter or Set methods (which makes settable properties stand out a bit, but it is not as straightforward as setters)
        // SetErrorMessage()
        // SetErrorCode
        // SetHttpStatusCode


        internal DomainServiceFault DomainServiceFault { get => fault; }
        
        // Maybe
        // void SetFromException(DomainException domainException) { }
    };
}
