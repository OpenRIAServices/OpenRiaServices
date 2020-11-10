using System.Security.Principal;

namespace OpenRiaServices.Client.Authentication
{
    /// <summary>
    /// Result of an logout request
    /// </summary>
    public sealed class LogoutResult : AuthenticationResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LogoutResult"/> class.
        /// </summary>
        /// <param name="user">The anonymous user</param>
        public LogoutResult(IPrincipal user)
            : base(user)
        {
        }
    }
}
