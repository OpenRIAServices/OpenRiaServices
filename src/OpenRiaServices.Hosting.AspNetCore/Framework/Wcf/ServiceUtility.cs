using System;
using System.Diagnostics;
using System.Globalization;
using System.Net;
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
        internal static DomainServiceFault CreateFaultException(Exception e, OpenRiaServicesOptions options)
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
            // the results (except fo 404) because silverlight only supports 404/500 error code. If customErrors 
            // are disabled, we'll also send the error message.
            int errorCode = (int)HttpStatusCode.InternalServerError;
            if (e is InvalidOperationException)
            {
                // invalid operation exception at root level generates BadRequest
                errorCode = (int)HttpStatusCode.BadRequest;
            }
            else if (e is UnauthorizedAccessException)
            {
                errorCode = (int)HttpStatusCode.Unauthorized;
            }

            // set error code. Also set error message if custom errors is disabled
            fault.ErrorCode = errorCode;
            if (options.IncludeExceptionMessageInErrors)
            {
                fault.SetFromException(e, options.IncludeExceptionStackTraceInErrors);
            }

            return fault;
        }
    }
}
