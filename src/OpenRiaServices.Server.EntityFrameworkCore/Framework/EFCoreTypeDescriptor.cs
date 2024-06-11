using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;

namespace OpenRiaServices.Server.EntityFrameworkCore
{
    /// <summary>
    /// CustomTypeDescriptor for LINQ To Entities
    /// </summary>
    internal class EFCoreTypeDescriptor : TypeDescriptorBase
    {
        private readonly EFCoreTypeDescriptionContext _typeDescriptionContext;
        private readonly bool _keyIsEditable;
        private HashSet<string> _foreignKeyMembers;

#if NETSTANDARD2_0
        private readonly IProperty _timestampProperty;
        private readonly IEntityType _entityType;
        public EFCoreTypeDescriptor(EFCoreTypeDescriptionContext typeDescriptionContext, IEntityType entityType, ICustomTypeDescriptor parent)
#else
        private readonly IReadOnlyProperty _timestampProperty;
        private readonly IReadOnlyEntityType _entityType;
        public EFCoreTypeDescriptor(EFCoreTypeDescriptionContext typeDescriptionContext, IReadOnlyEntityType entityType, ICustomTypeDescriptor parent)
#endif
        : base(parent)
        {
            _typeDescriptionContext = typeDescriptionContext;
            _entityType = entityType;

            var timestampMembers = entityType.GetProperties().Where(p => IsConcurrencyTimestamp(p)).ToArray();
            if (timestampMembers.Length == 1)
            {
                _timestampProperty = timestampMembers[0];
            }

            // TODO: determine if we should exclude owned entities just as EF6 excludes "complex objects" here
            // Needs to add owned typ scenarios
            if (!entityType.IsOwned())
            {
                // if any FK member of any association is also part of the primary key, then the key cannot be marked
                // Editable(false)
                _foreignKeyMembers = new HashSet<string>(entityType.GetNavigations()
                     .Where(n =>
#if NETSTANDARD2_0
                     n.IsDependentToPrincipal()
#else
                     n.IsOnDependent
#endif
                     )
                     .SelectMany(n => n.ForeignKey.Properties)
                     .Select(x => x.Name));

                foreach (var key in entityType.GetKeys())
                {
                    foreach (var keyMember in key.Properties)
                    {
                        if (IsForeignKeyMember(keyMember.Name))
                        {
                            _keyIsEditable = true;
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
#if NETSTANDARD2_0
        private static bool IsConcurrencyTimestamp(IProperty p)
        {
            return p.IsConcurrencyToken
                && p.ValueGenerated == ValueGenerated.OnAddOrUpdate;
        }
#else
        private static bool IsConcurrencyTimestamp(IReadOnlyProperty p)
        {
            return p.IsConcurrencyToken
                && p.ValueGenerated == ValueGenerated.OnAddOrUpdate;
        }
#endif


        /// <summary>
        /// Gets the metadata context
        /// </summary>
        public EFCoreTypeDescriptionContext TypeDescriptionContext
        {
            get
            {
                return _typeDescriptionContext;
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
            var attributes = new List<Attribute>();

            EditableAttribute editableAttribute = null;
            bool inferRoundtripOriginalAttribute = false;

            bool hasKeyAttribute = pd.Attributes[typeof(KeyAttribute)] != null;
            var property = _entityType.FindProperty(pd.Name);

            if (property != null)
            {
                if (property.IsPrimaryKey() && !hasKeyAttribute)
                {
                    attributes.Add(new KeyAttribute());
                    hasKeyAttribute = true;
                }

                if (hasKeyAttribute)
                {
                    // key members must always be roundtripped
                    inferRoundtripOriginalAttribute = true;

                    // key members that aren't also FK members are non-editable (but allow an initial value)
                    if (!_keyIsEditable)
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
#if !NETSTANDARD2_0
                        case ValueGenerated.OnUpdateSometimes:
                            break;
#endif
                        case ValueGenerated.OnUpdate:
                            attributes.Add(new DatabaseGeneratedAttribute(DatabaseGeneratedOption.Computed));
                            databaseGenerated = true;
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
                    pd.Attributes[typeof(StringLengthAttribute)] == null &&
                    property.GetMaxLength() is int maxLength)
                {
                    attributes.Add(new StringLengthAttribute(maxLength));
                }

                bool hasTimestampAttribute = pd.Attributes[typeof(TimestampAttribute)] != null;
                if (_timestampProperty == property && !hasTimestampAttribute)
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
                if ((_timestampProperty == null || _timestampProperty == property) &&
                    (inferRoundtripOriginalAttribute || IsForeignKeyMember(property.Name)) &&
                    pd.Attributes[typeof(AssociationAttribute)] == null)
                {
                    if (pd.Attributes[typeof(RoundtripOriginalAttribute)] == null)
                    {
                        attributes.Add(new RoundtripOriginalAttribute());
                    }
                }

                // Add the Editable attribute if required
                if (editableAttribute != null && pd.Attributes[typeof(EditableAttribute)] == null)
                {
                    attributes.Add(editableAttribute);
                }
            }

            // Add AssociationAttribute if required for the specified property
            if (_entityType.FindNavigation(pd.Name) is { } navigation)
            {
#if NETSTANDARD2_0
                bool isManyToMany = navigation.IsCollection() && navigation.FindInverse()?.IsCollection() == true;
                bool addAssociationAttribute = !isManyToMany;
#else
                bool isManyToMany = navigation.IsCollection && navigation.Inverse?.IsCollection == true;
                bool addAssociationAttribute = !isManyToMany
                    // Don't generate association attributes for Owned types (onless they have all FK fields explictly defined, in which case they can be treated as Entities)
                    //  if we generate association attributes then it cannot be treated as a ComplexObject
                    && !(navigation.ForeignKey.Properties.Any(static p => p.IsShadowProperty()));
#endif

                if (addAssociationAttribute)
                {
                    if (pd.Attributes[typeof(AssociationAttribute)] is null)
                        attributes.Add(EFCoreTypeDescriptionContext.CreateAssociationAttribute(navigation));
#if NET
                    if (navigation.TargetEntityType.IsOwned() && pd.Attributes[typeof(CompositionAttribute)] is null)
                    {
                        attributes.Add(new CompositionAttribute());
                    }
#endif
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
            // exclude IChangeDetector.EntityState members
#pragma warning disable EF1001 // Internal EF Core API usage.
            return pd.PropertyType == typeof(EntityState) && typeof(IChangeDetector).IsAssignableFrom(pd.ComponentType);
#pragma warning restore EF1001 // Internal EF Core API usage.
        }

        private bool IsForeignKeyMember(string name) => _foreignKeyMembers != null && _foreignKeyMembers.Contains(name);
    }
}
