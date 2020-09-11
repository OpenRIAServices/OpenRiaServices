using System;
using System.ComponentModel;

namespace OpenRiaServices.DomainServices.Server
{
    internal class MetadataPropertyDescriptorWrapper : PropertyDescriptor
    {
        private readonly PropertyDescriptor _descriptor;

        public MetadataPropertyDescriptorWrapper(PropertyDescriptor descriptor, Attribute[] attrs)
            : base(descriptor, attrs)
        {
            this._descriptor = descriptor;
        }

        public override Type ComponentType
        {
            get { return this._descriptor.ComponentType; }
        }

        public override bool IsReadOnly
        {
            get { return this._descriptor.IsReadOnly; }
        }

        public override Type PropertyType
        {
            get { return this._descriptor.PropertyType; }
        }

        public override bool SupportsChangeEvents
        {
            get { return this._descriptor.SupportsChangeEvents; }
        }

        public override void AddValueChanged(object component, EventHandler handler)
        {
            this._descriptor.AddValueChanged(component, handler);
        }

        public override bool CanResetValue(object component)
        {
            return this._descriptor.CanResetValue(component);
        }

        public override object GetValue(object component)
        {
            return this._descriptor.GetValue(component);
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
    }
}
