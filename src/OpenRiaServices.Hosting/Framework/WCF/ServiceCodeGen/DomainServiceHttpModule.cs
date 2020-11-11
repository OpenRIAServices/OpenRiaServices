using System;
using System.Collections.Generic;
using System.Web;

namespace OpenRiaServices.Hosting.WCF
{
    /// <summary>
    /// Represents a module which takes care of registering the <see cref="DomainServiceVirtualPathProvider"/>.
    /// </summary>
    public sealed class DomainServiceHttpModule : IHttpModule
    {
        void IHttpModule.Dispose()
        {
        }

        void IHttpModule.Init(HttpApplication application)
        {
            application.BeginRequest += new EventHandler(DomainServiceHttpModule.ApplicationBeginRequest);
            DomainServiceVirtualPathProvider.Register();
        }

        private static void ApplicationBeginRequest(object sender, EventArgs e)
        {
            HttpRequest request = HttpContext.Current.Request;
            string virtualPath = request.AppRelativeCurrentExecutionFilePath;

            // Rewrite /ClientBin/Northwind.svc to /Northwind.svc. This way we guarantee we 
            // only instantiate a single service host, and we allow someone to think about 
            // a single file (e.g. when defining ACLs, etc.).
            KeyValuePair<string, Type> domainServiceEntry;
            if (DomainServiceVirtualPathProvider.ShouldRewritePath(virtualPath, out domainServiceEntry))
            {
                string queryString = request.QueryString.ToString();
                HttpContext.Current.RewritePath(
                    DomainServiceVirtualPathProvider.DomainServicesDirectory + domainServiceEntry.Key,
                    request.PathInfo,
                    queryString,
                    /* setClientFilePath */ true);
            }
        }
    }
}
