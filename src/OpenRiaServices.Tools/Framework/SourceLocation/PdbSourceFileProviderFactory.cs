using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.SymbolStore;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Pdb;
using OpenRiaServices.Tools.SharedTypes;

namespace OpenRiaServices.Tools.SourceLocation
{
    /// <summary>
    /// PDB-based implementation of <see cref="ISourceFileProvider"/> to locate source files for types or methods.
    /// </summary>
    internal class PdbSourceFileProviderFactory : ISourceFileProviderFactory
    {
        private readonly ILogger _logger;
        private readonly string[] _symbolSearchPaths;

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="symbolSearchPaths">Optional list of paths to search for PDB files.</param>
        /// <param name="logger">Optional logger to use to report errors or warnings.</param>
        public PdbSourceFileProviderFactory(string[] symbolSearchPaths, ILogger logger)
            : base()
        {
            this._logger = logger;
            this._symbolSearchPaths = symbolSearchPaths;
        }


        #region ISourceFileProviderFactory
        public ISourceFileProvider CreateProvider()
        {
            return new CecilSourceFileProvider(this._symbolSearchPaths, this._logger);
        }
        #endregion // ISourceFileProviderFactory

        #region Nested classes
        /// <summary>
        /// Helper class to encapsulate the symbol reader during analysis
        /// of a type.  This class is instantiated when the base class asks
        /// for a location context, and then it is disposed by the base.
        /// </summary>
        internal sealed class CecilSourceFileProvider : ISourceFileProvider
        {
            private readonly ILogger _logger;
            private readonly Dictionary<string, AssemblyDefinition> _assembliesByLocation = new();
            private readonly ISymbolReaderProvider _symbolReaderProvider;

            internal CecilSourceFileProvider(string[] symbolSearchPath, ILogger logger)
            {
                _symbolReaderProvider = new SymbolReaderProvider(symbolSearchPath);
                this._logger = logger;
            }

            /// <summary>
            /// Indexer that gets the <see cref="TypeDefinition"/> appropriate to analyze
            /// the specified <paramref name="type"/>.
            /// </summary>
            /// <param name="type">The type we will analyze.</param>
            /// <returns>A <see cref="TypeDefinition"/> or <c>null</c> if one is not available.</returns>
            internal TypeDefinition this[Type type]
            {
                get
                {
                    Debug.Assert(type != null, "The type is required");
                    Assembly assembly = type.Assembly;
                    string location = assembly.Location;

                    // Lazily create the readers for assemblies we have not yet encountered.
                    if (!this._assembliesByLocation.TryGetValue(location, out AssemblyDefinition assemblyDefinition))
                    {
                        // We don't create symbol readers for System assemblies
                        if (!assembly.IsSystemAssembly())
                        {
                            // Lazy create.  Note that a null is a legal result and will
                            // be cached to avoid redundant failures
                            try
                            {
                                var parameters = new ReaderParameters
                                {
                                    ReadSymbols = true,
                                    ReadWrite = false,
                                    SymbolReaderProvider = _symbolReaderProvider
                                };

                                assemblyDefinition = AssemblyDefinition.ReadAssembly(location, parameters);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning($"Failed to load metadata for {assembly.FullName}: {ex}");
                                // Cache null
                                assemblyDefinition = null;
                            }
                        }

                        this._assembliesByLocation[location] = assemblyDefinition;
                    }

                    return assemblyDefinition?.MainModule.GetType(type.Namespace, type.Name);
                }
            }

            /// <summary>
            /// Returns the name of the file for the given method using the given symbol reader
            /// </summary>
            /// <param name="typeDefinition">The type tos search in</param>
            /// <param name="methodBase">The method to lookup</param>
            /// <returns>The file containing the method or null.</returns>
            private static string GetFileForMethod(TypeDefinition typeDefinition, MethodBase methodBase)
            {
                // Token is unique identifer per compilation (so cannot use old pdb)
                int token = methodBase.MetadataToken;

                var method = typeDefinition.Methods.FirstOrDefault(md => md.MetadataToken.ToInt32() == token);
                return method?.DebugInformation.SequencePoints.FirstOrDefault()?.Document.Url;
            }


            #region IDisposable members
            void IDisposable.Dispose()
            {
                foreach (var assemblyDefinition in this._assembliesByLocation.Values)
                {
                    assemblyDefinition?.Dispose();
                }
                this._assembliesByLocation.Clear();
            }
            #endregion // IDisposable member

            public string GetFileForMember(MemberInfo memberInfo)
            {
                string fileName = null;

                Type declaringType = memberInfo.DeclaringType;

                // Note: we allow checking for members declared anywhere in the hierarchy
                // and so must open a PDB for the assembly containing the declaring type.
                TypeDefinition typeDefinition = this[declaringType];

                // Failure to find PDB short-circuits all lookups
                if (typeDefinition != null)
                {
                    // PDB knows only about methods, so this is the only one we really care about
                    MethodBase methodBase = memberInfo as MethodBase;
                    if (methodBase != null)
                    {
                        fileName = CecilSourceFileProvider.GetFileForMethod(typeDefinition, methodBase);
                    }
                    else
                    {
                        // Asking about a property decomposes into asking about the setter or getter
                        // method.
                        PropertyInfo propertyInfo = memberInfo as PropertyInfo;
                        if (propertyInfo != null)
                        {
                            methodBase = propertyInfo.GetGetMethod() ?? propertyInfo.GetSetMethod();
                            if (methodBase != null)
                            {
                                fileName = this.GetFileForMember(methodBase);
                            }
                        }
                    }
                }
                return fileName;
            }
        }
        #endregion // Nested classes
    }

    /// <summary>
    /// A symbol reader which supports looking for pdb files based on symbol search path on lookup failure
    /// </summary>
    internal sealed class SymbolReaderProvider : ISymbolReaderProvider
    {
        readonly PdbReaderProvider _pdbReaderProvider = new PdbReaderProvider();
        readonly string[] _symbolSearchPaths;

        public SymbolReaderProvider(string[] symbolSearchPaths)
        {
            // List will contain duplicates
            _symbolSearchPaths = symbolSearchPaths?.Distinct().ToArray();
        }

        public Mono.Cecil.Cil.ISymbolReader GetSymbolReader(ModuleDefinition module, string fileName)
        {
            try
            {
                return _pdbReaderProvider.GetSymbolReader(module, fileName);
            }
            catch (FileNotFoundException) when (_symbolSearchPaths != null)
            {
                string fileNameOnly = Path.GetFileNameWithoutExtension(fileName) + ".pdb";
                foreach (var path in _symbolSearchPaths)
                {
                    var possibleName = Path.Combine(path, fileNameOnly);
                    if (File.Exists(possibleName))
                        return _pdbReaderProvider.GetSymbolReader(module, possibleName);
                }

                throw;
            }
        }

        public Mono.Cecil.Cil.ISymbolReader GetSymbolReader(ModuleDefinition module, Stream symbolStream)
        {
            return _pdbReaderProvider.GetSymbolReader(module, symbolStream);
        }
    }
}
