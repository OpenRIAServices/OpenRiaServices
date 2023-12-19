extern alias SSmDsClient;
using System;
using System.IO;
using System.Linq;
using System.Net;
using DataTests.AdventureWorks.LTS;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Silverlight.Testing;
using TestDomainServices;

namespace OpenRiaServices.Client.Test
{
    using Resource = SSmDsClient::OpenRiaServices.Client.Resource;

    /// <summary>
    /// Testing the DataService factory mechanisms
    /// </summary>
    [TestClass]
    public class DataServiceTests : UnitTestBase
    {
        // Allow some delay between when the Date header and Expires headers are set
        // The dates are set at differetn times and only have 1s of resolution so 
        // which can lead to 1 second difference even if only 1ms has passed
        private const double OutputCacheMaxDelta = 1.0;

        // Test Todo:
        // - test wrong number of parameters sent / mismatch of name/type

        private TestProvider_Scenarios CreateDomainContextWithRestDomainClient()
        {
            return new TestProvider_Scenarios(new WebDomainClient<TestProvider_Scenarios.ITestProvider_ScenariosContract>(TestURIs.TestProvider_Scenarios));
        }


        private void ExecuteQuery(Func<TestProvider_Scenarios, EntityQuery<CityWithCacheData>> getQuery, string expectedCacheData)
        {
            LoadOperation<CityWithCacheData> loadOp = null;
            EnqueueCallback(delegate
            {
                TestProvider_Scenarios dc = new TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);
                loadOp = dc.Load(getQuery(dc));
            });

            this.EnqueueCompletion(() => loadOp);
            EnqueueCallback(delegate
            {
                if (loadOp.Error != null)
                {
                    Assert.Fail(loadOp.Error.ToString());
                }

                CityWithCacheData city = loadOp.Entities.First();
                Assert.AreEqual(expectedCacheData, city.CacheData, "Incorrect cache data");
            });
        }

#if !ASPNETCORE // Only for WCF Endpoints
        [TestMethod]
        [Asynchronous]
        [WorkItem(880862)]
        [TestCategory("WCF")]
        public void JsonEndpointWithQuery()
        {
            // Verify that we can send a query to the JSON endpoint.
            ExecuteRequest(new Uri(TestURIs.Cities + "/json/GetCities?$take=1"), response =>
            {
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            });

            EnqueueTestComplete();
        }
#endif

        // Skip the following tests in Silverlight because HttpWebResponse.Headers is not implemented in Silverlight.
#if !ASPNETCORE // Cahcing is not yet implemented (and will probably work a little bit different

        [TestMethod]
        [Asynchronous]
        [FullTrustTest]
        public void TestCacheLocations()
        {
            ExecuteQuery(dc => dc.GetCitiesWithCacheLocationAnyQuery(), "Public");
            ExecuteQuery(dc => dc.GetCitiesWithCacheLocationDownstreamQuery(), "Public");
            ExecuteQuery(dc => dc.GetCitiesWithCacheLocationServerQuery(), "Server");
            ExecuteQuery(dc => dc.GetCitiesWithCacheLocationServerAndClientQuery(), "ServerAndPrivate");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void TestNoCache()
        {
            ExecuteRequest(new Uri(TestURIs.TestProvider_Scenarios + "/binary/GetCities"), response =>
            {
                Assert.AreEqual("no-cache", response.Headers["Cache-Control"], "Incorrect cache header");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void TestCacheAndThrow()
        {
            ExecuteRequest(new Uri(TestURIs.TestProvider_Scenarios + "/binary/GetCitiesWithCachingAndThrow"), response =>
            {
                Assert.AreEqual("no-cache", response.Headers["Cache-Control"], "Incorrect cache header");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void TestCacheVaryByHeaders()
        {
            string responseText = null;

            ExecuteRequest(
                new Uri(TestURIs.TestProvider_Scenarios + "/binary/GetCitiesWithCachingVaryByHeaders"),
                request =>
                {
                    request.Headers["foo"] = "1";
                },
                response =>
                {
                    Assert.AreEqual("no-cache", response.Headers["Cache-Control"], "Incorrect cache header");

                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        responseText = reader.ReadToEnd();
                    }
                });

            EnqueueDelay(1000);

            ExecuteRequest(
                new Uri(TestURIs.TestProvider_Scenarios + "/binary/GetCitiesWithCachingVaryByHeaders"),
                request =>
                {
                    request.Headers["foo"] = "1";
                },
                response =>
                {
                    Assert.AreEqual("no-cache", response.Headers["Cache-Control"], "Incorrect cache header");

                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        Assert.AreEqual(responseText, reader.ReadToEnd(), "Response was not cached");
                    }
                });

            ExecuteRequest(
                new Uri(TestURIs.TestProvider_Scenarios + "/binary/GetCitiesWithCachingVaryByHeaders"),
                request =>
                {
                    request.Headers["foo"] = "2";
                },
                response =>
                {
                    Assert.AreEqual("no-cache", response.Headers["Cache-Control"], "Incorrect cache header");

                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        Assert.AreNotEqual(responseText, reader.ReadToEnd(), "Response was cached");
                    }
                });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void TestCacheAbsoluteExpiration()
        {
            ExecuteRequest(new Uri(TestURIs.TestProvider_Scenarios + "/binary/GetCitiesWithCaching"), response =>
            {
                Assert.AreEqual("private, max-age=2", response.Headers["Cache-Control"], "Incorrect cache header");

                DateTime date = Convert.ToDateTime(response.Headers["Date"]);
                DateTime expires = Convert.ToDateTime(response.Headers["Expires"]);
                Assert.AreEqual(2.0, expires.Subtract(date).TotalSeconds, OutputCacheMaxDelta, "Incorrect cache duration");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [FullTrustTest] // Because OutputCache profiles require full trust.
        public void TestCacheAbsoluteExpirationViaCacheProfile()
        {
            ExecuteRequest(new Uri(TestURIs.TestProvider_Scenarios + "/binary/GetCitiesWithCachingViaCacheProfile"), response =>
            {
                Assert.AreEqual("private, max-age=2", response.Headers["Cache-Control"], "Incorrect cache header");

                DateTime date = Convert.ToDateTime(response.Headers["Date"]);
                DateTime expires = Convert.ToDateTime(response.Headers["Expires"]);
                Assert.AreEqual(2.0, expires.Subtract(date).TotalSeconds, OutputCacheMaxDelta, "Incorrect cache duration");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void TestCacheSlidingExpiration()
        {
            string responseText = null;

            ExecuteRequest(
                new Uri(TestURIs.TestProvider_Scenarios + "/binary/GetCitiesWithCaching2"),
                response =>
                {
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        responseText = reader.ReadToEnd();
                    }
                });

            EnqueueDelay(1000);

            ExecuteRequest(
                new Uri(TestURIs.TestProvider_Scenarios + "/binary/GetCitiesWithCaching2"),
                response =>
                {
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        Assert.AreEqual(responseText, reader.ReadToEnd(), "Response was not cached");
                    }
                });

            EnqueueDelay(1000);

            ExecuteRequest(
                new Uri(TestURIs.TestProvider_Scenarios + "/binary/GetCitiesWithCaching2"),
                response =>
                {
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        Assert.AreEqual(responseText, reader.ReadToEnd(), "Response was not cached");
                    }
                });

            // This time wait for 2 seconds such that the cache will be invalidated.
            EnqueueDelay(2000);

            ExecuteRequest(
                new Uri(TestURIs.TestProvider_Scenarios + "/binary/GetCitiesWithCaching2"),
                response =>
                {
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        Assert.AreNotEqual(responseText, reader.ReadToEnd(), "Response was cached");
                    }
                });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Ignore] 
        /// Test is failing in VisualStudioOnline and needs to be investigated
        /// The final WA response test fails with 
        ///  Expected:<4/10/2018 7:20:50 PM>. Actual:<4/10/2018 7:20:51 PM>. Response was not cached
        public void TestCacheAbsoluteExpirationWithParams()
        {
            DateTime date = DateTime.MinValue;
            DateTime expires = DateTime.MinValue;

            string responseText = null;

            ExecuteRequest(new Uri(TestURIs.TestProvider_Scenarios + "/binary/GetCitiesInStateWithCaching?state=WA"), response =>
            {
                Assert.AreEqual("private, max-age=2", response.Headers["Cache-Control"], "Incorrect cache header");

                date = Convert.ToDateTime(response.Headers["Date"]);
                expires = Convert.ToDateTime(response.Headers["Expires"]);
                Assert.AreEqual(2.0, expires.Subtract(date).TotalSeconds, OutputCacheMaxDelta, "Incorrect cache duration");

                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    responseText = reader.ReadToEnd();
                }
            });

            ExecuteRequest(new Uri(TestURIs.TestProvider_Scenarios + "/binary/GetCitiesInStateWithCaching?state=OR"), response =>
            {
                Assert.AreEqual("private, max-age=2", response.Headers["Cache-Control"], "Incorrect cache header");

                DateTime date2 = Convert.ToDateTime(response.Headers["Date"]);
                DateTime expires2 = Convert.ToDateTime(response.Headers["Expires"]);
                Assert.AreEqual(2.0, expires2.Subtract(date2).TotalSeconds, OutputCacheMaxDelta, "Incorrect cache duration");

                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    Assert.AreNotEqual(responseText, reader.ReadToEnd(), "Response was cached");
                }
            });

            EnqueueConditional(() => DateTime.UtcNow.Subtract(date.ToUniversalTime()).TotalSeconds >= 1); // Make sure 1 second has passed since the first request.

            ExecuteRequest(new Uri(TestURIs.TestProvider_Scenarios + "/binary/GetCitiesInStateWithCaching?state=WA"), response =>
            {
                Assert.AreEqual("private, max-age=2", response.Headers["Cache-Control"], "Incorrect cache header");

                Assert.IsTrue(DateTime.UtcNow.Subtract(date.ToUniversalTime()).TotalSeconds <= 2, "Less than 2 seconds should have passed");
                DateTime expires2 = Convert.ToDateTime(response.Headers["Expires"]);
                Assert.AreEqual(expires, expires2, "Response was not cached");

                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    Assert.AreEqual(responseText, reader.ReadToEnd(), "Response different");
                }
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void TestCacheAbsoluteExpirationWithParamsAndQuery()
        {
            ExecuteRequest(new Uri(TestURIs.TestProvider_Scenarios + "/binary/GetCitiesInStateWithCaching?state=WA&$where=StateName%3D\"WA\""), response =>
            {
                Assert.AreEqual("no-cache", response.Headers["Cache-Control"], "Incorrect cache header");
            });

            EnqueueTestComplete();
        }


        [TestMethod]
        [Asynchronous]
        public void TestCacheForInvokeWithNoSideEffects()
        {
            ExecuteRequest(new Uri(TestURIs.TestProvider_Scenarios + "/binary/VoidInvokeWithSideEffectsAndCaching"), null, response =>
            {
                Assert.AreEqual("no-cache", response.Headers["Cache-Control"], "Incorrect cache header");
            },
            "POST");
            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void TestCacheForInvoke()
        {
            ExecuteRequest(new Uri(TestURIs.TestProvider_Scenarios + "/binary/VoidInvokeNoSideEffectsAndNoCaching"), null, response =>
            {
                Assert.AreEqual("no-cache", response.Headers["Cache-Control"], "Incorrect cache header");
            },
            "GET");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [FullTrustTest] // Because OutputCache profiles require full trust.
        public void TestCacheAbsoluteExpirationInvoke()
        {
            ExecuteRequest(new Uri(TestURIs.TestProvider_Scenarios + "/binary/GetCitiesWithCachingInvoke"), null, response =>
            {
                Assert.AreEqual("private, max-age=2", response.Headers["Cache-Control"], "Incorrect cache header");

                DateTime date = Convert.ToDateTime(response.Headers["Date"]);
                DateTime expires = Convert.ToDateTime(response.Headers["Expires"]);
                Assert.AreEqual(2.0, expires.Subtract(date).TotalSeconds, OutputCacheMaxDelta, "Incorrect cache duration");
            },
            "GET");

            EnqueueTestComplete();
        }


        [TestMethod]
        [Asynchronous]
        public void TestInvokeCacheAndThrow()
        {
            ExecuteRequest(new Uri(TestURIs.TestProvider_Scenarios + "/binary/GetCitiesWithCachingAndThrowInvoke"), null, response =>
            {
                Assert.AreEqual("no-cache", response.Headers["Cache-Control"], "Incorrect cache header");
            },
            "GET");

            EnqueueTestComplete();
        }
#endif

        /// <summary>
        /// Verify that if a DomainService throws in its constructor, we 
        /// get back an internal server error.
        /// </summary>
        [TestMethod]
        [Asynchronous]
        public void TestProviderConstructorThrows()
        {
            // first test for a provider that derives from DomainService
            TestDataContext ctxt = new TestDataContext(new Uri(TestURIs.RootURI, "TestDomainServices-ThrowingDomainService.svc"));
            LoadOperation lo = ctxt.Load(ctxt.CreateQuery<Product>("GetProducts", null), false);
            this.EnqueueCompletion(() => lo);
            EnqueueCallback(delegate
            {
                DomainOperationException ex = (DomainOperationException)lo.Error;
#if ASPNETCORE
                // TODO: Determine how to fix this (ignore difference, fix later? or change all invokers to catch this type of error) 
                //  If it fails for WCF hosting it will be caught in DomainOperationInvoker.InvokeAsync and returned as any other openria exception (fault)
                // using binary encoding (HTTP error code will be 500) but DomainOperationException will be NotSupported since the constructor throws NotSupportedException
                // For ASPNETCORE the exception is not handled at all, giving a http 500 error (with "unparsable" text response by developer middleware)

                Assert.AreEqual(OperationErrorStatus.ServerError, ex.Status);
                Assert.Inconclusive("TODO: Determine what behaviour we want");
#else
                Assert.AreEqual(OperationErrorStatus.NotSupported, ex.Status);
                Assert.AreEqual(string.Format(Resource.DomainContext_LoadOperationFailed, "GetProducts", "Can't construct this type."), ex.Message);
#endif
            });

            EnqueueTestComplete();
        }

#if !ASPNETCORE
        /// <summary>
        /// Verify that errors are propagated correctly for L2S DomainService
        /// </summary>
        [TestMethod]
        [Asynchronous]
        [TestCategory("Linq2Sql")]
        public void TestL2SProviderConstructorThrows()
        {
            // Test for a provider that derives from LinqToSqlDomainService, since LinqToSqlDomainService is instantiated differently
            TestDataContext ctxt = new TestDataContext(new Uri(TestURIs.RootURI, "TestDomainServices-ThrowingDomainServiceL2S.svc"));
            LoadOperation lo = ctxt.Load(ctxt.CreateQuery<Product>("GetProducts", null), false);
            this.EnqueueCompletion(() => lo);
            EnqueueCallback(delegate
            {
                DomainOperationException ex = (DomainOperationException)lo.Error;
                Assert.IsNotNull(ex);
                Assert.AreEqual(OperationErrorStatus.NotSupported, ex.Status);
                Assert.AreEqual(string.Format(Resource.DomainContext_LoadOperationFailed, "GetProducts", "Couldn't construct this type."), ex.Message);
            });

            EnqueueTestComplete();
        }
#endif

        /// <summary>
        /// Verify that if an invalid DomainService name is specified, that the Load
        /// operation finishes with the expected WebResponse.StatusCode.
        /// </summary>
        [TestMethod]
        [Asynchronous]
        public void TestInvalidProviderName()
        {
            // first test an invalid name
            TestDataContext ctxt = new TestDataContext(new Uri(TestURIs.RootURI, "TestDomainServices-DNE.svc"));
            LoadOperation lo = ctxt.Load(ctxt.CreateQuery<Product>("NonExistentMethod", null), false);
            this.EnqueueCompletion(() => lo);
            EnqueueCallback(delegate
            {
                Assert.IsNotNull(lo.Error);
                Assert.IsTrue(lo.Error.Message.Contains("Load operation failed"));
            });

            // now test an empty name (just the service path is specified w/o a DomainService name)
            EnqueueCallback(delegate
            {
                ctxt = new TestDataContext(TestURIs.RootURI);
                lo = ctxt.Load(ctxt.CreateQuery<Product>("NonExistentMethod", null), false);
            });
            this.EnqueueCompletion(() => lo);
            EnqueueCallback(delegate
            {
                Assert.IsNotNull(lo.Error);
                Assert.IsTrue(lo.Error.Message.Contains("Load operation failed"));
            });

            EnqueueTestComplete();
        }

        /// <summary>
        /// Verify that if an invalid DomainOperationEntry name is specified, that the Load
        /// operation finishes with the expected WebResponse.StatusCode.
        /// </summary>
#if ASPNETCORE
        [Ignore("Does not work the same way with AspNetCore")]
#endif
        [TestMethod]
        public void TestInvalidMethodName()
        {
            TestDataContext ctxt = new TestDataContext(TestURIs.LTS_Catalog);
            ExceptionHelper.ExpectException<MissingMethodException>(delegate
            {
                ctxt.Load(ctxt.CreateQuery<Product>("DNE", null), false);
            }, String.Format(Resource.WebDomainClient_OperationDoesNotExist, "DNE"));
        }

        /// <summary>
        /// Verify that an existing DomainService is not accessible by the client if
        /// it is not publically exposed via the DomainService.EnableServiceAccess property.
        /// </summary>
        [TestMethod]
        [Asynchronous]
        public void TestProviderAccessibility()
        {
            TestDataContext ctxt = new TestDataContext(new Uri(TestURIs.RootURI, "TestDomainServices-InaccessibleProvider.svc"));
            LoadOperation lo = ctxt.Load(ctxt.CreateQuery<Product>("GetProducts", null), false);
            this.EnqueueCompletion(() => lo);
            EnqueueCallback(delegate
            {
                Assert.IsNotNull(lo.Error);
                // TODO: Assert proper error message... Note: WCF error messages differ between desktop and Silverlight.
                //Assert.IsTrue(lo.Error.InnerException.Message.StartsWith("There was no endpoint listening"));
                //Assert.AreEqual(OperationErrorStatus.NotFound, ex.Status);
                //Assert.AreEqual(Resource.DomainClient_ResourceNotFound, ex.Message);
            });
            EnqueueTestComplete();
        }

        /// <summary>
        /// Try to access a type that doesn't derive from DomainService
        /// </summary>
        [TestMethod]
        [Asynchronous]
        public void TestNonDomainService()
        {
            TestDataContext ctxt = new TestDataContext(new Uri(TestURIs.RootURI, "TestDomainServices-NonDomainService.svc"));
            LoadOperation lo = ctxt.Load(ctxt.CreateQuery<Product>("GetProducts", null), false);
            this.EnqueueCompletion(() => lo);
            EnqueueCallback(delegate
            {
                Assert.IsNotNull(lo.Error);
                // TODO: Assert proper error message... Note: WCF error messages differ between desktop and Silverlight.
                //Assert.IsTrue(lo.Error.InnerException.Message.StartsWith("There was no endpoint listening"));
                //Assert.AreEqual(OperationErrorStatus.NotFound, ex.Status);
                //Assert.AreEqual(Resource.DomainClient_ResourceNotFound, ex.Message);
            });
            EnqueueTestComplete();
        }

#if !ASPNETCORE // TODO: Look into how to handles RequiresSecureEndpoint
// TODO: Maybe obsolete it for netstandard/NET6+ or add check to "OperationInvoker.CreateDomainService" that HttpContext.IsHttps is true if property is true?

        [TestMethod]
        [Asynchronous]
        [Description("Ensures the DataFactory does not find a secure service when invoked over HTTP.")]
        public void InvokingHttpsServiceOverHttpFails()
        {
            TestService_RequiresSecureEndpoint service = new TestService_RequiresSecureEndpoint(new WebDomainClient<TestService_RequiresSecureEndpoint.ITestService_RequiresSecureEndpointContract>(TestURIs.TestService_RequiresSecureEndpoint));

            LoadOperation lo = service.Load(service.GetTestEntitiesQuery(), false);

            this.EnqueueCompletion(() => lo);

            EnqueueCallback(() =>
            {
                DomainOperationException ex = (DomainOperationException)lo.Error;
                Assert.IsNotNull(ex,
                    "HTTPS service should not have been found over an HTTP connection.");
                Assert.AreEqual(OperationErrorStatus.ServerError, ex.Status,
                    "Operation status should indicate the service was not accessible over HTTP.");
            });

            EnqueueTestComplete();
        }
#endif
        private void ExecuteRequest(Uri uri, Action<HttpWebResponse> responseCallback)
        {
            ExecuteRequest(uri, /* buildRequest */ null, responseCallback);
        }

        private void ExecuteRequest(Uri uri, Action<HttpWebRequest> buildRequest, Action<HttpWebResponse> responseCallback, string method = "GET")
        {
            bool hasResponse = false;
            HttpWebResponse response = null;

            EnqueueCallback(delegate
            {
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(uri);
                request.Method = method;

                if (request.Method == "POST")
                    request.ContentLength = 0;

                buildRequest?.Invoke(request);
                request.BeginGetResponse(delegate (IAsyncResult asyncResult)
                {
                    try
                    {
                        response = (HttpWebResponse)request.EndGetResponse(asyncResult);
                    }
                    catch (WebException ex)
                    {
                        response = (HttpWebResponse)ex.Response;
                    }
                    finally
                    {
                        hasResponse = true;
                    }
                }, null);
            });

            EnqueueConditional(() => hasResponse);
            EnqueueCallback(delegate
            {
                Assert.IsNotNull(response, "Didn't receive a response");
                responseCallback(response);
            });
        }
    }
}
