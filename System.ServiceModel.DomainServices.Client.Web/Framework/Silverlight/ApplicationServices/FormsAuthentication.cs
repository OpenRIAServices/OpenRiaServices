namespace System.ServiceModel.DomainServices.Client.ApplicationServices
{
    /// <summary>
    /// <see cref="AuthenticationService"/> that performs Forms authentication using
    /// a <see cref="System.ServiceModel.DomainServices.Client.DomainContext"/> generated from a domain service
    /// implementing <c>System.ServiceModel.DomainServices.Server.ApplicationServices.IAuthentication&lt;T&gt;</c>.
    /// </summary>
    public class FormsAuthentication : WebAuthenticationService
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="FormsAuthentication"/> class.
        /// </summary>
        public FormsAuthentication() 
        { 
        }

        #endregion
    }
}
