using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using OpenRiaServices.Tools.Logging;
using OpenRiaServices.Tools.SharedTypes;

namespace OpenRiaServices.Tools
{
    /// <summary>
    /// Custom MSBuild task to generate client proxy classes from server's entities and business objects
    /// </summary>
    public class CreateOpenRiaClientFilesTask : RiaClientFilesTask
    {
        private string _serverProjectDirectory;
        private string _serverOutputPath;
        private string _serverRootNamespace;
        private readonly List<ITaskItem> _generatedFiles = new List<ITaskItem>();
        private readonly List<ITaskItem> _copiedFiles = new List<ITaskItem>();
        private ProjectFileReader _projectFileReader;
        private ProjectSourceFileCache _serverProjectSourceFileCache;
        private LinkedServerProjectCache _linkedServerProjectCache;
        private IEnumerable<string> _clientAssemblyPathsNormalized;
        private HashSet<string> _clientReferenceAssembliesNormalized;
        private IEnumerable<string> _linkedFilesNormalized;
        private Dictionary<string, IList<string>> _sharedFilesByProject;

        /// <summary>
        /// Gets or sets root namespace of the client project
        /// </summary>
        public string ClientProjectRootNamespace { get; set; }

        /// <summary>
        /// Gets or sets the list of core server assemblies to analyze to extract the business objects
        /// </summary>
        /// <value>
        /// This property is required.  Normal MSBuild semantics will not permit this task
        /// to be used unless it has been set.
        /// </value>
        [Required]
        public ITaskItem[] ServerAssemblies { get; set; }

        /// <summary>
        /// Gets or sets the list of reference assemblies used to build the server assemblies
        /// </summary>
        [Required]
        public ITaskItem[] ServerReferenceAssemblies { get; set; }

        /// <summary>
        /// Gets or sets the list of reference assemblies used to build the server assemblies
        /// </summary>
        [Required]
        public ITaskItem[] ClientReferenceAssemblies { get; set; }

        /// <summary>
        /// Gets or sets a value containing the paths to search for
        /// client assemblies when it is necessary to locate referenced
        /// assemblies.
        /// </summary>
        [Required]
        public ITaskItem[] ClientAssemblySearchPaths { get; set; }

        /// <summary>
        /// Gets or sets the list of source files used by the client project (i.e. the @(Compile) item collection)
        /// </summary>
        [Required]
        public ITaskItem[] ClientSourceFiles { get; set; }

        /// <summary>
        /// Gets or sets the location of mscorlib.dll and the rest of the target framework for the client.
        /// </summary>
        [Required]
        public string ClientFrameworkPath { get; set; }

        /// <summary>
        /// Gets or sets the language in which to generate the client proxies.
        /// </summary>
        /// <value>
        /// Currently supported values are currently "C#" or "VB"
        /// This property is required.  Normal MSBuild semantics will not permit this task
        /// to be used unless it has been set.
        /// </value>
        [Required]
        public string Language { get; set; }

        /// <summary>
        /// Gets or sets the path to the server's project file
        /// </summary>
        /// <value>This value is required.</value>
        [Required]
        public string ServerProjectPath { get; set; }

        /// <summary>
        /// Gets the string form of a boolean that indicates
        /// whether the client project is an application.
        /// </summary>
        /// <value>A string value of "true" means the client project
        /// is an application.  If this value is empty or any other
        /// string value, the client project is assumed to be a
        /// class library.</value>
        public string IsClientApplication { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the code generator
        /// should generate fully qualified type names.
        /// </summary>
        /// <value>
        /// A string value of "true" means fully qualified type names
        /// should be generated.  Any other value indicates the code
        /// generator can use short type names at its discretion.
        /// </value>
        public string UseFullTypeNames { get; set; }

        /// <summary>
        /// Gets or sets the name of the code generator to use.
        /// </summary>
        /// <value>
        /// This value specifies the logical name of the code
        /// generator and is used to select one when multiple
        /// are available.  A <c>null</c> value will select
        /// the default code generator.
        /// </value>
        public string CodeGeneratorName { get; set; }

        /// <summary>
        /// Gets the string form of a boolean that indicates
        /// whether the shared files should be copied (instead of linked).
        /// </summary>
        public string SharedFilesMode { set => LinkSharedFilesInsteadOfCopy = !string.Equals(value, OpenRiaSharedFilesMode.Copy.ToString(), StringComparison.OrdinalIgnoreCase); }

        private bool LinkSharedFilesInsteadOfCopy { get; set; } = true;

        /// <summary>
        /// Gets the list of code files created by this task.
        /// </summary>
        /// <value>
        /// This value can be read at any time, but it will be valid only after
        /// the task has completed.  Its results are not predictable if the task
        /// has terminated abnormally.  This list contains only those code files
        /// created by the code-generation task.  It does not include any of the
        /// files named *.shared.".
        /// </value>
        [Output]
        public ITaskItem[] GeneratedFiles
        {
            get
            {
                ITaskItem[] items = this._generatedFiles.ToArray();
                return items;
            }
        }

        /// <summary>
        /// Gets the list of files copied by this task from the server project(s)
        /// </summary>
        /// <value>
        /// This value can be read at any time, even before the task has executed.
        /// </value>
        [Output]
        public ITaskItem[] SharedFiles
        {
            get
            {
                Dictionary<string, IList<string>> sharedFilesByProject = this.SharedFilesByProject;
                List<ITaskItem> result = new List<ITaskItem>();
                foreach (var filesByProject in sharedFilesByProject)
                {
                    foreach (string file in filesByProject.Value)
                    {
                        result.Add(new TaskItem(file));
                    }
                }
                return result.ToArray();
            }
        }

        /// <summary>
        /// Gets the list of shared files that have been copied into the client project
        /// </summary>
        /// <value>
        /// This value can be read at any time, but it will be valid only after
        /// the task has completed.  Its results are not predictable if the task
        /// has terminated abnormally.
        /// </value>
        [Output]
        public ITaskItem[] CopiedFiles
        {
            get
            {
                return this._copiedFiles.ToArray();
            }
        }

        /// <summary>
        /// Gets the list of files shared with the server project(s) via file links.
        /// </summary>
        /// <value>
        /// This value can be read at any time, but it will be valid only after
        /// the task has completed.  Its results are not predictable if the task
        /// has terminated abnormally.
        /// </value>
        [Output]
        public ITaskItem[] LinkedFiles
        {
            get
            {
                return this.LinkedFilesNormalized.Select<string, ITaskItem>(f => new TaskItem(f)).ToArray();
            }
        }

        /// <summary>
        /// Gets the root namespace of the server project.
        /// </summary>
        internal string ServerProjectRootNameSpace
        {
            get
            {
                if (this._serverRootNamespace == null)
                {
                    ProjectFileReader reader = this.ProjectFileReader;
                    this._serverRootNamespace = reader.GetPropertyValue(this.ServerProjectPath, "RootNamespace");
                }
                return this._serverRootNamespace;
            }
        }

        /// <summary>
        /// Gets the server project's output path.
        /// </summary>
        internal string ServerOutputPath
        {
            get
            {
                if (this._serverOutputPath == null)
                {
                    ProjectFileReader reader = this.ProjectFileReader;
                    string path = reader.GetPropertyValue(this.ServerProjectPath, "OutputPath");
                    this._serverOutputPath = GetFullPathRelativeToDirectory(path, this.ServerProjectDirectory);
                }
                return this._serverOutputPath;
            }
        }

        /// <summary>
        /// Gets a dictionary, keyed by project path and containing all the *.shared.* files known
        /// to that project.
        /// </summary>
        internal Dictionary<string, IList<string>> SharedFilesByProject
        {
            get
            {
                if (this._sharedFilesByProject == null)
                {
                    this._sharedFilesByProject = this.GetSharedFilesByProject();
                }
                return this._sharedFilesByProject;
            }
        }

        /// <summary>
        /// Gets the set of linked files visible to the client project and the server project, including projects
        /// referenced by the server project.
        /// </summary>
        internal IEnumerable<string> LinkedFilesNormalized
        {
            get
            {
                if (this._linkedFilesNormalized == null)
                {
                    // Compute the transitive closure of all source files visible to server and all its referenced projects
                    IEnumerable<string> serverFiles = this.ServerProjectSourceFileCache.GetSourceFilesInAllProjects();

                    // Convert the full set of explicitly specified source files in the client project
                    IEnumerable<string> clientFiles = this.NormalizedTaskItems(this.ClientSourceFiles, this.ClientProjectDirectory);

                    // Intersect them and remove duplicates.  This permits links to flow in either direction or for
                    // multiple projects to refer to the same files.
                    this._linkedFilesNormalized = clientFiles.Intersect(serverFiles, StringComparer.OrdinalIgnoreCase).ToArray();
                }

                return this._linkedFilesNormalized;
            }
        }

        /// <summary>
        /// Gets the set of client reference assemblies, with relative paths expanded to full paths.
        /// </summary>
        internal ISet<string> ClientReferenceAssembliesNormalized
        {
            get
            {
                if (this._clientReferenceAssembliesNormalized == null)
                {
                    // Expand to full paths
                    IEnumerable<string> currentRefs = this.NormalizedTaskItems(this.ClientReferenceAssemblies, this.ClientProjectDirectory);

                    // Remove duplicates and non-existant files
                    this._clientReferenceAssembliesNormalized = new HashSet<string>(currentRefs.Where(f => File.Exists(f)), StringComparer.OrdinalIgnoreCase);
                }
                return this._clientReferenceAssembliesNormalized;
            }
        }

        /// <summary>
        /// Gets the collection of files written by this task.
        /// </summary>
        /// <value>
        /// This list is a concatenation of <see cref="GeneratedFiles"/> and <see cref="CopiedFiles"/>.
        /// </value>
        [Output]
        public IEnumerable<ITaskItem> OutputFiles
        {
            get
            {
                return (LinkSharedFilesInsteadOfCopy) ? GeneratedFiles : GeneratedFiles.Concat(CopiedFiles);
            }
        }

        /// <summary>
        /// Returns the set of full paths of folders to search for framework and SDK assemblies
        /// when we need to resolve assembly references.
        /// </summary>
        internal IEnumerable<string> ClientAssemblyPathsNormalized
        {
            get
            {
                if (this._clientAssemblyPathsNormalized == null)
                {
                    ITaskItem[] taskItems = this.ClientAssemblySearchPaths;

                    // If the user specified search paths, honor those.
                    // If the user did not specify anything, use the default Silverlight paths
                    this._clientAssemblyPathsNormalized = (taskItems == null)
                                                            ? Array.Empty<string>()
                                                            : this.NormalizedTaskItems(taskItems, this.ClientProjectDirectory)
                                                                .ToArray();
                }
                return this._clientAssemblyPathsNormalized;
            }
        }

        /// <summary>
        /// Gets the boolean equivalent of <see cref="IsClientApplication"/>
        /// </summary>
        /// <value><c>true</c> means the client project is an application,
        /// otherwise it is a class library only.</value>
        internal bool IsClientApplicationAsBool
        {
            get
            {
                string str = this.IsClientApplication;
                bool result;
                if (!string.IsNullOrEmpty(str) && Boolean.TryParse(str, out result))
                {
                    return result;
                }
                return false;
            }
        }

        /// <summary>
        /// Gets the boolean equivalent of <see cref="UseFullTypeNames"/>
        /// </summary>
        /// <value><c>true</c> full type names should be used in code generation.</value>
        internal bool UseFullTypeNamesAsBool
        {
            get
            {
                string str = this.UseFullTypeNames;
                bool result;
                if (!string.IsNullOrEmpty(str) && Boolean.TryParse(str, out result))
                {
                    return result;
                }
                return false;
            }
        }

        internal TargetPlatform ClientTargetPlatform
        {
            get
            {
                if (string.IsNullOrEmpty(ClientFrameworkPath))
                    return TargetPlatform.Unknown;

                if (ClientFrameworkPath.IndexOf("Silverlight", StringComparison.InvariantCultureIgnoreCase) != -1)
                    return TargetPlatform.Silverlight;
                if (ClientFrameworkPath.IndexOf(".NETPortable", StringComparison.InvariantCultureIgnoreCase) != -1)
                    return TargetPlatform.Portable;
                if (ClientFrameworkPath.IndexOf(".NETFramework", StringComparison.InvariantCultureIgnoreCase) != -1)
                    return TargetPlatform.Desktop;
                if (ClientFrameworkPath.IndexOf(".NETCore", StringComparison.InvariantCultureIgnoreCase) != -1)
                    return TargetPlatform.Win8;

                return TargetPlatform.Unknown;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the server project file has been specified
        /// and exists on disk.
        /// </summary>
        internal bool IsServerProjectAvailable
        {
            get
            {
                string serverProject = this.ServerProjectPath;
                return !string.IsNullOrEmpty(serverProject) && File.Exists(serverProject);
            }
        }

        /// <summary>
        /// Gets the <see cref="ProjectFileReader"/> used by all caches that read project files.
        /// </summary>
        /// <value>
        /// This reader implements <see cref="IDisposable"/> and is explicitly disposed after this
        /// custom build task completes.  It is instantiated lazily and may not always be required.
        /// </value>
        private ProjectFileReader ProjectFileReader
        {
            get
            {
                if (this._projectFileReader == null)
                {
                    this._projectFileReader = new ProjectFileReader(/*ILogger*/ this);
                }
                return this._projectFileReader;
            }
        }

        /// <summary>
        /// Gets the absolute path to the server's directory
        /// </summary>
        private string ServerProjectDirectory
        {
            get
            {
                if (this._serverProjectDirectory == null)
                {
                    if (this.ServerProjectPath == null)
                    {
                        this.LogError(string.Format(CultureInfo.CurrentCulture, Resource.ProjectPath_Argument_Required, "ServerProjectPath"));
                        return string.Empty;
                    }
                    // The server project may be relative to the current project.  Allow for that.
                    this._serverProjectDirectory = Path.GetDirectoryName(this.GetFullPathRelativeToDirectory(this.ServerProjectPath, this.ClientProjectDirectory));
                }
                return this._serverProjectDirectory;
            }
        }

        /// <summary>
        /// Gets the file extension to use for the current language
        /// </summary>
        /// <value>Ideally, the CodeDom should provide this, but we want to avoid loading the provider
        /// when we are checking whether there is really any work to do.</value>
        private string FileExtension
        {
            get
            {
                return this.Language.Equals("C#", StringComparison.OrdinalIgnoreCase) ? "cs" : "vb";
            }
        }

        /// <summary>
        /// Gets the cache of server-side source files known to the
        /// server project and all the projects it references.
        /// </summary>
        internal ProjectSourceFileCache ServerProjectSourceFileCache
        {
            get
            {
                if (this._serverProjectSourceFileCache == null)
                {
                    this._serverProjectSourceFileCache = new ProjectSourceFileCache(this.ServerProjectPath, this.SourceFileListPath(), /*ILogger*/this, this.ProjectFileReader);
                }
                return this._serverProjectSourceFileCache;
            }
        }

        /// <summary>
        /// Gets the cache of client project references and their corresponding
        /// server projects if they have a Open RIA Services Link.
        /// </summary>
        internal LinkedServerProjectCache LinkedServerProjectCache
        {
            get
            {
                if (this._linkedServerProjectCache == null)
                {
                    this._linkedServerProjectCache = new LinkedServerProjectCache(this.ClientProjectPath, this.LinkedServerProjectsPath(), /*ILogger*/this, this.ProjectFileReader);
                }
                return this._linkedServerProjectCache;
            }
        }

        /// <summary>
        /// Invoked by MSBuild to run this task
        /// </summary>
        /// <returns>true if task succeeds</returns>
        protected override bool ExecuteInternal()
        {
            // Initialize any residual output items and cached results from a prior execution pass.
            // Note that these fields are explicitly left alone after execution
            // because MSBuild will consume them as the task outputs after invoking
            // the execute method.
            this._generatedFiles.Clear();
            this._copiedFiles.Clear();
            this._sharedFilesByProject = null;
            this._linkedFilesNormalized = null;
            this._serverProjectDirectory = null;
            this._clientAssemblyPathsNormalized = null;
            this._clientReferenceAssembliesNormalized = null;
            this._serverOutputPath = null;

            try
            {
                System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
                stopWatch.Start();

                // Log a startup message describing the project for which we are generating code
                this.LogMessage(string.Format(CultureInfo.CurrentCulture, Resource.CodeGen_Generating_Proxies, Path.GetFileName(this.ClientProjectPath)));

                this.DeleteCodeGenMetafilesIfInvalid();

                // Copy shared files and generate proxies -- but only if the server project exists.
                // We do the copy first so that we know which files are shared between projects
                if (this.IsServerProjectAvailable)
                {
                    this.CopySharedFiles();
                    this.GenerateClientProxies();
                }

                // Remove any files which we generated on a prior run but did not generate now
                this.PurgeOrphanFiles();

                // Prepare a file list containing our list of generated files -- used in clean and subsequent build.
                // This list is computed every time, even for no-change builds, so we always write it.
                this.WriteFileList();

                // Write out our cache of source files in other projects.
                if (!this.ServerProjectSourceFileCache.IsFileCacheCurrent &&
                    RiaClientFilesTaskHelpers.SafeFolderCreate(Path.GetDirectoryName(this.SourceFileListPath()), this))
                {
                    this.ServerProjectSourceFileCache.SaveCacheToFile();
                }

                // Write out our cache of Open RIA Services Links
                if (!this.LinkedServerProjectCache.IsFileCacheCurrent &&
                    RiaClientFilesTaskHelpers.SafeFolderCreate(Path.GetDirectoryName(this.LinkedServerProjectsPath()), this))
                {
                    this.LinkedServerProjectCache.SaveCacheToFile();
                }

                double secondsAsDouble = stopWatch.ElapsedMilliseconds / 1000.0;
                string executionTimeMessage = string.Format(CultureInfo.CurrentCulture, Resource.CodeGen_Execution_Time, secondsAsDouble);
                this.LogMessage(executionTimeMessage);
            }
            finally
            {
                // Discard our resources.  This will explicitly dispose
                // any IDisposable objects.
                this.ReleaseResources();
            }

            // Any error sent to the log constitutes failure
            return !this.HasLoggedErrors;
        }

        /// <summary>
        /// Checks if the project was moved from a different location, in which case it purges the breadcrumb files generated in the previous build.
        /// </summary>
        private void DeleteCodeGenMetafilesIfInvalid()
        {
            string fileListFile = this.FileListPath();

            // This runs only if we have a file list from a prior run
            if (!String.IsNullOrEmpty(fileListFile) && File.Exists(fileListFile))
            {
                bool isNewBuildLocation = false;

                // read the first line of the file which contains the project folder path
                using (StreamReader reader = new StreamReader(fileListFile))
                {
                    string projectPath;
                    if ((projectPath = reader.ReadLine()) != null)
                    {
                        // If the project path is different from the current client project path, then it means the project was copied from a different location. 
                        // So purge the breadcrumb files from the previous build in the previous location.
                        if (string.IsNullOrEmpty(projectPath) || !string.Equals(projectPath, this.ClientProjectDirectory, StringComparison.OrdinalIgnoreCase))
                        {
                            isNewBuildLocation = true;
                        }
                    }
                }

                if (isNewBuildLocation)
                {
                    this.DeleteCodeGenMetafileLists();
                }
            }
        }

        /// <summary>
        /// Generates the client proxies.
        /// </summary>
        /// <remarks>
        /// This method validates the presence of the necessary input server assemblies and
        /// then invokes the code generation logic to create a file containing the client
        /// proxies for the discovered server's Business Objects.  If the file already exists
        /// and is newer than the inputs, this method does nothing.
        /// <para>In all success paths, the client proxy file will be added to the list of generated
        /// files so the custom targets file can add them to the @Compile collection.</para>
        /// </remarks>
        internal void GenerateClientProxies()
        {
            IEnumerable<string> assemblies = this.GetServerAssemblies();
            IEnumerable<string> references = this.GetReferenceAssemblies();

            // It is a failure if any of the reference assemblies are missing
            if (!this.EnsureAssembliesExist(references))
            {
                return;
            }

            // Obtain the name of the output assembly from the server project
            // (it is currently a collection to be consistent with MSBuild item collections).
            // If there is no output assembly, log a warning.
            // We consider this non-fatal because an Intellisense build can trivially
            // encounter this immediately after creating a new Open Ria Services application
            string assemblyFile = assemblies.FirstOrDefault();
            if (string.IsNullOrEmpty(assemblyFile) || !File.Exists(assemblyFile))
            {
                string serverProjectFile = Path.GetFileName(this.ServerProjectPath);
                this.LogWarning(string.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_No_Input_Assemblies, serverProjectFile));
                return;
            }

            // Make it an absolute path and append the language-specific extension
            string generatedFileName = Path.Combine(this.GeneratedCodePath, this.GenerateProxyFileName(assemblyFile));

            // We will load all input and reference assemblies
            var assembliesToLoad = new HashSet<string>(assemblies.Concat(references), StringComparer.OrdinalIgnoreCase);

            // We maintain cached lists of references we used in prior builds.
            // Determine whether our current inputs are different from the last build that generated code.
            bool serverReferencesChanged = this.HaveReferencesChanged(this.ServerReferenceListPath(), assembliesToLoad, this.ServerProjectDirectory);
            bool clientReferencesChanged = this.HaveReferencesChanged(this.ClientReferenceListPath(), this.ClientReferenceAssembliesNormalized, this.ClientProjectDirectory);

            // Any change in the assembly references for either client or server are grounds to re-gen code.
            // Developer note -- we use the fact that the inputs have changed to trigger the full code-gen pass.
            bool needToGenerate = serverReferencesChanged || clientReferencesChanged;

            // Also trigger code-gen if the generated file is absent or empty.
            // Technically, the reference-change test is enough, but experience has shown users
            // manually delete the GeneratedCode folder and expect the next build to recreate it.
            // Therefore, its absence always triggers a code-gen pass, even though this has the
            // negative perf impact of causing a full code gen pass everytime until errors have been
            // resolved.

            if (!needToGenerate)
            {
                FileInfo fileInfo = new FileInfo(generatedFileName);
                bool fileExists = fileInfo.Exists;
                needToGenerate = (!fileExists || (fileInfo.Length == 0));

                // If we determine the generated
                // file has been touched since we last analyzed our server references, it is an indication
                // the user modified the generated file.  So force a code gen and
                // force a rewrite of the server reference file to short circuit this same code next build.
                if (!needToGenerate && File.Exists(this.ServerReferenceListPath()))
                {
                    if (File.GetLastWriteTime(generatedFileName) > File.GetLastWriteTime(this.ServerReferenceListPath()))
                    {
                        needToGenerate = true;
                        serverReferencesChanged = true;
                    }
                }
            }

            // If we need to generate the file, do that now
            if (needToGenerate)
            {
                // Warn the user if the server assembly has no PDB
                this.WarnIfNoPdb(assemblyFile);

                string generatedFileContent = string.Empty;
                string sourceDir = this.ServerProjectDirectory;

                // Capture the list of assemblies to load into an array to marshal across AppDomains
                string[] assembliesToLoadArray = assembliesToLoad.ToArray();

                // Create the list of options we will pass to the generator.
                // This instance is serializable and can cross AppDomains
                ClientCodeGenerationOptions options = new ClientCodeGenerationOptions()
                {
                    Language = this.Language,
                    ClientFrameworkPath = this.ClientFrameworkPath,
                    ClientRootNamespace = this.ClientProjectRootNamespace,
                    ServerRootNamespace = this.ServerProjectRootNameSpace,
                    ClientProjectPath = this.ClientProjectPath,
                    ServerProjectPath = this.ServerProjectPath,
                    IsApplicationContextGenerationEnabled = this.IsClientApplicationAsBool,
                    UseFullTypeNames = this.UseFullTypeNamesAsBool,
                    ClientProjectTargetPlatform = this.ClientTargetPlatform,
                };

                // Compose the parameters we will pass to the other AppDomain to create the SharedCodeService
                SharedCodeServiceParameters sharedCodeServiceParameters = this.CreateSharedCodeServiceParameters(assembliesToLoadArray);

                if (IsServerProjectNetFramework())
                {
#if NETFRAMEWORK
                    FilesWereWritten = GenerateClientProxiesForNetFramework(generatedFileName, sourceDir, options, sharedCodeServiceParameters);
#else
                    // TODO: Verify below statement, I exepct that it might not work (and does not need to work)
                    // Probably we need a "hosting process" for net framework in this case
                    FilesWereWritten = RiaClientFilesTaskHelpers.CodeGenForNet6(
                        generatedFileName,
                        options,
                        this,
                        sharedCodeServiceParameters,
                        this.CodeGeneratorName
                    );
#endif
                }
                else
                {
                    // PERF: If running with a compatible target framework (!NETFRAMEWORK) 
                    // we should be able to call into the code generation directly without invoking the executable
                    FilesWereWritten = GenerateClientProxiesOutOfProcess(generatedFileName, options, sharedCodeServiceParameters);
                }
            }
            else
            {
                // Log a message telling user we are skipping code gen because the inputs are older than the generated code
                this.LogMessage(string.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_Skipping_CodeGen, generatedFileName));
            }

            // We unconditionally declare the file was generated if it exists
            // on disk after this method finishes, even if it was not modified.
            // This permits the targets file to add it to the @COMPILE collection.
            // This also prevents adding it to the list if it was deleted above
            if (File.Exists(generatedFileName))
            {
                this.AddGeneratedFile(generatedFileName);
            }

            // Write out reference lists if they have changed
            if (serverReferencesChanged)
            {
                this.WriteReferenceList(this.ServerReferenceListPath(), assembliesToLoad, this.ServerProjectDirectory);
            }

            if (clientReferencesChanged)
            {
                this.WriteReferenceList(this.ClientReferenceListPath(), this.ClientReferenceAssembliesNormalized, this.ClientProjectDirectory);
            }

            return;
        }

        /// <summary>
        /// Run code generation in a separate .exe in order to support .NET (net core) code generation from NETFRAMEWORK version of msbuild
        /// </summary>
        /// <remarks>
        ///  We might want to add a "targetframework" parameter to 
        ///  1. allow running code generation under a specific target framework (instead of "lastest major")
        ///  2. allow launching NETFRAMEWORK version of code generation from dotnet build
        /// </remarks>
        private bool GenerateClientProxiesOutOfProcess(string generatedFileName, ClientCodeGenerationOptions options, SharedCodeServiceParameters sharedCodeServiceParameters)
        {
            // Call the console app from here if Net 6.0 or greater
            string path = Path.Combine(Path.GetDirectoryName(typeof(CreateOpenRiaClientFilesTask).Assembly.Location),
                "../net6.0/OpenRiaServices.Tools.CodeGenTask.exe");

            if (!File.Exists(path))
                throw new FileNotFoundException(path);

            using var loggingServer = new CrossProcessLoggingServer();
            var startInfo = new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            List<string> arguments = new List<string>();
            SetArgumentListForConsoleApp(arguments, generatedFileName, options, sharedCodeServiceParameters, loggingServer.PipeName);

            // TODO: Fix vulnerability with GetTempFileName, see https://sonarcloud.io/project/issues?resolved=false&severities=BLOCKER%2CCRITICAL%2CMAJOR%2CMINOR&sinceLeakPeriod=true&types=VULNERABILITY&pullRequest=414&id=OpenRIAServices_OpenRiaServices&open=AYi1D8MZVJzuBbc9Xd8Q&tab=why
            // and add error handling 
            string filename = Path.GetTempFileName();
            File.WriteAllLines(filename, arguments);
            startInfo.Arguments = "@" + filename;

            var process = Process.Start(startInfo);

            // Informs the system that this task has a long-running out-of - process component
            //     and other work can be done in the build while that work completes.
            BuildEngine3.Yield();
            try
            {
                // Read all logs from child process
                TimeSpan timeout = TimeSpan.FromSeconds(600);
#if DEBUG
                // Increase timeout when debugger is attached
                if (Debugger.IsAttached)
                    timeout = TimeSpan.FromSeconds(1200);
#endif

                // Consider doing async IO or similar in background and specify a timeout
                using (CancellationTokenSource cts = new CancellationTokenSource(timeout))
                {
                    // Kill process if CancellationTokenSource elapses before we go out of scope
                    using var _ = cts.Token.Register(obj => ((Process)obj).Kill(), process);

                    // Read logs from pipe and forward to this, it will no complete until the client has closed the pipe
                    // in case of any serious error where client fails to close the pipe, we rely on the CancellationTokenSource to timeout
                    loggingServer.WriteLogsTo(this, cts.Token);
                }

                // Even without a timeout this should complete quickly
                // 1. Either the process has either closed the pipe and is shutting down and should exit within a few ms
                // 2. or the timeout has elapsed and we have called Kill on the process so it should be stopped or stopping
                process.WaitForExit();
                var success = process.ExitCode == 0;
                if (!success)
                {
                    Log.LogError("Process failed with ExitCode: {0}", process.ExitCode);
                }
                else
                {
                    RiaClientFilesTaskHelpers.SafeFileDelete(filename, this);
                }
                return success;
            }
            finally
            {
                BuildEngine3.Reacquire();
            }
        }

#if NETFRAMEWORK
        /// <summary>
        /// Use <see cref="System.Web.Compilation"/> to build and load the server project assemblies
        /// </summary>
        /// <remarks>
        /// Prevent inlining so that we only reference and load System.Web when needed for compilation
        /// </remarks>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool GenerateClientProxiesForNetFramework(string generatedFileName, string sourceDir, ClientCodeGenerationOptions options, SharedCodeServiceParameters sharedCodeServiceParameters)
        {
            string generatedFileContent;
            // The other AppDomain gets a logger that will log back to this AppDomain
            CrossAppDomainLogger logger = new CrossAppDomainLogger((ILoggingService)this);

            // Surface a HttpRuntime initialization error that would otherwise manifest as a NullReferenceException
            // This can occur when the build environment is configured incorrectly
            if (System.Web.Hosting.HostingEnvironment.InitializationException != null)
            {
                throw new InvalidOperationException(
                    Resource.HttpRuntimeInitializationError,
                    System.Web.Hosting.HostingEnvironment.InitializationException);
            }

            // We override the default parameter to ask for ForceDebug, otherwise the PDB is not copied.
            System.Web.Compilation.ClientBuildManagerParameter cbmParameter = new System.Web.Compilation.ClientBuildManagerParameter()
            {
                PrecompilationFlags = System.Web.Compilation.PrecompilationFlags.ForceDebug,
            };
            using (System.Web.Compilation.ClientBuildManager cbm = new System.Web.Compilation.ClientBuildManager(/* appVDir */ "/", sourceDir, null, cbmParameter))
            using (ClientCodeGenerationDispatcher dispatcher = (ClientCodeGenerationDispatcher)cbm.CreateObject(typeof(ClientCodeGenerationDispatcher), false))
            {
                // Transfer control to the dispatcher in the 2nd AppDomain to locate and invoke
                // the appropriate code generator.
                generatedFileContent = dispatcher.GenerateCode(options, sharedCodeServiceParameters, logger, this.CodeGeneratorName);
            }

            // Tell the user where we are writing the generated code
            if (!string.IsNullOrEmpty(generatedFileContent))
            {
                logger.LogMessage(string.Format(CultureInfo.CurrentCulture, Resource.Writing_Generated_Code, generatedFileName));
            }

            // If VS is hosting us, write to its TextBuffer, else simply write to disk
            // If the file is empty, delete it.
            return RiaClientFilesTaskHelpers.WriteOrDeleteFileToVS(generatedFileName, generatedFileContent, /*forceWriteToFile*/ false, logger);
        }
#endif

        private bool IsServerProjectNetFramework()
        {
            if (ServerAssemblies.FirstOrDefault() is ITaskItem serverAssembly)
            {
                var targetIdentifier = serverAssembly.GetMetadata("TargetFrameworkIdentifier");
                if (!string.IsNullOrEmpty(targetIdentifier))
                {
                    bool isFramework = targetIdentifier == ".NETFramework";
                    Log.LogMessage("Is server project .NETFramework based on TargetFrameworkIdentifier: {0}", isFramework.ToString());

                    return isFramework;
                }

                return IsAssemblyNetFramework(serverAssembly.ItemSpec);
            }

            return true;
        }

        /// <summary>
        /// Fallback for determining target framework based on assembly.
        /// It is "only" required for tests since in runtime we can look at TaskItem metadata to determine target framework
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)] // Avoid having to load mono.cecil if not called
        private bool IsAssemblyNetFramework(string assemblyPath)
        {
            // An other solution would also be to look at the assemblies referenced
            // and se if there are any paths which contains '.NETFramework' or '\net4*\'
            var assembly = Mono.Cecil.AssemblyDefinition.ReadAssembly(assemblyPath);
            var targetFrameworkAttribute = assembly.CustomAttributes.FirstOrDefault(a => a.AttributeType.FullName == typeof(TargetFrameworkAttribute).FullName);

            if (targetFrameworkAttribute != null
                && targetFrameworkAttribute.HasConstructorArguments)
            {
                bool isFramework = targetFrameworkAttribute.ConstructorArguments[0].Value.ToString().StartsWith(".NETFramework");
                Log.LogMessage("Is server project .NETFramework based on TargetFrameworkAttribute: {0}", isFramework.ToString());
                return isFramework;
            }

#if NETFRAMEWORK
            return true;
#else
            return false;
#endif
        }

        /// <summary>
        /// Returns the computed list of shared and linked files between the server and client projects.
        /// </summary>
        /// <remarks>This list includes all shared files named via the "*.shared.* pattern as well
        /// as files found via file links.
        /// </remarks>
        /// <returns>The set of absolute file names that are the files both client and server see</returns>
        internal IEnumerable<string> GetSharedAndLinkedFiles()
        {
            // Get *all* files from the server and all its project references.
            // This is the raw list and is not constrained only to shared files or file links.
            IEnumerable<string> serverFiles = this.ServerProjectSourceFileCache.GetSourceFilesInAllProjects();

            // All *.shared.* files are automatically added to our set of known shared files
            IList<string> sharedFiles = new List<string>(serverFiles.Where(f => IsFileShared(f)));

            // Now get the set of linked files we find in common between the client and server
            IEnumerable<string> linkedFiles = this.LinkedFilesNormalized;

            // Return both collections as one
            return sharedFiles.Concat(linkedFiles);
        }

        /// <summary>
        /// Simple evaluator to determine whether a file matches the "*.shared.*" pattern
        /// </summary>
        /// <param name="fileName">The full or short name of the file to test.</param>
        /// <returns><c>true</c> means it matches the pattern for a shared file</returns>
        private static bool IsFileShared(string fileName)
        {
            return Path.GetFileNameWithoutExtension(fileName).EndsWith(".shared", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Computes the full path of the destination file name.
        /// </summary>
        /// <remarks>This method is responsible for preserving any folder hierarchies
        /// in the event the source file is not directly in the root.</remarks>
        /// <param name="sourceFilePath">The full path to the source file</param>
        /// <param name="sourceDirectory">The root directory of the server</param>
        /// <param name="destinationDirectory">The root directory of the destination</param>
        /// <returns>A string representing the full destination file path.</returns>
        private static string ComposeDestinationPath(string sourceFilePath, DirectoryInfo sourceDirectory, DirectoryInfo destinationDirectory)
        {
            DirectoryInfo sourceFileDirectory = new DirectoryInfo(Path.GetDirectoryName(sourceFilePath));
            IEnumerable<string> folders = GetSubfolders(sourceDirectory, sourceFileDirectory);
            string destinationPath = destinationDirectory.FullName;
            foreach (string folder in folders)
            {
                destinationPath = Path.Combine(destinationPath, folder);
            }
            return Path.Combine(destinationPath, Path.GetFileName(sourceFilePath));
        }

        /// <summary>
        /// Returns the ordered set of folder names the given <paramref name="sourceFileDirectory"/>
        /// lies under the given <paramref name="sourceDirectory"/>
        /// </summary>
        /// <remarks>In other words, this method returns the set of folder names that must be
        /// created under the eventual destination folder to preserve the folder hiearchy of
        /// the source.  An empty list means <paramref name="sourceFileDirectory"/> is not
        /// located under <paramref name="sourceDirectory"/> or is the same folder.
        /// </remarks>
        /// <param name="sourceDirectory">The root directory of the server project.</param>
        /// <param name="sourceFileDirectory">The directory in which a file-to-be-copied resides</param>
        /// <returns>The ordered set of folder names.</returns>
        private static IList<string> GetSubfolders(DirectoryInfo sourceDirectory, DirectoryInfo sourceFileDirectory)
        {
            List<string> folders = new List<string>();
            while (!sourceFileDirectory.FullName.Equals(sourceDirectory.FullName))
            {
                folders.Insert(0, sourceFileDirectory.Name);
                sourceFileDirectory = sourceFileDirectory.Parent;
                if (sourceFileDirectory == null)
                {
                    return new List<string>();
                }
            }
            return folders;
        }

        /// <summary>
        /// Read the list of reference assemblies we last code-genned against
        /// into a dictionary, keyed by file name and with the timestamp as a string
        /// </summary>
        /// <param name="fileName">The file name from which to read the references.</param>
        /// <param name="projectDir">The directory containing the project.</param>
        /// <returns>dictionary containing last set of known references</returns>
        private Dictionary<string, string> ReadReferenceList(string fileName, string projectDir)
        {
            Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (File.Exists(fileName))
            {
                string fileContents = File.ReadAllText(fileName);
                string[] strings = fileContents.Split(',', ';');
                for (int i = 0; i < strings.Length; ++i)
                {
                    if (string.IsNullOrEmpty(strings[i]))
                    {
                        break;
                    }
                    string refFileName = this.GetFullPathRelativeToDirectory(strings[i], projectDir);
                    string dateTimeAsString = strings[++i];
                    result[refFileName] = dateTimeAsString;
                }
            }
            return result;
        }

        /// <summary>
        /// Determines whether the given set of references are different from the set we
        /// last used that were saved to the given file.
        /// </summary>
        /// <param name="fileName">The file in which we last wrote the references.</param>
        /// <param name="references">The set of references we have currently.</param>
        /// <param name="projectDir">The directory containing the project.</param>
        /// <returns><c>true</c> means the current references are different from the set we last used for code generation.</returns>
        private bool HaveReferencesChanged(string fileName, ICollection<string> references, string projectDir)
        {
            // First time or after project clean always says "yes"
            if (!File.Exists(fileName))
            {
                return true;
            }

            // Read in the cache of reference assembly names and their timestamps
            Dictionary<string, string> priorRefs = this.ReadReferenceList(fileName, projectDir);

            // Look for any references that got dropped.  Grounds for saying "it changed"
            foreach (string reference in priorRefs.Keys)
            {
                if (!references.Contains(reference))
                {
                    return true;
                }
            }

            // Look for any references we didn't see before -- or a change in the timestamp
            foreach (string refFileName in references)
            {
                // Non-existant reference counts as a change
                if (!File.Exists(refFileName))
                {
                    return true;
                }

                string dateTimeAsString = null;
                if (!priorRefs.TryGetValue(refFileName, out dateTimeAsString))
                {
                    return true;
                }

                string dateTimeNow = string.Format(CultureInfo.InvariantCulture, "{0}", File.GetLastWriteTime(refFileName));
                if (!dateTimeNow.Equals(dateTimeAsString, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Releases all resources acquired by this instance.
        /// </summary>
        /// <remarks>
        /// This method returns all fields to their original uninitialized
        /// state and disposes any disposable objects that were lazily created.
        /// It may be called multiple times and will always leave the current
        /// instance in a valid state for reuse.  Note that this method does
        /// not affect any fields that are required for output items returned
        /// by this custom task, because they will be consumed by MSBuild after
        /// the task has executed.
        /// </remarks>
        private void ReleaseResources()
        {
            this._linkedServerProjectCache = null;
            this._serverProjectSourceFileCache = null;

            // The ProjectFileReader is IDisposable and requires
            // explicit disposal.  Because it is created lazily
            // there is no scope for a traditional 'using' block.
            if (this._projectFileReader != null)
            {
                this._projectFileReader.Dispose();
                this._projectFileReader = null;
            }
        }

        /// <summary>
        /// Issue a build warning if we cannot find a PDB file for the given assembly
        /// </summary>
        /// <param name="assemblyFile">The name of the assembly file whose PDB file we need.</param>
        private void WarnIfNoPdb(string assemblyFile)
        {
            if (File.Exists(assemblyFile))
            {
                string pdbFile = Path.ChangeExtension(assemblyFile, "pdb");
                if (!File.Exists(pdbFile))
                {
                    this.LogWarning(string.Format(CultureInfo.CurrentCulture, Resource.CodeGen_No_Pdb, assemblyFile));
                }
            }
        }

        /// <summary>
        /// This method deletes any files that we generated on a previous execution,
        /// but which we did not generate this time.  This is how renamed and deleted server files
        /// files get removed from the client project without the user needing to do an explicit clean.
        /// </summary>
        private void PurgeOrphanFiles()
        {
            // Get the list of files we wrote in a prior build.
            IEnumerable<string> files = this.FilesPreviouslyWritten();

            // Compute the list of all files we create, which includes the one
            // we generate and all the *.shared.* files we copied
            IEnumerable<ITaskItem> outputFiles = this.OutputFiles;

            // Now, scan the list and determine which ones went away
            foreach (string fileName in files)
            {
                // If this file exists on disk...
                if (File.Exists(fileName))
                {
                    // Scan our list of newly generated file to see if we still have it
                    bool generatedFile = outputFiles.Any(i => string.Equals(fileName, i.ItemSpec, StringComparison.OrdinalIgnoreCase));

                    // If we did not generate this file on this run, delete it
                    if (!generatedFile)
                    {
                        this.LogMessage(string.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_Deleting_Orphan, fileName));
                        RiaClientFilesTaskHelpers.DeleteFileFromVS(fileName, this);
                        this.DeleteFolderIfEmpty(Path.GetDirectoryName(fileName));
                    }
                }
                else
                {
                    // If the file was deleted outside of our control, we still take the opportunity
                    // to remove a residual empty folder
                    this.DeleteFolderIfEmpty(Path.GetDirectoryName(fileName));
                }
            }
        }

        /// <summary>
        /// Creates a file listing all the generated files.  Will be used for Clean operation.
        /// </summary>
        private void WriteFileList()
        {
            StringBuilder sb = new StringBuilder();
            StringBuilder logSb = new StringBuilder();
            //First line is the client project directory
            sb.AppendLine(this.ClientProjectDirectory);
            foreach (ITaskItem item in this.OutputFiles)
            {
                string relativeFilePath = RiaClientFilesTask.GetPathRelativeToProjectDirectory(item.ItemSpec, this.ClientProjectDirectory);
                sb.AppendLine(relativeFilePath);
                logSb.AppendLine();
                logSb.Append("    " + item.ItemSpec);
            }
            string outputFileLines = sb.ToString();
            if (outputFileLines.Length > 0)
            {
                this.LogMessage(string.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_OutputFiles_Are, logSb.ToString()));
            }

            string fileListFile = this.FileListPath();
            if (this.FilesWereWritten && !string.IsNullOrEmpty(fileListFile) &&
                RiaClientFilesTaskHelpers.SafeFolderCreate(Path.GetDirectoryName(fileListFile), this))
            {
                RiaClientFilesTaskHelpers.SafeFileWrite(fileListFile, outputFileLines, this);
            }
        }

        /// <summary>
        /// Writes out all the assembly references and their timestamps
        /// </summary>
        /// <param name="fileName">The file name into which to write.</param>
        /// <param name="references">The list of references to write</param>
        /// <param name="projectDir">The directory containing the project</param>
        private void WriteReferenceList(string fileName, IEnumerable<string> references, string projectDir)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string refFileName in references)
            {
                if (File.Exists(refFileName))
                {
                    string refFilePath = RiaClientFilesTask.GetPathRelativeToProjectDirectory(refFileName, projectDir);
                    sb.Append(refFilePath + "," + string.Format(CultureInfo.InvariantCulture, "{0}", File.GetLastWriteTime(refFileName)) + ";");
                }
            }
            string outputFileLines = sb.ToString();

            if (!string.IsNullOrEmpty(fileName) &&
                RiaClientFilesTaskHelpers.SafeFolderCreate(Path.GetDirectoryName(fileName), this))
            {
                RiaClientFilesTaskHelpers.SafeFileWrite(fileName, outputFileLines, this);
            }
        }

        /// <summary>
        /// Constructs the parameters to pass across the AppDomain boundary to construct a
        /// <see cref="SharedCodeService"/> in the AppDomain that will do code generation.
        /// </summary>
        /// <param name="serverAssemblies">The set of full paths to the server assemblies to use for code generation.</param>
        /// <returns>A <see cref="SharedCodeServiceParameters"/> instance ready to use to construct a <see cref="SharedCodeService"/>.</returns>
        private SharedCodeServiceParameters CreateSharedCodeServiceParameters(IEnumerable<string> serverAssemblies)
        {
            SharedCodeServiceParameters parameters = new SharedCodeServiceParameters();

            parameters.ServerAssemblies = serverAssemblies.ToArray();

            // Get all *.shared.* and linked files in common between client and server.
            // These are consider "shared files" by the shared type service
            parameters.SharedSourceFiles = this.GetSharedAndLinkedFiles().ToArray();

            // Present the list of shared files to the user as a informational level log
            if (parameters.SharedSourceFiles.Any())
            {
                StringBuilder sb = new StringBuilder();
                foreach (string file in parameters.SharedSourceFiles)
                {
                    sb.AppendLine();
                    sb.Append("    " + file);
                }
                this.LogMessage(string.Format(CultureInfo.CurrentCulture, Resource.CodeGen_Shared_Files, sb.ToString()));
            }

            parameters.ClientAssemblies = this.NormalizedTaskItems(this.ClientReferenceAssemblies, this.ClientProjectDirectory).ToArray();
            parameters.ClientAssemblyPathsNormalized = this.ClientAssemblyPathsNormalized.ToArray();

            // Convert the list of server assemblies into a search path for PDB's
            parameters.SymbolSearchPaths = serverAssemblies.Select(a => Path.GetDirectoryName(a)).Distinct().ToArray();

            return parameters;
        }

        /// <summary>
        /// Adds the given file name to the list returned by <see cref="GeneratedFiles"/>.
        /// </summary>
        /// <param name="fileName">The absolute path of a file.</param>
        private void AddGeneratedFile(string fileName)
        {
            this._generatedFiles.Add(new TaskItem(fileName));
        }

        /// <summary>
        /// Adds the given file name to the list returned by <see cref="CopiedFiles"/>.
        /// </summary>
        /// <param name="fileName">The absolute path of a file.</param>
        private void AddCopiedFile(string fileName)
        {
            this._copiedFiles.Add(new TaskItem(fileName));
        }

        /// <summary>
        /// Computes the set of all shared files for all projects.
        /// </summary>
        /// <returns>A dictionary, keyed by project path and containing the list of shared files from that project.</returns>
        private Dictionary<string, IList<string>> GetSharedFilesByProject()
        {
            Dictionary<string, IList<string>> sharedFiles = new Dictionary<string, IList<string>>();

            // To prevent copying a file into the client that has already been copied into one of our
            // referenced class libraries (due to a RIA link from within that class library), we collect
            // the list of known "RIA Links" visible to all projects referenced by the client project.
            // We will not copy any files from a project visible though such a RIA Link, else we would
            // have 2 copies of the file in scope for the client.
            HashSet<string> linkedServerProjects = new HashSet<string>(this.LinkedServerProjectCache.LinkedServerProjects, StringComparer.OrdinalIgnoreCase);

            // Each separate project's set of files is copied under a unique folder under GeneratedCode.
            // This organizes them for the user and also avoids overwrite conflicts for duplicately named files
            foreach (string projectPath in this.ServerProjectSourceFileCache.GetAllKnownProjects())
            {
                bool checkedRiaLink = false;
                bool skipThisProject = false;

                IEnumerable<string> filesToCopy = this.ServerProjectSourceFileCache.GetSourceFilesInProject(projectPath);
                foreach (string file in filesToCopy)
                {
                    if (!skipThisProject && IsFileShared(file) && File.Exists(file))
                    {
                        // We defer checking for redundant RIA Links until we know we have a shared file
                        // that needs to be copied.  This cuts down on the message chatter.
                        if (!checkedRiaLink)
                        {
                            checkedRiaLink = true;

                            // If some project visible to the client has a RIA Link to this project,
                            // do *NOT* copy its files, or we will have multiple copies
                            if (linkedServerProjects.Contains(projectPath))
                            {
                                // Get the name of the first client project with a RIA Link to that project.
                                // More might exist, but for the purpose of this message to the user, the first is sufficient.
                                string sourceProject = this.LinkedServerProjectCache.GetLinkedServerProjectSources(projectPath).FirstOrDefault();
                                this.LogMessage(string.Format(CultureInfo.CurrentCulture, Resource.RIA_Link_Prevents_Copy, projectPath, sourceProject));
                                skipThisProject = true;
                                continue;
                            }
                        }

                        IList<string> files = null;
                        if (!sharedFiles.TryGetValue(projectPath, out files))
                        {
                            sharedFiles[projectPath] = files = new List<string>();
                        }
                        files.Add(file);
                    }
                }
            }
            return sharedFiles;
        }

        /// <summary>
        /// Copy the specified files to the output directory
        /// </summary>
        /// <remarks>
        /// This subtask is done here (rather than reusing the MSBuild Copy task directly) so that it
        /// can interact with the Visual Studio TextBuffers.
        /// </remarks>
        private void CopySharedFiles()
        {
            Dictionary<string, IList<string>> sharedFilesByProject = this.SharedFilesByProject;

            // SharedFilesByProject initializes important information about service links
            // Dont exit until after initialization
            if (LinkSharedFilesInsteadOfCopy)
                return;

            HashSet<string> fileHash = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Each separate project's set of files is copied under a unique folder under GeneratedCode.
            // This organizes them for the user and also avoids overwrite conflicts for duplicately named files
            foreach (string projectPath in sharedFilesByProject.Keys)
            {
                string projectDirectory = Path.GetDirectoryName(projectPath);
                DirectoryInfo sourceDirectory = new DirectoryInfo(projectDirectory);
                string projectShortName = Path.GetFileNameWithoutExtension(projectPath);
                bool isServerProject = String.Equals(projectDirectory, this.ServerProjectDirectory, StringComparison.OrdinalIgnoreCase);

                IEnumerable<string> filesToCopy = sharedFilesByProject[projectPath];
                foreach (string file in filesToCopy)
                {
                    if (!fileHash.Contains(file))
                    {
                        // Defer creation of generated code folder until we know we must do work.
                        string generatedCodeFolder = this.GeneratedCodePath;
                        string destinationPath = isServerProject ? generatedCodeFolder : Path.Combine(generatedCodeFolder, projectShortName);
                        DirectoryInfo destinationDirectory = new DirectoryInfo(destinationPath);

                        this.CopyFile(file, sourceDirectory, destinationDirectory);
                        fileHash.Add(file);
                    }
                }
            }
        }

        /// <summary>
        /// Copies the specified file from the server to the client project
        /// </summary>
        /// <param name="sourceFilePath">Full path to the source file</param>
        /// <param name="sourceDirectory">The root directory of the server</param>
        /// <param name="destinationDirectory">The root directory of where generated code should go in the client project</param>
        /// <returns>A <see cref="Boolean"/> indicating whether a file was actually copied.  <c>false</c> means an error occurred or the
        /// file is already current on disk.</returns>
        private bool CopyFile(string sourceFilePath, DirectoryInfo sourceDirectory, DirectoryInfo destinationDirectory)
        {
            bool copiedFile = false;
            string destinationFilePath = ComposeDestinationPath(sourceFilePath, sourceDirectory, destinationDirectory);
            string destinationFolder = Path.GetDirectoryName(destinationFilePath);

            // Create the destination folder as late as possible
            if (!RiaClientFilesTaskHelpers.SafeFolderCreate(destinationFolder, this))
            {
                return false;
            }

            // Keep track of all files that are logically copied, even if we find it is current
            this.AddCopiedFile(destinationFilePath);
            // Don't do any work unless the inputs are newer.
            // Note: we are sensitive to a VS TextBuffer being dirty as being newer
            if (this.IsFileWriteTimeDifferent(sourceFilePath, destinationFilePath))
            {
                this.LogMessage(string.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_Copying_File, sourceFilePath, destinationDirectory.FullName));

                // Use the safe form of the copy to guarantee the folder exists and the readonly attribute is reset.
                // Note: we use a direct file-to-file copy here for 2 reasons:
                //  1. We want the file write time preserved for later comparison
                //  2. We want to copy only files on disk, not unsaved edits in VS text buffers, otherwise
                //     the user could cancel the edit and leave copied files that don't reflect what's on disk.
                this.SafeFileCopy(sourceFilePath, destinationFilePath, true);

                copiedFile = true;
            }
            else
            {
                // Log a message telling user we are skipping the copy because the inputs are older than the generated code
                this.LogMessage(string.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_Skipping_Copy, destinationFilePath));
            }
            return copiedFile;
        }

        /// <summary>
        /// Returns the list of absolute filenames of the input assemblies
        /// </summary>
        /// <returns>list of filenames of input assemblies</returns>
        private IEnumerable<string> GetServerAssemblies()
        {
            List<string> assemblies = new List<string>();
            foreach (ITaskItem item in this.ServerAssemblies)
            {
                string assemblyName = item.ItemSpec;
                if (!Path.IsPathRooted(assemblyName))
                {
                    assemblyName = Path.GetFullPath(Path.Combine(this.ClientProjectDirectory, assemblyName));
                }

                // If server project target AspNetCore then it might be an exe (the native dotnet host) which we cannot do reflection on
                // Change to .dll so that we target the actual managed implementation
                if (assemblyName.EndsWith(".exe"))
                    assemblyName = Path.ChangeExtension(assemblyName, ".dll");

                assemblies.Add(assemblyName);
            }
            return assemblies;
        }

        /// <summary>
        /// Returns the collection of the full paths to the input reference assemblies.
        /// </summary>
        /// <returns>The list of reference assembly file names</returns>
        private IEnumerable<string> GetReferenceAssemblies()
        {
            List<string> assemblies = new List<string>();
            if (this.ServerReferenceAssemblies != null)
            {
                foreach (ITaskItem item in this.ServerReferenceAssemblies)
                {
                    string assemblyName = item.ItemSpec;
                    if (!Path.IsPathRooted(assemblyName))
                    {
                        assemblyName = Path.GetFullPath(Path.Combine(this.ClientProjectDirectory, assemblyName));
                    }
                    assemblies.Add(assemblyName);
                }
            }
            return assemblies;
        }

        /// <summary>
        /// Validates existance of necessary files.  Returns false if any are missing.  Also logs an error message.
        /// This method explicitly does not check for an empty list, only that the list contains only valid items.
        /// </summary>
        /// <param name="assemblies">The set of assembly names to test</param>
        /// <returns><c>true</c> if all the assemblies exist.</returns>
        private bool EnsureAssembliesExist(IEnumerable<string> assemblies)
        {
            foreach (string assembly in assemblies)
            {
                if (!File.Exists(assembly))
                {
                    this.LogWarning(string.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_Input_Assembly_Not_Found, assembly));
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Create the name of the filename where the proxy code will be written
        /// </summary>
        /// <param name="serverAssemblyName">The name of the server assembly, to be used in forming the name</param>
        /// <returns>The base name.  It will not include a path.</returns>
        private string GenerateProxyFileName(string serverAssemblyName)
        {
            StringBuilder sb = new StringBuilder();
            if (serverAssemblyName == null)
            {
                sb.Append("_ClientProxies");
            }
            else
            {
                sb.Append(Path.GetFileNameWithoutExtension(serverAssemblyName));
            }
            sb.Append(".g.");
            sb.Append(this.FileExtension);
            return sb.ToString();
        }

        /// <summary>
        /// Returns the set of <see cref="ITaskItem"/>s normalized to be relative to the given
        /// directory.
        /// </summary>
        /// <param name="items">The list of task items to normalize.  Null is permitted and results in empty list returned</param>
        /// <param name="directory">The folder path to use as the root if the paths are relative.</param>
        /// <returns>A new string collection of the full paths of the file names in the given <paramref name="items"/> array.</returns>
        private IEnumerable<string> NormalizedTaskItems(IEnumerable<ITaskItem> items, string directory)
        {
            if (items == null)
            {
                return Array.Empty<string>();
            }

            return items.Select<ITaskItem, string>(ti => this.GetFullPathRelativeToDirectory(ti.ItemSpec, directory));
        }

        /// <summary>
        /// Helper method to convert input project paths to full path names.
        /// </summary>
        protected override void NormalizeProjectPaths()
        {
            // Let the base normalize the client path first since we depend on it
            base.NormalizeProjectPaths();

            if (!string.IsNullOrEmpty(this.ServerProjectPath) && !Path.IsPathRooted(this.ServerProjectPath))
            {
                this.ServerProjectPath = this.GetFullPathRelativeToDirectory(this.ServerProjectPath, this.ClientProjectDirectory);
            }

            // If we detect a Open Ria Services  Link but cannot locate the specified server project, it probably
            // means the user renamed or moved it.   Warn them of that and give a hint how to fix.
            if (!this.IsServerProjectAvailable)
            {
                string clientProject = Path.GetFileName(this.ClientProjectPath);
                this.LogError(string.Format(CultureInfo.CurrentCulture, Resource.Server_Project_File_Does_Not_Exist, clientProject, this.ServerProjectPath));
            }
        }

        private void SetArgumentListForConsoleApp(List<string> arguments, string generatedFileName, ClientCodeGenerationOptions options, SharedCodeServiceParameters parameters, string pipeName)
        {
            static void AddEscaped(List<string> list, string parameter)
            {
                list.Add($"\"{parameter}\"");
            }
            static void AddParameters(List<string> list, string parameter, string[] values)
            {
                if (values != null && values.Length > 0)
                {
                    list.Add(parameter);
                    foreach (var value in values)
                        AddEscaped(list, value);
                }
            }
            static void AddParameter(List<string> list, string parameter, string value)
            {
                if (value is not null)
                {
                    list.Add(parameter);
                    AddEscaped(list, value);
                }
            }


            //Arguments for ClientCodeGenerationOptions
            arguments.Add("--language");
            arguments.Add(options.Language);
            arguments.Add("--clientFrameworkPath");
            AddEscaped(arguments, options.ClientFrameworkPath);
            arguments.Add("--serverProjectPath");
            AddEscaped(arguments, options.ServerProjectPath);
            arguments.Add("--clientProjectPath");
            AddEscaped(arguments, options.ClientProjectPath);
            arguments.Add("--clientRootNamespace");
            AddEscaped(arguments, options.ClientRootNamespace ?? string.Empty);
            arguments.Add("--serverRootNamespace");
            AddEscaped(arguments, options.ServerRootNamespace);
            arguments.Add("--isApplicationContextGenerationEnabled");
            arguments.Add(options.IsApplicationContextGenerationEnabled.ToString());
            arguments.Add("--clientProjectTargetPlatform");
            arguments.Add(options.ClientProjectTargetPlatform.ToString());
            arguments.Add("--useFullTypeNames");
            arguments.Add(options.UseFullTypeNames.ToString());

            //Arguments for SharedCodeServiceParameters
            AddParameters(arguments, "--sharedSourceFiles", parameters.SharedSourceFiles);
            AddParameters(arguments, "--symbolSearchPaths", parameters.SymbolSearchPaths);
            AddParameters(arguments, "--serverAssemblies", parameters.ServerAssemblies);
            AddParameters(arguments, "--clientAssemblies", parameters.ClientAssemblies);
            AddParameters(arguments, "--clientAssemblyPathsNormalized", parameters.ClientAssemblyPathsNormalized);

            //Other arguments
            AddParameter(arguments, "--codeGeneratorName", CodeGeneratorName);
            AddParameter(arguments, "--generatedFileName", generatedFileName);
            AddParameter(arguments, "--loggingPipe", pipeName);
        }

        #region Nested Types
        /// <summary>
        /// Nested class to handle cross-appdomain logging requests
        /// </summary>
        internal class CrossAppDomainLogger : MarshalByRefObject, ILoggingService
        {
            private readonly ILoggingService baseLogger;

            public CrossAppDomainLogger(ILoggingService underlyingLogger)
            {
                this.baseLogger = underlyingLogger;
            }

            public bool HasLoggedErrors
            {
                get { return this.baseLogger.HasLoggedErrors; }
            }

            public void LogError(string message)
            {
                this.baseLogger.LogError(message);
            }
            public void LogException(Exception ex)
            {
                this.baseLogger.LogException(ex);
            }

            public void LogWarning(string message)
            {
                this.baseLogger.LogWarning(message);
            }

            public void LogMessage(string message)
            {
                this.baseLogger.LogMessage(message);
            }

            public void LogError(string message, string subCategory, string errorCode, string helpKeyword, string file, int lineNumber, int columnNumber, int endLineNumber, int endColumnNumber)
            {
                this.baseLogger.LogError(message, subCategory, errorCode, helpKeyword, file, lineNumber, columnNumber, endLineNumber, endColumnNumber);
            }

            public void LogWarning(string message, string subCategory, string errorCode, string helpKeyword, string file, int lineNumber, int columnNumber, int endLineNumber, int endColumnNumber)
            {
                this.baseLogger.LogWarning(message, subCategory, errorCode, helpKeyword, file, lineNumber, columnNumber, endLineNumber, endColumnNumber);
            }
        }
        #endregion // Nested Types
    }
}
