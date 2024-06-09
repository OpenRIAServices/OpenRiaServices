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
            AttributeDeclaration attributeDeclaration = new AttributeDeclaration(typeof(EntityAssociationAttribute));

            if (attribute is EntityAssociationAttribute entityAssociation)
            {
                attributeDeclaration.ConstructorArguments.Add(entityAssociation.Name);
                attributeDeclaration.ConstructorArguments.Add((string[])entityAssociation.ThisKeyMembers);
                attributeDeclaration.ConstructorArguments.Add((string[])entityAssociation.OtherKeyMembers);
            }
            //else if (attribute is AssociationAttribute associationAttribute)
            //{
            //    // [AssociationAttribute( {true|false} )]
            //    attributeDeclaration.ConstructorArguments.Add(associationAttribute.Name);
            //    attributeDeclaration.ConstructorArguments.Add(associationAttribute.ThisKeyMembers.ToArray());
            //    attributeDeclaration.ConstructorArguments.Add(associationAttribute.OtherKeyMembers.ToArray());
            //}
            else
            {
                return null;
            }

            return attributeDeclaration;
        }
    }
}
