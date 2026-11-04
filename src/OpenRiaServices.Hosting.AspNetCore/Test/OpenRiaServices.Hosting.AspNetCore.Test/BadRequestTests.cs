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
using Nerdbank.MessagePack;
using OpenRiaServices.Hosting.AspNetCore;
using OpenRiaServices.Hosting.AspNetCore.Operations;
using OpenRiaServices.Hosting.AspNetCore.Serialization;
using OpenRiaServices.Server;
using PolyType;
using PolyType.ReflectionProvider;
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
        public void TestQuery_LimitsValidateValuesAndArePerHost()
        {
            var options = new OpenRiaServicesOptions();
            Assert.AreEqual(200, options.QueryLimits.MaxExpressionDepth);
            Assert.AreEqual(100, options.QueryLimits.MaxNumericFormatPrecision);
            Assert.AreEqual(64, options.QueryLimits.MaxNumericFormatLength);
            Assert.ThrowsExactly<ArgumentNullException>(() => options.QueryLimits = null);
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => options.QueryLimits.MaxExpressionDepth = 0);
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => options.QueryLimits.MaxExpressionDepth = -1);
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => options.QueryLimits.MaxNumericFormatPrecision = -1);
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => options.QueryLimits.MaxNumericFormatLength = 0);
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => options.QueryLimits.MaxNumericFormatLength = -1);
            options.QueryLimits.MaxExpressionDepth = 10;
            options.QueryLimits.MaxNumericFormatPrecision = 0;
            options.QueryLimits.MaxNumericFormatLength = 1;
            var other = new OpenRiaServicesOptions();
            Assert.AreEqual(200, other.QueryLimits.MaxExpressionDepth);
            Assert.AreEqual(100, other.QueryLimits.MaxNumericFormatPrecision);
            Assert.AreEqual(64, other.QueryLimits.MaxNumericFormatLength);
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
                    QueryLimits = new QueryLimits { MaxExpressionDepth = depth },
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
                "Int32(1).ToString(\"D101\") != null",
                "String.Format(\"{0:D999999999}\", Int32(1)) != null",
                "Int32(1).ToString(Name) != null",
                $"Int32(1).ToString(\"{new string('0', 65)}\") != null",
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

        [TestMethod]
        [DataRow("GET", "where")]
        [DataRow("GET", "orderby")]
        [DataRow("POST", "where")]
        [DataRow("POST", "orderby")]
        [DataRow("QUERY", "where")]
        [DataRow("QUERY", "orderby")]
        [DataRow("MessagePack", "where")]
        [DataRow("MessagePack", "orderby")]
        public async Task TestQuery_ConfiguredNumericLimitsRejectBeforeInvocation(string method, string queryOperator)
        {
            var operation = DomainServiceDescription.GetDescription(typeof(BadRequestDomainService))
                .GetQueryMethod(nameof(BadRequestDomainService.GetPeople));
            foreach (var format in new[] { "F4", "00000", "F3" })
            {
                foreach (bool restricted in new[] { true, false })
                {
                    using var service = new BadRequestDomainService();
                    using var services = new ServiceCollection().AddSingleton(service).BuildServiceProvider();
                    var options = new OpenRiaServicesOptions
                    {
                        QueryLimits = restricted
                            ? new QueryLimits { MaxNumericFormatPrecision = 3, MaxNumericFormatLength = 4 }
                            : new QueryLimits(),
                        SerializationProviders = method == "MessagePack"
                            ? [new MessagePackSerializationProvider(new MessagePackSerializationOptions())]
                            : s_options.SerializationProviders,
                    };
                    var invoker = new QueryOperationInvoker<People.Person>(operation, options);
                    string expression = $"Int32(1).ToString(\"{format}\")";
                    if (queryOperator == "where")
                        expression += " != null";
                    var context = new DefaultHttpContext();
                    context.RequestServices = services;
                    context.Response.Body = new MemoryStream();
                    context.Request.Method = method == "MessagePack" ? "POST" : method;
                    if (method == "GET")
                        context.Request.QueryString = new QueryString($"?${queryOperator}=" + Uri.EscapeDataString(expression));
                    else if (method == "MessagePack")
                    {
                        context.Request.ContentType = "application/vnd.msgpack";
                        var envelope = new
                        {
                            QueryOptions = new[] { new ServiceQueryPart(queryOperator, expression) },
                        };
                        var body = new MemoryStream();
                        new MessagePackSerializer().SerializeObject(body, envelope,
                            ReflectionTypeShapeProvider.Default.GetTypeShapeOrThrow(envelope.GetType()));
                        body.Position = 0;
                        context.Request.Body = body;
                    }
                    else
                    {
                        context.Request.ContentType = "application/msbin1";
                        context.Request.Body = GetQueryRequest(nameof(BadRequestDomainService.GetPeople), expression,
                            queryOperator: queryOperator);
                    }
                    await invoker.Invoke(context);
                    bool rejected = restricted && format != "F3";
                    Assert.AreEqual(rejected ? StatusCodes.Status400BadRequest : StatusCodes.Status200OK,
                        context.Response.StatusCode, $"{method}: {queryOperator}, {format}, restricted={restricted}");
                    if (rejected)
                        Assert.IsInstanceOfType<System.Linq.Dynamic.ParseException>(service.LastError);
                    Assert.AreEqual(!rejected, service.PeopleQueryInvoked);
                }
            }
        }

        private static MemoryStream GetQueryRequest(string operation, string expression, bool includeValue = false, string queryOperator = "where")
        {
            var requestBody = new MemoryStream();
            using (var writer = XmlDictionaryWriter.CreateBinaryWriter(requestBody, null, null, ownsStream: false))
            {
                writer.WriteStartElement("MessageRoot");
                writer.WriteStartElement("QueryOptions");
                writer.WriteStartElement("QueryOption");
                writer.WriteAttributeString("Name", queryOperator);
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
            public IQueryable<People.Person> GetPeople()
            {
                PeopleQueryInvoked = true;
                return Array.Empty<People.Person>().AsQueryable();
            }

            public bool PeopleQueryInvoked { get; private set; }
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
