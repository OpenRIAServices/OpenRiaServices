extern alias httpDomainClient;

using Nerdbank.MessagePack;
using SimpleStructs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using httpDomainClient::******.Client.DomainClients;

namespace ******.Client.Test
{
    [TestClass]
    public class MessagePackHttpDomainClientFactoryTests
    {
        [TestMethod]
        public async Task BuffersResponsesByDefault()
        {
            using var httpHandler = new ResponseWrappingHttpHandler(new HttpClientHandler(), ResponseReadMode.BufferOnly);
            var factory = CreateFactory(httpHandler);

            Assert.IsTrue(factory.BufferResponseContent);

            InvokeCompletedResult result = await CreateDomainClient(factory).InvokeAsync(
                new InvokeArgs(
                    "RoundtripSimpleStruct",
                    typeof(SimpleStruct),
                    new Dictionary<string, object> { { "value", new SimpleStruct(42) } },
                    hasSideEffects: false),
                CancellationToken.None);

            Assert.AreEqual(new SimpleStruct(42), result.ReturnValue);
        }

        [TestMethod]
        public async Task CanDisableBufferedResponses()
        {
            using var httpHandler = new ResponseWrappingHttpHandler(new HttpClientHandler(), ResponseReadMode.StreamOnly);
            var factory = CreateFactory(httpHandler)
            {
                BufferResponseContent = false,
            };

            InvokeCompletedResult result = await CreateDomainClient(factory).InvokeAsync(
                new InvokeArgs(
                    "RoundtripSimpleStruct",
                    typeof(SimpleStruct),
                    new Dictionary<string, object> { { "value", new SimpleStruct(42) } },
                    hasSideEffects: false),
                CancellationToken.None);

            Assert.AreEqual(new SimpleStruct(42), result.ReturnValue);
        }

        private static DomainClient CreateDomainClient(MessagePackHttpDomainClientFactory factory)
            => factory.CreateDomainClient(
                typeof(SimpleStructDomainContext.ISimpleStructDomainServiceContract),
                new Uri("SimpleStructs-SimpleStructDomainService", UriKind.Relative),
                false);

        private static MessagePackHttpDomainClientFactory CreateFactory(HttpMessageHandler handler)
            => new(
                TestURIs.RootURI,
                uri =>
                {
                    var httpClient = new HttpClient(handler, disposeHandler: false);

                    const string toRemove = ".svc/binary/";
                    string uriString = uri.AbsoluteUri;
                    if (uriString.EndsWith(toRemove, StringComparison.Ordinal))
                    {
                        uri = new Uri(uriString.Remove(uriString.Length - toRemove.Length));
                    }

                    httpClient.BaseAddress = uri;
                    return httpClient;
                },
                new MessagePackSerializer());

        private enum ResponseReadMode
        {
            BufferOnly,
            StreamOnly,
        }

        private sealed class ResponseWrappingHttpHandler : DelegatingHandler
        {
            private readonly ResponseReadMode _mode;

            public ResponseWrappingHttpHandler(HttpMessageHandler innerHandler, ResponseReadMode mode)
                : base(innerHandler)
            {
                _mode = mode;
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
                response.Content = new ResponseGuardContent(await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false), response.Content.Headers, _mode);
                return response;
            }
        }

        private sealed class ResponseGuardContent : HttpContent
        {
            private readonly byte[] _payload;
            private readonly ResponseReadMode _mode;

            public ResponseGuardContent(byte[] payload, HttpContentHeaders headers, ResponseReadMode mode)
            {
                _payload = payload;
                _mode = mode;

                foreach (var header in headers)
                {
                    Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
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
