namespace System.ComponentModel.DataAnnotations.Schema
{
    /// <summary>
    /// The list of options that the <see cref="DatabaseGeneratedAttribute"/> may have to indicate the way a property is generated.
    /// </summary>
    internal enum DatabaseGeneratedOption
    {
        /// <summary>
        /// None indicates the property is not database generated.
        /// </summary>
        None,

        /// <summary>
        /// Identity indicates the value is generated on insert and remains unchanged on update.
        /// </summary>
        Identity,

        /// <summary>
        /// Computed indicates the value is generated on both insert and update.
        /// </summary>
        Computed
    }
}