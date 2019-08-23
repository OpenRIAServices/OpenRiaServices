using System;
using System.Linq;
using System.Web.Security;

namespace OpenRiaServices.DomainServices.Server.Authentication.AspNetMembership.Test
{
    public class MockRoleProvider : RoleProvider
    {
        #region NotImplemented

        public override void AddUsersToRoles(string[] usernames, string[] roleNames)
        {
            throw new NotImplementedException();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065", Justification = "Not Implemented.")]
        public override string ApplicationName
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public override void CreateRole(string roleName)
        {
            throw new NotImplementedException();
        }

        public override bool DeleteRole(string roleName, bool throwOnPopulatedRole)
        {
            throw new NotImplementedException();
        }

        public override string[] FindUsersInRole(string roleName, string usernameToMatch)
        {
            throw new NotImplementedException();
        }

        public override string[] GetAllRoles()
        {
            throw new NotImplementedException();
        }

        public override string[] GetUsersInRole(string roleName)
        {
            throw new NotImplementedException();
        }

        public override bool IsUserInRole(string username, string roleName)
        {
            throw new NotImplementedException();
        }

        public override void RemoveUsersFromRoles(string[] usernames, string[] roleNames)
        {
            throw new NotImplementedException();
        }

        public override bool RoleExists(string roleName)
        {
            throw new NotImplementedException();
        }

        #endregion

        public Exception Error { get; set; }

        public MockUser User { get; set; }

        public override string[] GetRolesForUser(string username)
        {
            if (Error != null)
            {
                throw Error;
            }
            if (User == null)
            {
                return new string[0];
            }
            return User.Roles.ToArray();
        }
    }
}
