using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
//using Microsoft.Build.Locator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenRiaServices.DomainServices.Tools.Test.Utilities
{
    [TestClass]
    public sealed class TestInitializer
    {
        [AssemblyInitialize]
        public static void AssemblyInit(TestContext context)
        {
            /*
            var msbuildLocator = typeof(TestInitializer
 ).Assembly.GetReferencedAssemblies().FirstOrDefault(n => n.Name == "Microsoft.Build.Locator");

            if (msbuildLocator != null)
            {
               var assembly = System.Reflection.Assembly.Load(msbuildLocator);
               var locator = assembly.GetType("Microsoft.Build.Locator.MSBuildLocator");
                locator.GetMethod("RegisterDefaults", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
                    .Invoke(null, null);

            }
            */
            Microsoft.Build.Locator.MSBuildLocator.RegisterDefaults();

            //Set currenct culture to en-US by default since there are hard coded
            //strings in some tests
            Thread.CurrentThread.CurrentCulture =
            Thread.CurrentThread.CurrentUICulture =
            CultureInfo.DefaultThreadCurrentCulture =
            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");

            // Ensure we reference sql client so that it is copied to test folder
            // se https://stackoverflow.com/questions/18455747/no-entity-framework-provider-found-for-the-ado-net-provider-with-invariant-name#18642452
            var type = typeof(System.Data.Entity.SqlServer.SqlProviderServices);
            if (type == null)
                throw new Exception("Do not remove, ensures static reference to System.Data.Entity.SqlServer");
        }
    }
}
