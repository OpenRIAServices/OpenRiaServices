using System;
using Microsoft.Silverlight.Testing.UnitTesting.Harness;
using Microsoft.Silverlight.Testing.UnitTesting.Metadata;

namespace Website.SilverlightHost
{
    public class RiaTestHarness : UnitTestHarness
    {
        private Func<ITestMethod, bool> _includeTest;

        public RiaTestHarness(Func<ITestMethod, bool> includeTest)
        {
            _includeTest = includeTest;
        }

        protected override TestRunFilter CreateTestRunFilter(Microsoft.Silverlight.Testing.UnitTestSettings settings)
        {
            return new RiaTestRunFilter(settings, this, this._includeTest);
        }
    }
}
