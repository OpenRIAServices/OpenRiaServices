using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace OpenRiaServices.Tools.SharedTypes
{
    /// <summary>
    /// Class that encapsulates the description of a code member in terms of strings.
    /// </summary>
    /// <remarks>
    /// This class can represent a type, a property, or a method.  It is meant to be
    /// used as a unique key and overrides <see cref="GetHashCode()"/> and 
    /// <see cref="Equals(object)"/> to convey value-based equality.
    /// </remarks>
    internal class CodeMemberKey
    {
        internal CodeMemberKeyKind KeyKind { get; set; }
        internal string TypeName { get; set; }
        internal string MemberName { get; set; }
        internal string[] ParameterTypeNames { get; set; }

        /// <summary>
        /// Creates a new instance of the <see cref="CodeMemberKey"/> class
        /// to describe a <see cref="Type"/>.
        /// </summary>
        /// <param name="typeName">The fully qualified type name.</param>
        /// <returns>A new instance which describes that type.</returns>
        internal static CodeMemberKey CreateTypeKey(string typeName)
        {
            Debug.Assert(!string.IsNullOrEmpty(typeName), "typeName cannot be null");
            return new CodeMemberKey()
            {
                KeyKind = CodeMemberKeyKind.TypeKey,
                TypeName = typeName
            };
        }

        /// <summary>
        /// Creates a new instance of the <see cref="CodeMemberKey"/> class
        /// to describe the a <see cref="Type"/> from the given <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> from which to construct the key.</param>
        /// <returns>A new instance which describes that type.</returns>
        internal static CodeMemberKey CreateTypeKey(Type type)
        {
            Debug.Assert(type != null, "type cannot be null");
            return CodeMemberKey.CreateTypeKey(type.AssemblyQualifiedName);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="CodeMemberKey"/> class that describes
        /// a property.
        /// </summary>
        /// <param name="typeName">The fully qualified type name declaring the property.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <returns>A new instance which describes that property.</returns>
        internal static CodeMemberKey CreatePropertyKey(string typeName, string propertyName)
        {
            Debug.Assert(!string.IsNullOrEmpty(typeName), "typeName cannot be null");
            Debug.Assert(!string.IsNullOrEmpty(propertyName), "propertyName cannot be null");
            return new CodeMemberKey()
            {
                KeyKind = CodeMemberKeyKind.PropertyKey,
                TypeName = typeName,
                MemberName = propertyName
            };
        }

        /// <summary>
        /// Creates a new instance of the <see cref="CodeMemberKey"/> class that describes
        /// a property.
        /// </summary>
        /// <param name="propertyInfo">The <see cref="PropertyInfo"/> of that property.</param>
        /// <returns>A new instance which describes that property.</returns>
        internal static CodeMemberKey CreatePropertyKey(PropertyInfo propertyInfo)
        {
            Debug.Assert(propertyInfo != null, "propertyInfo cannot be null");
            return CodeMemberKey.CreatePropertyKey(propertyInfo.DeclaringType.AssemblyQualifiedName, propertyInfo.Name);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="CodeMemberKey"/> class that describes
        /// a method.
        /// </summary>
        /// <param name="typeName">The fully qualified type name that declares the method.</param>
        /// <param name="methodName">The name of the method.</param>
        /// <param name="parameterTypeNames">The set of fully qualified type names of the method parameters.</param>
        /// <returns>A new instance that describes that method.</returns>
        internal static CodeMemberKey CreateMethodKey(string typeName, string methodName, string[] parameterTypeNames)
        {
            Debug.Assert(!string.IsNullOrEmpty(typeName), "typeName cannot be null");
            Debug.Assert(!string.IsNullOrEmpty(methodName), "methodName cannot be null");
            return new CodeMemberKey()
            {
                KeyKind = CodeMemberKeyKind.MethodKey,
                TypeName = typeName,
                MemberName = methodName,
                ParameterTypeNames = parameterTypeNames
            };
        }

        /// <summary>
        /// Creates a new instance of the <see cref="CodeMemberKey"/> class that describes
        /// a method or a constructor.
        /// </summary>
        /// <param name="methodBase">The <see cref="MethodBase"/> of the method or constructor.</param>
        /// <returns>A new instance that describes that method.</returns>
        internal static CodeMemberKey CreateMethodKey(MethodBase methodBase)
        {
            Debug.Assert(methodBase != null, "methodBase cannot be null");
            string[] parameterTypes = methodBase.GetParameters().Select<ParameterInfo, string>(p => p.ParameterType.AssemblyQualifiedName).ToArray();
            return CodeMemberKey.CreateMethodKey(methodBase.DeclaringType.AssemblyQualifiedName, methodBase.Name, parameterTypes);
        }

        /// <summary>
        /// Gets the <see cref="Type"/> for this key.  It may be null if
        /// the type does not exist in the current set of loaded assemblies.
        /// </summary>
        internal Type Type
        {
            get
            {
                return Type.GetType(this.TypeName, /*throwOnError*/ false);
            }
        }

        /// <summary>
        /// Gets the <see cref="PropertyInfo"/> from a property key.
        /// </summary>
        /// <remarks>
        /// This property is available only for a property key.
        /// </remarks>
        /// <value>The value may be <c>null</c> if the property does not actually exist.
        /// </value>
        internal PropertyInfo PropertyInfo
        {
            get
            {
                Debug.Assert(this.KeyKind == CodeMemberKeyKind.PropertyKey, "this key is not a property key");
                PropertyInfo propertyInfo = null;
                Type type = this.Type;
                if (type != null)
                {
                    propertyInfo = type.GetProperty(this.MemberName);
                }
                return propertyInfo;
            }
        }

        /// <summary>
        /// Gets the <see cref="MethodBase"/> from a method key.
        /// </summary>
        /// <remarks>
        /// This property is available only for a method key.
        /// </remarks>
        /// <value>The value may be <c>null</c> if the method does not actually exist.
        /// </value>
        internal MethodBase MethodBase
        {
            get
            {
                Debug.Assert(this.KeyKind == CodeMemberKeyKind.MethodKey, "this key is not a method key");
                MethodBase methodBase = null;
                Type type = this.Type;
                if (type != null)
                {
                    Type[] parameterTypes = this.ParameterTypes;
                    if (parameterTypes != null)
                    {
                        methodBase = type.GetMethod(this.MemberName, parameterTypes);
                        if (methodBase == null)
                        {
                            methodBase = type.GetConstructor(parameterTypes);
                        }
                    }
                }
                return methodBase;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the current key describes a constructor.
        /// </summary>
        internal bool IsConstructor
        {
            get
            {
                return this.KeyKind == CodeMemberKeyKind.MethodKey && string.Equals(".ctor", this.MemberName, StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Gets the set of <see cref="Type"/>s for the parameters of a method key.
        /// </summary>
        /// <remarks>
        /// This property is available only for a method key.
        /// </remarks>
        internal Type[] ParameterTypes
        {
            get
            {
                Debug.Assert(this.KeyKind == CodeMemberKeyKind.MethodKey, "this key is not a method key");
                Type[] types = (this.ParameterTypeNames != null)
                        ? this.ParameterTypeNames.Select<string, Type>(s => Type.GetType(s, /*throwOnError*/ false)).ToArray()
                        : Type.EmptyTypes;
                // Any type load failures force a null return which signals a problem.
                // A parameterless method returns an empty array
                return types.Any(t => t == null) ? null : types;
            }
        }

        /// <summary>
        /// Override to provide a unique hash code based on this key's data.
        /// </summary>
        /// <returns>A unique hash code.</returns>
        public override int GetHashCode()
        {
            int hashCode = this.KeyKind.GetHashCode() ^
                            (this.TypeName == null ? 0 : this.TypeName.GetHashCode()) ^
                            (this.MemberName == null ? 0 : this.MemberName.GetHashCode());
            if (this.ParameterTypeNames != null)
            {
                foreach (string parameterType in this.ParameterTypeNames)
                {
                    hashCode ^= (parameterType == null ? 0 : parameterType.GetHashCode());
                }
            }
            return hashCode;
        }

        /// <summary>
        /// Override to provide property equality checks using value-based comparison.
        /// </summary>
        /// <param name="obj">The object to compare against the current instance.</param>
        /// <returns><c>true</c> if the objects are equal.</returns>
        public override bool Equals(object obj)
        {
            CodeMemberKey other = obj as CodeMemberKey;
            if (Object.ReferenceEquals(other, null))
            {
                return false;
            }
            if (Object.ReferenceEquals(this, other))
            {
                return true;
            }
            if ((this.KeyKind != other.KeyKind) ||
                !string.Equals(this.TypeName, other.TypeName, StringComparison.Ordinal) ||
                !string.Equals(this.MemberName, other.MemberName, StringComparison.Ordinal))
            {
                return false;
            }
            int parameterCount = this.ParameterTypeNames == null ? 0 : this.ParameterTypeNames.Length;
            int otherParameterCount = other.ParameterTypeNames == null ? 0 : other.ParameterTypeNames.Length;
            if (parameterCount != otherParameterCount)
            {
                return false;
            }

            for (int i = 0; i < parameterCount; ++i)
            {
                if (!string.Equals(this.ParameterTypeNames[i], other.ParameterTypeNames[i], StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Nested internal enum type used by <see cref="CodeMemberKey"/>
        /// to identify the key type
        /// </summary>
        internal enum CodeMemberKeyKind
        {
            /// <summary>
            /// The key refers to a type
            /// </summary>
            TypeKey,

            /// <summary>
            /// The key refers to a property
            /// </summary>
            PropertyKey,

            /// <summary>
            /// The key refers to a method
            /// </summary>
            MethodKey
        }
    }
}
