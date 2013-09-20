namespace System.ServiceModel.DomainServices.Server
{
    /// <summary>
    /// Enumeration of the types of update operations that
    /// can be performed on an object.
    /// </summary>
    public enum ChangeOperation
    {
        /// <summary>
        /// No update to perform
        /// </summary>
        None,

        /// <summary>
        /// An Insert operation
        /// </summary>
        Insert,

        /// <summary>
        /// An Update operation
        /// </summary>
        Update,

        /// <summary>
        /// A Delete operation
        /// </summary>
        Delete
    }
}
