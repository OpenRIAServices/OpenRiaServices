using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Mapping;
using System.Data.Entity.Core.Metadata.Edm;
using System.Globalization;
using System.Linq;
using OpenRiaServices.Server;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore;
using OpenRiaServices.Server.EntityFrameworkCore;

namespace OpenRiaServices.EntityFrameworkCore
{
    /// <summary>
    /// Metadata context for LINQ To Entities domain services
    /// </summary>
    internal class LinqToEntitiesEFCoreTypeDescriptionContext : TypeDescriptionContextBase
    {
        private readonly Dictionary<string, AssociationInfo> _associationMap = new Dictionary<string, AssociationInfo>();
        private readonly Type _contextType;
        private MetadataWorkspace _metadataWorkspace;
        private IModel _model;

        /// <summary>
        /// Constructor that accepts a LINQ To Entities context type
        /// </summary>
        /// <param name="contextType">The ObjectContext Type</param>
        public LinqToEntitiesEFCoreTypeDescriptionContext(Type contextType)
        {
            if (contextType == null)
            {
                throw new ArgumentNullException(nameof(contextType));
            }
            this._contextType = contextType;
        }

        // TODO: Add model property _context.Model
        public IModel Model
        {
            get
            {
                if (_model == null)
                {
                    // TODO: Is there a smarter way ?? 
                    var dbContext = (DbContext)Activator.CreateInstance(_contextType);
                    _model = dbContext.Model;
                }
                return _model;
            }
        }

        public IEntityType GetEntityType(Type type) => Model.FindEntityType(type);

        /// <summary>
        /// Gets the MetadataWorkspace for the context
        /// </summary>
        public MetadataWorkspace MetadataWorkspace
        {
            get
            {
                if (this._metadataWorkspace == null)
                {
                    // we only support embedded mappings
                    this._metadataWorkspace = MetadataWorkspaceUtilitiesEFCore.CreateMetadataWorkspace(this._contextType);
                }
                return this._metadataWorkspace;
            }
        }

        /// <summary>
        /// Returns the <see cref="StructuralType"/> that corresponds to the given CLR type
        /// </summary>
        /// <param name="clrType">The CLR type</param>
        /// <returns>The StructuralType that corresponds to the given CLR type</returns>
        public StructuralType GetEdmType(Type clrType)
        {
            return null;
            //return ObjectContextUtilitiesEFCore.GetEdmType(this.MetadataWorkspace, clrType);
        }

        /// <summary>
        /// Returns the association information for the specified navigation property.
        /// </summary>
        /// <param name="navigationProperty">The navigation property to return association information for</param>
        /// <returns>The association info</returns>
        internal AssociationInfo GetAssociationInfo(NavigationProperty navigationProperty)
        {
            lock (this._associationMap)
            {
                string associationName = navigationProperty.RelationshipType.FullName;
                AssociationInfo associationInfo = null;
                if (!this._associationMap.TryGetValue(associationName, out associationInfo))
                {
                    AssociationType associationType = (AssociationType)navigationProperty.RelationshipType;

                    if (!associationType.ReferentialConstraints.Any())
                    {
                        // We only support EF models where FK info is part of the model.
                        throw new NotSupportedException(
                            string.Format(CultureInfo.CurrentCulture,
                            ResourceEFCore.LinqToEntitiesProvider_UnableToRetrieveAssociationInfo, associationName));
                    }

                    associationInfo = new AssociationInfo();
                    associationInfo.FKRole = associationType.ReferentialConstraints[0].ToRole.Name;
                    associationInfo.Name = this.GetAssociationName(navigationProperty, associationInfo.FKRole);
                    associationInfo.ThisKey = associationType.ReferentialConstraints[0].ToProperties.Select(p => p.Name).ToArray();
                    associationInfo.OtherKey = associationType.ReferentialConstraints[0].FromProperties.Select(p => p.Name).ToArray();
                    associationInfo.IsRequired = associationType.RelationshipEndMembers[0].RelationshipMultiplicity == RelationshipMultiplicity.One;

                    this._associationMap[associationName] = associationInfo;
                }

                return associationInfo;
            }
        }

        /// <summary>
        /// Creates an AssociationAttribute for the specified navigation property
        /// </summary>
        /// <param name="navigationProperty">The navigation property that corresponds to the association (it identifies the end points)</param>
        /// <returns>A new AssociationAttribute that describes the given navigation property association</returns>
        internal AssociationAttribute CreateAssociationAttribute(NavigationProperty navigationProperty)
        {
            AssociationInfo assocInfo = this.GetAssociationInfo(navigationProperty);
            bool isForeignKey = navigationProperty.FromEndMember.Name == assocInfo.FKRole;
            string thisKey;
            string otherKey;
            if (isForeignKey)
            {
                thisKey = FormatMemberList(assocInfo.ThisKey);
                otherKey = FormatMemberList(assocInfo.OtherKey);
            }
            else
            {
                otherKey = FormatMemberList(assocInfo.ThisKey);
                thisKey = FormatMemberList(assocInfo.OtherKey);
            }

            AssociationAttribute assocAttrib = new AssociationAttribute(assocInfo.Name, thisKey, otherKey);
            assocAttrib.IsForeignKey = isForeignKey;
            return assocAttrib;
        }

        /// <summary>
        /// Returns a unique association name for the specified navigation property.
        /// </summary>
        /// <param name="navigationProperty">The navigation property</param>
        /// <param name="foreignKeyRoleName">The foreign key role name for the property's association</param>
        /// <returns>A unique association name for the specified navigation property.</returns>
        private string GetAssociationName(NavigationProperty navigationProperty, string foreignKeyRoleName)
        {
            RelationshipEndMember fromMember = navigationProperty.FromEndMember;
            RelationshipEndMember toMember = navigationProperty.ToEndMember;

            RefType toRefType = toMember.TypeUsage.EdmType as RefType;
            EntityType toEntityType = toRefType.ElementType as EntityType;

            RefType fromRefType = fromMember.TypeUsage.EdmType as RefType;
            EntityType fromEntityType = fromRefType.ElementType as EntityType;

            bool isForeignKey = navigationProperty.FromEndMember.Name == foreignKeyRoleName;
            string fromTypeName = isForeignKey ? fromEntityType.Name : toEntityType.Name;
            string toTypeName = isForeignKey ? toEntityType.Name : fromEntityType.Name;

            // names are always formatted non-FK side type name followed by FK side type name
            string associationName = string.Format(CultureInfo.InvariantCulture, "{0}_{1}", toTypeName, fromTypeName);
            associationName = MakeUniqueName(associationName, this._associationMap.Values.Select(p => p.Name));

            return associationName;
        }
    }
}
