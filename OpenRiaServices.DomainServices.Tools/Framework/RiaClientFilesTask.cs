using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using OpenRiaServices.DomainServices.Tools.SharedTypes;

namespace OpenRiaServices.DomainServices.Tools
{
    /// <summary>
    /// Common base class shared by <see cref="CreateOpenRiaClientFilesTask"/> and <see cref="CleanOpenRiaClientFilesTask"/>
    /// </summary>
    public abstract class RiaClientFilesTask : Task, ILogger, ILoggingService
    {
        // Name of the folder where generated code will be written
        internal const string GeneratedCodeFolderName = "Generated_Code";

        // Name of file where we keep track of the files we generated (in OutputPath)
        internal const string FileListFileName = "RiaFiles.txt";

        // Name of file where we keep track of the client references between builds (in OutputPath)
        internal const string ClientReferenceListFileName = "RiaClientRefs.txt";

        // Name of file where we keep track of the server references between builds (in OutputPath)
        internal const string ServerReferenceListFileName = "RiaServerRefs.txt";

        // Name of file where we keep track of the known source files from the server projects
        internal const string SourceFileListFileName = "RiaSourceFiles.txt";

        // Name of file where we keep track of the known <LinkedServerProjects> for client projects
        internal const string LinkedServerProjectsFileName = "RiaLinks.txt";

        private string _outputDirectory;
        private string _generatedCodePath;
        private string _clientProjectDirectory;
        private bool _filesWritten;
        private bool _errorLogged;

        /// <summary>
        /// Gets a value indicating whether any files were written
        /// </summary>
        internal bool FilesWereWritten
        {
            get
            {
                return this._filesWritten;
            }
            set
            {
                this._filesWritten = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this is a build
        /// specifically for Live Intellisense.
        /// </summary>
        /// <value>
        /// This property is no longer used but exists for backward compatibility
        /// </value>
        public string LiveIntellisense { get; set; }

        /// <summary>
        /// Gets or sets the path where ancillary temporary files should go
        /// </summary>
        /// <value>
        /// This path may be absolute or relative.  If relative, it will be considered to
        /// be relative to the <see cref="ClientProjectPath"/>
        /// This property is required.  Normal MSBuild semantics will not permit this task
        /// to be used unless it has been set.
        /// </value>
        [Required]
        public string OutputPath { get; set; }

        /// <summary>
        /// Gets or sets the path to the client project currently running this task.
        /// </summary>
        /// <value>
        /// Currently supported values are currently "C#" or "VB"
        /// This property is required
        /// </value>
        [Required]
        public string ClientProjectPath { get; set; }

        /// <summary>
        /// Gets or sets the path where generated code files should go
        /// </summary>
        internal string GeneratedCodePath
        {
            get
            {
                if (string.IsNullOrEmpty(this._generatedCodePath))
                {
                    this._generatedCodePath = this.GetFullPathRelativeToDirectory(GeneratedCodeFolderName, this.ClientProjectDirectory);
                }
                return this._generatedCodePath;
            }
            set
            {
                // Null resets so next get recomputes default
                // Non-rooted path evals relative to client project
                // Full path is taken verbatim
                this._generatedCodePath = (string.IsNullOrEmpty(value))
                                            ? null
                                            : Path.IsPathRooted(value)
                                                ? value
                                                : this.GetFullPathRelativeToDirectory(value, this.ClientProjectDirectory);
            }
        }

        /// <summary>
        /// Gets the path of the file where to write the list of generated files
        /// </summary>
        /// <returns>The full path to where to write the list of generated files.</returns>
        internal string FileListPath()
        {
             return Path.Combine(this.GetHistoryFolder(), this.PrependClientProjectName(FileListFileName));
        }

        /// <summary>
        /// Gets the path of the file where we write the list of known source projects and their files
        /// </summary>
        /// <returns>The full path to where to write the list of source files.</returns>
        internal string SourceFileListPath()
        {
             return Path.Combine(this.GetHistoryFolder(), this.PrependClientProjectName(SourceFileListFileName));
        }

        /// <summary>
        /// Gets the path of the file where we write the list of linked server projects
        /// </summary>
        /// <returns>The full path to where to write the list of linked server project files.</returns>
        internal string LinkedServerProjectsPath()
        {
             return Path.Combine(this.GetHistoryFolder(), this.PrependClientProjectName(LinkedServerProjectsFileName));
        }

        /// <summary>
        /// Gets the path of the file where to write the list of client references
        /// </summary>
        /// <returns>The full path to where to write the list of client references files.</returns>
        internal string ClientReferenceListPath()
        {
            return Path.Combine(this.GetHistoryFolder(), this.PrependClientProjectName(ClientReferenceListFileName));
        }

        /// <summary>
        /// Gets the path of the file where to write the list of server references
        /// </summary>
        /// <returns>The full path to where to write the list of server references files.</returns>
        internal string ServerReferenceListPath()
        {
            return Path.Combine(this.GetHistoryFolder(), this.PrependClientProjectName(ServerReferenceListFileName));
        }

        /// <summary>
        /// Gets the absolute path to the project running this task
        /// </summary>
        protected string ClientProjectDirectory
        {
            get
            {
                if (this._clientProjectDirectory == null)
                {
                    if (this.ClientProjectPath == null)
                    {
                        this.LogError(string.Format(CultureInfo.CurrentCulture, Resource.ProjectPath_Argument_Required, "ClientProjectPath"));
                        return string.Empty;
                    }
                    this._clientProjectDirectory = Path.GetFullPath(Path.GetDirectoryName(this.ClientProjectPath));
                }
                return this._clientProjectDirectory;
            }
        }

        /// <summary>
        /// Gets the absolute path of the output directory.
        /// </summary>
        protected string OutputDirectory
        {
            get
            {
                if (this._outputDirectory == null)
                {
                    string path = this.OutputPath;
                    if (!Path.IsPathRooted(path))
                    {
                        path = Path.Combine(this.ClientProjectDirectory, path);
                    }
                    this._outputDirectory = Path.GetFullPath(path);
                }
                return this._outputDirectory;
            }
        }

        /// <summary>
        /// Implementation of the normal MSBuild tasks execution entry point method
        /// </summary>
        /// <returns><c>true</c> for success.  If <c>false</c>, warnings or errors must have been logged.</returns>
        public override bool Execute()
        {
            // clear internal state in case we are reused
            this._outputDirectory = null;
            this._clientProjectDirectory = null;
            this._filesWritten = false;
            this._errorLogged = false;

            // Preprocess input parameters to create full paths
            this.NormalizeProjectPaths();

            return this.ExecuteInternal();
        }

        /// <summary>
        /// Internal implementation for the <see cref="Execute"/> method, called from this base class.
        /// </summary>
        /// <returns><c>true</c> for success.  If <c>false</c>, warnings or errors must have been logged.</returns>
        protected abstract bool ExecuteInternal();

        /// <summary>
        /// Helper method to convert input project paths to full path names.
        /// </summary>
        protected virtual void NormalizeProjectPaths()
        {
            if (!string.IsNullOrEmpty(this.ClientProjectPath) && !Path.IsPathRooted(this.ClientProjectPath))
            {
                this.ClientProjectPath = Path.GetFullPath(this.ClientProjectPath);
            }
            if (!string.IsNullOrEmpty(this.OutputPath) && !Path.IsPathRooted(this.OutputPath))
            {
                this.OutputPath = this.GetFullPathRelativeToDirectory(this.OutputPath, this.ClientProjectDirectory);
            }
        }

        /// <summary>
        /// Returns the full path to the folder where we keep a breadcrumb history of
        /// what the prior code generation pass did,
        /// </summary>
        /// <returns>The full path to where we are allowed to write history files for subsequent builds.</returns>
        internal string GetHistoryFolder()
        {
            return this.OutputDirectory;
        }

        #region ILogger Members

        /// <summary>
        /// Gets a value indicating whether any errors were logged.
        /// </summary>
        public bool HasLoggedErrors
        {
            get
            {
                return this._errorLogged;
            }
            protected set
            {
                this._errorLogged = value;
            }
        }

        /// <summary>
        /// Logs the given error message to the logger associated with this task
        /// </summary>
        /// <param name="message">Message to log</param>
        public void LogError(string message)
        {
            this.HasLoggedErrors = true;
            this.Log.LogError(message);
        }

        /// <summary>
        /// Logs the given warning message to the logger associated with this task
        /// </summary>
        /// <param name="message">Message to log</param>
        public void LogWarning(string message)
        {
            this.Log.LogWarning(message);
        }

        /// <summary>
        /// Logs the given informational message to the logger associated with this task
        /// </summary>
        /// <param name="message">Message to log</param>
        public void LogMessage(string message)
        {
            this.Log.LogMessage(message);
        }

        /// <summary>
        /// Logs the given message as a warning, together with information about the source location.
        /// </summary>
        /// <param name="message">The message to log as an error.</param>
        /// <param name="subcategory">The optional description of the error type.</param>
        /// <param name="errorCode">The optional error code.</param>
        /// <param name="helpKeyword">The optional help keyword.</param>      
        /// <param name="file">The optional path to the file containing the error.</param>
        /// <param name="lineNumber">The zero-relative line number in the <paramref name="file"/> where the error begins.</param>
        /// <param name="columnNumber">The zero-relative column number in the <paramref name="file"/> where the error begins.</param>
        /// <param name="endLineNumber">The zero-relative line number in the <paramref name="file"/> where the error ends.</param>
        /// <param name="endColumnNumber">The zero-relative column number in the <paramref name="file"/> where the error ends.</param>
        public void LogWarning(string message, string subcategory, string errorCode, string helpKeyword, string file, int lineNumber, int columnNumber, int endLineNumber, int endColumnNumber)
        {
            this.Log.LogWarning(subcategory, errorCode, helpKeyword, file, lineNumber, columnNumber, endLineNumber, endColumnNumber, message);
        }

        /// <summary>
        /// Logs the given message as an error, together with information about the source location.
        /// </summary>
        /// <param name="message">The message to log as an error.</param>
        /// <param name="subcategory">The optional description of the error type.</param>
        /// <param name="errorCode">The optional error code.</param>
        /// <param name="helpKeyword">The optional help keyword.</param>      
        /// <param name="file">The optional path to the file containing the error.</param>
        /// <param name="lineNumber">The zero-relative line number in the <paramref name="file"/> where the error begins.</param>
        /// <param name="columnNumber">The zero-relative column number in the <paramref name="file"/> where the error begins.</param>
        /// <param name="endLineNumber">The zero-relative line number in the <paramref name="file"/> where the error ends.</param>
        /// <param name="endColumnNumber">The zero-relative column number in the <paramref name="file"/> where the error ends.</param>
        public void LogError(string message, string subcategory, string errorCode, string helpKeyword, string file, int lineNumber, int columnNumber, int endLineNumber, int endColumnNumber)
        {
            this.Log.LogError(subcategory, errorCode, helpKeyword, file, lineNumber, columnNumber, endLineNumber, endColumnNumber, message);
        }

        /// <summary>
        /// Sets or resets the read-only file attribute of the given file
        /// </summary>
        /// <param name="fileName">Full path to the file to modify</param>
        /// <param name="newState">The desired state of the bit</param>
        /// <returns><c>false</c> if a problem occurred and the bit could not be altered.</returns>
        internal bool SafeSetReadOnlyAttribute(string fileName, bool newState)
        {
            string errorMessage = null;

            if (File.Exists(fileName))
            {
                try
                {
                    FileAttributes attributes = File.GetAttributes(fileName);
                    bool isSet = ((attributes & FileAttributes.ReadOnly) != 0);
                    if (isSet != newState)
                    {
                        attributes ^= FileAttributes.ReadOnly;
                        File.SetAttributes(fileName, attributes);
                    }
                }
                catch (IOException ioe)
                {
                    errorMessage = ioe.Message;
                }
                catch (NotSupportedException nse)
                {
                    errorMessage = nse.Message;
                }
                catch (UnauthorizedAccessException uae)
                {
                    errorMessage = uae.Message;
                }

                if (errorMessage != null)
                {
                    this.LogError(string.Format(CultureInfo.CurrentCulture, Resource.Failed_To_Modify_ReadOnly, fileName, errorMessage));
                }
            }

            return (errorMessage == null);
        }

        /// <summary>
        /// Safe form of File.Delete that catches and logs exceptions as warnings.
        /// </summary>
        /// <param name="fileName">The full path to the file to delete.</param>
        /// <returns><c>false</c> if an error occurred and the file could not be deleted.</returns>
        internal bool SafeFileDelete(string fileName)
        {
            string errorMessage = null;
            if (!string.IsNullOrEmpty(fileName) && File.Exists(fileName))
            {
                // Ensure readonly bit is turned off
                if (!this.SafeSetReadOnlyAttribute(fileName, false))
                {
                    return false;
                }

                try
                {
                    File.Delete(fileName);
                }
                catch (IOException ioe)
                {
                    errorMessage = ioe.Message;
                }
                catch (NotSupportedException nse)
                {
                    errorMessage = nse.Message;
                }
                catch (UnauthorizedAccessException uae)
                {
                    errorMessage = uae.Message;
                }

                if (errorMessage != null)
                {
                    this.LogWarning(string.Format(CultureInfo.CurrentCulture, Resource.Failed_To_Delete_File_Error, fileName, errorMessage));
                }
            }

            return (errorMessage == null);
        }

        /// <summary>
        /// Safe form of writing string contents to a file.
        /// </summary>
        /// <param name="fileName">The full path to the file to write.</param>
        /// <param name="contents">The string content to write.</param>
        /// <returns><c>false</c> if an error occurred and the file could not be written.</returns>
        internal bool SafeFileWrite(string fileName, string contents)
        {
            string errorMessage = null;

            try
            {
                File.WriteAllText(fileName, contents);
            }
            catch (IOException ioe)
            {
                errorMessage = ioe.Message;
            }
            catch (NotSupportedException nse)
            {
                errorMessage = nse.Message;
            }
            catch (UnauthorizedAccessException uae)
            {
                errorMessage = uae.Message;
            }

            if (errorMessage != null)
            {
                this.LogWarning(string.Format(CultureInfo.CurrentCulture, Resource.Failed_To_Write_File, fileName, errorMessage));
            }
  
            return (errorMessage == null);
        }

        /// <summary>
        /// Copies the file specified by <paramref name="sourceFile"/> to <paramref name="destinationFile"/>
        /// </summary>
        /// <param name="sourceFile">Full path of the source file to copy.</param>
        /// <param name="destinationFile">Full path to the destination.</param>
        /// <param name="isProjectFile">If <c>true</c> handle the readonly attribute and track changes.</param>
        /// <returns><c>true</c> if this method succeeded</returns>
        internal bool SafeFileCopy(string sourceFile, string destinationFile, bool isProjectFile)
        {
            string errorMessage = null;
            if (!string.IsNullOrEmpty(destinationFile) && 
                !string.IsNullOrEmpty(sourceFile) && File.Exists(sourceFile))
            {
                // Ensure the destination folder exists
                this.SafeFolderCreate(Path.GetDirectoryName(destinationFile));

                // Ensure the read-only attribute is reset on the destination if it exists
                if (isProjectFile)
                {
                    this.SafeSetReadOnlyAttribute(destinationFile, false);
                }

                try
                {
                    // Copy the file content.  This also copies the file write time which we rely
                    // on for detecting the need to copy the next time.
                    File.Copy(sourceFile, destinationFile, /*overwrite*/ true);
                }
                catch (IOException ioe)
                {
                    errorMessage = ioe.Message;
                }
                catch (NotSupportedException nse)
                {
                    errorMessage = nse.Message;
                }
                catch (UnauthorizedAccessException uae)
                {
                    errorMessage = uae.Message;
                }

                if (errorMessage != null)
                {
                    this.LogWarning(string.Format(CultureInfo.CurrentCulture, Resource.Failed_To_Copy_File, sourceFile, destinationFile, errorMessage));
                }
            }

            // Set ReadOnly attribute to prevent casual edits.
            // Failure here is logged but does not affect the success of the write.
            if (isProjectFile)
            {
                this.SafeSetReadOnlyAttribute(destinationFile, true);
                this.FilesWereWritten = true;
            }

            return (errorMessage == null);
        }

        /// <summary>
        /// Moves/Renames the file specified by <paramref name="sourceFile"/> to <paramref name="destinationFile"/>
        /// </summary>
        /// <param name="sourceFile">Full path of the source file to move.</param>
        /// <param name="destinationFile">Full path to the destination.</param>
        /// <returns><c>true</c> if this method succeeded</returns>
        internal bool SafeFileMove(string sourceFile, string destinationFile)
        {
            string errorMessage = null;
            if (!string.IsNullOrEmpty(destinationFile) &&
                !string.IsNullOrEmpty(sourceFile) && File.Exists(sourceFile))
            {
                // Ensure the destination is gone
                this.SafeFileDelete(destinationFile);

                try
                {
                    File.Move(sourceFile, destinationFile);
                }
                catch (IOException ioe)
                {
                    errorMessage = ioe.Message;
                }
                catch (NotSupportedException nse)
                {
                    errorMessage = nse.Message;
                }
                catch (UnauthorizedAccessException uae)
                {
                    errorMessage = uae.Message;
                }

                if (errorMessage != null)
                {
                    this.LogWarning(string.Format(CultureInfo.CurrentCulture, Resource.Failed_To_Rename_File, sourceFile, destinationFile, errorMessage));
                }
            }

            return (errorMessage == null);
        }

        /// <summary>
        /// Safe form of <see cref="Directory.CreateDirectory(string)"/>
        /// </summary>
        /// <remarks>
        /// This method will do nothing if the folder already exists, otherwise it
        /// will create it.  Any error in creation will log an appropriate error message
        /// and return <c>false</c>
        /// </remarks>
        /// <param name="directoryPath">The full path to the folder to create.</param>
        /// <returns><c>false</c> if an error occurred and the folder could not be created.</returns>
        internal bool SafeFolderCreate(string directoryPath)
        {
            string errorMessage = null;
            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            {
                try
                {
                    Directory.CreateDirectory(directoryPath);
                }
                catch (IOException ioe)
                {
                    errorMessage = ioe.Message;
                }
                catch (NotSupportedException nse)
                {
                    errorMessage = nse.Message;
                }
                catch (UnauthorizedAccessException uae)
                {
                    errorMessage = uae.Message;
                }

                if (errorMessage != null)
                {
                    this.LogError(string.Format(CultureInfo.CurrentCulture, Resource.Failed_To_Create_Folder, directoryPath, errorMessage));
                }
            }
            return (errorMessage == null);
        }

        #endregion


        #region VS Integration

        /// <summary>
        /// Tests whether the last write time of the given <paramref name="fileName"/>
        /// is different than the given <paramref name="referenceTime"/>
        /// </summary>
        /// <param name="fileName">File to test</param>
        /// <param name="referenceTime">Time to test it againt</param>
        /// <returns><c>true</c> if the given file has a different last write time than the given time</returns>
        internal bool IsFileWriteTimeDifferent(string fileName, DateTime referenceTime)
        {
            // If the file being tested does not exist, it is by definition "different"
            if (!File.Exists(fileName))
            {
                return true;
            }

            DateTime fileTime = this.GetLastChangeTimeFromVS(fileName);
            return fileTime != referenceTime;
        }

        /// <summary>
        /// Tests whether the last write time of the given <paramref name="fileName"/>
        /// is different than the given <paramref name="referenceFileName"/>
        /// </summary>
        /// <param name="fileName">File to test</param>
        /// <param name="referenceFileName">File to test it againt</param>
        /// <returns><c>true</c> if the given <paramref name="fileName"/> has a different last write time than the given <paramref name="referenceFileName"/></returns>
        internal bool IsFileWriteTimeDifferent(string fileName, string referenceFileName)
        {
            // If the file we are comparing against does not exist, it means the fileName is "newer"
            if (!File.Exists(referenceFileName))
            {
                return true;
            }
            return this.IsFileWriteTimeDifferent(fileName, this.GetLastChangeTimeFromVS(referenceFileName));
        }

        /// <summary>
        /// Get the last time that the file was changed from Visual Studio
        /// </summary>
        /// <param name="visualStudioFile">The file to test</param>
        /// <returns>The DateTime of the file's last modified time</returns>
        internal DateTime GetLastChangeTimeFromVS(string visualStudioFile)
        {
            // If file does not exist, return a sentinel min value
            return File.Exists(visualStudioFile) ? File.GetLastWriteTime(visualStudioFile) : DateTime.MinValue;
        }

        /// <summary>
        /// Reads the contents of the given file.
        /// </summary>
        /// <param name="fileName">File to read</param>
        /// <returns>The contents of the file as a string</returns>
        internal string ReadFileFromVS(string fileName)
        {
            return File.ReadAllText(fileName);
        }

        /// <summary>
        /// Writes the given content to the given file if it is non-empty, else deletes the file
        /// </summary>
        /// <param name="destinationFile">Full path to file to write or delete</param>
        /// <param name="content">Content to write to file</param>
        /// <param name="forceWriteToFile">If <c>true</c>, write file always, even if Intellisense only build.</param>
        /// <returns><c>true</c> if the write succeeded, <c>false</c> if it was deleted or the write failed.</returns>
        internal bool WriteOrDeleteFileToVS(string destinationFile, string content, bool forceWriteToFile)
        {
            if (string.IsNullOrEmpty(content))
            {
                // Delete the old generated file -- but only if it exists.
                // If there was nothing generated and no file exists, it is better simply to say nothing.
                if (File.Exists(destinationFile))
                {
                    this.LogMessage(string.Format(CultureInfo.CurrentCulture, Resource.Deleting_Empty_File, destinationFile));
                    this.DeleteFileFromVS(destinationFile);
                }
                return false;
            }
            else
            {
                return this.WriteFileToVS(destinationFile, content, forceWriteToFile);
            }
        }

        /// <summary>
        /// Writes the given file to VS.  If VS is not present, it just writes to the file system
        /// </summary>
        /// <param name="destinationFile">Name of the file to create/write</param>
        /// <param name="content">String content of file to write</param>
        /// <param name="forceWriteToFile">If <c>true</c>, write file always, even if Intellisense only build.</param>
        /// <returns><c>true</c> if the write succeeded</returns>
        internal bool WriteFileToVS(string destinationFile, string content, bool forceWriteToFile)
        {
            // Create the folder as late as possible, but a failure here
            // logs a message and does not do the write
            string folder = Path.GetDirectoryName(destinationFile);
            if (!this.SafeFolderCreate(folder))
            {
                return false;
            }

            // Reset read-only bit first or write may fail.
            // We do this because we are the ones who set that attribute in the first place (to discourage user edits)
            if (!this.SafeSetReadOnlyAttribute(destinationFile, false))
            {
                return false;
            }

            this.SafeFileWrite(destinationFile, content);

            // Set ReadOnly attribute to prevent casual edits.
            // Failure here is logged but does not affect the success of the write.
            this.SafeSetReadOnlyAttribute(destinationFile, true);

            this.FilesWereWritten = true;
            return true;
        }

        /// <summary>
        /// Deletes the specified file in VS-compatible way
        /// </summary>
        /// <param name="fileName">Full path of the file to delete.  It may or may not exist on disk.</param>
        internal void DeleteFileFromVS(string fileName)
        {
            if (File.Exists(fileName))
            {
                // Reset Read-Only file attribute
                // We do this because we set this attribute on files we generate.
                this.SafeSetReadOnlyAttribute(fileName, false);

                // If VS does not delete this file or if VS is not present, delete it
                // from the file system manually.
                this.SafeFileDelete(fileName);

                // If anything failed here, log a warning so user knows the file could not be removed.
                if (File.Exists(fileName))
                {
                    this.LogWarning(string.Format(CultureInfo.CurrentCulture, Resource.Failed_To_Delete_File, fileName));
                }
            }
        }

        #endregion VS Integration

        /// <summary>
        /// Constructs the absolute path to a given file.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        /// <param name="directory">The directory path.</param>
        /// <returns>An absolute path based on specified fileName and directory.</returns>
        private static string FullPath(string fileName, string directory)
        {
            string path = Path.IsPathRooted(fileName) ? fileName : Path.Combine(directory, fileName);
            return Path.GetFullPath(path);
        }

        /// <summary>
        /// Prepends the simple name of the client project file
        /// to the given <paramref name="fileName"/> which is assumed
        /// to be just a base name (no path).
        /// </summary>
        /// <remarks>
        /// This method is used to produce unique file names associated
        /// with a client project in the event there are multiple client
        /// projects within a single physical folder.
        /// </remarks>
        /// <param name="fileName">Simple short name of a file</param>
        /// <returns>The input <paramref name="fileName"/> with the client project name prepended.</returns>
        private string PrependClientProjectName(string fileName)
        {
            string clientProjectFileName = Path.GetFileNameWithoutExtension(this.ClientProjectPath);
            return clientProjectFileName + "." + fileName;
        }

        /// <summary>
        /// Given a potentially relative path and a root directory, resolves to a full path.
        /// </summary>
        /// <param name="fileName">Name of file.  Maybe relative or full.</param>
        /// <param name="directory">Name of directory to use as root if file name is relative.</param>
        /// <returns>The full path of the file</returns>
        protected string GetFullPathRelativeToDirectory(string fileName, string directory)
        {
            if (!Path.IsPathRooted(fileName))
            {
                // If we are given a relative project path, resolve it relative to our current MSBuild project
                if (!Path.IsPathRooted(directory))
                {
                    directory = this.GetFullPathRelativeToDirectory(directory, this.ClientProjectDirectory);
                }

                fileName = Path.GetFullPath(Path.Combine(directory, fileName));
            }

            // Files with relative pathing inside need to expand that out now
            if (fileName.Contains(".."))
            {
                fileName = Path.GetFullPath(fileName);
            }
            return fileName;
        }

        /// <summary>
        /// If the file path is under the project directory, returns the relative path under the project directory, else returns the original file path.
        /// </summary>
        /// <param name="fileName">The full file path</param>
        /// <param name="projectDirectoryPath">The project directory path.</param>
        /// <returns>Relative path under the project directory</returns>
        protected static string GetPathRelativeToProjectDirectory(string fileName, string projectDirectoryPath)
        {
            string relativePath = Path.GetFullPath(fileName);
            if (!projectDirectoryPath.EndsWith("\\", StringComparison.OrdinalIgnoreCase))
            {
                projectDirectoryPath = string.Concat(projectDirectoryPath, "\\");
            }
            if (relativePath.StartsWith(projectDirectoryPath, StringComparison.OrdinalIgnoreCase))
            {
                relativePath = relativePath.Remove(0, projectDirectoryPath.Length);
            }

            return relativePath;
        }

        /// <summary>
        /// Returns the set of full file names previously written by the
        /// last code generation pass.
        /// </summary>
        /// <remarks>The list is ordered (descending) by length of folder name so that subfolders
        /// appear before their parent folders.
        /// </remarks>
        /// <returns>The collection of full file names.  The list may be empty but it will not be null.</returns>
        protected IEnumerable<string> FilesPreviouslyWritten()
        {
            List<string> files = new List<string>();
            string fileListFile = this.FileListPath();

            // This runs only if we have a file list from a prior run
            if (!String.IsNullOrEmpty(fileListFile) && File.Exists(fileListFile))
            {
                // Extract all the prior file names
                using (StreamReader reader = new StreamReader(fileListFile))
                {
                    string projectPath;
                    if ((projectPath = reader.ReadLine()) != null)
                    {
                        // Check (again) if the projectpath is same as client project path (just in case it is differnet and purging the breadcrumb files failed for some reason.)
                        if (!string.IsNullOrEmpty(projectPath) && string.Equals(projectPath, this.ClientProjectDirectory, StringComparison.OrdinalIgnoreCase))
                        {
                            string fileName;
                            while ((fileName = reader.ReadLine()) != null)
                            {
                                if (!string.IsNullOrEmpty(fileName))
                                {
                                    //Get full file path in case relative path was stored
                                    fileName = this.GetFullPathRelativeToDirectory(fileName, projectPath);
                                    files.Add(fileName);
                                }
                            }
                        }
                    }
                }
            }
            // OrderByDescending by length of folder path so we deal with longest paths first.
            // This makes subfolders appear before their respective parent folders, which
            // gives the most predictability when purging old files and we want to delete
            // empty folders as we go.
            return files.OrderByDescending(f => Path.GetDirectoryName(f).Length);
        }

        /// <summary>
        /// Deletes the specified folder if it is empty.
        /// </summary>
        /// <param name="folderPath">Full path to folder</param>
        protected void DeleteFolderIfEmpty(string folderPath)
        {
            // Specifically block attempts to delete the OutputPath folder.
            // Experience has shown other tools expect it to have been created for them,
            // and deleting it, even when empty, sets them up for failure later when they
            // attempt to write to it.
            if (string.Equals(RiaClientFilesTask.NormalizeFolderPath(folderPath), RiaClientFilesTask.NormalizeFolderPath(this.OutputDirectory), StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (Directory.Exists(folderPath))
            {
                try
                {
                    // Presence of files exits without deleting
                    string[] files = Directory.GetFiles(folderPath);
                    if (files.Length != 0)
                    {
                        return;
                    }

                    // Presence of folders attempts (safe) recursive delete in case they are empty
                    string[] folders = Directory.GetDirectories(folderPath);
                    foreach (string folder in folders)
                    {
                        string innerFolderPath = Path.Combine(folderPath, folder);
                        this.DeleteFolderIfEmpty(innerFolderPath);

                        // Failure to delete inner folder abandons attempt to delete outer
                        if (Directory.Exists(innerFolderPath))
                        {
                            return;
                        }
                    }

                    this.LogMessage(string.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_Deleting_Orphan_Folder, folderPath));

                    // TODO, 244509: the folder may be locked by devenv (Dev11)
                    // even after the compilation step was done.  Calling Directory.Delete
                    // will return successfully but leave the folder in a mark-deleted state 
                    // and not-reusable.  This is problematic for Generated_Code folder.
                    // We decided it is safe to leave empty folder as a workaround.
                    // Directory.Delete(folderPath);
                }
                catch (Exception ex)
                {
                    if (ex is IOException || ex is UnauthorizedAccessException || ex is DirectoryNotFoundException)
                    {
                        this.LogWarning(string.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_Failed_Delete_Folder, folderPath, ex.Message));
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Strips trailing slashes from the given folder path if present
        /// </summary>
        /// <param name="path">The path to normalize.</param>
        /// <returns>The input <paramref name="path"/> without any trailing slashes</returns>
        internal static string NormalizeFolderPath(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            // Empty paths are not accepted by Path.GetFullPath
            if (path.Length > 0)
            {
                path = Path.GetFullPath(path);
                int len = path.Length;
                if (len > 0 && path[len - 1] == Path.DirectorySeparatorChar)
                {
                    path = path.Substring(0, len - 1);
                }
            }
            return path;
        }

        /// <summary>
        /// Deletes the breadcrumb files we created to keep track of files written on prior pass
        /// </summary>
        protected void DeleteCodeGenMetafileLists()
        {
            string fileName = this.FileListPath();
            if (!string.IsNullOrEmpty(fileName) && File.Exists(fileName))
            {
                // File is not normally visible to VS and is not in generated code folder,
                // so we brute-force delete it
                this.SafeFileDelete(fileName);
            }

            fileName = this.ClientReferenceListPath();
            if (!string.IsNullOrEmpty(fileName) && File.Exists(fileName))
            {
                // File is not normally visible to VS and is not in generated code folder,
                // so we brute-force delete it
                this.SafeFileDelete(fileName);
            }

            fileName = this.ServerReferenceListPath();
            if (!string.IsNullOrEmpty(fileName) && File.Exists(fileName))
            {
                // File is not normally visible to VS and is not in generated code folder,
                // so we brute-force delete it
                this.SafeFileDelete(fileName);
            }

            // Delete our list of source files
            fileName = this.SourceFileListPath();
            if (!string.IsNullOrEmpty(fileName) && File.Exists(fileName))
            {
                // File is not normally visible to VS and is not in generated code folder,
                // so we brute-force delete it
                this.SafeFileDelete(fileName);
            }

            // Delete our list of RIA Links
            fileName = this.LinkedServerProjectsPath();
            if (!string.IsNullOrEmpty(fileName) && File.Exists(fileName))
            {
                // File is not normally visible to VS and is not in generated code folder,
                // so we brute-force delete it
                this.SafeFileDelete(fileName);
            }

            // And delete the history folder itself if it is empty
            this.DeleteFolderIfEmpty(this.GetHistoryFolder());
        }
    }
}
