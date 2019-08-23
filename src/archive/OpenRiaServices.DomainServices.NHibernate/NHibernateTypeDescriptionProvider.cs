using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using NHibernate.Cfg;
using OpenRiaServices.DomainServices.Server;

namespace OpenRiaServices.DomainServices.NHibernate
{
    /// <summary>
    /// DomainServiceDescriptionProvider used to extend Type description by applying additional
    /// metadata inferred from NHibernate mapping
    /// </summary>
    public class NHibernateTypeDescriptionProvider : DomainServiceDescriptionProvider
    {
        private readonly Dictionary<Type, ICustomTypeDescriptor> _descriptors =
            new Dictionary<Type, ICustomTypeDescriptor>();

        private readonly Type _domainServiceType;
        private readonly Configuration _nhConfiguration;

        public NHibernateTypeDescriptionProvider(Type domainServiceType, DomainServiceDescriptionProvider parent, Configuration configuration)
            : base(domainServiceType, parent)
        {
            _domainServiceType = domainServiceType;
            _nhConfiguration = configuration;
        }

        public override ICustomTypeDescriptor GetTypeDescriptor(Type type, ICustomTypeDescriptor parent)
        {
            ICustomTypeDescriptor td;
            if (!_descriptors.TryGetValue(type, out td))
            {
                // call base in order to keep chained the TypeDescriptors
                parent = base.GetTypeDescriptor(type, parent);
                //Assuming that an eventual metadata class is defined in the same assembly of the domain service class
                //and has the same name of the class + _Metadata; i.e. Customer => Customer_Metadata
                Type metaDataType = _domainServiceType.Assembly.GetType(type.FullName + "_Metadata");

                if (_nhConfiguration.ClassMappings.Count(x => x.MappedClass.FullName == type.FullName) > 0)
                    td = new NHibernateTypeDescriptor(type, parent, _nhConfiguration, metaDataType);
                else
                    td = parent;

                _descriptors[type] = td;
            }
            return td;
        }
    }
}
