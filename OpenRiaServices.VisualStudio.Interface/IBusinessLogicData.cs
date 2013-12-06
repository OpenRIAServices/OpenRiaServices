using System;
using System.Runtime.Remoting;

namespace OpenRiaServices.VisualStudio.DomainServices.Tools
{
    public interface IBusinessLogicData
    {
        /// <summary>
        /// Gets or sets the language for code generation,
        /// </summary>
        string Language { get; set; }

        /// <summary>
        /// Gets or sets the full path to the Linq to Sql DomainService assembly.
        /// This is required because Linq to Sql is an optional toolkit component
        /// and may not be present at runtime.
        /// </summary>
        string LinqToSqlPath { get; set; }

        /// <summary>
        /// Gets or sets the full paths to the assemblies that hold <see cref="ContextTypeNames"/>.
        /// These assemblies must be loaded first to evaluate <see cref="ContextTypeNames"/>.
        /// </summary>
        string[] AssemblyPaths { get; set; }

        /// <summary>
        /// Gets or sets the full paths to the assemblies referenced by AssemblyPaths.
        /// These assemblies must be loaded second to evaluate <see cref="ContextTypeNames"/>.
        /// </summary>
        string[] ReferenceAssemblyPaths { get; set; }

        /// <summary>
        /// Gets or sets the assembly qualified type names
        /// of the context types to offer to the user for
        /// selection.
        /// </summary>
        string[] ContextTypeNames { get; set; }

        object GetLifetimeService();
        object InitializeLifetimeService();
        ObjRef CreateObjRef(Type requestedType);
    }
}