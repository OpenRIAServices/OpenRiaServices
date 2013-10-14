using System;
using OpenRiaServices.DomainServices;

namespace OpenRiaServices.DomainController.Server.ApplicationServices
{
    /// <summary>
    /// Attribute that marks a <see cref="DomainController"/> as an authentication service.
    /// </summary>
    /// <remarks>
    /// This attribute is used to associate the <see cref="AuthenticationCodeProcessor"/> with
    /// an implementation of the <see cref="IAuthentication{T}"/> interface.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = true)]
    public sealed class AuthenticationServiceAttribute : DomainIdentifierAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationServiceAttribute"/> class.
        /// </summary>
        public AuthenticationServiceAttribute() : base("Authentication")
        {
            this.CodeProcessor = typeof(AuthenticationCodeProcessor);
            this.IsApplicationService = true;
        }
    }
}
