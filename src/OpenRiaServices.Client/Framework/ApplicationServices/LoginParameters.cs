namespace OpenRiaServices.Client.ApplicationServices
{
    /// <summary>
    /// The parameters that specify the user to authentication in the <c>Login</c>
    /// methods on <see cref="AuthenticationService"/>.
    /// </summary>
    public class LoginParameters
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LoginParameters"/> class with default values.
        /// </summary>
        /// <remarks>
        /// This login will not persist.
        /// </remarks>
        public LoginParameters() : this(null, null, false, null) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="LoginParameters"/> class with the specified name 
        /// and password.
        /// </summary>
        /// <remarks>
        /// This login will not persist.
        /// </remarks>
        /// <param name="userName">The name of the user to be authenticated</param>
        /// <param name="password">The password of the user to be authenticated</param>
        public LoginParameters(string userName, string password) : this(userName, password, false, null) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="LoginParameters"/> class with the specified name,
        /// password, and persistence.
        /// </summary>
        /// <param name="userName">The name of the user to be authenticated</param>
        /// <param name="password">The password of the user to be authenticated</param>
        /// <param name="isPersistent">Whether the login should persist between sessions</param>
        /// <param name="customData">Optional implementation-specific data</param>
        public LoginParameters(string userName, string password, bool isPersistent, string customData)
        {
            this.UserName = userName;
            this.Password = password;
            this.IsPersistent = isPersistent;
            this.CustomData = customData;
        }

        /// <summary>
        /// Gets optional implementation-specific data.
        /// </summary>
        public string CustomData { get; }

        /// <summary>
        /// Gets a value indicating whether the login should persist between sessions.
        /// </summary>
        public bool IsPersistent { get; }

        /// <summary>
        /// Gets the password of the user to be authenticated.
        /// </summary>
        public string Password { get; }

        /// <summary>
        /// Gets the name of the user to be authenticated.
        /// </summary>
        public string UserName { get; }
    }
}
