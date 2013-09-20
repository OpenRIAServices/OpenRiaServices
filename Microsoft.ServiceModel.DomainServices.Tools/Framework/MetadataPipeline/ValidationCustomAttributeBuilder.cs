using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;

namespace Microsoft.ServiceModel.DomainServices.Tools
{
    /// <summary>
    /// Standard custom attribute builder for ValidationAttributes containing globalized resources.
    /// </summary>
    /// <remarks>
    /// This subclass of StandardCustomAttributeBuilder is aware of the subset of attributes
    /// that contain globalized resources and validates that they are correct prior to generating code
    /// </remarks>
    internal class ValidationCustomAttributeBuilder : StandardCustomAttributeBuilder
    {
        /// <summary>
        /// Returns a representative <see cref="AttributeDeclaration"/> for a given <see cref="Attribute"/> instance.
        /// </summary>
        /// <param name="attribute">An attribute instance to create a <see cref="AttributeDeclaration"/> for.</param>
        /// <returns>A <see cref="AttributeDeclaration"/> representing the <paramref name="attribute"/>.</returns>
        public override AttributeDeclaration GetAttributeDeclaration(Attribute attribute)
        {
            ValidationAttribute validationAttribute = (ValidationAttribute)attribute;
            AttributeDeclaration attributeDeclaration = base.GetAttributeDeclaration(attribute);

            RegisterSharedResources(validationAttribute, attributeDeclaration);

            return attributeDeclaration;
        }

        /// <summary>
        /// Registers any resource type used by the given <see cref="ValidationAttribute"/> that must be shared and 
        /// have a named resource property available.
        /// </summary>
        /// <param name="validationAttribute">The <see cref="ValidationAttribute"/> instance to check.</param>
        /// <param name="attributeDeclaration">The <see cref="AttributeDeclaration"/> used to describe <paramref name="validationAttribute"/>.</param>
        protected static void RegisterSharedResources(ValidationAttribute validationAttribute, AttributeDeclaration attributeDeclaration)
        {
            string resourceName = validationAttribute.ErrorMessageResourceName;
            Type resourceType = validationAttribute.ErrorMessageResourceType;

            bool isEmptyResourceName = string.IsNullOrEmpty(resourceName);
            Type validateionAttributeType = validationAttribute.GetType();

            // At least one is non-null.  If the other is null, we have a problem.  We need both to
            // localize properly, or neither to signal we use the static string version
            if ((resourceType != null && isEmptyResourceName) || (resourceType == null && !isEmptyResourceName))
            {
                string resourceTypeMessage = resourceType != null ? resourceType.Name : Resource.Unspecified_Resource_Element;
                string resourceNameMessage = isEmptyResourceName ? Resource.Unspecified_Resource_Element : resourceName;

                attributeDeclaration.Errors.Add(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resource.ClientCodeGen_ValidationAttribute_Requires_ResourceType_And_Name,
                        validateionAttributeType,
                        resourceTypeMessage,
                        resourceNameMessage));
                return;
            }

            if (resourceType != null)
            {
                PropertyInfo resourceProperty = resourceType.GetProperty(resourceName);

                if (resourceProperty == null)
                {
                    attributeDeclaration.Errors.Add(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            Resource.ClientCodeGen_ValidationAttribute_ResourcePropertyNotFound,
                            validateionAttributeType,
                            resourceName,
                            resourceType));
                }
                else
                {
                    attributeDeclaration.RequiredTypes.Add(resourceType);
                    attributeDeclaration.RequiredProperties.Add(resourceProperty);
                }
            }
        }
    }
}
