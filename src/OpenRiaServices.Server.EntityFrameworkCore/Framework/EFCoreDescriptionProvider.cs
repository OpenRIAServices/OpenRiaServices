﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.EntityFrameworkCore.Metadata;

namespace OpenRiaServices.Server.EntityFrameworkCore
{
    internal class EFCoreDescriptionProvider : DomainServiceDescriptionProvider
    {
        private static Dictionary<Type, EFCoreTypeDescriptionContext> tdpContextMap = new Dictionary<Type, EFCoreTypeDescriptionContext>();
        private readonly EFCoreTypeDescriptionContext _typeDescriptionContext;
        private readonly Dictionary<Type, ICustomTypeDescriptor> _descriptors = new Dictionary<Type, ICustomTypeDescriptor>();

        public EFCoreDescriptionProvider(Type domainServiceType, Type contextType, DomainServiceDescriptionProvider parent)
            : base(domainServiceType, parent)
        {
            lock (tdpContextMap)
            {
                if (!tdpContextMap.TryGetValue(contextType, out _typeDescriptionContext))
                {
                    // create and cache a context for this provider type
                    _typeDescriptionContext = new EFCoreTypeDescriptionContext(contextType);
                    tdpContextMap.Add(contextType, _typeDescriptionContext);
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
            if (!_descriptors.TryGetValue(objectType, out td))
            {
                // call into base so the TDs are chained
                parent = base.GetTypeDescriptor(objectType, parent);

                // TODO: Check if ef model
                var model = _typeDescriptionContext.Model;
                if (model != null && model.FindEntityType(objectType.FullName) is IEntityType entityType)
                {
                    // TODO: ...
                    // only add an LTE TypeDescriptor if the type is an EF Entity or ComplexType
                    td = new EFCoreTypeDescriptor(_typeDescriptionContext, entityType, parent);
                }
                else
                {
                    td = parent;
                }

                _descriptors[objectType] = td;
            }

            return td;
        }

        public override bool LookupIsEntityType(Type type)
        {
            // TODO: Check if ef model, need to double check it fails for non- entity types
            return _typeDescriptionContext.GetEntityType(type) != null;
        }
    }
}