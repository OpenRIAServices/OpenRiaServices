using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;


namespace OpenRiaServices.Tools
{
    /// <summary>
    /// This class is responsible for managing the cache of
    /// source file names for all projects visible to a root project.
    /// </summary>
    /// <remarks>
    /// This class is used by the <see cref="CreateOpenRiaClientFilesTask"/> to
    /// capture the list of project-to-project references from the root
    /// project and all the source files within those projects.  It
    /// has the ability to write this information to disk between builds to
    /// enhance performance of incremental builds.
    /// </remarks>
    internal class ProjectSourceFileCache
    {
        private readonly string _rootProjectPath;
        private readonly string _historyFilePath;
        private readonly ILogger _logger;
        private Dictionary<string, IEnumerable<string>> _sourceFilesByProject;
        private List<string> _allFiles;
        private readonly ProjectFileReader _projectFileReader;
        private bool _isFileCacheCurrent;

        /// <summary>
        /// Sole constructor
        /// </summary>
        /// <param name="rootProjectPath">Full path to the root project file.</param>
        /// <param name="historyFilePath">Full path to the file where we are allowed to write results between builds.</param>
        /// <param name="logger">The <see cref="ILogger"/> to use for warnings and errors.</param>
        /// <param name="projectFileReader">Instance to use to read the project files.</param>
        internal ProjectSourceFileCache(string rootProjectPath, string historyFilePath, ILogger logger, ProjectFileReader projectFileReader)
        {
            if (string.IsNullOrEmpty(rootProjectPath))
            {
                throw new ArgumentNullException(nameof(rootProjectPath));
            }

            if (string.IsNullOrEmpty(historyFilePath))
            {
                throw new ArgumentNullException(nameof(historyFilePath));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (projectFileReader == null)
            {
                throw new ArgumentNullException(nameof(projectFileReader));
            }

            this._rootProjectPath = rootProjectPath;
            this._historyFilePath = historyFilePath;
            this._projectFileReader = projectFileReader;
            this._logger = logger;
        }

        /// <summary>
        /// Gets the value indicating whether the file on disk is
        /// current with respect to the in-memory contents
        /// </summary>
        internal bool IsFileCacheCurrent
        {
            get
            {
                return this._isFileCacheCurrent;
            }
            private set
            {
                this._isFileCacheCurrent = value;
            }
        }

        /// <summary>
        /// Gets the cache of known source file names keyed by project name.
        /// </summary>
        /// <value>
        /// The value is a dictionary keyed by full path to the project file(s),
        /// containing the set of known source file names for that project.
        /// The first access of this property allocates the data structure
        /// but does not load it.  The keys are case-insensitive.
        /// </value>
        internal Dictionary<string, IEnumerable<string>> SourceFilesByProject
        {
            get
            {
                if (this._sourceFilesByProject == null)
                {
                    this._sourceFilesByProject = new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase);
                }
                return this._sourceFilesByProject;
            }
        }

        /// <summary>
        /// Indexer property that associates a set of source file names
        /// with a project via the <see cref="SourceFilesByProject"/> property.
        /// </summary>
        /// <value>
        /// The set of files to associate with this project.  <c>Null</c>
        /// is treated as a request to remove the associated project from
        /// the internal cache.  An empty list is permitted and adds an
        /// entry for the given project.  It means that we know that project
        /// has been analyzed and has no files, whereas a null means
        /// we don't know.
        /// </value>
        /// <param name="projectPath">Full path to the project file.</param>
        /// <returns>The set of source file names associated with the given <paramref name="projectPath"/>.
        /// A <c>null</c> indicates no files are associated with it.</returns>
        internal IEnumerable<string> this[string projectPath]
        {
            get
            {
                if (string.IsNullOrEmpty(projectPath))
                {
                    throw new ArgumentNullException(nameof(projectPath));
                }
                IEnumerable<string> result = null;
                this.SourceFilesByProject.TryGetValue(projectPath, out result);
                return result;
            }
            set
            {
                if (string.IsNullOrEmpty(projectPath))
                {
                    throw new ArgumentNullException(nameof(projectPath));
                }
                this.IsFileCacheCurrent = false;
                this.SourceFilesByProject[projectPath] = value;
            }
        }

        /// <summary>
        /// Returns the list of all project file names, including the root project
        /// and all the projects to which is has project references.
        /// </summary>
        /// <remarks>
        /// This method will load the internal data structures the first time it is
        /// invoked.  Subsequent loads will reuse the cached data.
        /// </remarks>
        /// <returns>The collection of full project paths for the root project and all its project references.</returns>
        internal IEnumerable<string> GetAllKnownProjects()
        {
            return this.GetOrLoadSourceFilesByProject().Keys;
        }

        /// <summary>
        /// Returns the list of  source file names from the given project, which is assumed to be
        /// one of the projects returned by <see cref="GetAllKnownProjects"/>
        /// </summary>
        /// <remarks>
        /// This method will load the internal data structures the first time it is
        /// invoked.  An unknown <paramref name="projectPath"/> will return <c>null</c>.
        /// </remarks>
        /// <param name="projectPath">The full path of a project file.</param>
        /// <returns>The collection of the full paths of the files.  This list may be empty.
        /// A <c>null</c> return indicates the given <paramref name="projectPath"/> is not known.
        /// </returns>
        internal IEnumerable<string> GetSourceFilesInProject(string projectPath)
        {
            if (string.IsNullOrEmpty(projectPath))
            {
                throw new ArgumentNullException(nameof(projectPath));
            }
            IEnumerable<string> files = null;
            this.GetOrLoadSourceFilesByProject().TryGetValue(projectPath, out files);
            return files;
        }

        /// <summary>
        /// Retrieves the set of all known source file names from the root
        /// project and all the projects it references.
        /// </summary>
        /// <returns>The collection of full paths to all known source files.</returns>
        internal IEnumerable<string> GetSourceFilesInAllProjects()
        {
            if (_allFiles == null)
            {
                List<string> files = new List<string>();
                foreach (string projectPath in this.GetAllKnownProjects())
                {
                    files.AddRange(this.GetSourceFilesInProject(projectPath));
                }
                _allFiles = files;    
            }
            
            return _allFiles;
        }

        /// <summary>
        /// Writes our internal knowledge of project references and their source file names
        /// to the breadcrumb file passed to the ctor.
        /// </summary>
        /// <returns><c>true</c> means the file was written, <c>false</c> if nothing was written.</returns>
        internal bool SaveCacheToFile()
        {
            // If nothing to save, tell the caller and let them decide what to do
            if (this._sourceFilesByProject == null)
            {
                return false;
            }

            // Format is:
            //  One line per project: path, lastWriteTime, list of source files
            StringBuilder sb = new StringBuilder();
            foreach (string projectPath in this.SourceFilesByProject.Keys)
            {
                StringBuilder sb1 = new StringBuilder();
                sb1.Append(projectPath);
                sb1.Append(',');
                sb1.Append(File.GetLastWriteTime(projectPath).Ticks.ToString(CultureInfo.InvariantCulture));
                sb1.Append(',');
                foreach (string file in this.SourceFilesByProject[projectPath])
                {
                    // We convert files to relative paths to save space
                    string relativeFilePath = ProjectFileReader.ConvertToRelativePath(file, projectPath);
                    sb1.Append(relativeFilePath);
                    sb1.Append(',');
                }
                sb.AppendLine(sb1.ToString());
            }

            Exception exception = null;
            try
            {
                File.WriteAllText(this._historyFilePath, sb.ToString());
            }
            catch (IOException ioe)
            {
                exception = ioe;
            }
            catch (UnauthorizedAccessException uae)
            {
                exception = uae;
            }
            if (exception != null)
            {
                this._logger.LogWarning(string.Format(CultureInfo.CurrentCulture, Resource.Failed_To_Write_File, this._historyFilePath, exception.Message));
                return false;
            }
            this.IsFileCacheCurrent = true;
            return true;
        }


        /// <summary>
        /// Gets (or loads if this is the first call) the cache of all known project files
        /// and their associated source files.
        /// </summary>
        /// <returns>A dictionary keyed by full project name and containing all the source files for that project.</returns>
        internal Dictionary<string, IEnumerable<string>> GetOrLoadSourceFilesByProject()
        {
            // The count is zero only prior to the first load because we always have
            // at least one entry for the root project itself, even if it has no files
            if (this.SourceFilesByProject.Count == 0)
            {
                // Attempt to load from breadcrumb file.
                // If that fails or is out of date, read the project files directly
                if (!this.LoadCacheFromFile())
                {
                    this.LoadSourceFilesFromProjects();
                }
            }
            return this.SourceFilesByProject;
        }

        /// <summary>
        /// Loads the internal state from the breadcrumb file passed to the ctor.
        /// </summary>
        /// <remarks>
        /// If the root project has been modified since the breadcrumb file
        /// was written, the entire cache is considered invalid and <c>false</c> is returned.
        /// If any cached project has been touched since the cache was last written, just
        /// is portion of the cache will be reloaded from the project file.
        /// </remarks>
        /// <returns>A <c>true</c> means the cache was loaded from the breadcrumb file successfully.
        /// If we detect the cache is out of date or does not exist, a <c>false</c> is returned.
        /// </returns>
        internal bool LoadCacheFromFile()
        {
            this.IsFileCacheCurrent = false;

            if (!File.Exists(this._historyFilePath))
            {
                return false;
            }

            // If the root project itself has been touched since the
            // time we wrote the file, we can't trust anything in our cache
            DateTime projectWriteTime = File.GetLastWriteTime(this._rootProjectPath);
            DateTime breadCrumbWriteTime = File.GetLastWriteTime(this._historyFilePath);
            if (projectWriteTime.CompareTo(breadCrumbWriteTime) > 0)
            {
                return false;
            }

            // Read the breadcrumb file.
            // Format is:
            //  One line per project: path, lastWriteTime, list of source files separated by commas
            string fileContents = File.ReadAllText(this._historyFilePath);
            string[] projectEntries = fileContents.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string projectEntry in projectEntries)
            {
                string[] projectItems = projectEntry.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                // Fewer than 2 is formatting problem -- maybe the file is corrupt.  Just discard cache and rebuild.
                if (projectItems.Length < 2)
                {
                    // Always clear out any partial results when returning false
                    this.SourceFilesByProject.Clear();
                    return false;
                }
                string projectPath = projectItems[0];

                // If that project no longer exists, remove it from the cache but keep running
                if (!File.Exists(projectPath))
                {
                    continue;
                }

                List<string> files = new List<string>();

                // Projects with no source files have only the first 2 items (name and timestamp).
                // Those will be added to our cache with an empty list to show that we know they
                // have no source files (otherwise, we would need to open them again to know).
                // Any project with some number of source files falls into the body of this 'if'
                if (projectItems.Length >= 3)
                {
                    // Check whether the project file was touched since the last time
                    // we analyzed it.  Failure to parse last write time or discovery
                    // the project has been touched more recently causes us to reload
                    // just that project.  Incidentally, the use of Ticks here is more
                    // reliable than DateTime.ToString() which does not round-trip accurately.
                    projectWriteTime = File.GetLastWriteTime(projectPath);
                    long breadCrumbWriteTimeTicks = 0;
                    if (!long.TryParse(projectItems[1], out breadCrumbWriteTimeTicks) || projectWriteTime.Ticks > breadCrumbWriteTimeTicks)
                    {
                        // Go load from the project file and ignore what we had cached
                        files.AddRange(this._projectFileReader.LoadSourceFilesFromProject(projectPath));
                    }
                    else
                    {
                        // The last write time shows the project has not changed since
                        // we cached the results, so extract them from the text
                        for (int i = 2; i < projectItems.Length; ++i)
                        {
                            string file = projectItems[i];
                            if (string.IsNullOrEmpty(file))
                            {
                                continue;
                            }

                            // We write relative paths, so convert back to full
                            string fullFilePath = ProjectFileReader.ConvertToFullPath(file, projectPath);

                            // If the file has ceased to exist, but the project says it
                            // does, we do not add it to our internal lists
                            if (File.Exists(fullFilePath))
                            {
                                files.Add(fullFilePath);
                            }
                        }
                    }
                }
                this[projectPath] = files;
            }
            this.IsFileCacheCurrent = true;
            return true;
        }


        /// <summary>
        /// Loads the internal state by opening the root project and all its referenced projects.
        /// </summary>
        /// <remarks>This method does not use the cache and is expensive, 
        /// requiring on the order of 1/2 second per project opened.
        /// </remarks>
        internal void LoadSourceFilesFromProjects()
        {
            // Always show file copy is out of date, so we write even if we have no references
            this.IsFileCacheCurrent = false;

            // Ask for all the project-to-project references
            IEnumerable<string> projectPaths = this._projectFileReader.LoadProjectReferences(this._rootProjectPath);

            // Always record the list of source files for the root project itself.
            // We add entries even if the list of source files is empty to record
            // the fact we *know* that list is empty.  This avoids the need to reanalyze it.
            this[this._rootProjectPath] = this._projectFileReader.LoadSourceFilesFromProject(this._rootProjectPath);

            // And for each project, ask for the list of source files.  We always capture the list, even if it
            // is empty.  This tells us there are no files to copy the next time we read the breadcrumb file,
            // allowing us to avoid rescanning the project's list of source files
            foreach (string projectPath in projectPaths)
            {
                IEnumerable<string> sourceFiles = this._projectFileReader.LoadSourceFilesFromProject(projectPath);
                this[projectPath] = sourceFiles;
            }
        }        
    }
}
