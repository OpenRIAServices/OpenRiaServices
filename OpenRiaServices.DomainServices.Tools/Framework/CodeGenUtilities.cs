using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using OpenRiaServices.DomainServices;

namespace OpenRiaServices.DomainServices.Tools
{
    /// <summary>
    /// A collection of code generation utilities.
    /// </summary>
    internal static class CodeGenUtilities
    {
        private const string DefaultDataContractSchema = "http://schemas.datacontract.org/2004/07/";
        private static bool isVisualBasic;
        private static bool useFullTypeNames;
        private static string rootNamespace;
        private static Dictionary<Assembly, Dictionary<string, string>> contractNamespaces;

        // Keyed by CodeNamespace+Type, cache of existing CodeTypeReferences
        // These are kept separate by CodeNamespace due to potentially different imports needed for each type.
        private static Dictionary<Tuple<CodeNamespace, Type>, CodeTypeReference> codeTypeReferences;

        private static Dictionary<Type, string> typeNamespaceTranslations;
        private static Dictionary<string, Dictionary<string, string>> namespaceTypeReferences;

        // Integral MinValue/MaxValue pairs
        private static Dictionary<Type, object[]> integralMinMaxValues = new Dictionary<Type, object[]>()
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

        /// <summary>
        /// Clears the static dictionaries used to track type and assembly references.
        /// </summary>
        /// <param name="isVisualBasic">A <see cref="Boolean"/> indicating whether Visual Basic code is being generated.</param>
        /// <param name="useFullTypeNames">A <see cref="Boolean"/> indicating whether to generate full type names.</param>
        /// <param name="rootNamespace">The root namespace used for Visual Basic codegen. Can be null or empty.</param>
        internal static void Initialize(bool isVisualBasic, bool useFullTypeNames, string rootNamespace)
        {
            CodeGenUtilities.isVisualBasic = isVisualBasic;
            CodeGenUtilities.useFullTypeNames = useFullTypeNames;
            CodeGenUtilities.rootNamespace = rootNamespace ?? string.Empty;
            CodeGenUtilities.contractNamespaces = new Dictionary<Assembly, Dictionary<string, string>>();
            CodeGenUtilities.codeTypeReferences = new Dictionary<Tuple<CodeNamespace, Type>, CodeTypeReference>();
            CodeGenUtilities.namespaceTypeReferences = new Dictionary<string, Dictionary<string, string>>();
            CodeGenUtilities.typeNamespaceTranslations = new Dictionary<Type, string>();
        }

        /// <summary>
        /// Helper to generate a field name with a leading underscore and lowercase first letter
        /// </summary>
        /// <param name="fieldName">the field name to process</param>
        /// <returns>a new field name that begins with an underscore and lowercase first letter</returns>
        internal static string MakeCompliantFieldName(string fieldName)
        {
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

        /// <summary>
        /// Creates an attribute declaration based on the specified attribute
        /// </summary>
        /// <param name="attributeType">type of the attribute</param>
        /// <param name="codeGenerator">A <see cref="CodeDomClientCodeGenerator"/>.</param>
        /// <param name="referencingType">The referencing type.</param>
        /// <returns>the attribute declaration</returns>
        internal static CodeAttributeDeclaration CreateAttributeDeclaration(Type attributeType, CodeDomClientCodeGenerator codeGenerator, CodeTypeDeclaration referencingType)
        {
            if (!typeof(Attribute).IsAssignableFrom(attributeType))
            {
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, Resource.Type_Must_Be_Attribute, attributeType.Name), "attributeType");
            }

            CodeTypeReference attribute = GetTypeReference(attributeType, codeGenerator, referencingType, true);
            return new CodeAttributeDeclaration(attribute);
        }

        /// <summary>
        /// Generates a test of the given expression against null (e.g. "if (x == null)" or "if (x is nothing)"
        /// </summary>
        /// <param name="value">The code expression to test</param>
        /// <returns>An expression that tests whether the input expression is null</returns>
        internal static CodeExpression MakeEqualToNull(CodeExpression value)
        {
            return new CodeBinaryOperatorExpression(value, CodeBinaryOperatorType.IdentityEquality, new CodePrimitiveExpression(null));
        }

        /// <summary>
        /// Generates a test of the given expression against not being null (e.g. "if (x != null)" or "if (x is nothing) = false"
        /// </summary>
        /// <param name="value">The value to test</param>
        /// <returns>The new code expression containing the test</returns>
        internal static CodeExpression MakeNotEqualToNull(CodeExpression value)
        {
            return new CodeBinaryOperatorExpression(value, CodeBinaryOperatorType.IdentityInequality, new CodePrimitiveExpression(null));
        }

        /// <summary>
        /// Generates a test to determine if the given expressions are not equal.
        /// </summary>
        /// <param name="clrType">The Type of the values being compared.</param>
        /// <param name="left">The left side of the test.</param>
        /// <param name="right">The right side of the test.</param>
        /// <param name="isCSharp">Indicates whether or not the output should be C# specific.</param>
        /// <returns>A new <see cref="CodeExpression"/>.</returns>
        internal static CodeExpression MakeNotEqual(Type clrType, CodeExpression left, CodeExpression right, bool isCSharp)
        {
            if (isCSharp)
            {
                return new CodeBinaryOperatorExpression(left, CodeBinaryOperatorType.IdentityInequality, right);
            }
            else
            {
                CodeExpression eq = MakeEqual(clrType, left, right, isCSharp);
                return new CodeBinaryOperatorExpression(eq, CodeBinaryOperatorType.ValueEquality, new CodePrimitiveExpression(false));
            }
        }

        /// <summary>
        /// Generates a test to determine if the given expressions are equal.
        /// </summary>
        /// <param name="clrType">The Type of the values being compared.</param>
        /// <param name="left">The left side of the test.</param>
        /// <param name="right">The right side of the test.</param>
        /// <param name="isCSharp">Indicates whether or not the output should be C# specific.</param>
        /// <returns>A new <see cref="CodeExpression"/>.</returns>
        internal static CodeExpression MakeEqual(Type clrType, CodeExpression left, CodeExpression right, bool isCSharp)
        {
            if (isCSharp)
            {
                return new CodeBinaryOperatorExpression(left, CodeBinaryOperatorType.IdentityEquality, right);
            }
            else
            {
                CodeExpression eq;
                if (clrType != null && clrType.IsGenericType && clrType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    eq = new CodeMethodInvokeExpression(left, "Equals", right);
                }
                else if (clrType != null && clrType.IsValueType)
                {
                    eq = new CodeBinaryOperatorExpression(
                            left,
                            CodeBinaryOperatorType.ValueEquality,
                            right);
                }
                else if (clrType == typeof(string))
                {
                    eq = new CodeMethodInvokeExpression(null, "String.Equals", left, right);
                }
                else
                {
                    eq = new CodeMethodInvokeExpression(null, typeof(object).Name + ".Equals", left, right);
                }
                return eq;
            }
        }

        /// <summary>
        /// Creates an expression for creating a delegate.
        /// </summary>
        /// <param name="isCSharp">If this is C#</param>
        /// <param name="delegateType">The delegate type</param>
        /// <param name="methodName">The name of the method on 'this' to invoke</param>
        /// <returns>A CodeExpression that will create a delegate to invoke that method</returns>
        internal static CodeExpression MakeDelegateCreateExpression(bool isCSharp, CodeTypeReference delegateType, string methodName)
        {
            // CSharp is able to coerce a simple method reference to the generic delegate (and in fact, generates
            // verbose code when asked to create the delegate).
            return isCSharp
                ? (CodeExpression)new CodeMethodReferenceExpression(new CodeThisReferenceExpression(), methodName)
                : (CodeExpression)new CodeDelegateCreateExpression(delegateType, new CodeThisReferenceExpression(), methodName);
        }

        /// <summary>
        /// Creates a statement for raising an event.
        /// </summary>
        /// <param name="isCSharp">If this is C#.</param>
        /// <param name="ev">The event to raise.</param>
        /// <param name="parameters">The parameters to pass when raising the event.</param>
        /// <returns>The statement that raises the event.</returns>
        internal static CodeStatement MakeEventRaiseStatement(bool isCSharp, CodeEventReferenceExpression ev, params CodeExpression[] parameters)
        {
            CodeDelegateInvokeExpression eventInvocation = new CodeDelegateInvokeExpression(ev, parameters);

            if (isCSharp)
            {
                CodeConditionStatement eventNotNullStatement = new CodeConditionStatement();
                eventNotNullStatement.Condition = CodeGenUtilities.MakeNotEqualToNull(new CodeVariableReferenceExpression(ev.EventName));
                eventNotNullStatement.TrueStatements.Add(eventInvocation);
                return eventNotNullStatement;
            }
            else
            {
                return new CodeExpressionStatement(eventInvocation);
            }
        }

        /// <summary>
        /// Gets the data-contract namespace for an entity.
        /// </summary>
        /// <param name="entityType">The type of entity.</param>
        /// <returns>Either the assembly-level contract namespace, or the entity type's CLR namespace.</returns>
        internal static string GetContractNamespace(Type entityType)
        {
            Dictionary<string, string> contractNamespaces;
            if (!CodeGenUtilities.contractNamespaces.TryGetValue(entityType.Assembly, out contractNamespaces))
            {
                contractNamespaces = new Dictionary<string, string>();
                ContractNamespaceAttribute[] contractNamespaceAttribs = (ContractNamespaceAttribute[])entityType.Assembly.GetCustomAttributes(typeof(ContractNamespaceAttribute), /* inherit */ true);
                if (contractNamespaceAttribs.Length > 0)
                {
                    foreach (ContractNamespaceAttribute attrib in contractNamespaceAttribs)
                    {
                        if (attrib.ClrNamespace != null)
                        {
                            contractNamespaces.Add(attrib.ClrNamespace, attrib.ContractNamespace);
                        }
                    }
                }
                CodeGenUtilities.contractNamespaces.Add(entityType.Assembly, contractNamespaces);
            }

            // See if there's a mapping for this entity type's namespace.
            string entityTypeNamespace = entityType.Namespace ?? string.Empty;
            string contractNamespace;
            if (contractNamespaces.TryGetValue(entityTypeNamespace, out contractNamespace))
            {
                return contractNamespace;
            }

            // No mapping - use schema + CLR namespace instead.
            return CodeGenUtilities.DefaultDataContractSchema + Uri.EscapeUriString(entityTypeNamespace);
        }

        /// <summary>
        /// Performs necessary type translation for the given type.
        /// This method is called during client proxy generation.
        /// </summary>
        /// <param name="type">type to be translated</param>
        /// <returns>translated type</returns>
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
        /// Returns an array of types that represent the generic type arguments used in <paramref name="type"/>'s
        /// implementation of <see cref="IDictionary&lt;TKey,TValue&gt;"/>.
        /// </summary>
        /// <param name="type">The type to examine.</param>
        /// <returns>An array of types that represent the generic type arguments used in <paramref name="type"/>'s
        /// implementation of <see cref="IDictionary&lt;TKey,TValue&gt;"/>. Returns null if <paramref name="type"/> does 
        /// not implement <see cref="IDictionary&lt;TKey,TValue&gt;"/>.</returns>
        internal static Type[] GetDictionaryGenericArgumentTypes(Type type)
        {
            Debug.Assert(TypeUtility.IsPredefinedDictionaryType(type), "The type is unknown");

            Type genericType;
            if (typeof(IDictionary<,>).DefinitionIsAssignableFrom(type, out genericType))
            {
                return genericType.GetGenericArguments();
            }

            return null;
        }

        /// <summary>
        /// Gets a <see cref="CodeTypeReference"/> for a CLR type.
        /// </summary>
        /// <param name="type">A CLR type.</param>
        /// <param name="codeGenerator">A <see cref="CodeDomClientCodeGenerator"/>.</param>
        /// <param name="referencingType">The referencing type.</param>
        /// <returns>A <see cref="CodeTypeReference"/> for a CLR type.</returns>
        internal static CodeTypeReference GetTypeReference(Type type, CodeDomClientCodeGenerator codeGenerator, CodeTypeDeclaration referencingType)
        {
            return GetTypeReference(type, codeGenerator, referencingType, false, false);
        }

        /// <summary>
        /// Gets a <see cref="CodeTypeReference"/> for a CLR type.
        /// </summary>
        /// <param name="type">A CLR type.</param>
        /// <param name="codeGenerator">A <see cref="CodeDomClientCodeGenerator"/>.</param>
        /// <param name="referencingType">The referencing type.</param>
        /// <param name="optimizeAttributeName">Indicates whether or not the optimize <see cref="Attribute"/> names by removing the "Attribute" suffix.</param>
        /// <returns>A <see cref="CodeTypeReference"/> for a CLR type.</returns>
        internal static CodeTypeReference GetTypeReference(Type type, CodeDomClientCodeGenerator codeGenerator, CodeTypeDeclaration referencingType, bool optimizeAttributeName)
        {
            return GetTypeReference(type, codeGenerator, referencingType, optimizeAttributeName, false);
        }

        /// <summary>
        /// Gets a <see cref="CodeTypeReference"/> for a CLR type.
        /// </summary>
        /// <param name="type">A CLR type.</param>
        /// <param name="codeGenerator">A <see cref="CodeDomClientCodeGenerator"/>.</param>
        /// <param name="referencingType">The referencing type.</param>
        /// <param name="optimizeAttributeName">Indicates whether or not to optimize <see cref="Attribute"/> names by removing the "Attribute" suffix.</param>
        /// <param name="forceUseFullyQualifiedName">Indicates whether or not to generate the type using the fully qualified name irrespective the global setting.</param>
        /// <returns>A <see cref="CodeTypeReference"/> for a CLR type.</returns>
        internal static CodeTypeReference GetTypeReference(Type type, CodeDomClientCodeGenerator codeGenerator, CodeTypeDeclaration referencingType, bool optimizeAttributeName, bool forceUseFullyQualifiedName)
        {
            string typeName = type.Name;
            string typeNamespace = type.Namespace;

            // Add an import statement to the referencing type if needed
            CodeNamespace ns = codeGenerator.GetNamespace(referencingType);
            CodeTypeReference codeTypeReference = null;

            // Attribute?  If so, we special case these and remove the 'Attribute' suffix if present.
            if (optimizeAttributeName)
            {
                typeName = OptimizeAttributeName(type);
            }

            // Determine if we should generate this type with a full type name
            bool useFullyQualifiedName = forceUseFullyQualifiedName || CodeGenUtilities.useFullTypeNames || RegisterTypeName(typeNamespace, typeName, ns.Name);

            // Make sure we take into account root namespace in VB codegen.
            typeNamespace = TranslateNamespace(type, codeGenerator);

            // Conditionally add an import statement.  Skip this step if we need to generate a full
            // type name, if we're already in the target namespace, or if the type is in the global namespace.
            if (!useFullyQualifiedName && !ns.Name.Equals(type.Namespace) && !string.IsNullOrEmpty(type.Namespace))
            {
                // If the namespace is already imported, the following line will be a no-op.
                ns.Imports.Add(new CodeNamespaceImport(typeNamespace));
            }

            // If forced using Fully Qualified names, dont look up or store the code reference in the cache. That is because, 
            // we force the use of fully qualified names only in certain cases. Caching at this time will cause the fully qualified name 
            // to be used every time. 
            bool useCache = !forceUseFullyQualifiedName;

            // See if we already have a reference for this type            
            Tuple<CodeNamespace, Type> tupleKey = new Tuple<CodeNamespace, Type>(ns, type);
            if (!useCache || !CodeGenUtilities.codeTypeReferences.TryGetValue(tupleKey, out codeTypeReference))
            {
                if (useFullyQualifiedName && !string.IsNullOrEmpty(typeNamespace))
                {
                    // While this splicing may seem awkward, we perform this task
                    // rather than rely on 'type.FullName' as we may have performed
                    // a VB root namespace translation task above.
                    typeName = typeNamespace + "." + typeName;
                }

                // If not, create a new type reference. Use the constructor for CodeTypeReference
                // that takes a type's name rather than type to generate short names.
                if (type.IsArray)
                {
                    codeTypeReference = new CodeTypeReference(
                        CodeGenUtilities.GetTypeReference(type.GetElementType(), codeGenerator, referencingType, /* optimizeAttributeName */ false, forceUseFullyQualifiedName),
                        type.GetArrayRank());
                }
                else if (type.IsGenericType)
                {
                    Type[] genericArguments = type.GetGenericArguments();
                    CodeTypeReference[] typeArguments = new CodeTypeReference[genericArguments.Length];
                    for (int i = 0; i < genericArguments.Length; i++)
                    {
                        typeArguments[i] = GetTypeReference(genericArguments[i], codeGenerator, referencingType);
                    }
                    codeTypeReference = new CodeTypeReference(typeName, typeArguments);
                }
                else
                {
                    // Generate language-specific shorthands for core types by using CodeTypeReference constructor that takes a Type
                    if (type.IsPrimitive || type == typeof(void) || type == typeof(decimal) || type == typeof(string) || type == typeof(object))
                    {
                        codeTypeReference = new CodeTypeReference(type);
                    }
                    else
                    {
                        codeTypeReference = new CodeTypeReference(typeName);
                    }
                }

                // Keep track of the CLR type for identification purposes.
                codeTypeReference.UserData["ClrType"] = type;

                // Cache for later use.
                if (useCache)
                {
                    CodeGenUtilities.codeTypeReferences.Add(tupleKey, codeTypeReference);
                }
            }

            return codeTypeReference;
        }

        /// <summary>
        /// Gets a <see cref="CodeTypeReference"/> for a given <see cref="Type"/> full name and containing namespace.
        /// </summary>
        /// <remarks>
        /// If <paramref name="userType"/> is <c>true</c>, the <paramref name="typeFullName"/> value will be optimized
        /// when we are emitting Visual Basic code with a non-null root namespace.
        /// </remarks>
        /// <param name="typeFullName">The <see cref="Type"/> full name.</param>
        /// <param name="containingNamespace">The namespace that will contain the <paramref name="typeFullName"/> reference.</param>
        /// <param name="userType">A <see cref="Boolean"/> indicating whether or not the <paramref name="typeFullName"/> is a user type.</param>
        /// <returns>A <see cref="CodeTypeReference"/> for the provided <paramref name="typeFullName"/>.</returns>
        internal static CodeTypeReference GetTypeReference(string typeFullName, string containingNamespace, bool userType)
        {
            string safeTypeName = GetSafeTypeName(typeFullName, containingNamespace, userType);
            CodeTypeReference codeTypeReference = new CodeTypeReference(safeTypeName);

            return codeTypeReference;
        }

        /// <summary>
        /// Returns a <see cref="Type"/> name that is safe to use.
        /// </summary>
        /// <remarks>
        /// This method is not safe for use with generic types.
        /// </remarks>
        /// <param name="typeFullName">The full name of the <see cref="Type"/>.</param>
        /// <param name="containingNamespace">The containing namespace.</param>
        /// <param name="userType">A <see cref="Boolean"/> indicating whether or not the <paramref name="typeFullName"/> is a user type.</param>
        /// <returns>A string representing the safe type name.</returns>
        private static string GetSafeTypeName(string typeFullName, string containingNamespace, bool userType)
        {
            if (string.IsNullOrEmpty(typeFullName))
            {
                throw new ArgumentNullException("typeFullName");
            }

            string typeName = typeFullName;
            string typeNamespace = string.Empty;

            int idx = typeFullName.LastIndexOf('.');
            if (idx != -1)
            {
                typeName = typeFullName.Substring(idx + 1);
                typeNamespace = typeFullName.Substring(0, idx);
            }

            if (useFullTypeNames)
            {
                bool prependRootNamespace = isVisualBasic && userType && !string.IsNullOrEmpty(rootNamespace) &&
                    !(typeNamespace.Equals(rootNamespace, StringComparison.Ordinal) || typeNamespace.StartsWith(rootNamespace + ".", StringComparison.Ordinal));

                if (prependRootNamespace)
                {
                    typeFullName = rootNamespace + "." + typeFullName;
                }

                return typeFullName;
            }

            bool useFullName = CodeGenUtilities.RegisterTypeName(typeNamespace, typeName, containingNamespace);

            return (useFullName ? typeFullName : typeName);
        }

        /// <summary>
        /// Creates a type declaration with the specified name.
        /// </summary>
        /// <param name="typeName">The name of the type.</param>
        /// <param name="typeNamespace">The type's namespace.</param>
        /// <returns>A type declaration.</returns>
        internal static CodeTypeDeclaration CreateTypeDeclaration(string typeName, string typeNamespace)
        {
            CodeTypeDeclaration decl = new CodeTypeDeclaration(typeName);
            decl.UserData["Namespace"] = typeNamespace;
            return decl;
        }

        /// <summary>
        /// Creates a type declaration for the specified CLR type.
        /// </summary>
        /// <param name="type">The CLR type.</param>
        /// <returns>A type declaration.</returns>
        internal static CodeTypeDeclaration CreateTypeDeclaration(Type type)
        {
            CodeTypeDeclaration decl = CreateTypeDeclaration(type.Name, type.Namespace);

            // Keep track of the CLR type for identification purposes.
            decl.UserData["ClrType"] = type;

            return decl;
        }

        /// <summary>
        /// Checks if a using a <see cref="Type"/>'s short-name will generate an ambiguous type reference error.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> to examine.</param>
        /// <param name="containingNamespace">The namespace in which to check for <see cref="Type"/> short-name conflicts.</param>
        /// <returns>A Boolean indicating whether or not the <see cref="Type"/> will generate an ambiguous type 
        /// reference error.</returns>
        internal static bool RegisterTypeName(Type type, string containingNamespace)
        {
            // Short-circuit in case of global 'use full type names' flag
            if (useFullTypeNames)
            {
                return true;
            }

            // Translate generic types
            if (type.IsGenericType)
            {
                type = type.GetGenericTypeDefinition();
            }

            return CodeGenUtilities.RegisterTypeName(type.Namespace, type.Name, containingNamespace);
        }

        /// <summary>
        /// Creates a <see cref="CodeAttributeDeclaration"/> for a <see cref="DataContractAttribute"/>
        /// </summary>
        /// <param name="sourceType">The type to which the attribute will be applied (to use as a reference)</param>
        /// <param name="codeGenerator">The client proxy generator</param>
        /// <param name="referencingType">The type referencing this declaration</param>
        /// <returns>The new attribute declaration</returns>
        internal static CodeAttributeDeclaration CreateDataContractAttributeDeclaration(Type sourceType, CodeDomClientCodeGenerator codeGenerator, CodeTypeDeclaration referencingType)
        {
            CodeAttributeDeclaration dataContractAttrib = CodeGenUtilities.CreateAttributeDeclaration(typeof(System.Runtime.Serialization.DataContractAttribute), codeGenerator, referencingType);

            string dataContractNamespace = CodeGenUtilities.GetContractNamespace(sourceType);
            string dataContractName = null;

            // If the user specified a DataContract, we should copy the namespace and name. 
            DataContractAttribute sourceDataContractAttrib = (DataContractAttribute)Attribute.GetCustomAttribute(sourceType, typeof(DataContractAttribute));
            if (sourceDataContractAttrib != null)
            {
                if (sourceDataContractAttrib.Namespace != null)
                {
                    dataContractNamespace = sourceDataContractAttrib.Namespace;
                }
                if (sourceDataContractAttrib.Name != null)
                {
                    dataContractName = sourceDataContractAttrib.Name;
                }
            }

            dataContractAttrib.Arguments.Add(new CodeAttributeArgument("Namespace", new CodePrimitiveExpression(dataContractNamespace)));
            if (dataContractName != null)
            {
                dataContractAttrib.Arguments.Add(new CodeAttributeArgument("Name", new CodePrimitiveExpression(dataContractName)));
            }
            return dataContractAttrib;
        }

        /// <summary>
        /// Creates a <see cref="CodeAttributeDeclaration"/> for a <see cref="DisplayAttribute"/>.
        /// </summary>
        /// <param name="codeGenerator">The client proxy generator</param>
        /// <param name="referencingType">The type on whose member <see cref="DisplayAttribute"/> will be applied</param>
        /// <returns>The new attribute declaration</returns>
        internal static CodeAttributeDeclaration CreateDisplayAttributeDeclaration(CodeDomClientCodeGenerator codeGenerator, CodeTypeDeclaration referencingType)
        {
            CodeAttributeDeclaration displayAttributeDeclaration = CodeGenUtilities.CreateAttributeDeclaration(
                typeof(DisplayAttribute), 
                codeGenerator, 
                referencingType);

            displayAttributeDeclaration.Arguments.Add(new CodeAttributeArgument("AutoGenerateField", new CodePrimitiveExpression(false)));

            return displayAttributeDeclaration;
        }

        /// <summary>
        /// Creates an attribute declaration for <see cref="EnumMemberAttribute"/>
        /// </summary>
        /// <param name="memberInfo">The member that may contain an existing <see cref="EnumMemberAttribute"/></param>
        /// <param name="codeGenerator">The proxy generator</param>
        /// <param name="referencingType">The referencing type</param>
        /// <returns>A new attribute declaration</returns>
        internal static CodeAttributeDeclaration CreateEnumMemberAttributeDeclaration(MemberInfo memberInfo, CodeDomClientCodeGenerator codeGenerator, CodeTypeDeclaration referencingType)
        {
            CodeAttributeDeclaration enumMemberDecl = CodeGenUtilities.CreateAttributeDeclaration(typeof(System.Runtime.Serialization.EnumMemberAttribute), codeGenerator, referencingType);

            // If the user specified a DataContract, we should copy the namespace and name. 
            EnumMemberAttribute enumMemberAttrib = (EnumMemberAttribute)Attribute.GetCustomAttribute(memberInfo, typeof(EnumMemberAttribute));
            if (enumMemberAttrib != null)
            {
                string value = enumMemberAttrib.Value;
                if (!string.IsNullOrEmpty(value))
                {
                    enumMemberDecl.Arguments.Add(new CodeAttributeArgument("Value", new CodePrimitiveExpression(value)));
                }
            }
            return enumMemberDecl;
        }


        /// <summary>
        /// Creates a new <see cref="CodeTypeDeclaration"/> that is the generated form of
        /// the given <paramref name="enumType"/>.
        /// </summary>
        /// <param name="enumType">The enum type to generate.</param>
        /// <param name="codeGenerator">The current proxy generator context.</param>
        /// <returns>The newly generated enum type declaration.</returns>
        internal static CodeTypeDeclaration CreateEnumTypeDeclaration(Type enumType, CodeDomClientCodeGenerator codeGenerator)
        {
            System.Diagnostics.Debug.Assert(enumType.IsEnum, "Type must be an enum type");

            CodeTypeDeclaration typeDecl = CodeGenUtilities.CreateTypeDeclaration(enumType);
            typeDecl.IsEnum = true;

            // Always force generated enums to be public
            typeDecl.TypeAttributes |= TypeAttributes.Public;

            // Enums deriving from anything but int get an explicit base type
            Type underlyingType = enumType.GetEnumUnderlyingType();
            if (underlyingType != typeof(int))
            {
                typeDecl.BaseTypes.Add(new CodeTypeReference(underlyingType));
            }

            typeDecl.Comments.Add(new CodeCommentStatement("<summary>", true));
            typeDecl.Comments.Add(new CodeCommentStatement($"Enum {enumType.Name}", true));
            typeDecl.Comments.Add(new CodeCommentStatement("</summary>", true));

            // Generate [DataContract] if it appears in the original only.  Use Reflection only because that matches
            // what WCF will do.
            DataContractAttribute dataContractAttr = (DataContractAttribute)Attribute.GetCustomAttribute(enumType, typeof(DataContractAttribute));
            if (dataContractAttr != null)
            {
                CodeAttributeDeclaration attrDecl = CodeGenUtilities.CreateDataContractAttributeDeclaration(enumType, codeGenerator, typeDecl);
                typeDecl.CustomAttributes.Add(attrDecl);
            }

            string[] memberNames = Enum.GetNames(enumType);
            Type enumValueType = Enum.GetUnderlyingType(enumType);
            for (int i = 0; i < memberNames.Length; ++i)
            {
                string memberName = memberNames[i];
                CodeTypeReference enumTypeRef = CodeGenUtilities.GetTypeReference(enumValueType, codeGenerator, typeDecl);
                CodeMemberField enumMember = new CodeMemberField(enumTypeRef, memberName);

                enumMember.Comments.Add(new CodeCommentStatement("<summary>", true));
                enumMember.Comments.Add(new CodeCommentStatement(memberName, true));
                enumMember.Comments.Add(new CodeCommentStatement("</summary>", true));

                // Generate an initializer for the enum member.
                // GetRawConstantValue is the safest way to get the raw value of the enum field
                // and works for both Reflection and ReflectionOnly loaded assemblies.
                FieldInfo fieldInfo = enumType.GetField(memberName);
                if (fieldInfo != null)
                {
                    object memberValue = fieldInfo.GetRawConstantValue();

                    Debug.Assert(memberValue != null, "Enum type's GetRawConstantValue should never return null");

                    // We special-case MinValue and MaxValue for the integral types
                    // because VisualBasic will generate overflow compiler error for
                    // Int64.MinValue.   If we detect a known MinValue or MaxValue for
                    // this integral type, we generate that reference, otherwise we
                    // just generate a constant integral value of the enum's type
                    object[] minMaxValues = null;
                    CodeGenUtilities.integralMinMaxValues.TryGetValue(underlyingType, out minMaxValues);
                    Debug.Assert(minMaxValues == null || minMaxValues.Length == 3, "integralMinMaxValues elements must always contain 3 values");

                    // Gen xxx.MinValue if it matches, but give precedence to matching a true zero,
                    // which is the min value for the unsigned integral types
                    // minMaxValues[0]: the MinValue for this type
                    // minMaxValues[1]: the MaxValue for this type
                    // minMaxValues[2]: the zero for this type (memberValue is not boxed and cannot be cast)
                    if (minMaxValues != null && !memberValue.Equals(minMaxValues[2]) && memberValue.Equals(minMaxValues[0]))
                    {
                         enumMember.InitExpression = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(underlyingType), "MinValue");
                    }
                    // Gen xxx.MaxValue if it matches
                    else if (minMaxValues != null && memberValue.Equals(minMaxValues[1]))
                    {
                         enumMember.InitExpression = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(underlyingType), "MaxValue");
                    }
                    // All other cases generate an integral constant.
                    // CodeDom knows how to generate the right integral constant based on memberValue's type.
                    else
                    {
                         enumMember.InitExpression = new CodePrimitiveExpression(memberValue);
                    }
                }

                typeDecl.Members.Add(enumMember);

                // Generate an [EnumMember] if appropriate
                EnumMemberAttribute enumMemberAttr = (EnumMemberAttribute)Attribute.GetCustomAttribute(fieldInfo, typeof(EnumMemberAttribute));
                if (enumMemberAttr != null)
                {
                    CodeAttributeDeclaration enumAttrDecl = CodeGenUtilities.CreateEnumMemberAttributeDeclaration(fieldInfo, codeGenerator, typeDecl);
                    enumMember.CustomAttributes.Add(enumAttrDecl);
                }

                // Propagate any other attributes that can be seen by the client
                CustomAttributeGenerator.GenerateCustomAttributes(
                    codeGenerator,
                    typeDecl,
                    ex => string.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_Attribute_ThrewException_CodeTypeMember, ex.Message, fieldInfo.Name, typeDecl.Name, ex.InnerException.Message),
                    fieldInfo.GetCustomAttributes(false).Cast<Attribute>().Where(a => a.GetType() != typeof(EnumMemberAttribute)),
                    enumMember.CustomAttributes,
                    enumMember.Comments);
            }

            // Attributes marked with [Flag] propagate it
            if (enumType.GetCustomAttributes(typeof(FlagsAttribute), false).Length > 0)
            {
                CodeAttributeDeclaration attrDecl = CodeGenUtilities.CreateAttributeDeclaration(typeof(FlagsAttribute), codeGenerator, typeDecl);
                typeDecl.CustomAttributes.Add(attrDecl);
            }
            return typeDecl;
        }

        /// <summary>
        /// Generates a (potentially multi-line) Xml doc comment for a Summary element.
        /// </summary>
        /// <param name="comment">The formatted string to embed within a Summary element.  If it contains line breaks, it will become multiple comments.</param>
        /// <param name="isCSharp">Whether or not the doc comment is for C#.</param>
        /// <returns>The collection of generated Xml doc comments.</returns>
        internal static CodeCommentStatementCollection GenerateSummaryCodeComment(string comment, bool isCSharp)
        {
            System.Diagnostics.Debug.Assert(!string.IsNullOrEmpty(comment), "comment cannot be empty");
            return CodeGenUtilities.GetDocComments("<summary>" + Environment.NewLine + comment + Environment.NewLine + "</summary>", isCSharp);
        }

        /// <summary>
        /// Generates a (potentially multi-line) Xml doc comment for a Param element.
        /// </summary>
        /// <param name="paramName">The name of the parameter.</param>
        /// <param name="comment">The formatted string to embed in a Param element.</param>
        /// <param name="isCSharp">Whether or not the doc comment is for C#.</param>
        /// <returns>The collection of generated Xml doc comments.</returns>
        internal static CodeCommentStatementCollection GenerateParamCodeComment(string paramName, string comment, bool isCSharp)
        {
            System.Diagnostics.Debug.Assert(!string.IsNullOrEmpty(paramName), "paramName cannot be empty");
            System.Diagnostics.Debug.Assert(!string.IsNullOrEmpty(comment), "comment cannot be empty");

            return CodeGenUtilities.GetDocComments("<param name=\"" + paramName + "\">" + comment + "</param>", isCSharp);
        }

        /// <summary>
        /// Generates a (potentially multi-line) Xml doc comment for a Returns element.
        /// </summary>
        /// <param name="comment">The formatted string to embed in the Returns element.</param>
        /// <param name="isCSharp">Whether or not the doc comment is for C#.</param>
        /// <returns>The collection of generated Xml doc comments.</returns>
        internal static CodeCommentStatementCollection GenerateReturnsCodeComment(string comment, bool isCSharp)
        {
            System.Diagnostics.Debug.Assert(!string.IsNullOrEmpty(comment), "comment cannot be empty");
            return CodeGenUtilities.GetDocComments("<returns>" + comment + "</returns>", isCSharp);
        }

        /// <summary>
        /// Takes a multi-line comment defined in a resource file and correctly formats it as a doc comment
        /// for use in code-dom.
        /// </summary>
        /// <param name="resourceComment">The comment to format as a doc comment. This cannot be null.</param>
        /// <param name="isCSharp">Whether or not the doc comment is for C#.</param>
        /// <returns>A collection of comment statements that matches the input resource</returns>
        internal static CodeCommentStatementCollection GetDocComments(string resourceComment, bool isCSharp)
        {
            if (resourceComment == null)
            {
                throw new ArgumentNullException("resourceComment");
            }

            CodeCommentStatementCollection commentCollection = new CodeCommentStatementCollection();
            foreach (string comment in resourceComment.Split(new string[] { Environment.NewLine }, StringSplitOptions.None))
            {
                // VB needs to have a prefixing space before each comment, to ensure the ''' XML comments are properly marked
                // Otherwise a comment that starts with a single quote will be broken (build warnings).
                commentCollection.Add(new CodeCommentStatement((isCSharp ? comment : ' ' + comment), true));
            }
            return commentCollection;
        }

        /// <summary>
        /// Checks if a using a <see cref="Type"/>'s short-name will generate an ambiguous type reference error.
        /// </summary>
        /// <param name="typeNamespace">The type namespace.</param>
        /// <param name="typeName">The type name to check for conflict.</param>
        /// <param name="containingNamespace">The namespace in which to check for <see cref="Type"/> short-name conflicts.</param>
        /// <returns>A Boolean indicating whether or not the <see cref="Type"/> will generate an ambiguous type 
        /// reference error.</returns>
        private static bool RegisterTypeName(string typeNamespace, string typeName, string containingNamespace)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                throw new ArgumentNullException("typeName");
            }

            if (isVisualBasic || (containingNamespace == null))
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

        /// <summary>
        /// In cases where we are emitting types that aren't already in the root namespace, 
        /// we need to translate the generated type namespace so that it resides in the 
        /// root namespace.
        /// </summary>
        /// <remarks>
        /// Here, we're interested in cases where we're generating VB code
        /// and our target project has a non-null root namespace.  
        /// </remarks>
        /// <param name="type">The type who namespace should be translated</param>
        /// <param name="codeGenerator">The current proxy generator</param>
        /// <returns>A translated namespace string</returns>
        internal static string TranslateNamespace(Type type, CodeDomClientCodeGenerator codeGenerator)
        {
            // Set default namespace
            string typeNamespace;

            if (!typeNamespaceTranslations.TryGetValue(type, out typeNamespace))
            {
                // Set the appropriate namespace
                if (NeedToPrefaceRootNamespace(type, codeGenerator))
                {
                    typeNamespace = string.IsNullOrEmpty(type.Namespace) ? 
                        rootNamespace :
                        (rootNamespace + "." + type.Namespace);
                }
                else
                {
                    typeNamespace = type.Namespace;
                }

                // Cache the value for next time
                typeNamespaceTranslations[type] = typeNamespace;
            }

            return typeNamespace;
        }

        /// <summary>
        /// Determines if we need to preface the root namespace to the type
        /// </summary>
        /// <param name="type">The type in question</param>
        /// <param name="codeGenerator">The current proxy generator</param>
        /// <returns><c>true</c> if if we need to preface the root namespace to the type, <c>false</c> otherwise</returns>
        private static bool NeedToPrefaceRootNamespace(Type type, CodeDomClientCodeGenerator codeGenerator)
        {
            // System assemblies never preface with the root namespace
            if (type.Assembly.IsSystemAssembly())
            {
                return false;
            }
            bool isVbProjectWithRootNamespace = !codeGenerator.IsCSharp && !string.IsNullOrEmpty(codeGenerator.ClientProxyCodeGenerationOptions.ClientRootNamespace);

            bool typeIsGeneratedOnTheClient = (type.IsEnum && codeGenerator.NeedToGenerateEnumType(type)) ||
                codeGenerator.DomainServiceDescriptions.Any(dsd => dsd.EntityTypes.Contains(type) || dsd.ComplexTypes.Contains(type) || dsd.DomainServiceType == type);

            bool typeNameStartsWithRootNamespace = 
                string.Equals(type.Namespace, rootNamespace, StringComparison.Ordinal) ||
                (!string.IsNullOrEmpty(type.Namespace) && type.Namespace.StartsWith(rootNamespace + ".", StringComparison.Ordinal));

            return isVbProjectWithRootNamespace && typeIsGeneratedOnTheClient && !typeNameStartsWithRootNamespace;
        }

        /// <summary>
        /// Returns an attribute short name if the <paramref name="type"/> is an 
        /// <see cref="Attribute"/> with a name ending in 'Attribute'.
        /// </summary>
        /// <remarks>
        /// For example, an attribute named "QueryAttribute" can be optimized to 
        /// just "Query".
        /// </remarks>
        /// <param name="type">The attribute type.</param>
        /// <returns>A suitable attribute type name.</returns>
        private static string OptimizeAttributeName(Type type)
        {
            // Attribute?  If so, we special case these and remove the 'Attribute' suffix if present
            // when we're generating short type names.
            if (!useFullTypeNames && typeof(Attribute).IsAssignableFrom(type))
            {
                string typeName = type.Name;
                if (typeName.EndsWith("Attribute", StringComparison.Ordinal))
                {
                    return typeName.Substring(0, typeName.Length - 9 /* 9 = "Attribute".Length */);
                }
            }

            return type.Name;
        }
    }
}
