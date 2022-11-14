using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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

namespace OpenRiaServices.Hosting.AspNetCore
{
    [TestClass]
    public class OperationInvokerTests
    {
        private static DomainServiceDescription DomainServiceDescription = DomainServiceDescription.GetDescription(typeof(CityDomainService));

        [TestMethod]
        public async Task TestCancelInvokeOperationInvoker()
        {
            var operation = DomainServiceDescription.GetInvokeOperation("TODO");

            var serializationHelper = GetSerializationHelper();
            var operationInvoker = new InvokeOperationInvoker(operation, serializationHelper);
            await TestCancelOperationInvoker(operationInvoker);
        }

        [TestMethod]
        public async Task TestCancelQueryOperationInvoker()
        {
            var operation = DomainServiceDescription.GetQueryMethod("GetCities");
            var serializationHelper = GetSerializationHelper();
            var operationInvoker = new QueryOperationInvoker<City>(operation, serializationHelper);
            await TestCancelOperationInvoker(operationInvoker);
        }

        [TestMethod]
        public async Task SubmitOperationInvoker()
        {
            var submit = new ReflectionDomainServiceDescriptionProvider.ReflectionDomainOperationEntry(DomainServiceDescription.DomainServiceType,
                typeof(DomainService).GetMethod(nameof(DomainService.SubmitAsync)), DomainOperation.Custom);

            var serializationHelper = GetSerializationHelper();
            var operationInvoker = new SubmitOperationInvoker(submit, serializationHelper);

            var bytes = GetEmptyChangeSet(Array.Empty<ChangeSetEntry>());

            var context = GetHttpContext();
            context.Request.Method = "POST";
            context.Request.Body = bytes;

            var cts = new CancellationTokenSource();
            context.RequestAborted = cts.Token;

            cts.Cancel();
            await operationInvoker.Invoke(context);

            Assert.IsTrue(context.Response.ContentLength is not null, "A response should have been written");

            await TestCancelOperationInvoker(operationInvoker);
        }

        private MemoryStream GetEmptyChangeSet(IEnumerable<ChangeSetEntry> changeSet)
        {
            var domainServiceDescription = OpenRiaServices.Server.DomainServiceDescription.GetDescription(typeof(Cities.CityDomainService));

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
