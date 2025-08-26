﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

#if NETFRAMEWORK
using IReadOnlyNavigation = Microsoft.EntityFrameworkCore.Metadata.INavigation;
using IReadOnlyEntityType = Microsoft.EntityFrameworkCore.Metadata.IEntityType;
using IReadOnlyProperty = Microsoft.EntityFrameworkCore.Metadata.IProperty;
#endif

namespace OpenRiaServices.Server.EntityFrameworkCore
{
    /// <summary>
    /// Metadata context for LINQ To Entities domain services
    /// </summary>
    internal class EFCoreTypeDescriptionContext
    {
        private readonly Type _contextType;
        private IModel _model;

        /// <summary>
        /// Constructor that accepts a LINQ To Entities context type
        /// </summary>
        /// <param name="contextType">The ObjectContext Type</param>
        public EFCoreTypeDescriptionContext(Type contextType)
        {
            if (contextType == null)
            {
                throw new ArgumentNullException(nameof(contextType));
            }
            _contextType = contextType;
        }

        public IModel Model
        {
            get
            {
                if (_model == null)
                {
                    // TODO: Investigate to use compiled models in future (compiled models are only availible in latest versions of ef core)
                    var dbContext = (DbContext)Activator.CreateInstance(_contextType);
                    _model = dbContext.Model;
                }
                return _model;
            }
        }

        // Verify that full name is not null since Model.FindEntityType throws argument exception if full name is null
        public IReadOnlyEntityType GetEntityType(Type type) => type?.FullName != null ? Model.FindEntityType(type) : null;

        /// <summary>
        /// Creates an AssociationAttribute for the specified navigation property
        /// </summary>
        /// <param name="navigationProperty">The navigation property that corresponds to the association (it identifies the end points)</param>
        /// <returns>A new AssociationAttribute that describes the given navigation property association</returns>
        internal static EntityAssociationAttribute CreateAssociationAttribute(IReadOnlyNavigation navigationProperty)
        {
            var fk = navigationProperty.ForeignKey;

            string[] thisKey;
            string[] otherKey;
            string name = fk.GetConstraintName();

#if NETFRAMEWORK
            if (navigationProperty.IsDependentToPrincipal())
#else
            if (navigationProperty.IsOnDependent)
#endif
            {
                thisKey = FormatMemberList(fk.Properties);
                otherKey = FormatMemberList(fk.PrincipalKey.Properties);
            }
            else
            {
                Debug.Assert(fk.PrincipalEntityType == navigationProperty.DeclaringEntityType);

                thisKey = FormatMemberList(fk.PrincipalKey.Properties);
                otherKey = FormatMemberList(fk.Properties);
                Debug.Assert(fk.IsOwnership == fk.DeclaringEntityType.IsOwned());

                // In case there are multiple navigation properties to Owned entities
                // and they have explicity defined keys (they mirror the owners key) then
                // they will have the same foreign key name and we have to make them unique
                if (fk.DeclaringEntityType.IsOwned())
                {
                    name += "|owns:" + navigationProperty.Name;
                }
            }

            return new EntityAssociationAttribute(name, thisKey, otherKey)
            {
                IsForeignKey = IsForeignKey(navigationProperty)
            };
        }

#if NETFRAMEWORK
        private static bool IsForeignKey(IReadOnlyNavigation navigationProperty) => navigationProperty.IsDependentToPrincipal();
#else
        private static bool IsForeignKey(IReadOnlyNavigation navigationProperty) => navigationProperty.IsOnDependent;
#endif

        /// <summary>
        ///  Formats the list of members into an array of strings
        /// </summary>
        protected static string[] FormatMemberList(IReadOnlyList<IReadOnlyProperty> members)
        {
            string[] result = new string[members.Count];
            for(int i=0; i < members.Count; ++i)
                result[i] = members[i].Name;
            return result;
        }
    }
}
