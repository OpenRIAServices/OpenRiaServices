using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using OpenRiaServices.Server;

namespace OpenRiaServices.Tools
{
    /// <summary>
    /// Proxy generator for an entity.
    /// </summary>
    internal sealed class EntityProxyGenerator : DataContractProxyGenerator
    {
        private readonly DomainServiceDescriptionAggregate _domainServiceDescriptionAggregate;
        private readonly ICollection<DomainServiceDescription> _allDomainServiceDescriptions;

        private bool _generateGetIdentity;
        readonly List<PropertyDescriptor> _keyProperties;
        private readonly Type _visibleBaseType;

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityProxyGenerator"/> class.
        /// </summary>
        /// <param name="proxyGenerator">The client proxy generator against which this will generate code.  Cannot be null.</param>
        /// <param name="entityType">The type of the entity.  Cannot be null.</param>
        /// <param name="allDomainServiceDescriptions">Collection of all <see cref="DomainServiceDescription"/> defined in this project</param>
        /// <param name="typeMapping">A dictionary of <see cref="DomainService"/> and related entity types that maps to their corresponding client-side <see cref="CodeTypeReference"/> representations.</param>
        public EntityProxyGenerator(CodeDomClientCodeGenerator proxyGenerator, Type entityType, ICollection<DomainServiceDescription> allDomainServiceDescriptions, IDictionary<Type, CodeTypeDeclaration> typeMapping)
            : base(proxyGenerator, entityType, typeMapping)
        {
            this._domainServiceDescriptionAggregate = new DomainServiceDescriptionAggregate(allDomainServiceDescriptions.Where(dsd => dsd.EntityTypes.Contains(entityType)));
            this._allDomainServiceDescriptions = allDomainServiceDescriptions;

            // Determine this entity's logical (visible) base type based on the type hierarchy
            // and list of known types.  Null meant it was already the root.
            this._visibleBaseType = this._domainServiceDescriptionAggregate.GetEntityBaseType(this.Type);

            this._generateGetIdentity = !this.IsDerivedType;
            this._keyProperties = new List<PropertyDescriptor>();
        }

        protected override IEnumerable<Type> ComplexTypes
        {
            get
            {
                return this._domainServiceDescriptionAggregate.ComplexTypes;
            }
        }

        protected override bool IsDerivedType
        {
            get
            {
                return this._visibleBaseType != null;
            }
        }

        /// <summary>
        /// Generates the client proxy code for an entity
        /// </summary>
        public override void Generate()
        {
            // Shared root verification bails out of code gen - if it fails, error has been logged.
            if (!this.VerifySharedEntityRoot(this.Type))
            {
                return;
            }

            base.Generate();
        }

        protected override void AddBaseTypes(CodeNamespace ns)
        {
            if (this._visibleBaseType != null)
            {
                this.ProxyClass.BaseTypes.Add(CodeGenUtilities.GetTypeReference(this._visibleBaseType, this.ClientProxyGenerator, this.ProxyClass));
            }
            else
            {
                this.ProxyClass.BaseTypes.Add(CodeGenUtilities.GetTypeReference(TypeConstants.EntityTypeFullName, ns.Name, false));
            }
        }

        protected override bool CanGenerateProperty(PropertyDescriptor propertyDescriptor)
        {
            // Check if it is an excluded property
            if (propertyDescriptor.Attributes[typeof(ExcludeAttribute)] != null)
            {
                return false;
            }

            // Check if it is an external reference.
            if (propertyDescriptor.Attributes[typeof(ExternalReferenceAttribute)] != null)
            {
                return true;
            }

            // The base can't generate the property, it could be an association which we know how to generate.
            if (!base.CanGenerateProperty(propertyDescriptor))
            {
                AttributeCollection propertyAttributes = propertyDescriptor.ExplicitAttributes();
                bool hasKeyAttr = (propertyAttributes[typeof(KeyAttribute)] != null);

                // If we can't generate Key property, log a VS error (this will cancel CodeGen effectively)
                if (hasKeyAttr)
                {
                    Debug.Assert(TypeUtility.IsPredefinedSimpleType(propertyDescriptor.PropertyType), "At this point in code gen, the key must have undergone a simple type check.");

                    // Property must not be serializable based on attributes (e.g. no DataMember), because 
                    // we already checked its type which was fine.
                    this.ClientProxyGenerator.LogError(string.Format(
                        CultureInfo.CurrentCulture,
                        Resource.EntityCodeGen_EntityKey_PropertyNotSerializable,
                        this.Type, propertyDescriptor.Name));

                    return false;
                }

                // Get the implied element type (e.g. int32[], Nullable<int32>, IEnumerable<int32>)
                // If the ultimate element type is not allowed, it's not acceptable, no matter whether
                // this is an array, Nullable<T> or whatever
                Type elementType = TypeUtility.GetElementType(propertyDescriptor.PropertyType);
                if (!this._domainServiceDescriptionAggregate.EntityTypes.Contains(elementType) || (propertyDescriptor.Attributes[typeof(AssociationAttribute)] == null))
                {
                    // If the base class says we can't generate the property, it is because the property is not serializable.
                    // The only other type entity would serialize is associations. Since it is not, return now.
                    return false;
                }
            }

            // Ensure the property is not virtual, abstract or new
            // If there is a violation, we log the error and keep
            // running to accumulate all such errors.  This function
            // may return an "okay" for non-error case polymorphics.
            if (!this.CanGeneratePropertyIfPolymorphic(propertyDescriptor))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Determines whether the specified property belonging to the
        /// current entity is polymorphic, and if so whether it is legal to generate it.
        /// </summary>
        /// <param name="pd">The property to validate</param>
        /// <returns><c>true</c> if it is not polymorphic or it is legal to generate it, else <c>false</c></returns>
        protected override bool CanGeneratePropertyIfPolymorphic(PropertyDescriptor pd)
        {
            PropertyInfo propertyInfo = this.Type.GetProperty(pd.Name);

            if (propertyInfo != null)
            {
                if (this.IsMethodPolymorphic(propertyInfo.GetGetMethod()) ||
                    this.IsMethodPolymorphic(propertyInfo.GetSetMethod()))
                {
                    // This property is polymorphic.  To determine whether it is
                    // legal to generate, we determine whether any of our visible
                    // base types also expose this property.  If so, we cannot generate it.
                    foreach (Type baseType in this.GetVisibleBaseTypes(this.Type))
                    {
                        if (baseType.GetProperty(pd.Name) != null)
                        {
                            return false;
                        }
                    }

                    // If get here, we have not generated an entity in the hierarchy between the
                    // current entity type and the entity that declared this.  This means it is
                    // save to generate
                    return true;
                }
            }
            return true;
        }

        protected override void GenerateAdditionalMembers()
        {
            // ----------------------------------------------------------------
            // gen: public override object GetIdentity()...
            // ----------------------------------------------------------------
            if (this._generateGetIdentity)
            {
                this.GenGetIdentityMethod(this.ProxyClass, this._keyProperties);
            }

            // ----------------------------------------------------------------
            // generate the domain methods
            // ----------------------------------------------------------------
            CustomMethodProxyGenerator methodProxyGenerator = new CustomMethodProxyGenerator(this.ClientProxyGenerator, this.ProxyClass, this.Type, this._allDomainServiceDescriptions, this.NotificationMethodGen);
            methodProxyGenerator.Generate();

            base.GenerateAdditionalMembers();
        }

        protected override bool GenerateNonSerializableProperty(PropertyDescriptor propertyDescriptor)
        {
            AttributeCollection propertyAttributes = propertyDescriptor.ExplicitAttributes();
            AssociationAttribute associationAttr = (AssociationAttribute)propertyAttributes[typeof(AssociationAttribute)];
            bool externalReference = propertyAttributes[typeof(ExternalReferenceAttribute)] != null;

            if (associationAttr != null)
            {
                // generate Association members for members marked Association and of a Type that is exposed by the provider
                this.GenEntityAssocationProperty(this.ProxyClass, propertyDescriptor, associationAttr, externalReference);
                return true;
            }

            return false;
        }

        protected override void GenerateProperty(PropertyDescriptor propertyDescriptor)
        {
            base.GenerateProperty(propertyDescriptor);

            AttributeCollection propertyAttributes = propertyDescriptor.ExplicitAttributes();
            bool hasKeyAttr = (propertyAttributes[typeof(KeyAttribute)] != null);
            if (hasKeyAttr)
            {
                this._keyProperties.Add(propertyDescriptor);
            }
        }

        protected override IEnumerable<Type> GetDerivedTypes()
        {
            return this._domainServiceDescriptionAggregate.EntityTypes.Where(t => t != this.Type && this.Type.IsAssignableFrom(t));
        }

        /// <summary>
        /// Generates the summary comment for the entity class taking into account whether or not the entity is shared.
        /// </summary>
        /// <returns>Returns the summary comment content.</returns>
        protected override string GetSummaryComment()
        {
            string comment = string.Format(CultureInfo.CurrentCulture, Resource.CodeGen_Entity_Class_Summary_Comment, this.Type.Name);

            // The entity is not shared, a simple comment is sufficient.
            if (!this._domainServiceDescriptionAggregate.IsShared)
            {
                return comment;
            }

            // Add an additional comment that indicates the entity is shared.
            string sharedComment = string.Format(CultureInfo.CurrentCulture, Resource.CodeGen_Entity_Class_Shared_Summary_Comment);
            comment += Environment.NewLine + sharedComment;

            // Add which contexts the entity is exposed from.
            foreach (var domainServiceDescription in this._domainServiceDescriptionAggregate.DomainServiceDescriptions)
            {
                string domainContextComment = string.Format(CultureInfo.CurrentCulture, Resource.CodeGen_Entity_Class_Context_Summary_Comment, DomainServiceProxyGenerator.DomainContextTypeName(domainServiceDescription));
                comment += Environment.NewLine + domainContextComment;
            }

            return comment;
        }

        protected override bool IsPropertyShared(PropertyDescriptor pd)
        {
            if (base.IsPropertyShared(pd))
            {
                if (pd.ExplicitAttributes()[typeof(KeyAttribute)] != null)
                {
                    // If there are any shared key members, don't generate GetIdentity method. 
                    // Default implementation of GetIdentity on the Entity class will be used.
                    this._generateGetIdentity = false;
                }
                return true;
            }

            return false;
        }

        protected override void OnPropertySkipped(PropertyDescriptor pd)
        {
            AttributeCollection propertyAttributes = pd.ExplicitAttributes();
            bool hasKeyAttr = (propertyAttributes[typeof(KeyAttribute)] != null);

            // If we can't generate Key property, log a VS error (this will cancel CodeGen effectively)
            if (hasKeyAttr)
            {
                // Property must not be serializable based on attributes (e.g. no DataMember), because 
                // we already checked its type which was fine.
                this.ClientProxyGenerator.LogError(string.Format(
                    CultureInfo.CurrentCulture,
                    Resource.EntityCodeGen_EntityKey_PropertyNotSerializable,
                    this.Type, pd.Name));
            }
        }

        // Entities should not declare properties if the properties belong on base classes
        // or a key attribute is not a simple type.
        protected override bool ShouldDeclareProperty(PropertyDescriptor pd)
        {
            if (!base.ShouldDeclareProperty(pd))
            {
                return false;
            }

            // Inheritance: when dealing with derived entities, we need to
            // avoid generating a property already on the base.  But we also
            // need to account for flattening (holes in the exposed hiearchy.
            // This helper method encapsulates that logic.
            if (!this.ShouldFlattenProperty(pd))
            {
                return false;
            }

            AttributeCollection propertyAttributes = pd.ExplicitAttributes();
            Type propertyType = pd.PropertyType;

            // The [Key] attribute means this property is part of entity key
            bool hasKeyAttr = (propertyAttributes[typeof(KeyAttribute)] != null);

            if (hasKeyAttr)
            {
                if (!TypeUtility.IsPredefinedSimpleType(propertyType))
                {
                    this.ClientProxyGenerator.LogError(string.Format(
                        CultureInfo.CurrentCulture,
                        Resource.EntityCodeGen_EntityKey_KeyTypeNotSupported,
                        this.Type, pd.Name, propertyType));
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks if the TypeLevel attributes on the Entity are valid. Currently only checks
        /// if RTO is on the least derived exposed Entity and logs an error otherwise.
        /// </summary>
        /// <param name="typeAttributes">The collection of attributes on the type.</param>
        protected override void ValidateTypeAttributes(AttributeCollection typeAttributes)
        {
            // Check if RoundtripOriginalAttribute is present on the least derived exposed entity type.
            if (typeAttributes[typeof(RoundtripOriginalAttribute)] != null)
            {
                Type rootEntityType = this._domainServiceDescriptionAggregate.GetRootEntityType(this.Type);
                if (rootEntityType.Attributes()[typeof(RoundtripOriginalAttribute)] == null)
                {
                    // Base type does not have RTO but derived type does, so log an error.
                    this.ClientProxyGenerator.LogError(string.Format(CultureInfo.CurrentCulture, Resource.EntityCodeGen_RoundtripOriginalOnBaseType, this.Type, rootEntityType));
                }
            }
        }
        
        private static bool IsCollectionType(Type t)
        {
            return typeof(IEnumerable).IsAssignableFrom(t);
        }

        /// <summary>
        /// For the specified association property, get the association member on the other side of the
        /// association.  For example, given PurchaseOrderDetail.Product, will return Product.PurchaseOrderDetails.
        /// </summary>
        /// <param name="propertyDescriptor">The assocation property <see cref="PropertyDescriptor"/>.</param>
        /// <param name="assocAttrib">The association property <see cref="AssociationAttribute"/>.</param>
        /// <returns>The association member on the other side of the specified association property.</returns>
        private static PropertyDescriptor GetReverseAssociation(PropertyDescriptor propertyDescriptor, AssociationAttribute assocAttrib)
        {
            Type otherType = TypeUtility.GetElementType(propertyDescriptor.PropertyType);

            foreach (PropertyDescriptor entityMember in TypeDescriptor.GetProperties(otherType))
            {
                if (entityMember.Name == propertyDescriptor.Name)
                {
                    // for self associations, both ends of the association are in
                    // the same class and have the same name. Therefore, we need to 
                    // skip the member itself.
                    continue;
                }

                AssociationAttribute otherAssocAttrib = entityMember.Attributes[typeof(AssociationAttribute)] as AssociationAttribute;
                if (otherAssocAttrib != null && otherAssocAttrib.Name == assocAttrib.Name)
                {
                    return entityMember;
                }
            }

            return null;
        }

        /// <summary>
        /// Determines whether the given <paramref name="propertyDescriptor"/>
        /// should be declared by the current entity type.
        /// </summary>
        /// <remarks>
        /// This method contains the basic 'flattening' logic for properties
        /// in the fact of entity inheritance.  Holes in the hierarchy due to
        /// the user not using <see cref="KnownTypeAttribute"/> requires us to
        /// lift properties to their nearest visible derived type.
        /// </remarks>
        /// <param name="propertyDescriptor">The property descriptor to test</param>
        /// <returns><c>true</c> if the current entity is the correct place in the
        /// hierarchy for this property to be declared.</returns>
        private bool ShouldFlattenProperty(PropertyDescriptor propertyDescriptor)
        {
            Type declaringType = propertyDescriptor.ComponentType;

            // If this property is declared by the current entity type,
            // the answer is always 'yes'
            if (declaringType == this.Type)
            {
                return true;
            }

            // If this is a projection property (meaning it is declared outside this hierarchy),
            // then the answer is "yes, declare it"
            if (!declaringType.IsAssignableFrom(this.Type))
            {
                return true;
            }

            // If it is declared in any visible entity type, the answer is 'no'
            // because it will be generated with that entity
            if (this._domainServiceDescriptionAggregate.EntityTypes.Contains(declaringType))
            {
                return false;
            }

            // This property is defined in an entity that is not among the known types.
            // We may need to lift it during code generation.  But beware -- if there
            // are multiple gaps in the hierarchy, we want to lift it only if some other
            // visible base type of ours has not already done so.
            Type baseType = this.Type.BaseType;

            while (baseType != null)
            {
                // If we find the declaring type, we know from the test above it is
                // not a known entity type.  The first such non-known type is grounds
                // for us lifting its properties.
                if (baseType == declaringType)
                {
                    return true;
                }

                // The first known type we encounter walking toward the base type
                // will generate it, so we must not.
                if (this._domainServiceDescriptionAggregate.EntityTypes.Contains(baseType))
                {
                    break;
                }

                // Note: we explicitly allow this walkback to cross past
                // the visible root, examining types lower than our root.
                baseType = baseType.BaseType;
            }
            return false;
        }

        private void GenerateCollectionSideAssociation(CodeTypeDeclaration proxyClass, PropertyDescriptor pd, AssociationAttribute associationAttribute, bool isExternal)
        {
            CodeTypeReference propType = CodeGenUtilities.GetTypeReference(TypeConstants.EntityCollectionTypeFullName, this.Type.Namespace, false);
            CodeTypeReference fldType;
            Type elementType = TypeUtility.GetElementType(pd.PropertyType);

            if (isExternal)
            {
                CodeTypeReference externalTypeRef = new CodeTypeReference(elementType.FullName);
                propType.TypeArguments.Add(externalTypeRef);

                // Note: we set the global flag *after* adding as a TypeArgument
                //       to override the GenericTypeArgument flag.  This is required
                //       for proper global references.
                externalTypeRef.Options = CodeTypeReferenceOptions.GlobalReference;
            }
            else
            {
                propType.TypeArguments.Add(
                    CodeGenUtilities.GetTypeReference(
                        elementType,
                        this.ClientProxyGenerator,
                        proxyClass));
            }

            fldType = propType;

            CodeMemberField fld = new CodeMemberField();
            fld.Attributes = MemberAttributes.Private;
            fld.Name = CodeGenUtilities.MakeCompliantFieldName(pd.Name);
            fld.Type = fldType;
            proxyClass.Members.Add(fld);

            // --------------------------
            // Generate the filter method
            // --------------------------
            // private bool filter_PurchaseOrderDetails(PurchaseOrderDetail entity) {
            //     return entity.ProductID == ProductID;
            // }
            string[] thisKeyProps = associationAttribute.ThisKeyMembers.ToArray();
            string[] otherKeyProps = associationAttribute.OtherKeyMembers.ToArray();
            CodeMemberMethod filterMethod = this.GenerateFilterMethod(proxyClass, pd.Name, elementType, thisKeyProps, otherKeyProps, isExternal);

            CodeMemberProperty prop = new CodeMemberProperty();
            prop.Name = pd.Name;
            prop.Attributes = MemberAttributes.Public | MemberAttributes.Final;
            prop.Type = propType;
            prop.HasGet = true;

            // Generate <summary> comment for property
            string comment = string.Format(CultureInfo.CurrentCulture, Resource.CodeGen_Entity_Collection_Association_Property_Summary_Comment, elementType.Name);
            prop.Comments.AddRange(CodeGenUtilities.GenerateSummaryCodeComment(comment, this.ClientProxyGenerator.IsCSharp));

            // ----------------------------------------------------------------
            // Propagate the custom attributes (except DataMember)
            // ----------------------------------------------------------------
            List<Attribute> propertyAttributes = pd.ExplicitAttributes().Cast<Attribute>().ToList();

            // Here, we check for the existence of [ReadOnly(true)] attributes generated when
            // the property does not not have a setter.  We want to inject an [Editable(false)]
            // attribute into the pipeline.
            ReadOnlyAttribute readOnlyAttr = propertyAttributes.OfType<ReadOnlyAttribute>().SingleOrDefault();
            if (readOnlyAttr != null && !propertyAttributes.OfType<EditableAttribute>().Any())
            {
                propertyAttributes.Add(new EditableAttribute(!readOnlyAttr.IsReadOnly));

                // REVIEW:  should we strip out [ReadOnly] attributes here?
            }

            CustomAttributeGenerator.GenerateCustomAttributes(
                this.ClientProxyGenerator,
                proxyClass,
                ex => string.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_Attribute_ThrewException_CodeTypeMember, ex.Message, prop.Name, proxyClass.Name, ex.InnerException.Message),
                propertyAttributes.Where(a => a.GetType() != typeof(DataMemberAttribute)),
                prop.CustomAttributes,
                prop.Comments);

            // Generate "if (fld == null)" test for common use below
            CodeExpression isRefNullExpr =
                CodeGenUtilities.MakeEqualToNull(
                    new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fld.Name));

            // Generate a delegate style invoke of our filter method for common use below
            CodeExpression filterDelegate =
                CodeGenUtilities.MakeDelegateCreateExpression(this.ClientProxyGenerator.IsCSharp, new CodeTypeReference("System.Func"), filterMethod.Name);

            // only generate attach and detach when the relation is two-way
            PropertyDescriptor reverseAssociationMember = GetReverseAssociation(pd, associationAttribute);
            bool isBiDirectionalAssociation = (reverseAssociationMember != null) && this.CanGenerateProperty(reverseAssociationMember);
            if (isBiDirectionalAssociation)
            {
                if (IsCollectionType(pd.PropertyType))
                {
                    CodeMemberMethod attach = this.GenerateAttach(proxyClass, elementType, associationAttribute, pd);
                    CodeMemberMethod detach = this.GenerateDetach(proxyClass, elementType, associationAttribute, pd);

                    //// generate : 
                    //// if (_PurchaseOrderDetails == null) {
                    ////    _PurchaseOrderDetails = new EntityCollection<PurchaseOrderDetail>(this, filter_PurchaseOrderDetails, attach_PurchaseOrderDetails, detach_PurchaseOrderDetails);
                    //// }

                    CodeExpression attachDelegate =
                        CodeGenUtilities.MakeDelegateCreateExpression(this.ClientProxyGenerator.IsCSharp, new CodeTypeReference("System.Action"), attach.Name);

                    CodeExpression detachDelegate =
                        CodeGenUtilities.MakeDelegateCreateExpression(this.ClientProxyGenerator.IsCSharp, new CodeTypeReference("System.Action"), detach.Name);


                    CodeAssignStatement initExpr = new CodeAssignStatement(
                        new CodeFieldReferenceExpression(
                            new CodeThisReferenceExpression(), fld.Name),
                            new CodeObjectCreateExpression(
                                propType,
                                new CodeThisReferenceExpression(),
                                new CodePrimitiveExpression(prop.Name),
                                filterDelegate,
                                attachDelegate,
                                detachDelegate));
                    prop.GetStatements.Add(new CodeConditionStatement(isRefNullExpr, initExpr));
                }
            }
            else
            {
                CodeAssignStatement initExpr = new CodeAssignStatement(
                        new CodeFieldReferenceExpression(
                            new CodeThisReferenceExpression(), fld.Name),
                            new CodeObjectCreateExpression(
                                propType,
                                new CodeThisReferenceExpression(),
                                new CodePrimitiveExpression(prop.Name),
                                filterDelegate));
                prop.GetStatements.Add(new CodeConditionStatement(isRefNullExpr, initExpr));
            }

            prop.GetStatements.Add(
                new CodeMethodReturnStatement(
                    new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fld.Name)));

            proxyClass.Members.Add(prop);
            proxyClass.Members.Add(filterMethod);
        }

        private CodeMemberMethod GenerateAttach(CodeTypeDeclaration proxyClass, Type entityType, AssociationAttribute assoc, PropertyDescriptor pd)
        {
            // attach method
            CodeMemberMethod method = new CodeMemberMethod();
            method.Name = "Attach" + pd.Name;
            method.Attributes = MemberAttributes.Private;
            method.ReturnType = CodeGenUtilities.GetTypeReference(typeof(void), this.ClientProxyGenerator, proxyClass);
            CodeParameterDeclarationExpression pdef = new CodeParameterDeclarationExpression(
                                                        CodeGenUtilities.GetTypeReference(entityType, this.ClientProxyGenerator, proxyClass),
                                                        "entity");
            method.Parameters.Add(pdef);

            CodeVariableReferenceExpression pref = new CodeVariableReferenceExpression(pdef.Name);
            PropertyDescriptor reverseAssociationMember = GetReverseAssociation(pd, assoc);
            string revName = reverseAssociationMember.Name;

            if (!IsCollectionType(pd.PropertyType))
            {
                // entity.Prop.Add(this)
                method.Statements.Add(
                    new CodeExpressionStatement(
                        new CodeMethodInvokeExpression(
                            new CodePropertyReferenceExpression(pref, revName),
                            "Add",
                            new CodeThisReferenceExpression())));
            }
            else
            {
                // pref.Prop = this
                method.Statements.Add(
                    new CodeAssignStatement(
                        new CodePropertyReferenceExpression(pref, revName),
                        new CodeThisReferenceExpression()));
            }

            proxyClass.Members.Add(method);

            return method;
        }

        private CodeMemberMethod GenerateDetach(CodeTypeDeclaration proxyClass, Type entityType, AssociationAttribute assoc, PropertyDescriptor pd)
        {
            // detach method
            CodeMemberMethod method = new CodeMemberMethod();
            method.Name = "Detach" + pd.Name;
            method.Attributes = MemberAttributes.Private;
            method.ReturnType = CodeGenUtilities.GetTypeReference(typeof(void), this.ClientProxyGenerator, proxyClass);

            CodeParameterDeclarationExpression pdef = new CodeParameterDeclarationExpression(
                                                        CodeGenUtilities.GetTypeReference(entityType, this.ClientProxyGenerator, proxyClass),
                                                        "entity");
            method.Parameters.Add(pdef);

            CodeVariableReferenceExpression pref = new CodeVariableReferenceExpression(pdef.Name);
            PropertyDescriptor reverseAssociationMember = GetReverseAssociation(pd, assoc);
            string revName = reverseAssociationMember.Name;

            if (!IsCollectionType(pd.PropertyType))
            {
                // pref.Prop.Remove(this)
                method.Statements.Add(
                    new CodeExpressionStatement(
                        new CodeMethodInvokeExpression(
                            new CodePropertyReferenceExpression(pref, revName),
                            "Remove",
                            new CodeThisReferenceExpression())));
            }
            else
            {
                // pref.Prop = null
                method.Statements.Add(
                    new CodeAssignStatement(
                        new CodePropertyReferenceExpression(pref, revName),
                        new CodePrimitiveExpression(null)));
            }

            proxyClass.Members.Add(method);

            return method;
        }

        private CodeMemberMethod GenerateFilterMethod(CodeTypeDeclaration proxyClass, string targetName, Type entityType, string[] thisKeyProps, string[] otherKeyProps, bool isExternal)
        {
            CodeMemberMethod filterMethod = new CodeMemberMethod();
            filterMethod.Name = "Filter" + targetName;
            filterMethod.Attributes = MemberAttributes.Private;
            filterMethod.ReturnType = CodeGenUtilities.GetTypeReference(typeof(bool), this.ClientProxyGenerator, proxyClass);
            filterMethod.Parameters.Add(
                new CodeParameterDeclarationExpression(
                    isExternal ?
                        new CodeTypeReference(entityType.FullName) { Options = CodeTypeReferenceOptions.GlobalReference } :
                        CodeGenUtilities.GetTypeReference(entityType, this.ClientProxyGenerator, proxyClass),
                "entity"));

            CodeExpression filterPredicate = null;
            for (int i = 0; i < thisKeyProps.Length; i++)
            {
                CodeExpression currExpr;
                if (this.ClientProxyGenerator.IsCSharp)
                {
                    currExpr = new CodeBinaryOperatorExpression(
                        new CodePropertyReferenceExpression(new CodeVariableReferenceExpression("entity"), otherKeyProps[i]),
                        CodeBinaryOperatorType.ValueEquality,
                        new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), thisKeyProps[i]));
                }
                else
                {
                    // In VB.NET you can't do x == y for nullable values like you can in C#.
                    currExpr = CodeGenUtilities.MakeEqual(
                            typeof(object),
                            new CodePropertyReferenceExpression(new CodeVariableReferenceExpression("entity"), otherKeyProps[i]),
                            new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), thisKeyProps[i]),
                            this.ClientProxyGenerator.IsCSharp);
                }
                if (filterPredicate == null)
                {
                    filterPredicate = currExpr;
                }
                else
                {
                    filterPredicate = new CodeBinaryOperatorExpression(
                        filterPredicate,
                        CodeBinaryOperatorType.BooleanAnd,
                        currExpr);
                }
            }
            filterMethod.Statements.Add(new CodeMethodReturnStatement(filterPredicate));
            return filterMethod;
        }

        private void GenEntityAssocationProperty(CodeTypeDeclaration proxyClass, PropertyDescriptor pd, AssociationAttribute associationAttribute, bool isExternal)
        {
            // Register property types to prevent conflicts
            Type associationType =
                IsCollectionType(pd.PropertyType) ?
                    TypeUtility.GetElementType(pd.PropertyType) :
                    pd.PropertyType;

            // Check if we're in conflict
            if (!CodeGenUtilities.RegisterTypeName(associationType, this.Type.Namespace))
            {
                // Aggressively check for potential conflicts across other DomainService entity types.
                IEnumerable<Type> potentialConflicts =
                    this.ClientProxyGenerator.DomainServiceDescriptions
                        .SelectMany<DomainServiceDescription, Type>(dsd => dsd.EntityTypes)
                            .Where(entity => entity.Namespace == associationType.Namespace).Distinct();

                foreach (Type potentialConflict in potentialConflicts)
                {
                    // Check if we plan to include any types from this potential conflict's namespace
                    CodeGenUtilities.RegisterTypeName(potentialConflict, this.Type.Namespace);
                }
            }

            if (!IsCollectionType(pd.PropertyType))
            {
                this.GenerateSingletonAssociation(proxyClass, pd, associationAttribute, isExternal);
            }
            else
            {
                this.GenerateCollectionSideAssociation(proxyClass, pd, associationAttribute, isExternal);
            }
        }

        private void GenerateSingletonAssociation(CodeTypeDeclaration proxyClass, PropertyDescriptor pd, AssociationAttribute associationAttribute, bool isExternal)
        {
            CodeTypeReference propType =
                isExternal ?
                    new CodeTypeReference(pd.PropertyType.FullName) { Options = CodeTypeReferenceOptions.GlobalReference } :
                    CodeGenUtilities.GetTypeReference(pd.PropertyType, this.ClientProxyGenerator, proxyClass);

            // generate field:
            // private EntityRef<Product> _Product;
            CodeTypeReference fldType = CodeGenUtilities.GetTypeReference(TypeConstants.EntityRefTypeFullName, this.Type.Namespace, false);
            fldType.TypeArguments.Add(propType);

            CodeMemberField fld = new CodeMemberField();
            fld.Attributes = MemberAttributes.Private;
            fld.Name = CodeGenUtilities.MakeCompliantFieldName(pd.Name);
            fld.Type = fldType;
            proxyClass.Members.Add(fld);

            // generate property:
            // public Product Product { get {...} set {...} }
            CodeMemberProperty prop = new CodeMemberProperty();
            prop.Attributes = MemberAttributes.Public | MemberAttributes.Final;
            prop.Name = pd.Name;
            prop.Type = propType;
            prop.HasGet = true;
            prop.HasSet = true;

            // For the FK side of external associations, we generate a "reduced" setter that
            // does validation and synchronizes FK members
            if (isExternal && !associationAttribute.IsForeignKey)
            {
                prop.HasSet = false;
            }

            // Generate <summary> comment for property
            string format = prop.HasSet
                            ? Resource.CodeGen_Entity_Singleton_Association_Property_Summary_Comment 
                            : Resource.CodeGen_Entity_Singleton_Association_ReadOnly_Property_Summary_Comment;
            string comment = string.Format(CultureInfo.CurrentCulture, format, pd.PropertyType.Name);
            prop.Comments.AddRange(CodeGenUtilities.GenerateSummaryCodeComment(comment, this.ClientProxyGenerator.IsCSharp));

            // ----------------------------------------------------------------
            // Propagate the custom attributes (except DataMember)
            // ----------------------------------------------------------------
            AttributeCollection propertyAttributes = pd.ExplicitAttributes();
            CustomAttributeGenerator.GenerateCustomAttributes(
                this.ClientProxyGenerator,
                proxyClass,
                ex => string.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_Attribute_ThrewException_CodeTypeMember, ex.Message, prop.Name, proxyClass.Name, ex.InnerException.Message),
                propertyAttributes.Cast<Attribute>().Where(a => a.GetType() != typeof(DataMemberAttribute)),
                prop.CustomAttributes,
                prop.Comments);

            // --------------------------
            // Generate the filter method
            // --------------------------
            // private bool filter_Product(Product entity) {
            //     return entity.ProductID == ProductID;
            // }
            string[] thisKeyProps = associationAttribute.ThisKeyMembers.ToArray();
            string[] otherKeyProps = associationAttribute.OtherKeyMembers.ToArray();
            CodeMemberMethod filterMethod = this.GenerateFilterMethod(proxyClass, pd.Name, pd.PropertyType, thisKeyProps, otherKeyProps, isExternal);

            // --------------------------
            // Generate getter
            // --------------------------
            // generate delayed initialization
            // if (_Product == null) {
            //    _Product = new EntityRef<Product>(this, filter_Product);
            // }
            CodeExpression entityExpr = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fld.Name);
            entityExpr = new CodePropertyReferenceExpression(entityExpr, "Entity");
            CodeExpression isRefNullExpr =
                CodeGenUtilities.MakeEqualToNull(
                    new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fld.Name));

            CodeExpression filterDelegate =
                CodeGenUtilities.MakeDelegateCreateExpression(this.ClientProxyGenerator.IsCSharp, new CodeTypeReference("System.Func"), filterMethod.Name);

            CodeAssignStatement initExpr = new CodeAssignStatement(
                new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fld.Name),
                new CodeObjectCreateExpression(
                    fldType,
                    new CodeThisReferenceExpression(),
                    new CodePrimitiveExpression(prop.Name),
                    filterDelegate));
            prop.GetStatements.Add(new CodeConditionStatement(isRefNullExpr, initExpr));

            // generate : return _Product.Entity;
            prop.GetStatements.Add(new CodeMethodReturnStatement(entityExpr));

            // --------------------------
            // Generate setter
            // --------------------------
            if (prop.HasSet)
            {
                PropertyDescriptor reverseAssociationMember = GetReverseAssociation(pd, associationAttribute);

                CodeStatement detachStatement = null;
                CodeStatement attachStatement = null;
                bool reverseIsSingleton = false;

                // we need to emit back-reference fixup code if this association is bi-directional, and the other side 
                // of the association will also be generated (don't want to generate code that references non-existent
                // members)
                bool isBiDirectionalAssociation = (reverseAssociationMember != null) && this.CanGenerateProperty(reverseAssociationMember);

                if (!isExternal && isBiDirectionalAssociation)
                {
                    // currently relying on our naming convention for association names to get the name
                    // of the reverse collection property
                    string revName = reverseAssociationMember.Name;

                    reverseIsSingleton = !IsCollectionType(reverseAssociationMember.PropertyType);
                    if (!reverseIsSingleton)
                    {
                        detachStatement = new CodeExpressionStatement(
                            new CodeMethodInvokeExpression(
                                new CodePropertyReferenceExpression(
                                    new CodeVariableReferenceExpression("previous"),
                                    revName),
                                "Remove",
                                new CodeThisReferenceExpression()));
                        attachStatement = new CodeExpressionStatement(
                            new CodeMethodInvokeExpression(
                                new CodePropertyReferenceExpression(
                                    new CodePropertySetValueReferenceExpression(),
                                    revName),
                                "Add",
                                new CodeThisReferenceExpression()));
                    }
                    else
                    {
                        detachStatement = new CodeAssignStatement(
                            new CodePropertyReferenceExpression(
                                new CodeVariableReferenceExpression("previous"),
                                revName),
                                new CodePrimitiveExpression(null));
                        attachStatement = new CodeAssignStatement(
                            new CodePropertyReferenceExpression(
                                new CodePropertySetValueReferenceExpression(),
                                revName),
                                new CodeThisReferenceExpression());
                    }
                }

                // code to sync FK values from the new property value
                List<CodeStatement> statements1 = null;
                List<CodeStatement> statements2 = null;
                statements1 = new List<CodeStatement>();
                if (associationAttribute.IsForeignKey)
                {
                    // only emit FK sync code if this is a foreign key association
                    for (int i = 0; i < thisKeyProps.Length; i++)
                    {
                        statements1.Add(
                            new CodeAssignStatement(
                                new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), thisKeyProps[i]),
                                new CodePropertyReferenceExpression(new CodePropertySetValueReferenceExpression(), otherKeyProps[i])));
                    }

                    // code to set FK values to default values if the new property value is null
                    statements2 = new List<CodeStatement>();
                    for (int i = 0; i < thisKeyProps.Length; i++)
                    {
                        Type foreignKeyType = TypeDescriptor.GetProperties(this.Type).Find(thisKeyProps[i], false).PropertyType;
                        statements2.Add(
                            new CodeAssignStatement(
                                new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), thisKeyProps[i]),
                                new CodeDefaultValueExpression(
                                    CodeGenUtilities.GetTypeReference(foreignKeyType, this.ClientProxyGenerator, proxyClass))));
                    }
                }

                // if(previous != value)
                CodeExpression prevValueExpr = CodeGenUtilities.MakeNotEqual(null, new CodeVariableReferenceExpression("previous"), new CodePropertySetValueReferenceExpression(), this.ClientProxyGenerator.IsCSharp);

                // Product previous = Product;
                prop.SetStatements.Add(new CodeVariableDeclarationStatement(prop.Type, "previous", new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), pd.Name)));

                List<CodeStatement> stmts = new List<CodeStatement>();

                // Generate the validation test
                CodeStatement validationCode = GeneratePropertySetterValidation(prop.Name);
                stmts.Add(validationCode);

                if (!isExternal && isBiDirectionalAssociation)
                {
                    List<CodeStatement> detachStmts = new List<CodeStatement>();

                    // Generate : this._Product.Entity = null;
                    // This prevents infinite recursion in 1:1 association case,
                    // and in general ensures that change notifications don't get
                    // raised for the ref and fk properties during the transition
                    // time when the ref is set to null (detached) before setting
                    // to the new value.
                    detachStmts.Add(new CodeAssignStatement(entityExpr, new CodePrimitiveExpression(null)));

                    // previous.PurchaseOrderDetails.Remove(this);
                    detachStmts.Add(detachStatement);

                    // if (v != null) {
                    //    . . .
                    // }
                    stmts.Add(new CodeConditionStatement(CodeGenUtilities.MakeNotEqualToNull(new CodeVariableReferenceExpression("previous")), detachStmts.ToArray()));
                }

                // this._Product.Entity = value
                CodeAssignStatement setEntityStmt = new CodeAssignStatement(
                    entityExpr, new CodePropertySetValueReferenceExpression());

                // if (value != null)
                CodeConditionStatement stmt;
                if (associationAttribute.IsForeignKey)
                {
                    stmt = new CodeConditionStatement(
                    CodeGenUtilities.MakeNotEqualToNull(
                        new CodePropertySetValueReferenceExpression()),
                        statements1.ToArray(),
                        statements2.ToArray());

                    stmts.Add(stmt);

                    if (!isExternal)
                    {
                        // for FK sides of an association, we must set the entity
                        // reference AFTER FK member sync
                        stmts.Add(setEntityStmt);

                        // add the attach statement for bidirectional associations
                        if (isBiDirectionalAssociation)
                        {
                            stmts.Add(new CodeConditionStatement(
                                CodeGenUtilities.MakeNotEqualToNull(new CodePropertySetValueReferenceExpression()), attachStatement));
                        }
                    }
                }
                else
                {
                    stmts.Add(setEntityStmt);

                    // add the attach statement for bidirectional associations
                    if (isBiDirectionalAssociation)
                    {
                        stmts.Add(new CodeConditionStatement(
                            CodeGenUtilities.MakeNotEqualToNull(new CodePropertySetValueReferenceExpression()), attachStatement));
                    }

                    stmt = new CodeConditionStatement(
                        CodeGenUtilities.MakeNotEqualToNull(
                        new CodePropertySetValueReferenceExpression()),
                        statements1.ToArray());
                }

                if (!isExternal)
                {
                    // Generate : this.RaisePropertyChanged(<propName>);
                    stmts.Add(new CodeExpressionStatement(new CodeMethodInvokeExpression(
                        new CodeThisReferenceExpression(), "RaisePropertyChanged",
                        new CodePrimitiveExpression(prop.Name))));
                }

                prop.SetStatements.Add(new CodeConditionStatement(prevValueExpr, stmts.ToArray()));
            }

            proxyClass.Members.Add(prop);
            proxyClass.Members.Add(filterMethod);
        }

        private void GenGetIdentityMethod(CodeTypeDeclaration proxyClass, List<PropertyDescriptor> keyProperties)
        {
            // ----------------------------------------------
            // method decl:
            // public override object GetIdentity()
            // ----------------------------------------------
            CodeMemberMethod method = new CodeMemberMethod();
            method.Name = "GetIdentity";
            method.ReturnType = CodeGenUtilities.GetTypeReference(typeof(object), this.ClientProxyGenerator, proxyClass);
            method.Attributes = MemberAttributes.Public | MemberAttributes.Override;
            proxyClass.Members.Add(method);

            // Add <summary> and <returns> doc comments
            method.Comments.AddRange(CodeGenUtilities.GenerateSummaryCodeComment(Resource.CodeGen_GetIdentify_Method_Summary_Comment, this.ClientProxyGenerator.IsCSharp));
            method.Comments.AddRange(CodeGenUtilities.GenerateReturnsCodeComment(Resource.CodeGen_GetIdentify_Method_Returns_Comment, this.ClientProxyGenerator.IsCSharp));

            // Create an array of direct field references to key members. 
            // Field references are preferred over property references for performance reasons.
            CodeFieldReferenceExpression[] keyRefs = new CodeFieldReferenceExpression[this._keyProperties.Count];
            CodeExpression defaultKeyValueTest = null;
            int nullableKeyMemberCount = 0;
            for (int i = 0; i < keyRefs.Length; ++i)
            {
                PropertyDescriptor pd = keyProperties[i];
                CodeFieldReferenceExpression fieldRef = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), CodeGenUtilities.MakeCompliantFieldName(pd.Name));
                
                // generate null checks for any nullable/reference type key members
                Type keyType = keyProperties[i].PropertyType;
                CodeExpression compareExpr = null;
                if (!keyType.IsValueType || TypeUtility.IsNullableType(keyType))
                {
                    nullableKeyMemberCount++;
                    compareExpr = CodeGenUtilities.MakeEqualToNull(fieldRef);

                    if (defaultKeyValueTest != null)
                    {
                        defaultKeyValueTest = new CodeBinaryOperatorExpression(defaultKeyValueTest, CodeBinaryOperatorType.BooleanOr, compareExpr);
                    }
                    else
                    {
                        defaultKeyValueTest = compareExpr;
                    }
                }

                keyRefs[i] = fieldRef;
            }
           
            // Optimization -- single key field is alone sufficient for identity -- return it all by itself
            if (this._keyProperties.Count == 1)
            {
                method.Statements.Add(new CodeMethodReturnStatement(keyRefs[0]));
            }
            else
            {
                // ------------------------------------------------------------------------
                // Multipart key:
                // Generate the if test returning null if any key members are null.
                // (for single part keys of a reference/nullable type, we simply return the member itself)
                // Then generate EntityKey.Create(field1, field2, ..., fieldN)
                // ------------------------------------------------------------------------

                if (nullableKeyMemberCount > 0)
                {
                    CodeConditionStatement defaultKeyValueBlock = new CodeConditionStatement(defaultKeyValueTest, new CodeMethodReturnStatement(new CodePrimitiveExpression(null)));
                    method.Statements.Add(defaultKeyValueBlock);
                }                

                CodeTypeReference entityKeyRef = CodeGenUtilities.GetTypeReference(TypeConstants.EntityKeyTypeFullName, this.Type.Namespace, false);
                CodeMethodInvokeExpression keyCreateExpression = new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(entityKeyRef), "Create", keyRefs);
                method.Statements.Add(new CodeMethodReturnStatement(keyCreateExpression));
            }
        }

        /// <summary>
        /// Helper method to determine whether a method is polymorphic.
        /// This method gives an answer only, it does not log.
        /// </summary>
        /// <param name="methodInfo">The method to test.  It may be null.</param>
        /// <returns><c>true</c> if the method is virtual or new.</returns>
        private bool IsMethodPolymorphic(MethodInfo methodInfo)
        {
            // Null allowed for convenience.
            // If method is declared on a different entity type, then one of 2
            // things will be true:
            //  1. The declaring type is invisible, in which case it is not a problem
            //  2. The declaring type is visible, in which case the error is reported there.
            if (methodInfo == null || methodInfo.DeclaringType != this.Type)
            {
                return false;
            }

            // Virtual methods are disallowed.
            // But the CLR marks interface methods IsVirtual=true, so the
            // recommended test is this one.
            if (methodInfo.IsVirtual && !methodInfo.IsFinal)
            {
                return true;
            }

            // Detecting the "new" keyword requires a check whether this method is
            // hiding a method with the same signature in a derived type.  IsHideBySig does not do this.
            if (this.Type.BaseType != null)
            {
                Type[] parameterTypes = methodInfo.GetParameters().Select<ParameterInfo, Type>(p => p.ParameterType).ToArray();
                MethodInfo baseMethod = this.Type.BaseType.GetMethod(methodInfo.Name, parameterTypes);
                if (baseMethod != null)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Computes the (visible) base type hierarchy from the given <paramref name="entityType"/>
        /// back to its root.  Entities not exposed to the client are omitted.
        /// </summary>
        /// <param name="entityType">The starting entity type whose hierarchy is needed.</param>
        /// <returns>The list of base types.  The root type will be last.</returns>
        private IEnumerable<Type> GetVisibleBaseTypes(Type entityType)
        {
            List<Type> types = new List<Type>();
            for (Type baseType = this._domainServiceDescriptionAggregate.GetEntityBaseType(entityType);
                 baseType != null;
                 baseType = this._domainServiceDescriptionAggregate.GetEntityBaseType(baseType))
            {
                types.Add(baseType);
            }
            return types;
        }

        /// <summary>
        /// Returns whether or not a shared entity's least derived entity is also shared.
        /// If false, an error will be logged.
        /// </summary>
        /// <param name="entityType">The entity to compare against the least derived entity.</param>
        /// <returns>Returns true if the shared entity's least derived entity is the same
        /// across all <see cref="DomainService"/> that expose the entity.</returns>
        private bool VerifySharedEntityRoot(Type entityType)
        {
            // Only perform computation on shared entities.
            if (this._domainServiceDescriptionAggregate.IsShared)
            {
                Type firstRootType = null;
                DomainServiceDescription firstDescription = null;

                foreach (var nextDescription in this._domainServiceDescriptionAggregate.DomainServiceDescriptions)
                {
                    Type nextRootType = nextDescription.GetRootEntityType(entityType);

                    // The first root we have seen, continue.
                    if (firstRootType == null)
                    {
                        firstRootType = nextRootType;
                        firstDescription = nextDescription;
                        continue;
                    }

                    if (firstRootType != nextRootType)
                    {
                        this.ClientProxyGenerator.LogError(string.Format(CultureInfo.CurrentCulture,
                            Resource.EntityCodeGen_SharedEntityMustBeLeastDerived,
                            firstRootType, firstDescription.DomainServiceType,
                            nextRootType, nextDescription.DomainServiceType,
                            entityType));

                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// This class aggregates information from the all the <see cref="DomainServiceDescription"/>
        /// that exposes the entity being generated.
        /// </summary>
        private class DomainServiceDescriptionAggregate
        {
            private readonly HashSet<Type> _complexTypes;
            private readonly HashSet<Type> _entityTypes;

            /// <summary>
            /// Initializes a new instance of the <see cref="DomainServiceDescriptionAggregate"/> class.
            /// </summary>
            /// <param name="domainServiceDescriptions">The descriptions that exposes the entity type.</param>
            internal DomainServiceDescriptionAggregate(IEnumerable<DomainServiceDescription> domainServiceDescriptions)
            {
                this.DomainServiceDescriptions = domainServiceDescriptions;
                this._complexTypes = new HashSet<Type>();
                this._entityTypes = new HashSet<Type>();

                foreach (var dsd in domainServiceDescriptions)
                {
                    foreach (var complexType in dsd.ComplexTypes)
                    {
                        this._complexTypes.Add(complexType);
                    }

                    foreach (var entityType in dsd.EntityTypes)
                    {
                        this._entityTypes.Add(entityType);
                    }
                }
            }

            /// <summary>
            /// Gets all the <see cref="DomainServiceDescription"/> that expose this entity.
            /// </summary>
            internal IEnumerable<DomainServiceDescription> DomainServiceDescriptions
            {
                get;
                private set;
            }

            /// <summary>
            /// Gets an enumerable representing the union of all complex types exposed
            /// by each <see cref="DomainServiceDescription"/>.
            /// </summary>
            internal IEnumerable<Type> ComplexTypes
            {
                get
                {
                    foreach (Type complexType in this._complexTypes)
                    {
                        yield return complexType;
                    }
                }
            }

            /// <summary>
            /// Gets an enumerable representing the union of all entity types exposed
            /// by each <see cref="DomainServiceDescription"/>.
            /// </summary>
            internal IEnumerable<Type> EntityTypes
            {
                get
                {
                    foreach (Type entityType in this._entityTypes)
                    {
                        yield return entityType;
                    }
                }
            }

            /// <summary>
            /// Returns true if the entity is shared.
            /// </summary>
            internal bool IsShared
            {
                get
                {
                    return this.DomainServiceDescriptions.Count() > 1;
                }
            }

            /// <summary>
            /// Gets the base type of the given entity type.
            /// </summary>
            /// <param name="entityType">The entity type whose base type is required.</param>
            /// <returns>The base type or <c>null</c> if the given
            /// <paramref name="entityType"/> had no visible base types.</returns>
            internal Type GetEntityBaseType(Type entityType)
            {
                Debug.Assert(entityType != null, "entityType must be non-null");

                Type baseType = entityType.BaseType;
                while (baseType != null)
                {
                    if (this.EntityTypes.Contains(baseType))
                    {
                        break;
                    }
                    baseType = baseType.BaseType;
                }
                return baseType;
            }

            /// <summary>
            /// Returns the root type for the given entity type.
            /// </summary>
            /// <remarks>
            /// The root type is the least derived entity type in the entity type
            /// hierarchy that is exposed through a <see cref="DomainService"/>.
            /// </remarks>
            /// <param name="entityType">The entity type whose root is required.</param>
            /// <returns>The type of the root or <c>null</c> if the given <paramref name="entityType"/>
            /// has no base types.</returns>
            internal Type GetRootEntityType(Type entityType)
            {
                Debug.Assert(entityType != null, "entityType must be non-null");

                Type rootType = null;
                while (entityType != null)
                {
                    if (this.EntityTypes.Contains(entityType))
                    {
                        rootType = entityType;
                    }
                    entityType = entityType.BaseType;
                }
                return rootType;
            }
        }
    }
}
