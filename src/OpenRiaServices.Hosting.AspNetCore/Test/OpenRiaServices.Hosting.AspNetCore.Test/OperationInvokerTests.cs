using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Cities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Hosting.AspNetCore.Operations;
using OpenRiaServices.Server;

namespace OpenRiaServices.Hosting.AspNetCore;

[TestClass]
public class OperationInvokerTests
{
    private static readonly DomainServiceDescription domainServiceDescription = DomainServiceDescription.GetDescription(typeof(OperationCanceledDomainService));

    public enum SubmitType
    {
        Cancel = 0,
        CancelAndThrow = 1,
    }

    [TestMethod]
    [Description("Invoke operation: Cancellation is requested but method returned succesfully")]
    public async Task TestInvoke_CancelAndReturn()
    {
        var operation = domainServiceDescription.GetInvokeOperation(nameof(OperationCanceledDomainService.Invoke_CancelAndReturn));
        
        var operationInvoker = new InvokeOperationInvoker(operation, GetSerializationHelper());
        
        await InvokeAndAssertNoResponseIsWritten(operationInvoker);
    }

    [TestMethod]
    [Description("Invoke operation: Cancellation is requested and method throws OperationCanceledException")]
    public async Task TestInvoke_CancelAndAbort()
    {
        var operation = domainServiceDescription.GetInvokeOperation(nameof(OperationCanceledDomainService.Invoke_CancelAndAbort));
        
        var operationInvoker = new InvokeOperationInvoker(operation, GetSerializationHelper());
        
        await InvokeAndAssertNoResponseIsWritten(operationInvoker);
    }

    [TestMethod]
    [Description("Query operation: Cancellation is requested but method returned succesfully")]
    public async Task TestQuery_CancelAndReturn()
    {
        var operation = domainServiceDescription.GetQueryMethod(nameof(OperationCanceledDomainService.Query_CancelAndReturn));
        
        var operationInvoker = new QueryOperationInvoker<City>(operation, GetSerializationHelper());
        
        await InvokeAndAssertNoResponseIsWritten(operationInvoker);
    }

    [TestMethod]
    [Description("Query operation: Cancellation is requested and method throws OperationCanceledException")]
    public async Task TestQuery_CancelAndAbort()
    {
        var operation = domainServiceDescription.GetQueryMethod(nameof(OperationCanceledDomainService.Query_CancelAndAbort));
        
        var operationInvoker = new QueryOperationInvoker<City>(operation, GetSerializationHelper());
        
        await InvokeAndAssertNoResponseIsWritten(operationInvoker);
    }

    [TestMethod]
    [Description("Submit operation: Cancellation is requested but method returned succesfully")]
    public async Task TestSubmit_CancelAndReturn()
    {
        var submit = new ReflectionDomainServiceDescriptionProvider.ReflectionDomainOperationEntry(domainServiceDescription.DomainServiceType,
            typeof(DomainService).GetMethod(nameof(DomainService.SubmitAsync)), DomainOperation.Custom);

        var operationInvoker = new SubmitOperationInvoker(submit, GetSerializationHelper());

        await InvokeAndAssertNoResponseIsWritten(operationInvoker, SubmitType.Cancel);
    }

    [TestMethod]
    [Description("Submit operation: Cancellation is requested and method throws OperationCanceledException")]
    public async Task TestSubmit_CancelAndAbort()
    {
        var submit = new ReflectionDomainServiceDescriptionProvider.ReflectionDomainOperationEntry(domainServiceDescription.DomainServiceType,
            typeof(DomainService).GetMethod(nameof(DomainService.SubmitAsync)), DomainOperation.Custom);

        var operationInvoker = new SubmitOperationInvoker(submit, GetSerializationHelper());

        await InvokeAndAssertNoResponseIsWritten(operationInvoker, SubmitType.CancelAndThrow);
    }

    private static SerializationHelper GetSerializationHelper() => new (domainServiceDescription);

    private static async Task InvokeAndAssertNoResponseIsWritten(OperationInvoker operationInvoker)
    {
        var context = GetHttpContext();
        await operationInvoker.Invoke(context);
        Assert.IsTrue(context.Response.ContentLength is null, "A response should not have been written");
    }

    private static async Task InvokeAndAssertNoResponseIsWritten(SubmitOperationInvoker operationInvoker, SubmitType submitType)
    {
        var bytes = GetEmptyChangeSet(Array.Empty<ChangeSetEntry>());
        var context = GetHttpContext(submitType);
        context.Request.Method = "POST";
        context.Request.Body = bytes;
        await operationInvoker.Invoke(context);
        Assert.IsTrue(context.Response.ContentLength is null, "A response should not have been written");
    }

    private static HttpContext GetHttpContext(SubmitType submitType = SubmitType.Cancel)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        var cts = new CancellationTokenSource();
        var domainService = new OperationCanceledDomainService(cts, submitType);

        var services = new ServiceCollection();
        services.AddScoped(typeof(OperationCanceledDomainService));
        services.AddSingleton(domainService);
        httpContext.RequestServices = services.BuildServiceProvider();
        httpContext.Request.ContentType = "application/msbin1";
        httpContext.RequestAborted = cts.Token;

        return httpContext;
    }

    private static MemoryStream GetEmptyChangeSet(IEnumerable<ChangeSetEntry> changeSet)
    {
        var requestBody = new MemoryStream();
        var dataContract = new DataContractSerializer(typeof(ChangeSetEntry[]), domainServiceDescription.EntityTypes);
        using (var serializer = XmlDictionaryWriter.CreateBinaryWriter(requestBody, null, null, ownsStream: false))
        {
            serializer.WriteStartElement("SubmitChanges");
            serializer.WriteStartElement("changeSet");
            dataContract.WriteObject(serializer, changeSet);
            serializer.WriteEndElement();
            serializer.WriteEndElement();
        }

        requestBody.Seek(0, SeekOrigin.Begin);

        return requestBody;
    }

    public class OperationCanceledDomainService : DomainService
    {
        public OperationCanceledDomainService(CancellationTokenSource cancellationTokenSource, SubmitType submitType) 
        {
            _cancellationTokenSource = cancellationTokenSource;
            _submitType = submitType;
        }

        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly SubmitType _submitType;

        [Invoke]
        public void Invoke_CancelAndReturn(CancellationToken ct)
        {
            _cancellationTokenSource.Cancel();
        }

        [Invoke]
        public void Invoke_CancelAndAbort(CancellationToken ct)
        {
            _cancellationTokenSource.Cancel();
            ct.ThrowIfCancellationRequested();
        }

        [Query]
        public IQueryable<City> Query_CancelAndReturn(CancellationToken ct)
        {
            _cancellationTokenSource.Cancel();
            return Array.Empty<City>().AsQueryable();
        }

        [Query]
        public IQueryable<City> Query_CancelAndAbort(CancellationToken ct)
        {
            _cancellationTokenSource.Cancel();
            ct.ThrowIfCancellationRequested();
            return Array.Empty<City>().AsQueryable();
        }

        public override ValueTask<bool> SubmitAsync(ChangeSet changeSet, CancellationToken cancellationToken)
        {
            _cancellationTokenSource.Cancel();

            if (_submitType is SubmitType.CancelAndThrow)
                cancellationToken.ThrowIfCancellationRequested();

            return base.SubmitAsync(changeSet, cancellationToken);
        }
    }
}
