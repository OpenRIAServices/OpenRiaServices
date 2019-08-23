using System;
using System.Security.Principal;

namespace OpenRiaServices.DomainServices.Server.UnitTesting
{
    internal class ServiceProviderStub : IServiceProvider
    {
        private const string DefaultName = "UserStub";
        private static readonly string[] DefaultRoles = new string[0];

        private readonly IPrincipal _user;

        public ServiceProviderStub()
            : this(string.Empty)
        {
        }

        public ServiceProviderStub(bool isAuthenticated)
            : this(isAuthenticated ? ServiceProviderStub.DefaultName : string.Empty)
        {
        }

        private ServiceProviderStub(string userName)
            : this(new GenericPrincipal(new GenericIdentity(userName), ServiceProviderStub.DefaultRoles))
        {
        }

        public ServiceProviderStub(IPrincipal user)
        {
            this._user = user;
        }

        public IPrincipal User
        {
            get { return this._user; }
        }

        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(IPrincipal))
            {
                return this.User;
            }

            return null;
        }
    }
}
