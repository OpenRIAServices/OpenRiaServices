using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace OpenRiaServices.DomainServices.WindowsAzure
{
    /// <summary>
    /// <see cref="CustomTypeDescriptor"/> used to provide default metadata for entity types made available
    /// from a <see cref="TableDomainService{T}"/>.
    /// </summary>
    /// <remarks>
    /// Since each entity is required to extend <see cref="TableEntity"/>, they can use the default metadata
    /// declared in <see cref="TableEntityMetadata"/>.
    /// </remarks>
    internal class TableMetadataTypeDescriptor : CustomTypeDescriptor
    {
        private static readonly MemberInfo[] DefaultMetadataInfo = typeof(TableEntityMetadata).GetMembers();
        private static readonly Attribute[] DefaultMetadata = new Attribute[0];

        private readonly Type _componentType;

        /// <summary>
        /// Creates a new instance of the <see cref="TableMetadataTypeDescriptor"/>
        /// </summary>
        /// <param name="componentType">The component type</param>
        /// <param name="parent">The parent descriptor</param>
        public TableMetadataTypeDescriptor(Type componentType, ICustomTypeDescriptor parent)
            : base(parent)
        {
            this._componentType = componentType;
        }

        /// <summary>
        /// Returns the list of properties for this type.
        /// </summary>
        /// <returns>The list of properties for this type.</returns>
        public override PropertyDescriptorCollection GetProperties()
        {
            // Get properties from our parent
            PropertyDescriptorCollection originalCollection = base.GetProperties();

            List<PropertyDescriptor> tempPropertyDescriptors = new List<PropertyDescriptor>();

            // for every property exposed by our parent, see if we have additional metadata to add
            foreach (PropertyDescriptor propDescriptor in originalCollection)
            {
                Attribute[] newMetadata = TableMetadataTypeDescriptor.GetAdditionalMemberAttributes(propDescriptor).ToArray();
                if (newMetadata.Length > 0)
                {
                    tempPropertyDescriptors.Add(new MetadataPropertyDescriptorWrapper(propDescriptor, newMetadata));
                }
                else
                {
                    tempPropertyDescriptors.Add(propDescriptor);
                }
            }

            tempPropertyDescriptors.Add(new ETagPropertyDescriptor(this._componentType));

            return new PropertyDescriptorCollection(tempPropertyDescriptors.ToArray(), true);
        }

        /// <summary>
        /// Returns a collection of all the <see cref="Attribute"/>s we infer from the metadata associated
        /// with the metadata member corresponding to the given property descriptor
        /// </summary>
        /// <param name="pd">The property to get attributes for</param>
        /// <returns>a collection of attributes inferred from metadata in the given descriptor</returns>
        private static IEnumerable<Attribute> GetAdditionalMemberAttributes(PropertyDescriptor pd)
        {
            MemberInfo metadataInfo = TableMetadataTypeDescriptor.DefaultMetadataInfo.SingleOrDefault(m => m.Name == pd.Name);
            if (metadataInfo != null)
            {
                // Add attributes from the default metadata unless they have already been specified on the property
                List<Attribute> customAttributes = new List<Attribute>();
                foreach (Attribute attribute in metadataInfo.GetCustomAttributes(false))
                {
                    if (pd.Attributes[attribute.GetType()] == null)
                    {
                        customAttributes.Add(attribute);
                    }
                }
                return customAttributes;
            }
            else
            {
                return TableMetadataTypeDescriptor.DefaultMetadata;
            }
        }

        #region Nested Classes

        /// <summary>
        /// This class concatenates the attributes provided on construction with the base
        /// attributes of the specified PropertyDescriptor.
        /// </summary>
        private class MetadataPropertyDescriptorWrapper : PropertyDescriptor
        {
            private PropertyDescriptor _descriptor;
            public MetadataPropertyDescriptorWrapper(PropertyDescriptor descriptor, Attribute[] attrs)
                : base(descriptor, attrs)
            {
                this._descriptor = descriptor;
            }

            public override void AddValueChanged(object component, EventHandler handler)
            {
                this._descriptor.AddValueChanged(component, handler);
            }

            public override bool CanResetValue(object component)
            {
                return this._descriptor.CanResetValue(component);
            }

            public override Type ComponentType
            {
                get
                {
                    return this._descriptor.ComponentType;
                }
            }

            public override object GetValue(object component)
            {
                return this._descriptor.GetValue(component);
            }

            public override bool IsReadOnly
            {
                get
                {
                    return this._descriptor.IsReadOnly;
                }
            }

            public override Type PropertyType
            {
                get
                {
                    return this._descriptor.PropertyType;
                }
            }

            public override void RemoveValueChanged(object component, EventHandler handler)
            {
                this._descriptor.RemoveValueChanged(component, handler);
            }

            public override void ResetValue(object component)
            {
                this._descriptor.ResetValue(component);
            }

            public override void SetValue(object component, object value)
            {
                this._descriptor.SetValue(component, value);
            }

            public override bool ShouldSerializeValue(object component)
            {
                return this._descriptor.ShouldSerializeValue(component);
            }

            public override bool SupportsChangeEvents
            {
                get
                {
                    return this._descriptor.SupportsChangeEvents;
                }
            }
        }

        /// <summary>
        /// This <see cref="PropertyDescriptor"/> is used exclusively for describing the "ETag" property
        /// </summary>
        /// <remarks>
        /// We use this property descriptor (instead of a real property) so to generate the "ETag"
        /// property on the client without having it appear in table storage schema.
        /// <para>
        /// The <see cref="TableDomainService{T}"/> makes sure this cache is kept up-to-date for each 
        /// entity instance.
        /// </para>
        /// </remarks>
        private class ETagPropertyDescriptor : PropertyDescriptor
        {
            private static readonly Attribute[] DefaultAttributes = new Attribute[]
            {
                new EditableAttribute(false),
                new DisplayAttribute() { AutoGenerateField = false },
                new ConcurrencyCheckAttribute(),
            };

            private readonly Type _componentType;

            public ETagPropertyDescriptor(Type componentType)
                : base("ETag", ETagPropertyDescriptor.DefaultAttributes)
            {
                this._componentType = componentType;
            }

            public override bool CanResetValue(object component)
            {
                return true;
            }

            public override Type ComponentType
            {
                get { return this._componentType; }
            }

            public override object GetValue(object component)
            {
                return ((TableEntity)component).GetETag();
            }

            public override bool IsReadOnly
            {
                get { return false; }
            }

            public override Type PropertyType
            {
                get { return typeof(string); }
            }

            public override void ResetValue(object component)
            {
                ((TableEntity)component).SetETag(null);
            }

            public override void SetValue(object component, object value)
            {
                ((TableEntity)component).SetETag((string)value);
            }

            public override bool ShouldSerializeValue(object component)
            {
                return true;
            }
        }

        #endregion
    }
}
