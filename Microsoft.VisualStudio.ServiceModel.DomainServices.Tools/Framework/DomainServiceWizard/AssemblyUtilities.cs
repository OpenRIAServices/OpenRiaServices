using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Microsoft.VisualStudio.ServiceModel.DomainServices.Tools
{
    //// DEVNOTE: this was copied from AssemblyUtilities in Microsoft.ServiceModel.DomainServices.Tools.
    //// It was modified to use Action<string> instead of ILogger
    //// It was not shared because of dependencies on ILogger and resource strings.

    /// <summary>
    /// Assembly level utilities.
    /// </summary>
    internal static class AssemblyUtilities
    {   
        /// <summary>
        /// Loads the specified assembly file.
        /// </summary>
        /// <param name="assemblyFileName">The full path to the file of the assembly to load.  It cannot be null.</param>
        /// <param name="logger">The optional logger to use to report known load failures.</param>
        /// <returns>The loaded <see cref="Assembly"/> if successful, null if it could not be loaded for a known reason
        /// (and an error message will have been logged).
        /// </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily", Justification = "This code is doing exception type check only."), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Reflection.Assembly.LoadFrom", Justification = "This code needs to call into the problematic methods")]
        internal static Assembly LoadAssembly(string assemblyFileName, Action<string> logger)
        {
            System.Diagnostics.Debug.Assert(!string.IsNullOrEmpty(assemblyFileName), "assemblyFileName is required");

            Assembly assembly = null;

            // TODO: [roncain] Dev10 temp workaround.
            // Reference assemblies will fail by file path, so try to load via name
            // and silently accept that result.  If fail, let normal file load try.
            try
            {
                AssemblyName asmName = AssemblyName.GetAssemblyName(assemblyFileName);
                assembly = LoadAssembly(asmName, null);
                if (assembly != null)
                {
                    return assembly;
                }

                // Otherwise attempt to load from file
                assembly = Assembly.LoadFrom(assemblyFileName);
            }
            catch (Exception ex)
            {
                // Some common exceptions log a warning and keep running
                if (ex is System.IO.FileNotFoundException ||
                    ex is System.IO.FileLoadException ||
                    ex is System.IO.PathTooLongException ||
                    ex is BadImageFormatException ||
                    ex is System.Security.SecurityException)
                {
                    if (logger != null)
                    {
                        logger(string.Format(CultureInfo.CurrentCulture, Resources.BusinessLogicClass_Failed_Load, assemblyFileName, ex.Message));
                    }
                }
                else
                {
                    throw;
                }
            }
            return assembly;
        }

        /// <summary>
        /// Loads the specified assembly by name.
        /// </summary>
        /// <param name="assemblyName">The name of the assembly to load.  It cannot be null.</param>
        /// <param name="logger">The optional logger to use to report known load failures.</param>
        /// <returns>The loaded <see cref="Assembly"/> if successful, null if it could not be loaded for a known reason
        /// (and an error message will have been logged).
        /// </returns>
        internal static Assembly LoadAssembly(AssemblyName assemblyName, Action<string> logger)
        {
            System.Diagnostics.Debug.Assert(assemblyName != null, "assemblyName is required");

            Assembly assembly = null;

            try
            {
                assembly = Assembly.Load(assemblyName);
            }
            catch (Exception ex)
            {
                // Some common exceptions log a warning and keep running
                if (ex is System.IO.FileNotFoundException ||
                    ex is System.IO.FileLoadException ||
                    ex is System.IO.PathTooLongException ||
                    ex is BadImageFormatException ||
                    ex is System.Security.SecurityException)
                {
                    if (logger != null)
                    {
                        logger(ex.ToString());
                        logger(string.Format(CultureInfo.CurrentCulture, Resources.BusinessLogicClass_Failed_Load, assemblyName, ex.Message));
                    }
                }
                else
                {
                    throw;
                }
            }
            return assembly;
        }

        /// <summary>
        /// Returns the set of <see cref="Assembly"/> instances that are referenced
        /// by the given <paramref name="assembly"/>.
        /// </summary>
        /// <remarks>
        /// The resulting collection will contain only those assemblies that loaded
        /// successfully, and it will not contain <c>null</c> values.  Failed loads
        /// will be logged to the <paramref name="logger"/>.
        /// </remarks>
        /// <param name="assembly">The <see cref="Assembly"/> whose referenced assemblies are required.</param>
        /// <param name="logger">The optional logger to use to report known load failures.</param>
        /// <returns>The collection of referenced assemblies.</returns>
        internal static IEnumerable<Assembly> GetReferencedAssemblies(Assembly assembly, Action<string> logger)
        {
            System.Diagnostics.Debug.Assert(assembly != null, "assembly cannot be null");
            AssemblyName[] assemblyNames = (assembly == null) ? new AssemblyName[0] : assembly.GetReferencedAssemblies();
            List<Assembly> assemblies = new List<Assembly>();
            foreach (AssemblyName assemblyName in assemblyNames)
            {
                Assembly a = AssemblyUtilities.LoadAssembly(assemblyName, logger);
                if (a != null)
                {
                    assemblies.Add(a);
                }
            }
            return assemblies;
        }

        /// <summary>
        /// Returns the collection of exported types from the given <paramref name="assembly"/>.
        /// </summary>
        /// <param name="assembly">The <see cref="Assembly"/> whose types are required.</param>
        /// <param name="logger">The optional logger to use to report known load failures.</param>
        /// <returns>The collection of types exported by <paramref name="assembly"/>.</returns>
        internal static IEnumerable<Type> GetExportedTypes(Assembly assembly, Action<string> logger)
        {
            System.Diagnostics.Debug.Assert(assembly != null, "assembly cannot be null");
            Type[] types = null;
            try
            {
                types = (assembly == null) ? new Type[0] : assembly.GetExportedTypes();
            }
            catch (Exception ex)
            {
                // Some common exceptions log a warning and return an empty collection
                if (ex is TypeLoadException ||
                    ex is FileNotFoundException ||
                    ex is FileLoadException ||
                    ex is BadImageFormatException ||
                    ex is ReflectionTypeLoadException)
                {
                    // Show a warning message so user knows we have an issue
                    if (logger != null)
                    {
                        logger(string.Format(CultureInfo.CurrentCulture, Resources.BusinessLogicClass_Failed_Get_Types, assembly.FullName, ex.Message));
                    }
                    return new Type[0];
                }

                // All other exceptions rethrow
                throw;
            }
            // Successful return
            return types;
        }
     }
}
