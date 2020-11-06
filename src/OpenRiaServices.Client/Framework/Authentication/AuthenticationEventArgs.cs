﻿using System;
using System.Security.Principal;

namespace OpenRiaServices.Client.Authentication
{
    /// <summary>
    /// Event args for events raised by the <see cref="AuthenticationService"/> class.
    /// </summary>
    public sealed class AuthenticationEventArgs : EventArgs
    {
        /// <summary>
        /// Initiailizes a new instance of the <see cref="AuthenticationEventArgs"/> class.
        /// </summary>
        /// <param name="user">The user at the time the event occurred</param>
        /// <exception cref="ArgumentNullException"> is thrown if <paramref name="user"/> is <c>null</c>.
        /// </exception>
        internal AuthenticationEventArgs(IPrincipal user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            this.User = user;
        }

        /// <summary>
        /// Gets the user at the time the event occurred
        /// </summary>
        public IPrincipal User { get; }
    }
}
