﻿using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenRiaServices.Hosting.AspNetCore;
using OpenRiaServices.Server;

#nullable enable

namespace OpenRiaServices.Hosting
{
    // Consider exposing extension methods
    static class DomainServiceContextExtensions
    {
        public static HttpContext? GetHttpContext(this DomainServiceContext domainServiceContext)
            => (domainServiceContext as AspNetDomainServiceContext)?.HttpContext;

        /// <summary>
        /// <c>true</c> means that stack traces should not be sent to clients (secure).
        /// </summary>
        internal static bool GetDisableStackTraces(this DomainServiceContext serviceContext)
        {
            return !serviceContext.GetRequiredService<IOptions<OpenRiaServicesOptions>>()
                .Value.IncludeExceptionStackTraceInErrors;
        }
    }
}
