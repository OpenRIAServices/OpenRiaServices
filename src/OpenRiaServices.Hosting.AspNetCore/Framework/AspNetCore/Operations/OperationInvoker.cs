using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using OpenRiaServices.Hosting.AspNetCore.Serialization;
using OpenRiaServices.Hosting.Wcf;
using OpenRiaServices.Hosting.Wcf.Behaviors;
using OpenRiaServices.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

#nullable disable

namespace OpenRiaServices.Hosting.AspNetCore.Operations
{
    abstract class OperationInvoker
    {
        private static readonly WebHttpQueryStringConverter s_queryStringConverter = new();

        protected readonly DomainOperationEntry _operation;
        private readonly DomainOperationType _operationType;
        protected readonly RequestSerializer _requestSerializer;

        protected OperationInvoker(DomainOperationEntry operation, DomainOperationType operationType,
            RequestSerializer requestSerializer,
            OpenRiaServicesOptions options)
        {
            this._operation = operation;
            this._operationType = operationType;
            _requestSerializer = requestSerializer;
            Options = options;
        }

        public virtual string OperationName => _operation.Name;
        public DomainOperationEntry DomainOperation => _operation;

        public abstract bool HasSideEffects { get; }
        public OpenRiaServicesOptions Options { get; }

        public abstract Task Invoke(HttpContext context);

        protected static void SetDefaultResponseHeaders(HttpContext context)
        {
            context.Response.Headers.CacheControl = "private, no-store";
        }

        protected object[] GetParametersFromUri(HttpContext context)
        {
            var query = context.Request.Query;
            var parameters = _operation.Parameters;
            var inputs = new object[parameters.Count];
            for (int i = 0; i < parameters.Count; ++i)
            {
                if (query.TryGetValue(parameters[i].Name, out var values))
                {
                    var value = Uri.UnescapeDataString(values.FirstOrDefault());
                    inputs[i] = s_queryStringConverter.ConvertStringToValue(value, parameters[i].ParameterType);
                }
            }

            return inputs;
        }

        /// <summary>
        /// Transforms the specified exception as appropriate into a fault message that can be sent
        /// back to the client.
        /// </summary>
        /// <param name="ex">The exception that was caught.</param>
        /// <param name="hideStackTrace">same as <see cref="HttpContext.IsCustomErrorEnabled"/> <c>true</c> means dont send stack traces</param>
        /// <returns>The exception to return.</returns>
        protected Task WriteError(HttpContext context, Exception ex, DomainService domainService)
        {
            // Unwrap any TargetInvocationExceptions to get the real exception.
            ex = ExceptionHandlingUtility.GetUnwrappedException(ex);
            var fault = ServiceUtility.CreateFaultException(ex, Options, domainService.ServiceContext.User);

            // Set HttpStatus
            context.Response.StatusCode = (ex is UnauthorizedAccessException)
                ? (domainService.ServiceContext.User.Identity?.IsAuthenticated == true ? StatusCodes.Status403Forbidden : StatusCodes.Status401Unauthorized)
                : StatusCodes.Status500InternalServerError;

            if (Options.ExceptionHandler is { } exceptionHandler)
            {
                exceptionHandler(new UnhandledExceptionContext(ex, domainService), new UnhandledExceptionResponse(fault, context));
            }

            return WriteError(context, fault);
        }

        protected Task WriteError(HttpContext context, DomainServiceFault fault)
        {
            return _requestSerializer.WriteErrorAsync(context, fault, _operation);
        }

        protected Task WriteResponse(HttpContext context, object response)
        {
            return _requestSerializer.WriteResponseAsync(context, response, _operation);
        }

        protected Task WriteResponse(HttpContext context, IEnumerable<ChangeSetEntry> response)
        {
            return _requestSerializer.WriteResponseAsync(context, response, _operation);
        }

        protected Task<(ServiceQuery, object[])> ReadParametersFromBodyAsync(HttpContext context)
            => _requestSerializer.ReadParametersFromBodyAsync(context, _operation);

        protected Task WriteError(HttpContext context, IEnumerable<ValidationResult> validationErrors)
        {
            var errors = validationErrors.Select(ve => new ValidationResultInfo(ve.ErrorMessage, ve.MemberNames)).ToList();

            // Clear out the stacktrace if they should not be sent
            if (!Options.IncludeExceptionStackTraceInErrors)
            {
                foreach (ValidationResultInfo error in errors)
                {
                    error.StackTrace = null;
                }
            }

            context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
            return WriteError(context, new DomainServiceFault { OperationErrors = errors, ErrorCode = StatusCodes.Status422UnprocessableEntity });
        }


        protected DomainService CreateDomainService(HttpContext context)
        {
            var domainService = (DomainService)context.RequestServices.GetRequiredService(_operation.DomainServiceType);
            var serviceContext = new AspNetDomainServiceContext(context, _operationType);
            domainService.Initialize(serviceContext);
            return domainService;
        }
    }
}
