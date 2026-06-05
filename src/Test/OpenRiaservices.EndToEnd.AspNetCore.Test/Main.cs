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
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Nerdbank.MessagePack;
using AspNetCoreWebsite.MessagePack;

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

            var msgPackSerializer = new MessagePackSerializer()
            {
                Converters = [
                        new XElementConverter()
                    ],
                ComparerProvider = new CustomComparerProvider()
            };

            var messagePack = new MessagePackHttpDomainClientFactory(TestURIs.RootURI, httpClientFactory, msgPackSerializer)
            {
                UseQueryHttpMethod = true,
                
            };
            var binary = new BinaryHttpDomainClientFactory(TestURIs.RootURI, httpClientFactory)
            {
                UseQueryHttpMethod = true,
            };
            var xml = new XmlHttpDomainClientFactory(TestURIs.RootURI, httpClientFactory);

#if NET10_0
            DomainContext.DomainClientFactory = new CompositeDomainClientFactory(binary, messagePack, binary);
#else
            DomainContext.DomainClientFactory = binary;
#endif
            // DomainContext.DomainClientFactory = new XmlHttpDomainClientFactory(TestURIs.RootURI, httpClientFactory);
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

        private static void StartWebServer([CallerFilePath] string filePath = null)
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

    sealed class CompositeDomainClientFactory : IDomainClientFactory
    {
        private readonly IDomainClientFactory _queryFactory;
        private readonly IDomainClientFactory _invokeFactory;
        private readonly IDomainClientFactory _submitFactory;

        public CompositeDomainClientFactory(IDomainClientFactory queryFactory, IDomainClientFactory invokeFactory, IDomainClientFactory submitFactory)
        {
            _queryFactory = queryFactory;
            _invokeFactory = invokeFactory;
            _submitFactory = submitFactory;
        }

        public DomainClient CreateDomainClient([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type serviceContract, Uri serviceUri, bool requiresSecureEndpoint)
        {
            return new CompositeDomainClient(_queryFactory.CreateDomainClient(serviceContract, serviceUri, requiresSecureEndpoint)
                , _invokeFactory.CreateDomainClient(serviceContract, serviceUri, requiresSecureEndpoint)
                , _submitFactory.CreateDomainClient(serviceContract, serviceUri, requiresSecureEndpoint));
        }

        sealed class CompositeDomainClient(DomainClient queryClient, DomainClient invokeClient, DomainClient submitClient) : DomainClient
        {
            public override bool SupportsCancellation => true;

            protected override Task<InvokeCompletedResult> InvokeAsyncCore(InvokeArgs invokeArgs, CancellationToken cancellationToken)
            {
                if (invokeClient.EntityTypes is null)
                    invokeClient.EntityTypes = this.EntityTypes;

                return invokeClient.InvokeAsync(invokeArgs, cancellationToken);
            }

            protected override Task<QueryCompletedResult> QueryAsyncCore(EntityQuery query, CancellationToken cancellationToken)
            {
                if (queryClient.EntityTypes is null)
                    queryClient.EntityTypes = this.EntityTypes;

                return queryClient.QueryAsync(query, cancellationToken);
            }

            protected override Task<SubmitCompletedResult> SubmitAsyncCore(EntityChangeSet changeSet, CancellationToken cancellationToken)
            {
                if (submitClient.EntityTypes is null)
                    submitClient.EntityTypes = this.EntityTypes;

                return submitClient.SubmitAsync(changeSet, cancellationToken);
            }
        }
    }
}
