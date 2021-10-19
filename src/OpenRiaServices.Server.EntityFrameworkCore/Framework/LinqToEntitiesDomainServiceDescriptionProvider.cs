using System;
using System.Collections.Generic;
using System.ComponentModel;
#if DBCONTEXT
using System.Data.Entity.Core.Metadata.Edm;
#else
using System.Data.Metadata.Edm;
#endif
#if RIACONTRIB
using System.ServiceModel.DomainServices.Server;
#endif
using OpenRiaServices.Server;

namespace OpenRiaServices.EntityFramework
{
    // TODO: Remove and move code to DB context

    internal class LinqToEntitiesDomainServiceDescriptionProvider : DomainServiceDescriptionProvider
    {
        private static Dictionary<Type, LinqToEntitiesTypeDescriptionContext> tdpContextMap = new Dictionary<Type, LinqToEntitiesTypeDescriptionContext>();
        private readonly LinqToEntitiesTypeDescriptionContext _typeDescriptionContext;
        private readonly Dictionary<Type, ICustomTypeDescriptor> _descriptors = new Dictionary<Type, ICustomTypeDescriptor>();

        public LinqToEntitiesDomainServiceDescriptionProvider(Type domainServiceType, Type contextType, DomainServiceDescriptionProvider parent)
            : base(domainServiceType, parent)
        {
            lock (tdpContextMap)
            {
                if (!tdpContextMap.TryGetValue(contextType, out this._typeDescriptionContext))
                {
                    // create and cache a context for this provider type
                    this._typeDescriptionContext = new LinqToEntitiesTypeDescriptionContext(contextType);
                    tdpContextMap.Add(contextType, this._typeDescriptionContext);
                }
            }
        }

        /// <summary>
        /// Returns a custom type descriptor for the specified type (either an entity or complex type).
        /// </summary>
        /// <param name="objectType">Type of object for which we need the descriptor</param>
        /// <param name="parent">The parent type descriptor</param>
        /// <returns>Custom type description for the specified type</returns>
        public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, ICustomTypeDescriptor parent)
        {
            // No need to deal with concurrency... Worst case scenario we have multiple 
            // instances of this thing.
            ICustomTypeDescriptor td = null;
            if (!this._descriptors.TryGetValue(objectType, out td))
            {
                // call into base so the TDs are chained
                parent = base.GetTypeDescriptor(objectType, parent);

                StructuralType edmType = this._typeDescriptionContext.GetEdmType(objectType);
                if (edmType != null && 
                    (edmType.BuiltInTypeKind == BuiltInTypeKind.EntityType || edmType.BuiltInTypeKind == BuiltInTypeKind.ComplexType))
                {
                    // only add an LTE TypeDescriptor if the type is an EF Entity or ComplexType
                    td = new LinqToEntitiesTypeDescriptor(this._typeDescriptionContext, edmType, parent);
                }
                else
                {
                    td = parent;
                }

                this._descriptors[objectType] = td;
            }

            return td;
        }

        public override bool LookupIsEntityType(Type type)
        {
            StructuralType edmType = this._typeDescriptionContext.GetEdmType(type);
            if (edmType != null && edmType.BuiltInTypeKind == BuiltInTypeKind.EntityType)
            {
                return true;
            }
            return false;
        }
    }
}
