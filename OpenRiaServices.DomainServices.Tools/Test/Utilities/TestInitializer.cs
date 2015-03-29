using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenRiaServices.DomainServices.Tools.Test.Utilities
{
    [TestClass]
    public sealed class TestInitializer
    {
        [AssemblyInitialize]
        public static void AssemblyInit(TestContext context)
        {
             //Set currenct culture to en-US by default since there are hard coded
             //strings in some tests
            Thread.CurrentThread.CurrentCulture = 
            Thread.CurrentThread.CurrentUICulture = 
            CultureInfo.DefaultThreadCurrentCulture =
            CultureInfo.DefaultThreadCurrentUICulture =  new CultureInfo("en-US");
        }
    }
}
