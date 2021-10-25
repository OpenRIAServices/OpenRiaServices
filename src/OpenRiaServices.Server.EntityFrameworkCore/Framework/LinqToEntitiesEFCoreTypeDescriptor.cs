using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
#if RIACONTRIB
using System.ServiceModel.DomainServices.Server;
#endif
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using OpenRiaServices.Server;
using System.Data.Metadata.Edm;
using System.Data.Objects.DataClasses;

namespace OpenRiaServices.EntityFrameworkCore
{
    /// <summary>
    /// CustomTypeDescriptor for LINQ To Entities
    /// </summary>
    internal class LinqToEntitiesEFCoreTypeDescriptor : TypeDescriptorBase
    {
        private readonly LinqToEntitiesEFCoreTypeDescriptionContext _typeDescriptionContext;
        private readonly StructuralType _edmType;
        private readonly EdmMember _timestampMember;
        private readonly HashSet<EdmMember> _foreignKeyMembers;
        private readonly bool _keyIsEditable;

        /// <summary>
        /// Constructor taking a metadata context, an structural type, and a parent custom type descriptor
        /// </summary>
        /// <param name="typeDescriptionContext">The <see cref="LinqToEntitiesEFCoreTypeDescriptionContext"/> context.</param>
        /// <param name="edmType">The <see cref="StructuralType"/> type (can be an entity or complex type).</param>
        /// <param name="parent">The parent custom type descriptor.</param>
        public LinqToEntitiesEFCoreTypeDescriptor(LinqToEntitiesEFCoreTypeDescriptionContext typeDescriptionContext, StructuralType edmType, ICustomTypeDescriptor parent)
            : base(parent)
        {
            this._typeDescriptionContext = typeDescriptionContext;
            this._edmType = edmType;

            EdmMember[] timestampMembers = this._edmType.Members.Where(p => ObjectContextUtilitiesEFCore.IsConcurrencyTimestamp(p)).ToArray();
            if (timestampMembers.Length == 1)
            {
                this._timestampMember = timestampMembers[0];
            }

            if (edmType.BuiltInTypeKind == BuiltInTypeKind.EntityType)
            {
                // if any FK member of any association is also part of the primary key, then the key cannot be marked
                // Editable(false)
                EntityType entityType = (EntityType)edmType;
                this._foreignKeyMembers = new HashSet<EdmMember>(entityType.NavigationProperties.SelectMany(p => p.GetDependentProperties()));
                foreach (EdmProperty foreignKeyMember in this._foreignKeyMembers)
                {
                    if (entityType.KeyMembers.Contains(foreignKeyMember))
                    {
                        this._keyIsEditable = true;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the metadata context
        /// </summary>
        public LinqToEntitiesEFCoreTypeDescriptionContext TypeDescriptionContext
        {
            get
            {
                return this._typeDescriptionContext;
            }
        }

        /// <summary>
        /// Gets the Edm type
        /// </summary>
        private StructuralType EdmType
        {
            get
            {
                return this._edmType;
            }
        }

        /// <summary>
        /// Returns a collection of all the <see cref="Attribute"/>s we infer from the metadata associated
        /// with the metadata member corresponding to the given property descriptor
        /// </summary>
        /// <param name="pd">A <see cref="PropertyDescriptor"/> to examine</param>
        /// <returns>A collection of attributes inferred from the metadata in the given descriptor.</returns>
        protected override IEnumerable<Attribute> GetMemberAttributes(PropertyDescriptor pd)
        {
            List<Attribute> attributes = new List<Attribute>();

            // Exclude any EntityState, EntityReference, etc. members
            if (ShouldExcludeEntityMember(pd))
            {
                // for these members, we don't want to do any further
                // attribute inference
                attributes.Add(new ExcludeAttribute());
                return attributes.ToArray();
            }

            EditableAttribute editableAttribute = null;
            bool inferRoundtripOriginalAttribute = false;

            bool hasKeyAttribute = (pd.Attributes[typeof(KeyAttribute)] != null);
            bool isEntity = this.EdmType.BuiltInTypeKind == BuiltInTypeKind.EntityType;
            if (isEntity)
            {
                EntityType entityType = (EntityType)this.EdmType;
                EdmMember keyMember = entityType.KeyMembers.SingleOrDefault(k => k.Name == pd.Name);
                if (keyMember != null && !hasKeyAttribute)
                {
                    attributes.Add(new KeyAttribute());
                    hasKeyAttribute = true;
                }
            }

            EdmProperty member = this.EdmType.Members.SingleOrDefault(p => p.Name == pd.Name) as EdmProperty;
            if (member != null)
            {
                if (hasKeyAttribute)
                {
                    // key members must always be roundtripped
                    inferRoundtripOriginalAttribute = true;

                    // key members that aren't also FK members are non-editable (but allow an initial value)
                    if (!this._keyIsEditable)
                    {
                        editableAttribute = new EditableAttribute(false) { AllowInitialValue = true };
                    }
                }

                // Check if the member is DB generated and add the DatabaseGeneratedAttribute to it if not already present.                
                bool databaseGenerated = false;
                if (pd.Attributes[typeof(DatabaseGeneratedAttribute)] == null)
                {
                    MetadataProperty md = ObjectContextUtilitiesEFCore.GetStoreGeneratedPattern(member);
                    if (md != null)
                    {
                        if ((string)md.Value == "Computed")
                        {
                            attributes.Add(new DatabaseGeneratedAttribute(DatabaseGeneratedOption.Computed));
                            databaseGenerated = true;
                        }
                        else if ((string)md.Value == "Identity")
                        {
                            attributes.Add(new DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity));
                            databaseGenerated = true;
                        }
                    }
                }
                else
                {
                    databaseGenerated = ((DatabaseGeneratedAttribute)pd.Attributes[typeof(DatabaseGeneratedAttribute)]).DatabaseGeneratedOption != DatabaseGeneratedOption.None;
                }

                // Add implicit ConcurrencyCheck attribute to metadata if ConcurrencyMode is anything other than ConcurrencyMode.None
                Facet facet = member.TypeUsage.Facets.SingleOrDefault(p => p.Name == "ConcurrencyMode");
                if (facet != null && facet.Value != null && (ConcurrencyMode)facet.Value != ConcurrencyMode.None &&
                    pd.Attributes[typeof(ConcurrencyCheckAttribute)] == null)
                {
                    attributes.Add(new ConcurrencyCheckAttribute());
                    inferRoundtripOriginalAttribute = true;
                }
                
                // Add Required attribute to metadata if the member cannot be null and it is either a reference type or a Nullable<T>
                // unless it is a database generated field
                if (!member.Nullable && (!pd.PropertyType.IsValueType || IsNullableType(pd.PropertyType)) 
                    && !databaseGenerated
                    && pd.Attributes[typeof(RequiredAttribute)] == null)
                {
                    attributes.Add(new RequiredAttribute());
                }

                bool isStringType = pd.PropertyType == typeof(string) || pd.PropertyType == typeof(char[]);
                if (isStringType &&
                    pd.Attributes[typeof(StringLengthAttribute)] == null)
                {
                    facet = member.TypeUsage.Facets.SingleOrDefault(p => p.Name == "MaxLength");
                    if (facet != null && facet.Value != null && facet.Value.GetType() == typeof(int))
                    {
                        // need to test for Type int, since the value can also be of type
                        // System.Data.Metadata.Edm.EdmConstants.Unbounded
                        int maxLength = (int)facet.Value;
                        attributes.Add(new StringLengthAttribute(maxLength));
                    }
                }

                bool hasTimestampAttribute = (pd.Attributes[typeof(TimestampAttribute)] != null);

                if (this._timestampMember == member && !hasTimestampAttribute)
                {
                    attributes.Add(new TimestampAttribute());
                    hasTimestampAttribute = true;
                }

                // All members marked with TimestampAttribute (inferred or explicit) need to
                // have [Editable(false)] and [RoundtripOriginal] applied
                if (hasTimestampAttribute)
                {
                    inferRoundtripOriginalAttribute = true;

                    if (editableAttribute == null)
                    {
                        editableAttribute = new EditableAttribute(false);
                    }
                }

                // Add RTO to this member if required. If this type has a timestamp
                // member that member should be the ONLY member we apply RTO to.
                // Dont apply RTO if it is an association member.
                bool isForeignKeyMember = this._foreignKeyMembers != null && this._foreignKeyMembers.Contains(member);
                if ((this._timestampMember == null || this._timestampMember == member) &&
                    (inferRoundtripOriginalAttribute || isForeignKeyMember) &&
                    pd.Attributes[typeof(AssociationAttribute)] == null)
                {
                    if (pd.Attributes[typeof(RoundtripOriginalAttribute)] == null)
                    {
                        attributes.Add(new RoundtripOriginalAttribute());
                    }
                }
            }

            // Add the Editable attribute if required
            if (editableAttribute != null && pd.Attributes[typeof(EditableAttribute)] == null)
            {
                attributes.Add(editableAttribute);
            }

            if (isEntity)
            {
                this.AddAssociationAttributes(pd, attributes);
            }

            return attributes.ToArray();
        }

        /// <summary>
        /// Determines whether the specified property is an Entity member that
        /// should be excluded.
        /// </summary>
        /// <param name="pd">The property to check.</param>
        /// <returns>True if the property should be excluded, false otherwise.</returns>
        internal static bool ShouldExcludeEntityMember(PropertyDescriptor pd)
        {
            // exclude EntityState members
            if (pd.PropertyType == typeof(EntityState) &&
                (pd.ComponentType == typeof(EntityObject) || typeof(IEntityChangeTracker).IsAssignableFrom(pd.ComponentType)))
            {
                return true;
            }

            // exclude entity reference properties
            if (typeof(EntityReference).IsAssignableFrom(pd.PropertyType))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Add AssociationAttribute if required for the specified property
        /// </summary>
        /// <param name="pd">The property</param>
        /// <param name="attributes">The list of attributes to append to</param>
        private void AddAssociationAttributes(PropertyDescriptor pd, List<Attribute> attributes)
        {
            EntityType entityType = (EntityType)this.EdmType;
            NavigationProperty navProperty = entityType.NavigationProperties.Where(n => n.Name == pd.Name).SingleOrDefault();
            if (navProperty != null)
            {
                bool isManyToMany = navProperty.RelationshipType.RelationshipEndMembers[0].RelationshipMultiplicity == RelationshipMultiplicity.Many &&
                                    navProperty.RelationshipType.RelationshipEndMembers[1].RelationshipMultiplicity == RelationshipMultiplicity.Many;
                if (!isManyToMany)
                {
                    AssociationAttribute assocAttrib = (AssociationAttribute)pd.Attributes[typeof(System.ComponentModel.DataAnnotations.AssociationAttribute)];
                    if (assocAttrib == null)
                    {
                        assocAttrib = this.TypeDescriptionContext.CreateAssociationAttribute(navProperty);
                        attributes.Add(assocAttrib);
                    }
                }
            }
        }
    }
}
