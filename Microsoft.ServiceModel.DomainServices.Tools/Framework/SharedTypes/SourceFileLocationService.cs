namespace Microsoft.ServiceModel.DomainServices.Tools.SharedTypes
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.SymbolStore;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading;

    /// <summary>
    /// Implementation of <see cref="ISourceFileLocationService"/> to locate source files for types or members.
    /// </summary>
    /// <remarks>
    /// This class maintains the cache of types and members that have been interrogated
    /// previously.  It does not contain any implementation logic for how the files are found,
    /// but instead delegates that policy to the <see cref="ISourceFileProviderFactory"/>
    /// instances it has been given.
    /// </remarks>
    internal class SourceFileLocationService : ISourceFileLocationService, IDisposable
    {
        private ISourceFileProvider[] _providers;
        private FilenameMap _filenameMap;

        // Keyed by MemberInfo, returns an ID that can return a file name from this._filesById.
        // Note that we have a single large cache of all MemberInfo's independent of the types
        // that discover them, so we ask at most once where the file is for any given member.
        private Dictionary<MemberInfo, int> _fileIdsByMemberInfo = new Dictionary<MemberInfo, int>();

        // Keyed by type, returns the set of member infos belonging to that type, including those declared
        // by a base type.
        private Dictionary<Type, IEnumerable<MemberInfo>> _memberInfosByType = new Dictionary<Type, IEnumerable<MemberInfo>>();

        /// <summary>
        /// Initializes a new <see cref="SourceFileLocationService"/> instance.
        /// </summary>
        /// <param name="providerFactories">Ordered set of factories that can supply <see cref="ISourceFileProvider"/>
        /// instances to perform implementation-specific file location.
        /// </param>
        /// <param name="filenameMap">A file name to ID mapping cache to use to keep file names as integer ID's.
        /// </param>
        internal SourceFileLocationService(IEnumerable<ISourceFileProviderFactory> providerFactories, FilenameMap filenameMap)
        {
            Debug.Assert(providerFactories != null && providerFactories.Count() > 0, "providerFactories are required");
            Debug.Assert(filenameMap != null, "filenameMap is required");

            this._filenameMap = filenameMap;

            // Get the provider from each factory, and store our ordered list of providers
            this._providers = providerFactories.Select(f => f.CreateProvider()).ToArray();
        }

        /// <summary>
        /// Returns the collection of files that jointly define the given type.
        /// </summary>
        /// <param name="type">The type whose set of files is needed.  It cannot be null.</param>
        /// <returns>The collection of the source file names that define this type.  These names will be absolute paths.
        /// </returns>
        public IEnumerable<string> GetFilesForType(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            //  Get the list of members for this type
            IEnumerable<MemberInfo> memberInfos = this.GetMembersForType(type);

            // Get the distinct set of file ID's for these members
            IEnumerable<int> ids = memberInfos.Select<MemberInfo, int>(m => this._fileIdsByMemberInfo[m]).Distinct();

            // Get the set of non-blank file names from these ID's
            IEnumerable<string> files = ids.Select<int, string>(i => this._filenameMap[i]).Where(f => !string.IsNullOrEmpty(f));

            return files;
        }

        /// <summary>
        /// Returns the full path of the source file that defines the specified member
        /// </summary>
        /// <param name="memberInfo">The member whose file we want to identify.  It cannot be null.</param>
        /// <returns>The full path of the source file or null if it cannot be determined.</returns>
        public string GetFileForMember(MemberInfo memberInfo)
        {
            if (memberInfo == null)
            {
                throw new ArgumentNullException("memberInfo");
            }

            // Either load the members or retrieve them from the cache.
            // In either case, this memberInfo should be in the cache after the call.
            this.GetMembersForType(memberInfo.DeclaringType);
            string fileName = null;
            int id = -1;

            // We expect this test to succeed always, because the code above should have loaded all members
            if (this._fileIdsByMemberInfo.TryGetValue(memberInfo, out id))
            {
                // We are guaranteed the ID is a valid key, otherwise it would not have been returned to us.
                fileName = this._filenameMap[id];

                // We maintain an empty string to mean "not found"
                if (fileName.Length == 0)
                {
                    fileName = null;
                }
            }

            return fileName;
        }

        /// <summary>
        /// Adds (if not already present) the specified <paramref name="memberInfo"/> to the
        /// cache associating members with their declaring file.
        /// </summary>
        /// <param name="memberInfo">The <see cref="MemberInfo"/> to cache.</param>
        /// <param name="provider">The implementation-specific provider to invoke if do not already have in cache.</param>
        private void AddMemberInfoToCache(MemberInfo memberInfo, ISourceFileProvider provider)
        {
            Debug.Assert(provider != null, "provider is required");

            // Each MemberInfo is checked at most once, regardless which Type discovers it
            if (!this._fileIdsByMemberInfo.ContainsKey(memberInfo))
            {
                // Invoke the implementation specific code to locate the file
                string fileName = provider.GetFileForMember(memberInfo);

                // To permit multiple providers weighing in with better information
                // later, we do not cache the misses yet
                if (!string.IsNullOrEmpty(fileName))
                {
                    // Assign it a unique ID and cache it by ID
                    int id = this._filenameMap.AddOrGet(fileName);

                    // Associate this memberInfo with that ID
                    this._fileIdsByMemberInfo[memberInfo] = id;
                }
            }
        }

        /// <summary>
        /// Loads into the cache all the members for the given type.
        /// </summary>
        /// <param name="type">The type to load</param>
        /// <returns>A dictionary keyed by <see cref="MemberInfo"/> and containing the id's of the files</returns>
        private IEnumerable<MemberInfo> LoadAllMembersForType(Type type)
        {
            Debug.Assert(type != null, "Type cannot be null");
            List<MemberInfo> result = new List<MemberInfo>();

            // Let each factory provider an object that knows how to find the source file.
            // We maintain a single MemberInfo cache for all factories, meaning the first
            // provider with an answer wins.  This allows multiple providers to server as
            // fallbacks if the higher-precedence providers cannot determine the answer
            foreach (ISourceFileProvider provider in this._providers)
            {
                // Scan all methods.
                // Note that we scan all members, even the inherited ones.
                // The location services are capable of crossing type boundaries when searching for members' files
                MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (MethodBase method in methods)
                {
                    result.Add(method);
                    this.AddMemberInfoToCache(method, provider);
                }

                // Scan all ctors
                ConstructorInfo[] ctors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (ConstructorInfo ctor in ctors)
                {
                    result.Add(ctor);
                    this.AddMemberInfoToCache(ctor, provider);
                }

                // scan all properties
                PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (PropertyInfo property in properties)
                {
                    result.Add(property);
                    this.AddMemberInfoToCache(property, provider);
                }
            }

            // Final sweep, for any MemberInfo's that have no file from any
            // provider, give them a null file name so we don't try again.
            // We delay this final null insertion to allow multiple providers
            // to be given a chance to provide non-null file names if the
            // first provider could not.
            foreach (MemberInfo memberInfo in result)
            {
                if (!this._fileIdsByMemberInfo.ContainsKey(memberInfo))
                {
                    // Use a single common ID for the null case
                    int id = this._filenameMap.AddOrGet(null);
                    this._fileIdsByMemberInfo[memberInfo] = id;
                }
            }
            return result;
        }

        /// <summary>
        /// Returns the cached set of members for the given type.
        /// </summary>
        /// <remarks>Loads the cache for this type the first time it is called.
        /// </remarks>
        /// <param name="type">The type whose methods are required</param>
        /// <returns>The set of <see cref="MemberInfo"/>s for this type.</returns>
        private IEnumerable<MemberInfo> GetMembersForType(Type type)
        {
            Debug.Assert(type != null, "Type cannot be null");
            IEnumerable<MemberInfo> result = null;
            if (!this._memberInfosByType.TryGetValue(type, out result))
            {
                // Not in cache -- always fill cache for all methods in type on first request.
                result = this.LoadAllMembersForType(type);
                this._memberInfosByType[type] = result;
            }
            return result;
        }

        #region IDisposable Members

        public void Dispose()
        {
            foreach (ISourceFileProvider provider in this._providers)
            {
                provider.Dispose();
            }

            this._providers = new ISourceFileProvider[0];
        }

        #endregion
    }
}
