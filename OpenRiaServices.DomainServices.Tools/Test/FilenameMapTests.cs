using System.Collections.Generic;
using OpenRiaServices.DomainServices.Server;
using OpenRiaServices.DomainServices.Server.Test.Utilities;
using OpenRiaServices.DomainServices.Tools.SharedTypes;
using OpenRiaServices.DomainServices.Tools.SourceLocation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServerClassLib;

namespace OpenRiaServices.DomainServices.Tools.Test
{
    /// <summary>
    /// Tests for FileLocationCache
    /// </summary>
    [TestClass]
    public class FilenameMapTests
    {
        public FilenameMapTests()
        {
        }

        [Description("FileLocationCache ctor sets initial state correctly")]
        [TestMethod]
        public void FilenameMap_Ctor()
        {
            FilenameMap cache = new FilenameMap();
            int id = cache[string.Empty];
            Assert.AreEqual(FilenameMap.NotAFile, id, "expected empty string to be cached as NotAFile");

            string file = cache[id];
            Assert.AreEqual(string.Empty, file, "Expected cache[0] to be empty string");

            id = cache.AddOrGet(string.Empty);
            Assert.AreEqual(FilenameMap.NotAFile, id, "adding back string.Empty should be same ID");
        }

        [Description("FileLocationCache is case insensitive")]
        [TestMethod]
        public void FilenameMap_Case_Insensitive()
        {
            FilenameMap cache = new FilenameMap();
            int id1 = cache.AddOrGet("test");
            Assert.AreNotEqual(FilenameMap.NotAFile, id1, "actual ID cannot be NotAFile");

            int id2 = cache.AddOrGet("TeSt");
            Assert.AreEqual(id1, id2, "add should return same ID for files differing only in case");

            Assert.AreEqual(id1, cache["test"], "indexer should return same ID as add");
            Assert.AreEqual(id1, cache["tESt"], "indexer should return same ID as add differing in case");

            Assert.AreEqual("test", cache[id1], "cache should not have overwritten original");
        }
    }
}
