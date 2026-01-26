using System;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace OpenRiaServices.Tools
{
    /// <summary>
    /// A collection of code generation utilities.
    /// </summary>
    internal static partial class SharedCodeGenUtilities
    {
        public static EndpointRoutePattern? TryGetRoutePatternFromAssembly(Assembly assembly)
        {
#if NET
            if(assembly?.CustomAttributes.FirstOrDefault(cad => cad.AttributeType.FullName ==  "OpenRiaServices.Hosting.AspNetCore.DomainServiceEndpointRoutePatternAttribute")
                        is { ConstructorArguments: [var constructorArgument] } )
            {
                return (EndpointRoutePattern)Convert.ToInt32(constructorArgument.Value, CultureInfo.InvariantCulture);
            }
            return null;
#else
            return EndpointRoutePattern.WCF;
#endif
        }
    }
}
