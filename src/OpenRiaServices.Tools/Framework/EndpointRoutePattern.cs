namespace OpenRiaServices.Tools
{
#if NET
    /// <summary>
    /// IMPORTANT: THIS IS AN EXACT copy of <see cref="Server.EndpointRoutePattern"/> where all values are identical.
    /// We don't use the server version in the options because we don't want to load in the Server assembly until a bit later
    /// after the options are created.
    /// </summary>
    public enum EndpointRoutePattern
    {
        /// <summary>
        /// Enpoints routes match "My-Namespace-TypeName/MethodName"
        /// </summary>
        FullName,

        /// <summary>
        /// Enpoints routes match "TypeName/MethodName"
        /// </summary>
        Name,

        /// <summary>
        /// Enpoints routes match "My-Namespace-TypeName.svc/binary/MethodName" which is the same schema as in WCF hosting (and old WCF RIA Services)
        /// </summary>
        WCF
    }
#endif
}
