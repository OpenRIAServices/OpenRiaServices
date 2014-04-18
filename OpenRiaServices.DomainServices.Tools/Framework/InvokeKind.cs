namespace OpenRiaServices.DomainServices.Tools
{
    /// <summary>
    /// Defines the type of client side invoke operation to generate
    /// </summary>
    public enum InvokeKind
    {
        /// <summary>
        /// Generate invoke method without callback parameters
        /// </summary>
        WithoutCallback,
        /// <summary>
        /// Generate invoke method with callback and userstate parameters
        /// </summary>
        WithCallback,
        /// <summary>
        /// Generate "Async" invoke method returning Task and taking cancellation token as an extra parameter
        /// </summary>
        Async
    };
}
