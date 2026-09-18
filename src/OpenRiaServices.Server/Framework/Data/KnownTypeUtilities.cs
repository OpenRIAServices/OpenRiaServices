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
    /// Utility class to deal with <see cref="KnownTypeAttribute"/> and <see cref="DerivedTypeShapeAttribute"/>.
    /// </summary>
    internal static class KnownTypeUtilities
    {
        /// <summary>
        /// Obtains the set of known types from the polymorphism attributes
        /// attached to the specified <paramref name="type"/>.
        /// </summary>
        /// <remarks>
        /// <see cref="DerivedTypeShapeAttribute"/> takes precedence over <see cref="KnownTypeAttribute"/>
        /// on each declaring type, matching PolyType.
        /// </remarks>
        /// <param name="type">The type to examine for <see cref="KnownTypeAttribute"/>s</param>
        /// <param name="inherit"><c>true</c> to allow inheritance of <see cref="KnownTypeAttribute"/> from the base.</param>
        /// <returns>The distinct set of types found via the <see cref="KnownTypeAttribute"/>s</returns>
        internal static HashSet<Type> ImportKnownTypes(Type type, bool inherit)
        {
            HashSet<Type> knownTypes = new HashSet<Type>();
            for (Type currentType = type; currentType != null; currentType = inherit ? currentType.BaseType : null)
            {
                ImportDeclaredKnownTypes(currentType, knownTypes);
            }

            return knownTypes;
        }

        private static void ImportDeclaredKnownTypes(Type type, HashSet<Type> knownTypes)
        {
            DerivedTypeShapeAttribute[] derivedTypeShapeAttributes = type
                .GetCustomAttributes(typeof(DerivedTypeShapeAttribute), inherit: false)
                .Cast<DerivedTypeShapeAttribute>()
                .ToArray();

            if (derivedTypeShapeAttributes.Length > 0)
            {
                knownTypes.UnionWith(derivedTypeShapeAttributes.Select(attribute => attribute.Type));
                return;
            }

            IEnumerable<KnownTypeAttribute> knownTypeAttributes = type
                .GetCustomAttributes(typeof(KnownTypeAttribute), inherit: false)
                .Cast<KnownTypeAttribute>();

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
        }
    }
}
