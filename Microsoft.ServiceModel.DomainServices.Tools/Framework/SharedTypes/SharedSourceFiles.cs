using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.ServiceModel.DomainServices;

namespace Microsoft.ServiceModel.DomainServices.Tools.SharedTypes
{
    /// <summary>
    /// Internal class to maintain a list of known source files
    /// and to answer questions whether the source files for
    /// specific types or methods are defined in those files.
    /// </summary>
    internal class SharedSourceFiles
    {
        // Sentinel value that indicates a member is not part of a shared file
        internal const int NotShared = FilenameMap.NotAFile;

        private ISourceFileLocationService _sourceFileLocationService;
        private FilenameMap _filenameMap;
        private bool _anySharedFiles;

        // Keyed by file ID, contains only the ID's of files were told to consider shared.
        // This cache is not modified after it has been initialized and is for performance only.
        private HashSet<int> _sharedFileIds = new HashSet<int>();

        /// <summary>
        /// Initializes a new instance of the <see cref="SharedSourceFiles"/> class.
        /// </summary>
        /// <param name="sourceFileLocationService">Service to use to locate source files for types or methods.  
        /// It cannot be null.</param>
        /// <param name="filenameMap">Cache to map file names to an internal ID and back.</param>
        /// <param name="sharedFiles">The set of files to consider "shared" in answering requests about types or methods.  
        /// It may be empty but it cannot be null.</param>
        internal SharedSourceFiles(ISourceFileLocationService sourceFileLocationService, FilenameMap filenameMap, IEnumerable<string> sharedFiles)
        {
            System.Diagnostics.Debug.Assert(sourceFileLocationService != null, "sourceFileLocationService cannot be null");
            System.Diagnostics.Debug.Assert(filenameMap != null, "filenameMap cannot be null");
            System.Diagnostics.Debug.Assert(sharedFiles != null, "sharedFiles cannot be null");

            this._sourceFileLocationService = sourceFileLocationService;
            this._filenameMap = filenameMap;
            this._anySharedFiles = sharedFiles.Any();

            // Don't allocate anything if the list of files is empty
            if (this._anySharedFiles)
            {
                // Get a unique ID for every file declared as shared, and
                // keep this in a hashset for fast lookup
                foreach (string sharedFile in sharedFiles)
                {
                    this._sharedFileIds.Add(this._filenameMap.AddOrGet(sharedFile));
                }
            }
        }

        /// <summary>
        /// Gets the source file location service
        /// </summary>
        private ISourceFileLocationService SourceFileLocationService { get { return this._sourceFileLocationService; } }

        /// <summary>
        /// Gets the collection of internal ID's of the files that collectively
        /// define the code member described by <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The key describing the code member.</param>
        /// <returns>The collection of internal ID's, or <c>null</c> if no shared files exist for this code element.</returns>
        internal int[] GetSharedFileIds(CodeMemberKey key)
        {
            Debug.Assert(key != null, "key cannot be null");

            // Early exit if no shared files were specified
            if (!this._anySharedFiles)
            {
                return null;
            }

            Type type = key.Type;

            // If we don't have the type in this AppDomain, then we don't consider it shared.
            // For the sake of performance, System types are never considered
            // shared from the perspective of source files.   This optimization
            // skips attempts to open PDB's or reflect into system types.
            // We don't even add an entry in the cache for these.
            if (type == null || type.Assembly.IsSystemAssembly())
            {
                return null;
            }

            int[] fileIds = null;

            switch (key.KeyKind)
            {
                case CodeMemberKey.CodeMemberKeyKind.TypeKey:
                    IEnumerable<string> files = this.SourceFileLocationService.GetFilesForType(type);
                    if (files != null && files.Any())
                    {
                        IEnumerable<int> filesAsIds = files.Select<string, int>(s => this.FileNameToSharedID(s)).Distinct();
                        int[] sharedFileIds = filesAsIds.Where(i => i != SharedSourceFiles.NotShared).ToArray();
                        fileIds = sharedFileIds.Length == 0 ? null : sharedFileIds;
                    }

                    break;

                case CodeMemberKey.CodeMemberKeyKind.PropertyKey:
                    PropertyInfo propertyInfo = key.PropertyInfo;
                    if (propertyInfo == null)
                    {
                        return null;
                    }
                    string propertyFile = this.SourceFileLocationService.GetFileForMember(propertyInfo);
                    int sharedPropertyFileId = this.FileNameToSharedID(propertyFile);
                    if (sharedPropertyFileId != SharedSourceFiles.NotShared)
                    {
                        fileIds = new int[] { sharedPropertyFileId };
                    }

                    break;

                case CodeMemberKey.CodeMemberKeyKind.MethodKey:
                    MethodBase methodBase = key.MethodBase;
                    if (methodBase == null)
                    {
                        return null;
                    }
                    string methodFile = this.SourceFileLocationService.GetFileForMember(methodBase);
                    int sharedMethodFileId = this.FileNameToSharedID(methodFile);
                    if (sharedMethodFileId != SharedSourceFiles.NotShared)
                    {
                        fileIds = new int[] { sharedMethodFileId };
                    }
                    break;

                default:
                    Debug.Fail("unsupported key kind");
                    break;
            }
            return fileIds;
        }

        /// <summary>
        /// Returns the internal ID of the shared file for the given <paramref name="file"/>
        /// </summary>
        /// <param name="file">The file to test.</param>
        /// <returns>The internal ID of the shared file or <see cref="SharedSourceFiles.NotShared"/> 
        /// if it is not shared or <paramref name="file"/> was null or empty.</returns>
        private int FileNameToSharedID(string file)
        {
            // Empty files are never shared
            if (string.IsNullOrEmpty(file))
            {
                return SharedSourceFiles.NotShared;
            }
            // Always add a cache entry if this is the first time or get the
            // ID from our general file location cache.
            int id = this._filenameMap.AddOrGet(file);

            // Now ask whether this file ID is in our cache of known shared files
            return (this._sharedFileIds.Contains(id)) ? id : SharedSourceFiles.NotShared;
        }
    }
}
