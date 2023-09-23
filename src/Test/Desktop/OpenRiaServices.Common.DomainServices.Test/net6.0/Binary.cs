using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace System.Data.Linq
{
    [DataContract]
    [Serializable]
    public sealed class Binary : List<byte>, IEquatable<Binary> 
        // , ICollection<byte>
        
    {
        int? hashCode;

        public Binary(byte[] value)
            : base (value ?? Array.Empty<byte>())
        {
        }

        public static implicit operator Binary(byte[] value)
        {
            return value == null ? null : new Binary(value);
        }

        public bool Equals(Binary other)
        {
            return this.EqualsTo(other);
        }

        public static bool operator ==(Binary binary1, Binary binary2)
        {
            if ((object)binary1 == (object)binary2)
                return true;
            if ((object)binary1 == null && (object)binary2 == null)
                return true;
            if ((object)binary1 == null || (object)binary2 == null)
                return false;
            return binary1.EqualsTo(binary2);
        }

        public static bool operator !=(Binary binary1, Binary binary2)
        {
            if ((object)binary1 == (object)binary2)
                return false;
            if ((object)binary1 == null && (object)binary2 == null)
                return false;
            if ((object)binary1 == null || (object)binary2 == null)
                return true;
            return !binary1.EqualsTo(binary2);
        }

        public override bool Equals(object obj)
        {
            return this.EqualsTo(obj as Binary);
        }

        public override int GetHashCode()
        {
            if (!hashCode.HasValue)
            {
                // hash code is not marked [DataMember], so when
                // using the DataContractSerializer, we'll need
                // to recompute the hash after deserialization.
                ComputeHash();
            }
            return this.hashCode.Value;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('"');
            sb.Append(System.Convert.ToBase64String(base.ToArray()));
            sb.Append('"');
            return sb.ToString();
        }

        private bool EqualsTo(Binary binary)
        {
            if ((object)this == (object)binary)
                return true;
            if ((object)binary == null)
                return false;
            if (this.Count != binary.Count)
                return false;
            if (this.GetHashCode() != binary.GetHashCode())
                return false;
            for (int i = 0, n = this.Count; i < n; i++)
            {
                if (this[i] != binary[i])
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Simple hash using pseudo-random coefficients for each byte in 
        /// the array to achieve order dependency.
        /// </summary>
        private void ComputeHash()
        {
            int s = 314, t = 159;
            hashCode = 0;
            for (int i = 0; i < Count; i++)
            {
                hashCode = hashCode * s + base[i];
                s = s * t;
            }
        }
    }
}
//namespace TestDomainServices
//{
//    public static class BinaryExtensions
//    {
//        public static byte[] ToBinary(this byte[] bytes) => bytes;

//        public static byte[] ToArray(this byte[] bytes) => bytes;
//    }
//}
