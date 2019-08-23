using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Principal;
using OpenRiaServices.DomainServices.Server;

namespace Cities
{
    /// <summary>
    /// Derived <see cref="AuthorizationAttribute"/> that authorizes only explicitly identified users.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class RequiresUserAttribute : AuthorizationAttribute
    {
        private readonly string _userName;
        public RequiresUserAttribute(string userName)
        {
            this._userName = userName;
        }

        protected override AuthorizationResult IsAuthorized(IPrincipal principal, AuthorizationContext authorizationContext)
        {
            return (principal.Identity != null && principal.Identity.IsAuthenticated && string.Equals(this._userName, principal.Identity.Name, StringComparison.InvariantCultureIgnoreCase))
                        ? AuthorizationResult.Allowed
                        : new AuthorizationResult(this.FormatErrorMessage(authorizationContext.Operation));
        }
    }
}

