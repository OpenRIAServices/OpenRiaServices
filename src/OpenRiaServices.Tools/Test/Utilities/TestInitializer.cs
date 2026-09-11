using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Build.Locator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#if NETFRAMEWORK
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    internal sealed class ModuleInitializerAttribute : Attribute
    {
    }
}
#endif

namespace OpenRiaServices.Tools.Test.Utilities
{
    [TestClass]
    public static class TestInitializer
    {
        [ModuleInitializer]
        public static void InitializeMsBuildLocator()
        {
            if (MSBuildLocator.IsRegistered)
                return;

            var instance = MSBuildLocator.QueryVisualStudioInstances()
                .OrderByDescending(instance => instance.Version)
                .FirstOrDefault();

            // IMPORTANT: MSBuildLocator only discover SDK versions that are as old or older thant the
            // current target framework.
            // This means we can get errors in case, we compile for .NET 8 but only have .NET 9 or 10 SDK
#if NET
            string currentRuntime = typeof(TestInitializer).Assembly.GetCustomAttribute<System.Runtime.Versioning.TargetFrameworkAttribute>().FrameworkDisplayName;

            if (instance == null)
                throw new InvalidOperationException($"No dotnet SDK found (searched version <= {currentRuntime})");

            // Extract current runtime version
            if (!currentRuntime.StartsWith(".NET ", StringComparison.Ordinal))
                throw new InvalidOperationException($"Unexpected target framework name: {currentRuntime}");

            Version runtimeVersion = Version.Parse(currentRuntime.AsSpan(5));

            if (runtimeVersion >= instance.Version)
                throw new InvalidOperationException($"Expected dotnet sdk to be at least {runtimeVersion}, but found {instance.Version}");
#endif

            if (instance == null)
                throw new InvalidOperationException("MSBuildLocator failed to find msbuild");

            MSBuildLocator.RegisterInstance(instance);
        }

        [AssemblyInitialize]
        public static void AssemblyInit(TestContext testContext)
        {
            InitializeMsBuildLocator();

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
