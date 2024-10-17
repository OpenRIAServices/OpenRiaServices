using System;

#nullable enable

namespace OpenRiaServices.Hosting.AspNetCore
{
    public sealed class OpenRiaServicesOptions
    {
        //public Action<Exception, HttpContext> OnError { get; set; }
        //EventHandler<DomainServiceErrorEventArgs> OnError;
        // struct DomainServiceErrorEventArgs(Exception, Message? or IncludeExceptionMessageInFault, Stacktrace?, ErrorCode?, HttpResultCode? )

        /// <summary>
        /// Optional function to determine if the exception message should be included in the fault message.
        /// </summary>
        public Func<Exception, bool>? IncludeExceptionMessageInFault { get; set; }

        /// <summary>
        /// Setting this to true will include exceåtopm message details in the fault message.
        /// </summary>
        public bool? AlwaysIncludeExceptionMessageInFault { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether exception stack trace information should be included in error messages.
        /// <para>This is considered INSECURE since it gives an attacker to much information</para>
        /// </summary>
        public bool UnsafeIncludeStackTraceInErrors { get; set; }
    }
}
