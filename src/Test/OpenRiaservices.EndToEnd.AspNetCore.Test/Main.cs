extern alias httpDomainClient; 

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using httpDomainClient::OpenRiaServices.Client.DomainClients;
using OpenRiaServices.Common.Test;
using System.Diagnostics;

namespace OpenRiaServices.Client.Test
{
    [TestClass()]
    public sealed class Main
    {
        private static Process s_aspNetCoreSite = null;
        private static IISExpressWebserver s_webServer = null;

        [AssemblyInitialize()]
        public static void AssemblyInit(TestContext context)
        {
            Thread.CurrentThread.CurrentUICulture
                = Thread.CurrentThread.CurrentCulture
                    = new System.Globalization.CultureInfo("en-US");

            StartWebServer();

            DomainContext.DomainClientFactory = new BinaryHttpDomainClientFactory(TestURIs.RootURI, new HttpClientHandler()
            {
                CookieContainer = new CookieContainer(),
                UseCookies = true,
                AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip,
            });
#if NETFRAMEWORK
            DomainContext.DomainClientFactory = new Web.WebDomainClientFactory()
            {
                ServerBaseUri = TestURIs.RootURI,
            };
#endif

            HttpWebRequest.DefaultCachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.Default);
        }

        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
#if !VBTests
            // make sure our test database is removed on the server after all unit tests
            // have been run
            ((IDisposable)UpdateTests.TestDatabase).Dispose();
#endif
            s_webServer?.Stop();
            s_aspNetCoreSite?.Kill();
        }

        private static void StartWebServer()
        {
            const string ProcessName = "AspNetCoreWebsite";
            string projectPath = File.ReadAllLines("ClientTestProjectPath.txt")[0];
#if DEBUG
            string webSitePath = Path.GetFullPath(Path.Combine(projectPath, @"../AspNetCoreWebsite/bin/Debug/net6.0/"));
#else
            string webSitePath = Path.GetFullPath(Path.Combine(projectPath, @"../AspNetCoreWebsite/bin/Release/net6.0/"));
#endif

            string processPath = webSitePath + ProcessName + ".exe";
            //webSitePath = Path.GetFullPath(Path.Combine(projectPath, @"../AspNetCoreWebsite"));
            if (!Directory.Exists(webSitePath))
                throw new FileNotFoundException($"Website not found at {webSitePath}");

            if (!File.Exists(processPath))
                throw new FileNotFoundException($"AspNetCore website not found at {processPath}");

            var websites = Process.GetProcessesByName(ProcessName);
            if (websites.Any())
            {
                // Already running do nothing
            }
            else
            {
                ProcessStartInfo startInfo = new(processPath, "--urls \"https://localhost:7045;http://localhost:5246\"");
                startInfo.EnvironmentVariables.Add("ASPNETCORE_ENVIRONMENT", "Development");
                startInfo.UseShellExecute = false;
                s_aspNetCoreSite = Process.Start(startInfo);

                // TODO: Wait for standard output or similar instead (GET "/"))
                Thread.Sleep(TimeSpan.FromSeconds(2));
            }
        }
    }
}
