using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Description;
using System.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Common.Test;
using OpenRiaServices.Hosting.Wcf;
using OpenRiaServices.Hosting.Wcf.Configuration;
using OpenRiaServices.Client.Test;

namespace OpenRiaServices.Hosting.UnitTests
{
    /// <summary>
    /// Tests <see cref="DomainServiceHost"/> members.
    /// </summary>
    [TestClass]
    public class DomainServiceHostTest
    {
        [TestMethod]
        [Description("Tests that the built-in DomainServiceHost exposes 3 endpoints by default.")]
        public void DomainServiceHost_DefaultBehaviors()
        {
            DomainServiceHost host = this.CreateHost<DomainServiceHost>();

            // Verify we require ASP.NET compat mode.
            var aspNetCompatModeAtt = host.Description.Behaviors.Find<AspNetCompatibilityRequirementsAttribute>();
            Assert.IsNotNull(aspNetCompatModeAtt, "ASP.NET compat mode behavior not found.");
            Assert.AreEqual(AspNetCompatibilityRequirementsMode.Required, aspNetCompatModeAtt.RequirementsMode);

            // Verify service behavior defaults.
            var serviceBehaviorAtt = host.Description.Behaviors.Find<ServiceBehaviorAttribute>();
            Assert.IsNotNull(serviceBehaviorAtt, "Service behavior not found.");
            Assert.AreEqual(AddressFilterMode.Any, serviceBehaviorAtt.AddressFilterMode, "Unexpected address filter mode.");
            Assert.IsFalse(serviceBehaviorAtt.IncludeExceptionDetailInFaults, "Exception details should not be included in faults by default.");

            // Verify we that for a service with just a HTTP base address, only HttpGetEnabled is true.
            var metadataAtt = host.Description.Behaviors.Find<ServiceMetadataBehavior>();
            Assert.IsNotNull(metadataAtt, "Service metadata behavior not found.");
            Assert.IsTrue(metadataAtt.HttpGetEnabled, "HTTP GET disabled.");
            Assert.IsFalse(metadataAtt.HttpsGetEnabled, "HTTPS GET enabled.");

            // Verify we that for a service with just a HTTP base address, only HttpGetEnabled is true.
            host = this.CreateHost<DomainServiceHost>(new Uri("https://localhost/MyDomainService.svc"));
            metadataAtt = host.Description.Behaviors.Find<ServiceMetadataBehavior>();
            Assert.IsNotNull(metadataAtt, "Service metadata behavior not found.");
            Assert.IsFalse(metadataAtt.HttpGetEnabled, "HTTP GET enabled.");
            Assert.IsTrue(metadataAtt.HttpsGetEnabled, "HTTPS GET disabled.");
        }

        [TestMethod]
        [Description("Tests that the built-in DomainServiceHost exposes 1 endpoints by default.")]
        public void DomainServiceHost_DefaultEndpoints()
        {
            DomainServiceHost host = this.CreateHost<DomainServiceHost>();
            var eps = host.Description.Endpoints;

            Assert.AreEqual(1, eps.Count, "Unexpected amount of endpoints.");

            // REST w/ binary endpoint.
            Assert.IsTrue(eps.Any(ep => ep.Address.Uri.OriginalString.EndsWith("/binary")));
        }

        [TestMethod]
        [Description("Tests that HTTP base addresses are filtered out when a service requires a secure endpoint.")]
        public void DomainServiceHost_SecureEndpoint()
        {
            DomainServiceHost host = this.CreateHost<DomainServiceHost, MySecureDomainService>(new Uri[] {
                new Uri("http://localhost/MySecureDomainService.svc"),
                new Uri("https://localhost/MySecureDomainService.svc")
            });
            Assert.AreEqual(1, host.BaseAddresses.Count);
            Assert.AreEqual(Uri.UriSchemeHttps, host.BaseAddresses[0].Scheme);
        }

        [TestMethod]
        [Description("Tests that EndpointFactory.Name returns a string by default and does not accept nulls.")]
        public void EndpointFactory_Name()
        {
            var factory = new DomainServiceEndpointFactoryWithNoDefaultName();

            // By default, Name returns the default empty string.
            Assert.AreEqual(string.Empty, factory.Name, "Incorrect default value.");
            Assert.AreEqual("binary", (new PoxBinaryEndpointFactory()).Name, "Binary endpoint should have default name");

            // Name cannot be set to null.
            ExceptionHelper.ExpectArgumentNullException(delegate
            {
                factory.Name = null;
            }, "value");
        }

        private class DomainServiceEndpointFactoryWithNoDefaultName : DomainServiceEndpointFactory { }

        [TestMethod]
        [Description("Verifies that DomainServicesSection.InitializeDefaultInternal adds the default endpoints.")]
        public void DomainServicesSection_Initialize()
        {
            DomainServicesSection s = new DomainServicesSection();
            s.InitializeDefaultInternal();
            Assert.AreEqual(1, s.Endpoints.Count, "Default endpoint not added.");

            // Calling it again is safe.
            s.InitializeDefaultInternal();
            Assert.AreEqual(1, s.Endpoints.Count, "Default endpoint not added.");
        }

        private T CreateHost<T>() where T : DomainServiceHost
        {
            return (T)Activator.CreateInstance(typeof(T), typeof(MyDomainService), new Uri("http://localhost/MyDomainService.svc"));
        }

        private T CreateHost<T>(params Uri[] baseAddresses) where T : DomainServiceHost
        {
            return (T)Activator.CreateInstance(typeof(T), typeof(MyDomainService), baseAddresses);
        }

        private THost CreateHost<THost, TService>(params Uri[] baseAddresses) where THost : DomainServiceHost
        {
            return (THost)Activator.CreateInstance(typeof(THost), typeof(TService), baseAddresses);
        }
    }

    [OpenRiaServices.Server.EnableClientAccess]
    public class MyDomainService : OpenRiaServices.Server.DomainService
    {
        public IQueryable<MyDomainService_Entity> GetEntities()
        {
            return Array.Empty<MyDomainService_Entity>().AsQueryable();
        }
    }

    [OpenRiaServices.Server.EnableClientAccess(RequiresSecureEndpoint = true)]
    public class MySecureDomainService : MyDomainService
    {
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
}
