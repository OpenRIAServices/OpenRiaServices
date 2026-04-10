using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Hosting.Wcf;
using OpenRiaServices.Hosting.Wcf.Tracing;
using OpenRiaServices.Server;

namespace OpenRiaServices.Hosting.Local.Test
{
    /// <summary>
    ///This is a test class for TracingDomainServiceEndpointFactoryTest and is intended
    ///to contain all TracingDomainServiceEndpointFactoryTest Unit Tests
    ///</summary>
    [TestClass()]
    public class TracingDomainServiceEndpointFactoryTest
    {
        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext { get; set; }

        /// <summary>
        ///A test for CreateEndpoints
        ///</summary>
        [TestMethod()]
        public void CreateEndpointsTest()
        {
            DomainServiceHost sh = new DomainServiceHost(typeof(MyDomainService), new Uri("http://foo.com/bar"), new Uri("https://foo.com/bar"), new Uri("net.tcp://bar.baz.com"));
            TracingDomainServiceEndpointFactory target = new TracingDomainServiceEndpointFactory();
            target.Name = "tracing";
            target.Parameters["maxEntries"] = "123";
            IEnumerable<ServiceEndpoint> actual = target.CreateEndpoints(null, sh, null);
            Assert.AreEqual(123, InMemoryTraceListener.MaxEntries);
            Assert.IsNotNull(actual);
            ServiceEndpoint[] endpoints = actual.ToArray();
            Assert.AreEqual(2, endpoints.Length);
            Assert.AreEqual("http://foo.com/bar/tracing", endpoints[0].Address.Uri.OriginalString);
            Assert.IsInstanceOfType(endpoints[0].Binding, typeof(WebHttpBinding));
            Assert.AreEqual(WebHttpSecurityMode.None, ((WebHttpBinding)(endpoints[0].Binding)).Security.Mode);
            Assert.AreEqual("https://foo.com/bar/tracing", endpoints[1].Address.Uri.OriginalString);
            Assert.IsInstanceOfType(endpoints[1].Binding, typeof(WebHttpBinding));
            Assert.AreEqual(WebHttpSecurityMode.Transport, ((WebHttpBinding)(endpoints[1].Binding)).Security.Mode);
        }

        public class MyEntity
        {
            [Key]
            public int Number { get; set; }
        }


        class MyDomainService : DomainService
        {
            public IQueryable<MyEntity> GetInts()
            {
                return null;
            }
        }
    }
}
