using System;
using System.ComponentModel.DataAnnotations;

namespace OpenRiaServices.DomainServices.Tools
{
    /// <summary>
    /// Derived custom attribute builder that special-cases the [Range] attribute
    /// </summary>
    internal class RangeCustomAttributeBuilder : ValidationCustomAttributeBuilder
    {
        public override AttributeDeclaration GetAttributeDeclaration(Attribute attribute)
        {
            RangeAttribute rangeAttribute = (RangeAttribute)attribute;
            AttributeDeclaration attributeDeclaration = new AttributeDeclaration(typeof(RangeAttribute));

            // Register required resources for this ValidationAttribute
            RegisterSharedResources(rangeAttribute, attributeDeclaration);

            bool declareOperandType =
                rangeAttribute.Minimum == null ||
                rangeAttribute.Minimum.GetType() == typeof(string);

            // OperandType
            if (declareOperandType)
            {
                attributeDeclaration.ConstructorArguments.Add(rangeAttribute.OperandType);
            }

            // Minimum
            attributeDeclaration.ConstructorArguments.Add(rangeAttribute.Minimum);
            // Maximum
            attributeDeclaration.ConstructorArguments.Add(rangeAttribute.Maximum);

            // ErrorMessage
            if (rangeAttribute.ErrorMessage != null)
            {
                attributeDeclaration.NamedParameters.Add("ErrorMessage", rangeAttribute.ErrorMessage);
            }

            // ErrorMessageResourceType
            if (rangeAttribute.ErrorMessageResourceType != null)
            {
                attributeDeclaration.NamedParameters.Add("ErrorMessageResourceType", rangeAttribute.ErrorMessageResourceType);
            }

            // ErrorMessageResourceName
            if (rangeAttribute.ErrorMessageResourceName != null)
            {
                attributeDeclaration.NamedParameters.Add("ErrorMessageResourceName", rangeAttribute.ErrorMessageResourceName);
            }

            return attributeDeclaration;
        }
    }
}
