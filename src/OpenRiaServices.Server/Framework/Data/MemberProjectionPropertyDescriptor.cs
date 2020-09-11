using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;

namespace OpenRiaServices.Server
{
    /// <summary>
    /// PropertyDescriptor for a member projection.
    /// </summary>
    internal class MemberProjectionPropertyDescriptor : PropertyDescriptor
    {
        private readonly PropertyDescriptor _sourceProperty;
        private readonly string[] _path;
        private PropertyDescriptor _targetProperty;

        // Only allow these attributes to be propagated from the target property
        // onto the projected property.
        private static HashSet<Type> _projectionAttributeAllowSet = new HashSet<Type>
        {
            typeof(DisplayFormatAttribute),
            typeof(ExternalReferenceAttribute),
            typeof(StringLengthAttribute),
            typeof(UIHintAttribute),
        };

        public MemberProjectionPropertyDescriptor(PropertyDescriptor sourceProperty, PropertyDescriptor targetProperty, IncludeAttribute projectionInclude)
            : base(projectionInclude.MemberName, GetAttributes(targetProperty).ToArray())
        {
            this._sourceProperty = sourceProperty;
            this._path = projectionInclude.Path.Split('.');
            this._targetProperty = targetProperty;
        }

        /// <summary>
        /// We must override Attributes and compute them dynamically to get around TD registration ordering issues
        /// for projection include members. The issue is that it might be the case that custom TDs havent been registered
        /// yet for the target property at the point in time when the projection property is created. That would mean
        /// that all attributes aren't yet known when the projection property descriptor is created, so the attributes
        /// can't be set at construction time.
        /// </summary>
        public override AttributeCollection Attributes
        {
            get
            {
                // Get the current target property descriptor and filter the attributes to propagate
                this._targetProperty = TypeDescriptor.GetProperties(this._targetProperty.ComponentType)[this._targetProperty.Name];
                IEnumerable<Attribute> targetAttributesToPropagate = this._targetProperty.Attributes.Cast<Attribute>().Where(p => _projectionAttributeAllowSet.Contains(p.GetType()));
                AttributeCollection attributes = AttributeCollection.FromExisting(base.Attributes, targetAttributesToPropagate.ToArray());
                return attributes;
            }
        }

        private static IEnumerable<Attribute> GetAttributes(PropertyDescriptor targetProperty)
        {
            // merge attributes from the target member with the set of invariant
            // attributes for projection members
            IEnumerable<Attribute> targetAttributesToPropagate = targetProperty.Attributes.Cast<Attribute>()
                .Where(p => _projectionAttributeAllowSet.Contains(p.GetType()));

            Attribute[] newAttributes = new Attribute[] 
            { 
                new DataMemberAttribute(), 
                new EditableAttribute(false)
            };
            return targetAttributesToPropagate.Concat(newAttributes);
        }

        public override object GetValue(object component)
        {
            // Drill down through the member path to get the value to project.
            object value = this._sourceProperty.GetValue(component);
            if (value != null)
            {
                foreach (string pathMember in this._path)
                {
                    if (value == null)
                    {
                        // hit a null link in the path, so the projected
                        // value cannot be determined
                        value = null;
                        break;
                    }

                    PropertyDescriptor pd = TypeDescriptor.GetProperties(value)[pathMember];
                    System.Diagnostics.Debug.Assert(pd != null, "Failed to get the property descriptor");
                    value = pd.GetValue(value);
                }
            }

            if (value == null && this.PropertyType.IsValueType && !TypeUtility.IsNullableType(this.PropertyType))
            {
                // if the value is null or cannot be determined due to null
                // links in the path and the property type is a non-nullable
                // value type, we must return the default value for the type
                // rather than null.
                return Activator.CreateInstance(this.PropertyType);
            }

            return value;
        }

        public override void SetValue(object component, object value)
        {
            throw new NotImplementedException();
        }

        public override bool ShouldSerializeValue(object component)
        {
            return true;
        }

        public override bool CanResetValue(object component)
        {
            return false;
        }

        public override void ResetValue(object component)
        {
            throw new NotImplementedException();
        }

        public override Type ComponentType
        {
            get
            {
                return this._sourceProperty.ComponentType;
            }
        }

        public override bool IsReadOnly
        {
            get
            {
                return true;
            }
        }

        public override Type PropertyType
        {
            get
            {
                return this._targetProperty.PropertyType;
            }
        }
    }
}
