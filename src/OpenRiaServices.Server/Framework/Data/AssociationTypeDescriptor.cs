using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace OpenRiaServices.Server
{
    /// <summary>
    /// Custom TypeDescriptor that adds an inferred EntityAssociationAttribute for
    /// members that are annotated with the obsolete AssociationAttribute.
    /// </summary>
    internal class AssociationTypeDescriptor : CustomTypeDescriptor
    {
        private PropertyDescriptorCollection _properties;

        public AssociationTypeDescriptor(ICustomTypeDescriptor parent)
            : base(parent)
        {
        }

        public override PropertyDescriptorCollection GetProperties()
        {
            if (this._properties == null)
            {
                PropertyDescriptorCollection originalCollection = base.GetProperties();

                // Cache early to avoid reentrancy issues
                this._properties = originalCollection;

                bool customDescriptorsCreated = false;
                List<PropertyDescriptor> tempPropertyDescriptors = new List<PropertyDescriptor>();

                foreach (PropertyDescriptor propDescriptor in originalCollection)
                {
                    // If the property has the obsolete AssociationAttribute, create an EntityAssociationAttribute
#pragma warning disable CS0618 // Type or member is obsolete
                    if (propDescriptor.Attributes[typeof(AssociationAttribute)] is AssociationAttribute associationAttribute)
                    {
                        var entityAssoc = new EntityAssociationAttribute(associationAttribute.Name, associationAttribute.ThisKeyMembers.ToArray(), associationAttribute.OtherKeyMembers.ToArray())
                        {
                            IsForeignKey = associationAttribute.IsForeignKey
                        };

                        tempPropertyDescriptors.Add(new DomainPropertyDescriptor(propDescriptor, new Attribute[] { entityAssoc }));
                        customDescriptorsCreated = true;
                    }
                    else
#pragma warning restore CS0618 // Type or member is obsolete
                    {
                        tempPropertyDescriptors.Add(propDescriptor);
                    }
                }

                if (customDescriptorsCreated)
                {
                    this._properties = new PropertyDescriptorCollection(tempPropertyDescriptors.ToArray(), true);
                }
            }

            return this._properties;
        }

        /// <summary>
        /// Returns true if the provided descriptor has any properties decorated with the obsolete
        /// <see cref="AssociationAttribute"/> and therefore should be wrapped by this descriptor.
        /// </summary>
        internal static bool ShouldRegister(ICustomTypeDescriptor descriptor)
        {
            foreach (PropertyDescriptor pd in descriptor.GetProperties())
            {
#pragma warning disable CS0618 // Type or member is obsolete
                if (pd.Attributes[typeof(AssociationAttribute)] != null)
                {
                    return true;
                }
#pragma warning restore CS0618 // Type or member is obsolete
            }
            return false;
        }
    }
}
