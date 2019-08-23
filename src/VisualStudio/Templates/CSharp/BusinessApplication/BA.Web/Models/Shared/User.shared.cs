﻿namespace $safeprojectname$
{
    /// <summary>
    /// Partial class extending the User type that adds shared properties and methods.
    /// These properties and methods will be available both to the server app and the client app.
    /// </summary>
    public partial class User
    {
        /// <summary>
        /// Returns the user display name, which by default is its FriendlyName.
        /// If FriendlyName is not set, the User Name is returned.
        /// </summary>
        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrEmpty(this.FriendlyName))
                {
                    return this.FriendlyName;
                }
                else
                {
                    return this.Name;
                }
            }
        }
    }
}
