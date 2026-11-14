using System;
using System.Security.Principal;
using Microsoft.Extensions.Options;
using OpenRiaServices.Hosting.AspNetCore;

namespace OpenRiaServices.Server.Test
{
    public class MockDataService : IServiceProvider, IOptions<OpenRiaServicesOptions>
    {
        private readonly IPrincipal _user;
        private readonly OpenRiaServicesOptions _options;

        public MockDataService(IPrincipal user = null, Hosting.AspNetCore.OpenRiaServicesOptions options = null)
        {
            this._user = user;
            this._options = options ?? new Hosting.AspNetCore.OpenRiaServicesOptions();
        }

        public OpenRiaServicesOptions Value => _options;

        #region IServiceProvider Members

        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(IPrincipal))
            {
                return this._user;
            }
            else if (serviceType == typeof(IOptions<OpenRiaServicesOptions>))
            {
                return this;
            }
            return null;
        }

        #endregion
    }
}
