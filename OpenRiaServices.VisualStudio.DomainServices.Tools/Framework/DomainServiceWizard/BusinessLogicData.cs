using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace OpenRiaServices.VisualStudio.DomainServices.Tools
{
    /// <summary>
    /// Data-only class used to share state across AppDomain
    /// boundaries between <see cref="BusinessLogicModel"/> and
    /// <see cref="BusinessLogicViewModel"/>.
    /// </summary>
    [Serializable]
    public class BusinessLogicData : MarshalByRefObject
    {
        /// <summary>
        /// Gets or sets the language for code generation,
        /// </summary>
        public string Language { get; set; }

        /// <summary>
        /// Gets or sets the full path to the Linq to Sql DomainService assembly.
        /// This is required because Linq to Sql is an optional toolkit component
        /// and may not be present at runtime.
        /// </summary>
        public string LinqToSqlPath { get; set; }

        /// <summary>
        /// Gets or sets the full paths to the assemblies that hold <see cref="ContextTypeNames"/>.
        /// These assemblies must be loaded first to evaluate <see cref="ContextTypeNames"/>.
        /// </summary>
        public string[] AssemblyPaths { get; set; }

        /// <summary>
        /// Gets or sets the full paths to the assemblies referenced by AssemblyPaths.
        /// These assemblies must be loaded second to evaluate <see cref="ContextTypeNames"/>.
        /// </summary>
        public string[] ReferenceAssemblyPaths { get; set; }

        /// <summary>
        /// Gets or sets the assembly qualified type names
        /// of the context types to offer to the user for
        /// selection.
        /// </summary>
        public string[] ContextTypeNames { get; set; }
    }
}
