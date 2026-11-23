using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nerdbank.MessagePack;
using OpenRiaServices.Client.DomainClients;
using PolyType;

namespace OpenRiaServices.Client.Test
{
    [TestClass]
    public partial class HttpDomainClientTests
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

        [TestMethod]
        [DataRow("echoed")]
        [DataRow(null)]
        public async Task MessagePackReadsV1Result(string expected)
        {
            var bytes = new ArrayBufferWriter<byte>();
            var writer = new MessagePackWriter(bytes);
            writer.WriteMapHeader(1);
            writer.Write("Result");
            writer.Write(expected);
            writer.Flush();

            var result = await InvokeWithMessagePackResponseAsync(bytes.WrittenMemory, typeof(string), HttpStatusCode.OK);

            Assert.AreEqual(expected, result.ReturnValue);
        }

        [TestMethod]
        [DataRow("Metadata")]
        [DataRow("v")]
        public async Task MessagePackReadsResultWithUnknownMember(string member)
        {
            var bytes = new ArrayBufferWriter<byte>();
            var writer = new MessagePackWriter(bytes);
            writer.WriteMapHeader(2);
            writer.Write(member);
            if (member == "Metadata")
            {
                writer.WriteArrayHeader(2);
                writer.Write(42);
                writer.Write("ignored");
            }
            else
            {
                writer.Write(2);
            }
            writer.Write("Result");
            writer.Write("echoed");
            writer.Flush();

            var result = await InvokeWithMessagePackResponseAsync(bytes.WrittenMemory, typeof(string), HttpStatusCode.OK);

            Assert.AreEqual("echoed", result.ReturnValue);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task MessagePackReadsVoidResult(bool omitResult)
        {
            var bytes = new ArrayBufferWriter<byte>();
            var writer = new MessagePackWriter(bytes);
            writer.WriteMapHeader(omitResult ? 0 : 1);
            if (!omitResult)
            {
                writer.Write("Result");
                writer.WriteNil();
            }
            writer.Flush();

            var result = await InvokeWithMessagePackResponseAsync(bytes.WrittenMemory, typeof(void), HttpStatusCode.OK);

            Assert.IsNull(result.ReturnValue);
        }

        [TestMethod]
        public async Task MessagePackReadsFragmentedFaultOnHttpError()
        {
            var bytes = new ArrayBufferWriter<byte>();
            var writer = new MessagePackWriter(bytes);
            writer.WriteMapHeader(1);
            writer.Write("Fault");
            writer.WriteMapHeader(2);
            writer.Write("ErrorCode");
            writer.Write(403);
            writer.Write("ErrorMessage");
            writer.Write("Access denied by the service.");
            writer.Flush();

            var error = await Assert.ThrowsAsync<DomainOperationException>(
                () => InvokeWithMessagePackResponseAsync(bytes.WrittenMemory, typeof(string), HttpStatusCode.Forbidden));

            Assert.AreEqual(OperationErrorStatus.Unauthorized, error.Status);
            Assert.AreEqual(403, error.ErrorCode);
            Assert.AreEqual("Access denied by the service.", error.Message);
        }

        private static async Task<InvokeCompletedResult> InvokeWithMessagePackResponseAsync(ReadOnlyMemory<byte> body, Type returnType, HttpStatusCode status)
        {
            using var handler = new MessagePackResponseHandler(body, status);
            using var httpClient = new HttpClient(handler);
            var serviceUri = new Uri("https://localhost/TestService/");
            var factory = new MessagePackHttpDomainClientFactory(serviceUri, _ => httpClient,
                typeShapeProvider: MessagePackResponseTestShapes.GeneratedTypeShapeProvider);
            var client = factory.CreateDomainClient(typeof(ITestServiceContract), serviceUri, false);
            client.EntityTypes = Array.Empty<Type>();
            return await client.InvokeAsync(new InvokeArgs("Echo", returnType, new Dictionary<string, object>(), hasSideEffects: false), CancellationToken.None);
        }

        private sealed class MessagePackResponseHandler(ReadOnlyMemory<byte> body, HttpStatusCode status) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var content = new StreamContent(new FragmentedStream(body.ToArray()));
                content.Headers.ContentType = new("application/vnd.msgpack");
                return Task.FromResult(new HttpResponseMessage(status) { Content = content });
            }
        }

        private sealed class FragmentedStream(byte[] bytes) : MemoryStream(bytes)
        {
            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
                => base.ReadAsync(buffer[..Math.Min(buffer.Length, 1)], cancellationToken);
        }

        public enum DomainClientType
        {
            Binary,
            Xml,
            MessagePack
        }

        public interface ITestServiceContract
        {
            IAsyncResult BeginEcho(string value, AsyncCallback callback, object asyncState);
            string EndEcho(IAsyncResult result);
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

    [GenerateShapeFor<string>]
    partial class MessagePackResponseTestShapes;
    }
}
