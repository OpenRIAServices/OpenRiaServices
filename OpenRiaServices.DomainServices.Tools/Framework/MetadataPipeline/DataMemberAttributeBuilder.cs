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
        /// name is "Order" or "IsNameSetExplicitly".</remarks>
        protected override string MapProperty(PropertyInfo propertyInfo, Attribute attribute)
        {
            if (propertyInfo.Name.Equals("Order", StringComparison.Ordinal))
                return null;

            // .NET Framework 4.6 added the IsNameSetExplicitly property to the DataMemberAttribute class.
            // Because it is a read-only property, we want to suppress codegen.
            if (propertyInfo.Name.Equals("IsNameSetExplicitly", StringComparison.Ordinal))
                return null;

            return base.MapProperty(propertyInfo, attribute);
        }
    }
}
