using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Hosting.AspNetCore.Operations;
using OpenRiaServices.Server;
using TestDomainServices;

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
            var desc = DomainServiceDescription.GetDescription(typeof(TestProvider_Scenarios));
            var operation = desc.GetInvokeOperation(nameof(TestProvider_Scenarios.ReturnsDouble_Online));

            var operationInvoker = new InvokeOperationInvoker(operation, new SerializationHelper(desc), s_options);

            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            // register the domain service instance so CreateDomainService can resolve it
            var services = new ServiceCollection();
            services.AddTransient<TestProvider_Scenarios>();
            context.RequestServices = services.BuildServiceProvider();
            context.Request.Method = "GET";
            context.Request.QueryString = QueryString.FromUriComponent("?value=2.0");

            // Works if value is set
            await operationInvoker.Invoke(context);
            Assert.IsNotNull(context.Response.StatusCode == 200);

            // Missing value (empty string, or parameter name value)
            context.Request.QueryString = QueryString.FromUriComponent("?dummy=2.0");
            var missingParamEx = await Assert.ThrowsExactlyAsync<BadHttpRequestException>(async () => await operationInvoker.Invoke(context));
            Assert.IsNull(missingParamEx.InnerException);

            context.Request.QueryString = QueryString.FromUriComponent("?value=");
            var emptyValue = await Assert.ThrowsExactlyAsync<BadHttpRequestException>(async () => await operationInvoker.Invoke(context));
            
            // Wrong format
            context.Request.QueryString = QueryString.FromUriComponent("?value=two");
            var invalidFormatEx = await Assert.ThrowsExactlyAsync<BadHttpRequestException>(async () => await operationInvoker.Invoke(context));
            Assert.IsInstanceOfType<FormatException>(invalidFormatEx.InnerException);

            // Wrong format
            context.Request.QueryString = QueryString.FromUriComponent("?value=2...2");
            invalidFormatEx = await Assert.ThrowsExactlyAsync<BadHttpRequestException>(async () => await operationInvoker.Invoke(context));
            Assert.IsInstanceOfType<FormatException>(invalidFormatEx.InnerException);
        }
    }
}
