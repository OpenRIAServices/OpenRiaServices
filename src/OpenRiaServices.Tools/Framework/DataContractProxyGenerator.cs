using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using OpenRiaServices.Server;

namespace OpenRiaServices.Tools
{
    /// <summary>
    /// Proxy generator base class for an type that carries data contract data.
    /// This class is particularly suited for generating partial classes with
    /// DataContract properties and invoke partial OnPropertyChanging and
    /// OnPropertyChanged methods.
    /// </summary>
    internal abstract class DataContractProxyGenerator : ProxyGenerator
    {
        private readonly bool _isRoundtripType;
        private readonly IDictionary<Type, CodeTypeDeclaration> _typeMapping;
        private readonly List<Type> _attributeTypesToFilter;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataContractProxyGenerator"/> class.
        /// </summary>
        /// <param name="proxyGenerator">The client proxy generator against which this will generate code.  Cannot be <c>null</c>.</param>
        /// <param name="type">The type to generate.  Cannot be null.</param>
        /// <param name="typeMapping">A dictionary of <see cref="DomainService"/> and related types that maps to their corresponding client-side <see cref="CodeTypeReference"/> representations.</param>
        protected DataContractProxyGenerator(CodeDomClientCodeGenerator proxyGenerator, Type type, IDictionary<Type, CodeTypeDeclaration> typeMapping)
            : base(proxyGenerator)
        {
            Type = type;
            _typeMapping = typeMapping;
            NotificationMethodGen = new NotificationMethodGenerator(proxyGenerator);
            _isRoundtripType = type.Attributes()[typeof(RoundtripOriginalAttribute)] != null;

            // Add attributes which should be used to filter the attributes on type to be generated
            _attributeTypesToFilter = new()
            {
                // DataContractAttribute and KnownTypeAttribute are handled seperatly
                typeof(DataContractAttribute),
                typeof(KnownTypeAttribute),
#if NET
                // Filter out NullableAttribute and NullableContextAttribute, should only be used by compiler
                Type.GetType("System.Runtime.CompilerServices.NullableAttribute"),
                Type.GetType("System.Runtime.CompilerServices.NullableContextAttribute"),
#endif
            };
        }

        /// <summary>
        /// The complex types that can be exposed from this type, as exposed by DomainServiceDescription.
        /// </summary>
        protected abstract IEnumerable<Type> ComplexTypes
        {
            get;
        }

        /// <summary>
        /// Returns <c>true</c> if the Type property derives from another generated type.
        /// </summary>
        protected abstract bool IsDerivedType
        {
            get;
        }

        /// <summary>
        /// The class that generates all partial notification methods.
        /// </summary>
        protected NotificationMethodGenerator NotificationMethodGen
        {
            get;
            private set;
        }

        /// <summary>
        /// The declaration through which derived classes can generate more code through protected overrides.
        /// </summary>
        protected CodeTypeDeclaration ProxyClass
        {
            get;
            private set;
        }

        /// <summary>
        /// The type whose proxy is being generated.
        /// </summary>
        protected Type Type
        {
            get;
            private set;
        }

        /// <summary>
        /// Generates the client proxy code for the given type.
        /// </summary>
        public override void Generate()
        {
            // ----------------------------------------------------------------
            // namespace
            // ----------------------------------------------------------------
            CodeNamespace ns = this.ClientProxyGenerator.GetOrGenNamespace(this.Type);

            // Missing namespace bails out of code-gen -- error has been logged
            if (ns == null)
            {
                return;
            }

            // ----------------------------------------------------------------
            // public partial class {Type} : (Base)
            // ----------------------------------------------------------------
            this.ProxyClass = CodeGenUtilities.CreateTypeDeclaration(this.Type);
            this.ProxyClass.IsPartial = true;    // makes this a partial type
            this.ProxyClass.TypeAttributes = TypeAttributes.Public;

            // Abstract classes must be preserved as abstract to avoid explicit instantiation on client
            bool isAbstract = (this.Type.IsAbstract);
            if (isAbstract)
            {
                this.ProxyClass.TypeAttributes |= TypeAttributes.Abstract;
            }

            // Determine all types derived from this one.
            // Note this list does not assume the current type is the visible root.  That is a separate test.
            IEnumerable<Type> derivedTypes = this.GetDerivedTypes();

            // If this type doesn't have any derivatives, seal it.  Cannot seal abstracts.
            if (!isAbstract && !derivedTypes.Any())
            {
                this.ProxyClass.TypeAttributes |= TypeAttributes.Sealed;
            }

            // Add all base types including interfaces
            this.AddBaseTypes(ns);
            ns.Types.Add(this.ProxyClass);

            AttributeCollection typeAttributes = this.Type.Attributes();

            // Add <summary> xml comment to class
            string comment = this.GetSummaryComment();
            this.ProxyClass.Comments.AddRange(CodeGenUtilities.GenerateSummaryCodeComment(comment, this.ClientProxyGenerator.IsCSharp));

            // ----------------------------------------------------------------
            // Add default ctr
            // ----------------------------------------------------------------
            CodeConstructor constructor = new CodeConstructor();
            // Default ctor is public for concrete types but protected for abstracts.
            // This prevents direct instantiation on client
            constructor.Attributes = isAbstract ? MemberAttributes.Family : MemberAttributes.Public;

            // add default ctor doc comments
            comment = string.Format(CultureInfo.CurrentCulture, Resource.CodeGen_Default_Constructor_Summary_Comments, this.Type.Name);
            constructor.Comments.AddRange(CodeGenUtilities.GenerateSummaryCodeComment(comment, this.ClientProxyGenerator.IsCSharp));

            // add call to default OnCreated method
            constructor.Statements.Add(this.NotificationMethodGen.OnCreatedMethodInvokeExpression);
            this.ProxyClass.Members.Add(constructor);

            // ----------------------------------------------------------------
            // [KnownType(...), ...]
            // ----------------------------------------------------------------

            // We need to generate a [KnownType] for all derived entities on the visible root.
            if (!this.IsDerivedType)
            {
                // Generate a [KnownType] for every derived type.
                // We specifically exclude [KnownTypes] from the set of attributes we ask
                // the metadata pipeline to generate below, meaning we take total control
                // here for which [KnownType] attributes get through the metadata pipeline.
                //
                // Note, we sort in alphabetic order to give predictability in baselines and
                // client readability.  For cosmetic reasons, we sort by short or long name
                // depending on what our utility helpers will actually generated
                foreach (Type derivedType in derivedTypes.OrderBy(t => this.ClientProxyGenerator.ClientProxyCodeGenerationOptions.UseFullTypeNames ? t.FullName : t.Name))
                {
                    CodeAttributeDeclaration knownTypeAttrib = CodeGenUtilities.CreateAttributeDeclaration(typeof(System.Runtime.Serialization.KnownTypeAttribute), this.ClientProxyGenerator, this.ProxyClass);
                    knownTypeAttrib.Arguments.Add(new CodeAttributeArgument(new CodeTypeOfExpression(CodeGenUtilities.GetTypeReference(derivedType, this.ClientProxyGenerator, this.ProxyClass))));
                    this.ProxyClass.CustomAttributes.Add(knownTypeAttrib);
                }
            }

            this.ValidateTypeAttributes(typeAttributes);

            // ----------------------------------------------------------------
            // [DataContract(Namespace=X, Name=Y)]
            // ----------------------------------------------------------------
            CodeAttributeDeclaration dataContractAttrib = CodeGenUtilities.CreateDataContractAttributeDeclaration(this.Type, this.ClientProxyGenerator, this.ProxyClass);
            this.ProxyClass.CustomAttributes.Add(dataContractAttrib);

            // ----------------------------------------------------------------
            // Propagate all type-level Attributes across (except DataContractAttribute since that is handled above)
            // -----------------------------------------------------------------
            CustomAttributeGenerator.GenerateCustomAttributes(
                this.ClientProxyGenerator,
                this.ProxyClass,
                ex => string.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_Attribute_ThrewException_CodeType, ex.Message, this.ProxyClass.Name, ex.InnerException.Message),
                this.FilterTypeAttributes(typeAttributes),
                this.ProxyClass.CustomAttributes,
                this.ProxyClass.Comments);

            // ----------------------------------------------------------------
            // gen proxy getter/setter for each property
            // ----------------------------------------------------------------
            this.GenerateProperties();

            // ----------------------------------------------------------------
            // gen additional methods/events
            // ----------------------------------------------------------------
            this.GenerateAdditionalMembers();

            // Register created CodeTypeDeclaration with mapping
            this._typeMapping[this.Type] = this.ProxyClass;
        }

        /// <summary>
        /// Derived classes should add the base types from which the given type derives (including interfaces).
        /// </summary>
        /// <remarks>
        /// Due to the code dom implementation, if an interface is required, a class must be added first. VB will not compile otherwise.
        /// </remarks>
        /// <param name="ns">The namespace under which Type is generated..</param>
        protected abstract void AddBaseTypes(CodeNamespace ns);

        /// <summary>
        /// Returns true if this code generator can generate a member for the
        /// specified source property type.
        /// </summary>
        /// <remarks>The base method checks for DataContract serializability. If derived classes
        /// override, they must be able to generate additional properties in
        /// <see cref="GenerateNonSerializableProperty" />.</remarks>
        /// <param name="propertyDescriptor">The source property.</param>
        /// <returns>true if this code generator should/can generate a member for the Type.</returns>
        protected virtual bool CanGenerateProperty(PropertyDescriptor propertyDescriptor)
        {
            Type type = propertyDescriptor.PropertyType;

            // Make sure the member is serializable (based on data contract attributes, [Exclude], type support, etc.).
            if (SerializationUtility.IsSerializableDataMember(propertyDescriptor))
            {
                // If property type is an enum that cannot be generated, we cannot expose this property, but only log a warning
                string errorMessage = null;
                Type enumType = TypeUtility.GetNonNullableType(type);
                if (enumType.IsEnum)
                {
                    if (!this.ClientProxyGenerator.CanExposeEnumType(enumType, out errorMessage))
                    {
                        this.ClientProxyGenerator.LogWarning(String.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_Property_Enum_Error, this.Type, propertyDescriptor.Name, enumType.FullName, errorMessage));
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Properties appearing on the type may be generated on the base class. If that is the case,
        /// this method should return false.
        /// </summary>
        /// <param name="pd">The property to be generated.</param>
        /// <returns>True if the property should be generated on this proxy.</returns>
        protected virtual bool CanGeneratePropertyIfPolymorphic(PropertyDescriptor pd)
        {
            return true;
        }

        /// <summary>
        /// Derived classes should generate all non properties in this method.
        /// </summary>
        protected virtual void GenerateAdditionalMembers()
        {
            // Add OnMethodChanging/Changed partial methods.
            this.ProxyClass.Members.AddRange(this.NotificationMethodGen.PartialMethodsSnippetBlock);
        }

        /// <summary>
        /// Gererate property setter validation for the specified property name.
        /// </summary>
        /// <param name="propertyName">The property to validate.</param>
        /// <returns>The code statement for the validation call.</returns>
        protected static CodeStatement GeneratePropertySetterValidation(string propertyName)
        {
            // this.ValidateProperty("propertyName", value);
            return new CodeExpressionStatement(
                        new CodeMethodInvokeExpression(
                            new CodeThisReferenceExpression(),
                            "ValidateProperty",
                            new CodeExpression[]
                            {
                                new CodePrimitiveExpression(propertyName),
                                new CodePropertySetValueReferenceExpression()
                            }));
        }

        /// <summary>
        /// This is the interception point that allows derived classes to generate properties that
        /// are not strictly DataContract serializable. If the derived class overrode
        /// <see cref="CanGenerateProperty"/>, the derived class must override this method.
        /// </summary>
        /// <param name="pd">The property to be generated.</param>
        /// <returns>Returns true if the property was generated.</returns>
        protected virtual bool GenerateNonSerializableProperty(PropertyDescriptor pd)
        {
            return false;
        }

        /// <summary>
        /// Returns the types deriving from the type to be generated.
        /// </summary>
        /// <returns>The types deriving from the type to be generated.</returns>
        protected abstract IEnumerable<Type> GetDerivedTypes();

        /// <summary>
        /// Generates the summary comment for the class.
        /// </summary>
        /// <returns>Returns the summary comment content.</returns>
        protected abstract string GetSummaryComment();

        /// <summary>
        /// Users the shared types service to determine whether the given property is shared.
        /// </summary>
        /// <remarks>Derived classes can override this to insert extra behavior.</remarks>
        /// <param name="pd">The property descriptor that could be shared.</param>
        /// <returns>Returns true if the property is shared.</returns>
        protected virtual bool IsPropertyShared(PropertyDescriptor pd)
        {
            // If this property is visible to the client already because of partial types,
            // do not generate it again, or we will get a compile error
            CodeMemberShareKind shareKind = this.ClientProxyGenerator.GetPropertyShareKind(this.Type, pd.Name);
            return ((shareKind & CodeMemberShareKind.Shared) != 0);
        }

        /// <summary>
        /// Called when a property that should be generated, cannot be generated.
        /// </summary>
        /// <param name="pd">The property that was not generated.</param>
        protected virtual void OnPropertySkipped(PropertyDescriptor pd)
        {
        }

        /// <summary>
        /// Returns true if the property should be declared.
        /// </summary>
        /// <remarks>Properties are typically not declared if they are available
        /// through other means (such as shared files) or otherwise excluded from
        /// this type.</remarks>
        /// <param name="pd">The property being declared.</param>
        /// <returns>True if the property should be declared.</returns>
        protected virtual bool ShouldDeclareProperty(PropertyDescriptor pd)
        {
            AttributeCollection propertyAttributes = pd.ExplicitAttributes();

            if (this.IsExcluded(pd, propertyAttributes))
            {
                // Ignore the [Include] because that's what we do during serialization as well. (We don't want to 
                // check for [Exclude] + [Include] everywhere in our code base.)
                return false;
            }

            if (this.IsPropertyShared(pd))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Generates a property getter/setter pair into the given proxy class to match the given property info.
        /// </summary>
        /// <param name="propertyDescriptor">PropertyDescriptor for the property to generate for.</param>
        protected virtual void GenerateProperty(PropertyDescriptor propertyDescriptor)
        {
            string propertyName = propertyDescriptor.Name;
            Type propertyType = CodeGenUtilities.TranslateType(propertyDescriptor.PropertyType);

            // ----------------------------------------------------------------
            // Property type ref
            // ----------------------------------------------------------------
            var propTypeReference = CodeGenUtilities.GetTypeReference(propertyType, this.ClientProxyGenerator, this.ProxyClass);

            // ----------------------------------------------------------------
            // Property decl
            // ----------------------------------------------------------------
            var property = new CodeMemberProperty();
            property.Name = propertyName;
            property.Type = propTypeReference;
            property.Attributes = MemberAttributes.Public | MemberAttributes.Final; // final needed, else becomes virtual
            List<Attribute> propertyAttributes = propertyDescriptor.ExplicitAttributes().Cast<Attribute>().ToList();

            // Generate <summary> for property
            string comment = string.Format(CultureInfo.CurrentCulture, Resource.CodeGen_Entity_Property_Summary_Comment, propertyName);
            property.Comments.AddRange(CodeGenUtilities.GenerateSummaryCodeComment(comment, this.ClientProxyGenerator.IsCSharp));

            // ----------------------------------------------------------------
            // [DataMember] -> Add if not already present.
            // ----------------------------------------------------------------
            // Add if not already present.

            if (!propertyAttributes.OfType<DataMemberAttribute>().Any())
            {
                CodeAttributeDeclaration dataMemberAtt = CodeGenUtilities.CreateAttributeDeclaration(typeof(DataMemberAttribute), this.ClientProxyGenerator, this.ProxyClass);
                property.CustomAttributes.Add(dataMemberAtt);
            }

            // Here, we check for the existence of [ReadOnly(true)] attributes generated when
            // the property does not not have a setter.  We want to inject an [Editable(false)]
            // attribute into the pipeline.
            ReadOnlyAttribute readOnlyAttr = propertyAttributes.OfType<ReadOnlyAttribute>().SingleOrDefault();
            if (readOnlyAttr != null && !propertyAttributes.OfType<EditableAttribute>().Any())
            {
                propertyAttributes.Add(new EditableAttribute(!readOnlyAttr.IsReadOnly));

                // REVIEW:  should we strip out [ReadOnly] attributes here?
            }

            // Here we check if the type has a RoundtripOriginalAttribute. In that case we strip it out from the property
            if (this._isRoundtripType)
            {
                propertyAttributes.RemoveAll(attr => attr.GetType() == typeof(RoundtripOriginalAttribute));
            }

            // Here we check for database generated fields. In that case we strip any RequiredAttribute from the property.
            if (propertyAttributes.Any(a => a.GetType().Name == "DatabaseGeneratedAttribute"))
            {
                propertyAttributes.RemoveAll(attr => attr.GetType() == typeof(RequiredAttribute));
            }

            // Here, we check for the presence of a complex type. If it exists we need to add a DisplayAttribute
            // if not already there. DataSources windows do not handle complex types
            if (TypeUtility.IsSupportedComplexType(propertyType) && !propertyAttributes.OfType<DisplayAttribute>().Any())
            {
                CodeAttributeDeclaration displayAttribute = CodeGenUtilities.CreateDisplayAttributeDeclaration(this.ClientProxyGenerator, this.ProxyClass);
                property.CustomAttributes.Add(displayAttribute);
            }

            // ----------------------------------------------------------------
            // Propagate the custom attributes
            // ----------------------------------------------------------------

            CustomAttributeGenerator.GenerateCustomAttributes(
                this.ClientProxyGenerator,
                this.ProxyClass,
                ex => string.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_Attribute_ThrewException_CodeTypeMember, ex.Message, property.Name, this.ProxyClass.Name, ex.InnerException.Message),
                propertyAttributes.Cast<Attribute>(),
                property.CustomAttributes,
                property.Comments);

            // ----------------------------------------------------------------
            // backing private field (CodeDom doesn't yet know about auto properties)
            // ----------------------------------------------------------------
            string fieldName = CodeGenUtilities.MakeCompliantFieldName(propertyName);
            var field = new CodeMemberField(propTypeReference, fieldName);
            this.ProxyClass.Members.Add(field);
            var fieldRef = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fieldName);
            var valueRef = new CodePropertySetValueReferenceExpression();

            // ----------------------------------------------------------------
            // getter body
            // ----------------------------------------------------------------
            property.GetStatements.Add(new CodeMethodReturnStatement(fieldRef));

            // ----------------------------------------------------------------
            // setter body
            // ----------------------------------------------------------------
            List<CodeStatement> bodyStatements = new List<CodeStatement>();

            // this.OnPropertyXxxChanging(PropType value);
            bodyStatements.Add(this.NotificationMethodGen.GetMethodInvokeExpressionStatementFor(propertyName + "Changing"));

            bool propertyIsReadOnly = this.IsPropertyReadOnly(propertyDescriptor);
            if (!propertyIsReadOnly)
            {
                bodyStatements.Add(new CodeExpressionStatement(new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), "RaiseDataMemberChanging", new CodePrimitiveExpression(propertyDescriptor.Name))));
            }

            // Generate the validation tests.
            CodeStatement validationCode = GeneratePropertySetterValidation(propertyDescriptor.Name);
            bodyStatements.Add(validationCode);

            // this._field = value
            bodyStatements.Add(new CodeAssignStatement(fieldRef, valueRef));

            if (!propertyIsReadOnly)
            {
                bodyStatements.Add(new CodeExpressionStatement(new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), "RaiseDataMemberChanged", new CodePrimitiveExpression(propertyDescriptor.Name))));
            }
            else
            {
                // even read-only members need to raise PropertyChanged
                bodyStatements.Add(new CodeExpressionStatement(new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), "RaisePropertyChanged", new CodePrimitiveExpression(propertyDescriptor.Name))));
            }

            // this.OnPropertyXxxChanged();
            bodyStatements.Add(this.NotificationMethodGen.GetMethodInvokeExpressionStatementFor(propertyName + "Changed"));

            // if (this._field != value)...
            CodeExpression valueTest = CodeGenUtilities.MakeNotEqual(propertyType, fieldRef, valueRef, this.ClientProxyGenerator.IsCSharp);

            CodeConditionStatement body = new CodeConditionStatement(valueTest, bodyStatements.ToArray<CodeStatement>());

            property.SetStatements.Add(body);

            // add property
            this.ProxyClass.Members.Add(property);
        }

        /// <summary>
        /// Checks if the type level attributes on the type are valid.
        /// </summary>
        /// <param name="typeAttributes">The collection of attributes on the type.</param>
        protected virtual void ValidateTypeAttributes(AttributeCollection typeAttributes)
        {
        }

        /// <summary>
        /// Returns the list of all the attributes that need to be propagated to the client
        /// </summary>
        /// <param name="typeAttributes">List of attributes on the type</param>
        /// <returns>List of attributes to be propagated to the client</returns>
        private IEnumerable<Attribute> FilterTypeAttributes(AttributeCollection typeAttributes)
        {
            List<Attribute> filteredAttributes = new List<Attribute>();

            // Ignore DefaultMemberAttribute if it has been put for an indexer
            IEnumerable<Attribute> defaultMemberAttribs = typeAttributes.Cast<Attribute>().Where(a => a.GetType() == typeof(DefaultMemberAttribute));
            if (defaultMemberAttribs.Any())
            {
                HashSet<string> properties = new HashSet<string>(TypeDescriptor.GetProperties(this.Type).Cast<PropertyDescriptor>().Select(p => p.Name), StringComparer.Ordinal);
                foreach (DefaultMemberAttribute attrib in defaultMemberAttribs)
                {
                    if (!properties.Contains(attrib.MemberName))
                    {
                        filteredAttributes.Add(attrib);
                    }
                }
            }

            // Filter out attributes in filteredAttributes and attributeTypesToFilter
            return typeAttributes.Cast<Attribute>().Where(a => !_attributeTypesToFilter.Contains(a.GetType()) && !filteredAttributes.Contains(a));
        }

        /// <summary>
        /// Generates all of the properties for the type.
        /// </summary>
        private void GenerateProperties()
        {
            IEnumerable<PropertyDescriptor> properties = TypeDescriptor.GetProperties(this.Type)
                .Cast<PropertyDescriptor>()
                .OrderBy(p => p.Name);

            foreach (PropertyDescriptor pd in properties)
            {
                if (!this.ShouldDeclareProperty(pd))
                {
                    continue;
                }

                // Generate a property getter/setter pair for every property whose type
                // we support. Non supported property types will be skipped.
                if (this.CanGenerateProperty(pd))
                {
                    // Ensure the property is not virtual, abstract or new
                    // If there is a violation, we log the error and keep
                    // running to accumulate all such errors.  This function
                    // may return an "okay" for non-error case polymorphics.
                    if (!this.CanGeneratePropertyIfPolymorphic(pd))
                    {
                        continue;
                    }

                    if (!this.GenerateNonSerializableProperty(pd))
                    {
                        Type propType = CodeGenUtilities.TranslateType(pd.PropertyType);
                        List<Type> typesToCodeGen = new List<Type>();
                        bool isTypeSafeToGenerate = true;

                        // Create a list containing the types we will require on the client
                        if (TypeUtility.IsPredefinedDictionaryType(propType))
                        {
                            typesToCodeGen.AddRange(CodeGenUtilities.GetDictionaryGenericArgumentTypes(propType));
                        }
                        else
                        {
                            typesToCodeGen.Add(TypeUtility.GetElementType(propType));
                        }

                        // We consider all predefined types as legal to code-gen *except* those
                        // that would generate a compile error on the client due to missing reference.
                        // We treat "don't know" and "false" as grounds for a warning.
                        // Note that we do this *after* TranslateType so that types like System.Data.Linq.Binary
                        // which cannot exist on the client anyway has been translated
                        foreach (Type type in typesToCodeGen)
                        {
                            // Enum (and nullable<enum>) types may require generation on client
                            Type nonNullableType = TypeUtility.GetNonNullableType(type);


                            if (nonNullableType.IsEnum)
                            {
                                // Register use of this enum type, which could cause deferred generation
                                this.ClientProxyGenerator.RegisterUseOfEnumType(nonNullableType);
                            }
                            // If this is not an enum or nullable<enum> and we're not generating the complex type, determine whether this
                            // property type is visible to the client.  If it is not, log a warning.
                            else if (!this.ComplexTypes.Contains(type))
                            {
                                // "Don't know" counts as "no"
                                CodeMemberShareKind enumShareKind = this.ClientProxyGenerator.GetTypeShareKind(nonNullableType);
                                if ((enumShareKind & CodeMemberShareKind.Shared) == 0)
                                {
                                    this.ClientProxyGenerator.LogWarning(string.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_PropertyType_Not_Shared, pd.Name, this.Type.FullName, type.FullName, this.ClientProxyGenerator.ClientProjectName));
                                    isTypeSafeToGenerate = false; // Flag error but continue to allow accumulation of additional errors.
                                }
                            }
                        }

                        if (isTypeSafeToGenerate)
                        {
                            // Generate OnMethodXxxChanging/Changed partial methods.

                            // Note: the parameter type reference needs to handle the possibility the
                            // property type is defined in the project's root namespace and that VB prepends
                            // that namespace.  The utility helper gives us the right type reference.
                            CodeTypeReference parameterTypeRef =
                                CodeGenUtilities.GetTypeReference(propType, this.ClientProxyGenerator, this.ProxyClass);

                            this.NotificationMethodGen.AddMethodFor(pd.Name + "Changing", new CodeParameterDeclarationExpression(parameterTypeRef, "value"), null);
                            this.NotificationMethodGen.AddMethodFor(pd.Name + "Changed", null);

                            this.GenerateProperty(pd);
                        }
                    }
                }
                else
                {
                    this.OnPropertySkipped(pd);
                }
            }
        }

        /// <summary>
        /// Determines if a property is excluded by analyzing its attributes
        /// looking for an <see cref="ExcludeAttribute"/>.
        /// </summary>
        /// <param name="pd">The property to be tested</param>
        /// <param name="propertyAttributes">The attributes for the property</param>
        /// <returns><c>true</c> if the property is to be excluded, otherwise <c>false</c></returns>
        private bool IsExcluded(PropertyDescriptor pd, AttributeCollection propertyAttributes)
        {
            // The [Exclude] attribute is a signal simply to omit this property, no matter what
            bool hasExcludeAttr = (propertyAttributes[typeof(ExcludeAttribute)] != null);
            if (hasExcludeAttr)
            {
                // If we also see an [Include], warn the user.
                if (propertyAttributes[typeof(IncludeAttribute)] != null)
                {
                    this.ClientProxyGenerator.LogWarning(String.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_Cannot_Have_Include_And_Exclude, pd.Name, this.Type));
                }
            }

            return hasExcludeAttr;
        }

        /// <summary>
        /// Returns true if the specified property is read-only by virtue of having
        /// an appropriately configured ReadOnlyAttribute or EditableAttribute applied.
        /// </summary>
        /// <param name="property">The property to check for editability.</param>
        /// <returns>True if the specified property is read-only, false otherwise.</returns>
        protected bool IsPropertyReadOnly(PropertyDescriptor property)
        {
            // Here, we continue to respect the [ReadOnly] attribute because TypeDescriptor
            // will materialize this when a property setter is not available.
            ReadOnlyAttribute readOnlyAttr = property.Attributes[typeof(ReadOnlyAttribute)] as ReadOnlyAttribute;
            if (readOnlyAttr != null && readOnlyAttr.IsReadOnly)
            {
                return true;
            }

            EditableAttribute editableAttribute = property.Attributes[typeof(EditableAttribute)] as EditableAttribute;
            if (editableAttribute != null && !editableAttribute.AllowEdit)
            {
                return true;
            }

            return false;
        }
    }
}
