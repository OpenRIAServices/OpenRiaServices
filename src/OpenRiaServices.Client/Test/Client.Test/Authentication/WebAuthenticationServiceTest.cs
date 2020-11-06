extern alias SSmDsClient;
extern alias SSmDsWeb;

using System;
using System.Security.Principal;
using OpenRiaServices.Client.Test;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Silverlight.Testing;
using DescriptionAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;
using MockUser = OpenRiaServices.Client.Authentication.Test.AuthenticationDomainClient.MockUser;
using UserType = OpenRiaServices.Client.Authentication.Test.AuthenticationDomainClient.UserType;

namespace OpenRiaServices.Client.Authentication.Test
{
    using System.Threading;
    using System.Threading.Tasks;
#if SILVERLIGHT
    using Resource = SSmDsWeb::OpenRiaServices.Client.Resource;
#else
    using Resource = SSmDsClient::OpenRiaServices.Client.Resource;
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

        // Asynchronous, Cancel, Error, Synchronous
        #region Async

        [TestMethod]
        [Asynchronous]
        [Description("Tests that Login completes successfully.")]
        public void LoginAsync()
        {
            LoginParameters parameters = new LoginParameters(AuthenticationDomainClient.ValidUserName, string.Empty);

            this.AsyncTemplate((mock, service) =>
            {
                return service.LoginAsync(parameters, CancellationToken.None);
            },
            (mock, service, task) =>
            {
                LoginResult result = task.GetAwaiter().GetResult();
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
            (mock, service) =>
            {
                return service.LoginAsync(parameters, CancellationToken.None);
            },
            (mock, service, task) =>
            {
                LoginResult result = task.GetAwaiter().GetResult();
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
            (mock, service) =>
            {
                return service.LogoutAsync(CancellationToken.None);
            },
            (mock, service, task) =>
            {
                AuthenticationResult result = task.GetAwaiter().GetResult();
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
            (mock, service) =>
            {
                return service.LoadUserAsync(CancellationToken.None);
            },
            (mock, service, task) =>
            {
                AuthenticationResult result = task.GetAwaiter().GetResult();
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
            (mock, service) =>
            {
                ((MockUser)service.User).Name = "User";
                ((MockUser)service.User).Modify(mock);
                return service.SaveUserAsync(service.User, CancellationToken.None);
            },
            (mock, service, task) =>
            {
                AuthenticationResult result = task.GetAwaiter().GetResult();
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
            (mock, service) =>
            {
                ((MockUser)service.User).Name = "User";
                return service.SaveUserAsync(service.User, CancellationToken.None);
            },
            (mock, service, task) =>
            {
                AuthenticationResult result = task.GetAwaiter().GetResult();
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

            this.CancelTemplate((mock, service, cancellationToken) =>
            {
                return service.LoginAsync(parameters, cancellationToken);
            });
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that Logout cancels successfully.")]
        public void LogoutCancel()
        {
            this.CancelTemplate((mock, service, cancellationToken) =>
            {
                return service.LogoutAsync(cancellationToken);
            });
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that LoadUser cancels successfully.")]
        public void LoadUserCancel()
        {
            this.CancelTemplate((mock, service, cancellationToken) =>
            {
                return service.LoadUserAsync(cancellationToken);
            });
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that SaveUser cancels successfully.")]
        public void SaveUserCancel()
        {
            this.CancelTemplate((mock, service, cancellationToken) =>
            {
                ((MockUser)service.User).Name = "User";
                ((MockUser)service.User).Modify(mock);
                return service.SaveUserAsync(service.User, cancellationToken);
            },
            (mock, service) =>
            {
                Assert.IsFalse(mock.DomainClient.Submitted,
                    "User should not have been submitted.");
            });
        }

        #endregion

        #region Error

        [TestMethod]
        [Description("Tests that Login handles errors successfully.")]
        public async Task LoginError()
        {
            LoginParameters parameters = new LoginParameters(AuthenticationDomainClient.ValidUserName, string.Empty);

            AuthenticationDomainContext mock = new AuthenticationDomainContext();
            MockWebAuthenticationService service = new MockWebAuthenticationService { DomainContext = mock };
            mock.DomainClient.Error = new Exception(WebAuthenticationServiceTest.ErrorMessage);

            var task = service.LoginAsync(parameters, CancellationToken.None);
            mock.DomainClient.RequestCallback();

            await ExceptionHelper.ExpectExceptionAsync<DomainOperationException>(
                () => task,
                string.Format(Resource.DomainContext_LoadOperationFailed, "Login", WebAuthenticationServiceTest.ErrorMessage));
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that Logout handles errors successfully.")]
        public void LogoutError()
        {
            this.ErrorTemplate(
            (mock, service) =>
            {
                return service.LogoutAsync(CancellationToken.None);
            },
            (mock, service, task) =>
            {
                ExceptionHelper.ExpectException<DomainOperationException>(
                    () => Assert.IsNull(task.GetAwaiter().GetResult(), "This should fail."),
                    string.Format(Resource.DomainContext_LoadOperationFailed, "Logout", WebAuthenticationServiceTest.ErrorMessage));
            });
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that LoadUser handles errors successfully.")]
        public void LoadUserError()
        {
            this.ErrorTemplate(
            (mock, service) =>
            {
                return service.LoadUserAsync(CancellationToken.None);
            },
            (mock, service, task) =>
            {
                ExceptionHelper.ExpectException<DomainOperationException>(
                    () => Assert.IsNull(task.GetAwaiter().GetResult(), "This should fail."),
                    string.Format(Resource.DomainContext_LoadOperationFailed, "GetUser", WebAuthenticationServiceTest.ErrorMessage));
            });
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that SaveUser handles errors successfully.")]
        public void SaveUserError()
        {
            this.ErrorTemplate(
            (mock, service) =>
            {
                ((MockUser)service.User).Name = "User";
                ((MockUser)service.User).Modify(mock);
                return service.SaveUserAsync(service.User, CancellationToken.None);
            },
            (mock, service, task) =>
            {
                ExceptionHelper.ExpectException<SubmitOperationException>(
                    () => Assert.IsNull(task.GetAwaiter().GetResult(), "This should fail."),
                    string.Format(Resource.DomainContext_SubmitOperationFailed, WebAuthenticationServiceTest.ErrorMessage));
                Assert.IsFalse(mock.DomainClient.Submitted,
                    "User should not have been submitted.");
            });
        }

        #endregion

        #region Templates

        private void TestTemplate<T>(Func<AuthenticationDomainContext, MockWebAuthenticationService, Task<T>> invoke,
            Action<AuthenticationDomainContext, MockWebAuthenticationService, Task<T>> proceed,
            Action<AuthenticationDomainContext, MockWebAuthenticationService, Task<T>> verify)
        {
            AuthenticationDomainContext mock = new AuthenticationDomainContext();
            MockWebAuthenticationService service = new MockWebAuthenticationService { DomainContext = mock };

            Task<T> task = invoke(mock, service);

            Assert.IsNotNull(task);

            proceed(mock, service, task);
            this.EnqueueConditional(() => task.IsCompleted);
            this.EnqueueCallback(() =>
            {
                verify(mock, service, task);
            });
        }

        private void AsyncTemplate<T>(Func<AuthenticationDomainContext, MockWebAuthenticationService, Task<T>> invoke, Action<AuthenticationDomainContext, MockWebAuthenticationService, Task<T>> verify)
        {
            this.TestTemplate(
                invoke,
                (mock, service, task) =>
                {
                    mock.DomainClient.RequestCallback();
                },
                 (mock, service, task) =>
                 {
                     Assert.IsFalse(mock.DomainClient.CancellationRequested,
                             "Result should not be canceled.");
                     verify(mock, service, task);
                 });
            this.EnqueueTestComplete();
        }

        private void CancelTemplate<T>(Func<AuthenticationDomainContext, MockWebAuthenticationService, CancellationToken, Task<T>> invoke,
            Action<AuthenticationDomainContext, MockWebAuthenticationService> verify = null)
        {
            AuthenticationDomainContext mock = new AuthenticationDomainContext();
            MockWebAuthenticationService service = new MockWebAuthenticationService { DomainContext = mock };
            using var cts = new CancellationTokenSource();

            Task<T> task = invoke(mock, service, cts.Token);

            Assert.IsNotNull(task);
            Assert.IsFalse(task.IsCompleted, "task should not be complete.");
            cts.Cancel();

            this.EnqueueConditional(() => task.IsCompleted);
            this.EnqueueCallback(() =>
            {
                Assert.IsTrue(task.IsCanceled, "task should be cancelled");
                verify?.Invoke(mock, service);
            });
            this.EnqueueTestComplete();
        }

        private void ErrorTemplate<T>(Func<AuthenticationDomainContext, MockWebAuthenticationService, Task<T>> invoke,
            Action<AuthenticationDomainContext, MockWebAuthenticationService, Task<T>> verify)
        {
            this.TestTemplate(
                invoke,
                (mock, service, task) =>
                {
                    mock.DomainClient.Error = new Exception(WebAuthenticationServiceTest.ErrorMessage);
                    mock.DomainClient.RequestCallback();
                    Assert.IsFalse(mock.DomainClient.CancellationRequested,
                        "Result should not be canceled.");
                },
                verify);
            this.EnqueueTestComplete();
        }

        #endregion
    }
}
