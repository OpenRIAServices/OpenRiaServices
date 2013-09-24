using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using OpenRiaServices.DomainServices;
using System.Text;

namespace OpenRiaServices.DomainServices.Tools.TextTemplate
{
    internal static class CodeGenUtilities
    {
        private static Dictionary<Type, string> typeNamespaceTranslations = new Dictionary<Type, string>();
        private static Dictionary<string, Dictionary<string, string>> namespaceTypeReferences = new Dictionary<string, Dictionary<string, string>>();

        // List of C# keywords (from the msdn page - http://msdn.microsoft.com/en-us/library/x53a06bb.aspx)
        private static readonly HashSet<string> keywords = new HashSet<string> 
            {
                "abstract", "as", "async", "await", "base", "bool", "break", "byte", "case", "catch", "char", "checked", 
                "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for", 
                "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out", "override", "params", 
                "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", 
                "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while"
            };

        // List of C# built-in types (from the msdn page - http://msdn.microsoft.com/en-us/library/ya5y69ds.aspx).
        private static readonly HashSet<string> typeKeywords = new HashSet<string> 
            { 
                "bool", "byte", "sbyte", "char", "decimal", "double", "float", "int", "uint", "long", "ulong", "object", "short", "ushort", "string", "void"
            };

        private static readonly HashSet<string> nonTypeKeywords = new HashSet<string>(keywords.Except(typeKeywords));

        private static readonly Dictionary<Type, string> keywordTypeNames = new Dictionary<Type, string>()
        {
            { typeof(Boolean), "bool" },
            { typeof(SByte), "sbyte" },
            { typeof(Byte), "byte" },
            { typeof(UInt16), "ushort" },
            { typeof(Int16), "short" },
            { typeof(Int32), "int" },
            { typeof(UInt32), "uint" },
            { typeof(Int64), "long" },
            { typeof(UInt64), "ulong" },
            { typeof(Char), "char" },
            { typeof(Double), "double" },
            { typeof(Single), "float" },
            { typeof(String), "string" }
        };

        // Integral MinValue/MaxValue pairs
        private static readonly Dictionary<Type, object[]> integralMinMaxValues = new Dictionary<Type, object[]>()
        {
            { typeof(byte), new object[] { byte.MinValue, byte.MaxValue, (byte) 0 } },
            { typeof(sbyte), new object[] { sbyte.MinValue, sbyte.MaxValue, (sbyte) 0 } },
            { typeof(short), new object[] { short.MinValue, short.MaxValue, (short) 0 } },
            { typeof(ushort), new object[] { ushort.MinValue, ushort.MaxValue, (ushort) 0 } },
            { typeof(int), new object[] { int.MinValue, int.MaxValue, (int) 0 } },
            { typeof(uint), new object[] { uint.MinValue, uint.MaxValue, (uint) 0 } },
            { typeof(long), new object[] { long.MinValue, long.MaxValue, (long) 0 } },
            { typeof(ulong), new object[] { ulong.MinValue, ulong.MaxValue, (ulong) 0 } }
        };

        internal static Dictionary<Type, object[]> IntegralMinMaxValues
        {
            get
            {
                return CodeGenUtilities.integralMinMaxValues;
            }
        }

        internal static Type TranslateType(Type type)
        {
            if (BinaryTypeUtility.IsTypeBinary(type))
            {
                return typeof(byte[]);
            }

            // Don't translate array types.
            if (!type.IsArray && TypeUtility.IsPredefinedListType(type))
            {
                return typeof(IEnumerable<>).MakeGenericType(TypeUtility.GetElementType(type));
            }

            if (TypeUtility.IsPredefinedDictionaryType(type))
            {
                Type[] genericArgs = GetDictionaryGenericArgumentTypes(type).Select(t => TranslateType(t)).ToArray();

                // TODO: translate to IDictionary<,> when we can properly support 
                //       IDictonary<,> parameters in a serialized changeset.
                return typeof(Dictionary<,>).MakeGenericType(genericArgs);
            }

            return type;
        }

        /// <summary>
        /// Checks if the variable name is a keyword, in which case appends a '@' sign before the name
        /// </summary>
        /// <param name="name">Name of the variable</param>
        /// <returns>Valid variable name</returns>
        internal static string GetSafeName(string name)
        {
            name = (keywords.Contains(name) ? "@" : string.Empty) + name;
            return name;
        }

        /// <summary>
        ///  Gets the fieldName from the name of the property.
        /// </summary>
        /// <param name="fieldName">Original fieldName.</param>
        /// <returns>Compliant field name.</returns>
        internal static string MakeCompliantFieldName(string fieldName)
        {
            // If the fieldName starts with an "@", strip it out.
            fieldName = fieldName.StartsWith("@", StringComparison.Ordinal) ? fieldName.Substring(1) : fieldName;

            const string Prefix = "_";
            int fieldLen = fieldName.Length;

            // First character is lower-case, so just return the string as-is.
            if (Char.IsLower(fieldName[0]))
            {
                return Prefix + fieldName;
            }

            for (int i = 0; i < fieldLen - 1; i++)
            {
                // Check if the next character is lower-case. E.g. ISBNValue should become _isbnValue.
                if (Char.IsLower(fieldName[i + 1]))
                {
                    // If the second character is lower-case, all we need to do is convert the first two 
                    // characters to lower-case. E.g. MyISBNValue should become _myISBNValue.
                    if (i == 0)
                    {
                        i = 1;
                    }
                    return Prefix + fieldName.Substring(0, i).ToLowerInvariant() + fieldName.Substring(i);
                }
            }

            // No lower-case chars in the string. Make the entire string lower-case. E.g. ISBN should become _isbn.
            return Prefix + fieldName.ToLowerInvariant();
        }

        internal static Type[] GetDictionaryGenericArgumentTypes(Type type)
        {
            Type genericType;
            if (typeof(IDictionary<,>).DefinitionIsAssignableFrom(type, out genericType))
            {
                return genericType.GetGenericArguments();
            }

            return null;
        }

        internal static string GetBooleanString(bool value, bool isCSharp)
        {
            if (isCSharp)
            {
                if (value)
                {
                    return "true";
                }
                else
                {
                    return "false";
                }
            }
            else
            {
                return value.ToString();
            }
        }

        /// <summary>
        /// Gets the full name of the type to be used in the generated code.
        /// </summary>
        /// <param name="type">Type of which the name is to be returned.</param>
        /// <returns>The full name of the type.</returns>
        internal static string GetTypeName(Type type)
        {
            Debug.Assert(type != null, "Type should not be null");

            string typeName = string.Empty;
            if (type == typeof(void))
            {
                typeName = "void";
            }
            else if (type.IsArray)
            {
                string arrayTypeName = GetGenericTypeName(type);
                if (arrayTypeName != null)
                {
                    typeName = arrayTypeName + "[]";
                }
            }
            else if (type.IsGenericType)
            {
                typeName = GetGenericTypeName(type);
            }
            else
            {
                CodeGenUtilities.keywordTypeNames.TryGetValue(type, out typeName);
            }

            if (string.IsNullOrEmpty(typeName))
            {
                // type.FullName and type.ToString differ for generic types and if there is no namespace. So we check for FullName first 
                // and if it is null or empty, fall back to type.ToString()
                typeName = string.IsNullOrEmpty(type.FullName) ? type.ToString() : type.FullName;
            }

            typeName = CodeGenUtilities.GetValidTypeName(typeName);
            return typeName;
        }

        /// <summary>
        /// This method handles cases where the name of the type is some keyword like "@namespace". But we want to leave out the cases where the keyword is the type, like "int".
        /// </summary>
        /// <param name="typeName">The name of the type.</param>
        /// <returns>The modified valid name of the type.</returns>
        private static string GetValidTypeName(string typeName)
        {            
            string name = typeName;
            string adjustedTypeName = typeName;

            int nameIndex = typeName.LastIndexOf('.') + 1;
            if (nameIndex > 0)
            {
                name = typeName.Substring(nameIndex);
            }

            if (!string.IsNullOrEmpty(name) && CodeGenUtilities.nonTypeKeywords.Contains(name))
            {
                name = "@" + name;
                adjustedTypeName = typeName.Substring(0, nameIndex) + name;
            }

            return adjustedTypeName;
        }

        /// <summary>
        /// Returns the full name of the generic type.
        /// </summary>
        /// <param name="type">The generic type.</param>
        /// <returns>Full name of the type.</returns>
        private static string GetGenericTypeName(Type type)
        {
            string typeName = type.ToString();
            int index = typeName.IndexOf('`');
            if (index < 0)
            {
                return null;
            }
            typeName = typeName.Substring(0, index);
            Type[] typeArgs = type.GetGenericArguments();
            string typeArgsString = string.Empty;
            for (int i = 0; i < typeArgs.Length; i++)
            {
                typeArgsString += GetTypeName(typeArgs[i]);
                if (i + 1 < typeArgs.Length)
                {
                    typeArgsString += ", ";
                }
            }
            typeName = typeName + "<" + typeArgsString + ">";
            return typeName;
        }


        internal static string GetTypeNameInGlobalNamespace(Type type)
        {
            string typeName = CodeGenUtilities.GetTypeName(type);
            typeName = "global::" + typeName;
            return typeName;
        }

        internal static string TranslateNamespace(Type type)
        {
            // Set default namespace
            string typeNamespace;

            if (!typeNamespaceTranslations.TryGetValue(type, out typeNamespace))
            {
                // Set the appropriate namespace
                typeNamespace = type.Namespace;

                // Cache the value for next time
                typeNamespaceTranslations[type] = typeNamespace;
            }

            return typeNamespace;
        }

        internal static bool RegisterTypeName(Type type, string containingNamespace)
        {
            // Translate generic types
            if (type.IsGenericType)
            {
                type = type.GetGenericTypeDefinition();
            }

            string typeName = type.Name;
            string typeNamespace = type.Namespace;

            if (containingNamespace == null)
            {
                // Visual basic uses 1 single root namespace for imports.
                // Types in the global namespace will have a null namespace name
                containingNamespace = string.Empty;
            }

            // Register namespace
            if (!namespaceTypeReferences.ContainsKey(containingNamespace))
            {
                namespaceTypeReferences.Add(containingNamespace, new Dictionary<string, string>(StringComparer.Ordinal));
            }

            bool isConflict = false;
            string existingTypeName = null;
            string fullTypeName = string.IsNullOrEmpty(typeNamespace) ? typeName : string.Concat(typeNamespace, ".", typeName);

            // Check for conflict
            isConflict =
                namespaceTypeReferences[containingNamespace].TryGetValue(typeName, out existingTypeName) &&
                existingTypeName != fullTypeName;

            // Register namespace type reference
            if (!isConflict && existingTypeName == null)
            {
                namespaceTypeReferences[containingNamespace].Add(typeName, fullTypeName);
            }

            return isConflict;
        }
    }
}
