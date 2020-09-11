using System;
using System.Reflection;

namespace OpenRiaServices.Tools
{
    /// <summary>
    /// DomainIdentifierAttribute custom attribute builder.
    /// </summary>
    internal class DomainIdentifierAttributeBuilder : StandardCustomAttributeBuilder
    {
        /// <summary>
        /// Overrides the MapProperty method to simply to remove CodeProcessor from the list of properties to generate.
        /// </summary>
        /// <param name="propertyInfo">The getter property to consider</param>
        /// <param name="attribute">The current attribute instance we are considering</param>
        /// <returns>The name of the property we should use as the setter or null to suppress codegen.</returns>
        /// <remarks>Specifically for the <see cref="OpenRiaServices.DomainIdentifierAttribute"/> type, this method returns null if the property 
        /// name is "CodeProcessor".</remarks>
        protected override string MapProperty(PropertyInfo propertyInfo, Attribute attribute)
        {
            return propertyInfo.Name.Equals("CodeProcessor", StringComparison.Ordinal) ? null : base.MapProperty(propertyInfo, attribute);
        }
    }
}
