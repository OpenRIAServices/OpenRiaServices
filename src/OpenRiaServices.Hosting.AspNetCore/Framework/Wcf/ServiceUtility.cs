using System;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Http;
using OpenRiaServices.Hosting.AspNetCore;
using OpenRiaServices.Server;

// WARNING: Keep this file in sync with OpenRiaServices.Hosting

namespace OpenRiaServices.Hosting.Wcf
{
    internal static class ServiceUtility
    {
        /// <summary>
        /// Transforms the specified exception as appropriate into a fault message that can be sent
        /// back to the client.
        /// </summary>
        /// <remarks>
        /// This method will also trace the exception if tracing is enabled.
        /// </remarks>
        /// <param name="e">The exception that was caught.</param>
        /// <returns>The exception to return.</returns>
        internal static DomainServiceFault CreateFaultException(Exception e, OpenRiaServicesOptions options, System.Security.Principal.IPrincipal user)
        {
            Debug.Assert(!e.IsFatal(), "Fatal exception passed in");
            DomainServiceFault fault = new DomainServiceFault();

            if (e is DomainException dpe)
            {
                // we always propagate error info to the client for DomainServiceExceptions
                fault.SetFromDomainException(dpe, options.IncludeExceptionStackTraceInErrors);
                return fault;
            }

            // we always send back a 200 (i.e. not re-throwing) with the actual error code in 
            // the results (except fo 404) because silverlight only supports 404/500 error code
            fault.ErrorCode = e switch
            {
                // invalid operation exception at root level generates BadRequest
                InvalidOperationException => StatusCodes.Status400BadRequest,
                // UnauthorizedAccessException can happen if we lack permission or are not authenticcated
                UnauthorizedAccessException => user.Identity?.IsAuthenticated == true ? StatusCodes.Status403Forbidden : StatusCodes.Status401Unauthorized,
                _ => StatusCodes.Status500InternalServerError,
            };

            // Set error message if custom errors is disabled
            if (options.IncludeExceptionMessageInErrors)
            {
                fault.SetFromException(e, options.IncludeExceptionStackTraceInErrors);
            }

            return fault;
        }
    }
}
