using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Linq.Mapping;
using System.Globalization;
using System.Linq;
using System.Reflection;
using OpenRiaServices.Server;

namespace OpenRiaServices.LinqToSql
{
    /// <summary>
    /// Class that provides the basic metadata interface to a LINQ To SQL data context.
    /// </summary>
    internal class LinqToSqlTypeDescriptionContext : TypeDescriptionContextBase
    {
        private readonly MetaModel _model;
        private readonly Dictionary<MetaAssociation, string> _associationNameMap = new Dictionary<MetaAssociation, string>();

        /// <summary>
        /// Constructor that creates a metadata context for the specified LINQ To SQL domain service type
        /// </summary>
        /// <param name="dataContextType">The DataContext type</param>
        public LinqToSqlTypeDescriptionContext(Type dataContextType)
        {
            if (dataContextType == null)
            {
                throw new ArgumentNullException(nameof(dataContextType));
            }

            System.Data.Linq.DataContext dataContext = null;
            try
            {
                dataContext = (System.Data.Linq.DataContext)Activator.CreateInstance(dataContextType, String.Empty);
            }
            catch (TargetInvocationException tie)
            {
                if (tie.InnerException != null)
                {
                    throw tie.InnerException;
                }
                
                throw;
            }
            this._model = dataContext.Mapping;
        }

        /// <summary>
        /// Gets the MetaModel containing the metadata
        /// </summary>
        public MetaModel MetaModel
        {
            get
            {
                return this._model;
            }
        }

        /// <summary>
        /// Returns an AssociationAttribute for the specified association member
        /// </summary>
        /// <param name="member">The metadata member corresponding to the association member</param>
        /// <returns>The Association attribute</returns>
        public System.ComponentModel.DataAnnotations.AssociationAttribute CreateAssociationAttribute(MetaDataMember member)
        {
            MetaAssociation metaAssociation = member.Association;

            string associationName = this.GetAssociationName(metaAssociation);
            string thisKey = TypeDescriptionContextBase.FormatMemberList(metaAssociation.ThisKey.Select(p => p.Name));
            string otherKey = TypeDescriptionContextBase.FormatMemberList(metaAssociation.OtherKey.Select(p => p.Name));
            System.ComponentModel.DataAnnotations.AssociationAttribute assocAttrib = new System.ComponentModel.DataAnnotations.AssociationAttribute(associationName, thisKey, otherKey);
            assocAttrib.IsForeignKey = metaAssociation.IsForeignKey;

            return assocAttrib;
        }

        /// <summary>
        /// Returns a unique association name for the specified MetaAssociation
        /// </summary>
        /// <param name="metaAssociation">A <see cref="MetaAssociation"/>.</param>
        /// <returns>A <see cref="String"/> containing the association name.</returns>
        private string GetAssociationName(MetaAssociation metaAssociation)
        {
            lock (this._associationNameMap)
            {
                // We need a unique key for this association, so we use the MetaAssociation
                // itself. In the case of bi-directional associations, we use the FK side.
                if (!metaAssociation.IsForeignKey && metaAssociation.OtherMember != null)
                {
                    metaAssociation = metaAssociation.OtherMember.Association;
                }

                string associationName = null;
                if (!this._associationNameMap.TryGetValue(metaAssociation, out associationName))
                {
                    // names are always formatted non-FK side type name followed by FK side type name
                    // For example, the name for both ends of the PurchaseOrder/PurchaseOrderDetail 
                    // association will be PurchaseOrder_PurchaseOrderDetail
                    if (metaAssociation.IsForeignKey)
                    {
                        associationName = string.Format(CultureInfo.InvariantCulture, "{0}_{1}", metaAssociation.OtherType.Name, metaAssociation.ThisMember.DeclaringType.Name);
                    }
                    else
                    {
                        associationName = string.Format(CultureInfo.InvariantCulture, "{0}_{1}", metaAssociation.ThisMember.DeclaringType.Name, metaAssociation.OtherType.Name);
                    }

                    associationName = TypeDescriptionContextBase.MakeUniqueName(associationName, this._associationNameMap.Values);
                    this._associationNameMap[metaAssociation] = associationName;
                }

                return associationName;
            }
        }
    }
}
