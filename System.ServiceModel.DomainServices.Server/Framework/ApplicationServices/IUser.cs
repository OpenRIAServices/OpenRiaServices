using System.Collections.Generic;

namespace OpenRiaServices.DomainServices.Server.ApplicationServices
{
    /// <summary>
    /// Interface for user entities that has properties for passing principal values to the client.
    /// </summary>
    /// <remarks>
    /// This class is designed for use with the <see cref="IAuthentication{T}"/> interface.
    /// It provides properties to support serialization of principal values to an entity class
    /// generated from a type implementing this interface.
    /// </remarks>
    public interface IUser
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <remarks>
        /// <see cref="Name"/> may be null or empty to support anonymous users.
        /// </remarks>
        string Name { get; set; }

        /// <summary>
        /// Gets or sets the roles the user is a member of.
        /// </summary>
        /// <remarks>
        /// <see cref="Roles"/> may be <c>null</c>.
        /// </remarks>
        IEnumerable<string> Roles { get; set; }
    }
}
