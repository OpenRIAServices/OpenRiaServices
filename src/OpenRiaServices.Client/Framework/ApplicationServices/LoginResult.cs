using System;
using System.Security.Principal;

namespace OpenRiaServices.DomainServices.Client.ApplicationServices
{
    /// <summary>
    /// Result of an Login request
    /// </summary>
    public sealed class LoginResult : AuthenticationResult
    {
        private readonly bool _loginSuccess;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoginResult"/> class.
        /// </summary>
        /// <param name="user">The logged in user or <c>null</c> if authentication failed</param>
        /// <param name="loginSuccess">Whether the <c>Login</c> call completed successfully</param>
        /// <exception cref="InvalidOperationException"> is thrown if the <paramref name="user"/> is
        /// not authenticated when the <c>Login</c> call has finished successfully.
        /// </exception>
        public LoginResult(IPrincipal user, bool loginSuccess)
            : base(user)
        {
            if (loginSuccess && ((user == null) || (user.Identity == null) || !user.Identity.IsAuthenticated))
            {
                throw new InvalidOperationException(Resources.ApplicationServices_LoginSuccessRequiresAuthN);
            }
            this._loginSuccess = loginSuccess;
        }

        /// <summary>
        /// Gets a value indicating whether the <c>Login</c> call completed successfully.
        /// </summary>
        public bool LoginSuccess
        {
            get { return this._loginSuccess; }
        }
    }
}
