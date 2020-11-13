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
        public bool RequiresSecureEndpoint { get; set; }

        #endregion
    }
}
