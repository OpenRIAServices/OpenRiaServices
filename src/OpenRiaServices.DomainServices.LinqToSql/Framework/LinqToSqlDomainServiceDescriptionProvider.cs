using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Linq.Mapping;
using OpenRiaServices.DomainServices.Server;

namespace OpenRiaServices.DomainServices.LinqToSql
{
    internal class LinqToSqlDomainServiceDescriptionProvider : DomainServiceDescriptionProvider
    {
        private static Dictionary<Type, LinqToSqlTypeDescriptionContext> tdpContextMap = new Dictionary<Type, LinqToSqlTypeDescriptionContext>();
        private readonly LinqToSqlTypeDescriptionContext _typeDescriptionContext;
        private readonly Dictionary<Type, ICustomTypeDescriptor> _descriptors = new Dictionary<Type, ICustomTypeDescriptor>();

        public LinqToSqlDomainServiceDescriptionProvider(Type domainServiceType, Type dataContextType, DomainServiceDescriptionProvider parent)
            : base(domainServiceType, parent)
        {
            lock (tdpContextMap)
            {
                if (!tdpContextMap.TryGetValue(dataContextType, out this._typeDescriptionContext))
                {
                    // create and cache a context for this provider type
                    this._typeDescriptionContext = new LinqToSqlTypeDescriptionContext(dataContextType);
                    tdpContextMap.Add(dataContextType, this._typeDescriptionContext);
                }
            }
        }

        public override bool LookupIsEntityType(Type type)
        {
            MetaType metaType = this._typeDescriptionContext.MetaModel.GetMetaType(type);
            return metaType.IsEntity;
        }

        /// <summary>
        /// Returns a custom type descriptor for the specified entity type
        /// </summary>
        /// <param name="objectType">Type of object for which we need the descriptor</param>
        /// <param name="parent">The parent type descriptor</param>
        /// <returns>a custom type descriptor for the specified entity type</returns>
        public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, ICustomTypeDescriptor parent)
        {
            // No need to deal with concurrency... Worst case scenario we have multiple 
            // instances of this thing.
            ICustomTypeDescriptor td = null;
            if (!this._descriptors.TryGetValue(objectType, out td))
            {
                // call into base so the TDs are chained
                parent = base.GetTypeDescriptor(objectType, parent);

                MetaType metaType = this._typeDescriptionContext.MetaModel.GetMetaType(objectType);
                if (metaType.IsEntity)
                {
                    // only add an LTS TypeDescriptor if the type is a LTS Entity type
                    td = new LinqToSqlTypeDescriptor(this._typeDescriptionContext, metaType, parent);
                }
                else
                {
                    td = parent;
                }

                this._descriptors[objectType] = td;
            }

            return td;
        }
    }
}
