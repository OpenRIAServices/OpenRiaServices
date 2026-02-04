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

        protected OperationInvoker(DomainOperationEntry operation, DomainOperationType operationType, OpenRiaServicesOptions options)
        {
            this._operation = operation;
            this._operationType = operationType;
            Options = options;
        }

        public virtual string OperationName => _operation.Name;
        public DomainOperationEntry DomainOperation => _operation;

        /// <summary>
        /// Gets a value indicating whether evaluating this operation can have effects (by looking att <see cref="InvokeAttribute.HasSideEffects"/> or <see cref="QueryAttribute.HasSideEffects"/>)
        /// </summary>
        public abstract bool HasSideEffects { get; }

        public OpenRiaServicesOptions Options { get; }

        /// <summary>
        /// Processes an HTTP request for the associated <see cref="DomainOperation"/> and writes the serialized result or errors to the response.
        /// </summary>
        /// <param name="context">The HTTP context for the incoming request and response.</param>
        /// <returns>A Task that completes when the request has been processed and the response or error has been written.</returns>
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

                    System.Threading.Volatile.Write(ref _requestSerializers, result);
                    return result;
                }
            }
        }

        /// <summary>
        /// Get matching serialzer for reading the contents of <paramref name="context"/>, or <see langword="null"/>
        /// if no serialiser can read the format.
        /// </summary>
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
        /// </summary>
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

        protected static void SetDefaultResponseHeaders(HttpContext context)
        {
            context.Response.Headers.CacheControl = "private, no-store";
        }

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
        /// <returns>The exception to return.</returns>
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

        protected Task WriteError(RequestSerializer writer, HttpContext context, DomainServiceFault fault)
        {
            return writer.WriteErrorAsync(context, fault, _operation);
        }

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

        protected DomainService CreateDomainService(HttpContext context)
        {
            var domainService = (DomainService)context.RequestServices.GetRequiredService(_operation.DomainServiceType);
            var serviceContext = new AspNetDomainServiceContext(context, _operationType);
            domainService.Initialize(serviceContext);
            return domainService;
        }
    }
}
