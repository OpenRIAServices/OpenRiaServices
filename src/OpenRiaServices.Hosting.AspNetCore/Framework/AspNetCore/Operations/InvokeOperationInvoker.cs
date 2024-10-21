using Microsoft.AspNetCore.Http;
using OpenRiaServices.Server;
using System;
using System.Threading.Tasks;

namespace OpenRiaServices.Hosting.AspNetCore.Operations
{
    class InvokeOperationInvoker : OperationInvoker
    {
        public InvokeOperationInvoker(DomainOperationEntry operation, SerializationHelper serializationHelper, OpenRiaServicesOptions options)
                : base(operation, DomainOperationType.Invoke, serializationHelper, serializationHelper.GetSerializer(operation.ReturnType), options)
        {
        }

        public override bool HasSideEffects => ((InvokeAttribute)DomainOperation.OperationAttribute).HasSideEffects;

        public override async Task Invoke(HttpContext context)
        {
            try
            {
                DomainService domainService = CreateDomainService(context);

                // consider using ArrayPool<object>.Shared in future for allocating parameters
                object[] inputs;
                if (context.Request.Method == "GET")
                {
                    inputs = GetParametersFromUri(context);
                }
                else // POST
                {
                    if (context.Request.ContentType != "application/msbin1")
                    {
                        context.Response.StatusCode = 400; // maybe 406 / System.Net.HttpStatusCode.NotAcceptable
                        return;
                    }
                    (_, inputs) = await ReadParametersFromBodyAsync(context);
                }

                ServiceInvokeResult invokeResult;
                try
                {
                    var invokeDescription = new InvokeDescription(_operation, inputs);
                    invokeResult = await domainService.InvokeAsync(invokeDescription, domainService.ServiceContext.CancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (!ex.IsFatal())
                {
                    await WriteError(context, ex, domainService);
                    return;
                }

                if (invokeResult.HasValidationErrors)
                {
                    await WriteError(context, invokeResult.ValidationErrors);
                }
                else
                {
                    await WriteResponse(context, invokeResult.Result);
                }
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                //Swallow OperationCanceledException and do nothing
            }
        }
    }
}
