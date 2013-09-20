using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Principal;

[assembly: SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes", Scope = "member", Target = "System.ServiceModel.DomainServices.Server.ApplicationServices.UserBase.#System.Security.Principal.IPrincipal.Identity", Justification = "Low use so explicitly withheld from the API")]
[assembly: SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes", Scope = "member", Target = "System.ServiceModel.DomainServices.Server.ApplicationServices.UserBase.#System.Security.Principal.IIdentity.AuthenticationType", Justification = "Low use so explicitly withheld from the API")]
namespace System.ServiceModel.DomainServices.Server.ApplicationServices
{
    /// <summary>
    /// Base class for user entities that has properties for passing principal values to the client.
    /// </summary>
    /// <remarks>
    /// This class provides properties to support serialization of principal values to the
    /// <c>DomainContext</c> generated for any domain service extending <see cref="AuthenticationBase{T}"/>.
    /// It also presents those values via the <see cref="IPrincipal"/> and <see cref="IIdentity"/> interfaces 
    /// for use in shared authorization scenarios.
    /// </remarks>
    public abstract class UserBase : IUser, IPrincipal, IIdentity
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="UserBase"/> class.
        /// </summary>
        protected UserBase() { }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether the user is authenticated. See
        /// <see cref="IIdentity.IsAuthenticated"/>.
        /// </summary>
        /// <remarks>
        /// This value is true if <see cref="Name"/> is not <c>null</c> or empty. This is the
        /// same implementation as <see cref="GenericIdentity.IsAuthenticated"/>.
        /// </remarks>
        [ProfileUsage(IsExcluded = true)]
        [Exclude]
        public bool IsAuthenticated
        {
            get { return !string.IsNullOrEmpty(this.Name); }
        }

        /// <summary>
        /// Gets or sets the name. See <see cref="IIdentity.Name"/>.
        /// </summary>
        /// <remarks>
        /// The value is <c>null</c> by default, but must be set to a non-<c>null</c> value
        /// before it is serialized.
        /// </remarks>
        [Key]
        [ProfileUsage(IsExcluded = true)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the roles the user is a member of.
        /// </summary>
        /// <remarks>
        /// This value may be <c>null</c>. The value is <c>null</c> by default.
        /// </remarks>
        [Editable(false)]
        [ProfileUsage(IsExcluded = true)]
        public IEnumerable<string> Roles { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Determines whether the current user belongs to the specified role.
        /// </summary>
        /// <remarks>
        /// Returns whether the specified role is contained in <see cref="Roles"/>.
        /// This implementation is case sensitive.  See <see cref="IPrincipal.IsInRole"/>.
        /// </remarks>
        /// <param name="role">The name of the role for which to check membership.</param>
        /// <returns><c>true</c> if the current user is a member of the specified role;
        /// otherwise, <c>false</c>.
        /// </returns>
        public bool IsInRole(string role)
        {
            if (this.Roles == null)
            {
                return false;
            }
            return this.Roles.Contains(role);
        }

        #endregion

        #region IPrincipal Members

        /// <summary>
        /// Gets the identity. See <see cref="IPrincipal.Identity"/>.
        /// </summary>
        /// <remarks>
        /// This value is never <c>null</c>.
        /// </remarks>
        IIdentity IPrincipal.Identity
        {
            get { return this; }
        }

        #endregion

        #region IIdentity Members

        /// <summary>
        /// Gets the authentication type. See <see cref="IIdentity.AuthenticationType"/>.
        /// </summary>
        /// <remarks>
        /// The value is an empty string by default.
        /// </remarks>
        string IIdentity.AuthenticationType
        {
            get { return string.Empty; }
        }

        #endregion
    }
}
