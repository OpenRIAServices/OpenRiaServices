using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Globalization;
using System.Security.Principal;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using OpenRiaServices.Server;
using OpenRiaServices.Server.Authentication;

namespace RootNamespace.TestNamespace
{
    [EnableClientAccess]
    public class AuthenticationService1 : AuthenticationBase<User1> { }
    public class User1 : UserBase { };

    public class AuthenticationBase<TUser> : DomainService, IAuthentication<TUser>
        where TUser : IUser, new()
    {
        protected HttpContext HttpContext
            => base.ServiceContext.GetService<IHttpContextAccessor>().HttpContext;

        public TUser GetUser()
        {
            return new TUser() { Name = string.Empty, Roles = Array.Empty<string>() };
        }

        public TUser Login(string userName, string password, bool isPersistent, string customData)
        {
            if (userName is "manager" or "employee")
            {
                return new TUser()
                {
                    Name = userName,
                    Roles = new[] { userName }
                };
            }
            else
            {
                return new TUser() { Name = string.Empty, Roles = Array.Empty<string>() };
            }
        }

        public TUser Logout()
        {
            return new TUser() { Name = string.Empty, Roles = Array.Empty<string>() };
        }

        public void UpdateUser(TUser user)
        {
            throw new System.NotImplementedException();
        }
    }


    public class UserBase : IUser
    {
        [Key]
        public string Name { get; set; }
        public IEnumerable<string> Roles { get; set; } = new List<string>();
    }
}
