using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace System.ServiceModel.DomainServices.Server
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
        /// <returns>The distinct set of types fould via the <see cref="KnownTypeAttribute"/>s</returns>
        internal static IEnumerable<Type> ImportKnownTypes(Type type, bool inherit)
        {
            IDictionary<Type, Type> knownTypes = new Dictionary<Type, Type>();
            IEnumerable<KnownTypeAttribute> knownTypeAttributes = type.GetCustomAttributes(typeof(KnownTypeAttribute), inherit).Cast<KnownTypeAttribute>();
            
            foreach (KnownTypeAttribute knownTypeAttribute in knownTypeAttributes)
            {
                Type knownType = knownTypeAttribute.Type;
                if (knownType != null)
                {
                    knownTypes[knownType] = knownType;
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
                            foreach (Type t in enumerable)
                            {
                                knownTypes[t] = t;
                            }
                        }
                    }
                }
            }
            return knownTypes.Keys;
        }
    }
}
