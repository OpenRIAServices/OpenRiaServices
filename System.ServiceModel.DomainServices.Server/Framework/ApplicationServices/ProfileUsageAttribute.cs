namespace System.ServiceModel.DomainServices.Server.ApplicationServices
{
    /// <summary>
    /// Attribute that describes how a property is backed in an ASP.NET profile. It can 
    /// be used with user entities extending <see cref="UserBase"/>.
    /// </summary>
    /// <remarks>
    /// This attribute is used by the <see cref="AuthenticationBase{T}"/> to determine
    /// how it should read from or write to the ASP.NET profile that backs the data.
    /// If a property is in the profile with the same name, then this attribute does not
    /// need to be used. If a property is not in the profile then <see cref="IsExcluded"/>
    /// should be set to <c>true</c>. If a property has been named differently than the
    /// profile value that backs it, then <see cref="Alias"/> should be set to the name
    /// of the backing value in the profile.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class ProfileUsageAttribute : Attribute
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfileUsageAttribute"/> class
        /// </summary>
        public ProfileUsageAttribute() { }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the name of the member backing the property in an ASP.NET profile.
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the property is backed by a profile member
        /// </summary>
        public bool IsExcluded { get; set; }

        #endregion
    }
}
