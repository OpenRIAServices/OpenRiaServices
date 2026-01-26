using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace OpenRiaServices.Tools
{
    /// <summary>
    /// Custom attribute builder generates <see cref="AttributeDeclaration"/> representations of
    /// <see cref="EntityAssociationAttribute"/> instances.
    /// </summary>
    internal class EntityAssociationAttributeBuilder : ICustomAttributeBuilder
    {
        /// <summary>
        /// Generates a <see cref="AttributeDeclaration"/> representation of an 
        /// <see cref="EntityAssociationAttribute"/> instance.
        /// </summary>
        public AttributeDeclaration GetAttributeDeclaration(Attribute attribute)
        {
            string name;
            string[] thisKey, otherKey;
            bool isForeignKey;

            if (attribute is EntityAssociationAttribute entityAssociation)
            {
                name = entityAssociation.Name;
                thisKey = (string[])entityAssociation.ThisKeyMembers;
                otherKey = (string[])entityAssociation.OtherKeyMembers;
                isForeignKey = entityAssociation.IsForeignKey;
            }
            else if (attribute is AssociationAttribute associationAttribute)
            {
                name = associationAttribute.Name;
                thisKey = associationAttribute.ThisKeyMembers.ToArray();
                otherKey = associationAttribute.OtherKeyMembers.ToArray();
                isForeignKey = associationAttribute.IsForeignKey;
            }
            else
            {
                return null;
            }

            // Generate the attribute declaration
            // If there is only a single key member, we use string based constructor
            AttributeDeclaration attributeDeclaration = new AttributeDeclaration(typeof(EntityAssociationAttribute));
            attributeDeclaration.ConstructorArguments.Add(name);
            if (thisKey.Length == 1 && otherKey.Length == 1)
            {
                attributeDeclaration.ConstructorArguments.Add(thisKey[0]);
                attributeDeclaration.ConstructorArguments.Add(otherKey[0]);
            }
            else
            {
                attributeDeclaration.ConstructorArguments.Add(thisKey);
                attributeDeclaration.ConstructorArguments.Add(otherKey);
            }

            if (isForeignKey)
            {
                attributeDeclaration.NamedParameters.Add(nameof(EntityAssociationAttribute.IsForeignKey), true);
            }
            return attributeDeclaration;
        }
    }
}
