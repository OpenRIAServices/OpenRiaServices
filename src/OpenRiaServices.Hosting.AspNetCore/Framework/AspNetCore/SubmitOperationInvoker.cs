using Microsoft.AspNetCore.Http;
using OpenRiaServices;
using OpenRiaServices.Hosting;
using OpenRiaServices.Hosting.AspNetCore;
using OpenRiaServices.Hosting.Wcf;
using OpenRiaServices.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

class SubmitOperationInvoker : OperationInvoker
{
    private DataContractSerializer parameterSerializer;

    public override string Name => "SubmitChanges";

    public SubmitOperationInvoker(DomainOperationEntry operation, SerializationHelper serializationHelper)
            : base(operation, DomainOperationType.Submit, serializationHelper, serializationHelper.GetSerializer(typeof(IEnumerable<ChangeSetEntry>)))
    {
        this.parameterSerializer = serializationHelper.GetSerializer(typeof(IEnumerable<ChangeSetEntry>));
    }

    public override async Task Invoke(HttpContext context)
    {
        DomainService domainService = CreateDomainService(context);
        // Assert post ?

        if (context.Request.ContentType != "application/msbin1")
        {
            context.Response.StatusCode = 400; // maybe 406 / System.Net.HttpStatusCode.NotAcceptable
            return;
        }

        var (_, inputs) = await ReadParametersFromBodyAsync(context);
        IEnumerable<ChangeSetEntry> changeSetEntries = (IEnumerable<ChangeSetEntry>)inputs[0];

        IEnumerable<ChangeSetEntry> result;
        try
        {
            result = await ChangeSetProcessor.ProcessAsync(domainService, changeSetEntries);
        }
        catch (Exception ex) when (!ex.IsFatal())
        {
            await WriteError(context, ex, domainService.GetDisableStackTraces());
            return;
        }

        await WriteResponse(context, result);
    }

    protected override object[] ReadParameters(System.Xml.XmlDictionaryReader reader)
    {
        reader.ReadStartElement("SubmitChanges");
        if (!reader.IsStartElement("changeSet"))
        {
            // TODO: return BADREQUEST_data;
            throw new InvalidOperationException();
        }

        var changeSet = this.parameterSerializer.ReadObject(reader, verifyObjectName: false);
        reader.ReadEndElement();
        return new object[] { changeSet };
    }
}
