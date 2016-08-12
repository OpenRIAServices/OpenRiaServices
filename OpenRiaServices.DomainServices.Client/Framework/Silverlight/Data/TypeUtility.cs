using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
#if !SILVERLIGHT
using System.Runtime.Serialization;
#endif

#if SERVERFX
using OpenRiaServices.DomainServices.Server;
#else
using OpenRiaServices.DomainServices.Client;
#endif

namespace OpenRiaServices.DomainServices
{
    internal static class TypeUtility
    {
        internal const string OpenRiaServicesPublicKeyToken = "2e0b7ccb1ae5b4c8";
        internal static readonly byte[] OpenRiaServicesPublicKeyTokenBytes = PublicKeyTokenToBytes(OpenRiaServicesPublicKeyToken);

        /// <summary>
        /// List of public key tokens used for System assemblies
        /// </summary>
        private static string[] systemAssemblyPublicKeyTokens =
        {
            "b77a5c561934e089", // mscorlib, System, System.ComponentModel.Composition, and System.Core
            "31bf3856ad364e35", // System.ComponentModel.DataAnnotations
            "b03f5f7f11d50a3a", // Microsoft.VisualBasic, Microsoft.CSharp, System.Configuration
            "7cec85d7bea7798e",  // Silverlight system assemblies
            OpenRiaServicesPublicKeyToken, // OpenRiaServices.DomainServices.
        };

        /// <summary>
        /// The list of assemblies that form OpenRiaServices. If OpenRiaServices is extended with
        /// additional assemblies, or if assemblies are removed, this array must be updated accordingly.
        /// </summary>
        private static readonly string[] OpenRiaServicesAssemblyNames =
        {
            "OpenRiaServices.DomainServices.Client",
            "OpenRiaServices.DomainServices.Client.Web",
            "OpenRiaServices.DomainServices.EntityFramework",
            "OpenRiaServices.DomainServices.EntityFramework.EF4",
            "OpenRiaServices.DomainServices.Hosting",
            "OpenRiaServices.DomainServices.Hosting.Endpoint",
            "OpenRiaServices.DomainServices.Hosting.Local",
            "OpenRiaServices.DomainServices.Hosting.OData",
            "OpenRiaServices.DomainServices.LinqToSql",
            "OpenRiaServices.DomainServices.Server",
            "OpenRiaServices.DomainServices.Server.UnitTesting",
            "OpenRiaServices.DomainServices.Tools",
            "OpenRiaServices.DomainServices.Tools.TextTemplate"
        };

        private static byte[] PublicKeyTokenToBytes(string publicKeyToken)
        {
            if(publicKeyToken == null || publicKeyToken.Length != 16)
                return new byte[0];
            else
            {
                var bytes = new byte[8];
                for (int i = 0; i < bytes.Length; ++i)
                    bytes[i] = Convert.ToByte(publicKeyToken.Substring(2 * i, 2), fromBase: 16);
                return bytes;
            }
        }

#if !WIZARD
        // list of "simple" types we will always accept for
        // serialization, inclusion from entities, etc.
        // Primitive types are not here -- test for them via ReflectionUtility.IsPrimitive(type)
        private static HashSet<Type> predefinedTypes = new HashSet<Type>
        {
            typeof(string),
            typeof(decimal),
            typeof(DateTime),
            typeof(DateTimeOffset),
            typeof(TimeSpan),
            typeof(Guid),
            typeof(Uri)
        };

#region FRAMEWORK_INDEPENDENT_REFLECTION
#if REFLECTION_V2
        public static IEnumerable<Attribute> GetCustomAttributes(this Type type, bool inherit)
        {
            return type.GetTypeInfo().GetCustomAttributes(inherit);
        }

        public static IEnumerable<Attribute> GetCustomAttributes(this Type type, Type attributeType, bool inherit)
        {
            return type.GetTypeInfo().GetCustomAttributes(attributeType, inherit);
        }
#endif

#if REFLECTION_V2
        public static TypeInfo GetTypeInfo(Type type)
        {
            return type.GetTypeInfo();
        }
#else
        public static Type GetTypeInfo(this Type type)
        {
            return type;
        }
#endif

        public static bool IsGenericType(Type type)
        {
            return GetTypeInfo(type).IsGenericType;
        }

        public static Type GetBaseType(Type type)
        {
            return GetTypeInfo(type).BaseType;
        }

        public static bool IsEnum(Type type)
        {
            return GetTypeInfo(type).IsEnum;
        }

        public static bool IsInterface(Type type)
        {
            return GetTypeInfo(type).IsInterface;
        }

        public static bool IsPrimitive(Type type)
        {
            return GetTypeInfo(type).IsPrimitive;
        }

        public static bool IsAbstract(Type type)
        {
            return GetTypeInfo(type).IsAbstract;
        }

        public static bool IsValueType(Type type)
        {
            return GetTypeInfo(type).IsValueType;
        }

        public static Assembly GetAssembly(Type type)
        {
            return GetTypeInfo(type).Assembly;
        }

        public static bool IsAssignableFrom(this Type type, Type c)
        {
            return GetTypeInfo(type).IsAssignableFrom(GetTypeInfo(c));
        }

        /// <summary>
        /// Determines if a specific attribute is defined on a property.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="attributeType">the type of attribute to look for</param>
        /// <param name="inherit"></param>
        /// <returns><c>true</c> if the attribute is defined, otherwise <c>false</c></returns>
        public static bool IsAttributeDefined(PropertyInfo property, Type attributeType, bool inherit)
        {
#if REFLECTION_V2
            return property.GetCustomAttributes(attributeType, inherit).Any();
#else
            return property.IsDefined(attributeType, inherit);
#endif
        }

        /// <summary>
        /// Determines if a specific attribute is defined for a type.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="attributeType">the type of attribute to look for</param>
        /// <param name="inherit"></param>
        /// <returns><c>true</c> if the attribute is defined, otherwise <c>false</c></returns>
        public static bool IsAttributeDefined(Type type, Type attributeType, bool inherit)
        {
#if REFLECTION_V2
            return type.GetCustomAttributes(attributeType, inherit).Any();
#else
            return type.IsDefined(attributeType, inherit);
#endif
        }
    
        #endregion

        public static bool GetSingleAttributeOrNull(PropertyInfo property, Type attributeType, bool inherit)
        {
            var properties = property.GetCustomAttributes(attributeType, inherit);

#if REFLECTION_V2
            using (var enumerator = properties.GetEnumerator())
            {
                // Check for first item
                if (!enumerator.MoveNext())
                    return false;
                var attribute = enumerator.Current;

                // Check for second item
                if (enumerator.MoveNext())
                    return false;
                else
                    return true;
            }
#else
            return properties.Length == 1;
#endif
        }

#if PORTABLE
        private static Type[] s_emptyTypes = new Type[0];
        public static Type[] EmptyTypes
        {
            get 
            {
                return s_emptyTypes;
            }
        }
#else
        public static Type[] EmptyTypes { get { return Type.EmptyTypes; } }
#endif


        /// <summary>
        /// Returns <c>true</c> if the given type is a <see cref="Nullable"/>
        /// </summary>
        /// <param name="type">The type to test</param>
        /// <returns><c>true</c> if the given type is a nullable type</returns>
        public static bool IsNullableType(Type type)
        {
            return IsGenericType(type) && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        /// <summary>
        /// Returns <c>true</c> if the given type is a <see cref="Task"/>
        /// </summary>
        /// <param name="type">The type to test</param>
        /// <returns><c>true</c> if the given type is a Task or Task{T}</returns>
        public static bool IsTaskType(Type type)
        {
            return type == typeof(Task)
                || (IsGenericType(type) && type.GetGenericTypeDefinition() == typeof(Task<>));
        }

        public static Type GetTaskReturnType(Type type)
        {
            if (type == typeof(Task))
                return typeof(void);
            else if (IsGenericType(type) && type.GetGenericTypeDefinition() == typeof(Task<>))
                return type.GetGenericArguments()[0];
            else
                throw new ArgumentException("Type must be either Task, or Task<T>", "type");
        }

        /// <summary>
        /// If the given type is <see cref="Nullable"/>, returns the element type,
        /// otherwise simply returns the input type
        /// </summary>
        /// <param name="type">The type to test that may or may not be Nullable</param>
        /// <returns>Either the input type or, if it was Nullable, its element type</returns>
        public static Type GetNonNullableType(Type type)
        {
            return IsNullableType(type) ? type.GetGenericArguments()[0] : type;
        }

        /// <summary>
        /// Returns <c>true</c> if the given type is a primitive type or one
        /// of our standard acceptable simple types, such as <see cref="String"/>,
        /// <see cref="Guid"/>, or one of our standard generic types whose generic
        /// argument is primitive or simple (e.g. Nullable, IEnumerable, IDictionary&lt;TKey,TValue&gt;).
        /// </summary>
        /// <param name="type">The type to test</param>
        /// <returns><c>true</c> if the type is a primitive or standard acceptable types</returns>
        public static bool IsPredefinedType(Type type)
        {
            return IsPredefinedSimpleType(type) ||
                   IsPredefinedListType(type) ||
                   IsPredefinedDictionaryType(type);
        }

        /// <summary>
        /// Returns <c>true</c> if the given type is <see cref="IEnumerable&lt;T&gt;"/> or an <see cref="IList"/> type, 
        /// and is either an interface, an array, or has a default constructor.
        /// </summary>
        /// <param name="type">The type to test</param>
        /// <returns><c>true</c> if the type is a primitive or standard acceptable types</returns>
        public static bool IsPredefinedListType(Type type)
        {
            if (IsSupportedCollectionType(type))
            {
                Type elementType = GetElementType(type);
                return IsPredefinedSimpleType(elementType);
            }

            return false;
        }

        /// <summary>
        /// Returns <c>true</c> if specified type is a supported collection Type. This method only checks the collection
        /// Type itself, not whether the element Type is supported.
        /// </summary>
        /// <param name="type">The type to test</param>
        /// <returns><c>true</c> if the type is a suppored collection Type.</returns>
        public static bool IsSupportedCollectionType(Type type)
        {
            if (type.IsArray ||
               (IsGenericType(type) && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)) ||
               (typeof(IList).IsAssignableFrom(type) && type.GetConstructor(EmptyTypes) != null))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns <c>true</c> if <paramref name="type"/> implements <see cref="IDictionary&lt;TKey,TValue&gt;"/> and
        /// its generic type arguments are acceptable predefined simple types.
        /// </summary>
        /// <param name="type">The type to test.</param>
        /// <returns><c>true</c> if the type is a <see cref="IDictionary&lt;TKey,TValue&gt;"/> with supported generic type arguments.</returns>
        public static bool IsPredefinedDictionaryType(Type type)
        {
            Type genericType;

            if (typeof(IDictionary<,>).DefinitionIsAssignableFrom(type, out genericType))
            {
                return genericType.GetGenericArguments().All(t => IsPredefinedSimpleType(t));
            }

            return false;
        }

        /// <summary>
        /// Returns <c>true</c> if the given type is either primitive or one of our
        /// standard acceptable simple types, such as <see cref="String"/>,
        /// <see cref="Guid"/>, etc
        /// </summary>
        /// <param name="type">The type to test</param>
        /// <returns><c>true</c> if the type is a primitive or standard acceptable types</returns>
        public static bool IsPredefinedSimpleType(Type type)
        {
            type = GetNonNullableType(type);

            // primitive types (except IntPtr and UIntPtr) are supported
            if (IsPrimitive(type) && type != typeof(IntPtr) && type != typeof(UIntPtr))
            {
                return true;
            }

            if (IsEnum(type))
            {
                return true;
            }

            if (predefinedTypes.Contains(type))
            {
                return true;
            }

            if (BinaryTypeUtility.IsTypeBinary(type))
            {
                return true;
            }

            // We test XElement by Type Name so our client framework assembly can avoid
            // taking an assembly reference to System.Xml.Linq
            if (string.Equals(type.FullName, "System.Xml.Linq.XElement", StringComparison.Ordinal))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// This method determines if the specified Type should be treated as a
        /// complex type by the framework.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if the type is a complex type, false otherwise.</returns>
        public static bool IsComplexType(Type type)
        {
#if !SERVERFX
            // Client side we can rely on derivaition from ComplexObject
            if (!typeof(ComplexObject).IsAssignableFrom(type))
            {
                return false;
            }
#else
            if (!type.IsVisible || TypeUtility.IsGenericType(type) || IsAbstract(type))
            {
                return false;
            }

            if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                return false;
            }

            if (!type.IsClass)
            {
                return false;
            }

            if (type.GetConstructor(Type.EmptyTypes) == null)
            {
                return false;
            }

            if (TypeUtility.IsPredefinedType(type))
            {
                return false;
            }

            // can't be a framework type
            if (IsSystemAssembly(type.Assembly))
            {
                return false;
            }

            // server side only checks
            // can't be an entity
            if (TypeDescriptor.GetProperties(type).Cast<PropertyDescriptor>().Any(p => p.Attributes[typeof(KeyAttribute)] != null))
            {
                return false;
            }
#endif
            return true;
        }
        
        /// <summary>
        /// Determines whether the specified type is one of the supported collection types
        /// with a complex element type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if the type is a supported complex collection type, false otherwise.</returns>
        public static bool IsComplexTypeCollection(Type type)
        {
            // This check doesn't include dictionary types, since dictionaries of CTs aren't supported currently
            return TypeUtility.IsSupportedCollectionType(type) && TypeUtility.IsComplexType(TypeUtility.GetElementType(type));
        }

        /// <summary>
        /// Determines whether the specified type is a complex type or a collection of
        /// complex types.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if the specified type is a complex type or a collection of
        /// complex types, false otherwise.</returns>
        public static bool IsSupportedComplexType(Type type)
        {
            return TypeUtility.IsComplexType(type) || TypeUtility.IsComplexTypeCollection(type);
        }

        /// <summary>
        /// Returns the underlying element type starting from a given type.
        /// </summary>
        /// <remarks>
        /// Simple types simply return the input type.
        /// If the given type is an array, this method returns the array's
        /// element type.
        /// If the type is a generic type of <see cref="IEnumerable"/>, 
        /// or <see cref="Nullable"/>, this method returns the element
        /// type of the generic parameter
        /// </remarks>
        /// <param name="type"><see cref="Type"/> to examine.</param>
        /// <returns>The underlying element type starting from the given type</returns>
        public static Type GetElementType(Type type)
        {
            // Any simple type has no element type -- it is the element type itself
            if (IsPredefinedSimpleType(type))
            {
                return type;
            }

            // Array, pointers, etc.
            if (type.HasElementType)
            {
                return type.GetElementType();
            }

            // IEnumerable<T> returns T
            Type ienum = FindIEnumerable(type);
            if (ienum != null)
            {
                Type genericArg = ienum.GetGenericArguments()[0];
                return genericArg;
            }

            return type;
        }

        /// <summary>
        /// Determines whether the generic type definition is assignable from the derived type.
        /// </summary>
        /// <remarks>
        /// This behaves just like <see cref="Type.IsAssignableFrom"/> except that it determines
        /// whether any generic type that can be made from the <paramref name="genericTypeDefinition"/>
        /// is assignable from <paramref name="derivedType"/>.
        /// </remarks>
        /// <param name="genericTypeDefinition">The generic type definition</param>
        /// <param name="derivedType">The type to determine assignability from</param>
        /// <returns>Whether the type definition is assignable from the derived type</returns>
        internal static bool DefinitionIsAssignableFrom(this Type genericTypeDefinition, Type derivedType)
        {
            Type genericType = null;
            return DefinitionIsAssignableFrom(genericTypeDefinition, derivedType, out genericType);
        }

        /// <summary>
        /// Determines whether the generic type definition is assignable from the derived type.
        /// </summary>
        /// <remarks>
        /// This behaves just like <see cref="Type.IsAssignableFrom"/> except that it determines
        /// whether any generic type that can be made from the <paramref name="genericTypeDefinition"/>
        /// is assignable from <paramref name="derivedType"/>.
        /// </remarks>
        /// <param name="genericTypeDefinition">The generic type definition</param>
        /// <param name="derivedType">The type to determine assignability from</param>
        /// <param name="genericType">The generic base class or interface implemented by the derived
        /// type that can be made from the <paramref name="genericTypeDefinition"/>. This value is
        /// null when the method return false.
        /// </param>
        /// <returns>Whether the type definition is assignable from the derived type</returns>
        internal static bool DefinitionIsAssignableFrom(this Type genericTypeDefinition, Type derivedType, out Type genericType)
        {
            genericType = derivedType;

            while (genericType != null)
            {
                if (IsInterface(genericTypeDefinition))
                {
                    bool interfaceMatched = false;
                    foreach (Type interfaceType in genericType.GetInterfaces().Concat(new[] { derivedType }))
                    {
                        if (IsGenericType(interfaceType)  &&
                            genericTypeDefinition == interfaceType.GetGenericTypeDefinition())
                        {
                            interfaceMatched = true;
                            genericType = interfaceType;
                            break;
                        }
                    }
                    if (interfaceMatched)
                    {
                        break;
                    }
                }
                else
                {
                    if (IsGenericType(genericType) &&
                        genericTypeDefinition == genericType.GetGenericTypeDefinition())
                    {
                        break;
                    }
                }
                genericType = GetBaseType(genericType);
            }

            return genericType != null;
        }

        internal static Type FindIEnumerable(Type seqType)
        {
            if (seqType == null || seqType == typeof(string))
            {
                return null;
            }
            if (seqType.IsArray)
            {
                return typeof(IEnumerable<>).MakeGenericType(seqType.GetElementType());
            }
            if (IsGenericType(seqType))
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
            if (GetBaseType(seqType) != null && GetBaseType(seqType) != typeof(object))
            {
                return FindIEnumerable(GetBaseType(seqType));
            }
            return null;
        }
#endif

            /// <summary>
            /// Performs a check against an assembly to determine if it's a known
            /// System assembly.
            /// </summary>
            /// <param name="assembly">The assembly to check.</param>
            /// <returns><c>true</c> if the assembly is known to be a system assembly, otherwise <c>false</c>.</returns>
        internal static bool IsSystemAssembly(this Assembly assembly)
        {
            return IsSystemAssembly(assembly.FullName);
        }

        /// <summary>
        /// Performs a check against an <see cref="AssemblyName"/> to determine if it's a known
        /// System assembly.
        /// </summary>
        /// <param name="assemblyName">The assembly name to check.</param>
        /// <returns><c>true</c> if the assembly is known to be a system assembly, otherwise <c>false</c>.</returns>
        internal static bool IsSystemAssembly(this AssemblyName assemblyName)
        {
            return IsSystemAssembly(assemblyName.FullName);
        }

        /// <summary>
        /// Performs a check against an <see cref="AssemblyName"/> to determine if it's a known
        /// Open Ria assembly.
        /// </summary>
        /// <param name="assemblyName">The assembly name to check.</param>
        /// <returns><c>true</c> if the assembly is known to be a Open Ria assembly, otherwise <c>false</c>.</returns>
        internal static bool IsOpenRiaAssembly(this AssemblyName assemblyName)
        {
            // Return true if it is a Open Ria Services assembly
            if (OpenRiaServicesAssemblyNames.Contains(assemblyName.Name))
            {
                return true;
            }

            // parse the public key token
            int idx = assemblyName.FullName.IndexOf("PublicKeyToken=", StringComparison.OrdinalIgnoreCase);
            if (idx == -1)
            {
                return false;
            }
            string publicKeyToken = assemblyName.FullName.Substring(idx + 15);
            return OpenRiaServicesPublicKeyToken == publicKeyToken;
        }

        /// <summary>
        /// Check against an <see cref="AssemblyName"/> to determine if signed to create a strong name
        /// (it has public key token != null)
        /// </summary>
        /// <remarks>
        ///  This should not be confuse with assemblies signed with a certificate (authenticode signed).
        /// </remarks>
        /// <param name="assemblyName">The assembly name to check.</param>
        /// <returns><c>true</c> if the assembly is strongly named (signed), otherwise <c>false</c>.</returns>
        internal static bool IsSigned(this AssemblyName assemblyName)
        {
            return assemblyName.GetPublicKeyToken().Length > 0;
        }

        /// <summary>
        /// Performs a check against an assembly's full name to determine if it's a known
        /// System assembly.
        /// </summary>
        /// <remarks>
        /// We can't use Assembly.GetName().GetPublicKeyToken() since that requires FileIOPermissions.
        /// </remarks>
        /// <param name="assemblyFullName">The <see cref="AssemblyName.FullName"/> to check.</param>
        /// <returns><c>true</c> if the assembly is known to be a system assembly, otherwise <c>false</c>.</returns>
        internal static bool IsSystemAssembly(string assemblyFullName)
        {
            // Return true if it is a Open Ria Services assembly
            var assemblyName = new AssemblyName(assemblyFullName);
            if (OpenRiaServicesAssemblyNames.Contains(assemblyName.Name))
            {
                return true;
            }

            // parse the public key token
            int idx = assemblyFullName.IndexOf("PublicKeyToken=", StringComparison.OrdinalIgnoreCase);
            if (idx == -1)
            {
                return false;
            }

            try
            {
               string publicKeyToken = assemblyFullName.Substring(idx + 15, 16);
               return systemAssemblyPublicKeyTokens.Any(p => p.Equals(publicKeyToken, StringComparison.OrdinalIgnoreCase));
            }
            catch (ArgumentOutOfRangeException)
            {
               return false;
            }
        }
    }
}