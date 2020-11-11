namespace OpenRiaServices.Hosting.WCF.Behaviors
{
    /// <summary>
    /// Encapsulates the settings the user specified for the query method.
    /// </summary>
    internal interface IQueryOperationSettings
    {
        /// <summary>
        /// Gets a value indicating whether the query method has side-effects.
        /// </summary>
        bool HasSideEffects { get; }
    }
}
