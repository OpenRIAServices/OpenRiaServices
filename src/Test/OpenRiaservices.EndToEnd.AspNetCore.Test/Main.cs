extern alias httpDomainClient;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using httpDomainClient::OpenRiaServices.Client.DomainClients;

namespace OpenRiaServices.Client.Test
{
    [TestClass()]
    public sealed class Main
    {
        private static Process s_aspNetCoreSite;

        [AssemblyInitialize()]
        public static void AssemblyInit(TestContext context)
        {
            Thread.CurrentThread.CurrentUICulture
                = Thread.CurrentThread.CurrentCulture
                    = new System.Globalization.CultureInfo("en-US");

            StartWebServer();

            var clientHandler = new HttpClientHandler()
            {
                CookieContainer = new CookieContainer(),
                UseCookies = true,
                AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip,
            };

            // Map enpoint names from WCF format to "FullName" format
            // We do this since all DomainContext were generated usign old WCF format
            Func<Uri, HttpClient> httpClientFactory = uri =>
            {
                HttpClient httpClient = new HttpClient(clientHandler, disposeHandler: false);

                // Remove ".svc/binary" from the URI
                const string toRemove = ".svc/binary/";
                string uriString = uri.AbsoluteUri;

                if (uriString.EndsWith(toRemove, StringComparison.Ordinal))
                {
                    uri = new Uri(uriString.Remove(uriString.Length - toRemove.Length));
                }

                httpClient.BaseAddress = uri;
                return httpClient;
            };

            DomainContext.DomainClientFactory = new BinaryHttpDomainClientFactory(TestURIs.RootURI, httpClientFactory);
        }

        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
#if !VBTests
            // make sure our test database is removed on the server after all unit tests
            // have been run
            ((IDisposable)UpdateTests.TestDatabase).Dispose();
#endif
            s_aspNetCoreSite?.Kill();
        }

        private static void StartWebServer([CallerFilePath] string filePaht = null)
        {
            const string ProcessName = "AspNetCoreWebsite";
            string projectPath = Path.GetDirectoryName(filePaht);
#if DEBUG
            string configuration = "Debug";
#else
            string configuration = "Release";
#endif
            string targetFramework = "net8.0";


            string webSitePath = Path.GetFullPath(Path.Combine(projectPath, @$"../AspNetCoreWebsite/bin/{configuration}/{targetFramework}/"));
            string processPath = webSitePath + ProcessName + ".exe";

            if (!Directory.Exists(webSitePath))
                throw new FileNotFoundException($"Website not found at {webSitePath}");

            if (!File.Exists(processPath))
                throw new FileNotFoundException($"AspNetCore website not found at {processPath}");

            var websites = Process.GetProcessesByName(ProcessName);
            if (websites.Any())
            {
                Console.WriteLine("AssemblyInitialize: Webserver process was already started, not starting anything");
                // Already running do nothing
            }
            else
            {
                ProcessStartInfo startInfo = new(processPath, "--urls \"https://localhost:7045;http://localhost:5246\"");
                startInfo.EnvironmentVariables.Add("ASPNETCORE_ENVIRONMENT", "Development");
                startInfo.UseShellExecute = false;
                startInfo.WorkingDirectory = Path.GetFullPath(Path.Combine(projectPath, @"../AspNetCoreWebsite/"));
                s_aspNetCoreSite = Process.Start(startInfo);

                Console.WriteLine("AssemblyInitialize: Started webserver with PID {0}", s_aspNetCoreSite.Id);
            }

            // Wait for a successfull (GET "/") to succeed so we know webserver has started
            using HttpClient httpClient = new HttpClient();
            Stopwatch stopwatch = Stopwatch.StartNew();
            do
            {
                try
                {
                    var res = httpClient.GetAsync("http://localhost:5246/").GetAwaiter().GetResult();
                    if (res.IsSuccessStatusCode)
                    {
                        Console.WriteLine("AssemblyInitialize: Webserver started");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    // Ignore error
                }

                if (s_aspNetCoreSite.HasExited)
                    throw new Exception("Website stopped");

                Thread.Sleep(TimeSpan.FromSeconds(1));
            } while (stopwatch.Elapsed <= TimeSpan.FromMinutes(1));

            throw new TimeoutException("webserver did not respond to '/' in 1 minute");
        }
    }
}
