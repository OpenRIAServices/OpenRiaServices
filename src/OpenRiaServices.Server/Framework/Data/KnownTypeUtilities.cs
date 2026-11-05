using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using PolyType;

namespace OpenRiaServices.Server
{
    /// <summary>
    /// Utility class to deal with <see cref="KnownTypeAttribute"/>
    /// </summary>
    internal static class KnownTypeUtilities
    {
        /// <summary>
        /// Obtains the set of known types from the <see cref="KnownTypeAttribute"/> custom attributes
        /// attached to the specified <paramref name="type"/>.
        /// </summary>
        /// <remarks>
        /// This utility function duplicates what WCF does by either retrieving the declared
        /// types or invoking the method declared in <see cref="KnownTypeAttribute.MethodName"/>.
        /// </remarks>
        /// <param name="type">The type to examine for <see cref="KnownTypeAttribute"/>s</param>
        /// <param name="inherit"><c>true</c> to allow inheritance of <see cref="KnownTypeAttribute"/> from the base.</param>
        /// <returns>The distinct set of types found via the <see cref="KnownTypeAttribute"/>s</returns>
        internal static HashSet<Type> ImportKnownTypes(Type type, bool inherit)
        {
            HashSet<Type> knownTypes = new HashSet<Type>();
            IEnumerable<KnownTypeAttribute> knownTypeAttributes = type.GetCustomAttributes(typeof(KnownTypeAttribute), inherit).Cast<KnownTypeAttribute>();

            foreach (KnownTypeAttribute knownTypeAttribute in knownTypeAttributes)
            {
                Type knownType = knownTypeAttribute.Type;
                if (knownType != null)
                {
                    knownTypes.Add(knownType);
                }

                string methodName = knownTypeAttribute.MethodName;
                if (!string.IsNullOrEmpty(methodName))
                {
                    Type typeOfIEnumerableOfType = typeof(IEnumerable<Type>);
                    MethodInfo methodInfo = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly);
                    if (methodInfo != null && typeOfIEnumerableOfType.IsAssignableFrom(methodInfo.ReturnType))
                    {
                        IEnumerable<Type> enumerable = methodInfo.Invoke(null, null) as IEnumerable<Type>;
                        if (enumerable != null)
                        {
                            knownTypes.UnionWith(enumerable);
                        }
                    }
                }
            }
            return knownTypes;
        }

        /// <summary>
        /// Obtains the derived types registered with PolyType or data contract attributes.
        /// </summary>
        /// <remarks>
        /// Direct <see cref="DerivedTypeShapeAttribute"/> declarations take precedence over
        /// <see cref="KnownTypeAttribute"/> declarations on each type. Registrations that are
        /// not assignable to <paramref name="type"/> are excluded.
        /// </remarks>
        internal static HashSet<Type> ImportDerivedTypes(Type type, bool inherit)
        {
            HashSet<Type> derivedTypes = new HashSet<Type>();
            Queue<Type> pendingTypes = new Queue<Type>();
            pendingTypes.Enqueue(type);

            if (inherit && type != typeof(object))
            {
                for (Type currentType = type.BaseType; currentType != null && currentType != typeof(object); currentType = currentType.BaseType)
                {
                    pendingTypes.Enqueue(currentType);
                }
            }

            while (pendingTypes.Count > 0)
            {
                Type currentType = pendingTypes.Dequeue();

                DerivedTypeShapeAttribute[] derivedTypeShapeAttributes = (DerivedTypeShapeAttribute[])currentType.GetCustomAttributes(typeof(DerivedTypeShapeAttribute), inherit: false);

                IEnumerable<Type> types = (derivedTypeShapeAttributes.Length > 0)
                    ? derivedTypeShapeAttributes.Select(attribute => attribute.Type)
                    : ImportKnownTypes(currentType, inherit: false);

                foreach (var derivedType in types)
                {
                    if (type.IsAssignableFrom(derivedType) && derivedTypes.Add(derivedType))
                    {
                        pendingTypes.Enqueue(derivedType);
                    }
                }
            }

            return derivedTypes;
        }
    }
}
