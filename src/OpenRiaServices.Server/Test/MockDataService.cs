using System;
using System.Security.Principal;

namespace OpenRiaServices.Server.Test
{
    public class MockDataService : IServiceProvider
    {
        private readonly IPrincipal user;

        public MockDataService(IPrincipal user = null)
        {
            this.user = user;
        }

        #region IServiceProvider Members

        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(IPrincipal))
            {
                return this.user;
            }
            return null;
        }

        #endregion
    }
}
