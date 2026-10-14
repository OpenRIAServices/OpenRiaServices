using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Hosting.AspNetCore;
using OpenRiaServices.Hosting.AspNetCore.Operations;
using OpenRiaServices.Server;
using TestDomainServices;

[assembly: DomainServiceEndpointRoutePattern(EndpointRoutePattern.Name)]

namespace OpenRiaServices.Hosting.AspNetCore
{
    [TestClass]
    public class BadRequestTests
    {
        private static readonly OpenRiaServicesOptions s_options = new() { SerializationProviders = [new Serialization.BinaryXmlSerializationProvider()] };

        [TestMethod]
        [Description("Invoke operation: Missing parameter when calling InvokeOperationInvoker should throw BadHttpRequestException when invoked directly")]
        public async Task TestInvoke_MissingParameter_InvokeThrows()
        {
            var desc = DomainServiceDescription.GetDescription(typeof(BadRequestDomainService));
            var operation = desc.GetInvokeOperation(nameof(BadRequestDomainService.InvokeIntParam));

            var operationInvoker = new InvokeOperationInvoker(operation, s_options);

            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            // register the domain service instance so CreateDomainService can resolve it
            var services = new ServiceCollection();
            services.AddTransient<BadRequestDomainService>();
            context.RequestServices = services.BuildServiceProvider();
            context.Request.Method = "GET";
            context.Request.QueryString = QueryString.FromUriComponent("?value=2");

            // Works if value is set
            await operationInvoker.Invoke(context);
            Assert.AreEqual(StatusCodes.Status200OK, context.Response.StatusCode);

            // Missing value (empty string, or parameter name value)
            context.Request.QueryString = QueryString.FromUriComponent("?dummy=2");
            var missingParamEx = await Assert.ThrowsExactlyAsync<BadHttpRequestException>(async () => await operationInvoker.Invoke(context));
            Assert.IsNull(missingParamEx.InnerException);

            context.Request.QueryString = QueryString.FromUriComponent("?value=");
            await Assert.ThrowsExactlyAsync<BadHttpRequestException>(async () => await operationInvoker.Invoke(context));

            // Wrong format
            context.Request.QueryString = QueryString.FromUriComponent("?value=two");
            var invalidFormatEx = await Assert.ThrowsExactlyAsync<BadHttpRequestException>(async () => await operationInvoker.Invoke(context));
            Assert.IsInstanceOfType<FormatException>(invalidFormatEx.InnerException);

            // Wrong format
            context.Request.QueryString = QueryString.FromUriComponent("?value=2.0");
            invalidFormatEx = await Assert.ThrowsExactlyAsync<BadHttpRequestException>(async () => await operationInvoker.Invoke(context));
            Assert.IsInstanceOfType<FormatException>(invalidFormatEx.InnerException);
        }

        [TestMethod]
        public async Task TestServer_Query_Get_InvalidParameters()
        {
            using var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddRouting();
                        services.AddOpenRiaServices(o =>
                        {
                        });
                        services.AddDomainService<BadRequestDomainService>();
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(e =>
                        {
                            e.MapOpenRiaServices(builder =>
                            {
                                // Add all domainservices registered in the container
                                builder.AddRegisteredDomainServices();
                            });
                        });
                    });
            })
            .StartAsync();

            var client = host.GetTestClient();
            client.BaseAddress = new Uri(client.BaseAddress, "BadRequestDomainService/");

            var response = await client.GetAsync("QueryWithDoubleParam?value=1.0");
            response.EnsureSuccessStatusCode();

            await AssertBadRequestAsync(client.GetAsync($"QueryWithDoubleParam"));
            await AssertBadRequestAsync(client.GetAsync($"QueryWithDoubleParam?value=null"));
            await AssertBadRequestAsync(client.GetAsync($"QueryWithDoubleParam?value="));
            await AssertBadRequestAsync(client.GetAsync($"QueryWithDoubleParam?value=one"));
            await AssertBadRequestAsync(client.GetAsync($"QueryWithDoubleParam?value=1..2"));
        }

        [TestMethod]
        public async Task TestQuery_Post_NonComposableIgnoresQueryOptions()
        {
            var desc = DomainServiceDescription.GetDescription(typeof(BadRequestDomainService));
            var operation = desc.GetQueryMethod(nameof(BadRequestDomainService.QueryWithDoubleParam));
            var operationInvoker = new QueryOperationInvoker<People.Person>(operation, s_options);

            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            context.RequestServices = new ServiceCollection()
                .AddTransient<BadRequestDomainService>()
                .BuildServiceProvider();
            context.Request.Method = "POST";
            context.Request.ContentType = "application/msbin1";
            context.Request.Body = GetQueryRequestWithInvalidOptions();

            await operationInvoker.Invoke(context);

            Assert.AreEqual(StatusCodes.Status200OK, context.Response.StatusCode);
        }

        private static MemoryStream GetQueryRequestWithInvalidOptions()
            => GetQueryRequest(nameof(BadRequestDomainService.QueryWithDoubleParam), "(", includeValue: true);

        [TestMethod]
        public void TestQuery_DepthOptionRejectsNonPositiveValues()
        {
            var options = new OpenRiaServicesOptions();
            Assert.AreEqual(200, options.MaxQueryExpressionDepth);
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => options.MaxQueryExpressionDepth = 0);
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => options.MaxQueryExpressionDepth = -1);
        }

        [TestMethod]
        [DataRow("GET")]
        [DataRow("POST")]
        [DataRow("QUERY")]
        public async Task TestQuery_ConfiguredDepthIsPerHost(string method)
        {
            var operation = DomainServiceDescription.GetDescription(typeof(BadRequestDomainService))
                .GetQueryMethod(nameof(BadRequestDomainService.GetPeople));
            using var services = new ServiceCollection().AddTransient<BadRequestDomainService>().BuildServiceProvider();
            string expression = new string('(', 10) + "true" + new string(')', 10);
            foreach (int depth in new[] { 10, 20 })
            {
                var options = new OpenRiaServicesOptions
                {
                    MaxQueryExpressionDepth = depth,
                    SerializationProviders = s_options.SerializationProviders,
                };
                var invoker = new QueryOperationInvoker<People.Person>(operation, options);
                var context = new DefaultHttpContext();
                context.RequestServices = services;
                context.Response.Body = new MemoryStream();
                context.Request.Method = method;
                if (method == "GET")
                    context.Request.QueryString = new QueryString("?$where=" + Uri.EscapeDataString(expression));
                else
                {
                    context.Request.ContentType = "application/msbin1";
                    context.Request.Body = GetQueryRequest(nameof(BadRequestDomainService.GetPeople), expression);
                }
                await invoker.Invoke(context);
                Assert.AreEqual(depth == 10 ? StatusCodes.Status400BadRequest : StatusCodes.Status200OK,
                    context.Response.StatusCode);
            }
        }

        [TestMethod]
        [DataRow("GET")]
        [DataRow("POST")]
        [DataRow("QUERY")]
        public async Task TestQuery_InvalidExpressionsReturnBadRequestAndSubsequentQuerySucceeds(string method)
        {
            var description = DomainServiceDescription.GetDescription(typeof(BadRequestDomainService));
            var operation = description.GetQueryMethod(nameof(BadRequestDomainService.GetPeople));
            var invoker = new QueryOperationInvoker<People.Person>(operation, s_options);
            using var services = new ServiceCollection().AddTransient<BadRequestDomainService>().BuildServiceProvider();

            var expressions = new[]
            {
                "Name ==",
                new string('(', 5000) + "true" + new string(')', 5000),
                string.Join(" and ", Enumerable.Repeat("true", 1000)),
                "Name.Replace(\"a\",\"aaa\").Replace(\"a\",\"aaa\").Replace(\"a\",\"aaa\").Replace(\"a\",\"aaa\").Length > 0",
                "Name.Replace(\"a\",\"aaaaaaaaaaa\").Replace(\"a\",\"aaaaaaaaaaa\").Length > 0",
            };
            foreach (var expression in expressions)
            {
                var context = new DefaultHttpContext();
                context.RequestServices = services;
                context.Response.Body = new MemoryStream();
                context.Request.Method = method;
                if (method == "GET")
                    context.Request.QueryString = new QueryString("?$where=" + Uri.EscapeDataString(expression));
                else
                {
                    context.Request.ContentType = "application/msbin1";
                    context.Request.Body = GetQueryRequest(nameof(BadRequestDomainService.GetPeople), expression);
                }

                await invoker.Invoke(context);
                Assert.AreEqual(StatusCodes.Status400BadRequest, context.Response.StatusCode);
                Assert.IsTrue(context.Response.Body.Length > 0, "A fault must be returned.");
            }

            var subsequent = new DefaultHttpContext();
            subsequent.RequestServices = services;
            subsequent.Response.Body = new MemoryStream();
            subsequent.Request.Method = "GET";
            await invoker.Invoke(subsequent);
            Assert.AreEqual(StatusCodes.Status200OK, subsequent.Response.StatusCode);
        }

        private static MemoryStream GetQueryRequest(string operation, string expression, bool includeValue = false)
        {
            var requestBody = new MemoryStream();
            using (var writer = XmlDictionaryWriter.CreateBinaryWriter(requestBody, null, null, ownsStream: false))
            {
                writer.WriteStartElement("MessageRoot");
                writer.WriteStartElement("QueryOptions");
                writer.WriteStartElement("QueryOption");
                writer.WriteAttributeString("Name", "where");
                writer.WriteAttributeString("Value", expression);
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteStartElement(operation);
                if (includeValue)
                {
                    writer.WriteStartElement("value");
                    new DataContractSerializer(typeof(double)).WriteObjectContent(writer, 1d);
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
                writer.WriteEndElement();
            }

            requestBody.Position = 0;
            return requestBody;
        }

        [TestMethod]
        [DataRow("GET")]
        [DataRow("POST")]
        [DataRow("QUERY")]
        public async Task TestQuery_AuthorizationPrecedesParsing(string method)
        {
            var operation = DomainServiceDescription.GetDescription(typeof(BadRequestDomainService))
                .GetQueryMethod(nameof(BadRequestDomainService.GetProtectedPeople));
            var invoker = new QueryOperationInvoker<People.Person>(operation, s_options);
            foreach (bool authenticated in new[] { false, true })
            {
                using var service = new BadRequestDomainService();
                using var services = new ServiceCollection().AddSingleton(service).BuildServiceProvider();
                var context = new DefaultHttpContext();
                context.RequestServices = services;
                context.Response.Body = new MemoryStream();
                context.Request.Method = method;
                if (authenticated)
                    context.User = new ClaimsPrincipal(new ClaimsIdentity(Array.Empty<Claim>(), "test"));
                string expression = new string('(', 5000) + "true" + new string(')', 5000);
                if (method == "GET")
                    context.Request.QueryString = new QueryString("?$where=" + Uri.EscapeDataString(expression));
                else
                {
                    context.Request.ContentType = "application/msbin1";
                    context.Request.Body = GetQueryRequest(nameof(BadRequestDomainService.GetProtectedPeople), expression);
                }

                await invoker.Invoke(context);
                Assert.AreEqual(authenticated ? StatusCodes.Status400BadRequest : StatusCodes.Status401Unauthorized,
                    context.Response.StatusCode);
                if (authenticated)
                    Assert.IsInstanceOfType<System.Linq.Dynamic.ParseException>(service.LastError);
                else
                    Assert.IsInstanceOfType<UnauthorizedAccessException>(service.LastError);
                Assert.IsFalse(service.ProtectedQueryInvoked);
            }
        }

        private static async Task AssertBadRequestAsync(Task<System.Net.Http.HttpResponseMessage> responseTask)
        {
            try
            {
                var response = await responseTask;
                Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
            }
            catch (BadHttpRequestException ex)
            {
                // Testserver will not catch BadHttpRequestException and convert it to Http status code 400
                // so we catch the exception
                Assert.AreEqual(StatusCodes.Status400BadRequest, ex.StatusCode, "Expected BadHttpRequestException with status 400");
            }
        }

        [EnableClientAccess]
        public class BadRequestDomainService : DomainService
        {
            public IQueryable<People.Person> GetPeople() => Array.Empty<People.Person>().AsQueryable();

            public bool ProtectedQueryInvoked { get; private set; }
            public Exception LastError { get; private set; }

            [RequiresAuthentication]
            public IQueryable<People.Person> GetProtectedPeople()
            {
                ProtectedQueryInvoked = true;
                return GetPeople();
            }

            protected override void OnError(DomainServiceErrorInfo errorInfo)
            {
                LastError = errorInfo.Error;
                base.OnError(errorInfo);
            }

            [Invoke(HasSideEffects = false)]
            public int InvokeIntParam(int value) => value;

            [Query(HasSideEffects = false, IsComposable = false)]
            public People.Person QueryWithDoubleParam(double value) => new People.Person() { Name = value.ToString() };
        }
    }
}
