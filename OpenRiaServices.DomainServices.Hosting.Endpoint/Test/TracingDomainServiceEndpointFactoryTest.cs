using System.ServiceModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using OpenRiaServices.DomainServices.Server;
using OpenRiaServices.DomainServices.Hosting;
using System.ServiceModel.Description;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using OpenRiaServices;

namespace OpenRiaServices.DomainServices.Hosting.Local.Test
{
    
    
    /// <summary>
    ///This is a test class for TracingDomainServiceEndpointFactoryTest and is intended
    ///to contain all TracingDomainServiceEndpointFactoryTest Unit Tests
    ///</summary>
    [TestClass()]
    public class TracingDomainServiceEndpointFactoryTest
    {


        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion


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
            IEnumerable<ServiceEndpoint> actual = target.CreateEndpoints(null, sh);
            Assert.IsTrue(123 == InMemoryTraceListener_Accessor.MaxEntries);
            Assert.IsNotNull(actual);
            ServiceEndpoint[] endpoints = actual.ToArray();
            Assert.IsTrue(endpoints.Length == 2);
            Assert.IsTrue(endpoints[0].Address.Uri.OriginalString == "http://foo.com/bar/tracing");
            Assert.IsInstanceOfType(endpoints[0].Binding, typeof(WebHttpBinding));
            Assert.IsTrue(((WebHttpBinding)(endpoints[0].Binding)).Security.Mode == WebHttpSecurityMode.None);
            Assert.IsTrue(endpoints[1].Address.Uri.OriginalString == "https://foo.com/bar/tracing");
            Assert.IsInstanceOfType(endpoints[1].Binding, typeof(WebHttpBinding));
            Assert.IsTrue(((WebHttpBinding)(endpoints[1].Binding)).Security.Mode == WebHttpSecurityMode.Transport);
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
