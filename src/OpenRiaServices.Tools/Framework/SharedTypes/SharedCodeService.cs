﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using OpenRiaServices.Tools.SourceLocation;

namespace OpenRiaServices.Tools.SharedTypes
{
    /// <summary>
    /// Implementation of <see cref="ISharedCodeService"/> based on a set of
    /// known assemblies and a set of shared source files.
    /// </summary>
    /// <remarks>
    /// This class is also a cache so that subsequent requests for a
    /// given code member can give an immediate return.
    /// </remarks>
    internal class SharedCodeService : ISharedCodeService, IDisposable
    {
        private SourceFileLocationService _locationService;
        private readonly SharedSourceFiles _sharedSourceFiles;
        private readonly SharedAssemblies _sharedAssemblies;
        private readonly FilenameMap _filenameMap = new FilenameMap();

        // We maintain a cache so we never lookup any code element more than once.
        // The cache is keyed by SharedCodeKey, which describes the code element and serves as a unique key for it.
        private readonly ConcurrentDictionary<CodeMemberKey, SharedCodeDescription> _cachedDescriptions = new ConcurrentDictionary<CodeMemberKey, SharedCodeDescription>();

        internal SharedCodeService(SharedCodeServiceParameters parameters, ILoggingService loggingService)
        {
            Debug.Assert(parameters != null, "parameters cannot be null");
            Debug.Assert(parameters.SharedSourceFiles != null, "sharedSourceFiles cannot be null");
            Debug.Assert(parameters.ClientAssemblies != null, "clientAssemblies cannot be null");

            // We create an aggregating source file location service that will check in this order:
            //  1. SourceInfoAttributes generated by Live Intellisense, then
            //  2. PDB info
            ISourceFileProviderFactory[] factories = new ISourceFileProviderFactory[] 
            {
                new SourceInfoSourceFileProviderFactory(),
                new PdbSourceFileProviderFactory(parameters.SymbolSearchPaths, loggingService)
            };

            this._locationService = new SourceFileLocationService(factories, this._filenameMap);
            this._sharedSourceFiles = new SharedSourceFiles(this._locationService, this._filenameMap, parameters.SharedSourceFiles);
            this._sharedAssemblies = new SharedAssemblies(parameters.ClientAssemblies, parameters.ClientAssemblyPathsNormalized, loggingService);
        }

        /// <summary>
        /// Gets the instance managing the shared source files.
        /// </summary>
        private SharedSourceFiles SharedSourceFiles
        {
            get
            {
                return this._sharedSourceFiles;
            }
        }

        /// <summary>
        /// Gets the <see cref="SharedCodeDescription"/> for the code member described by <paramref name="key"/>.
        /// </summary>
        /// <param name="key">Describes the code member.</param>
        /// <returns>The <see cref="SharedCodeDescription"/> or <c>null</c>.</returns>
        internal SharedCodeDescription GetSharedCodeDescription(CodeMemberKey key)
        {
            return this._cachedDescriptions.GetOrAdd(key, k =>
            {
                string sharedAssemblyLocation = this._sharedAssemblies.GetSharedAssemblyPath(key);
                if (sharedAssemblyLocation != null)
                {
                    return new SharedCodeDescription(CodeMemberShareKind.SharedByReference, new[] { this._filenameMap.AddOrGet(sharedAssemblyLocation) });
                }

                int[] fileIds = this.SharedSourceFiles.GetSharedFileIds(key);
                if (fileIds != null && fileIds.Length != 0)
                {
                    return new SharedCodeDescription(CodeMemberShareKind.SharedBySource, fileIds);
                }

                return new SharedCodeDescription(CodeMemberShareKind.NotShared, null);
            });
        }


        #region IDisposable Members

        public void Dispose()
        {
            if (this._locationService != null)
            {
                this._locationService.Dispose();
                this._locationService = null;
            }

            (_sharedAssemblies as IDisposable)?.Dispose();
        }

        #endregion

        #region ISharedCodeService
        /// <summary>
        /// Returns a value indicating whether the <see cref="Type"/>specified by <paramref name="typeName"/>
        /// from the reference project is also visible to the dependent project.
        /// </summary>
        /// <param name="typeName">The full name of the <see cref="Type"/>from the reference project.</param>
        /// <returns>The <see cref="CodeMemberShareKind"/> representing whether it is shared and in what way.</returns>
        public CodeMemberShareKind GetTypeShareKind(string typeName)
        {
            CodeMemberKey key = CodeMemberKey.CreateTypeKey(typeName);
            SharedCodeDescription description = this.GetSharedCodeDescription(key);
            return description.ShareKind;
        }

        /// <summary>
        /// Returns a value indicating whether the a property named <paramref name="propertyName"/>
        /// exposed by the <see cref="Type"/> specified by <paramref name="typeName"/>
        /// from the reference project is also visible to the dependent project.
        /// </summary>
        /// <param name="typeName">The full name of the <see cref="Type"/> from the reference project.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <returns>The <see cref="CodeMemberShareKind"/> representing whether it is shared and in what way.</returns>
        public CodeMemberShareKind GetPropertyShareKind(string typeName, string propertyName)
        {
            CodeMemberKey key = CodeMemberKey.CreatePropertyKey(typeName, propertyName);
            SharedCodeDescription description = this.GetSharedCodeDescription(key);
            return description.ShareKind;
        }

        /// <summary>
        /// Returns a value indicating whether a method named <paramref name="methodName"/>
        /// exposed by the <see cref="Type"/> specified by <paramref name="typeName"/>
        /// from the reference project is also visible to the dependent project.
        /// </summary>
        /// <param name="typeName">The full name of the <see cref="Type"/> from the reference project.</param>
        /// <param name="methodName">The name of the method.</param>
        /// <param name="parameterTypeNames">The full type names of the method parameters, in the order they must be declared.</param>
        /// <returns>The <see cref="CodeMemberShareKind"/> representing whether it is shared and in what way.</returns>
        public CodeMemberShareKind GetMethodShareKind(string typeName, string methodName, IEnumerable<string> parameterTypeNames)
        {
            CodeMemberKey key = CodeMemberKey.CreateMethodKey(typeName, methodName, parameterTypeNames == null ? null : parameterTypeNames.ToArray());
            SharedCodeDescription description = this.GetSharedCodeDescription(key);
            return description.ShareKind;
        }
        #endregion ISharedCodeService

        #region Nested types

        /// <summary>
        /// Nested class that captures the location of a shared code member as well as how it is shared.
        /// </summary>
        internal class SharedCodeDescription
        {
            private readonly CodeMemberShareKind _shareKind;
            private readonly int[] _sharedFileIds;
            public SharedCodeDescription(CodeMemberShareKind shareKind, int[] sharedFileIds)
            {
                this._shareKind = shareKind;
                this._sharedFileIds = sharedFileIds;
            }
            internal CodeMemberShareKind ShareKind { get { return this._shareKind; } }
            internal int[] SharedFileIds { get { return this._sharedFileIds; } }
        }

        #endregion Nested types
    }
}
