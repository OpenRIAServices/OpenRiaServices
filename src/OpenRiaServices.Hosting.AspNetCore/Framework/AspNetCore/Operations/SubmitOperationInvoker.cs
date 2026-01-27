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

                var serializer = TryGetSerializerForReading(context);
                if (serializer is null)
                {
                    context.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
                    return;
                }

                var writer = GetSerializerForWrite(context);
                if (writer is null)
                {
                    context.Response.StatusCode = StatusCodes.Status406NotAcceptable;
                    return;
                }

                var changeSetEntries = await serializer.ReadSubmitRequest(context);

                IEnumerable<ChangeSetEntry> result;
                try
                {
                    result = await ChangeSetProcessor.ProcessAsync(domainService, changeSetEntries);
                }
                catch (Exception ex) when (!ex.IsFatal())
                {
                    await WriteError(writer, context, ex, domainService);
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

                await writer.WriteResponseAsync(context, result, DomainOperation);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                //Swallow OperationCanceledException and do nothing
            }
        }
    }
}
