using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace OpenRiaServices.VisualStudio.DomainServices.Tools
{
    internal static class TypeUtilities
    {
        // list of "simple" types we will always accept for
        // serialization, inclusion from entities, etc.
        // Primitive types are not here -- test for them via Type.IsPrimitive
        private static readonly Type[] predefinedTypes = 
        {
            typeof(string),
            typeof(decimal),
            typeof(DateTime),
            typeof(DateTimeOffset),
            typeof(TimeSpan),
            typeof(Guid),
            typeof(XElement)
        };

        private static readonly Type[] predefinedGenericTypes = 
        {
            typeof(IEnumerable<>),
            typeof(Nullable<>)
        };

        public static Type FindIEnumerable(Type seqType)
        {
            if (seqType == null || seqType == typeof(string))
            {
                return null;
            }
            if (seqType.IsArray)
            {
                return typeof(IEnumerable<>).MakeGenericType(seqType.GetElementType());
            }
            if (seqType.IsGenericType)
            {
                foreach (Type arg in seqType.GetGenericArguments())
                {
                    Type ienum = typeof(IEnumerable<>).MakeGenericType(arg);
                    if (ienum.IsAssignableFrom(seqType))
                    {
                        return ienum;
                    }
                }
            }

            Type[] ifaces = seqType.GetInterfaces();
            if (ifaces != null && ifaces.Length > 0)
            {
                foreach (Type iface in ifaces)
                {
                    Type ienum = FindIEnumerable(iface);
                    if (ienum != null)
                    {
                        return ienum;
                    }
                }
            }
            if (seqType.BaseType != null && seqType.BaseType != typeof(object))
            {
                return FindIEnumerable(seqType.BaseType);
            }
            return null;
        }

        /// <summary>
        /// Returns <c>true</c> if the given type is a primitive type or one
        /// of our standard acceptable simple types, such as <see cref="String"/>,
        /// <see cref="Guid"/>, etc/
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>true if given type is primitive or one of the accepted simple types</returns>
        public static bool IsPredefinedType(Type type)
        {
            return IsPredefinedSimpleType(type) || IsPredefinedGenericType(type) || type == typeof(byte[]) || type == typeof(System.Data.Linq.Binary);
        }

        public static bool IsPredefinedSimpleType(Type type)
        {
            if (type.IsPrimitive)
            {
                return true;
            }

            if (type.IsEnum)
            {
                return true;
            }

            foreach (Type t in predefinedTypes)
            {
                if (t == type)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsPredefinedGenericType(Type type)
        {
            if (type.IsGenericType)
            {
                type = type.GetGenericTypeDefinition();
            }
            else
            {
                return false;
            }

            foreach (Type t in predefinedGenericTypes)
            {
                if (t == type)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns the underlying element type starting from a given type.
        /// </summary>
        /// <remarks>
        /// Simple types simply return the input type.
        /// If the given type is an array, this method returns the array's
        /// element type.
        /// If the type is a generic type of <see cref="System.Collections.IEnumerable"/>, 
        /// or <see cref="Nullable"/>, this method returns the element
        /// type of the generic parameter
        /// </remarks>
        /// <param name="type">The type to base off of.</param>
        /// <returns>the underlying element type starting from a given type</returns>
        public static Type GetElementType(Type type)
        {
            // Any simple type has no element type -- it is the element type itself
            if (IsPredefinedSimpleType(type))
            {
                return type;
            }

            // Array, pointers, etc -- allow for recursion for arrays of arrays, etc
            if (type.HasElementType)
            {
                return GetElementType(type.GetElementType());
            }

            // If Nullable<T> or IEnumerable<T>, return the T
            if (IsPredefinedGenericType(type))
            {
                Type[] genericArgs = type.GetGenericArguments();
                return GetElementType(genericArgs[0]);
            }

            // IEnumerable<T> returns T
            Type ienum = FindIEnumerable(type);
            if (ienum != null)
            {
                Type genericArg = ienum.GetGenericArguments()[0];
                return GetElementType(genericArg);
            }

            return type;
        }

        /// <summary>
        /// Gets the type of the associated metadata type from the set of type level attributes
        /// </summary>
        /// <param name="type">The type whose attributes are needed</param>
        /// <returns>The associated metadata type or null.</returns>
        internal static Type GetAssociatedMetadataType(Type type)
        {
            Type metadataClassType = null;
            Attribute metadataTypeAttribute = type.GetCustomAttributes(true).OfType<Attribute>().Where(a => a.GetType().Name == BusinessLogicClassConstants.MetadataTypeAttributeTypeName).FirstOrDefault();
            if (metadataTypeAttribute != null)
            {
                PropertyInfo propertyInfo = metadataTypeAttribute.GetType().GetProperty("MetadataClassType");
                if (propertyInfo != null)
                {
                    metadataClassType = propertyInfo.GetValue(metadataTypeAttribute, null) as Type;
                }
            }
            return metadataClassType;
        }
    }
}
