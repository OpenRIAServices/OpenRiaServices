namespace OpenRiaServices.Tools.SourceLocation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.SymbolStore;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using Mono.Cecil;
    using OpenRiaServices;
    using OpenRiaServices.Tools.Pdb.SymStore;
    using OpenRiaServices.Tools.SharedTypes;

    /// <summary>
    /// PDB-based implementation of <see cref="ISourceFileProvider"/> to locate source files for types or methods.
    /// </summary>
    internal class CecilSourceFileProviderFactory : ISourceFileProviderFactory
    {
        private readonly ILogger _logger;
        private readonly string _symbolSearchPath;

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="symbolSearchPath">Optional list of semicolon separated paths to search for PDB files.</param>
        /// <param name="logger">Optional logger to use to report errors or warnings.</param>
        public CecilSourceFileProviderFactory(string symbolSearchPath, ILogger logger)
            : base()
        {

            this._logger = logger;
            this._symbolSearchPath = symbolSearchPath;
        }

        #region ISourceFileProviderFactory
        public ISourceFileProvider CreateProvider()
        {
            return new CecilSourceFileProvider(this._symbolSearchPath, this._logger);
        }
        #endregion // ISourceFileProviderFactory

        #region Nested classes
        /// <summary>
        /// Helper class to encapsulate the symbol reader during analysis
        /// of a type.  This class is instantiated when the base class asks
        /// for a location context, and then it is disposed by the base.
        /// </summary>
        internal class CecilSourceFileProvider : ISourceFileProvider, IDisposable
        {
            private readonly ILogger _logger;
            private readonly string _symbolSearchPath;
            private readonly Dictionary<string, AssemblyDefinition> _assembliesByLocation = new();

            internal CecilSourceFileProvider(string symbolSearchPath, ILogger logger)
            {
                this._symbolSearchPath = symbolSearchPath;
                this._logger = logger;
            }

            /// <summary>
            /// Indexer that gets the <see cref="ISymbolReader"/> appropriate to analyze
            /// the specified <paramref name="type"/>.  This indexer caches by Assembly.
            /// </summary>
            /// <param name="type">The type we will analyze.</param>
            /// <returns>A <see cref="ISymbolReader"/> or <c>null</c> if one is not available.</returns>
            internal AssemblyDefinition this[Type type]
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
                                    SymbolReaderProvider = new Mono.Cecil.Pdb.PdbReaderProvider()
                                    //ReadingMode = ReadingMode.
                                };
                                assemblyDefinition = AssemblyDefinition.ReadAssembly(location, parameters);
                            }
                            catch (Exception)
                            {
                                // Cache null
                                assemblyDefinition = null;
                            }
                        }

                        this._assembliesByLocation[location] = assemblyDefinition;
                    }

                    return assemblyDefinition;
                }
            }

            /// <summary>
            /// Returns the name of the file for the given method using the given symbol reader
            /// </summary>
            /// <param name="reader">The reader to use</param>
            /// <param name="methodBase">The method to lookup</param>
            /// <returns>The file containing the method or null.</returns>
            private static string GetFileForMethod(AssemblyDefinition reader, MethodBase methodBase)
            {
                int token = methodBase.MetadataToken;
                var type = reader.MainModule.Types
                    .FirstOrDefault(t => t.FullName == methodBase.DeclaringType.FullName);

                var method = type?.Methods.FirstOrDefault(md => md.Name == methodBase.Name || md.MetadataToken.ToUInt32() == methodBase.MetadataToken);
                if (method != null)
                {
                    if (!method.DebugInformation.HasSequencePoints)
                        return null;

                    return method.DebugInformation.SequencePoints.FirstOrDefault().Document.Url;
                }
                return null;
            }


            #region IDisposable members
            public void Dispose()
            {
                foreach (var assemblyDefinition in this._assembliesByLocation.Values)
                {
                    if (assemblyDefinition != null)
                    {
                        assemblyDefinition.Dispose();
                    }
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
                AssemblyDefinition reader = this[declaringType];

                // Failure to find PDB short-circuits all lookups
                if (reader != null)
                {
                    // PDB knows only about methods, so this is the only one we really care about
                    MethodBase methodBase = memberInfo as MethodBase;
                    if (methodBase != null)
                    {
                        fileName = CecilSourceFileProvider.GetFileForMethod(reader, methodBase);
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
}
