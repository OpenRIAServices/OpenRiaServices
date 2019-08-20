using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenRiaServices.DomainServices.Client.Test
{
    [TestClass()]
    public sealed class Main
    {
        private static Process s_iisProcess = null;

        [AssemblyInitialize()]
        public static void AssemblyInit(TestContext context)
        {
            Thread.CurrentThread.CurrentUICulture
                = Thread.CurrentThread.CurrentCulture
                    = new System.Globalization.CultureInfo("en-US");

            StartWebServer();

            DomainContext.DomainClientFactory = new Web.WebDomainClientFactory()
            {
                ServerBaseUri = TestURIs.RootURI,
            };

            HttpWebRequest.DefaultCachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.Default);
        }

        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            // make sure our test database is removed on the server after all unit tests
            // have been run
            ((IDisposable)UpdateTests.TestDatabase).Dispose();

            StopWebserver();
        }

        private static string GetCommandLine(Process processs)
        {
            ManagementObjectSearcher commandLineSearcher = new ManagementObjectSearcher(
                $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {processs.Id}");
            foreach (ManagementObject commandLineObject in commandLineSearcher.Get())
            {
                return commandLineObject["CommandLine"]?.ToString() ?? string.Empty;
            }

            return string.Empty;
        }


        private static bool ShouldStartNewWebserverProcess(string commandlineArguments, string port)
        {
            foreach (var process in Process.GetProcessesByName("iisexpress"))
            {
                var commandline = GetCommandLine(process);
                if (commandline.Contains(commandlineArguments))
                {
                    // Process was left from another test session.
                    // Kill it so we can start a new with redirected intput/output
                    process.Kill();
                    process.WaitForExit();
                    return true;
                }
                // TODO: Fix this condition, it probably dont work now
                else if (commandline.Contains(port))
                {
                    // return true and do nothing if different command line (started by user/vs)
                    return false;
                }
            }
            return true;
        }

        private static void StartWebServer()
        {
            string projectPath = File.ReadAllLines("ClientTestProjectPath.txt")[0];
            string webSitePath = Path.GetFullPath(Path.Combine(projectPath, @"..\..\..\Test\WebsiteFullTrust"));

            if (!Directory.Exists(webSitePath))
                throw new FileNotFoundException($"Website not found at {webSitePath}");

            string iisexpress = Environment.ExpandEnvironmentVariables(@"%programfiles%\IIS Express\iisexpress.exe");

            string portNumberText = "60002";
            string arguments = $"/port:{portNumberText} /path:\"{webSitePath}\"";

            // only start webserver if it is not already running
            if (ShouldStartNewWebserverProcess(arguments, portNumberText))
            {
                var startInfo = new ProcessStartInfo();
                startInfo.FileName = iisexpress;
                startInfo.Arguments = arguments;
                startInfo.RedirectStandardInput = true;
                startInfo.RedirectStandardOutput = true;
                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = true;

                s_iisProcess = Process.Start(startInfo);

                string str;
                while ((str = s_iisProcess.StandardOutput.ReadLine()) != null)
                {
                    if (str.Contains("IIS Express is running")
                        || str.Contains("'Q'"))
                    {
                        // Wait for IIS start.
                        /*var webRequest = HttpWebRequest.CreateHttp("http://localhost:60002/Cities-CityDomainService.svc");
                        var response = webRequest.GetResponse();
                        string responseText;
                        using (var reader = new StreamReader(response.GetResponseStream()))
                            responseText = reader.ReadToEnd();
                        */

                        new Thread(ProcessIISStandardOutput)
                            .Start();

                        return;
                    }

                }

                throw new Exception("Failed to start IIS express");
            }
        }

        private static void ProcessIISStandardOutput()
        {
            try
            {
                // Reade from standard output so the buffer doesnt get full and
                // process pauses
                string str;
                while (!s_iisProcess.HasExited &&
                    (str = s_iisProcess.StandardOutput.ReadLine()) != null)
                {

                }

            }
            catch (Exception)
            {
                if (!s_iisProcess.HasExited)
                    throw;
            }
        }

        /// <summary>Performs cleanup and ensures that there are no active web servers.</summary>
        public static void StopWebserver()
        {
            if (s_iisProcess != null)
            {
                // The local web server does not respond to CloseMainWindow.
                Trace.WriteLine("Closing web server process...");

                if (!s_iisProcess.HasExited)
                {
                    try
                    {
                        s_iisProcess.StandardInput.WriteLine("Q");
                        s_iisProcess.WaitForExit(1 * 1000); // wait 1 secs.

                        if (!s_iisProcess.HasExited)
                        {
                            s_iisProcess.Kill();
                            s_iisProcess.WaitForExit(60 * 1000); // wait 60 secs.
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        Trace.WriteLine("Unable to kill local web server process.");
                    }
                }

                s_iisProcess.Dispose();
                s_iisProcess = null;
            }
        }
    }
}
