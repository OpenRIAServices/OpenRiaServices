namespace OpenRiaServices.DomainServices.Tools.TextTemplate
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;
    using OpenRiaServices.DomainServices;
    using OpenRiaServices.DomainServices.Server;
    using OpenRiaServices.DomainServices.Tools;

    /// <summary>
    /// Base class to generate proxy for a data contract type.
    /// </summary>
    public abstract partial class DataContractProxyGenerator
    {
        internal bool IsAbstract { get; set; }

        List<PropertyDescriptor> _notificationMethodList;
        internal IEnumerable<PropertyDescriptor> NotificationMethodList
        {
            get
            {
                if (this._notificationMethodList == null)
                {
                    this._notificationMethodList = new List<PropertyDescriptor>();
                }
                return this._notificationMethodList;
            }
        }

        /// <summary>
        /// Gets or sets the type for which the proxy is to be generated.
        /// </summary>
        protected Type Type { get; set; }

        /// <summary>
        /// Gets or sets the ClientCodeGenerator object.
        /// </summary>
        protected ClientCodeGenerator ClientCodeGenerator { get; set; }

        /// <summary>
        /// Generates proxy code in a specific language.
        /// </summary>
        /// <returns>Actual code for the proxy.</returns>
        protected abstract string GenerateDataContractProxy();

        internal abstract IEnumerable<Type> ComplexTypes
        {
            get;
        }

        private IEnumerable<PropertyDescriptor> _properties;
        internal IEnumerable<PropertyDescriptor> Properties
        {
            get
            {
                if (this._properties == null)
                {
                    this._properties = new List<PropertyDescriptor>();
                }
                return this._properties;
            }
        }

        internal virtual void Initialize()
        {
            this.IsAbstract = this.Type.IsAbstract;
            this._notificationMethodList = new List<PropertyDescriptor>();
            this._properties = this.GetPropertiesToGenerate();
            this.GenerationEnvironment.Clear();
        }

        internal abstract string GetBaseTypeName();

        internal abstract bool IsDerivedType
        {
            get;
        }

        internal bool IsPropertyReadOnly(PropertyDescriptor property)
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

        internal IEnumerable<Attribute> GetPropertyAttributes(PropertyDescriptor propertyDescriptor, Type propertyType)
        {
            List<Attribute> propertyAttributes = propertyDescriptor.ExplicitAttributes().Cast<Attribute>().ToList();
            if (!propertyAttributes.OfType<DataMemberAttribute>().Any())
            {
                propertyAttributes.Add(new DataMemberAttribute());
            }

            ReadOnlyAttribute readOnlyAttr = propertyAttributes.OfType<ReadOnlyAttribute>().SingleOrDefault();
            if (readOnlyAttr != null && !propertyAttributes.OfType<EditableAttribute>().Any())
            {
                propertyAttributes.Add(new EditableAttribute(!readOnlyAttr.IsReadOnly));
            }

            if (TypeUtility.IsSupportedComplexType(propertyType) && !propertyAttributes.OfType<DisplayAttribute>().Any())
            {
                DisplayAttribute displayAttribute = new DisplayAttribute() { AutoGenerateField = false };
                propertyAttributes.Add(displayAttribute);
            }

            // If the data contract type already contains the RoundtripOriginalAttribute, then we remove the attribute from properties.
            if (this.Type.Attributes()[typeof(RoundtripOriginalAttribute)] != null)
            {
                propertyAttributes.RemoveAll(attr => attr.GetType() == typeof(RoundtripOriginalAttribute));
            }

            return propertyAttributes;
        }

        internal IEnumerable<Attribute> GetTypeAttributes()
        {
            AttributeCollection typeAttributes = this.Type.Attributes();
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

            // Filter out attributes in filteredAttributes as well as DataContractAttribute and KnownTypeAttribute (since they are already handled in GenerateTypeAttributes())
            return typeAttributes.Cast<Attribute>().Where(a => a.GetType() != typeof(DataContractAttribute) && a.GetType() != typeof(KnownTypeAttribute) &&
                !(filteredAttributes.Contains(a)));
        }

        internal IEnumerable<PropertyDescriptor> GetPropertiesToGenerate()
        {
            IEnumerable<PropertyDescriptor> properties = TypeDescriptor.GetProperties(this.Type)
                .Cast<PropertyDescriptor>()
                .OrderBy(p => p.Name);
            List<PropertyDescriptor> propertiesToGenerate = new List<PropertyDescriptor>();

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

                    if (!this.HandleNonSerializableProperty(pd))
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
                                this.ClientCodeGenerator.AddEnumTypeToGenerate(nonNullableType);
                            }
                            // If this is not an enum or nullable<enum> and we're not generating the complex type, determine whether this
                            // property type is visible to the client.  If it is not, log a warning.
                            else if (!this.ComplexTypes.Contains(type))
                            {
                                // "Don't know" counts as "no"
                                CodeMemberShareKind enumShareKind = this.ClientCodeGenerator.GetTypeShareKind(nonNullableType);
                                if ((enumShareKind & CodeMemberShareKind.Shared) == 0)
                                {
                                    this.ClientCodeGenerator.CodeGenerationHost.LogWarning(string.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_PropertyType_Not_Shared, pd.Name, this.Type.FullName, type.FullName, this.ClientCodeGenerator.ClientProjectName));
                                    isTypeSafeToGenerate = false; // Flag error but continue to allow accumulation of additional errors.
                                }
                            }
                        }

                        if (isTypeSafeToGenerate)
                        {
                            // Generate OnMethodXxChanging/Changed partial methods.
                            this._notificationMethodList.Add(pd);
                            propertiesToGenerate.Add(pd);
                        }
                    }
                }
                else
                {
                    this.OnPropertySkipped(pd);
                }
            }
            return propertiesToGenerate;
        }

        internal virtual void OnPropertySkipped(PropertyDescriptor pd)
        {
        }

        internal virtual bool CanGenerateProperty(PropertyDescriptor propertyDescriptor)
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
                    if (!this.ClientCodeGenerator.CanExposeEnumType(enumType, out errorMessage))
                    {
                        this.ClientCodeGenerator.CodeGenerationHost.LogWarning(String.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_Property_Enum_Error, this.Type, propertyDescriptor.Name, enumType.FullName, errorMessage));
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

        internal abstract IEnumerable<Type> GetDerivedTypes();


        internal virtual bool CanGeneratePropertyIfPolymorphic(PropertyDescriptor pd)
        {
            return true;
        }

        internal virtual bool HandleNonSerializableProperty(PropertyDescriptor pd)
        {
            return false;
        }

        internal virtual bool ShouldDeclareProperty(PropertyDescriptor pd)
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

        private bool IsExcluded(PropertyDescriptor pd, AttributeCollection propertyAttributes)
        {
            // The [Exclude] attribute is a signal simply to omit this property, no matter what
            bool hasExcludeAttr = (propertyAttributes[typeof(ExcludeAttribute)] != null);
            if (hasExcludeAttr)
            {
                // If we also see an [Include], warn the user.
                if (propertyAttributes[typeof(IncludeAttribute)] != null)
                {
                    this.ClientCodeGenerator.CodeGenerationHost.LogWarning(String.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_Cannot_Have_Include_And_Exclude, pd.Name, this.Type));
                }
            }

            return hasExcludeAttr;
        }

        internal virtual bool IsPropertyShared(PropertyDescriptor pd)
        {
            // If this property is visible to the client already because of partial types,
            // do not generate it again, or we will get a compile error
            CodeMemberShareKind shareKind = this.ClientCodeGenerator.GetPropertyShareKind(this.Type, pd.Name);
            return ((shareKind & CodeMemberShareKind.Shared) != 0);
        }
    }
}