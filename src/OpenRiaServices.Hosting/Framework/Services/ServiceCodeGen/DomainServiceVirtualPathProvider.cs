using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using OpenRiaServices.DomainServices.Server;
using System.Web;
using System.Web.Caching;
using System.Web.Compilation;
using System.Web.Hosting;

namespace OpenRiaServices.DomainServices.Hosting
{
    /// <summary>
    /// Takes care of generating a service file when a physical one doesn't exist.
    /// </summary>
    internal class DomainServiceVirtualPathProvider : VirtualPathProvider
    {
        internal const string DomainServicesDirectory = "~/Services/";

        private static readonly object vppRegistrationLock = new object();
        private static bool vppRegistered;

        private static Dictionary<string, Type> domainServiceTypes;

        public static void Register()
        {
            if (DomainServiceVirtualPathProvider.vppRegistered == false)
            {
                lock (DomainServiceVirtualPathProvider.vppRegistrationLock)
                {
                    if (DomainServiceVirtualPathProvider.vppRegistered == false)
                    {
                        DomainServiceVirtualPathProvider.EnsureDomainServiceTypes();
                        HostingEnvironment.RegisterVirtualPathProvider(new DomainServiceVirtualPathProvider());
                        DomainServiceVirtualPathProvider.vppRegistered = true;
                    }
                }
            }
        }

        public override string CombineVirtualPaths(string basePath, string relativePath)
        {
            if (this.Previous != null)
            {
                return this.Previous.CombineVirtualPaths(basePath, relativePath);
            }

            return base.CombineVirtualPaths(basePath, relativePath);
        }

        public override bool FileExists(string virtualPath)
        {
            if (DomainServiceVirtualPathProvider.OwnsFile(virtualPath))
            {
                return true;
            }

            return base.FileExists(virtualPath);
        }

        public override CacheDependency GetCacheDependency(string virtualPath, IEnumerable virtualPathDependencies, DateTime utcStart)
        {
            if (DomainServiceVirtualPathProvider.OwnsFile(virtualPath))
            {
                // Return a no-op cache dependency such that ASP.NET doesn't create a FileSystemWatcher 
                // to listen for changes to this file.
                return new DomainServiceCacheDependency();
            }

            return base.GetCacheDependency(virtualPath, virtualPathDependencies, utcStart);
        }

        public override string GetCacheKey(string virtualPath)
        {
            if (DomainServiceVirtualPathProvider.OwnsFile(virtualPath))
            {
                // We were able to provide the file, so we're responsible for returning a cache key.
                return virtualPath;
            }

            return base.GetCacheKey(virtualPath);
        }

        public override VirtualFile GetFile(string virtualPath)
        {
            KeyValuePair<string, Type> domainServiceEntry;
            if (DomainServiceVirtualPathProvider.OwnsFile(virtualPath, out domainServiceEntry))
            {
                return new DomainServiceVirtualFile(domainServiceEntry.Value, virtualPath);
            }

            return base.GetFile(virtualPath);
        }

        private static void EnsureDomainServiceTypes()
        {
            if (DomainServiceVirtualPathProvider.domainServiceTypes != null)
            {
                return;
            }

            Dictionary<string, Type> types = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

            Type domainServiceBaseType = typeof(DomainService);
            IEnumerable<Assembly> assemblies = BuildManager.GetReferencedAssemblies().Cast<Assembly>();
            foreach (Assembly assembly in assemblies)
            {
                if (!TypeUtility.CanContainDomainServiceImplementations(assembly))
                    continue;

                Type[] exportedTypes = null;
                try
                {
                    exportedTypes = assembly.GetExportedTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    exportedTypes = ex.Types;
                }
                catch (Exception ex)
                {
                    if (ex.IsFatal())
                    {
                        throw;
                    }
                    // If we're unable to load the assembly, ignore it for now.
                }

                if (exportedTypes != null)
                {
                    foreach (Type exportedType in exportedTypes)
                    {
                        if (exportedType.IsAbstract || exportedType.IsInterface || exportedType.IsValueType || !domainServiceBaseType.IsAssignableFrom(exportedType))
                        {
                            continue;
                        }

                        if (TypeDescriptor.GetAttributes(exportedType)[typeof(EnableClientAccessAttribute)] == null)
                        {
                            continue;
                        }

                        string name = GetCanonicalFileName(exportedType);
                        if (types.ContainsKey(name))
                        {
                            throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resource.DomainServiceVirtualPathProvider_DuplicateDomainServiceName, exportedType.AssemblyQualifiedName, types[name].AssemblyQualifiedName));
                        }
                        types.Add(name, exportedType);
                    }
                }
            }

            DomainServiceVirtualPathProvider.domainServiceTypes = types;
        }

        private static string GetCanonicalFileName(Type domainServiceType)
        {
            return domainServiceType.FullName.Replace('.', '-') + ServiceUtility.ServiceFileExtension;
        }

        // Checks whether the specified virtual path represents a service that we generate(d) on the fly.
        private static bool OwnsFile(string virtualPath)
        {
            if (!virtualPath.EndsWith(ServiceUtility.ServiceFileExtension, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string fileName = VirtualPathUtility.GetFileName(virtualPath);
            bool isDomainService = domainServiceTypes.ContainsKey(fileName);

            // Make sure we avoid doing any expensive lookups if we don't need to.
            if (!isDomainService)
            {
                return false;
            }

            // Verify this is a request to a file in the domain services directory.
            string directoryPath = VirtualPathUtility.ToAppRelative(VirtualPathUtility.GetDirectory(virtualPath));
            if (!directoryPath.Equals(DomainServiceVirtualPathProvider.DomainServicesDirectory))
            {
                return false;
            }

            // If a physical file with this name exists, let that file get through.
            string filePath = HttpContext.Current.Server.MapPath(virtualPath);
            if (File.Exists(filePath))
            {
                return false;
            }

            return isDomainService;
        }

        // Checks whether the specified virtual path represents a service that we generate(d) on the fly.
        private static bool OwnsFile(string virtualPath, out KeyValuePair<string, Type> domainServiceEntry)
        {
            domainServiceEntry = default(KeyValuePair<string, Type>);
            if (!virtualPath.EndsWith(ServiceUtility.ServiceFileExtension, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string fileName = VirtualPathUtility.GetFileName(virtualPath);

            Type domainServiceType;
            bool isDomainService = domainServiceTypes.TryGetValue(fileName, out domainServiceType);

            // Make sure we avoid doing any expensive lookups if we don't need to.
            if (!isDomainService)
            {
                return false;
            }

            // Verify this is a request to a file in the domain services directory.
            string directoryPath = VirtualPathUtility.ToAppRelative(VirtualPathUtility.GetDirectory(virtualPath));
            if (!directoryPath.Equals(DomainServiceVirtualPathProvider.DomainServicesDirectory))
            {
                return false;
            }

            // If a physical file with this name exists, let that file get through.
            string filePath = HttpContext.Current.Server.MapPath(virtualPath);
            if (File.Exists(filePath))
            {
                return false;
            }

            domainServiceEntry = new KeyValuePair<string, Type>(fileName, domainServiceType);
            return true;
        }

        // Checks whether the request to the specified path should be rewritten to the location where we 
        // require the service to be hosted from.
        internal static bool ShouldRewritePath(string virtualPath, out KeyValuePair<string, Type> domainServiceEntry)
        {
            domainServiceEntry = default(KeyValuePair<string, Type>);
            if (!virtualPath.EndsWith(ServiceUtility.ServiceFileExtension, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string fileName = VirtualPathUtility.GetFileName(virtualPath);

            Type domainServiceType;
            bool isDomainService = domainServiceTypes.TryGetValue(fileName, out domainServiceType);

            // Make sure we avoid doing any expensive lookups if we don't need to.
            if (!isDomainService)
            {
                return false;
            }

            // If this is a request to a file in the domain services directory, don't rewrite.
            string directoryPath = VirtualPathUtility.ToAppRelative(VirtualPathUtility.GetDirectory(virtualPath));
            if (directoryPath.Equals(DomainServiceVirtualPathProvider.DomainServicesDirectory))
            {
                return false;
            }

            // If a physical file with this name exists, let that file get through.
            string filePath = HttpContext.Current.Server.MapPath(virtualPath);
            if (File.Exists(filePath))
            {
                return false;
            }

            domainServiceEntry = new KeyValuePair<string, Type>(fileName, domainServiceType);
            return true;
        }

        /// <summary>
        /// This is a no-op cache dependency.
        /// </summary>
        private class DomainServiceCacheDependency : CacheDependency
        {
            public DomainServiceCacheDependency()
            {
            }
        }
    }
}
