using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Nerdbank.MessagePack;

#nullable enable

#if SERVERFX
namespace OpenRiaServices.Hosting.AspNetCore.Serialization.MessagePack
#else
namespace OpenRiaServices.Client.DomainClients.MessagePack
#endif
{
    internal static class MessagePackUtility
    {
        public static byte[] GetDiscriminator(Type type)
        {
            // Search base types for discriminator attributes mapping to type
            Type? baseType = type.BaseType;
            while (baseType != null)
            {
                var derivedTypeShapes = baseType.GetCustomAttributes(typeof(PolyType.DerivedTypeShapeAttribute), inherit: false)
                    .Cast<PolyType.DerivedTypeShapeAttribute>()
                    .FirstOrDefault(p => p.Type == type);

                if (derivedTypeShapes != null)
                {
                    if (derivedTypeShapes.Name is null)
                        throw new InvalidOperationException("DerivedTypeShapeAttribute must have a non-null Name property.");
                    return Encoding.UTF8.GetBytes(derivedTypeShapes.Name);
                }
                baseType = baseType.BaseType;
            }

            // Use DataContractAttribute if present, otherwise fallback to type name
            string discriminator;
            //if (type.GetCustomAttribute<System.Runtime.Serialization.DataContractAttribute>(inherit: false)
            //    is System.Runtime.Serialization.DataContractAttribute dataContractAttribute)
            //{
            //    string typeName = dataContractAttribute?.Name! ?? type.Name;
            //    string? ns = dataContractAttribute?.Namespace ?? "";

            //    if (ns is null)
            //        discriminator = typeName;

            //    else
            //        discriminator = $"{ns}.{typeName}";
            //}
            //else
            {
                discriminator = PolyType.Utilities.ReflectionUtilities.GetDerivedTypeShapeName(type);
            }

            return Encoding.UTF8.GetBytes(discriminator);
        }

        /// <summary>
        /// Perform basic setup of the MessagePackSerializer to ensure it is configured correctly for use in Open RIA Services.
        /// </summary>
        internal static MessagePackSerializer ConfigureSerializer(MessagePackSerializer serializer,
            IEnumerable<MessagePackConverter> converters)
        {
            serializer = serializer.WithHiFiDateTime();

            return serializer with
            {
                PreserveReferences = ReferencePreservationMode.Off,
                Converters = [.. serializer.Converters, .. converters]
            };
        }

        internal sealed class ByteArrayComparer : IEqualityComparer<byte[]?>
#if NET10_0_OR_GREATER
        , IAlternateEqualityComparer<ReadOnlySpan<byte>, byte[]?>
#endif
        {
            public bool Equals(byte[]? x, byte[]? y)
            {
                if (ReferenceEquals(x, y))
                    return true;
                if (x is null || y is null)
                    return false;

                return x.AsSpan().SequenceEqual(y);
            }

            int IEqualityComparer<byte[]?>.GetHashCode(byte[]? obj)
            {
                return obj == null ? 0 : (int)System.IO.Hashing.XxHash32.HashToUInt32(obj);
            }

#if NET10_0_OR_GREATER
        byte[]? IAlternateEqualityComparer<ReadOnlySpan<byte>, byte[]?>.Create(ReadOnlySpan<byte> alternate)
            => alternate.ToArray();

        bool IAlternateEqualityComparer<ReadOnlySpan<byte>, byte[]?>.Equals(ReadOnlySpan<byte> alternate, byte[]? other)
            => other is not null && alternate.SequenceEqual(other);

        int IAlternateEqualityComparer<ReadOnlySpan<byte>, byte[]?>.GetHashCode(ReadOnlySpan<byte> alternate)
            => (int)System.IO.Hashing.XxHash32.HashToUInt32(alternate);
#endif
        }
    }
}
