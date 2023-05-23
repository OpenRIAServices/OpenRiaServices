using System;
using System.Globalization;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenRiaServices.Tools.Test
{
    /// <summary>
    /// Tests for custom build task to generate client proxies
    /// </summary>
    [TestClass]
    public class CleanOpenRiaClientFilesTaskTests
    {
        public CleanOpenRiaClientFilesTaskTests()
        {
        }


        [Description("CleanOpenRiaClientFilesTask deletes ancillary files in OutputPath and code in GeneratedOutputPath")]
        [TestMethod]
        public void CleanRiaClientFiles_Deletes_Generated_Files_Copy()
        {
            CleanRiaClientFiles_Deletes_Generated_Files(OpenRiaSharedFilesMode.Copy);
        }

        [Description("CleanOpenRiaClientFilesTask deletes ancillary files in OutputPath and code in GeneratedOutputPath")]
        [TestMethod]
        public void CleanRiaClientFiles_Deletes_Generated_Files_Link()
        {
            CleanRiaClientFiles_Deletes_Generated_Files(OpenRiaSharedFilesMode.Link);
        }

        public void CleanRiaClientFiles_Deletes_Generated_Files(OpenRiaSharedFilesMode sharedFilesMode)
        {
            CreateOpenRiaClientFilesTask task = null;
            MockBuildEngine mockBuildEngine;
            try
            {
                // ====================================================
                // Test setup -- generate code by calling Create task
                // ====================================================
                task = CodeGenHelper.CreateOpenRiaClientFilesTaskInstance("", /*includeClientOutputAssembly*/ false);
                task.SharedFilesMode = sharedFilesMode.ToString();
                bool success = task.Execute();
                if (!success)
                {
                    mockBuildEngine = task.BuildEngine as MockBuildEngine;
                    Assert.Fail("CreateOpenRiaClientFilesTask failed:\r\n" + mockBuildEngine.ConsoleLogger.Errors);
                }

                string generatedCodeOutputFolder = task.GeneratedCodePath;
                Assert.IsTrue(Directory.Exists(generatedCodeOutputFolder), "Expected task to have created " + generatedCodeOutputFolder);

                string[] files = Directory.GetFiles(generatedCodeOutputFolder);

                if (sharedFilesMode == OpenRiaSharedFilesMode.Copy)
                {
                    Assert.AreEqual(3, files.Length, "Code gen should have generated 3 code files");

                    string copiedFile = Path.Combine(generatedCodeOutputFolder, "TestEntity.shared.cs");
                    Assert.IsTrue(File.Exists(copiedFile), "Expected task to have copied " + copiedFile);

                    copiedFile = Path.Combine(generatedCodeOutputFolder, "TestComplexType.shared.cs");
                    Assert.IsTrue(File.Exists(copiedFile), "Expected task to have copied " + copiedFile);
                }
                else
                {
                    Assert.AreEqual(1, files.Length, "Code gen should have generated 1 code files");
                }

                string generatedFile = Path.Combine(generatedCodeOutputFolder, "ServerClassLib.g.cs");
                Assert.IsTrue(File.Exists(generatedFile), "Expected task to have generated " + generatedFile);

                string outputFolder = task.OutputPath;
                Assert.IsTrue(Directory.Exists(outputFolder), "Expected task to have created " + outputFolder);

                files = Directory.GetFiles(outputFolder);
                string generatedFiles = string.Empty;
                foreach (string file in files)
                    generatedFiles += (file + Environment.NewLine);

                Assert.AreEqual(5, files.Length, "Code gen should have generated this many ancillary files but instead saw:" + Environment.NewLine + generatedFiles);

                string fileList = Path.Combine(outputFolder, "ClientClassLib.OpenRiaFiles.txt");
                Assert.IsTrue(File.Exists(fileList), "Expected code gen to have created " + fileList + " but saw:" +
                    Environment.NewLine + generatedFiles);

                string refList = Path.Combine(outputFolder, "ClientClassLib.OpenRiaClientRefs.txt");
                Assert.IsTrue(File.Exists(refList), "Expected code gen to have created " + refList + " but saw:" +
                    Environment.NewLine + generatedFiles);

                refList = Path.Combine(outputFolder, "ClientClassLib.OpenRiaServerRefs.txt");
                Assert.IsTrue(File.Exists(refList), "Expected code gen to have created " + refList + " but saw:" +
                    Environment.NewLine + generatedFiles);

                string sourceFileList = Path.Combine(outputFolder, "ClientClassLib.OpenRiaSourceFiles.txt");
                Assert.IsTrue(File.Exists(sourceFileList), "Expected code gen to have created " + sourceFileList + " but saw:" +
                    Environment.NewLine + generatedFiles);

                string riaLinkList = Path.Combine(outputFolder, "ClientClassLib.OpenRiaLinks.txt");
                Assert.IsTrue(File.Exists(riaLinkList), "Expected code gen to have created " + riaLinkList + " but saw:" +
                    Environment.NewLine + generatedFiles);

                // ==========================================
                // Main body of test -- the Clean
                // ==========================================

                // Step 1: instantiate Clean task instance and execute it, giving it same info as the Create task
                CleanOpenRiaClientFilesTask cleanTask = new CleanOpenRiaClientFilesTask();
                mockBuildEngine = new MockBuildEngine();
                cleanTask.BuildEngine = mockBuildEngine;
                cleanTask.OutputPath = task.OutputPath;
                cleanTask.GeneratedCodePath = task.GeneratedCodePath;
                cleanTask.ClientProjectPath = task.ClientProjectPath;
                success = cleanTask.Execute();
                Assert.IsTrue(success, "Clean task returned false");

                // No errors or warnings allowed
                TestHelper.AssertNoErrorsOrWarnings(mockBuildEngine.ConsoleLogger);

                // Step 2: validate files created above are gone
                // TODO, 244509: we no longer remove empty folder
                // Assert.IsFalse(Directory.Exists(generatedCodeOutputFolder), "Expected clean to have deleted " + generatedCodeOutputFolder);
                Assert.IsFalse(File.Exists(fileList), "Expected clean to have deleted " + fileList);
                Assert.IsFalse(File.Exists(refList), "Expected clean to have deleted " + refList);
                Assert.IsFalse(File.Exists(sourceFileList), "Expected clean to have deleted " + sourceFileList);
                Assert.IsFalse(File.Exists(riaLinkList), "Expected clean to have deleted " + riaLinkList);

                // Step 3: verify redundant clean does no harm and succeeds
                success = cleanTask.Execute();
                Assert.IsTrue(success, "Clean task returned false");
            }
            finally
            {
                CodeGenHelper.DeleteTempFolder(task);
            }
        }

        [Description("CleanOpenRiaClientFilesTask.SafeFileDelete catches expected exceptions")]
        [TestMethod]
        public void CleanRiaClientFiles_Safe_File_Delete()
        {
            CleanOpenRiaClientFilesTask task = new CleanOpenRiaClientFilesTask();
            MockBuildEngine mockBuildEngine = new MockBuildEngine();
            task.BuildEngine = mockBuildEngine;

            // Test 1 -- null and empty deletes do nothing
            task.SafeFileDelete(null);
            TestHelper.AssertNoErrorsOrWarnings(mockBuildEngine.ConsoleLogger);

            task.SafeFileDelete(string.Empty);
            TestHelper.AssertNoErrorsOrWarnings(mockBuildEngine.ConsoleLogger);

            // Test 2 -- nonexistant file does nothing
            string fileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Assert.IsFalse(File.Exists(fileName));
            task.SafeFileDelete(fileName);
            TestHelper.AssertNoErrorsOrWarnings(mockBuildEngine.ConsoleLogger);

            // Test 3 -- verify delete on actual file succeeds without error
            File.WriteAllText(fileName, "stuff");
            Assert.IsTrue(File.Exists(fileName));
            task.SafeFileDelete(fileName);
            Assert.IsFalse(File.Exists(fileName));
            TestHelper.AssertNoErrorsOrWarnings(mockBuildEngine.ConsoleLogger);

            // Test 4 -- verify delete on actual file with READONLY attribute set succeeds without error
            File.WriteAllText(fileName, "stuff");
            File.SetAttributes(fileName, FileAttributes.ReadOnly);
            Assert.IsTrue(File.Exists(fileName));
            task.SafeFileDelete(fileName);
            Assert.IsFalse(File.Exists(fileName));
            TestHelper.AssertNoErrorsOrWarnings(mockBuildEngine.ConsoleLogger);

            // Test 5 -- attempt to delete while file is open.
            // Verify we log a warning containing the exception's message
            File.WriteAllText(fileName, "stuff");
            Assert.IsTrue(File.Exists(fileName));
            string errorMessage = null;
            using (StreamReader t1 = new StreamReader(fileName))
            {
                // We do a delete here to capture the exception we expect the SafeFileDelete to encounter
                try
                {
                    File.Delete(fileName);
                }
                catch (IOException ioe)
                {
                    errorMessage = ioe.Message;
                }
                Assert.IsNotNull(errorMessage, "Expected File.Delete to throw IOException");
                task.SafeFileDelete(fileName);
            }
            Assert.IsTrue(File.Exists(fileName));
            File.Delete(fileName);
            string expectedWarning = string.Format(CultureInfo.CurrentCulture, Resource.Failed_To_Delete_File_Error, fileName, errorMessage);
            TestHelper.AssertContainsWarnings(mockBuildEngine.ConsoleLogger, expectedWarning);
        }
    }
}
