using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Xml;
using OpenRiaServices.Hosting.Wcf.Behaviors;
using OpenRiaServices.Server;

// WARNING: Keep this file in sync with OpenRiaServices.Hosting

namespace OpenRiaServices.Hosting.Wcf
{
    internal static class ServiceUtility
    {
        internal const long MaxReceivedMessageSize = int.MaxValue;
        internal const string SubmitOperationName = "SubmitChanges";
        internal const string ServiceFileExtension = ".svc";

        internal static readonly object[] EmptyObjectArray = Array.Empty<object>();


        /// <summary>
        /// Transforms the specified exception as appropriate into a fault message that can be sent
        /// back to the client.
        /// </summary>
        /// <remarks>
        /// This method will also trace the exception if tracing is enabled.
        /// </remarks>
        /// <param name="e">The exception that was caught.</param>
        /// <param name="hideStackTrace">same as <see cref="HttpContext.IsCustomErrorEnabled"/> <c>true</c> means dont send stack traces</param>
        /// <returns>The exception to return.</returns>
        internal static DomainServiceFault CreateFaultException(Exception e, bool hideStackTrace)
        {
            Debug.Assert(!e.IsFatal(), "Fatal exception passed in");
            DomainServiceFault fault = new DomainServiceFault();

            // Unwrap any TargetInvocationExceptions to get the real exception.
            e = ExceptionHandlingUtility.GetUnwrappedException(e);

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
                    if (!hideStackTrace)
                    {
                        // also send the stack trace if custom errors is disabled
                        fault.StackTrace = dpe.StackTrace;
                    }

                    return fault;
                }
            }

            // set error code. Also set error message if custom errors is disabled
            fault.ErrorCode = errorCode;
            if (!hideStackTrace)
            {
                fault.ErrorMessage = FormatExceptionMessage(e);
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
