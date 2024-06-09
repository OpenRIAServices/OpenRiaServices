using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using OpenRiaServices.Server;
using System.Xml;

namespace OpenRiaServices.Hosting.Wcf.Behaviors
{
    internal static class ServiceMetadataGenerator
    {
        private static List<TypeMetadata> entitiesMetadata = new List<TypeMetadata>();
        public static IEnumerable<TypeMetadata> EntitiesMetadata { get { return entitiesMetadata; } }

        public static void GenerateEntitiesMetadataJsonMap(DomainServiceDescription description)
        {
            foreach (Type entityType in description.EntityTypes)
            {
                entitiesMetadata.Add(new TypeMetadata(entityType));
            }
            foreach (Type complexType in description.ComplexTypes)
            {
                entitiesMetadata.Add(new TypeMetadata(complexType));
            }
        }

        internal class TypeMetadata
        {
            private readonly List<string> key = new List<string>();
            private readonly List<TypePropertyMetadata> properties = new List<TypePropertyMetadata>();

            public string Name { get; private set; }
            public string TypeName { get; private set; }
            public string TypeNamespace { get; private set; }
            public IEnumerable<string> Key { get { return this.key; } }
            public IEnumerable<TypePropertyMetadata> Properties { get { return this.properties; } }

            public TypeMetadata(Type entityType)
            {
                this.Name = entityType.Name;

                Type type = IsCollectionType(entityType) ? TypeUtility.GetElementType(entityType) : entityType;
                this.TypeName = type.Name;
                this.TypeNamespace = type.Namespace;

                IEnumerable<PropertyDescriptor> properties = GetPropertiesToGenerate(entityType);

                foreach (PropertyDescriptor pd in properties)
                {
                    this.properties.Add(new TypePropertyMetadata(pd));
                    if (pd.Attributes[typeof(KeyAttribute)] != null)
                    {
                        this.key.Add(pd.Name);
                    }
                }
            }

            public void WriteJson(XmlDictionaryWriter writer)
            {
                writer.WriteStartElement(MetadataStrings.TypeString);
                writer.WriteAttributeString("type", "string");
                writer.WriteString(String.Format("{0}{1}{2}", this.TypeName, MetadataStrings.NamespaceMarker, this.TypeNamespace));
                writer.WriteEndElement();

                writer.WriteStartElement(MetadataStrings.KeyString);
                writer.WriteAttributeString("type", "array");
                foreach (string keyitem in this.Key)
                {
                    writer.WriteStartElement("item");
                    writer.WriteAttributeString("type", "string");
                    writer.WriteValue(keyitem);
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();

                writer.WriteStartElement(MetadataStrings.FieldsString);
                writer.WriteAttributeString("type", "object");
                foreach (TypePropertyMetadata field in this.Properties)
                {
                    field.WriteJson(writer);
                }
                writer.WriteEndElement();

                this.WriteValidationRulesMetadata(writer);
            }

            private void WriteValidationRulesMetadata(XmlDictionaryWriter writer)
            {
                // The rules section is optional. Unless we encounter validation rules, we won't write it.
                bool ruleSectionStarted = false;
                foreach (TypePropertyMetadata field in this.Properties)
                {
                    if (field.ValidationRules.Any())
                    {
                        if (!ruleSectionStarted)
                        {
                            ruleSectionStarted = true;
                            writer.WriteStartElement(MetadataStrings.RulesString);
                            writer.WriteAttributeString("type", "object");
                        }
                        writer.WriteStartElement(field.Name);
                        writer.WriteAttributeString("type", "object");
                        foreach (TypePropertyValidationRuleMetadata rule in field.ValidationRules)
                        {
                            rule.WriteJson(writer);
                        }
                        writer.WriteEndElement();
                    }
                }

                //Close the rules section if it was opened, and add error message if any.
                if (ruleSectionStarted)
                {
                    writer.WriteEndElement();
                    this.WriteValidationErrorMessagesMetadata(writer);
                }
            }

            private void WriteValidationErrorMessagesMetadata(XmlDictionaryWriter writer)
            {
                // The messages section is optional. Unless we encounter messages, we won't write it.
                bool errorMsgSectionStarted = false;
                foreach (TypePropertyMetadata property in this.Properties)
                {
                    bool currentFieldMessageSectionStarted = false;
                    if (property.ValidationRules.Any())
                    {
                        foreach (TypePropertyValidationRuleMetadata rule in property.ValidationRules)
                        {
                            if (rule.ErrorMessageString != null)
                            {
                                // Need to write this once for all messages.
                                if (!errorMsgSectionStarted)
                                {
                                    errorMsgSectionStarted = true;
                                    writer.WriteStartElement(MetadataStrings.MessagesString);
                                    writer.WriteAttributeString("type", "object");
                                }
                                // Need to write this once for each fields.
                                if (!currentFieldMessageSectionStarted)
                                {
                                    currentFieldMessageSectionStarted = true;
                                    writer.WriteStartElement(property.Name);
                                    writer.WriteAttributeString("type", "object");
                                }
                                //now write error message for this rule.
                                writer.WriteStartElement(rule.Name);
                                writer.WriteAttributeString("type", "string");
                                writer.WriteValue(rule.ErrorMessageString);
                                writer.WriteEndElement();
                            }
                        }
                    }
                    // Close this field error message if it was written out.
                    if (currentFieldMessageSectionStarted)
                    {
                        writer.WriteEndElement();
                    }
                }

                //Close the messages section if it was opened.
                if (errorMsgSectionStarted)
                {
                    writer.WriteEndElement();
                }
            }

            private static IEnumerable<PropertyDescriptor> GetPropertiesToGenerate(Type entityType)
            {
                IEnumerable<PropertyDescriptor> properties = TypeDescriptor.GetProperties(entityType)
                    .Cast<PropertyDescriptor>()
                    .OrderBy(p => p.Name);
                List<PropertyDescriptor> propertiesToGenerate = new List<PropertyDescriptor>();

                foreach (PropertyDescriptor pd in properties)
                {
                    // Ignore this inherited type
                    if (pd.PropertyType.FullName.Equals("System.Data.EntityKey"))
                    {
                        continue;
                    }
                    // Ignore everything marked as excluded
                    if ((ExcludeAttribute)pd.Attributes[typeof(ExcludeAttribute)] != null)
                    {
                        continue;
                    }

                    propertiesToGenerate.Add(pd);
                }

                return propertiesToGenerate;
            }
        }

        internal class TypePropertyMetadata
        {
            private readonly List<TypePropertyValidationRuleMetadata> validationRules = new List<TypePropertyValidationRuleMetadata>();

            public string Name { get; private set; }
            public string TypeName { get; private set; }
            public string TypeNamespace { get; private set; }
            public bool IsReadOnly { get; private set; }
            public bool IsArray { get; private set; }
            public TypePropertyAssociationMetadata Association { get; private set; }
            public IEnumerable<TypePropertyValidationRuleMetadata> ValidationRules { get { return this.validationRules; } }

            public TypePropertyMetadata(PropertyDescriptor descriptor)
            {
                this.Name = descriptor.Name;

                if (IsCollectionType(descriptor.PropertyType))
                {
                    Type type = TypeUtility.GetElementType(descriptor.PropertyType);
                    // String is assignable from IEnumerable, but it is not an array in JavaScript.
                    // Manually ignoring this if the type is the same as the descriptor.PropertyType
                    this.IsArray = type.Equals(descriptor.PropertyType) ? false : true;
                    this.TypeName = type.Name;
                    this.TypeNamespace = type.Namespace;
                }
                else
                {
                    this.IsArray = false;
                    this.TypeName = descriptor.PropertyType.Name;
                    this.TypeNamespace = descriptor.PropertyType.Namespace;
                }

                AttributeCollection propertyAttributes = descriptor.Attributes;

                ReadOnlyAttribute readonlyAttr = (ReadOnlyAttribute)propertyAttributes[typeof(ReadOnlyAttribute)];
                this.IsReadOnly = (readonlyAttr != null) ? readonlyAttr.IsReadOnly : false;

                EntityAssociationAttribute associationAttr = (EntityAssociationAttribute)propertyAttributes[typeof(EntityAssociationAttribute)];
                if (associationAttr != null)
                {
                    this.Association = new TypePropertyAssociationMetadata(associationAttr);
                }

                foreach (Attribute attribute in propertyAttributes)
                {
                    if (attribute is RequiredAttribute)
                    {
                        this.validationRules.Add(new TypePropertyValidationRuleMetadata((RequiredAttribute)attribute));
                    }
                    else if (attribute is RangeAttribute)
                    {
                        RangeAttribute rangeAttribute = (RangeAttribute)attribute;

                        if (rangeAttribute.OperandType.Equals(typeof(Double))
                            || rangeAttribute.OperandType.Equals(typeof(Int16))
                            || rangeAttribute.OperandType.Equals(typeof(Int32))
                            || rangeAttribute.OperandType.Equals(typeof(Int64))
                            || rangeAttribute.OperandType.Equals(typeof(Single)))
                        {
                            this.validationRules.Add(new TypePropertyValidationRuleMetadata(rangeAttribute));
                        }
                    }
                    else if (attribute is StringLengthAttribute)
                    {
                        this.validationRules.Add(new TypePropertyValidationRuleMetadata((StringLengthAttribute)attribute));
                    }
                    else if (attribute is DataTypeAttribute)
                    {
                        DataTypeAttribute dataTypeAttribute = (DataTypeAttribute)attribute;

                        if (dataTypeAttribute.DataType.Equals(DataType.EmailAddress)
                            || dataTypeAttribute.DataType.Equals(DataType.Url))
                        {
                            this.validationRules.Add(new TypePropertyValidationRuleMetadata(dataTypeAttribute));
                        }
                    }
                }
            }

            public void WriteJson(XmlDictionaryWriter writer)
            {
                writer.WriteStartElement(this.Name);
                writer.WriteAttributeString("type", "object");

                writer.WriteStartElement(MetadataStrings.TypeString);
                writer.WriteAttributeString("type", "string");
                writer.WriteString(String.Format("{0}{1}{2}", this.TypeName, MetadataStrings.NamespaceMarker, this.TypeNamespace));
                writer.WriteEndElement();

                if (this.IsReadOnly)
                {
                    writer.WriteStartElement(MetadataStrings.ReadOnlyString);
                    writer.WriteAttributeString("type", "boolean");
                    writer.WriteValue(this.IsReadOnly);
                    writer.WriteEndElement();
                }

                if (this.IsArray)
                {
                    writer.WriteStartElement(MetadataStrings.ArrayString);
                    writer.WriteAttributeString("type", "boolean");
                    writer.WriteValue(this.IsArray);
                    writer.WriteEndElement();
                }

                if (this.Association != null)
                {
                    writer.WriteStartElement(MetadataStrings.AssociationString);
                    writer.WriteAttributeString("type", "object");
                    this.Association.WriteJson(writer);
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
            }
        }

        internal class TypePropertyAssociationMetadata
        {
            private readonly List<string> thisKeyMembers = new List<string>();
            private readonly List<string> otherKeyMembers = new List<string>();

            public string Name { get; private set; }
            public bool IsForeignKey { get; private set; }
            public IEnumerable<string> ThisKeyMembers { get { return this.thisKeyMembers; } }
            public IEnumerable<string> OtherKeyMembers { get { return this.otherKeyMembers; } }

            public TypePropertyAssociationMetadata(EntityAssociationAttribute associationAttr)
            {
                this.Name = associationAttr.Name;
                this.IsForeignKey = associationAttr.IsForeignKey;
                this.otherKeyMembers = associationAttr.OtherKeyMembers.ToList<string>();
                this.thisKeyMembers = associationAttr.ThisKeyMembers.ToList<string>();
            }

            public void WriteJson(XmlDictionaryWriter writer)
            {
                writer.WriteStartElement(MetadataStrings.NameString);
                writer.WriteAttributeString("type", "string");
                writer.WriteString(this.Name);
                writer.WriteEndElement();

                writer.WriteStartElement(MetadataStrings.ThisKeyString);
                writer.WriteAttributeString("type", "array");
                foreach (string thisKey in this.ThisKeyMembers)
                {
                    writer.WriteStartElement("item");
                    writer.WriteAttributeString("type", "string");
                    writer.WriteString(thisKey);
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();

                writer.WriteStartElement(MetadataStrings.OtherKeyString);
                writer.WriteAttributeString("type", "array");
                foreach (string otherKey in this.OtherKeyMembers)
                {
                    writer.WriteStartElement("item");
                    writer.WriteAttributeString("type", "string");
                    writer.WriteString(otherKey);
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();

                writer.WriteStartElement(MetadataStrings.IsForeignKey);
                writer.WriteAttributeString("type", "boolean");
                writer.WriteValue(this.IsForeignKey);
                writer.WriteEndElement();
            }
        }

        internal class TypePropertyValidationRuleMetadata
        {
            public string Name { get; private set; }
            public object Value1 { get; private set; }
            public object Value2 { get; private set; }
            public string ErrorMessageString { get; private set; }
            private readonly string type;

            public TypePropertyValidationRuleMetadata(RequiredAttribute attribute)
                : this((ValidationAttribute)attribute)
            {
                this.Name = "required";
                this.Value1 = true;
                this.type = "boolean";
            }

            public TypePropertyValidationRuleMetadata(RangeAttribute attribute)
                : this((ValidationAttribute)attribute)
            {
                this.Name = "range";
                this.Value1 = attribute.Minimum;
                this.Value2 = attribute.Maximum;
                this.type = "array";
            }

            public TypePropertyValidationRuleMetadata(StringLengthAttribute attribute)
                : this((ValidationAttribute)attribute)
            {
                if (attribute.MinimumLength != 0)
                {
                    this.Name = "rangelength";
                    this.Value1 = attribute.MinimumLength;
                    this.Value2 = attribute.MaximumLength;
                    this.type = "array";
                }
                else
                {
                    this.Name = "maxlength";
                    this.Value1 = attribute.MaximumLength;
                    this.type = "number";
                }
            }

            public TypePropertyValidationRuleMetadata(DataTypeAttribute attribute)
                : this((ValidationAttribute)attribute)
            {
                switch (attribute.DataType)
                {
                    case DataType.EmailAddress:
                        this.Name = "email";
                        break;
                    case DataType.Url:
                        this.Name = "url";
                        break;
                    default:
                        break;
                }
                this.Value1 = "true";
                this.type = "boolean";
            }

            public TypePropertyValidationRuleMetadata(ValidationAttribute attribute)
            {
                if (attribute.ErrorMessage != null)
                {
                    this.ErrorMessageString = attribute.ErrorMessage;
                }
            }

            // The output json is determined by the number of values. The object constructor takes care the value assignment.
            // When we have two values, we have two numbers that are written as an array.
            // When we have only one value, it is written as it's type only.
            public void WriteJson(XmlDictionaryWriter writer)
            {
                writer.WriteStartElement(this.Name);
                writer.WriteAttributeString("type", this.type);
                if (this.type == "array")
                {
                    writer.WriteStartElement("item");
                    writer.WriteAttributeString("type", "number");
                    writer.WriteValue(this.Value1);
                    writer.WriteEndElement();
                    writer.WriteStartElement("item");
                    writer.WriteAttributeString("type", "number");
                    writer.WriteValue(this.Value2);
                    writer.WriteEndElement();
                }
                else
                {
                    writer.WriteValue(this.Value1);
                }
                writer.WriteEndElement();
            }
        }

        private class MetadataStrings
        {
            public const string NamespaceMarker = ":#";
            public const string TypeString = "type";
            public const string ArrayString = "array";
            public const string AssociationString = "association";
            public const string FieldsString = "fields";
            public const string ThisKeyString = "thisKey";
            public const string IsForeignKey = "isForeignKey";
            public const string OtherKeyString = "otherKey";
            public const string NameString = "name";
            public const string ReadOnlyString = "readonly";
            public const string KeyString = "key";
            public const string RulesString = "rules";
            public const string MessagesString = "messages";
        }

        internal static bool IsCollectionType(Type t)
        {
            return typeof(IEnumerable).IsAssignableFrom(t);
        }
    }
}
