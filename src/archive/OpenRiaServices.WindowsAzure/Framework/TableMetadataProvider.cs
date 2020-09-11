using System;
using System.ComponentModel;
using OpenRiaServices.DomainServices.Server;

namespace OpenRiaServices.DomainServices.WindowsAzure
{
    /// <summary>
    /// Attribute that declares a table-specific <see cref="DomainServiceDescriptionProvider"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    internal sealed class TableMetadataProviderAttribute : DomainServiceDescriptionProviderAttribute
    {
        public TableMetadataProviderAttribute()
            : base(typeof(TableMetadataProvider))
        {
        }
    }

    /// <summary>
    /// <see cref="DomainServiceDescriptionProvider"/> that provides metadata for all the entities available
    /// on a specified domain service.
    /// </summary>
    internal class TableMetadataProvider : DomainServiceDescriptionProvider
    {
        public TableMetadataProvider(Type domainServiceType, DomainServiceDescriptionProvider parent)
            : base(domainServiceType, parent)
        {
        }

        public override ICustomTypeDescriptor GetTypeDescriptor(Type type, ICustomTypeDescriptor parent)
        {
            if (this.LookupIsEntityType(type))
            {
                return new TableMetadataTypeDescriptor(type, base.GetTypeDescriptor(type, parent));
            }
            return base.GetTypeDescriptor(type, parent);
        }

        public override bool LookupIsEntityType(Type type)
        {
            return typeof(TableEntity).IsAssignableFrom(type);
        }
    }
}
