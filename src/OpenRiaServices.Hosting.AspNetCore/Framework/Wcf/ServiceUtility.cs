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
            else
            {
                DomainException dpe = e as DomainException;
                if (dpe != null)
                {
                    // we always propagate error info to the client for DomainServiceExceptions
                    fault.ErrorCode = dpe.ErrorCode;
                    fault.ErrorMessage = FormatExceptionMessage(dpe);
                    fault.IsDomainException = true;
                    if (options.IncludeExceptionStackTraceInErrors)
                    {
                        fault.StackTrace = dpe.StackTrace;
                    }

                    return fault;
                }
            }

            // set error code. Also set error message if custom errors is disabled
            fault.ErrorCode = errorCode;
            if (options.IncludeExceptionMessageInErrors)
            {
                fault.ErrorMessage = FormatExceptionMessage(e);

                if (options.IncludeExceptionStackTraceInErrors)
                    fault.StackTrace = e.StackTrace;
            }

            return fault;
        }

        /// <summary>
        /// For the specified exception, return the error message concatenating
        /// the message of any inner exception to one level deep.
        /// </summary>
        /// <param name="e">The exception</param>
        /// <returns>The formatted exception message.</returns>
        private static string FormatExceptionMessage(Exception e)
        {
            if (e.InnerException == null)
            {
                return e.Message;
            }
            return string.Format(CultureInfo.CurrentCulture, Resource.FaultException_InnerExceptionDetails, e.Message, e.InnerException.Message);
        }
    }
}
