using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Principal;

namespace OpenRiaServices.Server
{
    /// <summary>
    /// Identifies a set of roles that are permitted to invoke a <see cref="DomainOperationEntry"/>.
    /// </summary>
    /// <remarks>
    /// This attribute is used to specify a set of roles that are required.  It is a subclass
    /// of <see cref="AuthorizationAttribute"/> and meant to be used on domain operations
    /// such as queries, update methods, etc. to control authorization.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public sealed class RequiresRoleAttribute : AuthorizationAttribute
    {
        /// <summary>
        /// The roles to which the current user must belong before the specified <see cref="DomainOperationEntry"/> may be invoked.
        /// </summary>
        private readonly ReadOnlyCollection<string> _roles;

        /// <summary>
        /// Initializes a new instance of the RequiresRoleAttribute class
        /// </summary>
        /// <param name="role">The role to which the current user must belong before the <see cref="DomainOperationEntry"/> may be invoked.</param>
        public RequiresRoleAttribute(string role)  // (ctor required for CLS compliance)
        {
            if (!String.IsNullOrEmpty(role))
            {
                var tempList = new List<string> { role };
                this._roles = tempList.AsReadOnly();
            }
            else
            {
                this._roles = new ReadOnlyCollection<string>(Array.Empty<string>());
            }
        }

        /// <summary>
        /// Initializes a new instance of the RequiresRoleAttribute class
        /// </summary>
        /// <param name="roles">The set of roles permitted to invoke the <see cref="DomainOperationEntry"/>. The
        /// current user must be in at least one of the roles to invoke the operation.</param>
        public RequiresRoleAttribute(params string[] roles)
        {
            if (roles != null && roles.Length > 0)
            {
                this._roles = roles.ToList().AsReadOnly();
            }
            else
            {
                this._roles = new ReadOnlyCollection<string>(Array.Empty<string>());
            }
        }

        /// <summary>
        /// Gets the roles permitted to invoke the operation.
        /// </summary>
        public IEnumerable<string> Roles
        {
            get
            {
                return this._roles;
            }
        }

#if !SILVERLIGHT
        /// <summary>
        /// Gets a unique identifier for this attribute.
        /// </summary>
        public override object TypeId
        {
            get
            {
                return this;
            }
        }
#endif

        /// <summary>
        /// Determines whether the given <paramref name="principal"/> is authorized to perform the operation
        /// specified by given <paramref name="authorizationContext"/>.
        /// </summary>
        /// <remarks>This method returns <see cref="AuthorizationResult.Allowed"/> only when the <paramref name="principal"/>
        /// is authenticated and belongs to at least one of the roles specified in <see cref="Roles"/>.
        /// <para>
        /// To require the principal to belong to multiple roles, multiple <see cref="RequiresRoleAttribute"/>
        /// custom attributes should be used on the respective operation.
        /// </para>
        /// </remarks>
        /// <param name="principal">The <see cref="IPrincipal"/> to authorize.</param>
        /// <param name="authorizationContext">The <see cref="AuthorizationContext"/> in which authorization is required.</param>
        /// <returns>A <see cref="AuthorizationResult"/> indicating whether or not the <paramref name="principal"/> is authorized.
        /// The value <see cref="AuthorizationResult.Allowed"/> indicates authorization is granted.  Any other value
        /// indicates it has been denied.</returns>
        protected override AuthorizationResult IsAuthorized(IPrincipal principal, AuthorizationContext authorizationContext)
        {
            if (this._roles.Count == 0)
            {
                throw new InvalidOperationException(Resource.RequiresRoleAttribute_MustSpecifyRole);
            }

            if (principal.Identity != null && principal.Identity.IsAuthenticated)
            {
                // the user has to be in at least one of the roles
                foreach (string role in this._roles)
                {
                    if (principal.IsInRole(role))
                    {
                        return AuthorizationResult.Allowed;
                    }
                }
            }

            return new AuthorizationResult(this.FormatErrorMessage(authorizationContext.Operation));
        }
    }
}
