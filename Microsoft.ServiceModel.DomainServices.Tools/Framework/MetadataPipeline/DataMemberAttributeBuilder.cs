using System;
using System.Reflection;
using System.Runtime.Serialization;

namespace OpenRiaServices.DomainServices.Tools
{
    /// <summary>
    /// DataMemberAttribute custom attribute builder.
    /// </summary>
    internal class DataMemberAttributeBuilder : StandardCustomAttributeBuilder
    {
        /// <summary>
        /// Overrides the MapProperty method to simply to remove Order from the list of properties to generate.
        /// </summary>
        /// <param name="propertyInfo">The getter property to consider</param>
        /// <param name="attribute">The current attribute instance we are considering</param>
        /// <returns>The name of the property we should use as the setter or null to suppress codegen.</returns>
        /// <remarks>Specifically for the <see cref="DataMemberAttribute"/> type, this method returns null if the property 
        /// name is "Order".</remarks>
        protected override string MapProperty(PropertyInfo propertyInfo, Attribute attribute)
        {
            return propertyInfo.Name.Equals("Order", StringComparison.Ordinal) ? null : base.MapProperty(propertyInfo, attribute);
        }
    }
}
