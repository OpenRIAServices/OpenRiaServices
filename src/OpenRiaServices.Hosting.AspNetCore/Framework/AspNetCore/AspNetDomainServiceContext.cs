// ReuqstDelegate

using Microsoft.AspNetCore.Http;
using OpenRiaServices.Server;

class AspNetDomainServiceContext : DomainServiceContext
{
    public AspNetDomainServiceContext(HttpContext httpContext, DomainOperationType operationType)
        : base(httpContext.RequestServices, httpContext.User, operationType)
    {
        CancellationToken = httpContext.RequestAborted;
    }
}
