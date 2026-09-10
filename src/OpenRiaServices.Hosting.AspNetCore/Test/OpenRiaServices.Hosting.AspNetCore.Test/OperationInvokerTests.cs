using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Cities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Hosting.AspNetCore.Operations;
using OpenRiaServices.Hosting.AspNetCore.Serialization;
using OpenRiaServices.Server;

namespace OpenRiaServices.Hosting.AspNetCore;

[TestClass]
public class OperationInvokerTests
{
    private static readonly DomainServiceDescription s_domainServiceDescription = DomainServiceDescription.GetDescription(typeof(OperationCanceledDomainService));
    private static readonly OpenRiaServicesOptions s_options = new OpenRiaServicesOptions()
    {
        SerializationProviders = [new BinaryXmlSerializationProvider()]
    };

    public enum SubmitType
    {
        Cancel = 0,
        CancelAndThrow = 1,
    }

    [TestMethod]
    [Description("Invoke operation: Cancellation is requested but method returned succesfully")]
    public async Task TestInvoke_CancelAndReturn()
    {
        var operation = s_domainServiceDescription.GetInvokeOperation(nameof(OperationCanceledDomainService.Invoke_CancelAndReturn));

        var operationInvoker = new InvokeOperationInvoker(operation, s_options);

        await InvokeAndAssertNoResponseIsWritten(operationInvoker);
    }

    [TestMethod]
    [Description("Invoke operation: Cancellation is requested and method throws OperationCanceledException")]
    public async Task TestInvoke_CancelAndAbort()
    {
        var operation = s_domainServiceDescription.GetInvokeOperation(nameof(OperationCanceledDomainService.Invoke_CancelAndAbort));

        var operationInvoker = new InvokeOperationInvoker(operation, s_options);

        await InvokeAndAssertNoResponseIsWritten(operationInvoker);
    }

    [TestMethod]
    [Description("Query operation: Cancellation is requested but method returned succesfully")]
    public async Task TestQuery_CancelAndReturn()
    {
        var operation = s_domainServiceDescription.GetQueryMethod(nameof(OperationCanceledDomainService.Query_CancelAndReturn));

        var operationInvoker = new QueryOperationInvoker<City>(operation, s_options);

        await InvokeAndAssertNoResponseIsWritten(operationInvoker);
    }

    [TestMethod]
    [Description("Query operation: Cancellation is requested and method throws OperationCanceledException")]
    public async Task TestQuery_CancelAndAbort()
    {
        var operation = s_domainServiceDescription.GetQueryMethod(nameof(OperationCanceledDomainService.Query_CancelAndAbort));

        var operationInvoker = new QueryOperationInvoker<City>(operation, s_options);

        await InvokeAndAssertNoResponseIsWritten(operationInvoker);
    }

    [TestMethod]
    [Description("Query operation: IAsyncEnumerable<T> results are recognized and composed")]
    public async Task TestQuery_IAsyncEnumerable()
    {
        var description = DomainServiceDescription.GetDescription(typeof(AsyncEnumerableQueryDomainService));
        var operation = description.GetQueryMethod(nameof(AsyncEnumerableQueryDomainService.GetCities));

        Assert.IsNotNull(operation);
        Assert.AreEqual(typeof(City), operation.AssociatedType);
        Assert.IsTrue(((QueryAttribute)operation.OperationAttribute).IsComposable);

        var service = new AsyncEnumerableQueryDomainService();
        service.Initialize(new DomainServiceContext(new ServiceCollection().BuildServiceProvider(), new GenericPrincipal(new GenericIdentity("user"), Array.Empty<string>()), DomainOperationType.Query));

        var query = Array.Empty<City>().AsQueryable().Where(c => c.StateName == "WA").Take(1);
        var result = await service.QueryAsync<City>(new QueryDescription(operation, Array.Empty<object>(), includeTotalCount: false, query), CancellationToken.None);

        Assert.IsFalse(result.HasValidationErrors);
        Assert.AreEqual(1, result.Result.Count());
        Assert.AreEqual("Redmond", result.Result.Single().Name);
    }

    [TestMethod]
    [Description("Query operation: IQueryable results that also implement IAsyncEnumerable<T> are composed before enumeration")]
    public async Task TestQuery_IQueryableAsyncEnumerable_ComposesBeforeEnumeration()
    {
        var description = DomainServiceDescription.GetDescription(typeof(AsyncQueryableQueryDomainService));
        var operation = description.GetQueryMethod(nameof(AsyncQueryableQueryDomainService.GetCities));

        Assert.IsNotNull(operation);

        var service = new AsyncQueryableQueryDomainService();
        service.Initialize(new DomainServiceContext(new ServiceCollection().BuildServiceProvider(), new GenericPrincipal(new GenericIdentity("user"), Array.Empty<string>()), DomainOperationType.Query));

        var query = Array.Empty<City>().AsQueryable().Where(c => c.StateName == "WA").Take(1);
        var result = await service.QueryAsync<City>(new QueryDescription(operation, Array.Empty<object>(), includeTotalCount: false, query), CancellationToken.None);

        Assert.IsFalse(result.HasValidationErrors);
        Assert.AreEqual(1, result.Result.Count());
        Assert.AreEqual("Redmond", result.Result.Single().Name);
    }

    [TestMethod]
    [Description("Submit operation: Cancellation is requested but method returned succesfully")]
    public async Task TestSubmit_CancelAndReturn()
    {
        var submit = new ReflectionDomainServiceDescriptionProvider.ReflectionDomainOperationEntry(s_domainServiceDescription.DomainServiceType,
            typeof(DomainService).GetMethod(nameof(DomainService.SubmitAsync)), DomainOperation.Custom);

        var operationInvoker = new SubmitOperationInvoker(submit, s_options);

        await InvokeAndAssertNoResponseIsWritten(operationInvoker, SubmitType.Cancel);
    }

    [TestMethod]
    [Description("Submit operation: Cancellation is requested and method throws OperationCanceledException")]
    public async Task TestSubmit_CancelAndAbort()
    {
        var submit = new ReflectionDomainServiceDescriptionProvider.ReflectionDomainOperationEntry(s_domainServiceDescription.DomainServiceType,
            typeof(DomainService).GetMethod(nameof(DomainService.SubmitAsync)), DomainOperation.Custom);

        var operationInvoker = new SubmitOperationInvoker(submit, s_options);

        await InvokeAndAssertNoResponseIsWritten(operationInvoker, SubmitType.CancelAndThrow);
    }

    private static async Task InvokeAndAssertNoResponseIsWritten(OperationInvoker operationInvoker)
    {
        var context = GetHttpContext();
        await operationInvoker.Invoke(context);
        Assert.IsNull(context.Response.ContentLength, "A response should not have been written");
    }

    private static async Task InvokeAndAssertNoResponseIsWritten(SubmitOperationInvoker operationInvoker, SubmitType submitType)
    {
        var bytes = GetEmptyChangeSet(Array.Empty<ChangeSetEntry>());
        var context = GetHttpContext(submitType);
        context.Request.Method = "POST";
        context.Request.Body = bytes;
        await operationInvoker.Invoke(context);
        Assert.IsNull(context.Response.ContentLength, "A response should not have been written");
    }

    private static HttpContext GetHttpContext(SubmitType submitType = SubmitType.Cancel)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        var cts = new CancellationTokenSource();
        var domainService = new OperationCanceledDomainService(cts, submitType);

        var services = new ServiceCollection();
        services.AddSingleton(domainService);
        httpContext.RequestServices = services.BuildServiceProvider();
        httpContext.Request.ContentType = "application/msbin1";
        httpContext.RequestAborted = cts.Token;

        return httpContext;
    }

    private static MemoryStream GetEmptyChangeSet(IEnumerable<ChangeSetEntry> changeSet)
    {
        var requestBody = new MemoryStream();
        var dataContract = new DataContractSerializer(typeof(ChangeSetEntry[]), s_domainServiceDescription.EntityTypes);
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

    [EnableClientAccess]
    public class AsyncEnumerableQueryDomainService : DomainService
    {
        [Query]
        public async IAsyncEnumerable<City> GetCities([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            yield return new City() { Name = "Redmond", CountyName = "King", StateName = "WA" };
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            yield return new City() { Name = "Bellevue", CountyName = "King", StateName = "WA" };
        }
    }

    [EnableClientAccess]
    public class AsyncQueryableQueryDomainService : DomainService
    {
        [Query]
        public IQueryable<City> GetCities()
        {
            return new ThrowOnSecondAsyncEnumerationQueryable<City>(new[]
            {
                new City() { Name = "Redmond", CountyName = "King", StateName = "WA" },
                new City() { Name = "Vancouver", CountyName = "Clark", StateName = "WA" },
            });
        }
    }

    internal sealed class ThrowOnSecondAsyncEnumerationQueryable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>
    {
        public ThrowOnSecondAsyncEnumerationQueryable(IEnumerable<T> enumerable)
            : base(enumerable)
        {
        }

        public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            var index = 0;
            foreach (var item in this)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (index++ > 0)
                {
                    throw new InvalidOperationException("Async enumeration should only happen after query composition.");
                }

                yield return item;
                await Task.Yield();
            }
        }
    }
}
