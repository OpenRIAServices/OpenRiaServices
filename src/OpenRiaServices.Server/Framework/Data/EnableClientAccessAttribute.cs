using System;

namespace OpenRiaServices.Server
{
    /// <summary>
    /// Attribute used to mark a <see cref="DomainService"/> as 
    /// accessible to clients.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class EnableClientAccessAttribute : Attribute
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="EnableClientAccessAttribute"/> class.
        /// </summary>
        public EnableClientAccessAttribute() { }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="DomainService"/>
        /// may only be accessed using a secure endpoint.
        /// </summary>
#if NET
        [Obsolete("RequiresSecureEndpoint is not enforced by AspNetCore Hosting, please enforce it using standard AspNetCore practices instead https://learn.microsoft.com/en-us/aspnet/core/security/enforcing-ssl?")]
#endif
        public bool RequiresSecureEndpoint { get; set; }

        #endregion
    }
}
