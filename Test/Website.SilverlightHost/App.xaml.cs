using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Browser;
using OpenRiaServices.DomainServices.Client.Test;
using Microsoft.Silverlight.Testing;
using Microsoft.Silverlight.Testing.UnitTesting.Metadata;

namespace Website.SilverlightHost
{
    public partial class App : Application
    {
        public App()
        {
            this.Startup += this.Application_Startup;
            this.UnhandledException += this.Application_UnhandledException;

            InitializeComponent();
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            this.RootVisual =
                UnitTestSystem.CreateTestPage(
                    this.GetUnitTestSettings(e.InitParams));
        }

        private UnitTestSettings GetUnitTestSettings(IDictionary<string, string> initParams)
        {
            var settings = UnitTestSystem.CreateDefaultSettings();
            string filter;
            if (!initParams.TryGetValue("AssemblyFilter", out filter))
            {
                filter = ".*";
            }

            string testFilter;
            HtmlPage.Document.QueryString.TryGetValue("testFilter", out testFilter);

            bool runPerfTestsOnly = false;
            string runPerfTestsOnlyString;
            if (initParams.TryGetValue("RunPerfTestsOnly", out runPerfTestsOnlyString) && runPerfTestsOnlyString.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                runPerfTestsOnly = true;
            }

            bool runFullTrustTestsOnly = false;
            string runFullTrustTestsOnlyString;
            if (initParams.TryGetValue("RunFullTrustTestsOnly", out runFullTrustTestsOnlyString) && runFullTrustTestsOnlyString.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                runFullTrustTestsOnly = true;
            }

            bool runMediumTrustTestsOnly = false;
            string runMediumTrustTestsOnlyString;
            if (initParams.TryGetValue("RunMediumTrustTestsOnly", out runMediumTrustTestsOnlyString) && runMediumTrustTestsOnlyString.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                runMediumTrustTestsOnly = true;
            }

            foreach (var part in Deployment.Current.Parts)
            {
                if (Regex.IsMatch(part.Source, filter))
                {
                    var sr = Application.GetResourceStream(new Uri(part.Source, UriKind.Relative));
                    var assembly = part.Load(sr.Stream);

                    settings.TestAssemblies.Add(assembly);
                }
            }

            if (!String.IsNullOrEmpty(testFilter))
            {
                settings.TestHarness = new RiaTestHarness(m => RunTest(m, testFilter));
            }
            else if (runPerfTestsOnly)
            {
                settings.TestHarness = new RiaTestHarness(m => IsPerfMethod(m));
            }
            else if (runFullTrustTestsOnly)
            {
                settings.TestHarness = new RiaTestHarness(m => IsFullTrustMethod(m));
            }
            else if (runMediumTrustTestsOnly)
            {
                settings.TestHarness = new RiaTestHarness(m => !IsFullTrustMethod(m));
            }

            return settings;
        }

        private bool RunTest(ITestMethod m, string testFilter)
        {
            return (m.Method.Name.Contains(testFilter) || m.Method.DeclaringType.FullName.Contains(testFilter));
        }

        private static bool IsPerfMethod(ITestMethod m)
        {
            return m.Method.GetCustomAttributes(typeof(PerfTestAttribute), /* inherit */ true).Any();
        }

        private static bool IsFullTrustMethod(ITestMethod m)
        {
            return m.Method.GetCustomAttributes(typeof(FullTrustTestAttribute), /* inherit */ true).Any();
        }

        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            // If the app is running outside of the debugger then report the exception using
            // the browser's exception mechanism. On IE this will display it a yellow alert 
            // icon in the status bar and Firefox will display a script error.
            if (!System.Diagnostics.Debugger.IsAttached)
            {

                // NOTE: This will allow the application to continue running after an exception has been thrown
                // but not handled. 
                // For production applications this error handling should be replaced with something that will 
                // report the error to the website and stop the application.
                e.Handled = true;
            }
        }
    }
}
