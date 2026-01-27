using Microsoft.AspNetCore.Http;
using OpenRiaServices.Hosting.AspNetCore.Serialization;
using OpenRiaServices.Server;
using System;
using System.Threading.Tasks;

namespace OpenRiaServices.Hosting.AspNetCore.Operations
{
    class InvokeOperationInvoker : OperationInvoker
    {
        public InvokeOperationInvoker(DomainOperationEntry operation, RequestSerializer serializer, OpenRiaServicesOptions options)
                : base(operation, DomainOperationType.Invoke, serializer, options)
        {
        }

        public override bool HasSideEffects => ((InvokeAttribute)DomainOperation.OperationAttribute).HasSideEffects;

        public override async Task Invoke(HttpContext context)
        {
            SetDefaultResponseHeaders(context);

            try
            {
                DomainService domainService = CreateDomainService(context);

                var writer = GetSerializerForWrite(context);
                if (writer is null)
                {
                    context.Response.StatusCode = StatusCodes.Status406NotAcceptable;
                    return;
                }

                // consider using ArrayPool<object>.Shared in future for allocating parameters
                object[] inputs;
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
