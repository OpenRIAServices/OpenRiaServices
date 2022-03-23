using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenRiaServices.Server;

namespace OpenRiaServices.Hosting
{
    static class DomainServiceExtensions
    {
        /// <summary>
        /// <c>true</c> means that stack traces should not be sent to clients (secure).
        /// </summary>
        public static bool GetDisableStackTraces(this DomainService domainService)
        {
            // todo: allow configuring via options, or maybe tweak based on "environment"
            return true;
        }
    }
}
