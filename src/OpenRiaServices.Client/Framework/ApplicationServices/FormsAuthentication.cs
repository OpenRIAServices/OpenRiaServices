namespace OpenRiaServices.DomainServices.Client.ApplicationServices
{
    /// <summary>
    /// <see cref="AuthenticationService"/> that performs Forms authentication using
    /// a <see cref="OpenRiaServices.DomainServices.Client.DomainContext"/> generated from a domain service
    /// implementing <c>OpenRiaServices.DomainServices.Server.Authentication.IAuthentication&lt;T&gt;</c>.
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
