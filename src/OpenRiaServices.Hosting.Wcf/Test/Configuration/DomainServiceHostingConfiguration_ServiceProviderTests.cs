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
                // Cannot set null provider
                ExceptionHelper.ExpectArgumentException(() => config.ServiceProvider = null
                    , Resource.DomainServiceHostingConfiguration_ServiceProvider_MustSupportScope, "value");
                Assert.AreEqual(previous, config.ServiceProvider);
                Assert.AreEqual(previousScopeFactory, DomainServiceHostingConfiguration.ServiceScopeFactory);

                // Verify the default factory creates an instance as expected.
                using (var scope = DomainServiceHostingConfiguration.ServiceScopeFactory.CreateScope())
                {
                    MockDomainService domainService = scope.ServiceProvider.GetRequiredService<MockDomainService>();
                    Assert.IsFalse(domainService.Initialized);

                    // Verify the default factory disposed the instance as expected.
                    scope.Dispose();
                    Assert.IsTrue(domainService.Disposed);
                }

                // Verify that the property works as expected for Microsoft DI Implementation
                var serviceCollection = new ServiceCollection();
                serviceCollection.AddDomainServices(ServiceLifetime.Transient, new[] { typeof(MockDomainService).Assembly });
                config.ServiceProvider = serviceCollection.BuildServiceProvider();
                using (var scope = DomainServiceHostingConfiguration.ServiceScopeFactory.CreateScope())
                {
                    var mock1 = scope.ServiceProvider.GetRequiredService<MockDomainService>();
                    var mock2 = scope.ServiceProvider.GetRequiredService<MockDomainService>();
                    Assert.AreNotSame(mock1, mock2, "Transient lifetime should be respected");

                    Assert.IsNull(scope.ServiceProvider.GetService(typeof(TestDomainServices.ServerSideAsyncDomainService)), "Should only resolve types from registered assemblies");
                }
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
