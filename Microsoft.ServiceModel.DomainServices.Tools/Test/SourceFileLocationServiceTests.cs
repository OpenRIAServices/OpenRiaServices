using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceModel.DomainServices.Server.Test.Utilities;
using System.Threading;
using Microsoft.ServiceModel.DomainServices.Tools.SharedTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServerClassLib;

using Test.Microsoft.VisualStudio.ServiceModel.DomainServices.Intellisense;

namespace Microsoft.ServiceModel.DomainServices.Tools.Test
{
    /// <summary>
    /// Tests SourceInfoAttribute -- a dynamically generated type used in Live Intellisense
    /// to provide file positions for declared members.
    /// </summary>
    [TestClass]
    public class SourceFileLocationServiceTests
    {
        public SourceFileLocationServiceTests()
        {
        }

        [TestMethod]
        [Description("Mock ISourceFileLocationServices works correctly")]
        public void Mock_SourceFileLocationService_Works()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();

            // We will generate a fake file name for the request of the currently executing method
            // This tests whether MemberInfo equality is a good mechanism and shows the basic
            // plumbing of the abstract base class works
            Func<MemberInfo, string> func = m => { return m.Equals(methodBase) ? m.Name : null; };
            FilenameMap filenameMap = new FilenameMap();

            using (SourceFileLocationService service = new SourceFileLocationService(new[] { new MockSourceFileProviderFactory(func) }, filenameMap))
            {
                IEnumerable<string> files = service.GetFilesForType(this.GetType());
                Assert.AreEqual(1, files.Count(), "Expected only one file name");
                Assert.IsTrue(files.Contains(methodBase.Name), "Expected the name of the current method to be returned for the declaring type");
            }
        }

        [TestMethod]
        [Description("Multiple ISourceFileProviders are called in the right order")]
        public void Multiple_Providers_Preserve_Order()
        {
            long createCountBefore = Interlocked.Read(ref MockSourceFileProviderFactory.createCount);

            // This func returns a file name only for the StringValue property
            Func<MemberInfo, string> func1 = m => { return m.Name == "StringValue" ? m.Name : null; };

            // The second func returns "IntValue" but also a contradictory name XXX for StringValue.
            // We are testing the preservation of order here in that only one service is allowed to
            // declare a file for a single MemberInfo, and the first one wins
            Func<MemberInfo, string> func2 = m => { return m.Name == "IntValue" ? m.Name : m.Name == "StringValue" ? "XXX" : null; };

            MockSourceFileProviderFactory factory1 = new MockSourceFileProviderFactory(func1);
            MockSourceFileProviderFactory factory2 = new MockSourceFileProviderFactory(func2);

            FilenameMap filenameMap = new FilenameMap();

            using (SourceFileLocationService locationService = new SourceFileLocationService(new[] { factory1, factory2 }, filenameMap))
            {
                IEnumerable<string> files = locationService.GetFilesForType(typeof(SourceFileLocationServiceTest));
                Assert.AreEqual(2, files.Count(), "Expected both service to contribute to files");
                Assert.IsTrue(files.Contains("StringValue"), "The first service should have gotten called");
                Assert.IsTrue(files.Contains("IntValue"), "The second service should have gotten called");
                Assert.IsFalse(files.Contains("XXX"), "The 2nd response to StringValue should be discarded");
            }

            long createCountAfter = Interlocked.Read(ref MockSourceFileProviderFactory.createCount);
            Assert.AreEqual(createCountBefore, createCountAfter, "Imbalanced create/dispose count of ISourceFileProviders");
        }
    }

    internal class SourceFileLocationServiceTest
    {
        public string StringValue { get; set; }
        public int IntValue { get; set; }
    }

    // Helper mock factory to create ISourceFileProvider mock instances
    internal class MockSourceFileProviderFactory : ISourceFileProviderFactory
    {
        public static long createCount = 0;

        private Func<MemberInfo, string> _func;

        public MockSourceFileProviderFactory(Func<MemberInfo, string> func)
        {
            this._func = func;
        }

        public ISourceFileProvider CreateProvider()
        {
            Interlocked.Increment(ref createCount);
            return new MockSourceFileProvider(this._func);
        }
    }

    // Helper mock ISourceFileProvider
    // It allows the caller to specify a Func that accepts a MemberInfo
    // and yields the file name to return as the declaring file
    internal class MockSourceFileProvider : ISourceFileProvider
    {
        private Func<MemberInfo, string> _func;

        public MockSourceFileProvider(Func<MemberInfo, string> func)
        {
            this._func = func;
        }
        public string GetFileForMember(MemberInfo memberInfo)
        {
            return this._func(memberInfo);
        }

        public void Dispose()
        {
            Interlocked.Decrement(ref MockSourceFileProviderFactory.createCount);
        }
    }
}
