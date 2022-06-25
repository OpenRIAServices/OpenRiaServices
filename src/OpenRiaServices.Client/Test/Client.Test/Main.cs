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

namespace OpenRiaServices.Client.Test
{
    [TestClass()]
    public sealed class Main
    {
        private static IISExpressWebserver s_webServer = null;
        private static DotNetCoreHost _coreHost = null;

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

            // Uncomment below to run tests using BinaryHttpDomainClientFactory instead:
            //
            //DomainContext.DomainClientFactory = new BinaryHttpDomainClientFactory(new HttpClientHandler()
            //{
            //    CookieContainer = new CookieContainer(),
            //    UseCookies = true,
            //    AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip,
            //})
            //{
            //    ServerBaseUri = TestURIs.RootURI,
            //};

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
            _coreHost.Stop();
        }

        private static void StartWebServer()
        {
            string projectPath = File.ReadAllLines("ClientTestProjectPath.txt")[0];
            string webSitePath = Path.GetFullPath(Path.Combine(projectPath, @"..\..\..\Test\WebsiteFullTrust"));

            if (!Directory.Exists(webSitePath))
                throw new FileNotFoundException($"Website not found at {webSitePath}");

            s_webServer = new IISExpressWebserver();
#if VBTests
            s_webServer.Start(webSitePath, 60000);
#else
            s_webServer.Start(webSitePath, 60002);
#endif
            _coreHost = new DotNetCoreHost();
            _coreHost.Start(@"E:\OpenSource\OpenRiaServices\5.1\src\Test\AspNetCoreWebsite");
        }
    }
}
