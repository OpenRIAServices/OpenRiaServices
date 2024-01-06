using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace OpenRiaServices.Tools
{
    /// <summary>
    /// Assembly level utilities.
    /// </summary>
    internal static class AssemblyUtilities
    {
        private static ConcurrentDictionary<string, Assembly> loadedAssemblyNames;

        /// <summary>
        /// Creates a dictionary of assemblyNames and assemblies and sets the AssemblyResolver for the current AppDomain.
        /// </summary>
        /// <param name="assemblies">List of loaded assemblies</param>
        internal static void SetAssemblyResolver(IEnumerable<Assembly> assemblies)
        {
            ConcurrentDictionary<string, Assembly> assemblyNames = new ConcurrentDictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);
            foreach (var assembly in assemblies)
            {
                // Keep a dictionary of assembly names and assemblies. It is used in the AssemblyResolve event handler.
                assemblyNames[assembly.FullName] = assembly;

                // If the assembly is a signed Open Ria assembly, then also add an entry
                // so that it is used in places where the of unsigned version of the assembly is requested
                var assemblyName = assembly.GetName();
                if(assemblyName.IsOpenRiaAssembly() && assemblyName.IsSigned())
                {
                    var unsignedName = new AssemblyName(assemblyName.FullName);
                    unsignedName.SetPublicKeyToken(Array.Empty<byte>());
                    assemblyNames[unsignedName.FullName] = assembly;
                }
            }
            loadedAssemblyNames = assemblyNames;

            // Unregister the event handler first, in case it was registered before. If it wasn't, this would be a no-op.
            AppDomain.CurrentDomain.AssemblyResolve -= new ResolveEventHandler(AssemblyUtilities.CurrentDomain_AssemblyResolveEventHandler);
            
            // Register the event handler for this call.
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(AssemblyUtilities.CurrentDomain_AssemblyResolveEventHandler);
        }

        /// <summary>
        /// <see cref="AppDomain.AssemblyResolve"/> Event handler to handle the cases when we cannot load
        /// assemblies because of the differences in load context. We go through the list of loaded assemblies
        /// and return the assembly if we find it.
        /// </summary>
        /// <param name="sender">The caller object</param>
        /// <param name="args">The resolve event arguments containing the information about the assembly to be resolved</param>
        /// <returns>The <see cref="Assembly"/> if found and <c>null</c> otherwise</returns>
        private static Assembly CurrentDomain_AssemblyResolveEventHandler(object sender, ResolveEventArgs args)
        {
            Assembly assembly = null;
            if (!string.IsNullOrEmpty(args.Name) && loadedAssemblyNames != null)
            {
                // If we are not able to load the assembly from our pre-populated list, we can check the current
                // AppDomain for assemblies loaded by reflection when the DomainServiceDescriptions were created.
                // This scenario occurs when we have to look up entity types in reference assemblies that Visual
                // Studio does not specify.
                assembly = loadedAssemblyNames.GetOrAdd(args.Name,
                    name => AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == name));
            }

            return assembly;
        }
        
        /// <summary>
        /// Loads the specified assembly file.
        /// </summary>
        /// <param name="assemblyFileName">The full path to the file of the assembly to load.  It cannot be null.</param>
        /// <param name="logger">The optional logger to use to report known load failures.</param>
        /// <returns>The loaded <see cref="Assembly"/> if successful, null if it could not be loaded for a known reason
        /// (and an error message will have been logged).
        /// </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily", Justification = "This code is doing exception type check only."), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Reflection.Assembly.LoadFrom", Justification = "This code needs to call into the problematic methods")]
        internal static Assembly LoadAssembly(string assemblyFileName, ILogger logger)
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
                if (ex is FileNotFoundException ||
                    ex is FileLoadException ||
                    ex is PathTooLongException ||
                    ex is DirectoryNotFoundException ||
                    ex is BadImageFormatException ||
                    ex is System.Security.SecurityException)
                {
                    if (logger != null)
                    {
                        logger.LogMessage(string.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_Assembly_Load_Error, assemblyFileName, ex.Message));
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
        internal static Assembly LoadAssembly(AssemblyName assemblyName, ILogger logger)
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
                        logger.LogMessage(string.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_Assembly_Load_Error, assemblyName, ex.Message));
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
        /// Standard implementation of <see cref="Assembly.GetExportedTypes()"/>
        /// with autorecovery and logging of common error conditions.
        /// </summary>
        /// <param name="assembly">The assembly whose exported types are needed</param>
        /// <param name="logger">Optional logger to use to report problems.</param>
        /// <returns>The collection of types.  It may be empty but it will not be null.</returns>
        internal static IEnumerable<Type> GetExportedTypes(Assembly assembly, ILogger logger)
        {
            Type[] types = null;
            try
            {
                types = assembly.GetExportedTypes();
            }
            catch (Exception ex)
            {
                if (ex.IsFatal())
                {
                    throw;
                }
                
                // Some common exceptions log a warning and return an empty collection
                if (ex is TypeLoadException ||
                    ex is FileNotFoundException ||
                    ex is FileLoadException ||
                    ex is BadImageFormatException)
                {
                    // We log only if we have a logger and this is not MSCORLIB
                    // MSCORLIB.GetExportedTypes will throw TypeLoadException when used
                    // in a Reflection-Only load.  Yet that is not fatal because the
                    // real MSCORLIB is loaded and the user would be distracted by
                    // any warnings of this nature.
                    if (logger != null && !IsAssemblyMsCorlib(assembly.GetName()))
                    {
                        logger.LogMessage(string.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_Assembly_Load_Type_Error, assembly.FullName, ex.Message));
                    }
                    return TypeUtility.EmptyTypes;
                }

                // This particular exception may have loaded at least some types.  Capture what we can.
                ReflectionTypeLoadException rtle = ex as ReflectionTypeLoadException;
                if (rtle != null)
                {
                    // They tell us the types they loaded -- but the array could have nulls where they failed
                    types = rtle.Types;

                    // Show a warning message so user knows we have an issue
                    if (logger != null)
                    {
                        StringBuilder sb = new StringBuilder();
                        Exception[] loadExceptions = rtle.LoaderExceptions;
                        foreach (Exception e in loadExceptions)
                        {
                            sb.AppendLine(e.Message);
                        }
                        logger.LogMessage(string.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_Failed_To_Load, assembly.FullName, sb.ToString()));
                    }
                    // Return collection without nulls
                    return types.Where(t => t != null);
                }

                // All other exceptions rethrow
                throw;
            }
            // Successful return
            return types;
        }

        /// <summary>
        /// Determines whether the given assembly is mscorlib
        /// </summary>
        /// <param name="assemblyName">assembly name to test</param>
        /// <returns><c>true</c> if the assembly is mscorlib</returns>
        internal static bool IsAssemblyMsCorlib(AssemblyName assemblyName)
        {
            return (string.Equals(assemblyName.Name, "mscorlib", StringComparison.OrdinalIgnoreCase)
                || string.Equals(assemblyName.Name, "System.Private.CoreLib", StringComparison.OrdinalIgnoreCase));
        }
    }
}
