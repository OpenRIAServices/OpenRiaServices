using Microsoft.AspNetCore.Http;
using OpenRiaServices.Hosting.AspNetCore.Serialization;
using OpenRiaServices.Hosting.Wcf;
using OpenRiaServices.Server;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;

#nullable disable

namespace OpenRiaServices.Hosting.AspNetCore.Operations
{
    class SubmitOperationInvoker : OperationInvoker
    {
        public override string OperationName => "SubmitChanges";
        public override bool HasSideEffects => true;

        public SubmitOperationInvoker(DomainOperationEntry operation, RequestSerializer serializer, OpenRiaServicesOptions options)
                : base(operation, DomainOperationType.Submit, serializer, options)
        {
        }

        public override async Task Invoke(HttpContext context)
        {
            SetDefaultResponseHeaders(context);

            try
            {
                DomainService domainService = CreateDomainService(context);
                // Assert post ?

                if (context.Request.ContentType != "application/msbin1"
                    && context.Request.ContentType != "application/xml")
                {
                    context.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
                    return;
                }

                var changeSetEntries = await _requestSerializer.ReadSubmitRequest(context);

                IEnumerable<ChangeSetEntry> result;
                try
                {
                    result = await ChangeSetProcessor.ProcessAsync(domainService, changeSetEntries);
                }
                catch (Exception ex) when (!ex.IsFatal())
                {
                    await WriteError(context, ex, domainService);
                    return;
                }

                // Set HTTP StatusCode on failed requests
                foreach (var change in result)
                {
                    if (change.HasError)
                    {
                        if (change.HasConflict)
                            context.Response.StatusCode = StatusCodes.Status409Conflict;
                        else
                            context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;

                        break;
                    }
                }

                await WriteResponse(context, result);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                //Swallow OperationCanceledException and do nothing
            }
        }
    }
}
