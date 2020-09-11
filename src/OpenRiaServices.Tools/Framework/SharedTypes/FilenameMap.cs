using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace OpenRiaServices.Tools.SharedTypes
{
    /// <summary>
    /// Internal class to maintain mapping between file names and an internally generated integer ID.
    /// </summary>
    /// <remarks>This class is used to eliminate duplicate copies of file names kept by the
    /// shared code utility classes.
    /// </remarks>
    internal class FilenameMap
    {
        // sentinel ID value used to cache an entry for "this is not a file" scenarios.
        // This permits clients to get the benefit of retaining an ID that means no file
        // is associated with 
        internal const int NotAFile = 0;

        // keyed by (case insensitive) file name, returns an internal ID
        private readonly ConcurrentDictionary<string, int> _idsByFile = new ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        // keyed by ID, returns the file name
        private readonly Dictionary<int, string> _filesById = new Dictionary<int, string>();

        // -1 so first interlocked increment becomes NotAFile
        private int _currentId = -1;

        internal FilenameMap()
        {
            // Unconditionally make the ID=0 element contain an empty file.
            // This allows our cache to be used by clients that
            // want a cached ID that means "no file is associated with this element."
            int id = this.AddOrGet(string.Empty);
            System.Diagnostics.Debug.Assert(id == FilenameMap.NotAFile, "First id should be same as NotAFile");
        }

        /// <summary>
        /// Indexer that gets the internal ID for a given file name.
        /// </summary>
        /// <remarks>The specified file name must already be known to have
        /// a unique ID or this internal method will assert.
        /// </remarks>
        /// <param name="fileName">The name of the file.</param>
        /// <returns>The internal ID.</returns>
        internal int this[string fileName]
        {
            get
            {
                // Map null to empty file so all map to common NotAFile ID
                if (fileName == null)
                {
                    fileName = string.Empty;
                }
                int id;
                if (!this._idsByFile.TryGetValue(fileName, out id))
                {
                    id = FilenameMap.NotAFile;
                    Debug.Fail("Invalid FileLocationCache filename: ", fileName);
                }
                return id;
            }
        }

        /// <summary>
        /// Indexer that gets the file name associated with the given internal ID.
        /// </summary>
        /// <remarks>The specified ID must already be known to have an associated
        /// file name or this internal method will assert.
        /// </remarks>
        /// <param name="id">The internal ID.</param>
        /// <returns>The file name associated with the given ID.</returns>
        internal string this[int id]
        {
            get
            {
                string fileName = null;
                if (!this._filesById.TryGetValue(id, out fileName))
                {
                    Debug.Fail("Invalid FileLocationCache ID: " + id);
                }
                return fileName;
            }
        }

        /// <summary>
        /// Retrieves the internal ID associated with a given file name.
        /// If it does not already have an ID, one will be created.
        /// </summary>
        /// <param name="fileName">The file name.  It may be null or empty.</param>
        /// <returns>The internal ID.   A null or empty file name will always return <see cref="FilenameMap.NotAFile"/>.</returns>
        internal int AddOrGet(string fileName)
        {
            // Map null to empty file so all map to common NotAFile ID
            if (fileName == null)
            {
                fileName = string.Empty;
            }
            return this._idsByFile.GetOrAdd(fileName, f =>
            {
                int id = Interlocked.Increment(ref this._currentId);
                this._filesById[id] = f;
                return id;
            });
        }
    }
}
