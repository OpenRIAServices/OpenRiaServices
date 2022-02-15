using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using OpenRiaServices;
using System.Data.Entity.Core.Metadata.Edm;
using Microsoft.EntityFrameworkCore;

namespace System.Data.Mapping
{
    /// <summary>
    /// EF metadata utilities class.
    /// </summary>
    internal static class MetadataWorkspaceUtilitiesEFCore
    {
        /// <summary>
        /// Creates a metadata workspace for the specified context.
        /// </summary>
        /// <param name="contextType">The type of the object context.</param>
        /// <returns>The metadata workspace.</returns>
        public static MetadataWorkspace CreateMetadataWorkspace(Type contextType)
        {
            MetadataWorkspace metadataWorkspace = null;
#if !DBCONTEXT

            metadataWorkspace = MetadataWorkspaceUtilities.CreateMetadataWorkspaceFromResources(contextType, typeof(ObjectContext));

#else
            metadataWorkspace = MetadataWorkspaceUtilitiesEFCore.CreateMetadataWorkspaceFromResources(contextType, typeof(DbContext));
            if (metadataWorkspace == null && typeof(DbContext).IsAssignableFrom(contextType))
            {
                if (contextType.GetConstructor(Type.EmptyTypes) == null)
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, OpenRiaServices.EntityFrameworkCore.DbResource.DefaultCtorNotFound, contextType.FullName));
                }

                try
                {
                    DbContext dbContext = Activator.CreateInstance(contextType) as DbContext;
                }
                catch (Exception efException)
                {
                    if (efException.IsFatal())
                    {
                        throw;
                    }
                    
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, OpenRiaServices.EntityFrameworkCore.DbResource.MetadataWorkspaceNotFound + ProcessException(efException), contextType.FullName), efException);
                }
            }
#endif
            if (metadataWorkspace == null)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, MetadataResource.LinqToEntitiesProvider_UnableToRetrieveMetadata, contextType.Name));
            }
            else
            {
                return metadataWorkspace;
            }
        }

        static string ProcessException(Exception ex)
        {
            if (ex == null)
                return string.Empty;
            else
                return " Exception: " + ex.Message + ProcessException(ex.InnerException);
        }
        /// <summary>
        /// Creates the MetadataWorkspace for the given context type and base context type.
        /// </summary>
        /// <param name="contextType">The type of the context.</param>
        /// <param name="baseContextType">The base context type (DbContext or ObjectContext).</param>
        /// <returns>The generated <see cref="MetadataWorkspace"/></returns>
        public static MetadataWorkspace CreateMetadataWorkspaceFromResources(Type contextType, Type baseContextType)
        {
            // get the set of embedded mapping resources for the target assembly and create
            // a metadata workspace info for each group
            IEnumerable<string> metadataResourcePaths = FindMetadataResources(contextType.Assembly);
            IEnumerable<MetadataWorkspaceInfo> workspaceInfos = GetMetadataWorkspaceInfos(metadataResourcePaths);

            // Search for the correct EntityContainer by name and if found, create
            // a comlete MetadataWorkspace and return it
            foreach (var workspaceInfo in workspaceInfos)
            {
                EdmItemCollection edmItemCollection = new EdmItemCollection(workspaceInfo.Csdl);

                Type currentType = contextType;
                while (currentType != baseContextType && currentType != typeof(object))
                {
                    EntityContainer container;
                    if (edmItemCollection.TryGetEntityContainer(currentType.Name, out container))
                    {
                        StoreItemCollection store = new StoreItemCollection(workspaceInfo.Ssdl);
                        MetadataWorkspace workspace = new MetadataWorkspace();
                        return workspace;
                    }

                    currentType = currentType.BaseType;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets the specified resource paths as metadata workspace info objects.
        /// </summary>
        /// <param name="resourcePaths">The metadata resource paths.</param>
        /// <returns>The metadata workspace info objects.</returns>
        private static IEnumerable<MetadataWorkspaceInfo> GetMetadataWorkspaceInfos(IEnumerable<string> resourcePaths)
        {
            // for file paths, you would want to group without the path or the extension like Path.GetFileNameWithoutExtension, but resource names can contain
            // forbidden path chars, so don't use it on resource names
            foreach (var group in resourcePaths.GroupBy(p => p.Substring(0, p.LastIndexOf('.')), StringComparer.InvariantCultureIgnoreCase))
            {
                yield return MetadataWorkspaceInfo.Create(group);
            }
        }

        /// <summary>
        /// Find all the EF metadata resources.
        /// </summary>
        /// <param name="assembly">The assembly to find the metadata resources in.</param>
        /// <returns>The metadata paths that were found.</returns>
        private static IEnumerable<string> FindMetadataResources(Assembly assembly)
        {
            foreach (string name in assembly.GetManifestResourceNames())
            {
                if (MetadataWorkspaceInfo.IsMetadata(name))
                {
                    yield return string.Format("res://{0}/{1}", assembly.FullName, name);
                }
            }
        }

        /// <summary>
        /// Represents the paths for a single metadata workspace.
        /// </summary>
        private class MetadataWorkspaceInfo
        {
            private const string CSDL_EXT = ".csdl";
            private const string MSL_EXT = ".msl";
            private const string SSDL_EXT = ".ssdl";

            public string Csdl
            {
                get;
                private set;
            }

            public string Msl
            {
                get;
                private set;
            }

            public string Ssdl
            {
                get;
                private set;
            }

            public static MetadataWorkspaceInfo Create(IEnumerable<string> paths)
            {
                string csdlPath = null;
                string mslPath = null;
                string ssdlPath = null;
                foreach (string path in paths)
                {
                    if (path.EndsWith(CSDL_EXT, StringComparison.OrdinalIgnoreCase))
                    {
                        csdlPath = path;
                    }
                    else if (path.EndsWith(MSL_EXT, StringComparison.OrdinalIgnoreCase))
                    {
                        mslPath = path;
                    }
                    else if (path.EndsWith(SSDL_EXT, StringComparison.OrdinalIgnoreCase))
                    {
                        ssdlPath = path;
                    }
                }

                return new MetadataWorkspaceInfo(csdlPath, mslPath, ssdlPath);
            }

            public static bool IsMetadata(string path)
            {
                return path.EndsWith(CSDL_EXT, StringComparison.OrdinalIgnoreCase) ||
                       path.EndsWith(MSL_EXT, StringComparison.OrdinalIgnoreCase) ||
                       path.EndsWith(SSDL_EXT, StringComparison.OrdinalIgnoreCase);
            }


            public MetadataWorkspaceInfo(string csdlPath, string mslPath, string ssdlPath)
            {
                if (csdlPath == null)
                {
                    throw new ArgumentNullException(nameof(csdlPath));
                }

                if (mslPath == null)
                {
                    throw new ArgumentNullException(nameof(mslPath));
                }

                if (ssdlPath == null)
                {
                    throw new ArgumentNullException(nameof(ssdlPath));
                }

                this.Csdl = csdlPath;
                this.Msl = mslPath;
                this.Ssdl = ssdlPath;
            }
        }
    }
}
