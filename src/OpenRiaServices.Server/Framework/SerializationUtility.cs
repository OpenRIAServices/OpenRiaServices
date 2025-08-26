using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using OpenRiaServices.Server;

namespace OpenRiaServices
{
    class SerializationUtility
    {
        /// <summary>
        /// Gets the type that should be used on the client for the specified type.
        /// </summary>
        /// <param name="t">The type to get the client type for.</param>
        /// <returns>The client type.</returns>
        public static Type GetClientType(Type t)
        {
            if (BinaryTypeUtility.IsTypeBinary(t))
            {
                return typeof(byte[]);
            }

            return t;
        }

        /// <summary>
        /// Gets a value that can be used by the client.
        /// </summary>
        /// <remarks>
        /// This method should be kept in sync with DataContractSurrogateGenerator.EmitToClientConversion.
        /// </remarks>
        /// <param name="targetType">The type used by the client.</param>
        /// <param name="value">The value on the server.</param>
        /// <returns>A value that can be used by the client.</returns>
        public static object GetClientValue(Type targetType, object value)
        {
            if (value == null)
            {
                return null;
            }

            if (targetType == typeof(byte[]) && BinaryTypeUtility.IsTypeBinary(value.GetType()))
            {
                return BinaryTypeUtility.GetByteArrayFromBinary(value);
            }

            return value;
        }

        /// <summary>
        /// Gets a value that can be used by the server.
        /// </summary>
        /// <remarks>
        /// This method should be kept in sync with DataContractSurrogateGenerator.EmitToServerConversion.
        /// </remarks>
        /// <param name="targetType">The type used by the server.</param>
        /// <param name="value">The value from the client.</param>
        /// <returns>A value that can be used by the server.</returns>
        public static object GetServerValue(Type targetType, object value)
        {
            if (value == null)
            {
                return null;
            }

            byte[] valueAsByteArray = value as byte[];
            if (BinaryTypeUtility.IsTypeBinary(targetType) && (valueAsByteArray != null))
            {
                return BinaryTypeUtility.GetBinaryFromByteArray(valueAsByteArray);
            }

            return value;
        }

        /// <summary>
        /// Returns true if the specified property is a data member that should be serialized
        /// </summary>
        /// <param name="propertyDescriptor">The property to inspect</param>
        /// <returns>true if the specified property is a data member that should be serialized</returns>
        public static bool IsSerializableDataMember(PropertyDescriptor propertyDescriptor)
        {
            if (!(TypeUtility.IsPredefinedType(propertyDescriptor.PropertyType) || TypeUtility.IsSupportedComplexType(propertyDescriptor.PropertyType)))
            {
                return false;
            }

            if (propertyDescriptor.Attributes[typeof(ExcludeAttribute)] != null)
            {
                // properties that are marked [Exclude] are not data members
                return false;
            }

            if (propertyDescriptor.Attributes[typeof(EntityAssociationAttribute)] != null)
            {
                // associations are not data members
                return false;
            }

            AttributeCollection attrs = propertyDescriptor.ComponentType.Attributes();

            if (attrs[typeof(DataContractAttribute)] != null)
            {
                // [DataContract] on type, nothing on member
                if (propertyDescriptor.Attributes[typeof(DataMemberAttribute)] == null)
                {
                    return false;
                }
            }
            else
            {
                // [IgnoreDataMember] on member
                if (propertyDescriptor.Attributes[typeof(IgnoreDataMemberAttribute)] != null)
                {
                    return false;
                }
            }

            return true;
        }       
    }
}
