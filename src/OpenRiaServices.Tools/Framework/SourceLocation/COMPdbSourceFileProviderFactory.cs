namespace OpenRiaServices.Tools.SourceLocation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.SymbolStore;
    using System.Globalization;
    using System.Reflection;
    using OpenRiaServices;
    using OpenRiaServices.Tools.Pdb.SymStore;
    using OpenRiaServices.Tools.SharedTypes;

    /// <summary>
    /// PDB-based implementation of <see cref="ISourceFileProvider"/> to locate source files for types or methods.
    /// </summary>
    internal class COMPdbSourceFileProviderFactory : ISourceFileProviderFactory
    {
        private readonly ILogger _logger;
        private readonly string _symbolSearchPath;

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="symbolSearchPath">Optional list of semicolon separated paths to search for PDB files.</param>
        /// <param name="logger">Optional logger to use to report errors or warnings.</param>
        public COMPdbSourceFileProviderFactory(string symbolSearchPath, ILogger logger)
            : base()
        {
            this._logger = logger;
            this._symbolSearchPath = symbolSearchPath;
        }

        #region ISourceFileProviderFactory
        public ISourceFileProvider CreateProvider()
        {
            return new PdbSourceFileProvider(this._symbolSearchPath, this._logger);
        }
        #endregion // ISourceFileProviderFactory

        #region Nested classes
        /// <summary>
        /// Helper class to encapsulate the symbol reader during analysis
        /// of a type.  This class is instantiated when the base class asks
        /// for a location context, and then it is disposed by the base.
        /// </summary>
        internal class PdbSourceFileProvider : ISourceFileProvider, IDisposable
        {
            private readonly ILogger _logger;
            private readonly string _symbolSearchPath;
            private readonly Dictionary<Assembly, ISymbolReader> _symbolReadersByType = new Dictionary<Assembly, ISymbolReader>();

            internal PdbSourceFileProvider(string symbolSearchPath, ILogger logger)
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
            internal ISymbolReader this[Type type]
            {
                get
                {
                    Debug.Assert(type != null, "The type is required");
                    Assembly assembly = type.Assembly;
                    ISymbolReader reader = null;

                    // Lazily create the readers for assemblies we have not yet encountered.
                    if (!this._symbolReadersByType.TryGetValue(assembly, out reader))
                    {
                        // We don't create symbol readers for System assemblies
                        if (!assembly.IsSystemAssembly())
                        {
                            // Lazy create.  Note that a null is a legal result and will
                            // be cached to avoid redundant failures
                            reader = this.CreateSymbolReader(assembly);
                        }

                        this._symbolReadersByType[assembly] = reader;
                    }

                    return reader;
                }
            }

            /// <summary>
            /// Returns the name of the file for the given method using the given symbol reader
            /// </summary>
            /// <param name="reader">The reader to use</param>
            /// <param name="methodBase">The method to lookup</param>
            /// <returns>The file containing the method or null.</returns>
            private static string GetFileForMethod(ISymbolReader reader, MethodBase methodBase)
            {
                int token = methodBase.MetadataToken;

                ISymbolMethod methodSymbol = reader == null ? null : reader.GetMethod(new SymbolToken(token));
                if (methodSymbol != null)
                {
                    int count = methodSymbol.SequencePointCount;
                    if (count == 0)
                        return null;

                    // Get the sequence points from the symbol store. 
                    // We could cache these arrays and reuse them.
                    int[] offsets = new int[count];
                    ISymbolDocument[] docs = new ISymbolDocument[count];
                    int[] startColumn = new int[count];
                    int[] endColumn = new int[count];
                    int[] startRow = new int[count];
                    int[] endRow = new int[count];
                    methodSymbol.GetSequencePoints(offsets, docs, startRow, startColumn, endRow, endColumn);

                    return docs[0].URL.ToString();
                }
                return null;
            }

            /// <summary>
            /// Creates a <see cref="ISymbolReader"/> for the given <paramref name="assembly"/>.
            /// </summary>
            /// <param name="assembly">The assembly whose reader is needed.</param>
            /// <returns>The <see cref="ISymbolReader"/> instance or <c>null</c> if one cannot be created (e.g. no PDB).</returns>
            private ISymbolReader CreateSymbolReader(Assembly assembly)
            {
                Debug.Assert(assembly != null, "The assembly is required");
                ISymbolReader reader = null;
                string assemblyFile = assembly.Location;

                try
                {
                    reader = SymbolAccess.GetReaderForFile(assemblyFile, this._symbolSearchPath);
                }
                catch (System.Runtime.InteropServices.COMException cex)
                {
                    // Experience has shown some large PDB's can exhaust memory and cause COM failures.
                    // When this occurs, we log a warning and continue as if the PDB was not there.
                    if (this._logger != null)
                    {
                        this._logger.LogWarning(string.Format(CultureInfo.CurrentCulture, Resource.Failed_To_Open_PDB, assemblyFile, cex.Message));
                    }
                }

                return reader;
            }

            #region IDisposable members
            public void Dispose()
            {
                foreach (ISymbolReader reader in this._symbolReadersByType.Values)
                {
                    IDisposable disposable = reader as IDisposable;
                    if (disposable != null)
                    {
                        disposable.Dispose();
                    }
                }
                this._symbolReadersByType.Clear();
            }
            #endregion // IDisposable member

            public string GetFileForMember(MemberInfo memberInfo)
            {
                string fileName = null;

                Type declaringType = memberInfo.DeclaringType;

                // Note: we allow checking for members declared anywhere in the hierarchy
                // and so must open a PDB for the assembly containing the declaring type.
                ISymbolReader reader = this[declaringType];

                // Failure to find PDB short-circuits all lookups
                if (reader != null)
                {
                    // PDB knows only about methods, so this is the only one we really care about
                    MethodBase methodBase = memberInfo as MethodBase;
                    if (methodBase != null)
                    {
                        fileName = PdbSourceFileProvider.GetFileForMethod(reader, methodBase);
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
