using System;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.ServiceModel.DomainServices.Tools
{
    /// <summary>
    /// Derived custom attribute builder that special-cases the [Display] attribute
    /// </summary>
    internal class DisplayCustomAttributeBuilder : StandardCustomAttributeBuilder
    {
        public override AttributeDeclaration GetAttributeDeclaration(Attribute attribute)
        {
            DisplayAttribute displayAttribute = (DisplayAttribute)attribute;
            AttributeDeclaration attributeDeclaration = new AttributeDeclaration(typeof(DisplayAttribute));

            // By convention, the attribute parameters are not validated until an attempt is made to
            // access the resources.  We do the following probe merely to trigger this validation process.
            // An InvalidOperationException will be thrown if the attribute is ill-formed.
            Type attributeType = attribute.GetType();

            try
            {
                displayAttribute.GetName();
            }
            catch (InvalidOperationException ex)
            {
                throw new AttributeBuilderException(ex, attributeType, "Name");
            }

            try
            {
                displayAttribute.GetShortName();
            }
            catch (InvalidOperationException ex)
            {
                throw new AttributeBuilderException(ex, attributeType, "ShortName");
            }

            try
            {
                displayAttribute.GetDescription();
            }
            catch (InvalidOperationException ex)
            {
                throw new AttributeBuilderException(ex, attributeType, "Description");
            }

            try
            {
                displayAttribute.GetPrompt();
            }
            catch (InvalidOperationException ex)
            {
                throw new AttributeBuilderException(ex, attributeType, "Prompt");
            }

            // Add AutoGenerateField
            if (displayAttribute.GetAutoGenerateField().HasValue)
            {
                attributeDeclaration.NamedParameters.Add("AutoGenerateField", displayAttribute.AutoGenerateField);
            }

            // Add AutoGenerateFilter
            if (displayAttribute.GetAutoGenerateFilter().HasValue)
            {
                attributeDeclaration.NamedParameters.Add("AutoGenerateFilter", displayAttribute.AutoGenerateFilter);
            }

            // Add Description
            if (!string.IsNullOrEmpty(displayAttribute.Description))
            {
                attributeDeclaration.NamedParameters.Add("Description", displayAttribute.Description);
            }

            // Add GroupName
            if (!string.IsNullOrEmpty(displayAttribute.GroupName))
            {
                attributeDeclaration.NamedParameters.Add("GroupName", displayAttribute.GroupName);
            }

            // Add Name
            if (!string.IsNullOrEmpty(displayAttribute.Name))
            {
                attributeDeclaration.NamedParameters.Add("Name", displayAttribute.Name);
            }

            // Add Order
            if (displayAttribute.GetOrder().HasValue)
            {
                attributeDeclaration.NamedParameters.Add("Order", displayAttribute.Order);
            }

            // Add Prompt
            if (!string.IsNullOrEmpty(displayAttribute.Prompt))
            {
                attributeDeclaration.NamedParameters.Add("Prompt", displayAttribute.Prompt);
            }

            // Add ResourceType
            if (displayAttribute.ResourceType != null)
            {
                attributeDeclaration.NamedParameters.Add("ResourceType", displayAttribute.ResourceType);
            }

            // Add ShortName
            if (!string.IsNullOrEmpty(displayAttribute.ShortName))
            {
                attributeDeclaration.NamedParameters.Add("ShortName", displayAttribute.ShortName);
            }

            return attributeDeclaration;
        }
    }
}
