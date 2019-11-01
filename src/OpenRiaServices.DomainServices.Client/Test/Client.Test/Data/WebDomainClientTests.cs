extern alias SSmDsClient;
extern alias SSmDsWeb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Channels;
using Cities;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Silverlight.Testing;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using SSmDsWeb::OpenRiaServices.DomainServices.Client.Web.Behaviors;

namespace OpenRiaServices.DomainServices.Client.Test
{
    using AsyncResultBase = SSmDsClient::OpenRiaServices.DomainServices.Client.AsyncResultBase;
    using Resource = SSmDsClient::OpenRiaServices.DomainServices.Client.Resource;
    using Resources = SSmDsClient::OpenRiaServices.DomainServices.Client.Resources;


    /// <summary>
    /// Tests <see cref="WebDomainClient&lt;TContract&gt;"/> members.
    /// </summary>
    [TestClass]
    public class WebDomainClientTests : UnitTestBase
    {
        public Exception Error
        {
            get;
            set;
        }

        public WebDomainClient<CityDomainContext.ICityDomainServiceContract> DomainClient
        {
            get
            {
                return (WebDomainClient<CityDomainContext.ICityDomainServiceContract>)this.CityDomainContext.DomainClient;
            }
        }

        public CityDomainContext CityDomainContext
        {
            get;
            set;
        }

        public InvokeCompletedResult InvokeCompletedResults
        {
            get;
            set;
        }

        public QueryCompletedResult QueryCompletedResults
        {
            get;
            set;
        }

        public SubmitCompletedResult SubmitCompletedResults
        {
            get;
            set;
        }

        [TestInitialize]
        public void TestInitialize()
        {
            this.Error = null;
            this.InvokeCompletedResults = null;
            this.QueryCompletedResults = null;
            this.SubmitCompletedResults = null;
            this.CreateDomainContext();
        }

        [TestMethod]
        [Description("Tests that an absolute ServiceUri set in the constructor is accessible.")]
        public void AbsoluteServiceUri()
        {
            Uri uri = new Uri(@"http://mock.domain/ignored/");
            WebDomainClient<CityDomainContext.ICityDomainServiceContract> client = new WebDomainClient<CityDomainContext.ICityDomainServiceContract>(uri, /* usesHttp */ false);
            Assert.AreEqual(uri, client.ServiceUri,
                "Absoluted Uri should be the same as passed into the constructor.");
        }

        [TestMethod]
        [Description("Tests that constructors raise ArgumentNullExceptions when passed a null Uri.")]
        public void ConstructorsThrowOnNullUri()
        {
            ExceptionHelper.ExpectArgumentNullException(
                () => new WebDomainClient<CityDomainContext.ICityDomainServiceContract>(null), "serviceUri");
            ExceptionHelper.ExpectArgumentNullException(
                () => new WebDomainClient<CityDomainContext.ICityDomainServiceContract>(null, false), "serviceUri");
        }

        [TestMethod]
        [Description("Tests that the UsesHttps property set in the constructor sets the scheme for relative uris.")]
        public void UsesHttps()
        {
#if SILVERLIGHT
            WebDomainClient<CityDomainContext.ICityDomainServiceContract> client = new WebDomainClient<CityDomainContext.ICityDomainServiceContract>(new Uri("ignored/", UriKind.Relative));
            Assert.AreEqual("http", client.ServiceUri.Scheme,
                "By default, Uri scheme should be HTTP.");

            client = new WebDomainClient<CityDomainContext.ICityDomainServiceContract>(new Uri("ignored/", UriKind.Relative), true);
            Assert.AreEqual("https", client.ServiceUri.Scheme,
                "Uri scheme should be HTTPS.");
#endif
        }

        [TestMethod]
        [Description("Tests that a relative ServiceUri set in the constructor is accessible as an absolute Uri and respects usesHttps.")]
        public void RelativeServiceUri()
        {
#if SILVERLIGHT
            Uri relativeUri = new Uri("relative/", UriKind.Relative);
            WebDomainClient<CityDomainContext.ICityDomainServiceContract> client = new WebDomainClient<CityDomainContext.ICityDomainServiceContract>(relativeUri);
            Assert.AreNotEqual(relativeUri, client.ServiceUri,
                "Relative Uri should not equal the absolute service uri.");
            Assert.IsTrue(client.ServiceUri.AbsoluteUri.Contains(@"http://"),
                "Absolute Uri should use HTTP scheme.");
            Assert.IsTrue(client.ServiceUri.AbsoluteUri.Contains(relativeUri.OriginalString),
                "Absolute Uri should contain the full path of the relative Uri.");

            client = new WebDomainClient<CityDomainContext.ICityDomainServiceContract>(relativeUri, true);
            Assert.AreNotEqual(relativeUri, client.ServiceUri,
                "Relative Uri should not equal the absolute service Uri.");
            Assert.IsTrue(client.ServiceUri.AbsoluteUri.Contains(@"https://"),
                "Absolute Uri should use HTTPS scheme.");
            Assert.IsTrue(client.ServiceUri.AbsoluteUri.Contains(relativeUri.OriginalString),
                "Absolute Uri should contain the full path of the relative Uri.");
#endif
        }

        [TestMethod]
        [Description("Tests that constructor accepts absolute serviceUri in combination with usesHttps = true.")]
        public void ConstructorDoesNotThrowOnAbsoluteUriWithHttps()
        {
            new WebDomainClient<CityDomainContext.ICityDomainServiceContract>(new Uri(@"http://mock.domain/ignored/"), usesHttps: true);
        }

        [TestMethod]
        [Asynchronous]
        public void Invoke_DefaultBehavior()
        {
            IAsyncResult asyncResult = null;
            this.EnqueueCallback(() =>
            {
                asyncResult = this.BeginInvokeRequest();
            });
            this.EnqueueConditional(() => asyncResult.IsCompleted && this.InvokeCompletedResults != null);
            this.EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void Invoke_CancellationBehavior()
        {
            Task<InvokeCompletedResult> asyncResult = null;
            this.EnqueueCallback(() =>
            {
                asyncResult = this.BeginInvokeRequestAndCancel();
            });
            this.EnqueueConditional(() => asyncResult.IsCompleted);
            this.EnqueueCallback(() =>
            {
                Assert.IsTrue(asyncResult.IsCanceled, "Task should be cancelled");
            });
            this.EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void Query_DefaultBehavior()
        {
            IAsyncResult asyncResult = null;
            this.EnqueueCallback(() =>
            {
                asyncResult = this.BeginQueryRequest();
            });
            this.EnqueueConditional(() => asyncResult.IsCompleted && this.QueryCompletedResults != null);
            this.EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void Query_CancellationBehavior()
        {
            Task<QueryCompletedResult> asyncResult = null;
            this.EnqueueCallback(() =>
            {
                asyncResult = this.BeginQueryRequestAndCancel();
            });
            this.EnqueueConditional(() => asyncResult.IsCompleted);
            this.EnqueueCallback(() =>
            {
                Assert.IsTrue(asyncResult.IsCanceled, "Task should be cancelled");
            });
            this.EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void Submit_DefaultBehavior()
        {
            Task<SubmitCompletedResult> submitTask = this.BeginSubmitRequest();

            this.EnqueueConditional(() => submitTask.IsCompleted);
            this.EnqueueCallback(() =>
            {
                Assert.IsNotNull(this.SubmitCompletedResults, "Missing submit result");
                // Validate that we got back all of our entity operations, even though the entity didn't get any new values on the server.
                Assert.AreEqual(1, this.SubmitCompletedResults.Results.Count());
            });
            this.EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void Submit_CancellationBehavior()
        {
            Task<SubmitCompletedResult> submitTask = null;
            this.EnqueueCallback(() =>
            {
                submitTask = this.BeginSubmitRequestAndCancel();
            });
            this.EnqueueConditional(() => submitTask.IsCompleted);
            this.EnqueueCallback(() =>
            {
                Assert.IsTrue(submitTask.IsCanceled, "Task should be cancelled");
            });
            this.EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void Query_MaximumUriLengthExceeded()
        {
            OperationBase op = null;
            Exception error = null;

            this.EnqueueCallback(() =>
            {
                this.CreateDomainContext(WebDomainClientTests.GenerateUriBase(2066)); // --> 2083, the max length
                op = this.CityDomainContext.Load(
                    this.CityDomainContext.GetCitiesQuery(),
                    (lo) => WebDomainClientTests.HandleError(lo, ref error),
                    null);
            });
            this.EnqueueConditional(() => op.IsComplete);
            this.EnqueueCallback(() =>
            {
                // Expect a 'Not Found'
                Assert.IsInstanceOfType(error, typeof(DomainOperationException));
                Assert.IsInstanceOfType(error.InnerException, typeof(CommunicationException));

                this.CreateDomainContext(WebDomainClientTests.GenerateUriBase(2067)); // --> 2084, one over the max length
                ExceptionHelper.ExpectException<InvalidOperationException>(() =>
                    this.CityDomainContext.Load(this.CityDomainContext.GetCitiesQuery()));
            });
            this.EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void Query_ShouldSupportLongQueries()
        {
            LoadOperation<Zip> op = null;
            Exception error = null;
            const int zipToFind = 98053;
            const int QUERY_ITERATIONS = 50;

            this.EnqueueCallback(() =>
            {
                this.CreateDomainContext();

                // Generate a really long query
                // The load will result in a query where just the query part has length > 3000
                var query = CityDomainContext.GetZipsQuery();

                // Create a query with QUERY_ITERATIONS where statements checking a range of QUERY_ITERATIONS each
                // this should in the end if simplified result in Code = zipToFind (zipToFind - 1 < Code  <= zipToFind)
                for (int i = 0; i < QUERY_ITERATIONS; ++i)
                {
                    int min = zipToFind + i - QUERY_ITERATIONS;
                    int max = zipToFind + i;
                    query = query.Where(c => min < c.Code && c.Code <= max);
                }

                op = this.CityDomainContext.Load(query,
                    (lo) => WebDomainClientTests.HandleError(lo, ref error),
                    null);
            });
            this.EnqueueConditional(() => op.IsComplete);
#if SILVERLIGHT
            this.EnqueueCallback(() =>
            {
                // The query should match a single zip 
                //new Zip() { Code=98053, FourDigit=8625, CityName="Redmond", CountyName="King", StateName="WA" },
                Assert.IsFalse(op.HasError, string.Format("The query returned the following error: {0}", op.Error));
                Assert.AreEqual(1, op.Entities.Count(), "A single entity was expected");

                var zip = op.Entities.First();
                Assert.AreEqual(zipToFind, zip.Code, "A single entity was expected");
                Assert.AreEqual(8625, zip.FourDigit);
                Assert.AreEqual("Redmond", zip.CityName);
            });
#endif
            this.EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void Invoke_MaximumUriLengthExceeded()
        {
            OperationBase op = null;
            Exception error = null;

            this.EnqueueCallback(() =>
            {
                this.CreateDomainContext(WebDomainClientTests.GenerateUriBase(2071)); // --> 2083, the max length
                op = this.CityDomainContext.Echo(
                    "",
                    (io) => WebDomainClientTests.HandleError(io, ref error),
                    null);
            });
            this.EnqueueConditional(() => op.IsComplete);
            this.EnqueueCallback(() =>
            {
                // Expect a 'Not Found'
                Assert.IsInstanceOfType(error, typeof(DomainOperationException));
                Assert.IsInstanceOfType(error.InnerException, typeof(CommunicationException));

                this.CreateDomainContext(WebDomainClientTests.GenerateUriBase(2072)); // --> 2084, one over the max length
                ExceptionHelper.ExpectException<InvalidOperationException>(() =>
                    this.CityDomainContext.Echo(""));
            });
            this.EnqueueTestComplete();
        }

        private static void HandleError(OperationBase op, ref Exception error)
        {
            if (op.HasError)
            {
                error = op.Error;
                op.MarkErrorAsHandled();
            }
        }

        private Task<InvokeCompletedResult> BeginInvokeRequest(CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, object>();
            parameters.Add("msg", "foo");

            InvokeArgs invokeArgs = new InvokeArgs("Echo", typeof(string), parameters, true /*hasSideEffects*/);
            var result = this.DomainClient.InvokeAsync(invokeArgs, cancellationToken)
                .ContinueWith(task =>
                {
                    try
                    {
                        InvokeCompletedResult res = task.GetAwaiter().GetResult();
                        this.InvokeCompletedResults = res;
                        return res;
                    }
                    catch (Exception ex)
                    {
                        this.Error = ex;
                        throw;
                    }
                }
                , TaskContinuationOptions.NotOnCanceled);
            this.AssertInProgress(result);
            return result;
        }

        private Task<InvokeCompletedResult> BeginInvokeRequestAndCancel()
        {
            var cts = new CancellationTokenSource();
            var task = this.BeginInvokeRequest(cts.Token);
            cts.Cancel();
            return task;
        }

        private Task<QueryCompletedResult> BeginQueryRequest(CancellationToken cancellationToken = default)
        {
            var result = this.DomainClient.QueryAsync(new EntityQuery<Zip>(this.DomainClient, "GetZips", null, true, false), cancellationToken);
            this.AssertInProgress(result);
            return result.ContinueWith(task =>
            {
                try
                {
                    QueryCompletedResult res = task.GetAwaiter().GetResult();
                    this.QueryCompletedResults = res;
                    return res;
                }
                catch (Exception ex)
                {
                    this.Error = ex;
                    throw;
                }
            }, TaskContinuationOptions.NotOnCanceled);
        }

        private Task<QueryCompletedResult> BeginQueryRequestAndCancel()
        {
            var cts = new CancellationTokenSource();
            var task = this.BeginQueryRequest(cts.Token);
            cts.Cancel();
            return task;
        }

        private Task<SubmitCompletedResult> BeginSubmitRequest(CancellationToken cancellationToken = default)
        {
            this.CityDomainContext.Cities.Add(new City() { Name = "TestCity", StateName = "ZZ" });
            var result = this.DomainClient.SubmitAsync(this.CityDomainContext.EntityContainer.GetChanges(), cancellationToken);
            this.AssertInProgress(result);

            return result.ContinueWith(task =>
            {
                try
                {
                    SubmitCompletedResult res = task.GetAwaiter().GetResult();
                    this.SubmitCompletedResults = res;
                    return res;
                }
                catch (Exception ex)
                {
                    this.Error = ex;
                    throw;
                }
            }
            , TaskContinuationOptions.NotOnCanceled);
        }

        private Task<SubmitCompletedResult> BeginSubmitRequestAndCancel()
        {
            var cts = new CancellationTokenSource();
            var task = this.BeginSubmitRequest(cts.Token);
            cts.Cancel();
            return task;
        }

        private static Uri GenerateUriBase(int length)
        {
            string template = TestURIs.RootURI.OriginalString + "{0}/";
            return new Uri(string.Format(template, new string('0', length - template.Length + 3)), UriKind.Absolute);
        }

        private void CreateDomainContext()
        {
            this.CreateDomainContext(TestURIs.Cities);
        }

        // Class to testthe channel factory extensibility.
        class CustomDomainClientFactory : Web.WcfDomainClientFactory
        {
            protected override Binding CreateBinding(Uri endpoint, bool requiresSecureEndpoint)
            {
                return new CustomBinding(
                        new PoxBinaryMessageEncodingBindingElement(),
                        new HttpTransportBindingElement()
                        {
                            ManualAddressing = true
                        });
            }

            protected override EndpointAddress CreateEndpointAddress(Uri endpoint, bool requiresSecureEndpoint)
            {
                return new EndpointAddress(
                        new Uri(endpoint.OriginalString + "/binary", UriKind.Absolute));
            }

            protected override ChannelFactory<TContract> CreateChannelFactory<TContract>(Uri endpoint, bool requiresSecureEndpoint)
            {
                var factory = base.CreateChannelFactory<TContract>(endpoint, requiresSecureEndpoint);
                factory.Endpoint.EndpointBehaviors.Add(new WebDomainClientWebHttpBehavior()
                {
                    DefaultBodyStyle = System.ServiceModel.Web.WebMessageBodyStyle.Wrapped
                });
                return factory;
            }
        }

        private void CreateDomainContext(Uri uri)
        {
            // Do not remove this code; it's used to test the channel factory extensibility.
            var factory = new CustomDomainClientFactory();

            this.CityDomainContext =
                new CityDomainContext(
                    new WebDomainClient<CityDomainContext.ICityDomainServiceContract>(uri, usesHttps: false, factory)
                    {
                        EntityTypes = new List<Type>() { typeof(City), typeof(Zip) }
                    });
        }

        private void AssertInProgress(IAsyncResult asyncResult)
        {
            Assert.IsNotNull(asyncResult, "Expected an instance of IAsyncResult");
            Assert.IsFalse(asyncResult.IsCompleted);
            Assert.IsFalse(asyncResult.CompletedSynchronously);
        }
    }

    /// <summary>
    /// Tests <see cref="WebDomainClient&lt;TContract&gt;"/> globalization.
    /// </summary>
    [TestClass]
    public class WebDomainClientTests_Globalization : UnitTestBase
    {
        private CultureInfo _defaultCulture;

        [TestInitialize]
        public void SetUp()
        {
            _defaultCulture = Thread.CurrentThread.CurrentCulture;
        }

        [TestCleanup]
        public void TearDown()
        {
            Thread.CurrentThread.CurrentCulture = _defaultCulture;
        }

        [TestMethod]
        public void Invoke_MaximumUriLengthExceeded()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");
            CityDomainContext dc = new CityDomainContext(GenerateUriBase(1000)); // --> the length when localized to Turkish is > 2083
            ExceptionHelper.ExpectException<InvalidOperationException>(() => dc.Echo(""),
                String.Format(SSmDsWeb::OpenRiaServices.DomainServices.Client.Resource.WebDomainClient_MaximumUriLengthExceeded, 2083));
        }

        [TestMethod]
        public void Query_MaximumUriLengthExceeded()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");
            CityDomainContext dc = new CityDomainContext(GenerateUriBase(1000)); // --> the length when localized to Turkish is > 2083
            ExceptionHelper.ExpectException<InvalidOperationException>(() => dc.Load(dc.GetCitiesQuery()),
                String.Format(SSmDsWeb::OpenRiaServices.DomainServices.Client.Resource.WebDomainClient_MaximumUriLengthExceeded, 2083));
        }

        private static Uri GenerateUriBase(int length)
        {
            string template = TestURIs.RootURI.OriginalString + "{0}/";
            return new Uri(string.Format(template, new string('i', length - template.Length + 3)).ToUpper(), UriKind.Absolute);
        }
    }
}
