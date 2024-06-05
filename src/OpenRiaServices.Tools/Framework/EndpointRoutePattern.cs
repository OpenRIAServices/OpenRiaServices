namespace OpenRiaServices.Tools
{
#if NET
    /// <summary>
    /// Determine how endpoints routes (Uris to access DomainServices) are generated
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
