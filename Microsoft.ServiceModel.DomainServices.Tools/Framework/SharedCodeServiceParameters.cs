using System;
using Microsoft.ServiceModel.DomainServices.Tools.SharedTypes;

namespace Microsoft.ServiceModel.DomainServices.Tools
{
    /// <summary>
    /// Internal data class that allows parameters to be
    /// passed across an AppDomain boundary to construct a <see cref="SharedCodeService"/>.
    /// </summary>
    [Serializable]
    internal class SharedCodeServiceParameters
    {
        /// <summary>
        /// Gets or sets the set of source files common to both the reference and dependent projects
        /// </summary>
        public string[] SharedSourceFiles { get; set; }

        /// <summary>
        /// Gets or sets the paths to search for symbols for the <see cref="ServerAssemblies"/>.
        /// </summary>
        public string[] SymbolSearchPaths { get; set; }

        /// <summary>
        /// Gets or sets the full path names of the set of server assemblies.
        /// </summary>
        public string[] ServerAssemblies { get; set; }

        /// <summary>
        /// Gets or sets the full path names of the set of client assemblies
        /// </summary>
        public string[] ClientAssemblies { get; set; }

        /// <summary>
        /// Gets the set of paths to search for the <see cref="ClientAssemblies"/>.
        /// </summary>
        public string[] ClientAssemblyPathsNormalized { get; set; } 
    }
}
