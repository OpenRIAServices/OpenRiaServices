using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        private static readonly OpenRiaServicesOptions s_options = new();

        [TestMethod]
        [Description("Invoke operation: Missing parameter when calling InvokeOperationInvoker should throw BadHttpRequestException when invoked directly")]
        public async Task TestInvoke_MissingParameter_InvokeThrows()
        {
            var desc = DomainServiceDescription.GetDescription(typeof(BadRequestDomainService));
            var operation = desc.GetInvokeOperation(nameof(BadRequestDomainService.RoundTrippInt));

            var operationInvoker = new InvokeOperationInvoker(operation, new SerializationHelper(desc), s_options);

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
            Assert.IsNotNull(context.Response.StatusCode == 200);

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
            //return null;
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

            var response = await client.GetAsync("QueryDouble?value=1.0");
            response.EnsureSuccessStatusCode();

            await AssertBadRequestAsync(client.GetAsync($"QueryDouble"));
            await AssertBadRequestAsync(client.GetAsync($"QueryDouble?value=null"));
            await AssertBadRequestAsync(client.GetAsync($"QueryDouble?value="));
            await AssertBadRequestAsync(client.GetAsync($"QueryDouble?value=one"));
            await AssertBadRequestAsync(client.GetAsync($"QueryDouble?value=1..2"));
        }

        private static async Task AssertBadRequestAsync(Task<System.Net.Http.HttpResponseMessage> responseTask)
        {
            try
            {
                var response = await responseTask;
                Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
            }
            catch (BadHttpRequestException)
            {
                // Testserver will not catch BadHttpRequestException and convert it to Http status code 400
                // so we catch the exception
            }
        }

        [EnableClientAccess]
        public class BadRequestDomainService : DomainService
        {
            [Invoke(HasSideEffects = false)]
            public int RoundTrippInt(int value) => value;

            [Query(HasSideEffects = false, IsComposable = false)]
            public People.Person QueryDouble(double value) => new People.Person() { Name = value.ToString() };
        }
    }
}
