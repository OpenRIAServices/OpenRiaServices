using System;
using System.Reflection;

namespace OpenRiaServices.Tools
{
    internal sealed class UseComparerAttributeBuilder : StandardCustomAttributeBuilder
    {
        public override AttributeDeclaration GetAttributeDeclaration(Attribute attribute)
        {
            ArgumentNullException.ThrowIfNull(attribute);

            Type attributeType = attribute.GetType();
            PropertyInfo comparerTypeProperty = attributeType.GetProperty("ComparerType");
            PropertyInfo memberNameProperty = attributeType.GetProperty("MemberName");
            if (comparerTypeProperty is null || memberNameProperty is null)
            {
                return null;
            }

            AttributeDeclaration declaration = new(attributeType);
            declaration.ConstructorArguments.Add(comparerTypeProperty.GetValue(attribute));
            if (memberNameProperty.GetValue(attribute) is string memberName)
            {
                declaration.ConstructorArguments.Add(memberName);
            }

            return declaration;
        }
    }
}
