extern alias httpDomainClient;

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using httpDomainClient::OpenRiaServices.Client.DomainClients;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace OpenRiaServices.Client.Test
{
    [TestClass()]
    public sealed class Main
    {
        private static Process s_aspNetCoreSite;

        /// <summary>
        /// Initializes the test assembly: sets the current culture to en-US, ensures the test web server is running, and configures the DomainClientFactory used by tests.
        /// </summary>
        /// <param name="context">The MSTest context for the assembly initialization.</param>
        /// <remarks>
        /// Configures an HttpClientHandler with cookie support and automatic GZip/Deflate decompression, and supplies a factory that trims a trailing ".svc/binary/" from endpoint URIs before setting the HttpClient BaseAddress. The DomainClientFactory is set to use the binary HTTP domain client pointing at TestURIs.RootURI.
        /// </remarks>
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
            // DomainContext.DomainClientFactory = new XmlHttpDomainClientFactory(TestURIs.RootURI, httpClientFactory);
        }

        /// <summary>
        /// Performs global teardown after all tests in the assembly by disposing the test database and stopping the test web server process if it is running.
        /// </summary>
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

        /// <summary>
        /// Starts the AspNetCoreWebsite executable (if not already running) and waits for a successful HTTP response from the site's root endpoint.
        /// </summary>
        /// <param name="filePath">Caller source file path used to locate the AspNetCoreWebsite project folder; provided automatically by the <see cref="System.Runtime.CompilerServices.CallerFilePathAttribute"/>.</param>
        /// <exception cref="System.IO.FileNotFoundException">Thrown when the website folder or executable cannot be found at the expected path.</exception>
        /// <exception cref="System.Exception">Thrown if the started website process exits before the root endpoint responds successfully.</exception>
        /// <exception cref="System.TimeoutException">Thrown if the root endpoint does not return a successful response within one minute.</exception>
        private static void StartWebServer([CallerFilePath]string filePath = null)
        {
            const string ProcessName = "AspNetCoreWebsite";
            string projectPath = Path.GetDirectoryName(filePath);

#if DEBUG
            string configuration = "Debug";
#else
            string configuration = "Release";
#endif

#if NET10_0
            string targetFramework = "net10.0";
#else
            string targetFramework = "net8.0";
#endif

            string webSitePath = Path.GetFullPath(Path.Join(projectPath, @$"../AspNetCoreWebsite/bin/{configuration}/{targetFramework}/"));
            string processPath = webSitePath + ProcessName + ".exe";

            if (!Directory.Exists(webSitePath))
                throw new FileNotFoundException($"Website not found at {webSitePath}");

            if (!File.Exists(processPath))
                throw new FileNotFoundException($"AspNetCore website not found at {processPath}");

            var websites = Process.GetProcessesByName(ProcessName);
            if (websites.Any())
            {
                Console.WriteLine("AssemblyInitialize: Webserver process was already started, not starting anything");
                // Already running. do nothing
            }
            else
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = processPath,
                    UseShellExecute = false,
                    WorkingDirectory = Path.GetFullPath(Path.Join(projectPath, @"../AspNetCoreWebsite/"))
                };
                startInfo.ArgumentList.Add("--urls");
                startInfo.ArgumentList.Add(TestURIs.RootURI.ToString());
                startInfo.EnvironmentVariables.Add("ASPNETCORE_ENVIRONMENT", "Development");
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
                    var res = httpClient.GetAsync(TestURIs.RootURI).GetAwaiter().GetResult();
                    if (res.IsSuccessStatusCode)
                    {
                        Console.WriteLine("AssemblyInitialize: Webserver started");
                        return;
                    }
                }
                catch (Exception)
                {
                    // Ignore error
                }

                if (s_aspNetCoreSite?.HasExited == true)
                    throw new Exception("Website stopped");

                Thread.Sleep(TimeSpan.FromSeconds(1));
            } while (stopwatch.Elapsed <= TimeSpan.FromMinutes(1));

            throw new TimeoutException("webserver did not respond to '/' in 1 minute");
        }
    }
}