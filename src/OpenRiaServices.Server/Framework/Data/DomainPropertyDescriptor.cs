using System;
using System.ComponentModel;

namespace OpenRiaServices.Server
{
    /// <summary>
    /// PropertyDescriptor for general domain Type properties.
    /// </summary>
    internal class DomainPropertyDescriptor : PropertyDescriptor
    {
        private readonly PropertyDescriptor _base;

        public override Type ComponentType => this._base.ComponentType;
        public override string DisplayName => _base.DisplayName;
        public override string Description => _base.Description;
        public override bool IsReadOnly => this._base.IsReadOnly;
        public override Type PropertyType => this._base.PropertyType;

        /// <summary>
        /// Similar to <see cref="DomainPropertyDescriptor(PropertyDescriptor, Attribute[])"/> but instead of adding merging attributes of <paramref name="pd"/>
        /// and <paramref name="attribs"/>, the resulting description will have exaclty the attributes specified of <paramref name="attribs"/>
        /// </summary>
        private DomainPropertyDescriptor(string name, PropertyDescriptor pd, Attribute[] attribs)
            : base(name, attribs)
        {
            this._base = pd;
        }

        public DomainPropertyDescriptor(PropertyDescriptor pd, Attribute[] attribs)
            : base(pd, attribs)
        {
            this._base = pd;
        }

        /// <summary>
        /// Similar to <see cref="DomainPropertyDescriptor(PropertyDescriptor, Attribute[])"/> but instead of adding merging attributes of <paramref name="pd"/>
        /// and <paramref name="attribs"/>, the resulting description will have exaclty the attributes specified of <paramref name="attribs"/>
        /// </summary>
        /// <param name="pd"></param>
        /// <param name="attribs"></param>
        /// <returns></returns>
        public static DomainPropertyDescriptor CreateWithExplicitAttributes(PropertyDescriptor pd, Attribute[] attribs)
            => new DomainPropertyDescriptor(pd.Name, pd, attribs);

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
    }
}
