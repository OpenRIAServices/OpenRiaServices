using System;
using System.IO;
using NHibernate.Cfg;
using OpenRiaServices.DomainServices.Server;

namespace OpenRiaServices.DomainServices.NHibernate
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class NHibernateMetadataProviderAttribute : DomainServiceDescriptionProviderAttribute
    {
        readonly string _nHibernateConfigurationPath;
        /// <summary>
        /// Instantiate a new NHibernateMetadataProviderAttribute using the specified configuration
        /// </summary>
        /// <param name="nHibernateConfigurationPath">full path to the nhibernate configuration file</param>
        public NHibernateMetadataProviderAttribute(string nHibernateConfigurationPath)
            : base(typeof(NHibernateTypeDescriptionProvider))
        {
            _nHibernateConfigurationPath = nHibernateConfigurationPath;
        }

        /// <summary>
        /// Instantiate a new NHibernateMetadataProviderAttribute assuming that the configuration file is in the Current AppDomain.BaseDirectory
        /// </summary>
        public NHibernateMetadataProviderAttribute()
            : base(typeof(NHibernateTypeDescriptionProvider))
        {

            //Assume current folder as the path to look for nHibernateConfiguration
            _nHibernateConfigurationPath = AppDomain.CurrentDomain.BaseDirectory + "hibernate.cfg.xml";

        }
        public override DomainServiceDescriptionProvider CreateProvider(Type domainServiceType, DomainServiceDescriptionProvider parent)
        {
            string currentDir = AppDomain.CurrentDomain.BaseDirectory;
            Configuration cfg = null;
            try
            {
                cfg = new Configuration();
                cfg.Configure(_nHibernateConfigurationPath);
                cfg.AddDirectory(new DirectoryInfo(currentDir));
            }
            catch
            {
                throw;
            }

            return new NHibernateTypeDescriptionProvider(domainServiceType, parent, cfg);
        }
    }
}
