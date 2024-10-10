using System;

#nullable enable

namespace OpenRiaServices.Hosting.AspNetCore
{
    public sealed class OpenRiaServicesOptions
    {
        //public Action<Exception, HttpContext> OnError { get; set; }

        /// <summary>
        /// Optional function to determine if the exception message should be included in the fault message.
        /// </summary>
        public Func<Exception, bool>? IncludeExceptionMessageInFault { get; set; }

        /// <summary>
        /// Setting this to true will include exceåtopm message details in the fault message.
        /// </summary>
        public bool? AlwaysIncludeExceptionMessageInFault { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the stack trace should be included in the error message.
        /// <para>This is considered INSECURE since it gives an attacker to much information</para>
        /// </summary>
        public bool UnsafeIncludeStackTraceInErrors { get; set; }
    }
}
