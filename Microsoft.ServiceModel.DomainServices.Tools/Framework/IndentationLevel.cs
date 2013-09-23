namespace OpenRiaServices.DomainServices.Tools
{
    /// <summary>
    /// This enum is designed to be used with type snippet generators.
    /// </summary>
    internal enum IndentationLevel
    {
        /// <summary>
        /// No indentation at all.
        /// </summary>
        None,

        /// <summary>
        /// Type declared in the global namespace (no namespace)
        /// </summary>
        GlobalNamespace,

        /// <summary>
        /// Type declared inside a namespace.
        /// </summary>
        Namespace,

        /// <summary>
        /// Type declared inside another type in the global namespace.
        /// </summary>
        NestedType,

        /// <summary>
        /// Type declared inside another type that belongs to a namespace.
        /// </summary>
        NamespaceNestedType
    }
}