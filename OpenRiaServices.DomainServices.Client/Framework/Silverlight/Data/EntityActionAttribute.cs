using System;

namespace OpenRiaServices.DomainServices.Client
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class EntityActionAttribute : Attribute
    {
        public EntityActionAttribute() { }

        public EntityActionAttribute(string name)
            : this(name, string.Format("Can{0}", name), string.Format("Is{0}Invoked", name))
        { }

        public EntityActionAttribute(string name, string canInvokePropertyName, string isInvokedPropertyName)
        {
            this.Name = name;
            this.CanInvokePropertyName = canInvokePropertyName;
            this.IsInvokedPropertyName = isInvokedPropertyName;
        }

        public string Name { get; set; }
        public string CanInvokePropertyName { get; set; }
        public string IsInvokedPropertyName { get; set; }
        public bool AllowMultipleInvocations { get; set; }
    }
}
