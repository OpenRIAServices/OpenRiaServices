using System;

namespace OpenRiaServices.Hosting.OData
{
    #region Namespace
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.Data.Linq;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;
    using OpenRiaServices.Server;
    using System.Threading;

    #endregion

    /// <summary>
    /// Contains utility methods for inspecting types through Reflection.
    /// </summary>
    internal static class TypeUtils
    {
        private static char[] xmlWhitespaceChars = new char[] { ' ', '\t', '\n', '\r' };

        /// <summary>'binary' constant prefixed to binary literals.</summary>
        internal const string LiteralPrefixBinary = "binary";

        /// <summary>'datetime' constant prefixed to datetime literals.</summary>
        internal const string LiteralPrefixDateTime = "datetime";

        /// <summary>'guid' constant prefixed to guid literals.</summary>
        internal const string LiteralPrefixGuid = "guid";

        /// <summary>'X': Prefix to binary type string representation.</summary>
        internal const string XmlBinaryPrefix = "X";

        /// <summary>'M': Suffix for decimal type's string representation</summary>
        internal const string XmlDecimalLiteralSuffix = "M";

        /// <summary>'L': Suffix for long (int64) type's string representation</summary>
        internal const string XmlInt64LiteralSuffix = "L";

        /// <summary>'f': Suffix for float (single) type's string representation</summary>
        internal const string XmlSingleLiteralSuffix = "f";

        /// <summary>'D': Suffix for double (Real) type's string representation</summary>
        internal const string XmlDoubleLiteralSuffix = "D";

        /// <summary>Type of OutOfMemoryException.</summary>
        private static readonly Type OutOfMemoryType = typeof(OutOfMemoryException);

        /// <summary>Type of StackOverflowException.</summary>
        private static readonly Type StackOverflowType = typeof(StackOverflowException);

        /// <summary>Type of ThreadAbortException.</summary>
        private static readonly Type ThreadAbortType = typeof(ThreadAbortException);

        /// <summary>XML whitespace characters to trim around literals.</summary>
        internal static char[] XmlWhitespaceChars { get { return TypeUtils.xmlWhitespaceChars; } }

        /// <summary>Checks if the given property has DataMember attribute.</summary>
        /// <param name="property">Given property.</param>
        /// <returns>true if DataMember attribute is set, false otherwise.</returns>
        internal static bool IsDataMember(PropertyDescriptor property)
        {
            return HasAttribute(property, typeof(DataMemberAttribute));
        }

        /// <summary>Checks if the given property represents an association.</summary>
        /// <param name="property">Given property.</param>
        /// <returns>true if property is an association, false otherwise.</returns>
        internal static bool IsAssociation(PropertyDescriptor property)
        {
            return HasAttribute(property, typeof(AssociationAttribute));
        }

        /// <summary>Checks if the given property represents a composition.</summary>
        /// <param name="property">Given property.</param>
        /// <returns>true if property is a composition, false otherwise.</returns>
        internal static bool IsComposition(PropertyDescriptor property)
        {
            return HasAttribute(property, typeof(CompositionAttribute));
        }

        /// <summary>Checks if the given property represents an external reference.</summary>
        /// <param name="property">Given property.</param>
        /// <returns>true if property is an external reference, false otherwise.</returns>
        internal static bool IsExternalReference(PropertyDescriptor property)
        {
            return HasAttribute(property, typeof(ExternalReferenceAttribute));
        }

        /// <summary>Checks if the given property is excluded.</summary>
        /// <param name="property">Given property.</param>
        /// <returns>true if property is excluded, false otherwise.</returns>
        internal static bool IsExcluded(PropertyDescriptor property)
        {
            return HasAttribute(property, typeof(ExcludeAttribute));
        }

        /// <summary>Checks if the given property is a key property.</summary>
        /// <param name="property">Property to check.</param>
        /// <returns>true if property is a key property, false otherwise.</returns>
        internal static bool IsKeyProperty(PropertyDescriptor property)
        {
            return HasAttribute(property, typeof(KeyAttribute));
        }

        /// <summary>
        /// Returns true if the specified property is a data member that should be serialized
        /// </summary>
        /// <param name="propertyDescriptor">The property to inspect</param>
        /// <returns>true if the specified property is a data member that should be serialized</returns>
        internal static bool IsSerializableDataMember(PropertyDescriptor propertyDescriptor)
        {
            bool serializable = SerializationUtility.IsSerializableDataMember(propertyDescriptor);

            Debug.Assert(!TypeUtils.IsKeyProperty(propertyDescriptor) || serializable, "Key property must be serializable.");
            Debug.Assert(!TypeUtils.IsAssociation(propertyDescriptor) || !serializable, "Association property should not be serializable.");
            Debug.Assert(!TypeUtils.IsComposition(propertyDescriptor) || !serializable, "Composition property should not be serializable.");
            Debug.Assert(!TypeUtils.IsExternalReference(propertyDescriptor) || !serializable, "External Reference property should not be serializable.");
            Debug.Assert(!TypeUtils.IsExcluded(propertyDescriptor) || !serializable, "Excluded property should not be serializable.");

            return serializable;
        }

        /// <summary>
        /// Returns the type of the IQueryable if the type implements IQueryable interface
        /// </summary>
        /// <param name="type">clr type on which IQueryable check needs to be performed.</param>
        /// <returns>Element type if the property type implements IQueryable, else returns null</returns>
        internal static Type GetIQueryableElement(Type type)
        {
            return GetGenericInterfaceElementType(type, IQueryableTypeFilter);
        }

        /// <summary>
        /// Returns the type of the IEnumerable if the type implements IEnumerable interface; null otherwise.
        /// </summary>
        /// <param name="type">type that needs to be checked</param>
        /// <returns>Element type if the type implements IEnumerable, else returns null</returns>
        internal static Type GetIEnumerableElement(Type type)
        {
            return GetGenericInterfaceElementType(type, IEnumerableTypeFilter);
        }

        /// <summary>
        /// Determines whether the specified exception can be caught and 
        /// handled, or whether it should be allowed to continue unwinding.
        /// </summary>
        /// <param name="e"><see cref="Exception"/> to test.</param>
        /// <returns>
        /// true if the specified exception can be caught and handled; 
        /// false otherwise.
        /// </returns>
        internal static bool IsCatchableExceptionType(Exception e)
        {
            // a 'catchable' exception is defined by what it is not.
            Debug.Assert(e != null, "Unexpected null exception!");
            Type type = e.GetType();

            return ((type != StackOverflowType) &&
                     (type != OutOfMemoryType) &&
                     (type != ThreadAbortType));
        }

        /// <summary>
        /// Gets the type that should be used on the client for the specified type.
        /// </summary>
        /// <param name="t">The type to get the client type for.</param>
        /// <returns>The client type.</returns>
        internal static Type GetClientType(Type t)
        {
            if (t == typeof(Binary))
            {
                return typeof(byte[]);
            }

            return t;
        }

        /// <summary>
        /// Gets a value that can be used by the client.
        /// </summary>
        /// <param name="targetType">The type used by the client.</param>
        /// <param name="value">The value on the server.</param>
        /// <returns>A value that can be used by the client.</returns>
        internal static object GetClientValue(Type targetType, object value)
        {
            if (value == null)
            {
                return null;
            }

            Binary valueAsBinary = value as Binary;
            if (targetType == typeof(byte[]) && valueAsBinary != null)
            {
                return valueAsBinary.ToArray();
            }

            return value;
        }

        /// <summary>
        /// Gets a value that can be used by the server.
        /// </summary>
        /// <param name="targetType">The type used by the server.</param>
        /// <param name="value">The value from the client.</param>
        /// <returns>A value that can be used by the server.</returns>
        internal static object GetServerValue(Type targetType, object value)
        {
            if (value == null)
            {
                return null;
            }

            byte[] valueAsByteArray = value as byte[];
            if (targetType == typeof(Binary) && valueAsByteArray != null)
            {
                return new Binary(valueAsByteArray);
            }

            return value;
        }


        /// <summary>Filter callback for finding IQueryable implementations.</summary>
        /// <param name="m">Type to inspect.</param>
        /// <param name="filterCriteria">Filter criteria.</param>
        /// <returns>true if the specified type is an IQueryable of T; false otherwise.</returns>
        private static bool IQueryableTypeFilter(Type m, object filterCriteria)
        {
            Debug.Assert(m != null, "m != null");
            return m.IsGenericType && m.GetGenericTypeDefinition() == typeof(IQueryable<>);
        }

        /// <summary>Filter callback for finding IEnumerable implementations.</summary>
        /// <param name="m">Type to inspect.</param>
        /// <param name="filterCriteria">Filter criteria.</param>
        /// <returns>true if the specified type is an IEnumerable of T; false otherwise.</returns>
        private static bool IEnumerableTypeFilter(Type m, object filterCriteria)
        {
            Debug.Assert(m != null, "m != null");
            return m.IsGenericType && m.GetGenericTypeDefinition() == typeof(IEnumerable<>);
        }

        /// <summary>
        /// Returns the "T" in the IQueryable of T implementation of type.
        /// </summary>
        /// <param name="type">Type to check.</param>
        /// <param name="typeFilter">filter against which the type is checked</param>
        /// <returns>
        /// The element type for the generic IQueryable interface of the type,
        /// or null if it has none or if it's ambiguous.
        /// </returns>
        private static Type GetGenericInterfaceElementType(Type type, TypeFilter typeFilter)
        {
            Debug.Assert(type != null, "type != null");
            Debug.Assert(!type.IsGenericTypeDefinition, "!type.IsGenericTypeDefinition");

            if (typeFilter(type, null))
            {
                return type.GetGenericArguments()[0];
            }

            Type[] queriables = type.FindInterfaces(typeFilter, null);
            if (queriables != null && queriables.Length == 1)
            {
                return queriables[0].GetGenericArguments()[0];
            }
            else
            {
                return null;
            }
        }

        /// <summary>Checks if given property has the attribute set.</summary>
        /// <param name="target">Property desriptor.</param>
        /// <param name="attribute">Attribute to check for.</param>
        /// <returns>true if attribute is present, false otherwise.</returns>
        private static bool HasAttribute(PropertyDescriptor target, Type attribute)
        {
            return null != target.Attributes[attribute];
        }
    }
}