using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Client.DomainClients;
using OpenRiaServices.Common.Test;
using System.Runtime.CompilerServices;

namespace OpenRiaServices.Client.Test
{
    [TestClass()]
    public static class Main
    {
        private static Process s_aspNetCoreSite;

        [AssemblyInitialize()]
        public static void AssemblyInit(TestContext context)
        {
            Thread.CurrentThread.CurrentUICulture
                = Thread.CurrentThread.CurrentCulture
                    = new System.Globalization.CultureInfo("en-US");

            StartWebServer();

            HttpClientHandler clientHandler = new HttpClientHandler()
            {
                CookieContainer = new CookieContainer(),
                UseCookies = true,
                AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip,
            };
            Func<Uri, HttpClient> httpClientFactory = uri =>
            {
                HttpClient httpClient = new HttpClient(clientHandler, disposeHandler: false);
                const string toRemove = ".svc/binary/";
                string uriString = uri.AbsoluteUri;

                if (uriString.EndsWith(toRemove, StringComparison.Ordinal))
                    uri = new Uri(uriString.Remove(uriString.Length - toRemove.Length));

                httpClient.BaseAddress = uri;
                return httpClient;
            };

            DomainContext.DomainClientFactory = new BinaryHttpDomainClientFactory(TestURIs.RootURI, httpClientFactory)
            {
                UseQueryHttpMethod = true
            };

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
            StopWebServer();
        }

        private static void StartWebServer([CallerFilePathAttribute] string filepath = null)
        {
            string projectPath = Path.GetDirectoryName(filepath);
            string configuration =
#if DEBUG
                "Debug";
#else
                "Release";
#endif
            string websiteProjectPath = Path.GetFullPath(Path.Combine(projectPath, @"..\..\..\Test\AspNetCoreWebsite"));
            string webSitePath = Path.Combine(websiteProjectPath, "bin", configuration, "net8.0");
            string processPath = Path.Combine(webSitePath, "AspNetCoreWebsite.exe");

            if (!File.Exists(processPath))
                throw new FileNotFoundException($"ASP.NET Core website not found at {processPath}");

            using (HttpClient httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(2) })
            {
                if (IsWebServerRunning(httpClient))
                    return;

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = processPath,
                    Arguments = $"--urls \"{TestURIs.RootURI}\"",
                    UseShellExecute = false,
                    WorkingDirectory = websiteProjectPath
                };
                startInfo.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"] = "Development";

                s_aspNetCoreSite = Process.Start(startInfo)
                    ?? throw new InvalidOperationException("Failed to start the ASP.NET Core website.");

                bool serverStarted = false;
                try
                {
                    Stopwatch stopwatch = Stopwatch.StartNew();
                    while (stopwatch.Elapsed <= TimeSpan.FromMinutes(1))
                    {
                        if (IsWebServerRunning(httpClient))
                        {
                            serverStarted = true;
                            return;
                        }

                        if (s_aspNetCoreSite.HasExited)
                            throw new InvalidOperationException($"The ASP.NET Core website exited with code {s_aspNetCoreSite.ExitCode}.");

                        Thread.Sleep(TimeSpan.FromSeconds(1));
                    }

                    throw new TimeoutException("The ASP.NET Core website did not respond within one minute.");
                }
                finally
                {
                    if (!serverStarted)
                        StopWebServer();
                }
            }
        }

        private static bool IsWebServerRunning(HttpClient httpClient)
        {
            try
            {
                using (HttpResponseMessage response = httpClient.GetAsync(TestURIs.RootURI).GetAwaiter().GetResult())
                    return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException)
            {
                return false;
            }
            catch (TaskCanceledException)
            {
                return false;
            }
        }

        private static void StopWebServer()
        {
            if (s_aspNetCoreSite == null)
                return;

            if (!s_aspNetCoreSite.HasExited)
            {
                s_aspNetCoreSite.Kill();
                s_aspNetCoreSite.WaitForExit();
            }

            s_aspNetCoreSite.Dispose();
            s_aspNetCoreSite = null;
        }
    }
}
