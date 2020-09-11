using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using OpenRiaServices.DomainServices;
using OpenRiaServices.DomainServices.Hosting;
using OpenRiaServices.DomainServices.Server;

namespace OpenRiaServices.DomainServices.Tools
{
    /// <summary>
    /// Represents a catalog of DomainServices.
    /// </summary>
    internal class DomainServiceCatalog
    {
        private readonly HashSet<string> _assembliesToLoad;
        private Dictionary<Assembly, bool> _loadedAssemblies;
        private readonly List<DomainServiceDescription> _domainServiceDescriptions = new List<DomainServiceDescription>();
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainServiceCatalog"/> class with the specified input and reference assemblies
        /// </summary>
        /// <param name="assembliesToLoad">The set of assemblies to load (includes all known assemblies and references).</param>
        /// <param name="logger">logger for logging messages while processing</param>
        /// <exception cref="ArgumentNullException"> is thrown if <paramref name="assembliesToLoad"/> or <paramref name="logger"/> is null.</exception>
        public DomainServiceCatalog(IEnumerable<string> assembliesToLoad, ILogger logger)
        {
            if (assembliesToLoad == null)
            {
                throw new ArgumentNullException(nameof(assembliesToLoad));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            this._logger = logger;

            this._assembliesToLoad = new HashSet<string>(assembliesToLoad, StringComparer.OrdinalIgnoreCase);

            this.LoadAllAssembliesAndSetAssemblyResolver();
            this.AddDomainServiceDescriptions();
            this.ValidateDomainServiceInheritance();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainServiceCatalog"/> class that permits code gen over a single domain service
        /// </summary>
        /// <param name="domainServiceType">a domain service type to generate code for</param>
        /// <param name="logger">logger for logging messages while processing</param>
        /// <exception cref="ArgumentNullException"> is thrown if <paramref name="domainServiceType"/> or <paramref name="logger"/> is null.</exception>
        public DomainServiceCatalog(Type domainServiceType, ILogger logger)
        {
            if (domainServiceType == null)
            {
                throw new ArgumentNullException(nameof(domainServiceType));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            this._logger = logger;

            this.AddDomainServiceType(domainServiceType);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainServiceCatalog"/> class that permits code gen over a list of domain services
        /// </summary>
        /// <param name="domainServiceTypes">list of domain service types to generate code for</param>
        /// <param name="logger">logger for logging messages while processing</param>
        /// <exception cref="ArgumentNullException"> is thrown if <paramref name="domainServiceTypes"/> or <paramref name="logger"/> is null.</exception>
        public DomainServiceCatalog(IEnumerable<Type> domainServiceTypes, ILogger logger)
        {
            if (domainServiceTypes == null)
            {
                throw new ArgumentNullException(nameof(domainServiceTypes));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            this._logger = logger;

            foreach (Type domainServiceType in domainServiceTypes)
            {
                this.AddDomainServiceType(domainServiceType);
            }
            this.ValidateDomainServiceInheritance();
        }

        private void AddDomainServiceType(Type domainServiceType)
        {
            bool enableClientAccess = TypeDescriptor.GetAttributes(domainServiceType)[typeof(EnableClientAccessAttribute)] != null;

            // The DomainService Type must be marked with EnableClientAccess attribute
            if (!enableClientAccess)
            {
                this.LogError(string.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_InvalidDomainServiceType, domainServiceType.Name));
                return;
            }

            // EF CodeFirst tries to initialize the Database when we new up a DbContext. We want to avoid that at design time.
            // So disable the Database initializer if it is a DbContext based DomainService.
            Type contextType = DbContextUtilities.GetDbContextType(domainServiceType);
            if (contextType != null)
            {
                // From the context type, get typeof(DbContext)
                Type dbContextTypeRef = DbContextUtilities.GetDbContextTypeReference(contextType);
                System.Diagnostics.Debug.Assert(dbContextTypeRef != null, "If we have the DbContext type, typeof(DbContext) should not be null");
                DbContextUtilities.SetDbInitializer(contextType, dbContextTypeRef, null);
            }

            try
            {
                DomainServiceDescription description = this.GetProviderDescription(domainServiceType);
                if (description != null)
                {
                    if (description.EntityTypes.Any() || description.DomainOperationEntries.Any(p => p.Operation == DomainOperation.Invoke))
                    {
                        this._domainServiceDescriptions.Add(description);
                    }
                    else
                    {
                        this.LogWarning(string.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_EmptyDomainService, domainServiceType.Name));
                    }
                }
            }
            catch (ArgumentException ae)
            {
                // Our DPD layer reports problems only by throwing.
                // Translate these exceptions into clean error logs
                // so that they appear in the Error window in VS
                this.LogError(ae.Message);
                
            }
            catch (InvalidOperationException ioe)
            {
                this.LogError(ioe.Message);
            }
        }

        /// <summary>
        /// Log an error message
        /// </summary>
        /// <param name="message">message to be logged</param>
        private void LogError(string message)
        {
            if (this._logger != null)
            {
                this._logger.LogError(message);
            }
        }

        /// <summary>
        /// Log an error exception
        /// </summary>
        /// <param name="ex">Exception to be logged</param>
        private void LogException(Exception ex)
        {
            if (this._logger != null)
            {
                this._logger.LogException(ex);
            }
        }

        /// <summary>
        /// Log a warning message
        /// </summary>
        /// <param name="message">message to be logged</param>
        private void LogWarning(string message)
        {
            if (this._logger != null)
            {
                this._logger.LogWarning(message);
            }
        }

        /// <summary>
        /// Gets a collection of domain service descriptions
        /// </summary>
        public ICollection<DomainServiceDescription> DomainServiceDescriptions
        {
            get
            {
                return this._domainServiceDescriptions;
            }
        }

        /// <summary>
        /// Looks at all loaded assemblies and adds DomainServiceDescription for each DomainService found
        /// </summary>
        private void AddDomainServiceDescriptions()
        {
            foreach (KeyValuePair<Assembly, bool> pair in this._loadedAssemblies)
            {
                // Performance optimization: standard Microsoft assemblies are excluded from this search
                // and assembly must reference OpenRiaServices.DomainServices.Server
                if (pair.Value)
                {
                    // Utility autorecovers and logs for common exceptions
                    IEnumerable<Type> types = AssemblyUtilities.GetExportedTypes(pair.Key, this._logger);

                    foreach (Type t in types)
                    {
                        if (typeof(DomainService).IsAssignableFrom(t) &&
                            TypeDescriptor.GetAttributes(t)[typeof(EnableClientAccessAttribute)] != null)
                        {
                            this.AddDomainServiceType(t);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Call GetDescription for the specified domain service type.
        /// </summary>
        /// <param name="providerType">type of <see cref="DomainService"/></param>
        /// <returns>the <see cref="DomainServiceDescription"/> corresponding 
        /// to the specified provider type. Null is returned if an error has occurred.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Catching all exception in GetDescription so that we can log the error without crashing the build task")]
        private DomainServiceDescription GetProviderDescription(Type providerType)
        {
            try
            {               
                return DomainServiceDescription.GetDescription(providerType);
            }
            catch (Exception ex)
            {
                if (ex.IsFatal())
                {
                    throw;
                }
                this.LogError(ex.Message);
            }
            return null;
        }

        /// <summary>
        /// Invoked once to force load all assemblies into an analysis unit
        /// </summary>
        private void LoadAllAssembliesAndSetAssemblyResolver()
        {
            this._loadedAssemblies = new Dictionary<Assembly, bool>();

            foreach (string assemblyName in this._assembliesToLoad)
            {
                Assembly assembly = AssemblyUtilities.LoadAssembly(assemblyName, this._logger);
                if (assembly != null)
                {
                    // The bool value indicates whether this assembly should be searched for a DomainService
                    this._loadedAssemblies[assembly] = TypeUtility.CanContainDomainServiceImplementations(assembly);
                }
            }

            AssemblyUtilities.SetAssemblyResolver(this._loadedAssemblies.Keys);
        }

        /// <summary>
        /// Validate that EnableClientAccess appears at the leaf in a DomainService inheritance chain.
        /// </summary>
        private void ValidateDomainServiceInheritance()
        {
            HashSet<Type> domainServices = new HashSet<Type>(this.DomainServiceDescriptions.Select(d => d.DomainServiceType));

            foreach (var domainServiceType in domainServices)
            {
                Type baseType = domainServiceType.BaseType;

                while (baseType != typeof(DomainService))
                {
                    // If the base type does not have EnableClientAttribute, stop looking.
                    if (TypeDescriptor.GetAttributes(domainServiceType)[typeof(EnableClientAccessAttribute)] == null)
                    {
                        break;
                    }

                    // Two code generated domain services have a ancestor-decendant relationship, log the error.
                    if (domainServices.Contains(baseType))
                    {
                        this.LogError(string.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_DomainService_Inheritance_Not_Allowed, domainServiceType, baseType));
                        break;
                    }

                    baseType = baseType.BaseType;
                }
            }
        }
    }
}
