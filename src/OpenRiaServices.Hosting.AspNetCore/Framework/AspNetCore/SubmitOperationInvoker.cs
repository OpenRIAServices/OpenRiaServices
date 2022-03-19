using Microsoft.AspNetCore.Http;
using OpenRiaServices;
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
            : base(operation, DomainOperationType.Submit, serializationHelper, GetResponseSerializer(operation, serializationHelper))
    {
        var desc = DomainServiceDescription.GetDescription(operation.DomainServiceType);
        var knownTypes = desc.EntityKnownTypes;

        // TODO: Look at wcf code for easiest way to do this
        HashSet<Type> allEntities = new HashSet<Type>(knownTypes.Keys);
        foreach (var item in knownTypes.Values)
            allEntities.UnionWith(item);

        this.parameterSerializer = new DataContractSerializer(typeof(List<ChangeSetEntry>), allEntities);
    }

    private static DataContractSerializer GetResponseSerializer(DomainOperationEntry operation, SerializationHelper serializationHelper)
    {
        var desc = DomainServiceDescription.GetDescription(operation.DomainServiceType);
        var knownTypes = desc.EntityKnownTypes;

        // TODO: Look at wcf code for easiest way to do this
        HashSet<Type> allEntities = new HashSet<Type>(knownTypes.Keys);
        foreach (var item in knownTypes.Values)
            allEntities.UnionWith(item);

        return serializationHelper.GetSerializer(typeof(IEnumerable<ChangeSetEntry>), allEntities);
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

        var (_, inputs) = await ReadParametersFromBody(context);
        IEnumerable<ChangeSetEntry> changeSetEntries = (IEnumerable<ChangeSetEntry>)inputs[0];

        try
        {
            var result = await ChangeSetProcessor.ProcessAsync(domainService, changeSetEntries);
            await WriteResponse(context, result);
        }
        catch (Exception ex) when (!ex.IsFatal())
        {
            await WriteError(context, ex, domainService.GetDisableStackTraces());
        }
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

    public HashSet<Type> GetAllKnownTypes(DomainServiceDescription domainServiceDescription)
    {
        var knownTypes = new HashSet<Type>();

        // Register types used in custom methods. Custom methods show up as part of the changeset.
        // KnownTypes are required for all non-primitive and non-string since these types will show up in the change set.
        foreach (DomainOperationEntry customOp in domainServiceDescription.DomainOperationEntries.Where(op => op.Operation == DomainOperation.Custom))
        {
            // KnownTypes will be added during surrogate registration for all entity and
            // complex types. We skip the first parameter because it is an entity type. We also
            // skip all complex types. Note, we do not skip complex type collections because
            // the act of registering surrogates only adds the type, and KnownTypes needs to
            // know about any collections.
            foreach (Type parameterType in customOp.Parameters.Skip(1).Select(p => p.ParameterType).Where(
                t => !t.IsPrimitive && t != typeof(string) && !domainServiceDescription.ComplexTypes.Contains(t)))
            {
                knownTypes.Add(parameterType);
            }
        }

        return knownTypes;
    }
    
}
