using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using OpenRiaServices.Client.Test;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenRiaServices.Tools.Test
{

    /// <summary>
    /// Tests for base class for our custom msbuild tasks
    /// </summary>
    [TestClass]
    public class RiaClientFilesTaskTests
    {

        public RiaClientFilesTaskTests()
        {
        }

        [Description("RiaClientFilesTask.NormalizeFolderPath works as expected")]
        [TestMethod]
        public void RiaClientFilesTask_NormalizeFolderPath()
        {
            // Null throws ArgNull
            ExceptionHelper.ExpectArgumentNullExceptionStandard(() => RiaClientFilesTask.NormalizeFolderPath(null), "path");

            // Empty path is permitted and does nothing
            string path = RiaClientFilesTask.NormalizeFolderPath(string.Empty);
            Assert.AreEqual(string.Empty, path);

            // Single character slash is removed.  Note, this could expand to include root drive when we evaluate it.
            path = RiaClientFilesTask.NormalizeFolderPath(@"\");
            Assert.IsFalse(path.EndsWith(@"\"), "Failed to strip slash when only slash was present");

            // Path with slash removes slash
            string path1 = RiaClientFilesTask.NormalizeFolderPath(@"c:\foo\");
            Assert.IsFalse(path1.EndsWith(@"\"), "Failed to strip slash");

            // Path without slash is not changed
            string path2 = RiaClientFilesTask.NormalizeFolderPath(@"c:\foo");
            Assert.AreEqual(@"c:\foo", path2, "Slashless path was altered");

            // Slashed and slashless paths normalize the same
            Assert.AreEqual(path1, path2, "Paths did not normalize properly");
        }

        [Description("RiaClientFilesTask.SafeFileCopy works and catches expected exceptions")]
        [TestMethod]
        public void RiaClientFilesTask_Safe_File_Copy()
        {
            CleanOpenRiaClientFilesTask task = new CleanOpenRiaClientFilesTask();
            MockBuildEngine mockBuildEngine = new MockBuildEngine();
            task.BuildEngine = mockBuildEngine;

            string tempFolder = CodeGenHelper.GenerateTempFolder();
            try
            {
                // Do a simple copy with no special handling for attributes
                string file1 = Path.Combine(tempFolder, "File1.txt");
                string file2 = Path.Combine(tempFolder, "File2.txt");
                File.AppendAllText(file1, "stuff");

                bool success = task.SafeFileCopy(file1, file2, /*isProjectFile*/ false);
                Assert.IsTrue(success, "SafeFileCopy reported failure");

                Assert.IsTrue(File.Exists(file2), "File2 did not get created");
                string content = File.ReadAllText(file2);
                Assert.AreEqual("stuff", content, "File2 did not get right content");

                FileAttributes fa = File.GetAttributes(file2);
                Assert.AreEqual(0, (int)(fa & FileAttributes.ReadOnly), "Expected RO bit not to be set");

                Assert.IsFalse(task.FilesWereWritten, "Should not have marked files as written");
                File.Delete(file2);

                // Repeat, but ask for it to be treated as a project file
                success = task.SafeFileCopy(file1, file2, /*isProjectFile*/ true);
                Assert.IsTrue(success, "SafeFileCopy reported failure");

                Assert.IsTrue(File.Exists(file2), "File2 did not get created");
                content = File.ReadAllText(file2);
                Assert.AreEqual("stuff", content, "File2 did not get right content");

                fa = File.GetAttributes(file2);
                Assert.AreEqual((int) FileAttributes.ReadOnly, (int)(fa & FileAttributes.ReadOnly), "Expected RO bit to be set");

                Assert.IsTrue(task.FilesWereWritten, "Should have marked files as written");
                RiaClientFilesTaskHelpers.SafeFileDelete(file2, task);

                string errorMessage = String.Empty;

                // Finally, try a clearly illegal copy and catch the error
                using (FileStream fs = new FileStream(file1, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    try
                    {
                        File.Copy(file1, file2, true);
                    }
                    catch (IOException iox)
                    {
                        errorMessage = iox.Message;
                    }
                    success = task.SafeFileCopy(file1, file2, /*isProjectFile*/ false);
                }

                Assert.IsFalse(success, "Expected illegal copy to report failure");


                string expectedWarning = string.Format(CultureInfo.CurrentCulture, Resource.Failed_To_Copy_File, file1, file2, errorMessage);
                TestHelper.AssertContainsWarnings(mockBuildEngine.ConsoleLogger, expectedWarning);
            }

            finally
            {
                CodeGenHelper.DeleteTempFolder(tempFolder);
            }
        }

        [Description("RiaClientFilesTask.SafeFileMove works and catches expected exceptions")]
        [TestMethod]
        public void RiaClientFilesTask_Safe_File_Move()
        {
            CleanOpenRiaClientFilesTask task = new CleanOpenRiaClientFilesTask();
            MockBuildEngine mockBuildEngine = new MockBuildEngine();
            task.BuildEngine = mockBuildEngine;

            string tempFolder = CodeGenHelper.GenerateTempFolder();
            try
            {
                // Do a simple move
                string file1 = Path.Combine(tempFolder, "File1.txt");
                string file2 = Path.Combine(tempFolder, "File2.txt");
                File.AppendAllText(file1, "stuff");

                bool success = task.SafeFileMove(file1, file2);
                Assert.IsTrue(success, "SafeFileMove reported failure");

                Assert.IsTrue(File.Exists(file2), "File2 did not get created");
                Assert.IsFalse(File.Exists(file1), "File1 still exists after move");

                string content = File.ReadAllText(file2);
                Assert.AreEqual("stuff", content, "File2 did not get right content");

                string errorMessage = String.Empty;

                // Finally, try a clearly illegal move and catch the error
                using (FileStream fs = new FileStream(file2, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    try
                    {
                        File.Move(file2, file1);
                    }
                    catch (IOException iox)
                    {
                        errorMessage = iox.Message;
                    }
                    success = task.SafeFileMove(file2, file1);
                }

                Assert.IsFalse(success, "Expected illegal move to report failure");

                string expectedWarning = string.Format(CultureInfo.CurrentCulture, Resource.Failed_To_Rename_File, file2, file1, errorMessage);
                TestHelper.AssertContainsWarnings(mockBuildEngine.ConsoleLogger, expectedWarning);
            }

            finally
            {
                CodeGenHelper.DeleteTempFolder(tempFolder);
            }
        }
    }
}
