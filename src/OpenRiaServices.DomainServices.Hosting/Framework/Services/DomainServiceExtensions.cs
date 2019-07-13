using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenRiaServices.DomainServices.Server;

namespace OpenRiaServices.DomainServices.Hosting
{
    static class DomainServiceExtensions
    {
        /// <summary>
        /// Same as <see cref="System.Web.HttpContext.IsCustomErrorEnabled"/>; <c>true</c> means 
        /// that stack traces should not be sent to clients (secure).
        /// </summary>
        public static bool GetDisableStackTraces(this DomainService domainService)
        {
            return ((WcfDomainServiceContext)domainService.ServiceContext).DisableStackTraces;
        }
    }
}
