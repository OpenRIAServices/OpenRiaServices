using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Extensions.DependencyInjection;
using OpenRiaServices.Hosting.Wcf;

namespace WebsiteMediumTrust
{
    public class Global : System.Web.HttpApplication
    {
        // Register http module dynamically since type name will be different for signed and unsiged
        private static IHttpModule DomainServiceHttpModule = new DomainServiceHttpModule();

        public override void Init()
        {
            base.Init();
            // Setup OpenRiaServices Http module.
            DomainServiceHttpModule.Init(this);

            // Setup DI
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddDomainServices(ServiceLifetime.Transient);
            var serviceProvider = serviceCollection.BuildServiceProvider();

            // Setup endpoints etc via config
            var config = OpenRiaServices.Hosting.Wcf.Configuration.Internal.DomainServiceHostingConfiguration.Current;
            config.EndpointFactories.Add(new JsonEndpointFactory());
            config.EndpointFactories.Add(new SoapXmlEndpointFactory());

            // Setup DI example - tests runs with default provider
            /*
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddDomainServices(ServiceLifetime.Transient);
            var serviceProvider = serviceCollection.BuildServiceProvider();

            config.ServiceProvider = serviceProvider;
            */
        }
    }
}
