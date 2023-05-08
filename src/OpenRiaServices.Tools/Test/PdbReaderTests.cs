using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using OpenRiaServices.Server.Test.Utilities;
using OpenRiaServices.Tools.SourceLocation;
using OpenRiaServices.Tools.SharedTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServerClassLib;

namespace OpenRiaServices.Tools.Test
{
    /// <summary>
    /// Tests PdbReader
    /// </summary>
    [TestClass]
    public class PdbReaderTests
    {
        public PdbReaderTests()
        {
        }

        [DeploymentItem(@"ProjectPath.txt", "PDB")]
        [Description("PdbReader finds files defining properties in server assembly")]
        [TestMethod]
        public void PdbReader_Finds_Method_Files()
        {
            string projectPath = null;
            string outputPath = null;
            TestHelper.GetProjectPaths("PDB", out projectPath, out outputPath);

            using (ISourceFileProvider pdbReader = new PdbSourceFileProviderFactory(/*symbolSearchPath*/ null, /*logger*/ null).CreateProvider())
            {
                string serverProjectPath = CodeGenHelper.ServerClassLibProjectPath(projectPath);

                Type testEntityType = typeof(TestEntity);
                PropertyInfo pInfo = testEntityType.GetProperty("TheKey");
                Assert.IsNotNull(pInfo);

                // Must find TheKey in only TestEntity.cs -- it is readonly and has no setter
                string actualFile = pdbReader.GetFileForMember(pInfo.GetGetMethod());
                string expectedFile = Path.Combine(Path.GetDirectoryName(serverProjectPath), "TestEntity.cs");
                Assert.AreEqual(expectedFile.ToUpperInvariant(), actualFile.ToUpperInvariant());

                // Must find TheValue in only TestEntity.cs
                pInfo = testEntityType.GetProperty("TheValue");
                actualFile = pdbReader.GetFileForMember(pInfo.GetGetMethod());
                expectedFile = Path.Combine(Path.GetDirectoryName(serverProjectPath), "TestEntity.cs");

                // Must find TheSharedValue in only TestEntity.shared.cs -- validates we locate shared files
                pInfo = testEntityType.GetProperty("TheSharedValue");
                actualFile = pdbReader.GetFileForMember(pInfo.GetGetMethod());
                expectedFile = Path.Combine(Path.GetDirectoryName(serverProjectPath), "TestEntity.shared.cs");

                // Must find ServerAndClientValue in only TestEntity.linked.cs -- validates we locate linked files
                pInfo = testEntityType.GetProperty("ServerAndClientValue");
                actualFile = pdbReader.GetFileForMember(pInfo.GetGetMethod());
                expectedFile = Path.Combine(Path.GetDirectoryName(serverProjectPath), "TestEntity.linked.cs");
            }
        }

        [DeploymentItem(@"ProjectPath.txt", "PDB")]
        [Description("PdbReader finds all files for a type")]
        [TestMethod]
        public void PdbReader_Finds_Types_Files()
        {
            string projectPath = null;
            string outputPath = null;
            TestHelper.GetProjectPaths("PDB", out projectPath, out outputPath);
            string serverProjectPath = CodeGenHelper.ServerClassLibProjectPath(projectPath);
            string clientProjectPath = CodeGenHelper.ClientClassLibProjectPath(projectPath);
            ConsoleLogger logger = new ConsoleLogger();
            FilenameMap filenameMap = new FilenameMap();
            using (SourceFileLocationService locationService = new SourceFileLocationService(new[] { new PdbSourceFileProviderFactory(/*symbolSearchPath*/ null, logger) }, filenameMap))
            {
                List<string> files = new List<string>(locationService.GetFilesForType(typeof(TestEntity)));
                Assert.AreEqual(4, files.Count);

                CodeGenHelper.AssertContainsFiles(files, serverProjectPath, new string[] { "TestEntity.cs", "TestEntity.shared.cs", "TestEntity.linked.cs" });
                CodeGenHelper.AssertContainsFiles(files, clientProjectPath, new string[] { "TestEntity.reverse.linked.cs" });
            }
        }

    }
}
