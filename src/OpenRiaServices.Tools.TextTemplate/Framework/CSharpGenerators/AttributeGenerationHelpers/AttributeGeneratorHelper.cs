using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace OpenRiaServices.Tools.TextTemplate
{
    internal class AttributeGeneratorHelper
    {
        private const string DefaultDataContractSchema = "http://schemas.datacontract.org/2004/07/";
        private static readonly Type[] blockList = new Type[]
            {
                typeof(MetadataTypeAttribute),
                typeof(ScaffoldColumnAttribute),
                typeof(ScaffoldTableAttribute),
                typeof(SerializableAttribute),
                typeof(System.Diagnostics.CodeAnalysis.SuppressMessageAttribute)
            };

        private static Dictionary<Type, Type> _knownBuilderTypes;

        private static Dictionary<Type, ICustomAttributeBuilder> _knownBuilders = new Dictionary<Type, ICustomAttributeBuilder>();

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

        public static AttributeDeclaration GetAttributeDeclaration(Attribute attribute, ClientCodeGenerator textTemplateClientCodeGenerator, bool forcePropagation)
        {
            Type attributeType = attribute.GetType();

            // Check if this attribute should be blocked
            if (IsAttributeBlocked(attributeType))
            {
                return null;
            }

            ICustomAttributeBuilder cab = GetCustomAttributeBuilder(attributeType);
            AttributeDeclaration attributeDeclaration = null;
            if (cab != null)
            {
                try
                {
                    attributeDeclaration = cab.GetAttributeDeclaration(attribute);
                }
                catch (AttributeBuilderException)
                {
                    return null;
                }
                if (attributeDeclaration != null)
                {
                    if (!forcePropagation)
                    {
                        // Verify attribute's shared type|property|method requirements are met
                        ValidateAttributeDeclarationRequirements(attributeDeclaration, textTemplateClientCodeGenerator);
                    }
                }
            }

            return attributeDeclaration;
        }

        private static void ValidateAttributeDeclarationRequirements(AttributeDeclaration attributeDeclaration, ClientCodeGenerator textTemplateClientCodeGenerator)
        {
            // Verify the attribute itself is shared.
            CodeMemberShareKind shareKind = textTemplateClientCodeGenerator.GetTypeShareKind(attributeDeclaration.AttributeType);

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
                    textTemplateClientCodeGenerator.ClientProjectName));
            }
            else if (shareKind == CodeMemberShareKind.NotShared)
            {
                attributeDeclaration.Errors.Add(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resource.ClientCodeGen_Attribute_RequiresShared,
                        attributeDeclaration.AttributeType,
                        textTemplateClientCodeGenerator.ClientProjectName));
            }

            // Verify shared types.  Here, we order by type name so that any generated errors
            // are presented in a consistent order.
            foreach (var type in attributeDeclaration.RequiredTypes.OrderBy(t => t.FullName))
            {
                shareKind = textTemplateClientCodeGenerator.GetTypeShareKind(type);

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
                        textTemplateClientCodeGenerator.ClientProjectName));
                }
                else if (shareKind == CodeMemberShareKind.NotShared)
                {
                    attributeDeclaration.Errors.Add(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            Resource.ClientCodeGen_Attribute_RequiresShared_Type,
                            attributeDeclaration.AttributeType,
                            type,
                            textTemplateClientCodeGenerator.ClientProjectName));
                }
            }

            // Verify shared methods.  Here, we order by method name so that any generated errors
            // are presented in a consistent order.
            foreach (var method in attributeDeclaration.RequiredMethods.OrderBy(p => p.Name))
            {
                shareKind = textTemplateClientCodeGenerator.GetMethodShareKind(method);
                if (shareKind == CodeMemberShareKind.NotShared)
                {
                    attributeDeclaration.Errors.Add(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            Resource.ClientCodeGen_Attribute_RequiresShared_Method,
                            attributeDeclaration.AttributeType,
                            method.Name,
                            method.DeclaringType,
                            textTemplateClientCodeGenerator.ClientProjectName));
                }
            }

            // Verify shared properties.  Here, we order by property name so that any generated errors
            // are presented in a consistent order.
            foreach (var property in attributeDeclaration.RequiredProperties.OrderBy(p => p.Name))
            {
                shareKind = textTemplateClientCodeGenerator.GetPropertyShareKind(property.DeclaringType, property.Name);
                if (shareKind == CodeMemberShareKind.NotShared)
                {
                    attributeDeclaration.Errors.Add(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            Resource.ClientCodeGen_Attribute_RequiresShared_Property,
                            attributeDeclaration.AttributeType,
                            property.Name,
                            property.DeclaringType,
                            textTemplateClientCodeGenerator.ClientProjectName));
                }
            }
        }

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
            if (cabType != null)
            {
                cab = Activator.CreateInstance(cabType) as ICustomAttributeBuilder;
            }

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

        private static bool IsAttributeBlocked(Type attributeType)
        {
            return blockList.Contains(attributeType)
                // __DynamicallyInvokableAttribute might be added at compile time, don't propagate them
                || attributeType.FullName == "__DynamicallyInvokableAttribute"
                ;
        }

        internal static string ConvertValueToCode(object value, bool isCSharp)
        {
            if (value == null)
            {
                return isCSharp ? "null" : "Nothing";
            }

            if (value is string)
            {
                return "@\"" + value.ToString() + "\"";
            }

            if (value is char)
            {
                return "'" + value.ToString() + "'";
            }

            Type typeValue = value as Type;
            if (typeValue != null)
            {
                string template =
                    isCSharp ?
                        "typeof({0})" :
                        "GetType({0})";

                return string.Format(CultureInfo.CurrentCulture, template, CodeGenUtilities.GetTypeName(typeValue));
            }

            if (value is bool)
            {
                return CodeGenUtilities.GetBooleanString((bool)value, true);
            }

            if (value.GetType().IsEnum)
            {
                return CodeGenUtilities.GetTypeName(value.GetType()) + "." + value.ToString();
            }

            return value.ToString();
        }

        internal static void GetContractNameAndNamespace(Type sourceType, out string dataContractNamespace, out string dataContractName)
        {
            dataContractNamespace = AttributeGeneratorHelper.GetContractNamespace(sourceType);
            dataContractName = null;

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
        }

        private static string GetContractNamespace(Type sourceType)
        {
            Dictionary<string, string> contractNamespaces = new Dictionary<string, string>();
            ContractNamespaceAttribute[] contractNamespaceAttribs = (ContractNamespaceAttribute[])sourceType.Assembly.GetCustomAttributes(typeof(ContractNamespaceAttribute), /* inherit */ true);
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

            // See if there's a mapping for this entity type's namespace.
            string entityTypeNamespace = sourceType.Namespace ?? string.Empty;
            string contractNamespace;
            if (contractNamespaces.TryGetValue(entityTypeNamespace, out contractNamespace))
            {
                return contractNamespace;
            }

            // No mapping - use schema + CLR namespace instead.
            return DefaultDataContractSchema + Uri.EscapeUriString(entityTypeNamespace);
        }
    }
}
