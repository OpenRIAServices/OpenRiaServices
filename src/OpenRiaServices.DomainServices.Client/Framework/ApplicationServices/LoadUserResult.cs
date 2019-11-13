using System.Security.Principal;

namespace OpenRiaServices.DomainServices.Client.ApplicationServices
{
    /// <summary>
    /// Result of a user load request
    /// </summary>
    public sealed class LoadUserResult : AuthenticationResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LoadUserResult"/> class.
        /// </summary>
        /// <param name="user">The loaded user</param>
        public LoadUserResult(IPrincipal user)
            : base(user)
        {
        }
    }
}
