using System;
using System.ComponentModel;

namespace OpenRiaServices.DomainServices.Server
{
    /// <summary>
    /// PropertyDescriptor for general domain Type properties.
    /// </summary>
    internal class DomainPropertyDescriptor : PropertyDescriptor
    {
        private PropertyDescriptor _base;

        public DomainPropertyDescriptor(PropertyDescriptor pd, Attribute[] attribs)
            : base(pd, attribs)
        {
            this._base = pd;
        }

        public override object GetValue(object component)
        {
            return this._base.GetValue(component);
        }

        public override void SetValue(object component, object value)
        {
            this._base.SetValue(component, value);
        }

        public override bool ShouldSerializeValue(object component)
        {
            return this._base.ShouldSerializeValue(component);
        }

        public override bool CanResetValue(object component)
        {
            return this._base.CanResetValue(component);
        }

        public override void ResetValue(object component)
        {
            this._base.ResetValue(component);
        }

        public override Type ComponentType
        {
            get
            {
                return this._base.ComponentType;
            }
        }

        public override bool IsReadOnly
        {
            get
            {
                return this._base.IsReadOnly;
            }
        }

        public override Type PropertyType
        {
            get
            {
                return this._base.PropertyType;
            }
        }
    }
}
