using Microsoft.AspNetCore.Http;
using OpenRiaServices;
using OpenRiaServices.Hosting.AspNetCore;
using OpenRiaServices.Server;
using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;

class InvokeOperationInvoker : OperationInvoker
{
    public InvokeOperationInvoker(DomainOperationEntry operation, SerializationHelper serializationHelper)
            : base(operation, DomainOperationType.Invoke, serializationHelper, GetRespponseSerializer(operation, serializationHelper))
    {
    }

    private static DataContractSerializer GetRespponseSerializer(DomainOperationEntry operation, SerializationHelper serializationHelper)
    {
        // var knownTypes = DomainServiceDescription.GetDescription(operation.DomainServiceType).EntityKnownTypes;
        return serializationHelper.GetSerializer(operation.ReturnType, null);
    }

    public override async Task Invoke(HttpContext context)
    {
        DomainService domainService = CreateDomainService(context);

        // TODO: consider using ArrayPool<object>.Shared in future for allocating parameters
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
            (_, inputs) = await ReadParametersFromBody(context);
        }

        ServiceInvokeResult invokeResult;
        try
        {
            //            SetOutputCachingPolicy(httpContext, operation);
            InvokeDescription invokeDescription = new InvokeDescription(this.operation, inputs);
            invokeResult = await domainService.InvokeAsync(invokeDescription, domainService.ServiceContext.CancellationToken).ConfigureAwait(false);

        }
        catch (Exception ex) when (!ex.IsFatal())
        {
            //   ClearOutputCachingPolicy(httpContext);
            await WriteError(context, ex, hideStackTrace: true);
            return;
        }

        if (invokeResult.HasValidationErrors)
        {
            await WriteError(context, invokeResult.ValidationErrors, hideStackTrace: true);
        }
        else
        {
            await WriteResponse(context, invokeResult.Result);
        }
    }
}
