using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

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
#if NETSTANDARD2_0
        public IEntityType GetEntityType(Type type) => type?.FullName != null ? Model.FindEntityType(type) : null;
#else
        public IReadOnlyEntityType GetEntityType(Type type) => type?.FullName != null ? ((IReadOnlyModel)Model).FindEntityType(type) : null;
#endif

        /// <summary>
        /// Creates an AssociationAttribute for the specified navigation property
        /// </summary>
        /// <param name="navigationProperty">The navigation property that corresponds to the association (it identifies the end points)</param>
        /// <returns>A new AssociationAttribute that describes the given navigation property association</returns>
        internal AssociationAttribute CreateAssociationAttribute(INavigation navigationProperty)
        {
            var fk = navigationProperty.ForeignKey;

            string thisKey;
            string otherKey;
#if NETSTANDARD2_0
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
            }

            var assocAttrib = new AssociationAttribute(fk.GetConstraintName(), thisKey, otherKey);
            assocAttrib.IsForeignKey = IsForeignKey(navigationProperty);
            return assocAttrib;
        }

#if NETSTANDARD2_0
        private static bool IsForeignKey(INavigation navigationProperty) => navigationProperty.IsDependentToPrincipal();
#else
        private static bool IsForeignKey(INavigation navigationProperty) => navigationProperty.IsOnDependent;
#endif


        /// <summary>
        /// Comma delimits the specified member name collection
        /// </summary>
        /// <param name="members">A collection of members.</param>
        /// <returns>A comma delimited list of member names.</returns>
        protected static string FormatMemberList(IEnumerable<IProperty> members)
        {
            string memberList = string.Empty;
            foreach (var prop in members)
            {
                if (memberList.Length > 0)
                {
                    memberList += ",";
                }
                memberList += prop.Name;
            }
            return memberList;
        }
    }
}
