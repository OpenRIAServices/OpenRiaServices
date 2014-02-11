using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace OpenRiaServices.DomainServices.Tools
{
    /// <summary>
    /// Class to maintain a cache of RIA Links (&lt;LinkedServerProject&gt;)
    /// visible from the project references of a client project.
    /// </summary>
    internal class LinkedServerProjectCache
    {
        // The name of the MSBuild property for RIA Links
        private const string LinkedServerProjectPropertyName = "LinkedOpenRiaServerProject";

        private string _rootProjectPath;
        private string _historyFilePath;
        private ILogger _logger;
        private ProjectFileReader _projectFileReader;
        private Dictionary<string, string> _linkedServerProjectsByProject;
        private bool _isFileCacheCurrent;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="rootProjectPath">Full file path to client project to use as root.</param>
        /// <param name="historyFilePath">Full file path to file to read and write in-memory cache.</param>
        /// <param name="logger">Instance of an <see cref="ILogger"/> to receive messages.</param>
        /// <param name="projectFileReader">Instance to use to read the project files.</param>
        internal LinkedServerProjectCache(string rootProjectPath, string historyFilePath, ILogger logger, ProjectFileReader projectFileReader)
        {
            if (string.IsNullOrEmpty(rootProjectPath))
            {
                throw new ArgumentNullException("rootProjectPath");
            }

            if (string.IsNullOrEmpty(historyFilePath))
            {
                throw new ArgumentNullException("historyFilePath");
            }

            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            if (projectFileReader == null)
            {
                throw new ArgumentNullException("projectFileReader");
            }

            this._rootProjectPath = rootProjectPath;
            this._historyFilePath = historyFilePath;
            this._logger = logger;
            this._projectFileReader = projectFileReader;
        }

        /// <summary>
        /// Gets the value indicating whether the file on disk is
        /// current with respect to the in-memory contents.
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
        /// Gets the dictionary associating client project paths with their server project identified
        /// via a RIA Link.
        /// </summary>
        /// <remarks>
        /// This dictionary will lazily load from the cache or the project the first time it is accessed.
        /// The key is the full path to a client project found via a project reference from our root project.
        /// The value associated with the key is the full path of the server project identified via a RIA Link.
        /// Null or empty values are legal and indicate the corresponding client project has no RIA Link.
        /// Keys are case-insensitive, so keys differing only in case are considered the same.
        /// There is no entry for the root project itself.
        /// </remarks>
        internal Dictionary<string, string> LinkedServerProjectsByProject
        {
            get
            {
                if (this._linkedServerProjectsByProject == null)
                {
                    this._linkedServerProjectsByProject = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    if (!this.LoadCacheFromFile())
                    {
                        this.LoadCacheFromProject();
                    }
                }
                return this._linkedServerProjectsByProject;
            }
        }

        /// <summary>
        /// Indexer property that associates a RIA Link project with a given client <paramref name="projectPath"/>
        /// </summary>
        /// <value>
        /// The full name of the server project considered as the target of a RIA Link
        /// from the perspective of the given <paramref name="projectPath"/>.
        /// A null or empty string means there is no RIA Link for <paramref name="projectPath"/>
        /// </value>
        /// <param name="projectPath">Full path to the client project file considered the source of the RIA Link.</param>
        /// <returns>The linked server project declared in <paramref name="projectPath"/>. It may be null or empty.
        /// </returns>
        internal string this[string projectPath]
        {
            get
            {
                if (string.IsNullOrEmpty(projectPath))
                {
                    throw new ArgumentNullException("projectPath");
                }
                string result = null;
                this.LinkedServerProjectsByProject.TryGetValue(projectPath, out result);
                return result;
            }
            set
            {
                if (string.IsNullOrEmpty(projectPath))
                {
                    throw new ArgumentNullException("projectPath");
                }
                this.LinkedServerProjectsByProject[projectPath] = value;
                this.IsFileCacheCurrent = false;
            }
        }

        /// <summary>
        /// Gets the set of full project paths to all projects referenced by
        /// our root project.
        /// </summary>
        internal IEnumerable<string> ProjectReferences
        {
            get
            {
                return this.LinkedServerProjectsByProject.Keys;
            }
        }

        /// <summary>
        /// Gets the set of full project paths to all the server projects
        /// visible through a RIA Link from one of the client projects
        /// found in <see cref="ProjectReferences"/>
        /// </summary>
        internal IEnumerable<string> LinkedServerProjects
        {
            get
            {
                // Filter out those with an empty or null RIA Link.
                // Also apply Distinct because multiple client projects might point to the same RIA Link server project
                return this.LinkedServerProjectsByProject.Values.Where(s => !string.IsNullOrEmpty(s)).Distinct();
            }
        }

        /// <summary>
        /// Given the name of a server project file known to be the destination of a RIA Link,
        /// this method returns all the client projects that have a RIA Link to it.
        /// </summary>
        /// <remarks>
        /// Generally there is only one client file in a solution pointing to any given server
        /// project via a RIA link, but we use a collection because it is possible to have many.
        /// </remarks>
        /// <param name="linkedServerProject">Full path to server project file known to be the destination of a RIA Link</param>
        /// <returns>The set of all client project paths that have a RIA link to <paramref name="linkedServerProject"/></returns>
        internal IEnumerable<string> GetLinkedServerProjectSources(string linkedServerProject)
        {
            return this.ProjectReferences.Where(s => String.Equals(linkedServerProject, this[s], StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Writes our in-memory cache to the history file specified in the ctor.
        /// </summary>
        /// <remarks>
        /// If the in-memory cache shows the client project has no project references
        /// and therefore no RIA Links, the file will be written, but it will be empty.
        /// This is intentional so that a subsequent <see cref="LoadCacheFromFile"/>
        /// can return to that empty state rather than force reopening the client project.
        /// </remarks>
        /// <returns><c>true</c> means the file was written, <c>false</c> if nothing was written.</returns>
        internal bool SaveCacheToFile()
        {
            // If nothing to save, tell the caller and let them decide what to do
            if (string.IsNullOrEmpty(this._historyFilePath) || this._linkedServerProjectsByProject == null)
            {
                return false;
            }

            // Format is:
            //  One line per project: path, lastWriteTime, linked server project
            StringBuilder sb = new StringBuilder();
            foreach (string projectPath in this.LinkedServerProjectsByProject.Keys)
            {
                StringBuilder sb1 = new StringBuilder();
                sb1.Append(projectPath);
                sb1.Append(',');
                sb1.Append(File.GetLastWriteTime(projectPath).Ticks.ToString(CultureInfo.InvariantCulture));
                sb1.Append(',');

                string linkedServerProject = this[projectPath];
                if (!string.IsNullOrEmpty(linkedServerProject))
                {
                    sb1.Append(linkedServerProject);
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
        /// Loads the internal state from the history file passed to the ctor.
        /// </summary>
        /// <remarks>
        /// If the root project has been modified since the history file
        /// was written, the entire cache is considered invalid and <c>false</c> is returned.
        /// If any cached project has been touched since the cache was last written, just
        /// is portion of the cache will be reloaded from the project file.
        /// </remarks>
        /// <returns>A <c>true</c> means the cache was loaded from the history file successfully.
        /// If we detect the cache is out of date or does not exist, a <c>false</c> is returned.
        /// </returns>
        internal bool LoadCacheFromFile()
        {
            this.IsFileCacheCurrent = false;

            // If the history file does not exist (such as after a Clean), we cannot load
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
            //  One line per project: client project path, lastWriteTime of it, linked server project name
            string fileContents = File.ReadAllText(this._historyFilePath);
            string[] projectEntries = fileContents.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string projectEntry in projectEntries)
            {
                string linkedServerProject = null;
                string[] projectItems = projectEntry.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                // Fewer than 2 is formatting problem -- maybe the file is corrupt.  Just discard cache and rebuild.
                if (projectItems.Length < 2)
                {
                    // Always clear out any partial results when returning false
                    this.LinkedServerProjectsByProject.Clear();
                    return false;
                }
                string projectPath = projectItems[0];

                // If that project no longer exists, remove it from the cache but keep running
                if (!File.Exists(projectPath))
                {
                    continue;
                }

                // If the client project has been touched since we wrote our history file,
                // its RIA Link might have changed, so go retrieve it.  It could be empty or null.
                projectWriteTime = File.GetLastWriteTime(projectPath);
                if (projectWriteTime.CompareTo(breadCrumbWriteTime) > 0)
                {
                    linkedServerProject = this.LoadRiaLinkFromProject(projectPath);
                }
                else
                {
                    // Projects with no RIA Link have only the first 2 items (name and timestamp).
                    // Those will be added to our cache with an empty RIA Link project to show we know this,
                    // otherwise we would need to open the projects again to know.
                    if (projectItems.Length == 3)
                    {
                        linkedServerProject = projectItems[2];
                    }
                }
                this[projectPath] = linkedServerProject;
            }

            this.IsFileCacheCurrent = true;
            return true;
        }

        /// <summary>
        /// Loads the in-memory cache by reading the root project, discovering all the
        /// project references, and evaluating the &lt;LinkedServerProject&gt; property
        /// (i.e. the RIA Link) for each.
        /// </summary>
        internal void LoadCacheFromProject()
        {
            // Empty cache and show that file is out-of-date.
            // This forces the cache write on shutdown to guarantee we use the
            // file cache copy even if there are no references found below.
            this._linkedServerProjectsByProject.Clear();
            this.IsFileCacheCurrent = false;

            // Ask for all the project-to-project references
            IEnumerable<string> projectPaths = this._projectFileReader.LoadProjectReferences(this._rootProjectPath);

            // Add an entry in our cache for every project reference, whether it has a RIA Link or not
            foreach (string projectPath in projectPaths)
            {
                string linkedServerProject = this.LoadRiaLinkFromProject(projectPath);
                this[projectPath] = linkedServerProject;
            }
        }

        /// <summary>
        /// Given the full path to a project, return the value of the &lt;LinkedServerProject&gt; property,
        /// expanded to a full path.
        /// </summary>
        /// <param name="projectPath">The full path to the project possibly containing this property.</param>
        /// <returns>The full path of the RIA link.  It may be null or empty.</returns>
        internal string LoadRiaLinkFromProject(string projectPath)
        {
            string linkedServerProject = this._projectFileReader.GetPropertyValue(projectPath, LinkedServerProjectCache.LinkedServerProjectPropertyName);

            // RIA Links are usually relative -- convert to full
            if (!string.IsNullOrEmpty(linkedServerProject))
            {
                linkedServerProject = ProjectFileReader.ConvertToFullPath(linkedServerProject, projectPath);

                // Emit a message to help user see we found a RIA Link
                this._logger.LogMessage(string.Format(CultureInfo.CurrentCulture, Resource.RIA_Link_Present, projectPath, linkedServerProject));
            }
            return linkedServerProject;
        }
    }
}
