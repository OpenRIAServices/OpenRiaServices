// ReuqstDelegate

using System;
using Microsoft.AspNetCore.Http;
using OpenRiaServices.Server;

namespace OpenRiaServices.Hosting.AspNetCore
{
    class AspNetDomainServiceContext : DomainServiceContext
    {
        public AspNetDomainServiceContext(HttpContext httpContext, DomainOperationType operationType)
            : base(httpContext.RequestServices, httpContext.User, operationType)
        {
            CancellationToken = httpContext.RequestAborted;
        }
    }
}
