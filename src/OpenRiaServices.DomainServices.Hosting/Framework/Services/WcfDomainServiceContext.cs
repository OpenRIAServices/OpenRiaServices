using System;
using OpenRiaServices.DomainServices.Server;
using System.Web;
using System.Security.Principal;
using System.ServiceModel;

namespace OpenRiaServices.DomainServices.Hosting
{
    /// <summary>
    /// Context with extra WCF specific per-request properties
    /// </summary>
    class WcfDomainServiceContext : DomainServiceContext
    {
        private readonly HttpContext _httpContext;

        /// <summary>
        /// Initializes a new instance of the DomainServiceContext class
        /// </summary>
        /// <param name="serviceProvider">A service provider.</param>
        /// <param name="operationType">The type of operation that is being executed.</param>
        public WcfDomainServiceContext(IServiceProvider serviceProvider, DomainOperationType operationType)
            : base(serviceProvider, operationType)
        {
            _httpContext = HttpContext.Current;

            this.DisableStackTraces = _httpContext?.IsCustomErrorEnabled ?? true;
        }


        /// <summary>
        /// Same as <see cref="HttpContext.IsCustomErrorEnabled"/>; <c>true</c> means 
        /// that stack traces should not be sent (secure).
        /// </summary>
        public bool DisableStackTraces { get; }

        /// <summary>
        /// Expose HttpContext related "services", since the HttpContext will and 
        /// can change on async/await etc.
        /// </summary>
        /// <param name="serviceType">type of service reqeusted</param>
        /// <returns></returns>
        public override object GetService(Type serviceType)
        {
            if (serviceType == typeof(IPrincipal))
            {
                return base.User;
            }

            if (serviceType == typeof(HttpContext))
            {
                return _httpContext;
            }

            if (serviceType == typeof(HttpContextBase))
            {
                return new HttpContextWrapper(_httpContext);
            }

            return base.GetService(serviceType);
        }
    }
}
