using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Principal;

namespace OpenRiaServices.DomainController.Server
{
    /// <summary>
    /// Identifies the authentication requirements needed to invoke a <see cref="DomainOperationEntry"/>.
    /// </summary>
    /// <remarks>
    /// This attribute is used to specify the permissions required to invoke a domain operation.
    /// The type containing the domain operation may also be marked with this attribute and its
    /// permission requirements will be applied to all domain operations within.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class RequiresAuthenticationAttribute : AuthorizationAttribute
    {
       /// <summary>
        /// Determines whether the given <paramref name="principal"/> is authorized to perform the operation
        /// specified by given <paramref name="authorizationContext"/>.
        /// </summary>
        /// <remarks>
        /// This method returns <see cref="AuthorizationResult.Allowed"/> only when the <paramref name="principal"/>
        /// is authenticated.
        /// </remarks>
        /// <param name="principal">The <see cref="IPrincipal"/> to authorize.</param>
        /// <param name="authorizationContext">The <see cref="AuthorizationContext"/> in which authorization is required.</param>
        /// <returns>A <see cref="AuthorizationResult"/> indicating whether or not the <paramref name="principal"/> is authorized.
        /// The value <see cref="AuthorizationResult.Allowed"/> indicates authorization is granted.  Any other value
        /// indicates it has been denied.</returns>        
        protected override AuthorizationResult IsAuthorized(IPrincipal principal, AuthorizationContext authorizationContext)
        {
            return (principal.Identity != null && principal.Identity.IsAuthenticated)
                        ? AuthorizationResult.Allowed
                        : new AuthorizationResult(this.FormatErrorMessage(authorizationContext.Operation));
        }
    }
}
