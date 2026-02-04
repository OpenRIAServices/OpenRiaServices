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
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace OpenRiaServices.Hosting.AspNetCore.Operations
{
    abstract class OperationInvoker
    {
        private static readonly WebHttpQueryStringConverter s_queryStringConverter = new();

        protected readonly DomainOperationEntry _operation;
        private readonly DomainOperationType _operationType;
        private RequestSerializer[]? _requestSerializers;

        /// <summary>
        /// Initializes a new instance of <see cref="OperationInvoker"/> for the specified domain operation and operation type.
        /// </summary>
        /// <param name="operation">The domain operation entry this invoker will handle.</param>
        /// <param name="operationType">The type of the domain operation (e.g., query, invoke) for contextual behavior.</param>
        /// <param name="options">Runtime options that control serialization, error handling, and other hosting behaviors.</param>
        protected OperationInvoker(DomainOperationEntry operation, DomainOperationType operationType, OpenRiaServicesOptions options)
        {
            this._operation = operation;
            this._operationType = operationType;
            Options = options;
        }

        public virtual string OperationName => _operation.Name;
        public DomainOperationEntry DomainOperation => _operation;

        public abstract bool HasSideEffects { get; }

        public OpenRiaServicesOptions Options { get; }

        /// <summary>
/// Invokes the domain operation for the given HTTP context and produces the HTTP response.
/// </summary>
/// <param name="context">The current HTTP context for the request being invoked.</param>
/// <returns>A task that completes when the operation invocation has finished and the response (or error) has been produced.</returns>
public abstract Task Invoke(HttpContext context);


        private RequestSerializer[] RequestSerializers
        {
            get
            {
                return _requestSerializers ?? CreateSerializersArray();

                // Separate creation to separate method to allow inlining of getter when value is set
                [MethodImpl(MethodImplOptions.NoInlining)]
                RequestSerializer[] CreateSerializersArray()
                {
                    RequestSerializer[] result;
                    var providers = Options.SerializationProviders;

                    result = new RequestSerializer[providers.Length];
                    for (int i = 0; i < providers.Length; i++)
                    {
                        result[i] = providers[i].GetRequestSerializer(DomainOperation);
                    }

                    _requestSerializers = result; // Compare Exchange
                    return result;
                }
            }
        }

        /// <summary>
        /// Get matching serialzer for reading the contents of <paramref name="context"/>, or <see langword="null"/>
        /// if no serialiser can read the format.
        /// <summary>
        /// Selects a RequestSerializer capable of reading the request's Content-Type header.
        /// </summary>
        /// <param name="context">The current HTTP context whose request headers are inspected.</param>
        /// <returns>The RequestSerializer that can read the request's Content-Type, or null if no serializer matches.</returns>
        protected RequestSerializer? TryGetSerializerForReading(HttpContext context)
        {
            var serializers = RequestSerializers;
            string contentType = context.Request.Headers.ContentType.ToString();

            foreach (var serializer in serializers)
            {
                if (serializer.CanRead(contentType))
                    return serializer;
            }

            return null;
        }

        /// <summary>
        /// Get serialzer to use for writing the response, based on client preferences.
        /// <summary>
        /// Selects the serializer to use for writing the response by negotiating the request's Accept and Content-Type headers, falling back to the first available serializer if no match is found.
        /// </summary>
        /// <param name="context">The current HTTP context used to read request headers for content negotiation.</param>
        /// <returns>The serializer selected for writing the response.</returns>
        protected RequestSerializer GetSerializerForWrite(HttpContext context)
        {
            // Look att accept headers first, then content-type
            var serializers = RequestSerializers;
            if (serializers.Length == 1)
                return serializers[0];

            return DoContentNegotiation(context, serializers);

            static RequestSerializer DoContentNegotiation(HttpContext context, RequestSerializer[] serializers)
            {
                var header = context.Request.Headers.Accept;

                // Handle only simple accept headers at the moment, since that is what domainclients are expected to use
                if (header.Count == 1 && MediaTypeHeaderValue.TryParse(header[0], out var mediaType))
                {
                    var mediaTypeSpan = mediaType.MediaType.AsSpan();
                    foreach (var serializer in serializers)
                    {
                        if (serializer.CanWrite(mediaTypeSpan))
                            return serializer;
                    }
                }
                else if (header.Count > 0 && MediaTypeHeaderValue.TryParseList(header, out var mediaTypes)) // multiple accept headers
                {
                    foreach (var type in mediaTypes.OrderByDescending(x => x.Quality ?? 1.0))
                    {
                        foreach (var serializer in serializers)
                        {
                            if (serializer.CanWrite(type.MediaType))
                                return serializer;
                        }
                    }
                }

                // Check Content-Type which is set on all POST requests
                if (context.Request.Headers.ContentType.Count > 0)
                {
                    string contentType = context.Request.Headers.ContentType.ToString();
                    foreach (var serializer in serializers)
                    {
                        if (serializer.CanWrite(contentType))
                            return serializer;
                    }
                }

                // Failed to find a match, fallback to the first one (default) for now
                return serializers[0];
            }
        }

        /// <summary>
        /// Sets response headers to disable caching for the current HTTP response.
        /// </summary>
        /// <param name="context">The current HTTP context whose response headers will be modified.</param>
        protected static void SetDefaultResponseHeaders(HttpContext context)
        {
            context.Response.Headers.CacheControl = "private, no-store";
        }

        /// <summary>
        /// Extracts the operation's parameters from the request query string and converts each value to the parameter's target type.
        /// </summary>
        /// <param name="context">The current HTTP context whose request query string contains the parameter values.</param>
        /// <returns>
        /// An array of converted parameter values aligned with the operation's parameter order; entries are `null` when a parameter is missing or its query value is null.
        /// </returns>
        protected object?[] GetParametersFromUri(HttpContext context)
        {
            var query = context.Request.Query;
            var parameters = _operation.Parameters;
            var inputs = new object?[parameters.Count];
            for (int i = 0; i < parameters.Count; ++i)
            {
                if (query.TryGetValue(parameters[i].Name, out var values))
                {
                    string? value = values[0];
                    if (value is not null)
                    {
                        value = Uri.UnescapeDataString(value);
                        inputs[i] = s_queryStringConverter.ConvertStringToValue(value, parameters[i].ParameterType);
                    }
                    else
                    {
                        inputs[i] = null;
                    }
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
        /// <summary>
        /// Creates a DomainServiceFault from the given exception, sets the appropriate HTTP status code, invokes the configured exception handler if any, and delegates serialization of the fault to the provided writer.
        /// </summary>
        /// <param name="writer">The serializer responsible for writing the fault to the response.</param>
        /// <param name="context">The current HTTP context.</param>
        /// <param name="ex">The exception to convert into a fault.</param>
        /// <param name="domainService">The domain service associated with the operation; used to build the fault and determine authentication state.</param>
        /// <returns>A task that completes when the fault has been written to the response.</returns>
        protected Task WriteError(RequestSerializer writer, HttpContext context, Exception ex, DomainService domainService)
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

            return WriteError(writer, context, fault);
        }

        /// <summary>
        /// Serialize the given domain service fault and write it to the HTTP response using the specified serializer.
        /// </summary>
        /// <param name="writer">The serializer used to format the fault into the response.</param>
        /// <param name="context">The current HTTP context for the request/response.</param>
        /// <param name="fault">The domain service fault to serialize and send to the client.</param>
        /// <returns>A task that completes when the fault has been written to the response.</returns>
        protected Task WriteError(RequestSerializer writer, HttpContext context, DomainServiceFault fault)
        {
            return writer.WriteErrorAsync(context, fault, _operation);
        }

        /// <summary>
        /// Serializes the provided validation errors into a DomainServiceFault, sets the response status to 422 Unprocessable Entity, and writes the fault using the specified serializer.
        /// </summary>
        /// <param name="writer">The serializer to use for writing the fault to the response.</param>
        /// <param name="context">The current HTTP context whose response will be written and status set.</param>
        /// <param name="validationErrors">Validation results to include in the fault's OperationErrors.</param>
        /// <returns>A task that completes when the fault has been written to the response.</returns>
        protected Task WriteError(RequestSerializer writer, HttpContext context, IEnumerable<ValidationResult> validationErrors)
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
            return WriteError(writer, context, new DomainServiceFault { OperationErrors = errors, ErrorCode = StatusCodes.Status422UnprocessableEntity });
        }

        /// <summary>
        /// Resolves and initializes a DomainService instance for the current request and operation.
        /// </summary>
        /// <param name="context">The current HTTP context used to resolve services and provide request-specific state.</param>
        /// <returns>The DomainService instance initialized with an AspNetDomainServiceContext for this request and operation.</returns>
        protected DomainService CreateDomainService(HttpContext context)
        {
            var domainService = (DomainService)context.RequestServices.GetRequiredService(_operation.DomainServiceType);
            var serviceContext = new AspNetDomainServiceContext(context, _operationType);
            domainService.Initialize(serviceContext);
            return domainService;
        }
    }
}