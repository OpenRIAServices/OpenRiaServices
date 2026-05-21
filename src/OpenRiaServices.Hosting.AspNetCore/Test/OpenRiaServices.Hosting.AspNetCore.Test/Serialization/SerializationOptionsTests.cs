using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Hosting.AspNetCore.Serialization;
using OpenRiaServices.Server;

namespace OpenRiaServices.Hosting.AspNetCore;

[TestClass]
public class SerializationOptionsTests
{
    // -------------------------------------------------------------------------
    // Builder API tests
    // -------------------------------------------------------------------------

    [TestMethod]
    [Description("ConfigureBinarySerialization replaces existing BinaryXmlSerializationProvider with configured one")]
    public void Builder_ConfigureBinarySerialization_ReplacesDefaultBinaryProvider()
    {
        bool configureWasCalled = false;
        var services = new ServiceCollection();
        var builder = services.AddOpenRiaServices();
        builder.ConfigureBinarySerialization(opts =>
        {
            configureWasCalled = true;
            opts.ReaderQuotas = new XmlDictionaryReaderQuotas { MaxStringContentLength = 2048 };
        });

        var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<OpenRiaServicesOptions>>().Value;

        Assert.IsTrue(configureWasCalled, "Configure callback should have been called");

        int binaryCount = options.SerializationProviders.Count(p => p is BinaryXmlSerializationProvider);
        Assert.AreEqual(1, binaryCount, "Should still have exactly one BinaryXmlSerializationProvider");
    }

    [TestMethod]
    [Description("ConfigureBinarySerialization can be fluently chained")]
    public void Builder_ConfigureBinarySerialization_ReturnsSameBuilder()
    {
        var services = new ServiceCollection();
        var builder = services.AddOpenRiaServices();
        var returned = builder.ConfigureBinarySerialization(_ => { });
        Assert.AreSame(builder, returned, "ConfigureBinarySerialization should return the builder for chaining");
    }

    [TestMethod]
    [Description("AddXmlSerialization with configure returns the builder for chaining")]
    public void Builder_AddXmlSerialization_WithConfigure_ReturnsSameBuilder()
    {
        var services = new ServiceCollection();
        var builder = services.AddOpenRiaServices();
        var returned = builder.AddXmlSerialization(_ => { });
        Assert.AreSame(builder, returned, "AddXmlSerialization should return the builder for chaining");
    }

    // -------------------------------------------------------------------------
    // Reader quota enforcement tests (binary)
    // -------------------------------------------------------------------------

    [TestMethod]
    [Description("Binary reader quota: MaxStringContentLength is enforced")]
    public async Task BinaryReaderQuota_MaxStringContentLength_IsEnforced()
    {
        using var host = await CreateHost(binaryConfigure: opts =>
        {
            opts.ReaderQuotas = new XmlDictionaryReaderQuotas { MaxStringContentLength = 10 };
        });

        var client = host.GetTestClient();

        // A request with a string param longer than 10 chars should fail
        var longString = new string('a', 100);
        using var tooLongContent = BuildBinaryInvokeRequest("EchoString", "value", longString);
        await AssertBadRequestAsync(client.PostAsync("SerializationTestDomainService/EchoString", tooLongContent), "The maximum string content length quota (10) has been exceeded");

        using var shortContent = BuildBinaryInvokeRequest("EchoString", "value", "hello");
        var response = await client.PostAsync("SerializationTestDomainService/EchoString", shortContent);
        Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode,
            "Request within quota should succeed");
    }

    // -------------------------------------------------------------------------
    // Reader quota enforcement tests (XML)
    // -------------------------------------------------------------------------

    [TestMethod]
    [Description("XML reader quota: MaxStringContentLength is enforced")]
    public async Task XmlReaderQuota_MaxStringContentLength_IsEnforced()
    {
        using var host = await CreateHost(xmlConfigure: opts =>
        {
            opts.ReaderQuotas = new XmlDictionaryReaderQuotas { MaxStringContentLength = 10 };
        });

        var client = host.GetTestClient();
        var longString = new string('a', 100);
        using var content = BuildXmlInvokeRequest("EchoString", "value", longString);

        await AssertBadRequestAsync(client.PostAsync("SerializationTestDomainService/EchoString", content), "The maximum string content length quota (10) has been exceeded");
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static async Task<IHost> CreateHost(
        Action<BinaryDataContractSerializerOptions> binaryConfigure = null,
        Action<XmlDataContractSerializerOptions> xmlConfigure = null)
    {
        return await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddRouting();
                        var oria = services.AddOpenRiaServices();
                        if (binaryConfigure is not null)
                            oria.ConfigureBinarySerialization(binaryConfigure);
                        if (xmlConfigure is not null)
                            oria.AddXmlSerialization(xmlConfigure);
                        services.AddDomainService<SerializationTestDomainService>();
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(e =>
                        {
                            e.MapOpenRiaServices(b => b.AddRegisteredDomainServices());
                        });
                    });
            })
            .StartAsync();
    }

    /// <summary>Asserts that a request results in HTTP 400 Bad Request (handles TestServer propagating the BadHttpRequestException).</summary>
    private static async Task AssertBadRequestAsync(Task<HttpResponseMessage> responseTask, string messageContents = null)
    {
        try
        {
            var response = await responseTask;
            Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, response.StatusCode,
                "Expected HTTP 400 Bad Request");
        }
        catch (BadHttpRequestException ex)
        {
            // TestServer propagates BadHttpRequestException directly instead of converting it to HTTP 400
            Assert.AreEqual(StatusCodes.Status400BadRequest, ex.StatusCode,
                "Expected BadHttpRequestException with status 400");

            if (messageContents is not null)
                Assert.Contains(messageContents, ex.Message);
        }
    }

    /// <summary>Builds a binary-XML POST body for an invoke operation with one string parameter.</summary>
    private static ByteArrayContent BuildBinaryInvokeRequest(string operationName, string paramName, string paramValue)
    {
        using var ms = new MemoryStream();
        using var writer = XmlDictionaryWriter.CreateBinaryWriter(ms, dictionary: null, session: null, ownsStream: false);
        writer.WriteStartElement(operationName);
        writer.WriteStartElement(paramName);
        writer.WriteString(paramValue);
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.Flush();
        var content = new ByteArrayContent(ms.ToArray());
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/msbin1");
        return content;
    }

    /// <summary>Builds a plain-XML POST body for an invoke operation with one string parameter.</summary>
    private static ByteArrayContent BuildXmlInvokeRequest(string operationName, string paramName, string paramValue)
    {
        using var ms = new MemoryStream();
        var settings = new XmlWriterSettings { Encoding = new UTF8Encoding(false), CloseOutput = false };
        using var writer = XmlWriter.Create(ms, settings);
        writer.WriteStartElement(operationName);
        writer.WriteStartElement(paramName);
        writer.WriteString(paramValue);
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.Flush();
        var content = new ByteArrayContent(ms.ToArray());
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/xml");
        return content;
    }

    // -------------------------------------------------------------------------
    // Test domain service
    // -------------------------------------------------------------------------

    [EnableClientAccess]
    public class SerializationTestDomainService : DomainService
    {
        [Invoke(HasSideEffects = true)]
        public string EchoString(string value) => value;
    }
}
