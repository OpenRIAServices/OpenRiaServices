using Microsoft.AspNetCore.Http;
using OpenRiaServices.Hosting.AspNetCore.Serialization;
using OpenRiaServices.Server;
using System;
using System.Threading.Tasks;

namespace OpenRiaServices.Hosting.AspNetCore.Operations
{
    class InvokeOperationInvoker : OperationInvoker
    {
        /// <summary>
        /// Initializes a new instance configured to invoke the specified domain operation using the provided hosting options.
        /// </summary>
        /// <param name="operation">The domain operation entry representing the Invoke operation to be handled.</param>
        /// <param name="options">Hosting options that influence serializer selection and runtime behavior.</param>
        public InvokeOperationInvoker(DomainOperationEntry operation, OpenRiaServicesOptions options)
                : base(operation, DomainOperationType.Invoke, options)
        {
        }

        public override bool HasSideEffects => ((InvokeAttribute)DomainOperation.OperationAttribute).HasSideEffects;

        /// <summary>
        /// Processes an HTTP request for the associated invoke domain operation and writes the serialized result or errors to the response.
        /// </summary>
        /// <param name="context">The HTTP context for the incoming request and response.</param>
        /// <returns>A Task that completes when the request has been processed and the response or error has been written.</returns>
        public override async Task Invoke(HttpContext context)
        {
            try
            {
                SetDefaultResponseHeaders(context);

                var writer = GetSerializerForWrite(context);
                if (writer is null)
                {
                    context.Response.StatusCode = StatusCodes.Status406NotAcceptable;
                    return;
                }

                // consider using ArrayPool<object>.Shared in future for allocating parameters
                object?[] inputs;
                if (context.Request.Method == "GET")
                {
                    inputs = GetParametersFromUri(context);
                }
                else // POST
                {
                    var serializer = TryGetSerializerForReading(context);
                    if (serializer is null)
                    {
                        context.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
                        return;
                    }

                    (_, inputs) = await serializer.ReadParametersFromBodyAsync(context, DomainOperation);
                }

                DomainService domainService = CreateDomainService(context);
                ServiceInvokeResult invokeResult;
                try
                {
                    var invokeDescription = new InvokeDescription(_operation, inputs);
                    invokeResult = await domainService.InvokeAsync(invokeDescription, domainService.ServiceContext.CancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (!ex.IsFatal())
                {
                    await WriteError(writer, context, ex, domainService);
                    return;
                }

                if (invokeResult.HasValidationErrors)
                {
                    await WriteError(writer, context, invokeResult.ValidationErrors);
                }
                else
                {
                    await writer.WriteResponseAsync(context, invokeResult.Result, DomainOperation);
                }
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                //Swallow OperationCanceledException and do nothing
            }
        }
    }
}