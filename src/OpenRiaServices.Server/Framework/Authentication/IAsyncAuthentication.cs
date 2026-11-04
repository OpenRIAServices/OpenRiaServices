using System.Threading.Tasks;

namespace OpenRiaServices.Server.Authentication
{
    /// <summary>
    /// An interface for a <see cref="DomainService"/> that encapsulates the authentication domain. A
    /// domain service implementing this interface will be used to populate the user on the client.
    /// </summary>
    /// <remarks>
    /// <c>OpenRiaServices.Client.Authentication.WebAuthenticationService</c>
    /// will work with the <c>DomainContext</c> generated for any domain service implementing this
    /// interface.
    /// <para>
    /// <see cref="UpdateUserAsync"/> is designed as an update method, and will be invoked via
    /// <c>SubmitChanges</c> on the client. This has a couple implications. First,
    /// invoking <see cref="UpdateUserAsync"/> via <c>AuthenticationService.SaveUser</c> will submit all 
    /// changes that have occurred in the <c>DomainContext</c> and may invoke other update methods.
    /// Second, invoking other update methods on the <c>DomainContext</c> from the client will submit
    /// all changes and may invoke <see cref="UpdateUserAsync"/>.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">The type of the user entity</typeparam>
    [AuthenticationService]
    public interface IAuthenticationAsync<T> where T : IUser
    {
        /// <summary>
        /// Authenticates and returns the user with the specified name and password.
        /// </summary>
        /// <remarks>
        /// This method will return a single user if the 
        /// authentication was successful. If the user could not be authenticated, <c>null</c>
        /// will be returned.
        /// </remarks>
        /// <param name="userName">The userName associated with the user to authenticate</param>
        /// <param name="password">The password associated with the user to authenticate</param>
        /// <param name="isPersistent">Whether the authentication should persist between sessions</param>
        /// <param name="customData">Optional implementation-specific data</param>
        /// <returns>A single user or <c>null</c> if authentication failed</returns>
        [Query(IsComposable = false, HasSideEffects = true)]
        Task<T> LoginAsync(string userName, string password, bool isPersistent, string customData);

        /// <summary>
        /// Logs an authenticated user out.
        /// </summary>
        /// <remarks>
        /// This method will return a single, anonymous user.
        /// </remarks>
        /// <returns>A single, default user.</returns>
        [Query(IsComposable = false, HasSideEffects = true)]
        Task<T> LogoutAsync();

        /// <summary>
        /// Gets the principal and profile for the current user.
        /// </summary>
        /// <remarks>
        /// This method will return a single user. If the user is not
        /// authenticated, an anonymous user will be returned.
        /// </remarks>
        /// <returns>An enumerable with a single user.</returns>
        [Query(IsComposable = false)]
        Task<T> GetUserAsync();

        /// <summary>
        /// Updates the profile for the authenticated user.
        /// </summary>
        /// <param name="user">The updated user</param>
        /// <exception cref="System.UnauthorizedAccessException"> is thrown if the authenticated 
        /// user does not have the correct permissions to update the profile.
        /// </exception>
        [Update]
        [RequiresAuthentication]
        Task UpdateUserAsync(T user);
    }
}
