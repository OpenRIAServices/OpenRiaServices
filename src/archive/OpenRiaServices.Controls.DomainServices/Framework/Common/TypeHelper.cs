using System.Diagnostics;
using System.Reflection;

namespace System.Windows.Common
{
    /// <summary>
    /// Utility class for Type related operations
    /// </summary>
    internal static class TypeHelper
    {
        #region Internal Fields
        internal const char PropertyNameSeparator = '.';
        #endregion

        /// <summary>
        /// Extension method that returns the type of a property. That property can be nested.
        /// Each element of the path needs to be a public instance property.
        /// </summary>
        /// <param name="parentType">Type that exposes that property</param>
        /// <param name="propertyPath">Property path</param>
        /// <returns>Property type</returns>
        internal static Type GetNestedPropertyType(this Type parentType, string propertyPath)
        {
            PropertyInfo propertyInfo;
            Type propertyType = parentType;
            if (!String.IsNullOrEmpty(propertyPath))
            {
                string[] propertyNames = propertyPath.Split(PropertyNameSeparator);
                for (int i = 0; i < propertyNames.Length; i++)
                {
                    propertyInfo = propertyType.GetProperty(propertyNames[i]);
                    if (propertyInfo == null)
                    {
                        return null;
                    }
                    propertyType = propertyInfo.PropertyType;
                }
            }
            return propertyType;
        }

        /// <summary>
        /// Retrieves the value of a property. That property can be nested.
        /// Each element of the path needs to be a public instance property.
        /// </summary>
        /// <param name="item">Object that exposes the property</param>
        /// <param name="propertyPath">Property path</param>
        /// <param name="exception">Potential exception</param>
        /// <returns>Property value</returns>
        internal static object GetNestedPropertyValue(object item, string propertyPath, out Exception exception)
        {
            Debug.Assert(item != null, "Unexpected null item in TypeHelper.GetNestedPropertyValue");
            object value = null;
            exception = GetOrSetNestedPropertyValue(false /*set*/, item, ref value, propertyPath);
            return value;
        }

        /// <summary>
        /// Retrieves the value of a property. That property can be nested.
        /// Each element of the path needs to be a public instance property.
        /// </summary>
        /// <param name="item">Object that exposes the property</param>
        /// <param name="propertyPath">Property path</param>
        /// <param name="propertyType">Property type</param>
        /// <param name="exception">Potential exception</param>
        /// <returns>Property value</returns>
        internal static object GetNestedPropertyValue(object item, string propertyPath, Type propertyType, out Exception exception)
        {
            exception = null;

            // if the propertyPath is null or empty, use the
            // item directly
            if (String.IsNullOrEmpty(propertyPath))
            {
                return item;
            }

            string[] propertyNames = propertyPath.Split(PropertyNameSeparator);
            for (int i = 0; i < propertyNames.Length; i++)
            {
                if (item == null)
                {
                    break;
                }

                Type type = item.GetType();

                // if we can't find the property or it is not of the correct type,
                // treat it as a null value
                PropertyInfo propertyInfo = type.GetProperty(propertyNames[i]);
                if (propertyInfo == null)
                {
                    break;
                }

                if (!propertyInfo.CanRead)
                {
                    exception = new InvalidOperationException(string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        CommonResources.PropertyNotReadable,
                        propertyNames[i],
                        type.GetTypeName()));

                    break;
                }

                if (i == propertyNames.Length - 1)
                {
                    // if the property type did not match, return null
                    if (propertyInfo.PropertyType != propertyType)
                    {
                        break;
                    }
                    else
                    {
                        return propertyInfo.GetValue(item, null);
                    }
                }
                else
                {
                    item = propertyInfo.GetValue(item, null);
                }
            }

            return null;
        }

        /// <summary>
        /// Get the type argument for a <see cref="Nullable{T}"/> type.
        /// </summary>
        /// <param name="type">Any <see cref="Type"/> that might extend <see cref="Nullable{T}"/>.</param>
        /// <returns>
        /// If the <paramref name="type"/> is a <see cref="Nullable{T}"/> type, then the type argument
        /// of that type will be returned.  Otherwise <paramref name="type"/> will be returned as-is.
        /// </returns>
        internal static Type GetNonNullableType(this Type type)
        {
            return type.IsNullableType() ? type.GetGenericArguments()[0] : type;
        }

        /// <summary>
        /// Get the underlying <see cref="Type"/> from an <see cref="Enum"/> type.
        /// </summary>
        /// <param name="type">Any <see cref="Type"/> that might represent an <see cref="Enum"/>
        /// or a <see cref="Nullable{T}"/> of an <see cref="Enum"/> type.</param>
        /// <returns>
        /// If the <paramref name="type"/> is an <see cref="Enum"/> type, then <see cref="Enum.GetUnderlyingType"/>
        /// will be returned.
        /// <para>
        /// If the <paramref name="type"/> is a <see cref="Nullable{T}"/> of an <see cref="Enum"/>
        /// type, then a <see cref="Nullable{T}"/> of the <see cref="Enum.GetUnderlyingType"/> will be returned.
        /// </para>
        /// <para>
        /// Otherwise, <paramref name="type"/> will be returned as-is.
        /// </para>
        /// </returns>
        /// <remarks>
        /// If the <paramref name="type"/> is a <see cref="Nullable{T}"/> type, then it will be returned as-is.
        /// </remarks>
        internal static Type GetUnderlyingEnumType(this Type type)
        {
            if (!type.IsEnumType())
            {
                return type;
            }
            else if (type.IsNullableType())
            {
                return typeof(Nullable<>).MakeGenericType(Enum.GetUnderlyingType(type.GetNonNullableType()));
            }
            else
            {
                return Enum.GetUnderlyingType(type);
            }
        }

        /// <summary>
        /// Gets or sets the value of a public instance property. The property can be nested. 
        /// </summary>
        /// <param name="set">Set to true to write the property value</param>
        /// <param name="item">Object that exposes the property</param>
        /// <param name="value">Property value</param>
        /// <param name="propertyPath">Property path</param>
        /// <returns>Potential exception</returns>
        private static Exception GetOrSetNestedPropertyValue(bool set, object item, ref object value, string propertyPath)
        {
            Debug.Assert(item != null, "Unexpected null item in TypeHelper.GetOrSetNestedPropertyValue");
            Debug.Assert(propertyPath != null, "Unexpected null propertyPath in TypeHelper.GetOrSetNestedPropertyValue");
            if (!set)
            {
                value = null;
            }
            string[] propertyNames = propertyPath.Split(PropertyNameSeparator);
            for (int i = 0; i < propertyNames.Length; i++)
            {
                if (item == null)
                {
                    Debug.Assert(i > 0, "Unexpected i==0 in TypeHelper.GetOrSetNestedPropertyValue");
                    return new InvalidOperationException(string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        CommonResources.InvalidPropertyAccess,
                        propertyNames[i - 1],
                        propertyNames[i]));
                }
                Type type = item.GetType();
                PropertyInfo propertyInfo = type.GetProperty(propertyNames[i]);
                if (propertyInfo == null)
                {
                    return new InvalidOperationException(string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        CommonResources.PropertyNotFound,
                        propertyNames[i],
                        type.GetTypeName()));
                }

                bool setProperty = set && i == propertyNames.Length - 1;
                if (setProperty)
                {
                    // If there is no set method or the set method is not public
                    if (!propertyInfo.CanWrite || (propertyInfo.GetSetMethod() == null))
                    {
                        return new InvalidOperationException(string.Format(
                            System.Globalization.CultureInfo.InvariantCulture,
                            CommonResources.PropertyNotWritable,
                            propertyNames[i],
                            type.GetTypeName()));
                    }
                }
                else if (!propertyInfo.CanRead)
                {
                    return new InvalidOperationException(string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        CommonResources.PropertyNotReadable,
                        propertyNames[i],
                        type.GetTypeName()));
                }

                if (setProperty)
                {
                    propertyInfo.SetValue(item, value, null);
                }
                else
                {
                    if (i == propertyNames.Length - 1)
                    {
                        value = propertyInfo.GetValue(item, null);
                    }
                    else
                    {
                        item = propertyInfo.GetValue(item, null);
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Returns the PropertyInfo corresponding to the provided propertyPath. The propertyPath can be a dotted
        /// path where each section is a public property name. Only public instance properties are searched for.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> extended by this method.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <returns>The found PropertyInfo or null otherwise</returns>
        internal static PropertyInfo GetPropertyInfo(this Type type, string propertyPath)
        {
            Debug.Assert(type != null, "Unexpected null type in TypeHelper.GetPropertyOrFieldInfo");
            if (!String.IsNullOrEmpty(propertyPath))
            {
                string[] propertyNames = propertyPath.Split(PropertyNameSeparator);
                for (int i = 0; i < propertyNames.Length; i++)
                {
                    PropertyInfo propertyInfo = type.GetProperty(propertyNames[i]);
                    if (propertyInfo == null)
                    {
                        return null;
                    }
                    if (i == propertyNames.Length - 1)
                    {
                        return propertyInfo;
                    }
                    else
                    {
                        type = propertyInfo.PropertyType;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Returns the friendly name for a type
        /// </summary>
        /// <param name="type">The <see cref="Type"/> extended by this method.</param>
        /// <returns>Textual representation of the input type</returns>
        internal static string GetTypeName(this Type type)
        {
            Type baseType = type.GetNonNullableType();
            string s = baseType.Name;
            if (type != baseType)
            {
                s += '?';
            }
            return s;
        }

        internal static bool IsEnumType(this Type type)
        {
            return type.GetNonNullableType().IsEnum;
        }

        internal static bool IsNullableType(this Type type)
        {
            return (((type != null) && type.IsGenericType) && (type.GetGenericTypeDefinition() == typeof(Nullable<>)));
        }

        /// <summary>
        /// Sets the value of a property. That property can be nested. 
        /// Only works on public instance properties.
        /// </summary>
        /// <param name="item">Object that exposes the property</param>
        /// <param name="value">Property value</param>
        /// <param name="propertyPath">Property path</param>
        /// <returns>Potential exception</returns>
        internal static Exception SetNestedPropertyValue(object item, object value, string propertyPath)
        {
            Debug.Assert(item != null, "Unexpected null item in TypeHelper.SetNestedPropertyValue");
            return GetOrSetNestedPropertyValue(true /*set*/, item, ref value, propertyPath);
        }
    }
}
