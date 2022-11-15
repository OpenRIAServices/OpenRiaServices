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
    private static CancellationTokenSource cts;
    private static string submitType;
    private static readonly DomainServiceDescription domainServiceDescription = DomainServiceDescription.GetDescription(typeof(OperationCanceledDomainService));
    private static readonly SerializationHelper serializationHelper = new(domainServiceDescription);

    [TestMethod]
    public async Task TestCancelInMethodInvokeOperationInvoker()
    {
        cts = new CancellationTokenSource();
        var operation = domainServiceDescription.GetInvokeOperation("CancelInvoke");

        var operationInvoker = new InvokeOperationInvoker(operation, serializationHelper);
        await TestCancelOperationInvoker(operationInvoker);
    }

    [TestMethod]
    public async Task TestCancelAndThrowInMethodInvokeOperationInvoker()
    {
        cts = new CancellationTokenSource();
        var operation = domainServiceDescription.GetInvokeOperation("CancelAndThrowExceptionInvoke");

        var operationInvoker = new InvokeOperationInvoker(operation, serializationHelper);
        await TestCancelOperationInvoker(operationInvoker);
    }

    [TestMethod]
    public async Task TestCancelBeforeMethodInvokeOperationInvoker()
    {
        cts = new CancellationTokenSource();
        var operation = domainServiceDescription.GetInvokeOperation("CheckCancellationRequestedInvoke");

        var operationInvoker = new InvokeOperationInvoker(operation, serializationHelper);
        cts.Cancel();
        await TestCancelOperationInvoker(operationInvoker);
    }

    [TestMethod]
    public async Task TestCancelInMethodQueryOperationInvoker()
    {
        cts = new CancellationTokenSource();
        var operation = domainServiceDescription.GetQueryMethod("CancelQuery");
        var operationInvoker = new QueryOperationInvoker<City>(operation, serializationHelper);
        await TestCancelOperationInvoker(operationInvoker);
    }

    [TestMethod]
    public async Task TestCancelAndThrowInMethodQueryOperationInvoker()
    {
        cts = new CancellationTokenSource();
        var operation = domainServiceDescription.GetQueryMethod("CancelAndThrowExceptionQuery");
        var operationInvoker = new QueryOperationInvoker<City>(operation, serializationHelper);
        await TestCancelOperationInvoker(operationInvoker);
    }

    [TestMethod]
    public async Task TestCancelBeforeMethodQueryOperationInvoker()
    {
        cts = new CancellationTokenSource();
        var operation = domainServiceDescription.GetQueryMethod("CheckCancellationRequestedQuery");
        var operationInvoker = new QueryOperationInvoker<City>(operation, serializationHelper);
        cts.Cancel();
        await TestCancelOperationInvoker(operationInvoker);
    }

    [TestMethod]
    public async Task TestCancelInMethodSubmitOperationInvoker()
    {
        cts = new CancellationTokenSource();
        submitType = "Cancel";
        var submit = new ReflectionDomainServiceDescriptionProvider.ReflectionDomainOperationEntry(domainServiceDescription.DomainServiceType,
            typeof(DomainService).GetMethod(nameof(DomainService.SubmitAsync)), DomainOperation.Custom);

        var operationInvoker = new SubmitOperationInvoker(submit, serializationHelper);

        cts.Cancel();
        await TestCancelSubmitOperationInvoker(operationInvoker);
    }

    [TestMethod]
    public async Task TestCancelAndThrowInMethodSubmitOperationInvoker()
    {
        cts = new CancellationTokenSource();
        submitType = "CancelAndThrow";
        var submit = new ReflectionDomainServiceDescriptionProvider.ReflectionDomainOperationEntry(domainServiceDescription.DomainServiceType,
            typeof(DomainService).GetMethod(nameof(DomainService.SubmitAsync)), DomainOperation.Custom);

        var operationInvoker = new SubmitOperationInvoker(submit, serializationHelper);

        cts.Cancel();
        await TestCancelSubmitOperationInvoker(operationInvoker);
    }

    [TestMethod]
    public async Task TestCancelBeforeMethodSubmitOperationInvoker()
    {
        cts = new CancellationTokenSource();
        submitType = "Throw";
        var submit = new ReflectionDomainServiceDescriptionProvider.ReflectionDomainOperationEntry(domainServiceDescription.DomainServiceType,
            typeof(DomainService).GetMethod(nameof(DomainService.SubmitAsync)), DomainOperation.Custom);

        var operationInvoker = new SubmitOperationInvoker(submit, serializationHelper);

        cts.Cancel();
        await TestCancelSubmitOperationInvoker(operationInvoker);
    }

    private static async Task TestCancelOperationInvoker(OperationInvoker operationInvoker)
    {
        var context = GetHttpContext();
        context.RequestAborted = cts.Token;
        await operationInvoker.Invoke(context);
        Assert.IsTrue(context.Response.ContentLength is null, "A response should not have been written");
    }

    private static async Task TestCancelSubmitOperationInvoker(SubmitOperationInvoker operationInvoker)
    {
        var bytes = GetEmptyChangeSet(Array.Empty<ChangeSetEntry>());
        var context = GetHttpContext();
        context.Request.Method = "POST";
        context.Request.Body = bytes;
        context.RequestAborted = cts.Token;
        await operationInvoker.Invoke(context);
        Assert.IsTrue(context.Response.ContentLength is null, "A response should not have been written");
    }

    private static HttpContext GetHttpContext()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        var services = new ServiceCollection();
        services.AddScoped(typeof(OperationCanceledDomainService));
        httpContext.RequestServices = services.BuildServiceProvider();
        httpContext.Request.ContentType = "application/msbin1";

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
        [Invoke]
        public async Task CancelInvokeAsync(CancellationToken ct)
        {
            cts.Cancel();
            await Task.Yield();
        }

        [Invoke]
        public async Task CancelAndThrowExceptionInvokeAsync(CancellationToken ct)
        {
            cts.Cancel();
            await Task.Yield();
            ct.ThrowIfCancellationRequested();
        }

        [Invoke]
        public async Task CheckCancellationRequestedInvokeAsync(CancellationToken ct)
        {
            await Task.Yield();
            ct.ThrowIfCancellationRequested();
        }

        [Query]
        public async Task<IQueryable<City>> CancelQueryAsync(CancellationToken ct)
        {
            cts.Cancel();
            await Task.Yield();
            return Array.Empty<City>().AsQueryable();
        }

        [Query]
        public async Task<IQueryable<City>> CancelAndThrowExceptionQueryAsync(CancellationToken ct)
        {
            cts.Cancel();
            await Task.Yield();
            ct.ThrowIfCancellationRequested();
            return Array.Empty<City>().AsQueryable();
        }

        [Query]
        public async Task<IQueryable<City>> CheckCancellationRequestedQueryAsync(CancellationToken ct)
        {
            await Task.Yield();
            ct.ThrowIfCancellationRequested();
            return Array.Empty<City>().AsQueryable();
        }

        public override ValueTask<bool> SubmitAsync(ChangeSet changeSet, CancellationToken cancellationToken)
        {
            if (submitType.Contains("Cancel"))
                cts.Cancel();

            if (submitType.Contains("Throw"))
                cancellationToken.ThrowIfCancellationRequested();

            return base.SubmitAsync(changeSet, cancellationToken);
        }
    }
}
