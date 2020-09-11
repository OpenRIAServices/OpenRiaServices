using System;
using System.ComponentModel.DataAnnotations;

namespace OpenRiaServices.Tools
{
    /// <summary>
    /// Custom attribute builder generates <see cref="AttributeDeclaration"/> representations of
    /// <see cref="EditableAttribute"/> instances.
    /// </summary>
    internal class EditableAttributeBuilder : StandardCustomAttributeBuilder
    {
        /// <summary>
        /// Generates a <see cref="AttributeDeclaration"/> representation of an 
        /// <see cref="EditableAttribute"/> instance.
        /// </summary>
        /// <param name="attribute">The <see cref="EditableAttribute"/>.</param>
        /// <returns>A <see cref="AttributeDeclaration"/> representation of 
        /// <paramref name="attribute"/>.</returns>
        /// <exception cref="InvalidCastException">if <paramref name="attribute"/> is 
        /// not a <see cref="EditableAttribute"/>.</exception>
        public override AttributeDeclaration GetAttributeDeclaration(Attribute attribute)
        {
            EditableAttribute editableAttribute = (EditableAttribute)attribute;
            AttributeDeclaration attributeDeclaration = new AttributeDeclaration(typeof(EditableAttribute));

            bool allowEdit = editableAttribute.AllowEdit;
            bool allowInitialValue = editableAttribute.AllowInitialValue;

            // [EditableAttribute( {true|false} )]
            attributeDeclaration.ConstructorArguments.Add(allowEdit);

            // Only add the 'AllowInitialValue' parameter if its value does not match with
            // the 'AllowEdit' value.  See the documentation of EditableAttribute for more info.
            if (allowEdit != allowInitialValue)
            {
                // [EditableAttribute( {true|false}, AllowInitialValue = {true|false} )]
                attributeDeclaration.NamedParameters.Add("AllowInitialValue", allowInitialValue);
            }

            return attributeDeclaration;
        }
    }
}
