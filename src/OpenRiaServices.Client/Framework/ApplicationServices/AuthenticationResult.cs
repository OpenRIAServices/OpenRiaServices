using System.Security.Principal;

namespace OpenRiaServices.DomainServices.Client.ApplicationServices
{
    /// <summary>
    /// Abstract base type for all the results returned by calls for
    /// asynchronous operations in <see cref="AuthenticationService"/>.
    /// </summary>
    public abstract class AuthenticationResult
    {
        private readonly IPrincipal _user;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationResult"/> class.
        /// </summary>
        /// <param name="user">The user principal.</param>
        internal AuthenticationResult(IPrincipal user)
        {
            this._user = user;
        }

        /// <summary>
        /// Gets the user
        /// </summary>
        public IPrincipal User
        {
            get { return this._user; }
        }
    }
}
