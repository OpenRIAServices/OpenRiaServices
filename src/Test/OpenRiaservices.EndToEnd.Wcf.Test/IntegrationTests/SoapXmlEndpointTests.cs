using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.ServiceModel.Description;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenRiaServices.Client.Test.IntegrationTests
{
    [TestClass]
    public class SoapXmlEndpointTests
    {
        [TestMethod]
        public async Task WsdlEnpointShouldGenerateValidWSDL()
        {
            // AuthenticationService1
            using var httpClient = new HttpClient() { BaseAddress = TestURIs.RootURI };
            var response = await httpClient.GetAsync(TestURIs.AuthenticationService1 + "?singleWsdl");
            response.EnsureSuccessStatusCode();
            using var contentStream = await response.Content.ReadAsStreamAsync();

            // Parse metadata in order to validate it
            var description = System.Web.Services.Description.ServiceDescription.Read(contentStream);
            Assert.AreEqual("AuthenticationService1", description.Name);
            Assert.AreEqual(1, description.Services.Count);
            Assert.AreEqual(12, description.Messages.Count);
        }
    }
}
