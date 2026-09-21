extern alias httpDomainClient;

using Nerdbank.MessagePack;
using PolyType;
using PolyType.ReflectionProvider;
using System;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using httpDomainClient::******.Client.DomainClients;

namespace ******.Client.Test
{
    [TestClass]
    public class MessagePackHttpDomainClientFactoryTests
    {
        [TestMethod]
        public async Task UsesPipeReaderByDefault()
        {
            MessagePackHttpDomainClientFactory factory = CreateFactory();
            DomainClient domainClient = CreateDomainClient(factory);
            byte[] payload = CreateInvokeResponsePayload("hello-messagepack");

            Assert.IsFalse(factory.BufferResponseContent);
            Assert.IsNull(factory.ResponsePipeReaderOptions);

            object result = await ReadResponseAsync(
                domainClient,
                CreateResponse(payload, ResponseReadMode.StreamOnly),
                "RoundtripString",
                typeof(string));

            Assert.AreEqual("hello-messagepack", result);
        }

        [TestMethod]
        public async Task CanBufferResponsesWhenEnabled()
        {
            MessagePackHttpDomainClientFactory factory = CreateFactory();
            factory.BufferResponseContent = true;
            DomainClient domainClient = CreateDomainClient(factory);
            byte[] payload = CreateInvokeResponsePayload("hello-messagepack");

            object result = await ReadResponseAsync(
                domainClient,
                CreateResponse(payload, ResponseReadMode.BufferOnly),
                "RoundtripString",
                typeof(string));

            Assert.AreEqual("hello-messagepack", result);
        }

        [TestMethod]
        public async Task CanUseCustomPipeReaderOptions()
        {
            MessagePackHttpDomainClientFactory factory = CreateFactory();
            StreamPipeReaderOptions options = new StreamPipeReaderOptions(bufferSize: 1024 * 1024, minimumReadSize: 256 * 1024, leaveOpen: true);
            factory.ResponsePipeReaderOptions = options;
            DomainClient domainClient = CreateDomainClient(factory);
            byte[] payload = CreateInvokeResponsePayload("hello-messagepack");

            object result = await ReadResponseAsync(
                domainClient,
                CreateResponse(payload, ResponseReadMode.StreamOnly),
                "RoundtripString",
                typeof(string));

            Assert.AreSame(options, factory.ResponsePipeReaderOptions);
            Assert.AreEqual("hello-messagepack", result);
        }

        private static DomainClient CreateDomainClient(MessagePackHttpDomainClientFactory factory)
        {
            DomainClient domainClient = factory.CreateDomainClient(
                typeof(SimpleStructs.SimpleStructDomainContext.ISimpleStructDomainServiceContract),
                new Uri("SimpleStructs-SimpleStructDomainService", UriKind.Relative),
                false);
            domainClient.EntityTypes = Type.EmptyTypes;
            return domainClient;
        }

        private static MessagePackHttpDomainClientFactory CreateFactory()
            => new(TestURIs.RootURI, _ => new HttpClient(new HttpClientHandler()));

        private static byte[] CreateInvokeResponsePayload(string result)
        {
            var payload = new System.Collections.Generic.Dictionary<string, string> { { "Result", result } };
            var serializer = new MessagePackSerializer();
            using var stream = new MemoryStream();
            serializer.SerializeObject(stream, payload, ReflectionTypeShapeProvider.Default.GetTypeShapeOrThrow(payload.GetType()));
            return stream.ToArray();
        }

        private static HttpResponseMessage CreateResponse(byte[] payload, ResponseReadMode mode)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ResponseGuardContent(payload, mode),
            };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.msgpack");
            return response;
        }

        private static async Task<object> ReadResponseAsync(DomainClient domainClient, HttpResponseMessage response, string operationName, Type returnType)
        {
            MethodInfo readResponseAsync = domainClient.GetType().GetMethod("ReadResponseAsync", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(readResponseAsync);

            var task = (Task<object>)readResponseAsync.Invoke(domainClient, new object[] { response, operationName, returnType });
            return await task.ConfigureAwait(false);
        }

        private enum ResponseReadMode
        {
            BufferOnly,
            StreamOnly,
        }

        private sealed class ResponseGuardContent : HttpContent
        {
            private readonly byte[] _payload;
            private readonly ResponseReadMode _mode;

            public ResponseGuardContent(byte[] payload, ResponseReadMode mode)
            {
                _payload = payload;
                _mode = mode;
            }

            protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
            {
                if (_mode == ResponseReadMode.StreamOnly)
                {
                    throw new InvalidOperationException("Buffered response reads are disabled for this test content.");
                }

                return stream.WriteAsync(_payload, 0, _payload.Length);
            }

            protected override Task<Stream> CreateContentReadStreamAsync()
            {
                if (_mode == ResponseReadMode.BufferOnly)
                {
                    throw new InvalidOperationException("Stream response reads are disabled for this test content.");
                }

                Stream stream = new MemoryStream(_payload, writable: false);
                return Task.FromResult(stream);
            }

            protected override bool TryComputeLength(out long length)
            {
                length = _payload.Length;
                return true;
            }
        }

    }
}
