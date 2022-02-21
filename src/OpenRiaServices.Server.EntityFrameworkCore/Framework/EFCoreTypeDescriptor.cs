using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using OpenRiaServices.Server;
using Microsoft.EntityFrameworkCore.Metadata;

namespace OpenRiaServices.EntityFrameworkCore
{
    /// <summary>
    /// CustomTypeDescriptor for LINQ To Entities
    /// </summary>
    internal class EFCoreTypeDescriptor : TypeDescriptorBase
    {
        private readonly EFCoreTypeDescriptionContext _typeDescriptionContext;
        private readonly IEntityType _entityType;
        private readonly IProperty _timestampProperty;
        private readonly bool _keyIsEditable;
        private Dictionary<string, IProperty> _foreignKeyMembers; // TODO: change to List<string> / HashSet<string>

        public EFCoreTypeDescriptor(EFCoreTypeDescriptionContext typeDescriptionContext, IEntityType entityType, ICustomTypeDescriptor parent)
        : base(parent)
        {
            this._typeDescriptionContext = typeDescriptionContext;
            this._entityType = entityType;

            var timestampMembers = entityType.GetProperties().Where(p => IsConcurrencyTimestamp(p)).ToArray();
            if (timestampMembers.Length == 1)
            {
                this._timestampProperty = timestampMembers[0];
            }

            // if (edmType.BuiltInTypeKind == BuiltInTypeKind.EntityType)
            {
                // if any FK member of any association is also part of the primary key, then the key cannot be marked
                // Editable(false)
                _foreignKeyMembers = entityType.GetNavigations()
                    .Where(n => n.IsDependentToPrincipal())
                    .SelectMany(n => n.ForeignKey.Properties)
                    .ToDictionary(x => x.Name);
                //this._foreignKeyMembers

                foreach(var key in entityType.GetKeys())
                {
                    foreach (var keyMember in key.Properties)
                    {
                        if (_foreignKeyMembers.ContainsKey(keyMember.Name))
                        {
                            this._keyIsEditable = true;
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Check if a property is a "Timestamp" property (rowversion or similar)
        /// </summary>
        ///  /// <remarks>Since EF doesn't expose "timestamp" as a first class
        /// concept, we use the below criteria to infer this for ourselves.
        /// </remarks>
        private static bool IsConcurrencyTimestamp(IProperty p)
        {
            //IsConcurrencyTimestamp
            return p.IsConcurrencyToken
                // && p.GetMaxLength() == 8
                // && p.IsFixedLength
                && p.ValueGenerated == ValueGenerated.OnAddOrUpdate;
        }

        /// <summary>
        /// Gets the metadata context
        /// </summary>
        public EFCoreTypeDescriptionContext TypeDescriptionContext
        {
            get
            {
                return this._typeDescriptionContext;
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

            EditableAttribute editableAttribute = null;
            bool inferRoundtripOriginalAttribute = false;

            bool hasKeyAttribute = (pd.Attributes[typeof(KeyAttribute)] != null);
            bool isEntity = true; //  this.EdmType.BuiltInTypeKind == BuiltInTypeKind.EntityType;
            var property = _entityType.FindProperty(pd.Name);

            if (property != null)
            {
                if (property.IsPrimaryKey())
                {
                    attributes.Add(new KeyAttribute());
                    hasKeyAttribute = true;
                }

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
                    switch (property.ValueGenerated)
                    {
                        case ValueGenerated.Never:
                            break;
                        case ValueGenerated.OnAddOrUpdate:
                            attributes.Add(new DatabaseGeneratedAttribute(DatabaseGeneratedOption.Computed));
                            databaseGenerated = true;
                            break;
                        case ValueGenerated.OnAdd:
                            attributes.Add(new DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity));
                            databaseGenerated = true;
                            break;
                        default: throw new NotImplementedException("Only suppport ValueGenerated.OnAdd, OnAddOrUpdate, None");
                    }
                }
                else
                {
                    databaseGenerated = ((DatabaseGeneratedAttribute)pd.Attributes[typeof(DatabaseGeneratedAttribute)]).DatabaseGeneratedOption != DatabaseGeneratedOption.None;
                }

                // Add implicit ConcurrencyCheck attribute to metadata if ConcurrencyMode is anything other than ConcurrencyMode.None
                if (property.IsConcurrencyToken &&
                    pd.Attributes[typeof(ConcurrencyCheckAttribute)] == null)
                {
                    attributes.Add(new ConcurrencyCheckAttribute());
                    inferRoundtripOriginalAttribute = true;
                }

                // Add Required attribute to metadata if the member cannot be null and it is either a reference type or a Nullable<T>
                // unless it is a database generated field
                if (!property.IsNullable && (!pd.PropertyType.IsValueType || IsNullableType(pd.PropertyType))
                    && !databaseGenerated
                    && pd.Attributes[typeof(RequiredAttribute)] == null)
                {
                    attributes.Add(new RequiredAttribute());
                }

                bool isStringType = pd.PropertyType == typeof(string) || pd.PropertyType == typeof(char[]);
                if (isStringType &&
                    pd.Attributes[typeof(StringLengthAttribute)] == null)
                {
                    if (property.GetMaxLength() is int maxLength)
                    {
                        attributes.Add(new StringLengthAttribute(maxLength));
                    }
                }

                bool hasTimestampAttribute = (pd.Attributes[typeof(TimestampAttribute)] != null);
                if (this._timestampProperty == property && !hasTimestampAttribute)
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
                bool isForeignKeyMember = this._foreignKeyMembers != null  && this._foreignKeyMembers.ContainsKey(property.Name);
                if ((this._timestampProperty == null || this._timestampProperty == property) &&
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

            // Add AssociationAttribute if required for the specified property
            if (_entityType.FindNavigation(pd.Name) is INavigation navigation)
            {
                bool isManyToMany = navigation.IsCollection() && (navigation.FindInverse()?.IsCollection() == true);
                if (!isManyToMany)
                {
                    AssociationAttribute assocAttrib = (AssociationAttribute)pd.Attributes[typeof(System.ComponentModel.DataAnnotations.AssociationAttribute)];
                    if (assocAttrib == null)
                    {
                        assocAttrib = this.TypeDescriptionContext.CreateAssociationAttribute(navigation);
                        attributes.Add(assocAttrib);
                    }
                }

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
            //TODO: remove this, EntityState is not part of entities 

            // exclude EntityState members
            if (pd.PropertyType == typeof(EntityState)) // TODO: Maybe also check pd.Component type
            {
                return true;
            }


            return false;
        }
    }
}
