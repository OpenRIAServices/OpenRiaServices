namespace Microsoft.VisualStudio.ServiceModel.DomainServices.Tools
{
    /// <summary>
    /// This class is reserved for constants that relate to imports, class names, enums, etc.
    /// </summary>
    /// <remarks>
    /// This class exists because we can't take a static dependency on the Business Logic assemblies,
    /// or we would need to GAC them.   Hence, all code-gen relies on string names of types and enums.
    /// </remarks>
    internal static class BusinessLogicClassConstants
    {
        // TODO: consider way to know type and namespace names using stub file at compile time

        /// <summary>
        /// These imports will be added to all namespaces generated in the business logic class file
        /// </summary>
        internal static readonly string[] FixedImports = 
        { 
            "System", 
            "System.Collections.Generic", 
            "System.ComponentModel",
            "System.ComponentModel.DataAnnotations",
            "System.Linq", 
            "System.ServiceModel.DomainServices.Hosting",
            "System.ServiceModel.DomainServices.Server",
        };

        /// <summary>
        /// Additional imports for LinqToSql
        /// </summary>
        internal static readonly string[] LinqToSqlImports = 
        { 
            "System.Data.Linq",
            "Microsoft.ServiceModel.DomainServices.LinqToSql",
        };

        /// <summary>
        /// Additional imports for LinqToEntities
        /// </summary>
        internal static readonly string[] LinqToEntitiesImports = 
        { 
            "System.Data",
            "System.ServiceModel.DomainServices.EntityFramework",
        };

        /// <summary>
        /// Additional imports for LinqToEntities
        /// </summary>
        internal static readonly string[] LinqToEntitiesDbImports = 
        { 
            "System.Data",
            "System.Data.Entity.Infrastructure",
            "System.ServiceModel.DomainServices.EntityFramework"
        };

        /// <summary>
        /// The assembly name for the LinqToSql DomainService provider.
        /// </summary>
        internal static readonly string LinqToSqlDomainServiceAssemblyName = "Microsoft.ServiceModel.DomainServices.LinqToSql";

        /// <summary>
        /// The type name of the domain service factory we will register in the web.config
        /// </summary>
        internal static readonly string DomainServiceModuleTypeName = "System.ServiceModel.DomainServices.Hosting.DomainServiceHttpModule";

        /// <summary>
        /// The assembly name of the assembly containing the domain service factory type
        /// </summary>
        internal static readonly string HostingAssemblyName = "System.ServiceModel.DomainServices.Hosting";

        /// <summary>
        /// Effectively typeof(DomainService).Name
        /// </summary>
        internal static readonly string DomainServiceTypeName = "DomainService";

        /// <summary>
        /// Effectively typeof(LinqToSqlDomainService).Name
        /// </summary>
        internal static readonly string LinqToSqlDomainServiceTypeName = "LinqToSqlDomainService";

        /// <summary>
        /// Effectively typeof(LinqToEntitiesDomainService).Name
        /// </summary>
        internal static readonly string LinqToEntitiesDomainServiceTypeName = "LinqToEntitiesDomainService";

        /// <summary>
        /// Effectively typeof(EnableClientAccessAttribute).Name minus "Attribute"
        /// </summary>
        internal static readonly string EnableClientAccessAttributeTypeName = "EnableClientAccess";

        /// <summary>
        /// Effectively typeof(MetadataTypeAttribute).Name
        /// </summary>
        internal static readonly string MetadataTypeAttributeTypeName = "MetadataTypeAttribute";

        /// <summary>
        /// The name of the module we will register in web.config.  Does not need to match type names
        /// </summary>
        internal static readonly string DomainServiceModuleName = "DomainServiceModule";

        /// <summary>
        /// One of the pre-conditions that can be applied to http modules.
        /// </summary>
        internal static readonly string ManagedHandler = "managedHandler";

        /// <summary>
        /// Name of the OData endpoint in system.serviceModel/domainServices
        /// </summary>
        internal static readonly string ODataEndpointName = "OData";

        // DbContext related constants

        /// <summary>
        /// Effectively typeof(DbDomainService).Name
        /// </summary>
        internal static readonly string DbDomainServiceTypeName = "DbDomainService";

        /// <summary>
        /// Effectively typeof(DbDomainService).AssemblyName
        /// </summary>
        internal const string DbDomainServiceAssemblyName = DbDomainServiceAssemblyShortName + @", Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";

        /// <summary>
        /// Effectively the name portion of typeof(DbDomainService).AssemblyName (i.e. without Version, Culture, etc.)
        /// </summary>
        internal const string DbDomainServiceAssemblyShortName = @"Microsoft.ServiceModel.DomainServices.EntityFramework";
        
        /// <summary>
        /// Effectively typeof(DbContext).FullName
        /// </summary>
        internal const string DbContextTypeName = @"System.Data.Entity.DbContext";

        /// <summary>
        /// Effectively typeof(DbContext).Namespace
        /// </summary>
        internal const string DbContextNamespace = @"System.ServiceModel.DomainServices.EntityFramework";

        /// <summary>
        /// Effectively typeof(IObjectContextAdapter).FullName
        /// </summary>
        internal const string IObjectContextAdapterTypeName = @"System.Data.Entity.Infrastructure.IObjectContextAdapter";
        
        /// <summary>
        /// Effectively typeof(DbSet).FullName
        /// </summary>
        internal const string DbSetTypeName = @"System.Data.Entity.DbSet`1";

        /// <summary>
        /// Effectively typeof(EdmMetadata).FullName
        /// </summary>
        internal const string EdmMetadataTypeName = @"System.Data.Entity.Infrastructure.EdmMetadata";
    }
}
