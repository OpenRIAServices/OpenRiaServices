using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenRiaServices.Hosting.Wcf;
using OpenRiaServices.Server;

namespace OpenRiaServices.Hosting
{
    static class DomainServiceExtensions
    {
        /// <summary>
        /// Same as <see cref="System.Web.HttpContext.IsCustomErrorEnabled"/>; <c>true</c> means 
        /// that stack traces should not be sent to clients (secure).
        /// </summary>
        internal static bool GetDisableStackTraces(this DomainServiceContext domainServiceContext)
        {
            return (domainServiceContext as WcfDomainServiceContext)?.DisableStackTraces ?? true;
        }
    }
}
