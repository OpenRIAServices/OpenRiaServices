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
        //      private readonly Dictionary<string, AssociationInfo> _associationMap = new Dictionary<string, AssociationInfo>();
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
                    // TODO: Is there a smarter way ?? 
                    var dbContext = (DbContext)Activator.CreateInstance(_contextType);
                    _model = dbContext.Model;
                }
                return _model;
            }
        }

        public IEntityType GetEntityType(Type type) => Model.FindEntityType(type);

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
            if (navigationProperty.IsDependentToPrincipal())
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
            assocAttrib.IsForeignKey = navigationProperty.IsDependentToPrincipal(); // TODO:  isForeignKey;
            return assocAttrib;
        }

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
