using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Configuration;
using System.Web.Security;
using System.Web.SessionState;
using OpenRiaServices.DomainServices.Hosting;

namespace WebsiteMediumTrust
{
    public class Global : System.Web.HttpApplication
    {
        // Register http module dynamically since type name will be different for signed and unsiged
        private static IHttpModule DomainServiceHttpModule = new DomainServiceHttpModule();

        public override void Init()
        {
            base.Init();
            DomainServiceHttpModule.Init(this);

            DomainServicesSection config = DomainServicesSection.Current;
            config.Endpoints.Add(new System.Configuration.ProviderSettings("json", typeof(JsonEndpointFactory).AssemblyQualifiedName));
            config.Endpoints.Add(new System.Configuration.ProviderSettings("soap", typeof(SoapXmlEndpointFactory).AssemblyQualifiedName));
        }
    }
}