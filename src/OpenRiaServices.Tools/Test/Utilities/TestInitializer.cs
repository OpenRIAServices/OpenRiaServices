using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.Build.Locator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenRiaServices.Tools.Test.Utilities
{
    [TestClass]
    public sealed class TestInitializer
    {
        [AssemblyInitialize]
        public static void AssemblyInit(TestContext testContext)
        {
            // Register the most recent version of MSBuild
            Microsoft.Build.Locator.MSBuildLocator.RegisterInstance(MSBuildLocator.QueryVisualStudioInstances()
                .OrderByDescending(instance => instance.Version).First());

            //Set currenct culture to en-US by default since there are hard coded
            //strings in some tests
            Thread.CurrentThread.CurrentCulture =
            Thread.CurrentThread.CurrentUICulture =
            CultureInfo.DefaultThreadCurrentCulture =
            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");

            DeploymentDirectory = testContext.DeploymentDirectory;

            // Ensure we reference sql client so that it is copied to test folder
            // se https://stackoverflow.com/questions/18455747/no-entity-framework-provider-found-for-the-ado-net-provider-with-invariant-name#18642452
            var type = typeof(System.Data.Entity.SqlServer.SqlProviderServices);
            if (type == null)
                throw new Exception("Do not remove, ensures static reference to System.Data.Entity.SqlServer");
        }

        public static string DeploymentDirectory { get; private set; }
    }
}
