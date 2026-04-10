using System.Collections.Generic;
using OpenRiaServices.Server;
using OpenRiaServices.Server.Test.Utilities;
using OpenRiaServices.Tools.SharedTypes;
using OpenRiaServices.Tools.SourceLocation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServerClassLib;

namespace OpenRiaServices.Tools.Test
{
    /// <summary>
    /// Tests for SharedFiles service
    /// </summary>
    [TestClass]
    public class SharedSourceFilesTests
    {
        public SharedSourceFilesTests()
        {
        }

        [Description("SharedSourceFiles locates shared types between projects")]
        [TestMethod]
        public void SharedSourceFiles_Types()
        {
            string projectPath = null;
            string outputPath = null;
            TestHelper.GetProjectPaths("", out projectPath, out outputPath);
            string clientProjectPath = CodeGenHelper.ClientClassLibProjectPath(projectPath);
            List<string> sourceFiles = CodeGenHelper.ClientClassLibSourceFiles(clientProjectPath);
            ConsoleLogger logger = new ConsoleLogger();
            FilenameMap filenameMap = new FilenameMap();

            using (SourceFileLocationService locationService = new SourceFileLocationService(new[] { new PdbSourceFileProviderFactory(/*symbolSearchPath*/ null, logger) }, filenameMap))
            {
                SharedSourceFiles ssf = new SharedSourceFiles(locationService, filenameMap, sourceFiles);

                int[] fileIds = ssf.GetSharedFileIds(CodeMemberKey.CreateTypeKey(typeof(TestEntity)));
                Assert.IsNotNull(fileIds, "Expected TestEntity to have non-null file ID's because it is shared");
                Assert.HasCount(2, fileIds, "Expected TestEntity to be found in exactly 2 files");
                foreach (int i in fileIds)
                {
                    string file = filenameMap[i];
                    Assert.IsTrue(file.Contains("TestEntity.linked.cs") || file.Contains("TestEntity.reverse.linked.cs"), "Expected exactly these 2 files to be shared");
                }

                fileIds = ssf.GetSharedFileIds(CodeMemberKey.CreateTypeKey(typeof(TestValidator)));
                Assert.IsNotNull(fileIds, "Expected TestValidator to have non-null file ID's because it is shared");
                Assert.HasCount(1, fileIds, "Expected TestValidator to be found in exactly one file");
                Assert.Contains("TestValidator.linked.cs", filenameMap[fileIds[0]], "expected this to be the sole shared file for TestValidator");

                fileIds = ssf.GetSharedFileIds(CodeMemberKey.CreateTypeKey(typeof(DomainService)));
                Assert.IsNull(fileIds, "Expected DomainService to have no shared file ids");

                fileIds = ssf.GetSharedFileIds(CodeMemberKey.CreateTypeKey(typeof(TestValidatorServer)));
                Assert.IsNull(fileIds, "Expected DomainService to have no shared file ids");
            }
        }

        [Description("SharedSourceFiles locates shared methods between projects")]
        [TestMethod]
        public void SharedSourceFiles_Methods()
        {
            string projectPath = null;
            string outputPath = null;
            TestHelper.GetProjectPaths("", out projectPath, out outputPath);
            string clientProjectPath = CodeGenHelper.ClientClassLibProjectPath(projectPath);
            List<string> sourceFiles = CodeGenHelper.ClientClassLibSourceFiles(clientProjectPath);
            ConsoleLogger logger = new ConsoleLogger();
            FilenameMap filenameMap = new FilenameMap();

            using (SourceFileLocationService locationService = new SourceFileLocationService(new[] { new PdbSourceFileProviderFactory(/*symbolSearchPath*/ null, logger) }, filenameMap))
            {
                SharedSourceFiles ssf = new SharedSourceFiles(locationService, filenameMap, sourceFiles);

                int[] fileIds = ssf.GetSharedFileIds(CodeMemberKey.CreateMethodKey(typeof(TestValidator).GetMethod("IsValid")));
                Assert.IsNotNull(fileIds, "Expected TestValidator.IsValid to have non-null file ID's because it is shared");
                Assert.HasCount(1, fileIds, "Expected TestValidator.IsValid to be found in exactly one file");
                Assert.Contains("TestValidator.linked.cs", filenameMap[fileIds[0]], "Expected TestValidator.IsValid to be in TestValidator.linked.cs");

                fileIds = ssf.GetSharedFileIds(CodeMemberKey.CreateMethodKey(typeof(TestEntity).GetMethod("ServerAndClientMethod")));
                Assert.IsNotNull(fileIds, "Expected TestEntity.ServerAndClientMethod to have non-null file ID's because it is shared");
                Assert.HasCount(1, fileIds, "Expected TestEntity.ServerAndClientMethod to be found in exactly one file");
                Assert.Contains("TestEntity.linked.cs", filenameMap[fileIds[0]], "Expected TestEntity.ServerAndClientMethod to be in TestEntity.linked.cs");

                fileIds = ssf.GetSharedFileIds(CodeMemberKey.CreateMethodKey(typeof(TestEntity).GetMethod("ServerMethod")));
                Assert.IsNull(fileIds, "Expected TestEntity.ServerMethod to have null file ids");

                fileIds = ssf.GetSharedFileIds(CodeMemberKey.CreateMethodKey(typeof(TestValidatorServer).GetMethod("IsValid")));
                Assert.IsNull(fileIds, "Expected TestValidatorServer.IsValid to have null file ids");
            }
        }
    }
}
