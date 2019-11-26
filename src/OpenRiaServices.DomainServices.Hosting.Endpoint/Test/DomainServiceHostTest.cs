using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using OpenRiaServices;
using System.ServiceModel.Activation;
using System.ServiceModel.Description;
using OpenRiaServices.DomainServices.Hosting;
using OpenRiaServices.DomainServices.Server;
using System.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenRiaServices.DomainServices.Hosting.UnitTests
{
    /// <summary>
    /// Tests <see cref="DomainServiceHost"/> members.
    /// </summary>
    [TestClass]
    public class DomainServiceHostTest
    {
        [TestMethod]
        [Description("Tests that the built-in DomainServiceHost exposes 2 endpoints by default.")]
        public void DomainServiceHost_DefaultEndpoints()
        {
            DomainServiceHost host = this.CreateHost<DomainServiceHost>();
            var eps = host.Description.Endpoints;

            Assert.AreEqual(2, eps.Count, "Unexpected amount of endpoints.");

            // REST w/ binary endpoint.
            Assert.IsTrue(eps.Any(ep => ep.Address.Uri.OriginalString.EndsWith("/binary")));

            // REST w/ JSON endpoint.
            Assert.IsTrue(eps.Any(ep => ep.Address.Uri.OriginalString.EndsWith("/json")));

            Assert.AreEqual(1, CustomJsonEndpointFactory.LastParameters.Count, "Incorrect number of parameters were retrieved from config.");
        }

        private T CreateHost<T>() where T : DomainServiceHost
        {
            return (T)Activator.CreateInstance(typeof(T), typeof(MyDomainService), new Uri("http://localhost/MyDomainService.svc"));
        }
    }

    [EnableClientAccess]
    public class MyDomainService : DomainService
    {
        public IQueryable<MyDomainService_Entity> GetEntities()
        {
            return null;
        }
    }

    public class MyDomainService_Entity
    {
        [Key]
        public int Key
        {
            get;
            set;
        }
    }

    public class CustomJsonEndpointFactory : JsonEndpointFactory
    {
        public static NameValueCollection LastParameters;

        public override IEnumerable<ServiceEndpoint> CreateEndpoints(DomainServiceDescription description, DomainServiceHost serviceHost, ContractDescription contractDescription)
        {
            CustomJsonEndpointFactory.LastParameters = this.Parameters;
            return base.CreateEndpoints(description, serviceHost, contractDescription);
        }
    }
}
