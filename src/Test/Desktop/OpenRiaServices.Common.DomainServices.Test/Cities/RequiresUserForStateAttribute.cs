using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Security.Principal;

namespace Cities
{
    /// <summary>
    /// Derived <see cref="AuthorizationAttribute"/> that authorizes only explicitly identified users to modify
    /// specific states.  Tests attribute derivation as well as access into entity instance.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class RequiresUserForStateAttribute : RequiresUserAttribute
    {
        private readonly string _stateName;

        public RequiresUserForStateAttribute(string userName, string stateName) : base(userName)
        {
            this._stateName = stateName;
        }

        protected override AuthorizationResult IsAuthorized(IPrincipal principal, AuthorizationContext authorizationContext)
        {
            // Allowed only on domain operations for entities with a StateName property
            PropertyInfo property = authorizationContext.Instance == null ? null : authorizationContext.Instance.GetType().GetProperty("StateName");
            string stateName = property == null ? null : property.GetValue(authorizationContext.Instance, null) as string;
            if (stateName == null)
            {
                throw new InvalidOperationException("The RequiresUserForStateAttribute can be used only on entities with a StateName property");
            }

            // If not the city, it is allowed
            if (!string.Equals(this._stateName, stateName, StringComparison.OrdinalIgnoreCase))
            {
                return AuthorizationResult.Allowed;
            }

            // It is the city.  Let base class continue the authorization
            return base.IsAuthorized(principal, authorizationContext);
        }
    }
}

