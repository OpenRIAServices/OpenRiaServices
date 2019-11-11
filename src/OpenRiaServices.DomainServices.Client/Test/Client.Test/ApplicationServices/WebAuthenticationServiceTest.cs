extern alias SSmDsClient;
extern alias SSmDsWeb;

using System;
using System.Security.Principal;
using OpenRiaServices.DomainServices.Client.Test;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Silverlight.Testing;
using DescriptionAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;
using MockUser = OpenRiaServices.DomainServices.Client.ApplicationServices.Test.AuthenticationDomainClient.MockUser;
using UserType = OpenRiaServices.DomainServices.Client.ApplicationServices.Test.AuthenticationDomainClient.UserType;

namespace OpenRiaServices.DomainServices.Client.ApplicationServices.Test
{
    using System.Threading;
    using System.Threading.Tasks;
#if SILVERLIGHT
    using Resource = SSmDsWeb::OpenRiaServices.DomainServices.Client.Resource;
#else
    using Resource = SSmDsClient::OpenRiaServices.DomainServices.Client.Resource;
#endif


    /// <summary>
    /// Tests <see cref="WebAuthenticationService"/> members.
    /// </summary>
    [TestClass]
    public class WebAuthenticationServiceTest : UnitTestBase
    {
        #region Mock

        private class MockWebAuthenticationService : WebAuthenticationService
        {
            public IPrincipal CreateDefaultUserMock()
            {
                return base.CreateDefaultUser();
            }

            protected internal override Task<LoadUserResult> LoadUserAsync(CancellationToken cancellationToken)
            {
                return base.LoadUserAsync(cancellationToken);
            }

            protected internal override Task<LoginResult> LoginAsync(LoginParameters parameters, CancellationToken cancellationToken)
            {
                return base.LoginAsync(parameters, cancellationToken);
            }

            protected internal override Task<LogoutResult> LogoutAsync(CancellationToken cancellationToken)
            {
                return base.LogoutAsync(cancellationToken);
            }

            protected internal override Task<SaveUserResult> SaveUserAsync(IPrincipal user, CancellationToken cancellationToken)
            {
                return base.SaveUserAsync(user, cancellationToken);
            }
        }

#endregion

        private const int Delay = 200;
        private const string ErrorMessage = "There was an error";

        // TODO:
        // Deserialize default user: would require setting InitParams for the application
        // Resolve does not find a provider: would require adding an authentication domain service to the application

        [TestMethod]
        [Description("Tests that the DomainContext can be accessed and mutated throughout the service lifecycle.")]
        public void DomainContext()
        {
            AuthenticationDomainContext mock = new AuthenticationDomainContext();
            MockWebAuthenticationService service = new MockWebAuthenticationService();

            service.DomainContext = mock;

            Assert.AreEqual(mock, service.DomainContext,
                "DomainContexts should be equal.");

            WebAuthenticationServiceTest.InitializeService(service);

            Assert.AreEqual(mock, service.DomainContext,
                "DomainContexts should be equal.");

            ExceptionHelper.ExpectException<InvalidOperationException>(
                () => service.DomainContext = null);
        }

        [TestMethod]
        [Description("Tests that the DomainContextType can be accessed and mutated throughout the service lifecycle.")]
        public void DomainContextType()
        {
            string mockType = typeof(AuthenticationDomainContext).FullName;
            MockWebAuthenticationService service = new MockWebAuthenticationService();

            service.DomainContextType = typeof(AuthenticationDomainContext).AssemblyQualifiedName;

            WebAuthenticationServiceTest.InitializeService(service);

            Assert.IsInstanceOfType(service.DomainContext, typeof(AuthenticationDomainContext),
                "DomainContext should be a MockDomainContext.");

            service = new MockWebAuthenticationService();

            ExceptionHelper.ExpectException<InvalidOperationException>(
                () => WebAuthenticationServiceTest.InitializeService(service));
        }

        [TestMethod]
        [Description("Tests that the DefaultUser can be accessed throughout the service lifecycle.")]
        public void DefaultUser()
        {
            AuthenticationDomainContext mock = new AuthenticationDomainContext();
            MockWebAuthenticationService service = new MockWebAuthenticationService();

            service.DomainContext = mock;

            IPrincipal defaultUser = service.CreateDefaultUserMock();

            Assert.IsInstanceOfType(defaultUser, typeof(MockUser),
                "DefaultUser should be of type MockUser after service has started.");
        }

        [TestMethod]
        [Description("Tests that BeginSaveUser throws when saving an anonymous user.")]
        public async Task SaveAnonymousUserThrows()
        {
            AuthenticationDomainContext mock = new AuthenticationDomainContext();
            MockWebAuthenticationService service = new MockWebAuthenticationService();

            service.DomainContext = mock;

            await ExceptionHelper.ExpectExceptionAsync<InvalidOperationException>(
                () => service.SaveUserAsync(service.User, CancellationToken.None));
        }

        [TestMethod]
        [Description("Tests that invoking BeginLogin with null parameters throws an ArgumentNullException.")]
        public void LoginWithNullParametersThrows()
        {
            AuthenticationDomainContext mock = new AuthenticationDomainContext();
            MockWebAuthenticationService service = new MockWebAuthenticationService();

            service.DomainContext = mock;

            ExceptionHelper.ExpectArgumentNullExceptionStandard(
                () => service.LoginAsync(null, CancellationToken.None), "parameters");
        }

        private static void InitializeService(MockWebAuthenticationService service)
        {
            Assert.IsNotNull(service.User,
                "We're really just initializing the service.");
        }

        // TODO: Fix tests
#if FALSE
        // Asynchronous, Cancel, Error, Synchronous
#region Async

        [TestMethod]
        [Asynchronous]
        [Description("Tests that Login completes successfully.")]
        public void LoginAsync()
        {
            LoginParameters parameters = new LoginParameters(AuthenticationDomainClient.ValidUserName, string.Empty);

            this.AsyncTemplate(
            (mock, service, callback, state) =>
            {
                return service.BeginLoginMock(parameters, callback, state);
            },
            (mock, service, asyncResult) =>
            {
                LoginResult result = service.EndLoginMock(asyncResult);
                Assert.IsNotNull(result,
                    "LoginResults should not be null.");
                Assert.IsTrue(result.LoginSuccess,
                    "LoginSuccess should be true.");
                Assert.AreEqual(UserType.LoggedIn, ((MockUser)result.User).Type,
                    "User should be of type LoggedIn.");
            });
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that Login completes with failure.")]
        public void LoginFailedAsync()
        {
            LoginParameters parameters = new LoginParameters(AuthenticationDomainClient.InvalidUserName, string.Empty);

            this.AsyncTemplate(
            (mock, service, callback, state) =>
            {
                return service.BeginLoginMock(parameters, callback, state);
            },
            (mock, service, asyncResult) =>
            {
                LoginResult result = service.EndLoginMock(asyncResult);
                Assert.IsNotNull(result,
                    "LoginResults should not be null.");
                Assert.IsFalse(result.LoginSuccess,
                    "LoginSuccess should be false.");
                Assert.IsNull(result.User,
                    "User should be null.");
            });
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that Logout completes successfully.")]
        public void LogoutAsync()
        {
            this.AsyncTemplate(
            (mock, service, callback, state) =>
            {
                return service.BeginLogoutMock(callback, state);
            },
            (mock, service, asyncResult) =>
            {
                AuthenticationResult result = service.EndLogoutMock(asyncResult);
                Assert.IsNotNull(result,
                    "LogoutResults should not be null.");
                Assert.IsInstanceOfType(result.User, typeof(MockUser),
                    "User should be a MockUser");
                Assert.AreEqual(UserType.LoggedOut, ((MockUser)result.User).Type,
                    "User should be of type LoggedOut.");
            });
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that LoadUser completes successfully.")]
        public void LoadUserAsync()
        {
            this.AsyncTemplate(
            (mock, service, callback, state) =>
            {
                return service.BeginLoadUserMock(callback, state);
            },
            (mock, service, asyncResult) =>
            {
                AuthenticationResult result = service.EndLoadUserMock(asyncResult);
                Assert.IsNotNull(result,
                    "LoadUserResults should not be null.");
                Assert.IsInstanceOfType(result.User, typeof(MockUser),
                    "User should be a MockUser");
                Assert.AreEqual(UserType.Loaded, ((MockUser)result.User).Type,
                    "User should be of type Loaded.");
            });
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that SaveUser completes successfully.")]
        public void SaveUserAsync()
        {
            this.AsyncTemplate(
            (mock, service, callback, state) =>
            {
                ((MockUser)service.User).Name = "User";
                ((MockUser)service.User).Modify(mock);
                return service.BeginSaveUserMock(service.User, callback, state);
            },
            (mock, service, asyncResult) =>
            {
                AuthenticationResult result = service.EndSaveUserMock(asyncResult);
                Assert.IsNotNull(result,
                    "SaveUserResults should not be null.");
                Assert.IsInstanceOfType(result.User, typeof(MockUser),
                    "User should be a MockUser");
                Assert.AreEqual(UserType.Saved, ((MockUser)result.User).Type,
                    "User should be of type Saved.");
                Assert.IsTrue(mock.DomainClient.Submitted,
                    "User should have been submitted.");
            });
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that SaveUser completes successfully.")]
        public void SaveUserEmptyAsync()
        {
            this.AsyncTemplate(
            (mock, service, callback, state) =>
            {
                ((MockUser)service.User).Name = "User";
                return service.BeginSaveUserMock(service.User, callback, state);
            },
            (mock, service, asyncResult) =>
            {
                AuthenticationResult result = service.EndSaveUserMock(asyncResult);
                Assert.IsNotNull(result,
                    "SaveUserResults should not be null.");
                Assert.IsNull(result.User,
                    "User should be null.");
                Assert.IsFalse(mock.DomainClient.Submitted,
                    "User should not have been submitted.");
            });
        }

#endregion

#region Cancel

        [TestMethod]
        [Asynchronous]
        [Description("Tests that Login cancels successfully.")]
        public void LoginCancel()
        {
            LoginParameters parameters = new LoginParameters(AuthenticationDomainClient.ValidUserName, string.Empty);

            this.CancelTemplate(
            (mock, service, callback, state) =>
            {
                return service.BeginLoginMock(parameters, callback, state);
            },
            (mock, service, asyncResult) =>
            {
                service.CancelLoginMock(asyncResult);
            },
            (mock, service, asyncResult) =>
            {
                ExceptionHelper.ExpectException<InvalidOperationException>(
                    () => Assert.IsNull(service.EndLoginMock(asyncResult), "This should fail."));
            });
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that Logout cancels successfully.")]
        public void LogoutCancel()
        {
            this.CancelTemplate(
            (mock, service, callback, state) =>
            {
                return service.BeginLogoutMock(callback, state);
            },
            (mock, service, asyncResult) =>
            {
                service.CancelLogoutMock(asyncResult);
            },
            (mock, service, asyncResult) =>
            {
                ExceptionHelper.ExpectException<InvalidOperationException>(
                    () => Assert.IsNull(service.EndLogoutMock(asyncResult), "This should fail."));
            });
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that LoadUser cancels successfully.")]
        public void LoadUserCancel()
        {
            this.CancelTemplate(
            (mock, service, callback, state) =>
            {
                return service.BeginLoadUserMock(callback, state);
            },
            (mock, service, asyncResult) =>
            {
                service.CancelLoadUserMock(asyncResult);
            },
            (mock, service, asyncResult) =>
            {
                ExceptionHelper.ExpectException<InvalidOperationException>(
                    () => Assert.IsNull(service.EndLoadUserMock(asyncResult), "This should fail."));
            });
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that SaveUser cancels successfully.")]
        public void SaveUserCancel()
        {
            this.CancelTemplate(
            (mock, service, callback, state) =>
            {
                ((MockUser)service.User).Name = "User";
                ((MockUser)service.User).Modify(mock);
                return service.BeginSaveUserMock(service.User, callback, state);
            },
            (mock, service, asyncResult) =>
            {
                service.CancelSaveUserMock(asyncResult);
            },
            (mock, service, asyncResult) =>
            {
                ExceptionHelper.ExpectException<InvalidOperationException>(
                    () => Assert.IsNull(service.EndSaveUserMock(asyncResult), "This should fail."));
                Assert.IsFalse(mock.DomainClient.Submitted,
                    "User should not have been submitted.");
            });
        }

#endregion

#region Error

        [TestMethod]
        [Asynchronous]
        [Description("Tests that Login handles errors successfully.")]
        public void LoginError()
        {
            LoginParameters parameters = new LoginParameters(AuthenticationDomainClient.ValidUserName, string.Empty);

            this.ErrorTemplate(
            (mock, service, callback, state) =>
            {
                return service.BeginLoginMock(parameters, callback, state);
            },
            (mock, service, asyncResult) =>
            {
                ExceptionHelper.ExpectException<DomainOperationException>(
                    () => Assert.IsNull(service.EndLoginMock(asyncResult), "This should fail."),
                    string.Format(Resource.DomainContext_LoadOperationFailed, "Login", WebAuthenticationServiceTest.ErrorMessage));
            });
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that Logout handles errors successfully.")]
        public void LogoutError()
        {
            this.ErrorTemplate(
            (mock, service, callback, state) =>
            {
                return service.BeginLogoutMock(callback, state);
            },
            (mock, service, asyncResult) =>
            {
                ExceptionHelper.ExpectException<DomainOperationException>(
                    () => Assert.IsNull(service.EndLogoutMock(asyncResult), "This should fail."),
                    string.Format(Resource.DomainContext_LoadOperationFailed, "Logout", WebAuthenticationServiceTest.ErrorMessage));
            });
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that LoadUser handles errors successfully.")]
        public void LoadUserError()
        {
            this.ErrorTemplate(
            (mock, service, callback, state) =>
            {
                return service.BeginLoadUserMock(callback, state);
            },
            (mock, service, asyncResult) =>
            {
                ExceptionHelper.ExpectException<DomainOperationException>(
                    () => Assert.IsNull(service.EndLoadUserMock(asyncResult), "This should fail."),
                    string.Format(Resource.DomainContext_LoadOperationFailed, "GetUser", WebAuthenticationServiceTest.ErrorMessage));
            });
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that SaveUser handles errors successfully.")]
        public void SaveUserError()
        {
            this.ErrorTemplate(
            (mock, service, callback, state) =>
            {
                ((MockUser)service.User).Name = "User";
                ((MockUser)service.User).Modify(mock);
                return service.BeginSaveUserMock(service.User, callback, state);
            },
            (mock, service, asyncResult) =>
            {
                ExceptionHelper.ExpectException<SubmitOperationException>(
                    () => Assert.IsNull(service.EndSaveUserMock(asyncResult), "This should fail."),
                    string.Format(Resource.DomainContext_SubmitOperationFailed, WebAuthenticationServiceTest.ErrorMessage));
                Assert.IsFalse(mock.DomainClient.Submitted,
                    "User should not have been submitted.");
            });
        }

#endregion
#endif

#region Templates

        private delegate IAsyncResult InvokeCallback(AuthenticationDomainContext mock, MockWebAuthenticationService service, AsyncCallback callback, object state);
        private delegate void TestCallback(AuthenticationDomainContext mock, MockWebAuthenticationService service, IAsyncResult asyncResult);

        private void TestTemplate(InvokeCallback invoke, TestCallback proceed, TestCallback verify, bool verifyInCallback)
        {
            AuthenticationDomainContext mock = new AuthenticationDomainContext();
            MockWebAuthenticationService service = new MockWebAuthenticationService() ;
            service.DomainContext = mock;
            object state = new object();
            
            bool testCompleted = false;
            AsyncCallback asyncCallback = ar =>
            {
                Assert.IsNotNull(ar,
                    "IAsyncResult should not be null.");
                Assert.AreEqual(state, ar.AsyncState,
                    "States should be equal.");
                ExceptionHelper.ExpectException<NotSupportedException>(
                    () => Assert.IsNull(ar.AsyncWaitHandle, "This property is not supported."));
                Assert.IsFalse(ar.CompletedSynchronously,
                    "IAsyncResult should not have completed synchronously.");
                Assert.IsTrue(ar.IsCompleted || mock.DomainClient.CancellationRequested,
                    "IAsyncResult should be complete or cancelled.");

                try
                {
                    verify(mock, service, ar);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("TestTemplate cought exception {0}", ex);
                    var exception = System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex);
                    this.EnqueueCallback(() => { exception.Throw();  });
                }

                testCompleted = true;
            };

            IAsyncResult asyncResult = invoke(mock, service, verifyInCallback ? asyncCallback : null, state);

            Assert.IsNotNull(asyncResult,
                "IAsyncResult should not be null.");
            Assert.AreEqual(state, asyncResult.AsyncState,
                "States should be equal.");
            ExceptionHelper.ExpectException<NotSupportedException>(
                () => Assert.IsNull(asyncResult.AsyncWaitHandle, "This property is not supported."));
            Assert.IsFalse(asyncResult.CompletedSynchronously,
                "IAsyncResult should not have completed synchronously.");

            // We don't have a dispatcher SynchronizationContext when running tests on the
            // full framework, so the operation will complete on another thread which can happen before we reach this assert
            // TODO: Try to get tests to run on a dispatcher by using mstest v2 extensobility
#if SILVERLIGHT
             Assert.IsFalse(asyncResult.IsCompleted,    "IAsyncResult should not be complete.");
#endif

            proceed(mock, service, asyncResult);

            if (!verifyInCallback)
            {
                this.EnqueueCallback(() => asyncCallback(asyncResult));
            }
            this.EnqueueConditional(() => testCompleted);
        }

        private void AsyncTemplate(InvokeCallback invoke, TestCallback verify)
        {
            this.TestTemplate(
                invoke,
                (mock, service, asyncResult) =>
                {
                    mock.DomainClient.RequestCallback();
                    Assert.IsFalse(mock.DomainClient.CancellationRequested,
                        "Result should not be canceled.");
                },
                verify,
                true);
            this.TestTemplate(
                invoke,
                (mock, service, asyncResult) =>
                {
                    mock.DomainClient.RequestCallback(WebAuthenticationServiceTest.Delay);
                    this.EnqueueConditional(() => asyncResult.IsCompleted);
                    this.EnqueueCallback(() =>
                    {
                        Assert.IsFalse(mock.DomainClient.CancellationRequested,
                            "Result should not be canceled.");
                    });
                },
                verify,
                false);
            this.EnqueueTestComplete();
        }

        private void CancelTemplate(InvokeCallback invoke, TestCallback proceed, TestCallback verify)
        {
            this.TestTemplate(
                invoke,
                (mock, service, asyncResult) =>
                {
                    proceed(mock, service, asyncResult);
                    mock.DomainClient.RequestCallback();
                    Assert.IsTrue(mock.DomainClient.CancellationRequested,
                        "Result should be canceled.");
                },
                verify,
                false);
            this.TestTemplate(
                invoke,
                (mock, service, asyncResult) =>
                {
                    proceed(mock, service, asyncResult);
                    mock.DomainClient.RequestCallback(WebAuthenticationServiceTest.Delay);
                    this.EnqueueCallback(() =>
                    {
                        Assert.IsTrue(mock.DomainClient.CancellationRequested,
                            "Result should be canceled.");
                    });
                },
                verify,
                false);
            this.EnqueueTestComplete();
        }

        private void ErrorTemplate(InvokeCallback invoke, TestCallback verify)
        {
            this.TestTemplate(
                invoke,
                (mock, service, asyncResult) =>
                {
                    mock.DomainClient.Error = new Exception(WebAuthenticationServiceTest.ErrorMessage);
                    mock.DomainClient.RequestCallback();
                    Assert.IsFalse(mock.DomainClient.CancellationRequested,
                        "Result should not be canceled.");
                },
                verify,
                true);
            this.TestTemplate(
                invoke,
                (mock, service, asyncResult) =>
                {
                    mock.DomainClient.Error = new Exception(WebAuthenticationServiceTest.ErrorMessage);
                    mock.DomainClient.RequestCallback(WebAuthenticationServiceTest.Delay);
                    this.EnqueueConditional(() => asyncResult.IsCompleted);
                    this.EnqueueCallback(() =>
                    {
                        Assert.IsFalse(mock.DomainClient.CancellationRequested,
                            "Result should not be canceled.");
                    });
                },
                verify,
                false);
            this.EnqueueTestComplete();
        }

#endregion
    }
}
