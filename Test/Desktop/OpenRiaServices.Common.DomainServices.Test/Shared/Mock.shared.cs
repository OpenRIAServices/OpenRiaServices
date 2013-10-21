using System;
using System.Runtime.Serialization;

namespace TestDomainServices
{
    [Flags]
    [DataContract]
    public enum TestEnum
    {
        [EnumMember]
        Value0 = 0,

        [EnumMember]
        Value1 = 1,

        [EnumMember]
        Value2 = 2,

        [EnumMember]
        Value3 = 4
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public class MockAttributeAllowOnce : Attribute
    {
        public MockAttributeAllowOnce(string value) { this.Value = value; }
        public string Value { get; private set; }
#if !SILVERLIGHT
        public override object TypeId { get { return Guid.NewGuid(); } }
#endif
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public class MockAttributeAllowOnce_AppliedToInterfaceOnly : Attribute
    {
        public MockAttributeAllowOnce_AppliedToInterfaceOnly(string value) { this.Value = value; }
        public string Value { get; private set; }
#if !SILVERLIGHT
        public override object TypeId { get { return Guid.NewGuid(); } }
#endif
    }

    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
    public class MockAttributeAllowOnce_InterfaceOnly : Attribute
    {
        public MockAttributeAllowOnce_InterfaceOnly(string value) { this.Value = value; }
        public string Value { get; private set; }
#if !SILVERLIGHT
        public override object TypeId { get { return Guid.NewGuid(); } }
#endif
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class MockAttributeAllowMultiple : Attribute
    {
        public MockAttributeAllowMultiple(string value) { this.Value = value; }
        public string Value { get; private set; }
#if !SILVERLIGHT
        public override object TypeId { get { return Guid.NewGuid(); } }
#endif
    }

    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = true)]
    public class MockAttributeAllowMultiple_InterfaceOnly : Attribute
    {
        public MockAttributeAllowMultiple_InterfaceOnly(string value) { this.Value = value; }
        public string Value { get; private set; }
#if !SILVERLIGHT
        public override object TypeId { get { return Guid.NewGuid(); } }
#endif
    }
}

// Used to verify that the code-generator properly generates code for attributes defined in unrelated namespaces.
namespace CustomNamespace
{
    [AttributeUsage(AttributeTargets.All)]
    public class CustomAttribute : Attribute
    {
        //
    }
}
