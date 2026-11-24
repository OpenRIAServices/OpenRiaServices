using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Client.DomainClients;
using OpenRiaServices.Client.DomainClients.Http;

namespace OpenRiaServices.Client.Test
{
    [TestClass]
    public class HttpDomainClientTests
    {
        [TestMethod]
        [DataRow(DomainClientType.Binary, false)]
        [DataRow(DomainClientType.Binary, true)]
        [DataRow(DomainClientType.Xml, false)]
        [DataRow(DomainClientType.Xml, true)]
        [DataRow(DomainClientType.MessagePack, false)]
        [DataRow(DomainClientType.MessagePack, true)]
        public async Task RequestsUseHttpClientVersionSettings(DomainClientType clientType, bool useQuery)
        {
            using var handler = new RequestCheckingHandler(useQuery ? new HttpMethod("QUERY") : HttpMethod.Post);
            using var httpClient = new HttpClient(handler)
            {
                DefaultRequestVersion = HttpVersion.Version20,
                DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
            };

            var serviceUri = new Uri("https://localhost/TestService/");
            HttpDomainClientFactory factory = clientType switch
            {
                DomainClientType.Binary => new BinaryHttpDomainClientFactory(serviceUri, _ => httpClient),
                DomainClientType.Xml => new XmlHttpDomainClientFactory(serviceUri, _ => httpClient),
                DomainClientType.MessagePack => new MessagePackHttpDomainClientFactory(serviceUri, _ => httpClient),
                _ => throw new ArgumentOutOfRangeException(nameof(clientType))
            };
            factory.UseQueryHttpMethod = useQuery;
            factory.MaxQueryStringLength = 0;

            var domainClient = factory.CreateDomainClient(typeof(ITestServiceContract), serviceUri, false);
            domainClient.EntityTypes = Array.Empty<Type>();
            var args = new InvokeArgs("Echo", typeof(string),
                new Dictionary<string, object> { { "value", "test" } }, hasSideEffects: !useQuery);

            await Assert.ThrowsAsync<DomainOperationException>(() => domainClient.InvokeAsync(args, CancellationToken.None));
            Assert.AreEqual(1, handler.RequestCount);
        }

        public enum DomainClientType
        {
            Binary,
            Xml,
            MessagePack
        }

        public interface ITestServiceContract
        {
            Task<string> Echo(string value);
            Task WithParameters(string callback, int asyncState);
            Task NoParameters();
        }

        public interface ILegacyServiceContract
        {
            IAsyncResult BeginEcho(string value, AsyncCallback callback, object asyncState);
            string EndEcho(IAsyncResult result);
        }

        [TestMethod]
        public void MethodParametersSupportsTaskAndLegacyContracts()
        {
            var taskParameters = new MethodParameters(typeof(ITestServiceContract), "WithParameters");
            Assert.AreEqual(typeof(string), taskParameters.GetTypeForMethodParameter("callback"));
            Assert.AreEqual(typeof(int), taskParameters.GetTypeForMethodParameter("asyncState"));
            var emptyParameters = new MethodParameters(typeof(ITestServiceContract), "NoParameters");
            Assert.Throws<MissingMethodException>(() => emptyParameters.GetTypeForMethodParameter("callback"));

            var legacyParameters = new MethodParameters(typeof(ILegacyServiceContract), "Echo");
            Assert.AreEqual(typeof(string), legacyParameters.GetTypeForMethodParameter("value"));
            Assert.Throws<MissingMethodException>(() => legacyParameters.GetTypeForMethodParameter("callback"));
            Assert.Throws<MissingMethodException>(() => new MethodParameters(typeof(ITestServiceContract), "Missing"));
        }

        private sealed class RequestCheckingHandler(HttpMethod method) : HttpMessageHandler
        {
            public int RequestCount { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                RequestCount++;
                Assert.AreEqual(method, request.Method);
                Assert.AreEqual(HttpVersion.Version20, request.Version);
                Assert.AreEqual(HttpVersionPolicy.RequestVersionOrHigher, request.VersionPolicy);
                Assert.IsNotNull(request.Content);

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("Test response without a serialized service result.")
                });
            }
        }
    }
}
