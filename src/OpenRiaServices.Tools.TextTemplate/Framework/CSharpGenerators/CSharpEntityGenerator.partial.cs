namespace OpenRiaServices.Tools.TextTemplate.CSharpGenerators
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;
    using OpenRiaServices;
    using OpenRiaServices.Server;

    /// <summary>
    /// C# generator for entity types.
    /// </summary>
    public partial class CSharpEntityGenerator
    {
        /// <summary>
        /// Generates entity class in C#.
        /// </summary>
        /// <returns>Generated entity class code.</returns>
        protected override string GenerateDataContractProxy()
        {
            return this.TransformText();
        }

        private void Generate()
        {
            this.Initialize();
            this.GenerateEntityClass();
        }

        private void GenerateEntityClass()
        {
            this.GenerateNamespace();
            this.GenerateOpeningBrace();

            this.GenerateClassDeclaration();
            this.GenerateOpeningBrace();

            this.GenerateBody();

            this.GenerateClosingBrace();
            this.GenerateClosingBrace();
        }

        /// <summary>
        /// Generates entity class body.
        /// </summary>
        /// <remarks>
        /// The default implementation of this method invokes <see cref="GenerateConstructor"/>,
        /// <see cref="GenerateProperties"/>, <see cref="GenerateExtensibilityMethods"/> and <see cref="GenerateCustomMethods"/>.
        /// </remarks>
        protected virtual void GenerateBody()
        {
            this.GenerateConstructor();
            this.GenerateProperties();
            this.GenerateExtensibilityMethods();
            this.GenerateCustomMethods();

            // This is a hook to special case the User type that we define.
            if (this.IsUserType)
            {
                this.GenerateAdditionalUserCode();
            }
        }

        /// <summary>
        /// Generates entity properties.
        /// </summary>
        protected virtual void GenerateProperties()
        {
            this.GeneratePropertiesInternal();
            this.GenerateAssociations();
        }

        private void GenerateAssociations()
        {
            foreach (PropertyDescriptor pd in this.AssociationProperties)
            {
                this.GenerateAssociation(pd);
            }
        }

        /// <summary>
        /// Generates association property.
        /// </summary>
        /// <param name="pd">PropertyDescriptor for the association property to be generated.</param>
        protected virtual void GenerateAssociation(PropertyDescriptor pd)
        {
            if (!IsCollectionType(pd.PropertyType))
            {
                this.GenerateSingletonAssociation(pd);
            }
            else
            {
                this.GenerateCollectionSideAssociation(pd);
            }
        }

        /// <summary>
        /// Generates extensibility methods.
        /// </summary>
        protected virtual void GenerateExtensibilityMethods()
        {
            if (this.GenerateGetIdentity)
            {
                this.GenerateGetIdentityMethod();
            }

            this.GenerateNotificationMethods();
        }

        /// <summary>
        /// Generates the custom methods on the entity.
        /// </summary>
        protected virtual void GenerateCustomMethods()
        {
            IEnumerable<DomainOperationEntry> entityCustomMethods = this.GetEntityCustomMethods();
            foreach (DomainOperationEntry methodEntry in entityCustomMethods.OrderBy(e => e.Name))
            {
                this.GenerateCustomMethod(methodEntry);
                this.GenerateCustomMethodProperties(methodEntry);
            }
        }
    }

    internal class AssociationMetadata
    {
        public PropertyDescriptor PropertyDescriptor { get; private set; }
        public bool IsExternal { get; private set; }
        public bool IsCollection { get; private set; }
        public string PropTypeName { get; private set; }
        public string AssociationTypeName { get; private set; }
        public string PropertyName { get; private set; }
        public string FieldName { get; private set; }
        public IEnumerable<Attribute> Attributes { get; private set; }
        public AssociationAttribute AssociationAttribute { get; private set; }

        public AssociationMetadata(PropertyDescriptor pd)
        {
            this.PropertyDescriptor = pd;
            AttributeCollection propertyAttributes = pd.ExplicitAttributes();
            this.AssociationAttribute = (AssociationAttribute)propertyAttributes[typeof(AssociationAttribute)];
            this.IsExternal = propertyAttributes[typeof(ExternalReferenceAttribute)] != null;
            this.IsCollection = EntityGenerator.IsCollectionType(pd.PropertyType);

            if (!this.IsCollection)
            {
                this.PropTypeName = CodeGenUtilities.GetTypeName(pd.PropertyType);
                this.AssociationTypeName = @"OpenRiaServices.Client.EntityRef<" + this.PropTypeName + ">";
                this.Attributes = propertyAttributes.Cast<Attribute>().Where(a => a.GetType() != typeof(DataMemberAttribute));
            }
            else
            {
                this.PropTypeName = CodeGenUtilities.GetTypeName(TypeUtility.GetElementType(pd.PropertyType));
                this.AssociationTypeName = "OpenRiaServices.Client.EntityCollection<" + this.PropTypeName + ">";

                List<Attribute> attributeList = propertyAttributes.Cast<Attribute>().ToList();
                ReadOnlyAttribute readOnlyAttr = propertyAttributes.OfType<ReadOnlyAttribute>().SingleOrDefault();
                if (readOnlyAttr != null && !propertyAttributes.OfType<EditableAttribute>().Any())
                {
                    attributeList.Add(new EditableAttribute(!readOnlyAttr.IsReadOnly));
                }
                this.Attributes = attributeList.Where(a => a.GetType() != typeof(DataMemberAttribute));
            }

            this.PropertyName = CodeGenUtilities.GetSafeName(pd.Name);
            this.FieldName = CodeGenUtilities.MakeCompliantFieldName(this.PropertyName);
        }
    }
}

