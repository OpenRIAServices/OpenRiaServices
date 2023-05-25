using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenRiaServices.Tools.Test
{
    /// <summary>
    /// Tests for custom build task to generate client proxies
    /// </summary>
    [TestClass]
    public class CreateOpenRiaClientFilesTaskTests
    {
        // Expected shared and linked files from ServerClassLib/ServerClassLib2
        private static readonly string[] expectedServerNamedSharedFiles = new string[] {
                        "TestEntity.shared.cs",
                        "TestComplexType.shared.cs",};

        private static readonly string[] expectedServerLinkedFiles = new string[] {
                        "TestEntity.linked.cs",
                        "TestComplexType.linked.cs",
                        "TestValidator.linked.cs",
                        "SharedClass.cs",
                        "CodelessType.linked.cs",
                        "CodelessTypeNoClientCompile.linked.cs"};

        // comes from ServerClassLib
        private static readonly string[] expectedServerSharedFiles = expectedServerNamedSharedFiles.Concat(expectedServerLinkedFiles).ToArray();

        // comes from p2p ref to ServerClassLib2
        private static readonly string[] expectedServer2NamedSharedFiles = new string[] {
                        "ServerClassLib2.shared.cs" };

        // comes from ClientClassLib
        private static readonly string[] expectedClientLinkedFiles = new string[] {
                        "TestEntity.reverse.linked.cs"};

        public CreateOpenRiaClientFilesTaskTests()
        {
        }

        [Description("CreateOpenRiaClientFilesTask populates its internal computed properties correctly")]
        [TestMethod]
        public void CreateRiaClientFiles_Validate_Internal_Computed_Task_Properties()
        {
            CreateOpenRiaClientFilesTask task = null;

            try
            {
                task = CodeGenHelper.CreateOpenRiaClientFilesTaskInstance("", /*includeClientOutputAssembly*/ false);

                // ServerProjectRootNamespace
                string serverRootNamespace = task.ServerProjectRootNameSpace;
                Assert.AreEqual("ServerClassLib", serverRootNamespace, "ServerRootNamespace was not calculated correctly");

                // ServerOutputPath
                string serverOutputPath = task.ServerOutputPath;
                Assert.IsFalse(string.IsNullOrEmpty(serverOutputPath), "Empty server output path");

                // The ServerOutputPath always resolves to Debug since no configuration is set
#if !DEBUG
                serverOutputPath = serverOutputPath.Replace("Debug", "Release");
#endif

                Assert.IsTrue(Directory.Exists(serverOutputPath), "Server output path should exist");
                string dllPath = Path.Combine(serverOutputPath, "ServerClassLib.dll");
                Assert.IsTrue(File.Exists(dllPath), "Should have found ServerClassLib.dll at " + dllPath);

                // IsServerProjectAvailable
                Assert.IsTrue(task.IsServerProjectAvailable, "Server project should have been available");
            }
            finally
            {
                CodeGenHelper.DeleteTempFolder(task);
            }
        }

        [Description("CreateOpenRiaClientFilesTask should issue warning if no server assembly was specified")]
        [TestMethod]
        public void CreateRiaClientFiles_Warn_No_Assembly_Specified()
        {
            CreateOpenRiaClientFilesTask task = null;

            try
            {
                task = CodeGenHelper.CreateOpenRiaClientFilesTaskInstance("", /*includeClientOutputAssembly*/ false);

                task.ServerAssemblies = Array.Empty<TaskItem>();
                task.ServerReferenceAssemblies = Array.Empty<TaskItem>();
                task.GenerateClientProxies();

                ITaskItem[] generatedFiles = task.OutputFiles.ToArray();
                Assert.IsNotNull(generatedFiles);
                Assert.AreEqual(0, generatedFiles.Length);

                string expectedWarning = string.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_No_Input_Assemblies, Path.GetFileName(task.ServerProjectPath));
                MockBuildEngine mockBuildEngine = task.BuildEngine as MockBuildEngine;
                TestHelper.AssertContainsWarnings(mockBuildEngine.ConsoleLogger, expectedWarning);
            }
            finally
            {
                CodeGenHelper.DeleteTempFolder(task);
            }
        }

        [Description("CreateOpenRiaClientFilesTask should issue warning if server assembly does not exist")]
        [TestMethod]
        public void CreateRiaClientFiles_Warn_No_Assembly_Exists()
        {
            CreateOpenRiaClientFilesTask task = null;

            try
            {
                task = CodeGenHelper.CreateOpenRiaClientFilesTaskInstance("", /*includeClientOutputAssembly*/ false);
                task.ServerAssemblies = MsBuildHelper.AsTaskItems(new string[] { "NotExist.dll" }).ToArray();
                task.ServerReferenceAssemblies = Array.Empty<TaskItem>();

                task.GenerateClientProxies();

                ITaskItem[] generatedFiles = task.OutputFiles.ToArray();
                Assert.IsNotNull(generatedFiles);
                Assert.AreEqual(0, generatedFiles.Length);

                string expectedWarning = string.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_No_Input_Assemblies, Path.GetFileName(task.ServerProjectPath));
                MockBuildEngine mockBuildEngine = task.BuildEngine as MockBuildEngine;
                TestHelper.AssertContainsWarnings(mockBuildEngine.ConsoleLogger, expectedWarning);
            }
            finally
            {
                CodeGenHelper.DeleteTempFolder(task);
            }
        }


        [Description("CreateOpenRiaClientFilesTask.SafeFolderCreate catches expected exceptions")]
        [TestMethod]
        public void CreateRiaClientFiles_Safe_Folder_Create()
        {
            CleanOpenRiaClientFilesTask task = new CleanOpenRiaClientFilesTask();
            MockBuildEngine mockBuildEngine = new MockBuildEngine();
            task.BuildEngine = mockBuildEngine;

            string fakeFolder = @"FAKE:\notAFolder";
            string realMessage = null;
            try
            {
                Directory.CreateDirectory(fakeFolder);
            }
            catch (Exception e)
            {
                realMessage = e.Message;
            }

            Assert.IsNotNull(realMessage, "Expected creation of fake folder " + fakeFolder + " to fail.");

            bool success = task.SafeFolderCreate(fakeFolder);
            Assert.IsFalse(success, "Expected SafeFolderCreate to report failure for " + fakeFolder);
            Assert.IsFalse(Directory.Exists(fakeFolder), "Did not expect SafeFolderCreate to really create " + fakeFolder);

            string expectedMessage = string.Format(CultureInfo.CurrentCulture, Resource.Failed_To_Create_Folder, fakeFolder, realMessage);
            TestHelper.AssertContainsErrors(mockBuildEngine.ConsoleLogger, expectedMessage);

            // Clear errors
            mockBuildEngine.ConsoleLogger.Reset();

            string realFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Assert.IsFalse(Directory.Exists(realFolder), "Did not expect temp folder to exist.");

            try
            {
                success = task.SafeFolderCreate(realFolder);
                Assert.IsTrue(success, "Expected SafeFolderCreate to have reported success for " + realFolder);
                Assert.IsTrue(Directory.Exists(realFolder), "Expected SafeFolderCreate to have created " + realFolder);
            }
            finally
            {
                if (Directory.Exists(realFolder))
                {
                    Directory.Delete(realFolder);
                }
            }
        }

        [Description("CreateOpenRiaClientFilesTask issues no warning if ask to write null content to non-existant file")]
        [TestMethod]
        public void CreateRiaClientFiles_No_Warning_Write_Empty_Content()
        {
            CreateOpenRiaClientFilesTask task = null;

            try
            {
                task = CodeGenHelper.CreateOpenRiaClientFilesTaskInstance("", false);

                // Create place to write file that should not exist
                string outputFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Assert.IsFalse(File.Exists(outputFile));

                bool result = task.WriteOrDeleteFileToVS(outputFile, string.Empty, true);

                Assert.IsFalse(result);                     // should have reported false
                Assert.IsFalse(File.Exists(outputFile));    // should not have created file
                MockBuildEngine mockBuildEngine = task.BuildEngine as MockBuildEngine;
                Assert.AreEqual(string.Empty, mockBuildEngine.ConsoleLogger.Warnings, "Expected no warnings but saw: " + mockBuildEngine.ConsoleLogger.Warnings);
                Assert.AreEqual(string.Empty, mockBuildEngine.ConsoleLogger.Errors, "Expected no errors but saw: " + mockBuildEngine.ConsoleLogger.Errors);
            }
            finally
            {
                CodeGenHelper.DeleteTempFolder(task);
            }
        }

        [Description("CreateOpenRiaClientFilesTask logs error if the RIA Link points to non-existent file")]
        [TestMethod]
        public void CreateRiaClientFiles_Bad_RIA_Link()
        {
            CreateOpenRiaClientFilesTask task = null;

            try
            {
                task = CodeGenHelper.CreateOpenRiaClientFilesTaskInstance("", /*includeClientOutputAssembly*/ false);
                MockBuildEngine mockBuildEngine = task.BuildEngine as MockBuildEngine;
                task.ServerProjectPath = task.ServerProjectPath + "bogus";  // tweak RIA Link to point to non existent file

                // Validate a helper method detects this
                Assert.IsFalse(task.IsServerProjectAvailable, "IsServerProjectAvailable should have reported false");

                bool success = task.Execute();

                Assert.IsFalse(success, "CreateOpenRiaClientFilesTask should have failed with bad RIA Link");
                string error = string.Format(CultureInfo.CurrentCulture, Resource.Server_Project_File_Does_Not_Exist, "ClientClassLib.csproj", task.ServerProjectPath);
                TestHelper.AssertContainsErrors(mockBuildEngine.ConsoleLogger, error);
            }
            finally
            {
                CodeGenHelper.DeleteTempFolder(task);
            }
        }



        [Description("CreateOpenRiaClientFilesTask issues warning if no PDB is found")]
        [TestMethod]
        public void CreateRiaClientFiles_Missing_Pdb_Warns()
        {
            CreateOpenRiaClientFilesTask task = null;

            try
            {
                task = CodeGenHelper.CreateOpenRiaClientFilesTaskInstance("", /*includeClientOutputAssembly*/ false);
                MockBuildEngine mockBuildEngine = task.BuildEngine as MockBuildEngine;
                string serverAssemblyFile = task.ServerAssemblies[0].ItemSpec;  // use name we mapped to deployment dir
                string serverPdbFile = Path.ChangeExtension(serverAssemblyFile, "pdb");
                string serverTempFile = Path.ChangeExtension(serverAssemblyFile, "tmp");

                // Move PDB file so it cannot be found
                Assert.IsTrue(File.Exists(serverPdbFile), "Expected to find " + serverPdbFile);
                File.Move(serverPdbFile, serverTempFile);
                Assert.IsFalse(File.Exists(serverPdbFile));

                bool success = task.Execute();

                // Restore the PDB
                File.Move(serverTempFile, serverPdbFile);
                Assert.IsTrue(File.Exists(serverPdbFile));

                if (!success)
                {
                    Assert.Fail("CreateOpenRiaClientFilesTask failed:\r\n" + mockBuildEngine.ConsoleLogger.Errors);
                }

                string error = string.Format(CultureInfo.CurrentCulture, Resource.CodeGen_No_Pdb, serverAssemblyFile);
                TestHelper.AssertContainsWarnings(mockBuildEngine.ConsoleLogger, error);
            }
            finally
            {
                CodeGenHelper.DeleteTempFolder(task);
            }
        }

#if !NETFRAMEWORK
        [TestMethod]
        //[Ignore]
        public void CreateRiaClientFiles_For_Net_70()
        {
            var rootPath = new DirectoryInfo(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName;
            var serverProjectPath = Path.Combine(rootPath,  "ServerClassLib\\ServerClassLib.csproj");
            var clientProjectPath = Path.Combine(rootPath, "ClientClassLib\\ClientClassLib.csproj");
            
            var asmPath = "bin\\Debug\\net6.0";
            string[] serverAsm = GetAssemblies(serverProjectPath, asmPath);
            string[] clientAsm = GetAssemblies(clientProjectPath, asmPath).Where(c => !c.Contains("Client")).ToArray(); // Exclude client asm since they should not being built yet
            var code = CodeGenHelper.CreateOpenRiaClientFilesTaskInstance(serverProjectPath, clientProjectPath, false, serverAsm, clientAsm);

            var task = code.Execute();
            Assert.IsTrue(task);
            Assert.IsTrue(Directory.Exists(code.GeneratedCodePath));
        }

        private static string[] GetAssemblies(string projectPath, string path)
        {
            var asmPath = Path.Combine(Path.GetDirectoryName(projectPath), path);
            var asm = Directory.GetFiles(asmPath, "*.dll");
            return asm;
        }

        [TestMethod]
        [Ignore]
        public void CreateRIA60_TS()
        {
            var clientProjectPath = "C:\\Dev2\\production\\Finance\\Client\\CRM.Finance.Client.Model\\CRM.Finance.Client.Model.csproj";
            var serverProjectPath = "C:\\Dev2\\production\\Finance\\Web\\CRM.Finance.Web.Hosting\\CRM.Finance.Web.Hosting.csproj";
            //var coreProjectPath = "C:\\Dev2\\production\\Finance\\Web\\CRM.Finance.Web.Core\\CRM.Finance.Web.Core.csproj";
            var serverAsm = GetAssemblies(serverProjectPath, "bin\\Debug\\net6.0").ToList();
            string[] clientAsm = GetAssemblies(clientProjectPath, "bin\\Debug\\net6.0-windows");
            var code = CodeGenHelper.CreateOpenRiaClientFilesTaskInstance(serverProjectPath, clientProjectPath, false, hardCodedServerAsm: serverAsm.ToArray());

            var task = code.Execute();
            Assert.IsTrue(task);
            Assert.IsTrue(Directory.Exists(code.GeneratedCodePath));
        }
#endif

#if !NETFRAMEWORK
        [Ignore("Do no work in NET6")]
        [TestCategory("NET6_ERRORS")]
#endif
        [Description("CreateOpenRiaClientFilesTask creates ancillary files in OutputPath and code in GeneratedOutputPath")]
        [TestMethod]
        public void CreateRiaClientFiles_Validate_Generated_Files_Copy()
            => CreateRiaClientFiles_Validate_Generated_Files(OpenRiaSharedFilesMode.Copy);

#if !NETFRAMEWORK
        [Ignore("Do no work in NET6")]
        [TestCategory("NET6_ERRORS")]
#endif
        [Description("CreateOpenRiaClientFilesTask creates ancillary files in OutputPath and code in GeneratedOutputPath")]
        [TestMethod]
        public void CreateRiaClientFiles_Validate_Generated_Files_Link()
            => CreateRiaClientFiles_Validate_Generated_Files(OpenRiaSharedFilesMode.Link);

        public void CreateRiaClientFiles_Validate_Generated_Files(OpenRiaSharedFilesMode sharedFilesMode)
        {
            CreateOpenRiaClientFilesTask task = null;

            try
            {
                task = CodeGenHelper.CreateOpenRiaClientFilesTaskInstance(string.Empty, /*includeClientOutputAssembly*/ false);
                task.SharedFilesMode = sharedFilesMode.ToString();
                bool success = task.Execute();
                if (!success)
                {
                    MockBuildEngine mockBuildEngine = task.BuildEngine as MockBuildEngine;
                    Assert.Fail("CreateOpenRiaClientFilesTask failed:\r\n" + mockBuildEngine.ConsoleLogger.Errors);
                }

                string generatedCodeOutputFolder = task.GeneratedCodePath;
                Assert.IsTrue(Directory.Exists(generatedCodeOutputFolder), "Expected task to have created " + generatedCodeOutputFolder);

                string[] files = Directory.GetFiles(generatedCodeOutputFolder);
                Assert.AreEqual(sharedFilesMode == OpenRiaSharedFilesMode.Copy ? 3 : 1, files.Length, "Code gen should have generated 3 code files");

                string generatedFile = Path.Combine(generatedCodeOutputFolder, "ServerClassLib.g.cs");
                Assert.IsTrue(File.Exists(generatedFile), "Expected task to have generated " + generatedFile);


                foreach (var sharedFile in expectedServerNamedSharedFiles)
                {
                    if (sharedFilesMode == OpenRiaSharedFilesMode.Copy)
                    {
                        string copiedFile = Path.Combine(generatedCodeOutputFolder, sharedFile);
                        Assert.IsTrue(File.Exists(copiedFile), "Expected task to have copied " + copiedFile);
                    }
                    else
                    {
                        CodeGenHelper.GetOutputFile(task.SharedFiles, sharedFile);
                    }
                }

                string outputFolder = task.OutputPath;
                Assert.IsTrue(Directory.Exists(outputFolder), "Expected task to have created " + outputFolder);

                files = Directory.GetFiles(outputFolder);
                string generatedFiles = string.Empty;
                foreach (string file in files)
                    generatedFiles += (file + Environment.NewLine);

                Assert.AreEqual(5, files.Length, "Code gen should have generated this many ancillary files but instead saw:" + Environment.NewLine + generatedFiles);

                // ----------------------------------------------
                // Validate task.GeneratedFiles
                // ----------------------------------------------
                string[] generatedFilesFromTask = task.GeneratedFiles.Select<ITaskItem, string>(i => i.ItemSpec).ToArray();
                Assert.AreEqual(1, generatedFilesFromTask.Length, "Expected one generated file");
                Assert.AreEqual(generatedFile, generatedFilesFromTask[0], "Expected generated file");

                // ----------------------------------------------
                // Validate task.CopiedFiles
                // ----------------------------------------------
                string[] copiedFilesFromTask = task.CopiedFiles.Select<ITaskItem, string>(i => i.ItemSpec).ToArray();
                string mockProjectPath = Path.Combine(generatedCodeOutputFolder, "Mock");
                if (sharedFilesMode == OpenRiaSharedFilesMode.Copy)
                {
                    Assert.AreEqual(expectedServerNamedSharedFiles.Length + expectedServer2NamedSharedFiles.Length, copiedFilesFromTask.Length, "Unexpected number of copied files");
                    TestHelper.AssertContainsAtLeastTheseFiles(copiedFilesFromTask, mockProjectPath, CreateOpenRiaClientFilesTaskTests.expectedServerNamedSharedFiles);
                    mockProjectPath = Path.Combine(Path.Combine(generatedCodeOutputFolder, "ServerClassLib2"), "Mock");
                    TestHelper.AssertContainsAtLeastTheseFiles(copiedFilesFromTask, mockProjectPath, expectedServer2NamedSharedFiles);
                }
                else
                {
                    CollectionAssert.AreEqual(Array.Empty<string>(), copiedFilesFromTask, "No files should have been copied");
                }

                // ---------------------------------------------
                // OpenRiaFiles.txt should have been generated
                // ---------------------------------------------
                string fileList = Path.Combine(outputFolder, "ClientClassLib.OpenRiaFiles.txt");
                Assert.IsTrue(File.Exists(fileList), "Expected code gen to have created " + fileList + " but saw:" +
                    Environment.NewLine + generatedFiles);

                string fileListContents = string.Empty;
                using (StreamReader t1 = new StreamReader(fileList))
                {
                    fileListContents = t1.ReadToEnd();
                }

                // Files list 
                bool shouldCopy = (sharedFilesMode == OpenRiaSharedFilesMode.Copy);
                foreach (var sharedFile in expectedServerNamedSharedFiles)
                    Assert.AreEqual(shouldCopy, fileListContents.Contains(sharedFile), "Checking if OpenRiaFiles.txt contains '{0}' with mode '{1}' actual content is '{2}'", sharedFile, sharedFilesMode, fileListContents);


                // ---------------------------------------------
                // Client and server reference lists should have been generated
                // ---------------------------------------------
                string refList = Path.Combine(outputFolder, "ClientClassLib.OpenRiaClientRefs.txt");
                Assert.IsTrue(File.Exists(refList), "Expected code gen to have created " + refList + " but saw:" +
                    Environment.NewLine + generatedFiles);

                string refListContents = string.Empty;
                using (StreamReader t1 = new StreamReader(refList))
                {
                    refListContents = t1.ReadToEnd();
                }
                Assert.IsTrue(refListContents.Contains("DataAnnotations.dll"), "Expected to see DataAnnotations in client ref list but saw " + refListContents);

                // Repeat for server references
                refList = Path.Combine(outputFolder, "ClientClassLib.OpenRiaServerRefs.txt");
                Assert.IsTrue(File.Exists(refList), "Expected code gen to have created " + refList + " but saw:" +
                    Environment.NewLine + generatedFiles);

                refListContents = string.Empty;
                using (StreamReader t1 = new StreamReader(refList))
                {
                    refListContents = t1.ReadToEnd();
                }
                Assert.IsTrue(refListContents.Contains("DataAnnotations.dll"), "Expected to see DataAnnotations in server ref list but saw " + refListContents);

                // ---------------------------------------------
                // RiaSourceFiles.txt should have been generated
                // ---------------------------------------------
                string sourceFileList = Path.Combine(outputFolder, "ClientClassLib.OpenRiaSourceFiles.txt");
                Assert.IsTrue(File.Exists(sourceFileList), "Expected code gen to have created " + sourceFileList + " but saw:" +
                    Environment.NewLine + generatedFiles);
                string sourceFileListContents = string.Empty;
                using (StreamReader t1 = new StreamReader(sourceFileList))
                {
                    sourceFileListContents = t1.ReadToEnd();
                }
                Assert.IsTrue(sourceFileListContents.Contains(task.ServerProjectPath));
                Assert.IsTrue(sourceFileListContents.Contains("TestEntity.shared.cs"), "Expected file list to have TestEntity.shared.cs but instead had " + fileListContents);
                Assert.IsTrue(sourceFileListContents.Contains("ServerClassLib2.shared.cs"), "Expected file list to have ServerClassLib2.shared.cs but instead had " + fileListContents);

                // ---------------------------------------------
                // RiaLinks.txt should have been generated
                // ---------------------------------------------
                string riaLinkList = Path.Combine(outputFolder, "ClientClassLib.OpenRiaLinks.txt");
                Assert.IsTrue(File.Exists(riaLinkList), "Expected code gen to have created " + riaLinkList + " but saw:" +
                    Environment.NewLine + generatedFiles);
            }
            finally
            {
                CodeGenHelper.DeleteTempFolder(task);
            }
        }

#if !NETFRAMEWORK
        [Ignore("Do no work in NET6")]
        [TestCategory("NET6_ERRORS")]
#endif
        [Description("CreateOpenRiaClientFilesTask can access web.config using ASP.NET AppDomain")]
        [TestMethod]
        public void CreateRiaClientFiles_ASPNET_AppDomain()
        {
            CreateOpenRiaClientFilesTask task = null;

            try
            {
                // This test works as follows:
                // 1. It creates a custom task instance for code gen
                // 2. It identifies the TestWAP.csproj as the server project
                // 3. During code gen, this TestWAP project has a CodeProcessor that
                //    analyzes the AppDomain configuration information to verify it looks
                //    the same as we would expect at runtime.  We use the CodeProcessor
                //    approach because it is called by the code generator while it is
                //    running within a ASP.NET AppDomain
                task = CodeGenHelper.CreateOpenRiaClientFilesTaskInstanceForWAP("");

                bool success = task.Execute();
                if (!success)
                {
                    MockBuildEngine mockBuildEngine = task.BuildEngine as MockBuildEngine;
                    Assert.Fail("CreateOpenRiaClientFilesTask failed:\r\n" + mockBuildEngine.ConsoleLogger.Errors);
                }

                // Validate ServerOutputPath is what we expect -- it is critical for ASP.NET AppDomain
                // logic to be able to locate the right bin folder
                string serverOutputPath = TestHelper.NormalizedFolder(task.ServerOutputPath);
                string serverProjectPath = task.ServerProjectPath;
                string expectedOutputPath = TestHelper.NormalizedFolder(Path.Combine(Path.GetDirectoryName(serverProjectPath), "bin"));
                Assert.AreEqual(expectedOutputPath, serverOutputPath, "ServerOutputPath property is incorrect");

                string generatedCodeOutputFolder = task.GeneratedCodePath;
                Assert.IsTrue(Directory.Exists(generatedCodeOutputFolder), "Expected task to have created " + generatedCodeOutputFolder);

                string[] files = Directory.GetFiles(generatedCodeOutputFolder);
                Assert.AreEqual(1, files.Length, "Code gen should have generated 1 code file");

                string generatedFile = Path.Combine(generatedCodeOutputFolder, "TestWap.g.cs");
                Assert.IsTrue(File.Exists(generatedFile), "Expected task to have generated " + generatedFile);

                string generatedCode = File.ReadAllText(generatedFile);
                Assert.IsFalse(string.IsNullOrEmpty(generatedCode), "Expected generated code");

                // The code processor running the ASP.NET AppDomain should have detected Forms Auth
                Dictionary<string, string> codeProcessorComments = GetCodeProcessorElements(generatedCode);
                Assert.IsTrue(codeProcessorComments.ContainsKey("Authentication"), "Expected TestWAP to have authentication section");
                Assert.AreEqual("forms", codeProcessorComments["Authentication"], "Expected TestWAP to have detected we are using Forms auth");

                // The ASP.NET AppDomain BaseDirectory should be the WAP project's
                Assert.IsTrue(codeProcessorComments.ContainsKey("BaseDirectory"), "TestWAP should have located BaseDirectory in web config");
                string baseDirectory = codeProcessorComments["BaseDirectory"];
                Assert.IsFalse(string.IsNullOrEmpty(baseDirectory), "TestWAP did not obtain BaseDirectory");
                string f1 = TestHelper.NormalizedFolder(baseDirectory);
                string f2 = TestHelper.NormalizedFolder(Path.GetDirectoryName(task.ServerProjectPath));
                Assert.AreEqual(f2, f1, "Expected TestWAP BaseDirectory to match server directory.");
            }
            finally
            {
                CodeGenHelper.DeleteTempFolder(task);
            }
        }


        [Description("CreateOpenRiaClientFilesTask computes the correct set of shared files")]
        [TestMethod]
        public void CreateRiaClientFiles_Validate_Shared_Files_Copy()
            => CreateRiaClientFiles_Validate_Shared_Files(OpenRiaSharedFilesMode.Copy);

        [Description("CreateOpenRiaClientFilesTask computes the correct set of shared files")]
        [TestMethod]
        public void CreateRiaClientFiles_Validate_Shared_Files_Link()
            => CreateRiaClientFiles_Validate_Shared_Files(OpenRiaSharedFilesMode.Link);

        public void CreateRiaClientFiles_Validate_Shared_Files(OpenRiaSharedFilesMode sharedFilesMode)
        {
            string projectPath = null;
            string outputPath = null;
            TestHelper.GetProjectPaths("", out projectPath, out outputPath);

            CreateOpenRiaClientFilesTask task = null;

            try
            {
                task = CodeGenHelper.CreateOpenRiaClientFilesTaskInstance("", /*includeClientOutputAssembly*/ true);
                task.SharedFilesMode = sharedFilesMode.ToString();

                // Note: we do not execute this task because it will fail (due to true passed to helper above)

                // Should have detected the files in the ServerClassLib2 project via P2P references
                string server2ProjectPath = CodeGenHelper.ServerClassLib2ProjectPath(projectPath);

                // -----------------------------------
                // Validate public task.LinkedFiles output is accurate
                // -----------------------------------
                string[] linkedFiles = task.LinkedFiles.ToArray().Select<ITaskItem, string>(i => i.ItemSpec).ToArray();

                TestHelper.AssertContainsAtLeastTheseFiles(linkedFiles, task.ServerProjectPath, expectedServerLinkedFiles);
                TestHelper.AssertContainsAtLeastTheseFiles(linkedFiles, task.ClientProjectPath, expectedClientLinkedFiles);

                // ---------------------------------------------------
                // Validate public task.SharedFiles output is accurate
                // ---------------------------------------------------
                string[] sharedFiles = task.SharedFiles.ToArray().Select<ITaskItem, string>(i => i.ItemSpec).ToArray();
                TestHelper.AssertContainsAtLeastTheseFiles(sharedFiles, task.ServerProjectPath, expectedServerNamedSharedFiles);
                TestHelper.AssertContainsAtLeastTheseFiles(sharedFiles, server2ProjectPath, expectedServer2NamedSharedFiles);
                Assert.AreEqual(expectedServer2NamedSharedFiles.Length + expectedServerNamedSharedFiles.Length, sharedFiles.Length, "Unexpected number of shared files");

                // --------------------------------------------------
                // Validate internal task.GetCommonFiles is accurate
                // --------------------------------------------------
                List<string> commonFiles = new List<string>(task.GetSharedAndLinkedFiles());

                Assert.AreEqual(expectedServerSharedFiles.Length + expectedClientLinkedFiles.Length + expectedServer2NamedSharedFiles.Length, commonFiles.Count);

                // Should have detected both the *.shared.cs as well as ones in the server but linked from the client
                TestHelper.AssertContainsAtLeastTheseFiles(commonFiles, task.ServerProjectPath, expectedServerSharedFiles);

                // Should have detected the reverse linked file living in the client but linked from the server
                TestHelper.AssertContainsAtLeastTheseFiles(commonFiles, task.ClientProjectPath, expectedClientLinkedFiles);

                TestHelper.AssertContainsAtLeastTheseFiles(commonFiles, server2ProjectPath, expectedServer2NamedSharedFiles);

            }
            finally
            {
                CodeGenHelper.DeleteTempFolder(task);
            }
        }


#if !NETFRAMEWORK
        [Ignore("Do no work in NET6")]
        [TestCategory("NET6_ERRORS")]
#endif
        [Description("CreateOpenRiaClientFilesTask does not regen files on second code-gen (OpenRiaSharedFilesMode.Copy)")]
        [TestMethod]
        public void CreateRiaClientFiles_Second_CodeGen_Does_Nothing_Copy()
            => CreateRiaClientFiles_Second_CodeGen_Does_Nothing(OpenRiaSharedFilesMode.Copy);

#if !NETFRAMEWORK
        [Ignore("Do no work in NET6")]
        [TestCategory("NET6_ERRORS")]
#endif
        [Description("CreateOpenRiaClientFilesTask does not regen files on second code-gen (OpenRiaSharedFilesMode.Link)")]
        [TestMethod]
        public void CreateRiaClientFiles_Second_CodeGen_Does_Nothing_Link()
        => CreateRiaClientFiles_Second_CodeGen_Does_Nothing(OpenRiaSharedFilesMode.Link);


        public void CreateRiaClientFiles_Second_CodeGen_Does_Nothing(OpenRiaSharedFilesMode sharedFilesMode)
        {
            CreateOpenRiaClientFilesTask task = null;
            int expectedNumberOfFiles = sharedFilesMode == OpenRiaSharedFilesMode.Copy ? 3 : 1;

            try
            {
                task = CodeGenHelper.CreateOpenRiaClientFilesTaskInstance(string.Empty, /*includeClientOutputAssembly*/ false);
                task.SharedFilesMode = sharedFilesMode.ToString();
                bool success = task.Execute();
                if (!success)
                {
                    MockBuildEngine mockBuildEngine = task.BuildEngine as MockBuildEngine;
                    Assert.Fail("CreateOpenRiaClientFilesTask failed:\r\n" + mockBuildEngine.ConsoleLogger.Errors);
                }

                string generatedCodeOutputFolder = task.GeneratedCodePath;
                Assert.IsTrue(Directory.Exists(generatedCodeOutputFolder), "Expected task to have created " + generatedCodeOutputFolder);

                string[] files = Directory.GetFiles(generatedCodeOutputFolder);
                Assert.AreEqual(expectedNumberOfFiles, files.Length, "Code gen should have generated 3 code files");

                string generatedFile = Path.Combine(generatedCodeOutputFolder, "ServerClassLib.g.cs");
                Assert.IsTrue(File.Exists(generatedFile), "Expected task to have generated " + generatedFile);
                DateTime generatedTimestamp = File.GetLastWriteTime(generatedFile);

                DateTime copiedTimestamp = default;
                DateTime copiedComplexTimestamp = default;

                if (sharedFilesMode == OpenRiaSharedFilesMode.Copy)
                {
                    string copiedFile = Path.Combine(generatedCodeOutputFolder, "TestEntity.shared.cs");
                    Assert.IsTrue(File.Exists(copiedFile), "Expected task to have copied " + copiedFile);
                    copiedTimestamp = File.GetLastWriteTime(copiedFile);

                    string copiedComplexFile = Path.Combine(generatedCodeOutputFolder, "TestComplexType.shared.cs");
                    Assert.IsTrue(File.Exists(copiedComplexFile), "Expected task to have copied " + copiedComplexFile);
                    copiedComplexTimestamp = File.GetLastWriteTime(copiedComplexFile);
                }

                // Should have detected the set of client references changed
                string clientReferenceListPath = task.ClientReferenceListPath();
                Assert.IsTrue(File.Exists(clientReferenceListPath), "Expected file for client references");
                DateTime clientReferenceWriteTime = File.GetLastWriteTime(clientReferenceListPath);

                // Should have detected the set of server references changed
                string serverReferenceListPath = task.ServerReferenceListPath();
                Assert.IsTrue(File.Exists(serverReferenceListPath), "Expected file for server references");
                DateTime serverReferenceWriteTime = File.GetLastWriteTime(serverReferenceListPath);

                // Now -- code gen a 2nd time after a tiny delay to get newer time stamps if write
                System.Threading.Thread.Sleep(50);

                success = task.Execute();
                if (!success)
                {
                    MockBuildEngine mockBuildEngine = task.BuildEngine as MockBuildEngine;
                    Assert.Fail("CreateOpenRiaClientFilesTask failed on 2nd pass:\r\n" + mockBuildEngine.ConsoleLogger.Errors);
                }

                generatedCodeOutputFolder = task.GeneratedCodePath;
                Assert.IsTrue(Directory.Exists(generatedCodeOutputFolder), "Expected task to have created " + generatedCodeOutputFolder);

                files = Directory.GetFiles(generatedCodeOutputFolder);
                Assert.AreEqual(expectedNumberOfFiles, files.Length, "Code gen should have generated 3 code files");

                generatedFile = Path.Combine(generatedCodeOutputFolder, "ServerClassLib.g.cs");
                Assert.IsTrue(File.Exists(generatedFile), "Expected task to have generated " + generatedFile);
                DateTime generatedTimestamp1 = File.GetLastWriteTime(generatedFile);

                if (sharedFilesMode == OpenRiaSharedFilesMode.Copy)
                {
                    string copiedFile = Path.Combine(generatedCodeOutputFolder, "TestEntity.shared.cs");
                    Assert.IsTrue(File.Exists(copiedFile), "Expected task to have copied " + copiedFile);
                    DateTime copiedTimestamp1 = File.GetLastWriteTime(copiedFile);

                    string copiedComplexFile = Path.Combine(generatedCodeOutputFolder, "TestComplexType.shared.cs");
                    Assert.IsTrue(File.Exists(copiedComplexFile), "Expected task to have copied " + copiedComplexFile);
                    DateTime copiedComplexTimestamp1 = File.GetLastWriteTime(copiedComplexFile);

                    Assert.AreEqual(copiedTimestamp, copiedTimestamp1, "Did not expect 2nd code gen to recopy TestEntity.shared.cs");
                    Assert.AreEqual(copiedComplexTimestamp, copiedComplexTimestamp1, "Did not expect 2nd code gen to recopy TestComplexType.shared.cs");
                }

                Assert.AreEqual(generatedTimestamp, generatedTimestamp1, "Did not expect 2nd code gen to regen ServerClassLib.g.cs");

                // Should not have updated the client or server references
                Assert.IsTrue(File.Exists(serverReferenceListPath), "Expected file for server references");
                Assert.AreEqual(serverReferenceWriteTime, File.GetLastWriteTime(serverReferenceListPath), "Did not expect server references to be written on 2nd build");

                Assert.IsTrue(File.Exists(clientReferenceListPath), "Expected file for client references");
                Assert.AreEqual(clientReferenceWriteTime, File.GetLastWriteTime(clientReferenceListPath), "Did not expect client references to be written on 2nd build");

            }
            finally
            {
                CodeGenHelper.DeleteTempFolder(task);
            }
        }

#if !NETFRAMEWORK
        [Ignore("Do no work in NET6")]
        [TestCategory("NET6_ERRORS")]
#endif
        [Description("CreateOpenRiaClientFilesTask generates breadcrumb files with relative paths, and does nothing on second build")]
        [TestMethod]
        public void CreateRiaClientFiles_CopyClientProject_Copy()
            => CreateRiaClientFiles_CopyClientProject(OpenRiaSharedFilesMode.Copy);

#if !NETFRAMEWORK
        [Ignore("Do no work in NET6")]
        [TestCategory("NET6_ERRORS")]
#endif
        [Description("CreateOpenRiaClientFilesTask generates breadcrumb files with relative paths, and does nothing on second build")]
        [TestMethod]
        public void CreateRiaClientFiles_CopyClientProject_Link()
            => CreateRiaClientFiles_CopyClientProject(OpenRiaSharedFilesMode.Link);

        public void CreateRiaClientFiles_CopyClientProject(OpenRiaSharedFilesMode sharedFilesMode)
        {
            CreateOpenRiaClientFilesTask task = null;
            int expectedNumberOfFiles = sharedFilesMode == OpenRiaSharedFilesMode.Copy ? 3 : 1;

            try
            {
                task = CodeGenHelper.CreateOpenRiaClientFilesTaskInstance_CopyClientProjectToOutput("", /*includeClientOutputAssembly*/ false);
                task.SharedFilesMode = sharedFilesMode.ToString();
                bool success = task.Execute();
                if (!success)
                {
                    MockBuildEngine mockBuildEngine = task.BuildEngine as MockBuildEngine;
                    Assert.Fail("CreateOpenRiaClientFilesTask failed:\r\n" + mockBuildEngine.ConsoleLogger.Errors);
                }

                string generatedCodeOutputFolder = task.GeneratedCodePath;
                Assert.IsTrue(Directory.Exists(generatedCodeOutputFolder), "Expected task to have created " + generatedCodeOutputFolder);

                string[] files = Directory.GetFiles(generatedCodeOutputFolder);
                Assert.AreEqual(expectedNumberOfFiles, files.Length, "Code gen should have generated 3 code files");

                string generatedFile = Path.Combine(generatedCodeOutputFolder, "ServerClassLib.g.cs");
                Assert.IsTrue(File.Exists(generatedFile), "Expected task to have generated " + generatedFile);
                DateTime generatedTimestamp = File.GetLastWriteTime(generatedFile);

                DateTime copiedTimestamp = default;
                DateTime copiedComplexTimestamp = default;
                if (sharedFilesMode == OpenRiaSharedFilesMode.Copy)
                {
                    string copiedFile = Path.Combine(generatedCodeOutputFolder, "TestEntity.shared.cs");
                    Assert.IsTrue(File.Exists(copiedFile), "Expected task to have copied " + copiedFile);
                    copiedTimestamp = File.GetLastWriteTime(copiedFile);

                    string copiedComplexFile = Path.Combine(generatedCodeOutputFolder, "TestComplexType.shared.cs");
                    Assert.IsTrue(File.Exists(copiedComplexFile), "Expected task to have copied " + copiedComplexFile);
                    copiedComplexTimestamp = File.GetLastWriteTime(copiedComplexFile);
                }

                // Should have detected the set of client references changed
                string clientReferenceListPath = task.ClientReferenceListPath();
                Assert.IsTrue(File.Exists(clientReferenceListPath), "Expected file for client references");
                DateTime clientReferenceWriteTime = File.GetLastWriteTime(clientReferenceListPath);

                // Should have detected the set of server references changed
                string serverReferenceListPath = task.ServerReferenceListPath();
                Assert.IsTrue(File.Exists(serverReferenceListPath), "Expected file for server references");
                DateTime serverReferenceWriteTime = File.GetLastWriteTime(serverReferenceListPath);

                string riaFilesListPath = task.FileListPath();
                Assert.IsTrue(File.Exists(riaFilesListPath), "Expected file for generated files");
                string[] contents = File.ReadAllLines(riaFilesListPath);
                for (int i = 1; i < contents.Length; i++)
                {
                    Assert.IsTrue(!Path.IsPathRooted(contents[i]), "Expect relative path to be stored");
                }

                // Now -- code gen a 2nd time after a tiny delay to get newer time stamps if write
                System.Threading.Thread.Sleep(50);

                success = task.Execute();
                if (!success)
                {
                    MockBuildEngine mockBuildEngine = task.BuildEngine as MockBuildEngine;
                    Assert.Fail("CreateOpenRiaClientFilesTask failed on 2nd pass:\r\n" + mockBuildEngine.ConsoleLogger.Errors);
                }

                generatedCodeOutputFolder = task.GeneratedCodePath;
                Assert.IsTrue(Directory.Exists(generatedCodeOutputFolder), "Expected task to have created " + generatedCodeOutputFolder);

                files = Directory.GetFiles(generatedCodeOutputFolder);
                Assert.AreEqual(expectedNumberOfFiles, files.Length, "Code gen should have generated 3 code files");

                generatedFile = Path.Combine(generatedCodeOutputFolder, "ServerClassLib.g.cs");
                Assert.IsTrue(File.Exists(generatedFile), "Expected task to have generated " + generatedFile);
                DateTime generatedTimestamp1 = File.GetLastWriteTime(generatedFile);

                if (sharedFilesMode == OpenRiaSharedFilesMode.Copy)
                {
                    string copiedFile = Path.Combine(generatedCodeOutputFolder, "TestEntity.shared.cs");
                    Assert.IsTrue(File.Exists(copiedFile), "Expected task to have copied " + copiedFile);
                    DateTime copiedTimestamp1 = File.GetLastWriteTime(copiedFile);

                    string copiedComplexFile = Path.Combine(generatedCodeOutputFolder, "TestComplexType.shared.cs");
                    Assert.IsTrue(File.Exists(copiedComplexFile), "Expected task to have copied " + copiedComplexFile);
                    DateTime copiedComplexTimestamp1 = File.GetLastWriteTime(copiedComplexFile);

                    Assert.AreEqual(copiedTimestamp, copiedTimestamp1, "Did not expect 2nd code gen to recopy TestEntity.shared.cs");
                    Assert.AreEqual(copiedComplexTimestamp, copiedComplexTimestamp1, "Did not expect 2nd code gen to recopy TestComplexType.shared.cs");
                }

                Assert.AreEqual(generatedTimestamp, generatedTimestamp1, "Did not expect 2nd code gen to regen ServerClassLib.g.cs");

                // Should not have updated the client or server references
                Assert.IsTrue(File.Exists(serverReferenceListPath), "Expected file for server references");
                Assert.AreEqual(serverReferenceWriteTime, File.GetLastWriteTime(serverReferenceListPath), "Did not expect server references to be written on 2nd build");

                Assert.IsTrue(File.Exists(clientReferenceListPath), "Expected file for client references");
                Assert.AreEqual(clientReferenceWriteTime, File.GetLastWriteTime(clientReferenceListPath), "Did not expect client references to be written on 2nd build");

                Assert.IsTrue(File.Exists(riaFilesListPath), "Expected file for generated files");
                contents = File.ReadAllLines(riaFilesListPath);
                for (int i = 1; i < contents.Length; i++)
                {
                    Assert.IsTrue(!Path.IsPathRooted(contents[i]), "Expect relative path to be stored");
                }
            }
            finally
            {
                CodeGenHelper.DeleteTempFolder(task);
            }
        }

#if !NETFRAMEWORK
        [Ignore("Do no work in NET6")]
        [TestCategory("NET6_ERRORS")]
#endif
        [Description("CreateOpenRiaClientFilesTask regenerates code if list of references changes")]
        [TestMethod]
        public void CreateRiaClientFiles_Missing_ReferenceList_Regens_Copy()
            => CreateRiaClientFiles_Missing_ReferenceList_Regens(OpenRiaSharedFilesMode.Copy);

#if !NETFRAMEWORK
        [Ignore("Do no work in NET6")]
        [TestCategory("NET6_ERRORS")]
#endif
        [Description("CreateOpenRiaClientFilesTask regenerates code if list of references changes")]
        [TestMethod]
        public void CreateRiaClientFiles_Missing_ReferenceList_Regens_Link()
            => CreateRiaClientFiles_Missing_ReferenceList_Regens(OpenRiaSharedFilesMode.Link);

        public void CreateRiaClientFiles_Missing_ReferenceList_Regens(OpenRiaSharedFilesMode sharedFilesMode)
        {
            CreateOpenRiaClientFilesTask task = null;
            int expectedNumberOfFiles = sharedFilesMode == OpenRiaSharedFilesMode.Copy ? 3 : 1;

            try
            {
                task = CodeGenHelper.CreateOpenRiaClientFilesTaskInstance("", /*includeClientOutputAssembly*/ false);
                task.SharedFilesMode = sharedFilesMode.ToString();
                bool success = task.Execute();
                if (!success)
                {
                    MockBuildEngine mockBuildEngine = task.BuildEngine as MockBuildEngine;
                    Assert.Fail("CreateOpenRiaClientFilesTask failed:\r\n" + mockBuildEngine.ConsoleLogger.Errors);
                }

                string generatedCodeOutputFolder = task.GeneratedCodePath;
                Assert.IsTrue(Directory.Exists(generatedCodeOutputFolder), "Expected task to have created " + generatedCodeOutputFolder);

                string[] files = Directory.GetFiles(generatedCodeOutputFolder);
                Assert.AreEqual(expectedNumberOfFiles, files.Length, "Code gen should have generated 3 code files");

                string generatedFile = Path.Combine(generatedCodeOutputFolder, "ServerClassLib.g.cs");
                Assert.IsTrue(File.Exists(generatedFile), "Expected task to have generated " + generatedFile);
                DateTime generatedTimestamp = File.GetLastWriteTime(generatedFile);

                if (sharedFilesMode == OpenRiaSharedFilesMode.Copy)
                {
                    string copiedFile = Path.Combine(generatedCodeOutputFolder, "TestEntity.shared.cs");
                    Assert.IsTrue(File.Exists(copiedFile), "Expected task to have copied " + copiedFile);

                    string copiedComplexFile = Path.Combine(generatedCodeOutputFolder, "TestComplexType.shared.cs");
                    Assert.IsTrue(File.Exists(copiedComplexFile), "Expected task to have copied " + copiedComplexFile);
                }

                string outputFolder = task.OutputPath;
                Assert.IsTrue(Directory.Exists(outputFolder), "Expected task to have created " + outputFolder);

                string clientRefList = Path.Combine(outputFolder, "ClientClassLib.OpenRiaClientRefs.txt");
                Assert.IsTrue(File.Exists(clientRefList), "Expected code gen to have created " + clientRefList);

                // Delete our client reference file -- thus it is forced to regen
                File.Delete(clientRefList);
                Assert.IsFalse(File.Exists(clientRefList), "Failed to delete file");

                // Repeat for server references
                string serverRefList = Path.Combine(outputFolder, "ClientClassLib.OpenRiaServerRefs.txt");
                Assert.IsTrue(File.Exists(serverRefList), "Expected code gen to have created " + serverRefList);
                File.Delete(serverRefList);
                Assert.IsFalse(File.Exists(serverRefList), "Failed to delete file");

                // Now -- code gen a 2nd time after a tiny delay to get newer time stamps if write
                System.Threading.Thread.Sleep(50);

                success = task.Execute();
                if (!success)
                {
                    MockBuildEngine mockBuildEngine = task.BuildEngine as MockBuildEngine;
                    Assert.Fail("CreateOpenRiaClientFilesTask failed on 2nd pass:\r\n" + mockBuildEngine.ConsoleLogger.Errors);
                }

                generatedCodeOutputFolder = task.GeneratedCodePath;
                Assert.IsTrue(Directory.Exists(generatedCodeOutputFolder), "Expected task to have created " + generatedCodeOutputFolder);

                files = Directory.GetFiles(generatedCodeOutputFolder);
                Assert.AreEqual(expectedNumberOfFiles, files.Length, "Code gen should have generated 3 code files");

                generatedFile = Path.Combine(generatedCodeOutputFolder, "ServerClassLib.g.cs");
                Assert.IsTrue(File.Exists(generatedFile), "Expected task to have generated " + generatedFile);
                DateTime generatedTimestamp1 = File.GetLastWriteTime(generatedFile);

                Assert.AreNotEqual(generatedTimestamp, generatedTimestamp1, "Expected 2nd code gen to regen ServerClassLib.g.cs");
            }
            finally
            {
                CodeGenHelper.DeleteTempFolder(task);
            }
        }

        [Description("CreateOpenRiaClientFilesTask purges orphan files and folders on subsequent builds")]
        [TestMethod]
        public void CreateRiaClientFiles_Deletes_Orphan_Files()
        {
            CreateOpenRiaClientFilesTask task = null;
            string tempSharedFileFolder = CodeGenHelper.GenerateTempFolder();
            try
            {
                task = CodeGenHelper.CreateOpenRiaClientFilesTaskInstance("", /*includeClientOutputAssembly*/ false);
                task.SharedFilesMode = OpenRiaSharedFilesMode.Copy.ToString();

                MockBuildEngine mockBuildEngine = task.BuildEngine as MockBuildEngine;
                string serverProjectPath = task.ServerProjectPath;

                // +Generated_Code
                //    +OuterFolder
                //       Outer.shared.cs
                //       +InnerFolder
                //          Inner.shared.cs
                string outerSharedFolder = Path.Combine(tempSharedFileFolder, "OuterFolder");
                string innerSharedFolder = Path.Combine(outerSharedFolder, "InnerFolder");
                string outerSharedFile = Path.Combine(outerSharedFolder, "Outer.shared.cs");
                string innerSharedFile = Path.Combine(innerSharedFolder, "Inner.shared.cs");

                Directory.CreateDirectory(outerSharedFolder);
                Directory.CreateDirectory(innerSharedFolder);
                File.WriteAllText(outerSharedFile, "// outer");
                File.WriteAllText(innerSharedFile, "// outer");

                // Zap the task's cache of known source files
                task.ServerProjectSourceFileCache.SourceFilesByProject.Clear();

                // To get our folders created, we need to make them appear to be relative to the server project.
                // So copy the actual project file into the temp folder and redirect to it
                string newServerProjectPath = Path.Combine(tempSharedFileFolder, Path.GetFileName(task.ServerProjectPath));
                File.Copy(task.ServerProjectPath, newServerProjectPath);
                task.ServerProjectPath = newServerProjectPath;
                task.ServerProjectSourceFileCache[newServerProjectPath] = new string[] { outerSharedFile, innerSharedFile };

                //
                // 1st build should generate these files
                //
                bool success = task.Execute();
                if (!success)
                {
                    Assert.Fail("CreateOpenRiaClientFilesTask failed:\r\n" + mockBuildEngine.ConsoleLogger.Errors);
                }

                string generatedCodeOutputFolder = task.GeneratedCodePath;
                Assert.IsTrue(Directory.Exists(generatedCodeOutputFolder), "Expected task to have created " + generatedCodeOutputFolder);

                string generatedOuterFolder = Path.Combine(generatedCodeOutputFolder, "OuterFolder");
                string generatedInnerFolder = Path.Combine(generatedOuterFolder, "InnerFolder");
                string generatedOuterSharedFile = Path.Combine(generatedOuterFolder, "Outer.shared.cs");
                string generatedInnerSharedFile = Path.Combine(generatedInnerFolder, "Inner.shared.cs");

                Assert.IsTrue(Directory.Exists(generatedOuterFolder), "Expected generation of folder " + generatedOuterFolder);
                Assert.IsTrue(Directory.Exists(generatedInnerFolder), "Expected generation of folder " + generatedInnerFolder);

                Assert.IsTrue(File.Exists(generatedOuterSharedFile), "Expected generation of file " + generatedOuterSharedFile);
                Assert.IsTrue(File.Exists(generatedInnerSharedFile), "Expected generation of file " + generatedInnerSharedFile);

                //
                // 2nd build with no change should not alter these
                //
                mockBuildEngine.ConsoleLogger.Reset();
                success = task.Execute();
                if (!success)
                {
                    Assert.Fail("CreateOpenRiaClientFilesTask failed:\r\n" + mockBuildEngine.ConsoleLogger.Errors);
                }
                Assert.IsTrue(Directory.Exists(generatedOuterFolder), "Expected no change for folder " + generatedOuterFolder);
                Assert.IsTrue(Directory.Exists(generatedInnerFolder), "Expected no change for folder " + generatedInnerFolder);

                Assert.IsTrue(File.Exists(generatedOuterSharedFile), "Expected no change for file " + generatedOuterSharedFile);
                Assert.IsTrue(File.Exists(generatedInnerSharedFile), "Expected no change for file " + generatedInnerSharedFile);

                //
                // 3rd build after deleting inner file should remove its file and folder, even though we still name the file
                //
                File.Delete(innerSharedFile);

                mockBuildEngine.ConsoleLogger.Reset();
                success = task.Execute();
                if (!success)
                {
                    Assert.Fail("CreateOpenRiaClientFilesTask failed:\r\n" + mockBuildEngine.ConsoleLogger.Errors);
                }
                Assert.IsTrue(Directory.Exists(generatedOuterFolder), "Expected no change for folder " + generatedOuterFolder);
                // TODO, 244509: we no longer remove empty folder
                // Assert.IsFalse(Directory.Exists(generatedInnerFolder), "Expected deletion of folder " + generatedInnerFolder);

                Assert.IsTrue(File.Exists(generatedOuterSharedFile), "Expected no change for file " + generatedOuterSharedFile);
                Assert.IsFalse(File.Exists(generatedInnerSharedFile), "Expected deletion of file " + generatedInnerSharedFile);

                //
                // 4th build after renaming outer file should delete generated file.
                //
                string renamedFile = Path.Combine(Path.GetDirectoryName(outerSharedFile), "Renamed.shared.cs");
                string generatedRenamedFile = Path.Combine(Path.GetDirectoryName(generatedOuterSharedFile), "Renamed.shared.cs");

                File.Copy(outerSharedFile, renamedFile);
                File.Delete(outerSharedFile);
                task.ServerProjectSourceFileCache[newServerProjectPath] = new string[] { renamedFile };

                mockBuildEngine.ConsoleLogger.Reset();
                success = task.Execute();
                if (!success)
                {
                    Assert.Fail("CreateOpenRiaClientFilesTask failed:\r\n" + mockBuildEngine.ConsoleLogger.Errors);
                }
                Assert.IsTrue(Directory.Exists(generatedOuterFolder), "Expected no change for folder " + generatedOuterFolder);
                Assert.IsFalse(File.Exists(generatedOuterSharedFile), "Expected deletion file " + generatedOuterSharedFile);
                Assert.IsTrue(File.Exists(generatedRenamedFile), "Expected creation of file " + generatedRenamedFile);

                //
                // 5th build -- delete final shared file but prevent deletion of directory by marking readonly.
                // The delete will fail and generate a warning message.
                //
                File.Delete(renamedFile);

                DirectoryInfo dirInfo = new DirectoryInfo(generatedOuterFolder);
                dirInfo.Attributes |= FileAttributes.ReadOnly;

                mockBuildEngine.ConsoleLogger.Reset();
                success = task.Execute();

                dirInfo.Attributes &= ~FileAttributes.ReadOnly;

                if (!success)
                {
                    Assert.Fail("CreateOpenRiaClientFilesTask failed:\r\n" + mockBuildEngine.ConsoleLogger.Errors);
                }
                Assert.IsTrue(Directory.Exists(generatedOuterFolder), "Expected no change for folder " + generatedOuterFolder);
                Assert.IsFalse(File.Exists(generatedRenamedFile), "Expected deletion of file " + generatedRenamedFile);

                // TODO, 244509: we no longer remove empty folder
                // Assert.IsTrue(mockBuildEngine.ConsoleLogger.Warnings.Contains(generatedOuterFolder), "Expected to see warning about open folder but instead saw\r\n" + mockBuildEngine.ConsoleLogger.Warnings);
            }
            finally
            {
                CodeGenHelper.DeleteTempFolder(task);
                CodeGenHelper.DeleteTempFolder(tempSharedFileFolder);
            }
        }

        [Description("CreateOpenRiaClientFilesTask.IsClientApplication property setter & getter tests")]
        [TestMethod]
        public void CreateOpenRiaClientFilesTask_Validate_IsClientApplication_Property()
        {
            // All these strings should be considered 'false' when parsed in this string property
            string[] falseStrings = { Boolean.FalseString, string.Empty, null, "1", "junk" };

            CreateOpenRiaClientFilesTask task = new CreateOpenRiaClientFilesTask();

            // Default is null which translates to 'false'
            Assert.IsNull(task.IsClientApplication, "IsClientApplication should default to null");
            Assert.IsFalse(task.IsClientApplicationAsBool, "IsClientApplicationAsBool should default to false");

            // Setting to "true" is possible and is reflected in bool
            task.IsClientApplication = Boolean.TrueString;
            Assert.AreEqual(Boolean.TrueString, task.IsClientApplication);
            Assert.IsTrue(task.IsClientApplicationAsBool, "IsClientApplicationAsBool should be true when string is Boolean.TrueString");

            // Try a combination of 'false' strings and verify all yield false bool
            foreach (string falseString in falseStrings)
            {
                task.IsClientApplication = falseString;
                Assert.AreEqual(falseString, task.IsClientApplication);
                Assert.IsFalse(task.IsClientApplicationAsBool, "IsClientApplicationAsBool should be false when string is <" + falseString + ">");
            }
        }

        [Description("CreateOpenRiaClientFilesTask.UseFullTypeNames property setter & getter tests")]
        [TestMethod]
        public void CreateOpenRiaClientFilesTask_Validate_UseFullTypeNames_Property()
        {
            // All these strings should be considered 'false' when parsed in this string property
            string[] falseStrings = { Boolean.FalseString, string.Empty, null, "1", "junk" };

            CreateOpenRiaClientFilesTask task = new CreateOpenRiaClientFilesTask();

            // Default is null which translates to 'false'
            Assert.IsNull(task.UseFullTypeNames, "UseFullTypeNames should default to null");
            Assert.IsFalse(task.UseFullTypeNamesAsBool, "UseFullTypeNamesAsBool should default to false");

            // Setting to "true" is possible and is reflected in bool
            task.UseFullTypeNames = Boolean.TrueString;
            Assert.AreEqual(Boolean.TrueString, task.UseFullTypeNames);
            Assert.IsTrue(task.UseFullTypeNamesAsBool, "UseFullTypeNamesAsBool should be true when string is Boolean.TrueString");

            // Try a combination of 'false' strings and verify all yield false bool
            foreach (string falseString in falseStrings)
            {
                task.UseFullTypeNames = falseString;
                Assert.AreEqual(falseString, task.UseFullTypeNames);
                Assert.IsFalse(task.UseFullTypeNamesAsBool, "UseFullTypeNameAsBool should be false when string is <" + falseString + ">");
            }
        }

        [Description("CreateOpenRiaClientFilesTask.GeneratedCodePath property setter & getter tests")]
        [TestMethod]
        public void CreateOpenRiaClientFilesTask_Validate_GeneratedCodePath_Property()
        {
            CreateOpenRiaClientFilesTask task = new CreateOpenRiaClientFilesTask();

            // Create a dummy folder and project file.  They don't have to exist for this test to run
            string clientProjectFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            string clientProjectPath = Path.Combine(clientProjectFolder, "MockProj.csproj");
            task.ClientProjectPath = clientProjectPath;

            // Verify default generated code path is computed
            string path = task.GeneratedCodePath;
            Assert.IsNotNull(path);
            Assert.IsTrue(path.Length > 0);
            Assert.AreEqual(RiaClientFilesTask.GeneratedCodeFolderName, Path.GetFileName(path));
            Assert.IsTrue(Path.IsPathRooted(path), "Generated code path must be full path");
            Assert.AreEqual(Path.GetDirectoryName(task.ClientProjectPath), Path.GetDirectoryName(path), "Generated code path must be relative to project");

            // Verify can set to arbitrary relative path that is converted to full path relative to project
            task.GeneratedCodePath = "foo";
            path = task.GeneratedCodePath;
            Assert.IsNotNull(path);
            Assert.IsTrue(path.Length > 0);
            Assert.AreEqual("foo", Path.GetFileName(path));
            Assert.IsTrue(Path.IsPathRooted(path), "Generated code path must be full path");
            Assert.AreEqual(Path.GetFullPath(path), path, "Generated code path should have been full path");
            Assert.AreEqual(Path.GetDirectoryName(task.ClientProjectPath), Path.GetDirectoryName(path), "Generated code path must be relative to project");

            // Verify can set to full path
            string fullPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            task.GeneratedCodePath = fullPath;
            Assert.AreEqual(fullPath, task.GeneratedCodePath);
            Assert.IsFalse(Directory.Exists(fullPath), "Getter should not create folder");

            try
            {
                // Now, use normal helper to create it if it does not exist
                bool createdFolder = task.SafeFolderCreate(fullPath);
                Assert.IsTrue(createdFolder, "Failed to create folder");
                Assert.IsTrue(Directory.Exists(fullPath), "Generated code path should have been generated");
            }
            finally
            {
                if (Directory.Exists(fullPath))
                {
                    Directory.Delete(fullPath);
                }
            }
        }

        /// <summary>
        /// Extracts the comments injected by the code processor into a dictionary
        /// where the key is the name of the injected item, and the value is the content
        /// </summary>
        /// <param name="generatedCode">The generated code to analyze</param>
        /// <returns>A new dictionary</returns>
        private static Dictionary<string, string> GetCodeProcessorElements(string generatedCode)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            int pos = 0;
            while (pos >= 0)
            {
                pos = generatedCode.IndexOf("[CodeProcessor] ", pos);
                if (pos >= 0)
                {
                    pos += 16;
                    int colonPos = generatedCode.IndexOf(':', pos);
                    string key = generatedCode.Substring(pos, colonPos - pos);
                    int endPos = generatedCode.IndexOf(Environment.NewLine, colonPos);
                    string content = generatedCode.Substring(colonPos + 1, endPos - colonPos - Environment.NewLine.Length + 1);
                    pos = endPos;

                    string existingContent = null;
                    if (result.TryGetValue(key, out existingContent))
                    {
                        content = existingContent + Environment.NewLine + content;
                    }
                    result[key] = content;
                }
            }
            return result;
        }

    }
}
