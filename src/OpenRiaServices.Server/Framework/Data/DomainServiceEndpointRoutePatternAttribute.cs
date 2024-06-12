using System;

namespace OpenRiaServices.Server
{
#if NET
    /// <summary>
    /// Determine how endpoints routes (Uris to access DomainServices) are generated
    /// </summary>
    /// <remarks>
    /// IMPORTANT: If any value is changed here, then the corresponding value must be changed in "OpenRiaServices.Tools.EndpointRoutePattern"
    /// </remarks>
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

    /// <summary>
    /// Attribute to configure the pattern used for endpoint, see <see cref="Server.EndpointRoutePattern"/>
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = true)]
    public sealed class DomainServiceEndpointRoutePatternAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DomainServiceEndpointRoutePatternAttribute"/> class.
        /// </summary>
        /// <param name="endpointRoutePattern"><see cref="Server.EndpointRoutePattern"/> to use</param>
        public DomainServiceEndpointRoutePatternAttribute(EndpointRoutePattern endpointRoutePattern)
            => EndpointRoutePattern = endpointRoutePattern;

        /// <summary>
        /// <see cref="EndpointRoutePattern"/> that should be used
        /// </summary>
        public EndpointRoutePattern EndpointRoutePattern { get; }
    }
#endif
}
