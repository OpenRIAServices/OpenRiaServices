using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Cities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Hosting.AspNetCore.Operations;
using OpenRiaServices.Server;
using OpenRiaServices.Server.Test;

namespace OpenRiaServices.Hosting.AspNetCore
{
    [TestClass]
    public class OperationInvokerTests
    {
        [TestMethod]
        public async Task TestCancelInvokeOperationInvoker()
        {
            var operation = GetOperationEntry();
            var serializationHelper = GetSerializationHelper();
            var operationInvoker = new InvokeOperationInvoker(operation, serializationHelper);
            await TestCancelOperationInvoker(operationInvoker);
        }

        [TestMethod]
        public async Task TestCancelQueryOperationInvoker()
        {
            var operation = GetOperationEntry();
            var serializationHelper = GetSerializationHelper();
            var operationInvoker = new QueryOperationInvoker<City>(operation, serializationHelper);
            await TestCancelOperationInvoker(operationInvoker);
        }

        private static async Task TestCancelOperationInvoker(OperationInvoker operationInvoker)
        {
            var context = GetHttpContext();
            var cts = new CancellationTokenSource();
            context.RequestAborted = cts.Token;
            var context2 = GetHttpContext();
            var cts2 = new CancellationTokenSource();
            context2.RequestAborted = cts2.Token;

            cts2.Cancel();
            await operationInvoker.Invoke(context);
            await operationInvoker.Invoke(context2);
            Assert.IsTrue(context.Response.ContentLength is not null, "A response should have been written");
            Assert.IsTrue(context2.Response.ContentLength is null, "A response should not have been written");
        }

        private static SerializationHelper GetSerializationHelper()
        {
            return new SerializationHelper(DomainServiceDescription.GetDescription(typeof(CityDomainService)));
        }

        private static TestDomainOperationEntry GetOperationEntry()
        {
            return new TestDomainOperationEntry(typeof(CityDomainService), "GetCities", DomainOperation.Query, typeof(City), Array.Empty<DomainOperationParameter>(), AttributeCollection.Empty);
        }

        private static HttpContext GetHttpContext()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Response.Body = new MemoryStream();

            var services = new ServiceCollection();
            services.AddScoped(typeof(CityDomainService));
            httpContext.RequestServices = services.BuildServiceProvider();
            httpContext.Request.ContentType = "application/msbin1";

            return httpContext;
        }
    }
}
