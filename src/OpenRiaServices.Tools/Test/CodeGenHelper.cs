using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRiaServices.Server.Test.Utilities;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using OpenRiaServices.Tools.SharedTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32;

namespace OpenRiaServices.Tools.Test
{

    public static class CodeGenHelper
    {
        public static void AssertGenerated(string generatedCode, string expected)
        {
            string normalizedGenerated = TestHelper.NormalizeWhitespace(generatedCode);
            string normalizedExpected = TestHelper.NormalizeWhitespace(expected);
            Assert.IsTrue(normalizedGenerated.IndexOf(normalizedExpected) >= 0, "Expected <" + expected + "> but saw\r\n<" + generatedCode + ">");
        }

        public static void AssertNotGenerated(string generatedCode, string notExpected)
        {
            string normalizedGenerated = TestHelper.NormalizeWhitespace(generatedCode);
            string normalizedNotExpected = TestHelper.NormalizeWhitespace(notExpected);
            Assert.IsTrue(normalizedGenerated.IndexOf(normalizedNotExpected) < 0, "Did not expect <" + notExpected + ">");
        }

        /// <summary>
        /// Assert that all the files named by shortNames exist in the files collection
        /// </summary>
        /// <param name="files">list of files to check</param>
        /// <param name="projectPath">project owning the short named files</param>
        /// <param name="shortNames">collection of file short names that must be in list</param>
        public static void AssertContainsOnlyFiles(List<string> files, string projectPath, string[] shortNames)
        {
            Assert.AreEqual(shortNames.Length, files.Count);
            AssertContainsFiles(files, projectPath, shortNames);
        }

        /// <summary>
        /// Assert that all the files named by shortNames exist in the files collection
        /// </summary>
        /// <param name="files">list of files to check</param>
        /// <param name="projectPath">project owning the short named files</param>
        /// <param name="shortNames">collection of file short names that must be in list</param>
        public static void AssertContainsFiles(List<string> files, string projectPath, string[] shortNames)
        {
            foreach (string shortName in shortNames)
            {
                string fullName = Path.Combine(Path.GetDirectoryName(projectPath), shortName);
                bool foundIt = false;
                foreach (string file in files)
                {
                    if (file.Equals(fullName, StringComparison.OrdinalIgnoreCase))
                    {
                        foundIt = true;
                        break;
                    }
                }
                if (!foundIt)
                {
                    string allFiles = string.Empty;
                    foreach (string file in files)
                        allFiles += ("\r\n" + file);

                    Assert.Fail("Expected to find " + fullName + " in list of files, but saw instead:" + allFiles);
                }
            }
        }

        public static string GetOutputFile(ITaskItem[] items, string shortName)
        {
            for (int i = 0; i < items.Length; ++i)
            {
                if (Path.GetFileName(items[i].ItemSpec).Equals(shortName, StringComparison.OrdinalIgnoreCase))
                {
                    string fileName = items[i].ItemSpec;
                    Assert.IsTrue(File.Exists(fileName), "Expected file " + fileName + " to have been created.");
                    return fileName;
                }
            }
            Assert.Fail("Expected to find output file " + shortName);
            return null;
        }

        /// <summary>
        /// Assert that <paramref name="items"/> contains all the files specified in <paramref name="shortNames"/>
        /// </summary>
        public static void AssertOutputContainsFiles(ITaskItem[] items, string[] shortNames)
        {
            foreach (var shortName in shortNames)
                GetOutputFile(items, shortName); // GetOutputFile assert that the file exists
        }

        /// <summary>
        /// Returns the name of the assembly built by the server project
        /// </summary>
        /// <param name="serverProjectPath"></param>
        /// <returns></returns>
        public static string ServerClassLibOutputAssembly(string serverProjectPath)
        {
            // We need to map any server side assembly references back to our deployment directory
            // if we have the same assembly there, otherwise the assembly load from calls end up
            // with multiple assemblies with the same types
            string deploymentDir = Path.GetDirectoryName(typeof(CodeGenHelper).Assembly.Location);
            string assembly = MsBuildHelper.GetOutputAssembly(serverProjectPath);
            return MapAssemblyReferenceToDeployment(deploymentDir, assembly);
        }

        /// <summary>
        /// Returns the collection of assembly references from the server project
        /// </summary>
        /// <param name="serverProjectPath"></param>
        /// <returns></returns>
        public static List<string> ServerClassLibReferences(string serverProjectPath, string[] hardCodedAsm = null)
        {
            //TODO: Change code to get output assemblies instead of references

            // We need to map any server side assembly references back to our deployment directory
            // if we have the same assembly there, otherwise the assembly load from calls end up
            // with multiple assemblies with the same types
            string deploymentDir = System.IO.Path.GetDirectoryName(typeof(CodeGenHelper).Assembly.Location);
            List<string> assemblies;
            if (hardCodedAsm == null)
                assemblies = MsBuildHelper.GetReferenceAssemblies(serverProjectPath);
            else
                assemblies = hardCodedAsm.ToList();
            // Remove reference assemblies since these do not contain any actual implementations
            assemblies.RemoveAll(asm => asm.Contains("Microsoft.AspNetCore.App.Ref"));
            MapAssemblyReferencesToDeployment(deploymentDir, assemblies);
            return assemblies;
        }

        /// <summary>
        /// Returns the collection of source files from the server
        /// </summary>
        /// <param name="serverProjectPath"></param>
        /// <returns></returns>
        public static List<string> ServerClassLibSourceFiles(string serverProjectPath)
        {
            return MsBuildHelper.GetSourceFiles(serverProjectPath);
        }

        /// <summary>
        /// Returns the collection of assembly references from the client project
        /// </summary>
        /// <param name="clientProjectPath"></param>
        /// <returns></returns>
        public static List<string> ClientClassLibReferences(string clientProjectPath, bool includeClientOutputAssembly, string[] hardCodedAsm = null)
        {
            List<string> references;
            if (hardCodedAsm == null)
                references = MsBuildHelper.GetReferenceAssemblies(clientProjectPath);
            else
                references = hardCodedAsm.ToList();

            // Note: we conditionally add the output assembly to enable this unit test to
            // define some shared types 
            if (includeClientOutputAssembly)
            {
                references.Add(MsBuildHelper.GetOutputAssembly(clientProjectPath));
            }

            // Remove mscorlib -- it causes problems using ReflectionOnlyLoad ("parent does not exist")
            for (int i = 0; i < references.Count; ++i)
            {
                if (Path.GetFileName(references[i]).Equals("mscorlib.dll", StringComparison.OrdinalIgnoreCase))
                {
                    references.RemoveAt(i);
                    break;
                }
            }
            return references;
        }

        /// <summary>
        /// Returns the collection of source files from the client
        /// </summary>
        /// <param name="clientProjectPath"></param>
        /// <returns></returns>
        public static List<string> ClientClassLibSourceFiles(string clientProjectPath)
        {
            return MsBuildHelper.GetSourceFiles(clientProjectPath);
        }


        /// <summary>
        /// Returns the full path of the server project based on our current project
        /// </summary>
        /// <param name="currentProjectPath"></param>
        /// <returns></returns>
        public static string ServerClassLibProjectPath(string currentProjectPath)
        {
            return Path.Combine(Path.GetDirectoryName(currentProjectPath), @"ServerClassLib\ServerClassLib.csproj");
        }

        /// <summary>
        /// Returns the full path of the server WAP project based on our current project
        /// </summary>
        /// <param name="currentProjectPath"></param>
        /// <returns></returns>
        public static string ServerWapProjectPath(string currentProjectPath)
        {
            return Path.Combine(Path.GetDirectoryName(currentProjectPath), @"TestWAP\TestWAP.csproj");
        }

        /// <summary>
        /// Returns the full path of the 2nd server project based on our current project
        /// </summary>
        /// <param name="currentProjectPath"></param>
        /// <returns></returns>
        public static string ServerClassLib2ProjectPath(string currentProjectPath)
        {
            return Path.Combine(Path.GetDirectoryName(currentProjectPath), @"ServerClassLib2\ServerClassLib2.csproj");
        }

        /// <summary>
        /// Returns the full path of the client project based on our current project
        /// </summary>
        /// <param name="currentProjectPath"></param>
        /// <returns></returns>
        public static string ClientClassLibProjectPath(string currentProjectPath)
        {
            return Path.Combine(Path.GetDirectoryName(currentProjectPath), @"ClientClassLib\ClientClassLib.csproj");
        }

        /// <summary>
        /// Returns the full path of the 2nd client project based on our current project
        /// </summary>
        /// <param name="currentProjectPath"></param>
        /// <returns></returns>
        public static string ClientClassLib2ProjectPath(string currentProjectPath)
        {
            return Path.Combine(Path.GetDirectoryName(currentProjectPath), @"ClientClassLib2\ClientClassLib2.csproj");
        }

        /// <summary>
        /// When running unit tests, assemblies we are analyzing may come from one place,
        /// but VSTT has copied a version locally that we are running.  This will cause
        /// confusion, so map all assembly references that have a local equivalent to
        /// that local version.
        /// </summary>
        /// <param name="referenceAssemblies"></param>
        public static void MapAssemblyReferencesToDeployment(string deploymentDir, IList<string> assemblies)
        {
            for (int i = 0; i < assemblies.Count; ++i)
            {
                assemblies[i] = MapAssemblyReferenceToDeployment(deploymentDir, assemblies[i]);
            }
        }

        public static string MapAssemblyReferenceToDeployment(string deploymentDir, string assembly)
        {
            string localPath = Path.Combine(deploymentDir, Path.GetFileName(assembly));
            if (File.Exists(localPath))
            {
                assembly = localPath;
            }
            return assembly;
        }

        internal static SharedCodeService CreateSharedCodeService(string clientProjectPath, ILoggingService logger)
        {
            List<string> sourceFiles = CodeGenHelper.ClientClassLibSourceFiles(clientProjectPath);
            List<string> assemblies = CodeGenHelper.ClientClassLibReferences(clientProjectPath, true);

            SharedCodeServiceParameters parameters = new SharedCodeServiceParameters()
            {
                SharedSourceFiles = sourceFiles.ToArray(),
                ClientAssemblies = assemblies.ToArray(),
                ClientAssemblyPathsNormalized = CodeGenHelper.GetClientAssemblyPaths()
            };

            SharedCodeService sts = new SharedCodeService(parameters, logger);
            return sts;
        }

        /// <summary>
        /// Generate a temporary folder for generating code
        /// </summary>
        /// <returns></returns>
        public static string GenerateTempFolder()
        {
            string rootPath = Path.GetTempPath();
            string tempFolder = Path.Combine(rootPath, Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempFolder);
            return tempFolder;
        }

        /// <summary>
        /// Delete the temporary folder provided by GenerateTempFolder
        /// </summary>
        /// <param name="tempFolder"></param>
        public static void DeleteTempFolder(string tempFolder)
        {
            try
            {
                if (tempFolder.StartsWith(Path.GetTempPath()))
                {
                    RecursiveDelete(tempFolder);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                System.Diagnostics.Debug.WriteLine("Failed to delete temp folder: " + tempFolder);
            }
        }

        /// <summary>
        /// Deletes all the files and folders created by the given CreteRiaClientFilesTask
        /// </summary>
        /// <param name="task"></param>
        public static void DeleteTempFolder(CreateOpenRiaClientFilesTask task)
        {
            if (task != null)
            {
                string tempFolder = Path.GetDirectoryName(task.OutputPath);
                DeleteTempFolder(tempFolder);
            }
        }

        /// <summary>
        /// Deletes the given folder and everything inside it
        /// </summary>
        /// <param name="dir"></param>
        public static void RecursiveDelete(string dir)
        {
            if (!System.IO.Directory.Exists(dir))
            {
                return;
            }
            //get all the subdirectories in the given directory
            string[] dirs = Directory.GetDirectories(dir);
            for (int i = 0; i < dirs.Length; i++)
            {
                RecursiveDelete(dirs[i]);
            }
            string[] files = Directory.GetFiles(dir);

            foreach (string file in files)
            {
                FileInfo fInfo = new FileInfo(file);
                fInfo.Attributes &= ~(FileAttributes.ReadOnly);
                File.Delete(file);
            }

            Directory.Delete(dir);
        }

        /// <summary>
        /// Creates a new CreateOpenRiaClientFilesTask instance to use to generate code
        /// </summary>
        /// <param name="relativeTestDir"></param>
        /// <param name="includeClientOutputAssembly">if <c>true</c> include clients own output assembly in analysis</param>
        /// <returns>A new task instance that can be invoked to do code gen</returns>
        public static CreateOpenRiaClientFilesTask CreateOpenRiaClientFilesTaskInstance(string relativeTestDir, bool includeClientOutputAssembly)
        {
            string deploymentDir = Path.GetDirectoryName(typeof(CodeGenHelper).Assembly.Location);
            string projectPath = null;
            string outputPath = null;
            TestHelper.GetProjectPaths(relativeTestDir, out projectPath, out outputPath);

            Assert.IsTrue(File.Exists(projectPath), "Could not locate " + projectPath + " necessary for test.");
            string serverProjectPath = CodeGenHelper.ServerClassLibProjectPath(projectPath);
            string clientProjectPath = CodeGenHelper.ClientClassLibProjectPath(projectPath);

            return CodeGenHelper.CreateOpenRiaClientFilesTaskInstance(serverProjectPath, clientProjectPath, includeClientOutputAssembly);
        }

        /// <summary>
        /// Creates a new CreateOpenRiaClientFilesTask instance to use to generate code
        /// </summary>
        /// <param name="serverProjectPath">The file path to the ASP.NET server project</param>
        /// <param name="clientProjectPath">The file path to the Silverlight client project</param>
        /// <param name="includeClientOutputAssembly">if <c>true</c> include client's own output assembly in analysis</param>
        /// <returns>A new task instance that can be invoked to do code gen</returns>
        public static CreateOpenRiaClientFilesTask CreateOpenRiaClientFilesTaskInstance(string serverProjectPath, string clientProjectPath, bool includeClientOutputAssembly, string[] hardCodedServerAsm = null, string[] hardCodedClientAsm = null)
        {
            CreateOpenRiaClientFilesTask task = new CreateOpenRiaClientFilesTask();

            MockBuildEngine mockBuildEngine = new MockBuildEngine();
            task.BuildEngine = mockBuildEngine;

            task.Language = "C#";

            task.ServerProjectPath = serverProjectPath;
            task.ServerAssemblies = new TaskItem[] { new TaskItem(CodeGenHelper.ServerClassLibOutputAssembly(task.ServerProjectPath)) };
            task.ServerReferenceAssemblies = MsBuildHelper.AsTaskItems(CodeGenHelper.ServerClassLibReferences(task.ServerProjectPath, hardCodedServerAsm)).ToArray();

            task.ClientProjectPath = clientProjectPath;
            task.ClientReferenceAssemblies = MsBuildHelper.AsTaskItems(CodeGenHelper.ClientClassLibReferences(clientProjectPath, includeClientOutputAssembly, hardCodedClientAsm)).ToArray();
            task.ClientSourceFiles = MsBuildHelper.AsTaskItems(CodeGenHelper.ClientClassLibSourceFiles(clientProjectPath)).ToArray();
            task.ClientFrameworkPath = CodeGenHelper.GetClientRuntimeDirectory();

            // Generate the code to our deployment directory
            string tempFolder = CodeGenHelper.GenerateTempFolder();
            task.OutputPath = Path.Combine(tempFolder, "FileWrites");
            task.GeneratedCodePath = Path.Combine(tempFolder, "Generated_Code");

            return task;
        }

        /// <summary>
        /// Creates a new CreateOpenRiaClientFilesTask instance to use to generate code
        /// </summary>
        /// <param name="relativeTestDir"></param>
        /// <param name="includeClientOutputAssembly">if <c>true</c> include clients own output assembly in analysis</param>
        /// <returns>A new task instance that can be invoked to do code gen</returns>
        public static CreateOpenRiaClientFilesTask CreateOpenRiaClientFilesTaskInstance_CopyClientProjectToOutput(string relativeTestDir, bool includeClientOutputAssembly)
        {
            string deploymentDir = Path.GetDirectoryName(typeof(CodeGenHelper).Assembly.Location);
            string projectPath = null;
            string outputPath = null;
            TestHelper.GetProjectPaths(relativeTestDir, out projectPath, out outputPath);

            Assert.IsTrue(File.Exists(projectPath), "Could not locate " + projectPath + " necessary for test.");
            string serverProjectPath = CodeGenHelper.ServerClassLibProjectPath(projectPath);
            string clientProjectPath = CodeGenHelper.ClientClassLibProjectPath(projectPath);

            return CodeGenHelper.CreateOpenRiaClientFilesTaskInstance_CopyClientProjectToOutput(serverProjectPath, clientProjectPath, includeClientOutputAssembly);
        }

        /// <summary>
        /// Creates a new CreateOpenRiaClientFilesTask instance to use to generate code
        /// </summary>
        /// <param name="relativeTestDir"></param>
        /// <param name="includeClientOutputAssembly">if <c>true</c> include clients own output assembly in analysis</param>
        /// <returns>A new task instance that can be invoked to do code gen</returns>
        public static CreateOpenRiaClientFilesTask CreateOpenRiaClientFilesTaskInstance_CopyClientProjectToOutput(string serverProjectPath, string clientProjectPath, bool includeClientOutputAssembly)
        {
            CreateOpenRiaClientFilesTask task = new CreateOpenRiaClientFilesTask();

            MockBuildEngine mockBuildEngine = new MockBuildEngine();
            task.BuildEngine = mockBuildEngine;

            task.Language = "C#";

            task.ServerProjectPath = serverProjectPath;
            task.ServerAssemblies = new TaskItem[] { new TaskItem(CodeGenHelper.ServerClassLibOutputAssembly(task.ServerProjectPath)) };
            task.ServerReferenceAssemblies = MsBuildHelper.AsTaskItems(CodeGenHelper.ServerClassLibReferences(task.ServerProjectPath)).ToArray();
            task.ClientFrameworkPath = CodeGenHelper.GetClientRuntimeDirectory();

            // Generate the code to our deployment directory
            string tempFolder = CodeGenHelper.GenerateTempFolder();
            task.OutputPath = Path.Combine(tempFolder, "FileWrites");
            task.GeneratedCodePath = Path.Combine(tempFolder, "Generated_Code");

            string clientProjectFileName = Path.GetFileName(clientProjectPath);
            string clientProjectDestPath = Path.Combine(tempFolder, clientProjectFileName);
            File.Copy(clientProjectPath, clientProjectDestPath);
            task.ClientProjectPath = clientProjectDestPath;
            task.ClientReferenceAssemblies = MsBuildHelper.AsTaskItems(CodeGenHelper.ClientClassLibReferences(clientProjectPath, includeClientOutputAssembly)).ToArray();
            task.ClientSourceFiles = MsBuildHelper.AsTaskItems(CodeGenHelper.ClientClassLibSourceFiles(clientProjectPath)).ToArray();

            return task;
        }

        /// <summary>
        /// Creates a new CreateOpenRiaClientFilesTask instance to use to generate code
        /// using the TestWap project.
        /// </summary>
        /// <param name="relativeTestDir"></param>
        /// <returns>A new task instance that can be invoked to do code gen</returns>
        public static CreateOpenRiaClientFilesTask CreateOpenRiaClientFilesTaskInstanceForWAP(string relativeTestDir)
        {
            string deploymentDir = Path.GetDirectoryName(typeof(CodeGenHelper).Assembly.Location);
            string projectPath = null;
            string outputPath = null;
            TestHelper.GetProjectPaths(relativeTestDir, out projectPath, out outputPath);

            Assert.IsTrue(File.Exists(projectPath), "Could not locate " + projectPath + " necessary for test.");
            string serverProjectPath = CodeGenHelper.ServerWapProjectPath(projectPath);
            string clientProjectPath = CodeGenHelper.ClientClassLibProjectPath(projectPath);

            return CodeGenHelper.CreateOpenRiaClientFilesTaskInstance(serverProjectPath, clientProjectPath, false);
        }

        /// <summary>
        /// Creates a new <see cref="ValidateDomainServicesTask"/> instance
        /// </summary>
        /// <param name="relativeTestDir">The relative output directory of the test</param>
        /// <returns>A new <see cref="ValidateDomainServicesTask"/> instance</returns>
        public static ValidateDomainServicesTask CreateValidateDomainServicesTask(string relativeTestDir)
        {
            string deploymentDir = Path.GetDirectoryName(typeof(CodeGenHelper).Assembly.Location);
            string projectPath = null;
            string outputPath = null;
            TestHelper.GetProjectPaths(relativeTestDir, out projectPath, out outputPath);

            Assert.IsTrue(File.Exists(projectPath), "Could not locate " + projectPath + " necessary for test.");
            string serverProjectPath = CodeGenHelper.ServerClassLibProjectPath(projectPath);

            ValidateDomainServicesTask task = new ValidateDomainServicesTask();

            MockBuildEngine mockBuildEngine = new MockBuildEngine();
            task.BuildEngine = mockBuildEngine;

            task.ProjectPath = serverProjectPath;
            task.Assembly = new TaskItem(CodeGenHelper.ServerClassLibOutputAssembly(task.ProjectPath));
            task.ReferenceAssemblies = MsBuildHelper.AsTaskItems(CodeGenHelper.ServerClassLibReferences(task.ProjectPath)).ToArray();

            return task;
        }

        /// <summary>
        /// Gets the full path of the Silverlight runtime framework folder.
        /// </summary>
        /// 
        /// <returns>The Silverlight platform runtime folder or null if it cannot be found.</returns>
        public static string GetClientRuntimeDirectory()
        {
            return Path.GetDirectoryName(typeof(string).Assembly.Location);
        }

        /// <summary>
        /// Previously returned the set of Silverlight runtime and SDK paths
        /// </summary>
        /// <returns>Path to client assemblies (currently net framework assembly path)</returns>
        public static string[] GetClientAssemblyPaths()
        {
            // This returns net framework search directory instead
            return new string[]
            {
                Path.GetDirectoryName(typeof(int).Assembly.Location)
            };
        }

        /// <summary>
        /// Basic success test. Method verifies that domain service compiles.
        /// </summary>
        /// <param name="domainServices">DomainService to compile</param>
        /// <param name="codeContains">strings that this code must contain, can be c>null</c>.</param>
        /// <param name="codeNotContains">strings that this code must not contain, can be <c>null</c>.</param>
        public static void BaseSuccessTest(Type[] domainServices, string[] codeContains, string[] codeNotContains)
        {
            MockSharedCodeService sts = TestHelper.CreateCommonMockSharedCodeService();
            ConsoleLogger logger = new ConsoleLogger();
            string generatedCode = TestHelper.GenerateCodeAssertSuccess("C#", domainServices, logger, sts);

            if (codeContains != null)
            {
                TestHelper.AssertGeneratedCodeContains(generatedCode, codeContains);
            }

            if (codeNotContains != null)
            {
                TestHelper.AssertGeneratedCodeDoesNotContain(generatedCode, codeNotContains);
            }
        }
    }
}
