using System.Security.Principal;

namespace OpenRiaServices.Client.ApplicationServices
{
    /// <summary>
    /// Result of a save user request
    /// </summary>
    public sealed class SaveUserResult : AuthenticationResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SaveUserResult"/> class.
        /// </summary>
        /// <param name="user">The saved user</param>
        public SaveUserResult(IPrincipal user)
            : base(user)
        {
        }
    }
}
