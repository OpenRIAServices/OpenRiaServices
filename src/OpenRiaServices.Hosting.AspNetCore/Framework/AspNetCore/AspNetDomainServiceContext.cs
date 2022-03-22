// ReuqstDelegate

using System;
using Microsoft.AspNetCore.Http;
using OpenRiaServices.Server;

class AspNetDomainServiceContext : DomainServiceContext, IServiceProvider
{
    private readonly HttpContext _httpContext;

    public AspNetDomainServiceContext(HttpContext httpContext, DomainOperationType operationType)
        : base(httpContext.RequestServices, httpContext.User, operationType)
    {
        CancellationToken = httpContext.RequestAborted;
        _httpContext = httpContext;
    }

    object IServiceProvider.GetService(Type serviceType)
    {
        if (serviceType == typeof(HttpContext))
            return _httpContext;
        else
            return base.GetService(serviceType);
    }
}
