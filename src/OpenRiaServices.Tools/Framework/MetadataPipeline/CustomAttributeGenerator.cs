using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using OpenRiaServices.Server;

namespace OpenRiaServices.Tools
{
    /// <summary>
    /// Proxy generator for custom attributes
    /// </summary>
    internal static class CustomAttributeGenerator
    {
        /// <summary>
        /// Block list for framework attributes we want to block from flowing.
        /// </summary>
        private static readonly Type[] blockList = new Type[]
            {
                typeof(MetadataTypeAttribute),
                typeof(ScaffoldColumnAttribute),
#if HAS_LINQ2SQL
                typeof(ScaffoldTableAttribute),
#endif
                typeof(SerializableAttribute),
                typeof(System.Diagnostics.CodeAnalysis.SuppressMessageAttribute),
                typeof(System.Diagnostics.DebuggerStepThroughAttribute),
                typeof(System.Runtime.CompilerServices.AsyncStateMachineAttribute),
            };

        /// <summary>
        /// Known attribute builder types.
        /// </summary>
        private static Dictionary<Type, Type> _knownBuilderTypes;

        /// <summary>
        /// Known attribute builder instances.
        /// </summary>
        private static Dictionary<Type, ICustomAttributeBuilder> _knownBuilders = new Dictionary<Type, ICustomAttributeBuilder>()
        {
             // OpenRiaServices Attributes which should not be copied over
            { typeof(EnableClientAccessAttribute), null },
            { typeof(IncludeAttribute), null },
            { typeof(ExcludeAttribute), null },
            { typeof(QueryAttribute), null },
            { typeof(InvokeAttribute), null },
            { typeof(InsertAttribute), null },
            { typeof(UpdateAttribute), null },
            { typeof(DeleteAttribute), null },
            { typeof(EntityActionAttribute), null },
            { typeof(RequiresAuthenticationAttribute), null },
            { typeof(RequiresRoleAttribute), null },
        };
        /// <summary>
        /// Gets the dictionary mapping custom attribute types to their known custom attribute builder types
        /// </summary>
        private static Dictionary<Type, Type> KnownBuilderTypes
        {
            get
            {
                if (_knownBuilderTypes == null)
                {
                    // TODO: (ron) this deserves an extensibility mechanism.  For now, hard coded allow list
                    _knownBuilderTypes = new Dictionary<Type, Type>();
                    _knownBuilderTypes[typeof(CustomValidationAttribute)] = typeof(CustomValidationCustomAttributeBuilder);
                    _knownBuilderTypes[typeof(DataMemberAttribute)] = typeof(DataMemberAttributeBuilder);
                    _knownBuilderTypes[typeof(DisplayAttribute)] = typeof(DisplayCustomAttributeBuilder);
                    _knownBuilderTypes[typeof(DomainIdentifierAttribute)] = typeof(DomainIdentifierAttributeBuilder);
                    _knownBuilderTypes[typeof(EditableAttribute)] = typeof(EditableAttributeBuilder);
                    _knownBuilderTypes[typeof(RangeAttribute)] = typeof(RangeCustomAttributeBuilder);
                    _knownBuilderTypes[typeof(RegularExpressionAttribute)] = typeof(ValidationCustomAttributeBuilder);
                    _knownBuilderTypes[typeof(RequiredAttribute)] = typeof(ValidationCustomAttributeBuilder);
                    _knownBuilderTypes[typeof(StringLengthAttribute)] = typeof(ValidationCustomAttributeBuilder);
                    _knownBuilderTypes[typeof(UIHintAttribute)] = typeof(UIHintCustomAttributeBuilder);
                }
                return _knownBuilderTypes;
            }
        }

        /// <summary>
        /// Gets the dictionary mapping custom attribute types to their builder instances
        /// </summary>
        private static Dictionary<Type, ICustomAttributeBuilder> KnownBuilders
        {
            get
            {
                return _knownBuilders;
            }
        }

        /// <summary>
        /// Generates code for the given custom attributes and adds them to the given <see cref="CodeAttributeDeclarationCollection"/>
        /// </summary>
        /// <param name="proxyGenerator">Root client proxy generator</param>
        /// <param name="referencingType">The referencing type</param>
        /// <param name="getLogWarningMessage">The function to call to get the warning message to be logged</param>
        /// <param name="attributes">Collection of attributes to generate</param>
        /// <param name="outputCollection">The collection to which the generated attributes will be added</param>
        /// <param name="comments">Collection of comments that should be updated if errors are discovered.</param>
        public static void GenerateCustomAttributes(CodeDomClientCodeGenerator proxyGenerator, CodeTypeDeclaration referencingType, Func<AttributeBuilderException, string> getLogWarningMessage, IEnumerable<Attribute> attributes, CodeAttributeDeclarationCollection outputCollection, CodeCommentStatementCollection comments)
        {
            GenerateCustomAttributes(
                proxyGenerator,
                referencingType,
                getLogWarningMessage,
                attributes,
                outputCollection,
                comments,
                Resource.ClientCodeGen_Attribute_FailedToGenerate);
        }

        /// <summary>
        /// Generates code for the given custom attributes and adds them to the given <see cref="CodeAttributeDeclarationCollection"/>
        /// </summary>
        /// <param name="proxyGenerator">Root client proxy generator</param>
        /// <param name="referencingType">The referencing type</param>
        /// <param name="getLogWarningMessage">The function to call to get the warning message to be logged</param>
        /// <param name="attributes">Collection of attributes to generate</param>
        /// <param name="outputCollection">The collection to which the generated attributes will be added</param>
        /// <param name="comments">Collection of comments that should be updated if errors are discovered.</param>
        /// <param name="customCommentHeader">A custom comment header that will be displayed for any generated comment errors.</param>
        public static void GenerateCustomAttributes(CodeDomClientCodeGenerator proxyGenerator, CodeTypeDeclaration referencingType, Func<AttributeBuilderException, string> getLogWarningMessage, IEnumerable<Attribute> attributes, CodeAttributeDeclarationCollection outputCollection, CodeCommentStatementCollection comments, string customCommentHeader)
        {
            GenerateCustomAttributes(
                proxyGenerator,
                referencingType,
                getLogWarningMessage,
                attributes,
                outputCollection,
                comments,
                customCommentHeader,
                false);
        }

        /// <summary>
        /// Generates code for the given custom attributes and adds them to the given <see cref="CodeAttributeDeclarationCollection"/>
        /// </summary>
        /// <param name="proxyGenerator">Root client proxy generator</param>
        /// <param name="referencingType">The referencing type</param>
        /// <param name="attributes">Collection of attributes to generate</param>
        /// <param name="outputCollection">The collection to which the generated attributes will be added</param>
        /// <param name="comments">Collection of comments that should be updated if errors are discovered.</param>
        /// <param name="forcePropagation">Indicates whether or not to force attribute propagation.</param>
        public static void GenerateCustomAttributes(CodeDomClientCodeGenerator proxyGenerator, CodeTypeDeclaration referencingType, IEnumerable<Attribute> attributes, CodeAttributeDeclarationCollection outputCollection, CodeCommentStatementCollection comments, bool forcePropagation)
        {
            GenerateCustomAttributes(
                proxyGenerator,
                referencingType,
                null,
                attributes,
                outputCollection,
                comments,
                Resource.ClientCodeGen_Attribute_FailedToGenerate,
                forcePropagation);
        }

        /// <summary>
        /// Generates code for the given custom attributes and adds them to the given <see cref="CodeAttributeDeclarationCollection"/>
        /// </summary>
        /// <param name="proxyGenerator">Root client proxy generator</param>
        /// <param name="referencingType">The referencing type</param>
        /// <param name="getLogWarningMessage">The function to call to get the warning message to be logged</param>
        /// <param name="attributes">Collection of attributes to generate</param>
        /// <param name="outputCollection">The collection to which the generated attributes will be added</param>
        /// <param name="comments">Collection of comments that should be updated if errors are discovered.</param>
        /// <param name="customCommentHeader">A custom comment header that will be displayed for any generated comment errors.</param>
        /// <param name="forcePropagation">Indicates whether or not to force attribute propagation.</param>
        public static void GenerateCustomAttributes(CodeDomClientCodeGenerator proxyGenerator, CodeTypeDeclaration referencingType, Func<AttributeBuilderException, string> getLogWarningMessage, IEnumerable<Attribute> attributes, CodeAttributeDeclarationCollection outputCollection, CodeCommentStatementCollection comments, string customCommentHeader, bool forcePropagation)
        {
            IEnumerable<CodeAttributeDeclaration> cads = GenerateCustomAttributes(proxyGenerator, referencingType, getLogWarningMessage, attributes, comments, customCommentHeader, forcePropagation);
            foreach (var cad in cads)
            {
                outputCollection.Add(cad);
            }
        }

        /// <summary>
        /// Generates code for the given set of custom attributes
        /// </summary>
        /// <param name="proxyGenerator">Root client proxy generator</param>
        /// <param name="referencingType">The referencing type</param>
        /// <param name="getLogWarningMessage">The function to call to get the warning message to be logged</param>
        /// <param name="attributes">Collection of attributes for which to generate code</param>
        /// <param name="comments">Collection of comments that should be updated if errors are discovered.</param>
        /// <param name="customCommentHeader">A custom comment header that will be displayed for any generated comment errors.</param>
        /// <param name="forcePropagation">Indicates whether or not to force attribute propagation.</param>
        /// <returns>The collection of generated attribute declarations corresponding to <paramref name="attributes"/></returns>
        private static IEnumerable<CodeAttributeDeclaration> GenerateCustomAttributes(CodeDomClientCodeGenerator proxyGenerator, CodeTypeDeclaration referencingType, Func<AttributeBuilderException, string> getLogWarningMessage, IEnumerable<Attribute> attributes, CodeCommentStatementCollection comments, string customCommentHeader, bool forcePropagation)
        {
            bool emittedErrorCommentHeader = false;
            List<CodeAttributeDeclaration> result = new List<CodeAttributeDeclaration>(attributes.Count());

            // Enumerate over attributes sorted by name.  Here, we sort by name to ensure that our
            // generated baselines (including possible error comments!) are ordered consistently.
            foreach (Attribute attribute in attributes.OrderBy(a => a.GetType().Name))
            {
                Type attributeType = attribute.GetType();

                // Check if this attribute should be blocked
                if (IsAttributeBlocked(attributeType))
                {
                    continue;
                }

                bool attributePropagated = false;
                bool isDataAnnotationsAttribute = string.Equals(attributeType.Namespace, typeof(ValidationAttribute).Namespace, StringComparison.Ordinal);

                ICustomAttributeBuilder cab = GetCustomAttributeBuilder(attributeType);

                if (cab != null)
                {
                    AttributeDeclaration attributeDeclaration = null;
                    // If the attempt to build the attribute fails, log a clean error.
                    // One common exception path is InvalidOperationException arising from
                    // attributes that have been improperly constructed (see DisplayAttribute)
                    try
                    {
                        attributeDeclaration = cab.GetAttributeDeclaration(attribute);
                    }
                    catch (AttributeBuilderException attributeBuilderException)
                    {
                        // Ensure we've generated the attribute generation failure error header
                        GenerateCustomAttributesErrorCommentHeader(comments, customCommentHeader, ref emittedErrorCommentHeader);

                        // Generate comments stating the attribute couldn't be generated
                        comments.AddRange(ConstructCodeAttributeFailureComments(attributeBuilderException.Message));

                        // Log the build warning if a method was specified to get the warning message
                        if (getLogWarningMessage != null)
                        {
                            string warningMessage = getLogWarningMessage(attributeBuilderException);
                            proxyGenerator.LogWarning(warningMessage);
                        }

                        // Move on to the next attribute
                        continue;
                    }

                    // Null is acceptable indicator that code-gen was not possible.
                    if (attributeDeclaration != null)
                    {
                        if (!forcePropagation)
                        {
                            // Verify attribute's shared type|property|method requirements are met
                            ValidateAttributeDeclarationRequirements(proxyGenerator, attributeDeclaration);
                        }

                        if (attributeDeclaration.HasErrors)
                        {
                            // Only generate comments if the attribute is a DataAnnotations attribute
                            if (isDataAnnotationsAttribute)
                            {
                                // Ensure we've generated the attribute generation failure error header
                                GenerateCustomAttributesErrorCommentHeader(comments, customCommentHeader, ref emittedErrorCommentHeader);

                                // Generate attribute and an error message as comments
                                comments.AddRange(ConstructCodeAttributeFailureComments(proxyGenerator, attributeDeclaration));
                            }
                        }
                        else
                        {
                            // Generate the attribute declaration
                            CodeAttributeDeclaration codeAttributeDeclaration = CreateCodeAttributeDeclaration(proxyGenerator, referencingType, attributeDeclaration);
                            result.Add(codeAttributeDeclaration);
                            attributePropagated = true;
                        }
                    }
                }

                // We generate VS warnings in certain scenarios:
                //  - A DataAnnotation attribute type was not available on the client, user needs to add a reference.
                //  - An attribute subclassed ValidationAttribute (custom or framework) and we couldn't build it.
                if (!attributePropagated)
                {
                    // Was it a DA attribute that wasn't available?  If so, log a warning.
                    if (isDataAnnotationsAttribute)
                    {
                        CodeMemberShareKind shareKind = proxyGenerator.GetTypeShareKind(attributeType);
                        if (shareKind == CodeMemberShareKind.NotShared)
                        {
                            // Indicate that a reference to 'System.ComponentModel.DataAnnotations' is required.
                            proxyGenerator.LogWarning(
                                string.Format(
                                    CultureInfo.CurrentCulture,
                                    Resource.ClientCodeGen_Attribute_RequiresDataAnnotations,
                                    attributeType,
                                    proxyGenerator.ClientProjectName));
                        }
                    }
                    // Was it a validation attribute that we couldn't build?  If so, log a warning.
                    else if (cab == null && typeof(ValidationAttribute).IsAssignableFrom(attributeType))
                    {
                        // Indicate that a builder was not found, attribute does not meet heuristics.
                        proxyGenerator.LogWarning(
                            string.Format(
                                CultureInfo.CurrentCulture,
                                Resource.ClientCodeGen_Attribute_RequiresBuilder,
                                attributeType));
                    }
                }
            }

            // Issue -- CodeDom outputs the attributes in the order they are generated.
            // To allow consistent output for easy baseline comparisons, sort the list.
            result.Sort(new Comparison<CodeAttributeDeclaration>((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal)));
            return result;
        }

        /// <summary>
        /// Ensures that the specified custom comment header has been generated in the comments.
        /// </summary>
        /// <param name="comments">The collection of comments being built</param>
        /// <param name="customCommentHeader">The custom comment header to ensure has been emitted</param>
        /// <param name="emittedErrorCommentHeader">The boolean to track whether or not the custom comment header has been emitted</param>
        private static void GenerateCustomAttributesErrorCommentHeader(CodeCommentStatementCollection comments, string customCommentHeader, ref bool emittedErrorCommentHeader)
        {
            if (!emittedErrorCommentHeader)
            {
                // Emit the friendly header text only once
                comments.Add(new CodeCommentStatement(customCommentHeader));
                comments.Add(new CodeCommentStatement(string.Empty /* blank comment */));
                emittedErrorCommentHeader = true;
            }
        }

        /// <summary>
        /// Retrieves the appropriate custom attribute builder for a given attribute instance
        /// </summary>
        /// <param name="attributeType">The attribute type.  It cannot be null.</param>
        /// <returns>The custom attribute builder for it.</returns>
        private static ICustomAttributeBuilder GetCustomAttributeBuilder(Type attributeType)
        {
            if (attributeType == null)
            {
                throw new ArgumentNullException(nameof(attributeType));
            }

            ICustomAttributeBuilder cab = null;

            // We maintain a cache of known builder instances, created lazily
            if (KnownBuilders.TryGetValue(attributeType, out cab))
            {
                return cab;
            }

            // Don't have a builder instance yet
            // See if we have a registered builder type for this attribute
            Type cabType = null;
            if (!KnownBuilderTypes.TryGetValue(attributeType, out cabType))
            {
                // Don't have an explicit builder -- see if we this attribute derives from
                // a known builder type and assume it is okay to use it.
                foreach (KeyValuePair<Type, Type> pair in KnownBuilderTypes)
                {
                    if (pair.Key.IsAssignableFrom(attributeType))
                    {
                        cabType = pair.Value;
                        break;
                    }
                }
            }

            // If don't have a builder -- see if the attribute is visible to the
            // client.  If so, we will attempt to build with our standard builder
            if (cabType == null)
            {
                cabType = typeof(StandardCustomAttributeBuilder);
            }

            // If we found a builder type, instantiate it now.  We'll reuse it
            cab = Activator.CreateInstance(cabType) as ICustomAttributeBuilder;

            // Don't cache null builders, because we may be re-using this cache for a next code-gen 
            // run where we may have a different set of shared types.
            // TODO: We need to get rid of our static caches, because caches look different between 
            //       builds of different DomainServices. E.g. for one DomainService an attribute may 
            //       be shared, while in another it's not, and thus the KnownBuilders mapping is
            //       different between the two.
            //       Instead of static caches, we should consider storing state per build in a context
            //       object. The context object would not be shared against different builds of different 
            //       DomainServices.
            if (cab != null)
            {
                KnownBuilders[attributeType] = cab;
            }

            return cab;
        }

        /// <summary>
        /// Generate comments indicating attribute propagation failure.
        /// </summary>
        /// <param name="proxyGenerator">The context for generating code.  It cannot be <c>null</c>.</param>
        /// <param name="attributeDeclaration">The attribute declaration to generate as a comment.</param>
        /// <returns>A collection of comments.</returns>
        private static CodeCommentStatementCollection ConstructCodeAttributeFailureComments(CodeDomClientCodeGenerator proxyGenerator, AttributeDeclaration attributeDeclaration)
        {
            // We are generating failure comments in the following example form:
            //
            //    // Unable to generate the following attribute(s) due to the following error(s):
            //    // - The attribute 'System.ComponentModel.DataAnnotations.CustomValidationAttribute' references type 'TestDomainServices.ServerOnlyValidator'.  This is not accessible in the client project.
            //    // [CustomValidationAttribute(typeof(TestDomainServices.ServerOnlyValidator), "IsObjectValid")]
            //    //
            CodeCommentStatementCollection comments = new CodeCommentStatementCollection();
            foreach (string error in attributeDeclaration.Errors)
            {
                comments.Add(new CodeCommentStatement(string.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_Attribute_FailedToGenerate_ErrorTemplate, error)));
            }
            comments.Add(new CodeCommentStatement(GenerateCodeAttribute(proxyGenerator, attributeDeclaration)));
            comments.Add(new CodeCommentStatement(string.Empty /* blank comment */));
            return comments;
        }

        /// <summary>
        /// Generate comments indicating attribute propagation failure.
        /// </summary>
        /// <param name="errorMessage">The error message to generate an attribute failure comment for.</param>
        /// <returns>A collection of comments.</returns>
        private static CodeCommentStatementCollection ConstructCodeAttributeFailureComments(string errorMessage)
        {
            // We are generating failure comments in the following example form:
            //
            //    // Unable to generate the following attribute(s) due to the following error(s):
            //    // - The attribute 'MyApplication.MyCustomAttribute' threw an exception from the 'ThrowingProperty' property.
            //    //
            CodeCommentStatementCollection comments = new CodeCommentStatementCollection();

            comments.Add(new CodeCommentStatement(string.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_Attribute_FailedToGenerate_ErrorTemplate, errorMessage)));
            comments.Add(new CodeCommentStatement(string.Empty /* blank comment */));
            return comments;
        }

        /// <summary>
        /// Verifies that a <see cref="AttributeDeclaration"/>'s shared type requirements are met.
        /// </summary>
        /// <param name="proxyGenerator">The context for code generation</param>
        /// <param name="attributeDeclaration">The <see cref="AttributeDeclaration"/> to verify.</param>
        private static void ValidateAttributeDeclarationRequirements(CodeDomClientCodeGenerator proxyGenerator, AttributeDeclaration attributeDeclaration)
        {
            // Verify the attribute itself is shared.
            CodeMemberShareKind shareKind = proxyGenerator.GetTypeShareKind(attributeDeclaration.AttributeType);

            // If there is no PDB or this type has no human-authored code, we cannot determine
            // whether it is shared and get a null value.  This requires a special message to
            // explain why we treat the type as not shared.
            if (shareKind == CodeMemberShareKind.Unknown)
            {
                attributeDeclaration.Errors.Add(
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resource.ClientCodeGen_Attribute_RequiresShared_NoPDB,
                    attributeDeclaration.AttributeType,
                    attributeDeclaration.AttributeType.Assembly.GetName().Name,
                    proxyGenerator.ClientProjectName));
            }
            else if (shareKind == CodeMemberShareKind.NotShared)
            {
                attributeDeclaration.Errors.Add(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resource.ClientCodeGen_Attribute_RequiresShared,
                        attributeDeclaration.AttributeType,
                        proxyGenerator.ClientProjectName));
            }

            // Verify shared types.  Here, we order by type name so that any generated errors
            // are presented in a consistent order.
            foreach (var type in attributeDeclaration.RequiredTypes.OrderBy(t => t.FullName))
            {
                shareKind = proxyGenerator.GetTypeShareKind(type);

                // Missing PDB or lack of user code means we cannot know -- issue special warning
                if (shareKind == CodeMemberShareKind.Unknown)
                {
                    attributeDeclaration.Errors.Add(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resource.ClientCodeGen_Attribute_RequiresShared_Type_NoPDB,
                        attributeDeclaration.AttributeType,
                        type,
                        type.Assembly.GetName().Name,
                        proxyGenerator.ClientProjectName));
                }
                else if (shareKind == CodeMemberShareKind.NotShared)
                {
                    attributeDeclaration.Errors.Add(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            Resource.ClientCodeGen_Attribute_RequiresShared_Type,
                            attributeDeclaration.AttributeType,
                            type,
                            proxyGenerator.ClientProjectName));
                }
            }

            // Verify shared methods.  Here, we order by method name so that any generated errors
            // are presented in a consistent order.
            foreach (var method in attributeDeclaration.RequiredMethods.OrderBy(p => p.Name))
            {
                shareKind = proxyGenerator.GetMethodShareKind(method);
                if (shareKind == CodeMemberShareKind.NotShared)
                {
                    attributeDeclaration.Errors.Add(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            Resource.ClientCodeGen_Attribute_RequiresShared_Method,
                            attributeDeclaration.AttributeType,
                            method.Name,
                            method.DeclaringType,
                            proxyGenerator.ClientProjectName));
                }
            }

            // Verify shared properties.  Here, we order by property name so that any generated errors
            // are presented in a consistent order.
            foreach (var property in attributeDeclaration.RequiredProperties.OrderBy(p => p.Name))
            {
                shareKind = proxyGenerator.GetPropertyShareKind(property.DeclaringType, property.Name);
                if (shareKind == CodeMemberShareKind.NotShared)
                {
                    attributeDeclaration.Errors.Add(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            Resource.ClientCodeGen_Attribute_RequiresShared_Property,
                            attributeDeclaration.AttributeType,
                            property.Name,
                            property.DeclaringType,
                            proxyGenerator.ClientProjectName));
                }
            }
        }

        /// <summary>
        /// Generates an attribute declaration string.  This is used in scenarios where an attribute declaration is needed
        /// in the form of a comment.  CodeDOM does not support generation of standalone attributes.
        /// </summary>
        /// <param name="proxyGenerator">The context for generating code.  It cannot be null.</param>
        /// <param name="attributeDeclaration">The <see cref="AttributeDeclaration"/> to represent.</param>
        /// <returns>An attribute declaration.</returns>
        private static string GenerateCodeAttribute(CodeDomClientCodeGenerator proxyGenerator, AttributeDeclaration attributeDeclaration)
        {
            StringBuilder result = new StringBuilder();
            bool isCSharp = proxyGenerator.IsCSharp;

            result.Append(isCSharp ? '[' : '<');
            result.Append(attributeDeclaration.AttributeType.Name);
            result.Append('(');

            // Add ctor args
            if (attributeDeclaration.ConstructorArguments.Count > 0)
            {
                foreach (object value in attributeDeclaration.ConstructorArguments)
                {
                    result.Append(ConvertValueToCode(value, isCSharp));
                    result.Append(", ");
                }
            }

            // Add named params
            if (attributeDeclaration.NamedParameters.Count > 0)
            {
                foreach (KeyValuePair<string, object> pair in attributeDeclaration.NamedParameters)
                {
                    result.Append(pair.Key);
                    result.Append(isCSharp ? " = " : " := ");
                    result.Append(ConvertValueToCode(pair.Value, isCSharp));
                    result.Append(", ");
                }
            }

            if (attributeDeclaration.ConstructorArguments.Count > 0 || attributeDeclaration.NamedParameters.Count > 0)
            {
                result.Remove(result.Length - 2, 2);
            }

            result.Append(')');
            result.Append(isCSharp ? "]" : "> _");

            return result.ToString();
        }

        /// <summary>
        /// Converts a value to its source code representation.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="isCSharp">A booleaning indicating whether or not the target language is C#.</param>
        /// <returns>A string representing the <paramref name="value"/>.</returns>
        private static string ConvertValueToCode(object value, bool isCSharp)
        {
            if (value == null)
            {
                return isCSharp ? "null" : "Nothing";
            }

            if (value is string)
            {
                return "\"" + value.ToString() + "\"";
            }

            if (value is char)
            {
                return "'" + value.ToString() + "'";
            }

            if (value is Type)
            {
                string template =
                    isCSharp ?
                        "typeof({0})" :
                        "GetType({0})";

                return string.Format(CultureInfo.CurrentCulture, template, value);
            }

            return value.ToString();
        }

        /// <summary>
        /// Creates a <see cref="CodeAttributeDeclaration"/> for the given <see cref="AttributeDeclaration"/>.
        /// </summary>
        /// <param name="proxyGenerator">The context for generating code.  It cannot be null.</param>
        /// <param name="referencingType">The referencing type.</param>
        /// <param name="attributeDeclaration">The <see cref="AttributeDeclaration"/> to build.</param>
        /// <returns>A <see cref="CodeAttributeDeclaration"/>.</returns>
        private static CodeAttributeDeclaration CreateCodeAttributeDeclaration(CodeDomClientCodeGenerator proxyGenerator, CodeTypeDeclaration referencingType, AttributeDeclaration attributeDeclaration)
        {
            CodeAttributeDeclaration codeAttributeDeclaration = CodeGenUtilities.CreateAttributeDeclaration(attributeDeclaration.AttributeType, proxyGenerator, referencingType);

            // Add ctor args
            foreach (object arg in attributeDeclaration.ConstructorArguments)
            {
                CodeExpression expression = CreateCodeExpression(proxyGenerator, referencingType, arg);
                codeAttributeDeclaration.Arguments.Add(new CodeAttributeArgument(expression));
            }

            // Add named params
            foreach (KeyValuePair<string, object> pair in attributeDeclaration.NamedParameters)
            {
                CodeExpression expression = CreateCodeExpression(proxyGenerator, referencingType, pair.Value);
                codeAttributeDeclaration.Arguments.Add(new CodeAttributeArgument(pair.Key, expression));
            }

            return codeAttributeDeclaration;
        }

        /// <summary>
        /// Returns a value expressing whether or not the attribute type is blocked from generation.
        /// </summary>
        /// <param name="attributeType">The attribute type to check.</param>
        /// <returns>True if the attribute should be blocked.</returns>
        private static bool IsAttributeBlocked(Type attributeType)
        {
            return blockList.Contains(attributeType)
                // __DynamicallyInvokableAttribute might be added at compile time, don't propagate them
                || attributeType.FullName == "__DynamicallyInvokableAttribute"
                ;
        }

        /// <summary>
        /// Creates the CodeDom CodeExpression for the given value.  Returns null if unable to generate a CodeExpression.
        /// </summary>
        /// <remarks>This method exists solely to help generate code for all object types that can appear in an
        /// attribute declaration, such as typeof()</remarks>
        /// <param name="proxyGenerator">The context for generating code.  It cannot be null.</param>
        /// <param name="referencingType">The referencing type</param>
        /// <param name="value">The value.  Null is permitted.</param>
        /// <returns>The code expression</returns>
        private static CodeExpression CreateCodeExpression(CodeDomClientCodeGenerator proxyGenerator, CodeTypeDeclaration referencingType, object value)
        {
            Type typeOfValue = value == null ? null : value.GetType();
            if (value == null || typeOfValue.IsPrimitive || value is string)
            {
                CodeExpression e = new CodePrimitiveExpression(value);

                // Workaround CodeDom issue -- it looks like CodePrimitiveExpression is fooled and generates double
                // literals as integers when there is no fraction.  We take a general strategy of forcing an explicit
                // compile time cast to ensure we recompile the same type.
                if (value != null && (value is double || value is float))
                {
                    e = new CodeCastExpression(value.GetType(), e);
                }
                return e;
            }

            // typeof(T) requires special handling
            Type valueAsType = value as Type;
            if (valueAsType != null)
            {
                // Verify the type is shared
                // Don't know counts as not shared
                CodeMemberShareKind shareKind = proxyGenerator.GetTypeShareKind(valueAsType);
                if ((shareKind & CodeMemberShareKind.Shared) == 0)
                {
                    // Here we return a fully-qualified type name to ensure we don't cause compilation
                    // errors by adding invalid 'using' statements into our codedom graph.
                    CodeTypeReference valueTypeReference = CodeGenUtilities.GetTypeReference(valueAsType, proxyGenerator, referencingType, false, /*Use fully qualified name*/ true);
                    valueTypeReference.Options = CodeTypeReferenceOptions.GlobalReference;
                    return new CodeTypeOfExpression(valueTypeReference);
                }

                return new CodeTypeOfExpression(CodeGenUtilities.GetTypeReference(valueAsType, proxyGenerator, referencingType));
            }

            // Enum values need special handling
            if (typeOfValue.IsEnum)
            {
                string enumValueName = Enum.GetName(typeOfValue, value);
                string enumTypeName;
                if (proxyGenerator.ClientProxyCodeGenerationOptions.UseFullTypeNames)
                {
                    enumTypeName = typeOfValue.FullName;
                }
                else
                {
                    enumTypeName = typeOfValue.Name;
                }
                return new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(enumTypeName), enumValueName);
            }

            return null;
        }
    }
}

