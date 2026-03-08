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
    internal class LegacyAssociationAttributeCompatTypeDescriptor : CustomTypeDescriptor
    {
        private PropertyDescriptorCollection _properties;

        public LegacyAssociationAttributeCompatTypeDescriptor(ICustomTypeDescriptor parent)
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
                    if (propDescriptor.Attributes[typeof(AssociationAttribute)] is not null)
                    {
                        // Copy attributes but replace AssociationAttribute with EntityAssociationAttribute
                        var newAttributes = new Attribute[propDescriptor.Attributes.Count];
                        propDescriptor.Attributes.CopyTo(newAttributes, 0);
                        for (int i = 0; i < newAttributes.Length; ++i)
                        {
                            if (newAttributes[i] is AssociationAttribute associationAttribute)
                            {
                                newAttributes[i] = new EntityAssociationAttribute(associationAttribute.Name, associationAttribute.ThisKeyMembers.ToArray(), associationAttribute.OtherKeyMembers.ToArray())
                                {
                                    IsForeignKey = associationAttribute.IsForeignKey
                                };
                                // AllowMultiple = false so break after we find an instance
                                break;
                            }
                        }

                        tempPropertyDescriptors.Add(DomainPropertyDescriptor.CreateWithExplicitAttributes(propDescriptor, newAttributes));
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
#pragma warning disable CS0618 // Type or member is obsolete (AssociationAttribute)
                if (pd.Attributes[typeof(AssociationAttribute)] is not null)
                {
                    return true;
                }
#pragma warning restore CS0618 // Type or member is obsolete
            }
            return false;
        }
    }
}
