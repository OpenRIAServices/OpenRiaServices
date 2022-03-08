using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Client.Test;
using OpenRiaServices.Hosting.Wcf.Configuration.Internal;
using OpenRiaServices.Server;

namespace OpenRiaServices.Hosting.Wcf.Configuration
{
    [TestClass]
    public class DomainServiceHostingConfiguration_ServiceProviderTests
    {
        [TestMethod]
        [Description("Verifies the default factory behavior of the DomainService.Factory property.")]
        public void DomainServiceFactory_DefaultServiceBehavior()
        {
            // After setting the DP Factory to null, verify that the property getter does 
            // not return null.  It should instead return a default factory implementation.
            var config = DomainServiceHostingConfiguration.Current;
            var previous = config.ServiceProvider;
            var previousScopeFactory = DomainServiceHostingConfiguration.ServiceScopeFactory;
            try
            {
                ExceptionHelper.ExpectArgumentException(() => config.ServiceProvider = null, Resource.DomainServiceHostingConfiguration_ServiceProvider_MustSupportScope);
                Assert.AreEqual(previous, config.ServiceProvider);
                Assert.AreEqual(previousScopeFactory, DomainServiceHostingConfiguration.ServiceScopeFactory);

                using var scope = DomainServiceHostingConfiguration.ServiceScopeFactory.CreateScope();

                // Verify the default factory creates an instance as expected.
                MockDomainService domainService = scope.ServiceProvider.GetRequiredService<MockDomainService>();
                Assert.IsFalse(domainService.Initialized);

                // Verify the default factory disposed the instance as expected.
                scope.Dispose();
                Assert.IsTrue(domainService.Disposed);
            }
            finally
            {
                // Be sure to restore the factory!
                config.ServiceProvider = previous;
            }
        }

        [TestMethod]
        [Description("Verifies the default factory throws on invalid DomainService types.")]
        public void DomainServiceFactory_InvalidDomainServiceType()
        {
            using var scope = DomainServiceHostingConfiguration.ServiceScopeFactory.CreateScope();

            // Verify the default provider does not creates instance of other types.
            Assert.IsNull(scope.ServiceProvider.GetService(typeof(string)));
        }

        [EnableClientAccess]
        public class MockDomainService : DomainService
        {
            public bool Disposed { get; private set; }
            public bool Initialized { get; private set; }

            public override void Initialize(DomainServiceContext domainServiceContext)
            {
                this.Initialized = true;
            }

            protected override void Dispose(bool disposing)
            {
                this.Disposed = true;
            }
        }
    }
}
