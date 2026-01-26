using System;
using OpenRiaServices.Server;

#nullable enable

namespace OpenRiaServices.Hosting.AspNetCore
{
    public sealed class OpenRiaServicesOptions
    {
        /// <summary>
        /// Specifies a global exception handler for unhandled exceptions that occur during the processing of a request.
        /// <para>It is similar to <see cref="DomainService.OnError(DomainServiceErrorInfo)"/> but is shared for all DomainServices 
        /// and allows direct modification of the error message and status code sent to the client.</para>
        /// </summary>
        public Action<UnhandledExceptionContext, UnhandledExceptionResponse>? ExceptionHandler { get; set; }

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
}
