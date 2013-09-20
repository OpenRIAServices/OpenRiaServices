namespace System.ServiceModel.DomainServices.Server
{
    /// <summary>
    /// Attribute applied to an association member to indicate that the associated entities should be
    /// made available for client access.
    /// </summary>
    /// <remarks>
    /// When applied to an entity association, this attribute indicates that the association should be
    /// part of any code generated client entities, and that any related entities should be included when
    /// serializing results to the client. Note that it is up to the query method to make sure the associated
    /// entities are actually loaded. This attribute can also be used to specify member projections.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public sealed class IncludeAttribute : Attribute
    {
        private string _path;
        private string _memberName;

        /// <summary>
        /// Default constructor
        /// </summary>
        public IncludeAttribute()
        {
        }

        /// <summary>
        /// Constructor used to specify a member projection.
        /// </summary>
        /// <param name="path">Dotted path specifying the navigation path from the member this attribute
        /// is applied to, to the member to be projected. The projected member must be a scalar.</param>
        /// <param name="memberName">The member name for the projected member.</param>
        public IncludeAttribute(string path, string memberName)
        {
            this._path = path;
            this._memberName = memberName;
        }

        /// <summary>
        /// Gets a value indicating whether this attribute specifies a member projection
        /// </summary>
        /// <exception cref="InvalidOperationException"> is thrown if the current attribute is invalid.</exception>
        public bool IsProjection
        {
            get
            {
                // Either path or memberName is enough to cause validation.  Our convention
                // is that they must both be precisely null (not empty) or both non-null and not empty.
                bool isProjection = this._path != null || this._memberName != null;
                if (isProjection)
                {
                    this.ThrowIfAttributeNotValid();
                }
                return isProjection;
            }
        }

        /// <summary>
        /// Gets the member projection path
        /// </summary>
        public string Path
        {
            get
            {
                return this._path;
            }
        }

        /// <summary>
        /// Gets the name of the destination member for the projection 
        /// </summary>
        public string MemberName
        {
            get
            {
                return this._memberName;
            }
        }

#if !SILVERLIGHT
        /// <summary>
        /// Gets a unique identifier for this attribute.
        /// </summary>
        public override object TypeId
        {
            get
            {
                return this;
            }
        }
#endif

        /// <summary>
        /// Determines whether the current attribute instance is properly formed
        /// </summary>
        /// <param name="errorMessage">Error message returned to describe the problem</param>
        /// <returns><c>true</c> means it's valid</returns>
        private bool IsAttributeValid(out string errorMessage)
        {
            errorMessage = null;

            if (this._path != null || this._memberName != null)
            {
                if (string.IsNullOrEmpty(this._path))
                {
                    errorMessage = Resource.InvalidMemberProjection_EmptyPath;
                }
                if (string.IsNullOrEmpty(this._memberName))
                {
                    errorMessage = Resource.InvalidMemberProjection_EmptyMemberName;
                }
            }
            return errorMessage == null;
        }

        /// <summary>
        /// Throws InvalidOperationException is anything is wrong with the attribute
        /// </summary>
        private void ThrowIfAttributeNotValid()
        {
            string errorMessage = null;
            if (!this.IsAttributeValid(out errorMessage))
            {
                throw new InvalidOperationException(errorMessage);
            }
        }
    }
}
