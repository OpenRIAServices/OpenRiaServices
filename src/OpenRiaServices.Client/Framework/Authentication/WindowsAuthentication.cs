using System;
using System.Threading;
using System.Threading.Tasks;

namespace OpenRiaServices.Client.Authentication
{
    /// <summary>
    /// <see cref="AuthenticationService"/> that performs Windows authentication using
    /// a <see cref="OpenRiaServices.Client.DomainContext"/> generated from a domain service
    /// implementing <c>OpenRiaServices.Server.Authentication.IAuthentication&lt;T&gt;</c>.
    /// </summary>
    public class WindowsAuthentication : WebAuthenticationService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsAuthentication"/> class.
        /// </summary>
        public WindowsAuthentication() { }

        /// <summary>
        /// <c>Login</c> is not an operation supported for Windows authentication
        /// </summary>
        /// <param name="parameters">The parameter is not used.</param>
        /// <param name="cancellationToken">The parameter is not used.</param>
        /// <returns>The result.</returns>
        /// <exception cref="NotSupportedException"> is always thrown.</exception>

        protected internal override Task<LoginResult> LoginAsync(LoginParameters parameters, CancellationToken cancellationToken)
        {
            throw new NotSupportedException(Resources.ApplicationServices_WANoLogin);
        }

        /// <summary>
        /// <c>Logout</c> is not an operation supported for Windows authentication
        /// </summary>
        /// <param name="cancellationToken">The parameter is not used.</param>
        /// <returns>The result.</returns>
        /// <exception cref="NotSupportedException"> is always thrown.</exception>
        protected internal override Task<LogoutResult> LogoutAsync(CancellationToken cancellationToken)
        {
            throw new NotSupportedException(Resources.ApplicationServices_WANoLogout);
        }
    }
}
