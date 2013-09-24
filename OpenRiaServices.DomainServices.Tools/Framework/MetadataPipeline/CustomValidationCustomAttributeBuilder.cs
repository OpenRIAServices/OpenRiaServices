using System;
using System.ComponentModel.DataAnnotations;

namespace OpenRiaServices.DomainServices.Tools
{
    /// <summary>
    /// Derived custom attribute builder to deal with CustomValidationAttribute.
    /// </summary>
    /// <remarks>This class exists primarily to do type-checking during code generation.  A [CustomValidation]
    /// attribute is flagged as an error if the named type and validation methods cannot be located. If the 
    /// validator type is visible to the client the attribute will be propagated.
    /// </remarks>
    internal class CustomValidationCustomAttributeBuilder : ValidationCustomAttributeBuilder
    {
        public override AttributeDeclaration GetAttributeDeclaration(Attribute attribute)
        {
            CustomValidationAttribute cva = (CustomValidationAttribute)attribute;

            // Our convention is that parameter validation in the CVA occurs when it is
            // first used.  Simply ask the attribute to produce an error message, as this
            // will trigger an InvalidOperationException if the attribute is ill-formed
            cva.FormatErrorMessage(string.Empty);

            // Delegate to the base implementation to generate the attribute.
            // Note that the base implementation already checks that Types are 
            // shared so we do not perform that check here.
            AttributeDeclaration attributeDeclaration = base.GetAttributeDeclaration(attribute);

            attributeDeclaration.RequiredTypes.Add(cva.ValidatorType);
            attributeDeclaration.RequiredMethods.Add(cva.ValidatorType.GetMethod(cva.Method));

            return attributeDeclaration;
        }
    }
}
