namespace OpenRiaServices.Client.Authentication
{
    /// <summary>
    /// <see cref="AuthenticationService"/> that performs Forms authentication using
    /// a <see cref="OpenRiaServices.Client.DomainContext"/> generated from a domain service
    /// implementing <c>OpenRiaServices.Server.Authentication.IAuthentication&lt;T&gt;</c>.
    /// </summary>
    public class FormsAuthentication : WebAuthenticationService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FormsAuthentication"/> class.
        /// </summary>
        public FormsAuthentication()
        {
        }
    }
}
