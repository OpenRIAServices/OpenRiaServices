using System;
using System.Runtime.Serialization;

namespace SimpleStructs
{
    [DataContract]
    public struct SimpleStruct : IEquatable<SimpleStruct>
    {
        public SimpleStruct(int value)
        {
            Value = value;
        }

        [DataMember]
        public int Value { get; set; }

        public bool Equals(SimpleStruct other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is SimpleStruct && Equals((SimpleStruct)obj);
        }

        public override int GetHashCode()
        {
            return Value;
        }

        public static bool operator ==(SimpleStruct left, SimpleStruct right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SimpleStruct left, SimpleStruct right)
        {
            return !left.Equals(right);
        }
    }

    [DataContract]
    public struct CompositeSimpleStruct : IEquatable<CompositeSimpleStruct>
    {
        public CompositeSimpleStruct(int value, Guid tenantId)
        {
            Value = value;
            TenantId = tenantId;
        }

        [DataMember]
        public int Value { get; set; }

        [DataMember]
        public Guid TenantId { get; set; }

        public bool Equals(CompositeSimpleStruct other)
        {
            return Value == other.Value && TenantId == other.TenantId;
        }

        public override bool Equals(object obj)
        {
            return obj is CompositeSimpleStruct && Equals((CompositeSimpleStruct)obj);
        }

        public override int GetHashCode()
        {
            return (Value * 397) ^ TenantId.GetHashCode();
        }

        public static bool operator ==(CompositeSimpleStruct left, CompositeSimpleStruct right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CompositeSimpleStruct left, CompositeSimpleStruct right)
        {
            return !left.Equals(right);
        }
    }
}
