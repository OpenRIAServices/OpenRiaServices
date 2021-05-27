﻿namespace OpenRiaServices.VisualStudio.DomainServices.Tools
{
    /// <summary>
    /// This class is reserved for constants that relate to imports, class names, enums, etc.
    /// </summary>
    /// <remarks>
    /// This class exists because we can't take a static dependency on the Business Logic assemblies,
    /// or we would need to GAC them.   Hence, all code-gen relies on string names of types and enums.
    /// </remarks>
    public static class BusinessLogicClassConstants
    {
        // TODO: consider way to know type and namespace names using stub file at compile time

        /// <summary>
        /// These imports will be added to all namespaces generated in the business logic class file
        /// </summary>
        public static readonly string[] FixedImports = 
        { 
            "System", 
            "System.Collections.Generic", 
            "System.ComponentModel",
            "System.ComponentModel.DataAnnotations",
            "System.Linq", 
            "OpenRiaServices.Server",
        };

        /// <summary>
        /// Additional imports for LinqToSql
        /// </summary>
        public static readonly string[] LinqToSqlImports = 
        { 
            "System.Data.Linq",
            "OpenRiaServices.LinqToSql",
        };

        /// <summary>
        /// Additional imports for LinqToEntities
        /// </summary>
        public static readonly string[] LinqToEntitiesImports = 
        { 
            "System.Data.Entity",
            "OpenRiaServices.EntityFramework",
        };

        /// <summary>
        /// Additional imports for LinqToEntities
        /// </summary>
        public static readonly string[] LinqToEntitiesDbImports = 
        { 
            "System.Data",
            "System.Data.Entity",
            "System.Data.Entity.Infrastructure",
            "OpenRiaServices.EntityFramework"
        };

        /// <summary>
        /// The assembly name for the LinqToSql DomainService provider.
        /// </summary>
        public static readonly string LinqToSqlDomainServiceAssemblyName = "OpenRiaServices.LinqToSql";

        /// <summary>
        /// The type name of the domain service factory we will register in the web.config
        /// </summary>
        public static readonly string DomainServiceModuleTypeName = "OpenRiaServices.Hosting.DomainServiceHttpModule";

        /// <summary>
        /// The assembly name of the assembly containing the domain service factory type
        /// </summary>
        public static readonly string HostingAssemblyName = "OpenRiaServices.Hosting";

        /// <summary>
        /// Effectively typeof(DomainService).Name
        /// </summary>
        public static readonly string DomainServiceTypeName = "DomainService";

        /// <summary>
        /// Effectively typeof(LinqToSqlDomainService).Name
        /// </summary>
        public static readonly string LinqToSqlDomainServiceTypeName = "LinqToSqlDomainService";

        /// <summary>
        /// Effectively typeof(LinqToEntitiesDomainService).Name
        /// </summary>
        public static readonly string LinqToEntitiesDomainServiceTypeName = "LinqToEntitiesDomainService";

        /// <summary>
        /// Effectively typeof(EnableClientAccessAttribute).Name minus "Attribute"
        /// </summary>
        public static readonly string EnableClientAccessAttributeTypeName = "EnableClientAccess";

        /// <summary>
        /// Effectively typeof(MetadataTypeAttribute).Name
        /// </summary>
        public static readonly string MetadataTypeAttributeTypeName = "MetadataTypeAttribute";

        /// <summary>
        /// The name of the module we will register in web.config.  Does not need to match type names
        /// </summary>
        public static readonly string DomainServiceModuleName = "DomainServiceModule";

        /// <summary>
        /// One of the pre-conditions that can be applied to http modules.
        /// </summary>
        public static readonly string ManagedHandler = "managedHandler";

        /// <summary>
        /// Name of the OData endpoint in system.serviceModel/domainServices
        /// </summary>
        public static readonly string ODataEndpointName = "OData";

        // DbContext related constants

        /// <summary>
        /// Effectively typeof(DbDomainService).Name
        /// </summary>
        public static readonly string DbDomainServiceTypeName = "DbDomainService";

        /// <summary>
        /// Effectively typeof(DbDomainService).AssemblyName
        /// </summary>
        public const string DbDomainServiceAssemblyName = DbDomainServiceAssemblyShortName ;

        /// <summary>
        /// Effectively the name portion of typeof(DbDomainService).AssemblyName (i.e. without Version, Culture, etc.)
        /// </summary>
        public const string DbDomainServiceAssemblyShortName = @"OpenRiaServices.EntityFramework";
        
        /// <summary>
        /// Effectively typeof(DbContext).FullName
        /// </summary>
        public const string DbContextTypeName = @"System.Data.Entity.DbContext";

        /// <summary>
        /// Effectively typeof(DbContext).Namespace
        /// </summary>
        public const string DbContextNamespace = @"OpenRiaServices.EntityFramework";

        /// <summary>
        /// Effectively typeof(IObjectContextAdapter).FullName
        /// </summary>
        public const string IObjectContextAdapterTypeName = @"System.Data.Entity.Infrastructure.IObjectContextAdapter";
        
        /// <summary>
        /// Effectively typeof(DbSet).FullName
        /// </summary>
        public const string DbSetTypeName = @"System.Data.Entity.DbSet`1";

        /// <summary>
        /// Effectively typeof(EdmMetadata).FullName
        /// </summary>
        public const string EdmMetadataTypeName = @"System.Data.Entity.Infrastructure.EdmMetadata";
    }
}
