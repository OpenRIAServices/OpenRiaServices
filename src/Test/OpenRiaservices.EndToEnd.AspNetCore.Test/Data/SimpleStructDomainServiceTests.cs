extern alias httpDomainClient;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using httpDomainClient::OpenRiaServices.Client.DomainClients;
using SimpleStructs;

namespace OpenRiaServices.Client.Test
{
    [TestClass]
    public class SimpleStructDomainServiceTests
    {
        [TestMethod]
        public async Task RoundtripsSimpleStructs()
        {
            SimpleStructDomainContext domainContext = new SimpleStructDomainContext();
            SimpleStruct simple = new(42);
            CompositeSimpleStruct composite = new(7, Guid.NewGuid());

            InvokeResult<SimpleStruct> simpleResult = await domainContext.RoundtripSimpleStructAsync(simple, CancellationToken.None);
            InvokeResult<CompositeSimpleStruct> compositeResult = await domainContext.RoundtripCompositeSimpleStructAsync(composite, CancellationToken.None);

            Assert.AreEqual(simple, simpleResult.Value);
            Assert.AreEqual(composite, compositeResult.Value);
        }

        [TestMethod]
        public async Task RoundtripsSimpleStructCollections()
        {
            SimpleStructDomainContext domainContext = new SimpleStructDomainContext();
            SimpleStruct[] simple = [new(1), new(2)];
            CompositeSimpleStruct[] composite = [new(3, Guid.NewGuid()), new(4, Guid.NewGuid())];

            InvokeResult<System.Collections.Generic.IEnumerable<SimpleStruct>> simpleResult =
                await domainContext.RoundtripSimpleStructCollectionAsync(simple, CancellationToken.None);
            InvokeResult<System.Collections.Generic.IEnumerable<CompositeSimpleStruct>> compositeResult =
                await domainContext.RoundtripCompositeSimpleStructCollectionAsync(composite, CancellationToken.None);

            CollectionAssert.AreEqual(simple, simpleResult.Value.ToArray());
            CollectionAssert.AreEqual(composite, compositeResult.Value.ToArray());
        }

        [TestMethod]
        public async Task LoadsEntityWithSimpleStructKeyAndProperty()
        {
            SimpleStructDomainContext domainContext = new SimpleStructDomainContext();

            LoadResult<SimpleStructEntity> result =
                await domainContext.LoadAsync(domainContext.GetSimpleStructEntitiesQuery());

            SimpleStructEntity entity = result.Single();
            Assert.AreEqual(new SimpleStruct(1), entity.Id);
            Assert.AreEqual(
                new CompositeSimpleStruct(2, new Guid("207b1d8f-1f78-4f35-b262-b2787e1289f1")),
                entity.CompositeValue);
            Assert.AreSame(entity, domainContext.SimpleStructEntities.Single());
        }

        [TestMethod]
        public async Task RoundtripsSimpleStructsUsingGet()
        {
            using var httpHandler = new SimpleStructRecordingHttpHandler(new HttpClientHandler());
            var domainClient = new BinaryHttpDomainClientFactory(TestURIs.RootURI, httpHandler)
                .CreateDomainClient(
                    typeof(SimpleStructDomainContext.ISimpleStructDomainServiceContract),
                    new Uri("SimpleStructs-SimpleStructDomainService", UriKind.Relative),
                    false);
            SimpleStruct simple = new(42);
            CompositeSimpleStruct composite = new(7, Guid.NewGuid());

            InvokeCompletedResult simpleResult = await domainClient.InvokeAsync(
                new InvokeArgs(
                    "RoundtripSimpleStruct",
                    typeof(SimpleStruct),
                    new Dictionary<string, object> { { "value", simple } },
                    hasSideEffects: false),
                CancellationToken.None);
            InvokeCompletedResult compositeResult = await domainClient.InvokeAsync(
                new InvokeArgs(
                    "RoundtripCompositeSimpleStruct",
                    typeof(CompositeSimpleStruct),
                    new Dictionary<string, object> { { "value", composite } },
                    hasSideEffects: false),
                CancellationToken.None);

            Assert.AreEqual(simple, simpleResult.ReturnValue);
            Assert.AreEqual(composite, compositeResult.ReturnValue);
            Assert.IsTrue(httpHandler.Requests.All(r => r.Method == HttpMethod.Get));
            Assert.IsTrue(httpHandler.Requests.All(r => r.RequestUri.Query.Contains("value=", StringComparison.Ordinal)));
        }

        private sealed class SimpleStructRecordingHttpHandler : DelegatingHandler
        {
            public SimpleStructRecordingHttpHandler(HttpMessageHandler innerHandler)
                : base(innerHandler)
            {
            }

            public List<HttpRequestMessage> Requests { get; } = [];

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Requests.Add(request);
                return base.SendAsync(request, cancellationToken);
            }
        }
    }
}
