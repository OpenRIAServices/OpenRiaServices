using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;

namespace OpenRiaServices.Server.Test
{
    public class MockUser : IPrincipal, IIdentity
    {
        private readonly IEnumerable<string> roles;
        private readonly string name;
        private bool isAuthenticated;

        public MockUser(string name)
        {
            this.name = name;
        }

        public MockUser(string name, IEnumerable<string> roles)
        {
            this.name = name;
            this.roles = roles;
        }

        #region IPrincipal Members

        public IIdentity Identity
        {
            get
            {
                return this;
            }
        }

        public bool IsInRole(string role)
        {
            return this.roles.Contains(role);
        }

        #endregion

        #region IIdentity Members

        public string AuthenticationType
        {
            get
            {
                return "forms";
            }
        }

        public bool IsAuthenticated
        {
            get
            {
                return this.isAuthenticated;
            }
            set
            {
                this.isAuthenticated = value;
            }
        }

        public string Name
        {
            get
            {
                return this.name;
            }
        }

        #endregion
    }
}
