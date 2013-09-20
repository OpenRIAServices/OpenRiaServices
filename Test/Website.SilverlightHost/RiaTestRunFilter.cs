using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Silverlight.Testing.UnitTesting.Harness;
using Microsoft.Silverlight.Testing;
using Microsoft.Silverlight.Testing.UnitTesting.Metadata;

namespace Website.SilverlightHost
{
    public class RiaTestRunFilter : TestRunFilter
    {
        private Func<ITestMethod, bool> _includeTest;

        public RiaTestRunFilter(UnitTestSettings settings, UnitTestHarness harness, Func<ITestMethod, bool> includeTest)
            : base(settings, harness)
        {
            _includeTest = includeTest;
        }

        protected override void FilterCustomTestMethods(IList<ITestMethod> methods)
        {
            foreach (var method in methods.ToList())
            {
                if (!_includeTest(method))
                {
                    methods.Remove(method);
                }
            }
        }
    }
}
