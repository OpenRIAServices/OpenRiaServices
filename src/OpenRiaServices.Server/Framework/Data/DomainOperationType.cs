namespace OpenRiaServices.Server
{
    /// <summary>
    /// Enumeration of the core operations a <see cref="DomainService"/> can perform.
    /// </summary>
    public enum DomainOperationType
    {
        /// <summary>
        /// Indicates a query operation.
        /// </summary>
        Query = 0,

        /// <summary>
        /// Indicates a submit operation.
        /// </summary>
        Submit = 1,

        /// <summary>
        /// Indicates an invoke operation.
        /// </summary>
        Invoke = 2,

        /// <summary>
        /// Indicates a metadata analysis operation
        /// </summary>
        Metadata = 3
    }
}
