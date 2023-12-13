extern alias SSmDsClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Security.Principal;
using System.Threading;
using OpenRiaServices.Client.Test;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Silverlight.Testing;
using DescriptionAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;
using System.Threading.Tasks;

namespace OpenRiaServices.Client.Authentication.Test
{
    /// <summary>
    /// Tests <see cref="AuthenticationService"/> members.
    /// </summary>
    [TestClass]
    public class AuthenticationServiceTest : UnitTestBase
    {
        #region Mock AuthenticationService

        public enum UserType { None, LoggedIn, LoggedOut, Loaded, Saved }

        internal class MockIdentity : IIdentity
        {
            public string AuthenticationType { get; set; }
            public bool IsAuthenticated { get; set; }
            public string Name { get; set; }
        }

        internal class MockPrincipal : IPrincipal
        {
            private readonly MockIdentity _identity = new MockIdentity();

            public IIdentity Identity { get { return this._identity; } }
            public bool IsInRole(string role) { return false; }

            public UserType Type { get; set; }
        }

        private class MockAuthenticationNoCancel : AuthenticationService, IDisposable
        {
            public const string ValidUserName = "ValidUser";
            public const string InvalidUserName = "InvalidUser";
            // wait timetout to fix issue with some tests haning
            private TimeSpan DefaultTimeout => TimeSpan.FromMinutes(1);

            public Exception Error { get; set; }

            public bool CreateNullDefaultUser { get; set; }

            private readonly SemaphoreSlim _delay = new SemaphoreSlim(0);

            public void RequestCallback()
            {
                _delay.Release();
            }

            public void RequestCallback(int delay)
            {
                Task.Delay(delay)
                    .ContinueWith(_ => this.RequestCallback());
            }

            protected override IPrincipal CreateDefaultUser()
            {
                return this.CreateNullDefaultUser ? null : new MockPrincipal();
            }

            protected internal override async Task<LoginResult> LoginAsync(LoginParameters parameters, CancellationToken cancellationToken)
            {
                MockPrincipal user = null;
                if ((parameters != null) && (parameters.UserName == MockAuthentication.ValidUserName))
                {
                    user = new MockPrincipal() { Type = UserType.LoggedIn };
                    ((MockIdentity)user.Identity).IsAuthenticated = true;
                }

                await _delay.WaitAsync(DefaultTimeout, cancellationToken);
                if (this.Error != null)
                {
                    throw this.Error;
                }

                return new LoginResult(user, (user != null));
            }

            protected internal override async Task<LogoutResult> LogoutAsync(CancellationToken cancellationToken)
            {
                await _delay.WaitAsync(DefaultTimeout, cancellationToken);

                if (this.Error != null)
                {
                    throw this.Error;
                }

                return new LogoutResult(new MockPrincipal() { Type = UserType.LoggedOut });
            }

            protected internal override async Task<LoadUserResult> LoadUserAsync(CancellationToken cancellationToken)
            {
                await _delay.WaitAsync(DefaultTimeout, cancellationToken);

                if (this.Error != null)
                {
                    throw this.Error;
                }

                return new LoadUserResult(new MockPrincipal() { Type = UserType.Loaded });
            }

            protected internal override async Task<SaveUserResult> SaveUserAsync(IPrincipal user, CancellationToken cancellationToken)
            {
                Assert.IsNotNull(user, "User should never be null.");

                await _delay.WaitAsync(DefaultTimeout, cancellationToken);
                if (this.Error != null)
                {
                    throw this.Error;
                }

                return new SaveUserResult(new MockPrincipal() { Type = UserType.Saved });
            }

            #region IDisposable Support
            private bool _disposedValue; // To detect redundant calls

            protected virtual void Dispose(bool disposing)
            {
                if (!_disposedValue)
                {
                    if (disposing)
                    {
                        _delay.Dispose();
                    }

                    _disposedValue = true;
                }
            }

            public void Dispose()
            {
                // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
                Dispose(true);
            }
            #endregion
        }

        private class MockAuthentication : MockAuthenticationNoCancel
        {
            protected internal override bool SupportsCancellation
            {
                get { return true; }
            }

        }

        private class TrackingAuthentication : MockAuthentication
        {
            public int CreateDefaultUserCount { get; set; }

            public AuthenticationOperation Operation => base.Operation;

            protected override IPrincipal CreateDefaultUser()
            {
                this.CreateDefaultUserCount++;
                return base.CreateDefaultUser();
            }

            public int BeginLoginCount { get; set; }

            public int CancelLoginCount { get; set; }

            public int EndLoginCount { get; set; }

            protected internal override Task<LoginResult> LoginAsync(LoginParameters parameters, CancellationToken cancellationToken)
            {
                this.BeginLoginCount++;
                return base.LoginAsync(parameters, cancellationToken)
                    .ContinueWith(res =>
                    {
                        if (res.IsCanceled)
                            CancelLoginCount++;
                        else
                            EndLoginCount++;
                        return res;
                    }).Unwrap();
            }

            public int BeginLogoutCount { get; set; }
            public int CancelLogoutCount { get; set; }
            public int EndLogoutCount { get; set; }
            protected internal override Task<LogoutResult> LogoutAsync(CancellationToken cancellationToken)
            {
                this.BeginLogoutCount++;
                return base.LogoutAsync(cancellationToken).ContinueWith(res =>
                {
                    if (res.IsCanceled)
                        CancelLogoutCount++;
                    else
                        EndLogoutCount++;

                    return res;
                }).Unwrap();
            }

            public int BeginLoadUserCount { get; set; }
            public int CancelLoadUserCount { get; set; }
            public int EndLoadUserCount { get; set; }

            protected internal override Task<LoadUserResult> LoadUserAsync(CancellationToken cancellationToken)
            {
                this.BeginLoadUserCount++;
                return base.LoadUserAsync(cancellationToken).ContinueWith(res =>
                {
                    if (res.IsCanceled)
                        CancelLoadUserCount++;
                    else
                        EndLoadUserCount++;

                    return res;
                }).Unwrap();
            }

            public int BeginSaveUserCount { get; set; }

            public int CancelSaveUserCount { get; set; }
            public int EndSaveUserCount { get; set; }

            protected internal override Task<SaveUserResult> SaveUserAsync(IPrincipal user, CancellationToken cancellationToken)
            {
                this.BeginSaveUserCount++;
                return base.SaveUserAsync(user, cancellationToken)
                    .ContinueWith(res =>
                    {
                        if (res.IsCanceled)
                            CancelSaveUserCount++;
                        else
                            EndSaveUserCount++;

                        return res;
                    }).Unwrap();
            }
        }

        private class ThrowingAuthentication : MockAuthentication
        {
            public Exception BeginError { get; set; }
            public Exception CancelError { get; set; }
            public Exception EndError { get; set; }

            protected internal override Task<LoginResult> LoginAsync(LoginParameters parameters, CancellationToken cancellationToken)
            {
                if (this.BeginError != null)
                    throw this.BeginError;

                if (this.CancelError != null)
                    cancellationToken.Register(() => { throw this.CancelError; });

                var result = base.LoginAsync(parameters, cancellationToken);
                if (this.EndError != null)
                    result = result.ContinueWith<LoginResult>(task => throw this.EndError);

                return result;

            }

            protected internal override Task<LoadUserResult> LoadUserAsync(CancellationToken cancellationToken)
            {
                if (this.BeginError != null)
                    throw this.BeginError;

                if (this.CancelError != null)
                    cancellationToken.Register(() => { throw this.CancelError; });

                var result = base.LoadUserAsync(cancellationToken);
                if (this.EndError != null)
                    result = result.ContinueWith<LoadUserResult>(task => throw this.EndError);

                return result;
            }

            protected internal override Task<LogoutResult> LogoutAsync(CancellationToken cancellationToken)
            {
                if (this.BeginError != null)
                    throw this.BeginError;

                if (this.CancelError != null)
                    cancellationToken.Register(() => { throw this.CancelError; });

                var result = base.LogoutAsync(cancellationToken);
                if (this.EndError != null)
                    result = result.ContinueWith<LogoutResult>(task => throw this.EndError);

                return result;
            }

            protected internal override Task<SaveUserResult> SaveUserAsync(IPrincipal user, CancellationToken cancellationToken)
            {
                if (this.BeginError != null)
                    throw this.BeginError;

                if (this.CancelError != null)
                    cancellationToken.Register(() => { throw this.CancelError; });

                var result = base.SaveUserAsync(user, cancellationToken);
                if (this.EndError != null)
                    result = result.ContinueWith<SaveUserResult>(task => throw this.EndError);

                return result;

            }
        }

        #endregion

        private const int Delay = 5;
        private const string ErrorMessage = "There was an error";

        // TODO:
        // LoggedIn event
        // LoggedOut event
        // PropertyChanged/RaisePropertyChanged/OnPropertyChanged

        [TestMethod]
        [Description("Tests that cancelling an operation that does not support cancel with throw a NotSupportedException.")]
        public void CancelThrowsWhenNotSupported()
        {
            using (MockAuthenticationNoCancel mock = new MockAuthenticationNoCancel())
                ExceptionHelper.ExpectException<NotSupportedException>(
                    () => mock.Login(string.Empty, string.Empty).Cancel());

            using (MockAuthenticationNoCancel mock = new MockAuthenticationNoCancel())
                ExceptionHelper.ExpectException<NotSupportedException>(
                    () => mock.Logout(false).Cancel());

            using (MockAuthenticationNoCancel mock = new MockAuthenticationNoCancel())
                ExceptionHelper.ExpectException<NotSupportedException>(
                () => mock.LoadUser().Cancel());

            using (MockAuthenticationNoCancel mock = new MockAuthenticationNoCancel())
                ExceptionHelper.ExpectException<NotSupportedException>(
                () => mock.SaveUser(false).Cancel());
        }

        [TestMethod]
        [Description("Tests that User throws an InvalidOperationException when CreateDefaultUser returns null.")]
        public void UserThrowsOnNull()
        {
            using MockAuthentication mock = new MockAuthentication();
            mock.CreateNullDefaultUser = true;

            ExceptionHelper.ExpectException<InvalidOperationException>(
                () => Assert.IsNotNull(mock.User, "This should throw."));
        }

        [TestMethod]
        [Description("Tests the default values for an AuthenticationService.")]
        public void DefaultValues()
        {
            using MockAuthenticationNoCancel mock = new MockAuthenticationNoCancel();

            Assert.IsFalse(mock.IsBusy,
                "The AuthenticationService should not be busy.");
            Assert.IsFalse(mock.IsLoggingIn,
                "The AuthenticationService should not be busy.");
            Assert.IsFalse(mock.IsLoggingOut,
                "The AuthenticationService should not be busy.");
            Assert.IsFalse(mock.IsLoadingUser,
                "The AuthenticationService should not be busy.");
            Assert.IsFalse(mock.IsSavingUser,
                "The AuthenticationService should not be busy.");

            Assert.IsFalse(mock.SupportsCancellation,
                "The AuthenticationService should not support authentication.");

            Assert.IsNotNull(mock.User,
                "The AuthenticationService should never return a null user.");

            // Check that subscription and unsubscription work
            EventHandler<AuthenticationEventArgs> aeHandler = (sender, e) => Assert.IsTrue(true);
            mock.LoggedIn += aeHandler;
            mock.LoggedIn -= aeHandler;
            mock.LoggedOut += aeHandler;
            mock.LoggedOut -= aeHandler;
            PropertyChangedEventHandler pceHandler = (sender, e) => Assert.IsTrue(true);
            ((INotifyPropertyChanged)mock).PropertyChanged += pceHandler;
            ((INotifyPropertyChanged)mock).PropertyChanged -= pceHandler;
        }

        [TestMethod]
        [Description("Tests that exceptions thrown from BeginXx are thrown from Xx.")]
        public void BeginExceptionsThrown()
        {
            using ThrowingAuthentication mock = new ThrowingAuthentication();
            mock.BeginError = new Exception(AuthenticationServiceTest.ErrorMessage);

            ExceptionHelper.ExpectException<Exception>(
                () => mock.Login(string.Empty, string.Empty), AuthenticationServiceTest.ErrorMessage);
            ExceptionHelper.ExpectException<Exception>(
                () => mock.Logout(false), AuthenticationServiceTest.ErrorMessage);
            ExceptionHelper.ExpectException<Exception>(
                () => mock.LoadUser(), AuthenticationServiceTest.ErrorMessage);
            ExceptionHelper.ExpectException<Exception>(
                () => mock.SaveUser(false), AuthenticationServiceTest.ErrorMessage);
        }

        [TestMethod]
        [Description("Tests that exceptions thrown from CancelXx are thrown from OperationBase.Cancel.")]
        public void CancelExceptionsThrown()
        {
            ThrowingAuthentication mock = new ThrowingAuthentication();
            Exception error = new Exception(AuthenticationServiceTest.ErrorMessage);
            mock.CancelError = error;

            ExceptionHelper.ExpectException<Exception>(
                () => mock.Login(string.Empty, string.Empty).Cancel(), AuthenticationServiceTest.ErrorMessage);

            mock = new ThrowingAuthentication();
            mock.CancelError = error;

            ExceptionHelper.ExpectException<Exception>(
                () => mock.Logout(false).Cancel(), AuthenticationServiceTest.ErrorMessage);

            mock = new ThrowingAuthentication();
            mock.CancelError = error;

            ExceptionHelper.ExpectException<Exception>(
                () => mock.LoadUser().Cancel(), AuthenticationServiceTest.ErrorMessage);

            mock = new ThrowingAuthentication();
            mock.CancelError = error;

            ExceptionHelper.ExpectException<Exception>(
                () => mock.SaveUser(false).Cancel(), AuthenticationServiceTest.ErrorMessage);
        }

        [TestMethod]
        [Description("Tests that exceptions thrown from EndXx are caught and available in Operation.Error.")]
        public async Task EndExceptionsCaughtAsync()
        {
            Exception error = new Exception(ErrorMessage);
            using ThrowingAuthentication mock = new ThrowingAuthentication { EndError = error };

            Action<AuthenticationOperation> callback =
                ao =>
                {
                    Assert.AreEqual(mock.EndError, ao.Error, "Exceptions should be equal.");
                    ao.MarkErrorAsHandled();
                };

            await CompleteAndCheckErrorAsync(mock, mock.Login(new LoginParameters(string.Empty, string.Empty), ConvertCallback<LoginOperation>(callback), null), error);

            await CompleteAndCheckErrorAsync(mock, mock.Logout(ConvertCallback<LogoutOperation>(callback), null), error);

            await CompleteAndCheckErrorAsync(mock, mock.LoadUser(ConvertCallback<LoadUserOperation>(callback), null), error);

            await CompleteAndCheckErrorAsync(mock, mock.SaveUser(ConvertCallback<SaveUserOperation>(callback), null), error);
        }

        #region Tracking

        [TestMethod]
        [Description("Tests that getting User will call CreateDefaultUser.")]
        public void UserCallsCreateDefaultUser()
        {
            using TrackingAuthentication mock = new TrackingAuthentication();
            Assert.IsNotNull(mock.User,
                "Getting User.");
            Assert.IsNotNull(mock.User,
                "Getting User a second time.");
            Assert.AreEqual(1, mock.CreateDefaultUserCount,
                "CreateDefaultUser should have been called a single time.");
        }

        [TestMethod]
        [Description("Tests that Login calls BeginLogin, CancelLogin, and EndLogin.")]
        public async Task LoginCallsBeginCancelEnd()
        {
            using TrackingAuthentication mock = new TrackingAuthentication();

            // Begin/Cancel
            await CancelAndCheckStatusAsync(mock.Login(MockAuthentication.ValidUserName, string.Empty));
            await CancelAndCheckStatusAsync(mock.Login(new LoginParameters(MockAuthentication.ValidUserName, string.Empty)));
            await CancelAndCheckStatusAsync(mock.Login(new LoginParameters(MockAuthentication.ValidUserName, string.Empty), null, null));
            Assert.AreEqual(3, mock.BeginLoginCount,
                "BeginLogin should have been called 3 times.");
            Assert.AreEqual(3, mock.CancelLoginCount, "CancelLoginCount should have been called 3 times.");

            mock.BeginLoginCount = 0;
            // Begin/End
            await CompleteAndCheckStatusAsync(mock, mock.Login(MockAuthentication.ValidUserName, string.Empty));
            await CompleteAndCheckStatusAsync(mock, mock.Login(new LoginParameters(MockAuthentication.ValidUserName, string.Empty)));
            await CompleteAndCheckStatusAsync(mock, mock.Login(new LoginParameters(MockAuthentication.ValidUserName, string.Empty), null, null));
            Assert.AreEqual(3, mock.BeginLoginCount,
                "BeginLogin should have been called 3 times.");
            Assert.AreEqual(3, mock.EndLoginCount,
                "EndLogin should have been called 3 times.");
        }

        [TestMethod]
        [Description("Tests that Logout calls BeginLogout, CancelLogout, and EndLogout.")]
        public async Task LogoutCallsBeginCancelEnd()
        {
            using TrackingAuthentication mock = new TrackingAuthentication();

            // Begin/Cancel
            await CancelAndCheckStatusAsync(mock.Logout(false));
            await CancelAndCheckStatusAsync(mock.Logout(null, null));
            Assert.AreEqual(2, mock.BeginLogoutCount,
                "BeginLogout should have been called 2 times.");
            Assert.AreEqual(2, mock.CancelLogoutCount,
                "CancelLogout should have been called 2 times.");

            mock.BeginLogoutCount = 0;

            // Begin/End
            await CompleteAndCheckStatusAsync(mock, mock.Logout(false));
            await CompleteAndCheckStatusAsync(mock, mock.Logout(null, null));
            Assert.AreEqual(2, mock.BeginLogoutCount,
                "BeginLogout should have been called 2 times.");
            Assert.AreEqual(2, mock.EndLogoutCount,
                "EndLogout should have been called 2 times.");
        }

        [TestMethod]
        [Description("Tests that LoadUser calls BeginLoadUser, CancelLoadUser, and EndLoadUser.")]
        public async Task LoadUserCallsBeginCancelEnd()
        {
            using TrackingAuthentication mock = new TrackingAuthentication();

            // Begin/Cancel
            await CancelAndCheckStatusAsync(mock.LoadUser());
            await CancelAndCheckStatusAsync(mock.LoadUser(null, null));
            Assert.AreEqual(2, mock.BeginLoadUserCount,
                "BeginLoadUser should have been called 2 times.");
            Assert.AreEqual(2, mock.CancelLoadUserCount,
                "CancelLoadUser should have been called 2 times.");

            mock.BeginLoadUserCount = 0;

            // Begin/End
            await CompleteAndCheckStatusAsync(mock, mock.LoadUser());
            await CompleteAndCheckStatusAsync(mock, mock.LoadUser(null, null));
            Assert.AreEqual(2, mock.BeginLoadUserCount,
                "BeginLoadUser should have been called 2 times.");
            Assert.AreEqual(2, mock.EndLoadUserCount,
                "EndLoadUser should have been called 2 times.");
        }

        [TestMethod]
        [Description("Tests that SaveUser calls BeginSaveUser, CancelSaveUser, and EndSaveUser.")]
        public async Task SaveUserCallsBeginCancelEnd()
        {
            using TrackingAuthentication mock = new TrackingAuthentication();

            // Begin/Cancel
            await CancelAndCheckStatusAsync(mock.SaveUser(false));
            await CancelAndCheckStatusAsync(mock.SaveUser(null, null));
            Assert.AreEqual(2, mock.BeginSaveUserCount,
                "BeginSaveUser should have been called 2 times.");
            Assert.AreEqual(2, mock.CancelSaveUserCount,
                "CancelSaveUser should have been called 2 times.");

            mock.BeginSaveUserCount = 0;

            // Begin/End
            await CompleteAndCheckStatusAsync(mock, mock.SaveUser(false));
            await CompleteAndCheckStatusAsync(mock, mock.SaveUser(null, null));
            Assert.AreEqual(2, mock.BeginSaveUserCount,
                "BeginSaveUser should have been called 2 times.");
            Assert.AreEqual(2, mock.EndSaveUserCount,
                "EndSaveUser should have been called 2 times.");
        }

        #endregion

        #region Twice (invoking operation while busy should raise exception)

        [TestMethod]
        [Description("Tests that invoking Login twice throws an InvalidOperationException")]
        public void LoginTwiceThrows()
        {
            using MockAuthentication mock = new MockAuthentication();
            mock.Login(null);
            ExceptionHelper.ExpectException<InvalidOperationException>(
                () => mock.Login(null));
        }

        [TestMethod]
        [Description("Tests that invoking Logout twice throws an InvalidOperationException")]
        public void LogoutTwiceThrows()
        {
            using MockAuthentication mock = new MockAuthentication();
            mock.Logout(false);
            ExceptionHelper.ExpectException<InvalidOperationException>(
                () => mock.Logout(false));
        }

        [TestMethod]
        [Description("Tests that invoking LoadUser twice throws an InvalidOperationException")]
        public void LoadUserTwiceThrows()
        {
            using MockAuthentication mock = new MockAuthentication();
            mock.LoadUser();
            ExceptionHelper.ExpectException<InvalidOperationException>(
                () => mock.LoadUser());
        }

        [TestMethod]
        [Description("Tests that invoking SaveUser twice throws an InvalidOperationException")]
        public void SaveUserTwiceThrows()
        {
            using MockAuthentication mock = new MockAuthentication();
            mock.SaveUser(false);
            ExceptionHelper.ExpectException<InvalidOperationException>(
                () => mock.SaveUser(false));
        }

        #endregion

        #region TwiceWithCancel

        [TestMethod]
        [Description("Tests that Login and Cancel can be synchronously interleaved")]
        public async Task LoginCancelLogin()
        {
            using MockAuthentication mock = new MockAuthentication();

            Assert.IsFalse(mock.IsBusy,
                "Service should not be busy.");
            Assert.IsFalse(mock.IsLoggingIn,
                "Service should not be logging in.");

            LoginOperation op = mock.Login(null);

            Assert.IsTrue(mock.IsBusy,
                "Service should be busy.");
            Assert.IsTrue(mock.IsLoggingIn,
                "Service should be logging in.");

            op.Cancel();
            await op;

            Assert.IsFalse(mock.IsBusy,
                "Service should not be busy.");
            Assert.IsFalse(mock.IsLoggingIn,
                "Service should not be logging in.");

            op = mock.Login(null);

            Assert.IsTrue(mock.IsBusy,
                "Service should be busy.");
            Assert.IsTrue(mock.IsLoggingIn,
                "Service should be logging in.");

            op.Cancel();
            await op;
        }

        [TestMethod]
        [Description("Tests that Logout and Cancel can be synchronously interleaved")]
        public async Task LogoutCancelLogout()
        {
            using MockAuthentication mock = new MockAuthentication();

            Assert.IsFalse(mock.IsBusy,
                "Service should not be busy.");
            Assert.IsFalse(mock.IsLoggingOut,
                "Service should not be logging out.");

            LogoutOperation op = mock.Logout(false);

            Assert.IsTrue(mock.IsBusy,
                "Service should be busy.");
            Assert.IsTrue(mock.IsLoggingOut,
                "Service should be logging out.");

            op.Cancel();
            await op;

            Assert.IsFalse(mock.IsBusy,
                "Service should not be busy.");
            Assert.IsFalse(mock.IsLoggingOut,
                "Service should not be logging out.");

            op = mock.Logout(false);

            Assert.IsTrue(mock.IsBusy,
                "Service should be busy.");
            Assert.IsTrue(mock.IsLoggingOut,
                "Service should be logging out.");

            op.Cancel();
            await op;
        }

        [TestMethod]
        [Description("Tests that LoadUser and Cancel can be synchronously interleaved")]
        public async Task LoadUserCancelLoadUser()
        {
            using MockAuthentication mock = new MockAuthentication();

            Assert.IsFalse(mock.IsBusy,
                "Service should not be busy.");
            Assert.IsFalse(mock.IsLoadingUser,
                "Service should not be loading.");

            LoadUserOperation op = mock.LoadUser();

            Assert.IsTrue(mock.IsBusy,
                "Service should be busy.");
            Assert.IsTrue(mock.IsLoadingUser,
                "Service should be loading.");

            op.Cancel();
            await op;

            Assert.IsFalse(mock.IsBusy,
                "Service should not be busy.");
            Assert.IsFalse(mock.IsLoadingUser,
                "Service should not be loading.");

            op = mock.LoadUser();

            Assert.IsTrue(mock.IsBusy,
                "Service should be busy.");
            Assert.IsTrue(mock.IsLoadingUser,
                "Service should be loading.");

            op.Cancel();
            await op;
        }

        [TestMethod]
        [Description("Tests that SaveUser and Cancel can be synchronously interleaved")]
        public async Task SaveUserCancelSaveUser()
        {
            using MockAuthentication mock = new MockAuthentication();

            Assert.IsFalse(mock.IsBusy,
                "Service should not be busy.");
            Assert.IsFalse(mock.IsSavingUser,
                "Service should not be saving.");

            SaveUserOperation op = mock.SaveUser(false);

            Assert.IsTrue(mock.IsBusy,
                "Service should be busy.");
            Assert.IsTrue(mock.IsSavingUser,
                "Service should be saving.");

            op.Cancel();
            await op;

            Assert.IsFalse(mock.IsBusy,
                "Service should not be busy.");
            Assert.IsFalse(mock.IsSavingUser,
                "Service should not be saving.");

            op = mock.SaveUser(false);

            Assert.IsTrue(mock.IsBusy,
                "Service should be busy.");
            Assert.IsTrue(mock.IsSavingUser,
                "Service should be saving.");

            op.Cancel();
            await op;
        }

        #endregion

        #region Async

        [TestMethod]
        [Asynchronous]
        [Description("Tests that Login completes successfully")]
        public void Login()
        {
            LoginParameters parameters =
                new LoginParameters(MockAuthentication.ValidUserName, string.Empty);

            this.AsyncTemplate(
            (mock, callback, state) =>
            {
                ((INotifyPropertyChanged)mock).PropertyChanged += AuthenticationServiceTest.GetHandler(
                    new string[] { "IsBusy", "IsLoggingIn", "User", "IsBusy", "IsLoggingIn" });
                return mock.Login(parameters, AuthenticationServiceTest.ConvertCallback<LoginOperation>(callback), state);
            },
            (mock, operation) =>
            {
                Assert.IsInstanceOfType(operation, typeof(LoginOperation),
                    "Call should return a LoginOperation.");
                LoginOperation loginOperation = operation as LoginOperation;

                Assert.AreEqual(parameters, loginOperation.LoginParameters,
                    "Parameters should be equal.");
                Assert.IsTrue(loginOperation.LoginSuccess,
                    "Login should be successful.");
                Assert.AreEqual(UserType.LoggedIn, ((MockPrincipal)loginOperation.User).Type,
                    "User should be logged in.");
            });
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that Login fails successfully")]
        public void LoginFail()
        {
            LoginParameters parameters =
                new LoginParameters(MockAuthentication.InvalidUserName, string.Empty);

            this.AsyncTemplate(
            (mock, callback, state) =>
            {
                ((INotifyPropertyChanged)mock).PropertyChanged += AuthenticationServiceTest.GetHandler(
                    new string[] { "IsBusy", "IsLoggingIn", "IsBusy", "IsLoggingIn" });
                return mock.Login(parameters, AuthenticationServiceTest.ConvertCallback<LoginOperation>(callback), state);
            },
            (mock, operation) =>
            {
                Assert.IsInstanceOfType(operation, typeof(LoginOperation),
                    "Call should return a LoginOperation.");
                LoginOperation loginOperation = operation as LoginOperation;

                Assert.AreEqual(parameters, loginOperation.LoginParameters,
                    "Parameters should be equal.");
                Assert.IsFalse(loginOperation.LoginSuccess,
                    "Login should have failed.");
                Assert.IsNull(loginOperation.User,
                    "User should not be logged in.");
                // TODO: DC_API - this might be the preferred alternative
                //Assert.AreEqual(UserType.None, ((MockPrincipal)loginOperation.User).Type,
                //    "User should not be logged in.");
            },
            false);
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that Logout completes successfully")]
        public void Logout()
        {
            this.AsyncTemplate(
            (mock, callback, state) =>
            {
                ((INotifyPropertyChanged)mock).PropertyChanged += AuthenticationServiceTest.GetHandler(
                    new string[] { "IsBusy", "IsLoggingOut", "User", "IsBusy", "IsLoggingOut" });
                return mock.Logout(AuthenticationServiceTest.ConvertCallback<LogoutOperation>(callback), state);
            },
            (mock, operation) =>
            {
                Assert.IsInstanceOfType(operation, typeof(LogoutOperation),
                    "Call should return a LogoutOperation.");
                LogoutOperation logoutOperation = operation as LogoutOperation;

                Assert.AreEqual(UserType.LoggedOut, ((MockPrincipal)logoutOperation.User).Type,
                    "User should be logged out.");
            });
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that LoadUser completes successfully")]
        public void LoadUser()
        {
            this.AsyncTemplate(
            (mock, callback, state) =>
            {
                ((INotifyPropertyChanged)mock).PropertyChanged += AuthenticationServiceTest.GetHandler(
                    new string[] { "IsBusy", "IsLoadingUser", "User", "IsBusy", "IsLoadingUser" });
                return mock.LoadUser(AuthenticationServiceTest.ConvertCallback<LoadUserOperation>(callback), state);
            },
            (mock, operation) =>
            {
                Assert.IsInstanceOfType(operation, typeof(LoadUserOperation),
                    "Call should return a LoadUserOperation.");
                LoadUserOperation loadUserOperation = operation as LoadUserOperation;

                Assert.AreEqual(UserType.Loaded, ((MockPrincipal)loadUserOperation.User).Type,
                    "User should be loaded.");
            });
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that SaveUser completes successfully")]
        public void SaveUser()
        {
            this.AsyncTemplate(
            (mock, callback, state) =>
            {
                ((INotifyPropertyChanged)mock).PropertyChanged += AuthenticationServiceTest.GetHandler(
                    new string[] { "IsBusy", "IsSavingUser", "User", "IsBusy", "IsSavingUser" });
                return mock.SaveUser(AuthenticationServiceTest.ConvertCallback<SaveUserOperation>(callback), state);
            },
            (mock, operation) =>
            {
                Assert.IsInstanceOfType(operation, typeof(SaveUserOperation),
                    "Call should return a SaveUserOperation.");
                SaveUserOperation saveUserOperation = operation as SaveUserOperation;

                Assert.AreEqual(UserType.Saved, ((MockPrincipal)saveUserOperation.User).Type,
                    "User should be saved.");
            });
        }

        #endregion

        #region Cancel

        [TestMethod]
        [Asynchronous]
        [Description("Tests that Login cancels successfully")]
        public void LoginCancel()
        {
            LoginParameters parameters =
                new LoginParameters(MockAuthentication.ValidUserName, string.Empty);

            this.CancelTemplate(
            (mock, callback, state) =>
            {
                ((INotifyPropertyChanged)mock).PropertyChanged += AuthenticationServiceTest.GetHandler(
                    new string[] { "IsBusy", "IsLoggingIn", "IsBusy", "IsLoggingIn" });
                return mock.Login(parameters, AuthenticationServiceTest.ConvertCallback<LoginOperation>(callback), state);
            },
            (mock, operation) =>
            {
                Assert.IsInstanceOfType(operation, typeof(LoginOperation),
                    "Call should return a LoginOperation.");
                LoginOperation loginOperation = operation as LoginOperation;

                // TODO: DC_API - determine the expected value for these two
                //Assert.AreEqual(parameters, loginOperation.LoginParameters,
                //    "Parameters should be equal.");
                //Assert.IsFalse(loginOperation.LoginSuccess,
                //    "Login should be successful.");
                Assert.IsNull(loginOperation.User,
                    "User should not be logged in.");
                // TODO: DC_API - this might be the preferred alternative
                //Assert.AreEqual(UserType.None, ((MockPrincipal)loginOperation.User).Type,
                //    "User should not be logged in.");
            });
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that Logout cancels successfully")]
        public void LogoutCancel()
        {
            this.CancelTemplate(
            (mock, callback, state) =>
            {
                ((INotifyPropertyChanged)mock).PropertyChanged += AuthenticationServiceTest.GetHandler(
                    new string[] { "IsBusy", "IsLoggingOut", "IsBusy", "IsLoggingOut" });
                return mock.Logout(AuthenticationServiceTest.ConvertCallback<LogoutOperation>(callback), state);
            },
            (mock, operation) =>
            {
                Assert.IsInstanceOfType(operation, typeof(LogoutOperation),
                    "Call should return a LogoutOperation.");
                LogoutOperation logoutOperation = operation as LogoutOperation;

                Assert.IsNull(logoutOperation.User,
                    "User should not be logged out.");
                // TODO: DC_API - this might be the preferred alternative
                //Assert.AreEqual(UserType.None, ((MockPrincipal)logoutOperation.User).Type,
                //    "User should not be logged out.");
            });
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that LoadUser cancels successfully")]
        public void LoadUserCancel()
        {
            this.CancelTemplate(
            (mock, callback, state) =>
            {
                ((INotifyPropertyChanged)mock).PropertyChanged += AuthenticationServiceTest.GetHandler(
                    new string[] { "IsBusy", "IsLoadingUser", "IsBusy", "IsLoadingUser" });
                return mock.LoadUser(AuthenticationServiceTest.ConvertCallback<LoadUserOperation>(callback), state);
            },
            (mock, operation) =>
            {
                Assert.IsInstanceOfType(operation, typeof(LoadUserOperation),
                    "Call should return a LoadUserOperation.");
                LoadUserOperation loadUserOperation = operation as LoadUserOperation;

                Assert.IsNull(loadUserOperation.User,
                    "User should not be loaded.");
                // TODO: DC_API - this might be the preferred alternative
                //Assert.AreEqual(UserType.None, ((MockPrincipal)loadUserOperation.User).Type,
                //    "User should not be loaded.");
            });
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that SaveUser cancels successfully")]
        public void SaveUserCancel()
        {
            this.CancelTemplate(
            (mock, callback, state) =>
            {
                ((INotifyPropertyChanged)mock).PropertyChanged += AuthenticationServiceTest.GetHandler(
                    new string[] { "IsBusy", "IsSavingUser", "IsBusy", "IsSavingUser" });
                return mock.SaveUser(AuthenticationServiceTest.ConvertCallback<SaveUserOperation>(callback), state);
            },
            (mock, operation) =>
            {
                Assert.IsInstanceOfType(operation, typeof(SaveUserOperation),
                    "Call should return a SaveUserOperation.");
                SaveUserOperation saveUserOperation = operation as SaveUserOperation;

                Assert.IsNull(saveUserOperation.User,
                    "User should not be saved.");
                // TODO: DC_API - this might be the preferred alternative
                //Assert.AreEqual(UserType.None, ((MockPrincipal)saveUserOperation.User).Type,
                //    "User should not be saved.");
            });
        }

        #endregion

        #region Error

        [TestMethod]
        [Asynchronous]
        [Description("Tests that Login handles errors successfully")]
        public void LoginError()
        {
            LoginParameters parameters =
                new LoginParameters(MockAuthentication.ValidUserName, string.Empty);

            this.ErrorTemplate(
            (mock, callback, state) =>
            {
                ((INotifyPropertyChanged)mock).PropertyChanged += AuthenticationServiceTest.GetHandler(
                    new string[] { "IsBusy", "IsLoggingIn", "IsBusy", "IsLoggingIn" });
                return mock.Login(parameters, AuthenticationServiceTest.ConvertCallback<LoginOperation>(callback), state);
            },
            (mock, operation) =>
            {
                Assert.IsInstanceOfType(operation, typeof(LoginOperation),
                    "Call should return a LoginOperation.");
                LoginOperation loginOperation = operation as LoginOperation;

                // TODO: DC_API - determine the expected value for these two
                //Assert.AreEqual(parameters, loginOperation.LoginParameters,
                //    "Parameters should be equal.");
                //Assert.IsFalse(loginOperation.LoginSuccess,
                //    "Login should be successful.");
                Assert.IsNull(loginOperation.User,
                    "User should not be logged in.");
                // TODO: DC_API - this might be the preferred alternative
                //Assert.AreEqual(UserType.None, ((MockPrincipal)loginOperation.User).Type,
                //    "User should not be logged in.");
            });
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that Logout handles errors successfully")]
        public void LogoutError()
        {
            this.ErrorTemplate(
            (mock, callback, state) =>
            {
                ((INotifyPropertyChanged)mock).PropertyChanged += AuthenticationServiceTest.GetHandler(
                    new string[] { "IsBusy", "IsLoggingOut", "IsBusy", "IsLoggingOut" });
                return mock.Logout(AuthenticationServiceTest.ConvertCallback<LogoutOperation>(callback), state);
            },
            (mock, operation) =>
            {
                Assert.IsInstanceOfType(operation, typeof(LogoutOperation),
                    "Call should return a LogoutOperation.");
                LogoutOperation logoutOperation = operation as LogoutOperation;

                Assert.IsNull(logoutOperation.User,
                    "User should not be logged out.");
                // TODO: DC_API - this might be the preferred alternative
                //Assert.AreEqual(UserType.None, ((MockPrincipal)logoutOperation.User).Type,
                //    "User should not be logged out.");
            });
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that LoadUser handles errors successfully")]
        public void LoadUserError()
        {
            this.ErrorTemplate(
            (mock, callback, state) =>
            {
                ((INotifyPropertyChanged)mock).PropertyChanged += AuthenticationServiceTest.GetHandler(
                    new string[] { "IsBusy", "IsLoadingUser", "IsBusy", "IsLoadingUser" });
                return mock.LoadUser(AuthenticationServiceTest.ConvertCallback<LoadUserOperation>(callback), state);
            },
            (mock, operation) =>
            {
                Assert.IsInstanceOfType(operation, typeof(LoadUserOperation),
                    "Call should return a LoadUserOperation.");
                LoadUserOperation loadUserOperation = operation as LoadUserOperation;

                Assert.IsNull(loadUserOperation.User,
                    "User should not be loaded.");
                // TODO: DC_API - this might be the preferred alternative
                //Assert.AreEqual(UserType.None, ((MockPrincipal)loadUserOperation.User).Type,
                //    "User should not be loaded.");
            });
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that SaveUser handles errors successfully")]
        public void SaveUserError()
        {
            this.ErrorTemplate(
            (mock, callback, state) =>
            {
                ((INotifyPropertyChanged)mock).PropertyChanged += AuthenticationServiceTest.GetHandler(
                    new string[] { "IsBusy", "IsSavingUser", "IsBusy", "IsSavingUser" });
                return mock.SaveUser(AuthenticationServiceTest.ConvertCallback<SaveUserOperation>(callback), state);
            },
            (mock, operation) =>
            {
                Assert.IsInstanceOfType(operation, typeof(SaveUserOperation),
                    "Call should return a SaveUserOperation.");
                SaveUserOperation saveUserOperation = operation as SaveUserOperation;

                Assert.IsNull(saveUserOperation.User,
                    "User should not be saved.");
                // TODO: DC_API - this might be the preferred alternative
                //Assert.AreEqual(UserType.None, ((MockPrincipal)saveUserOperation.User).Type,
                //    "User should not be saved.");
            });
        }

        #endregion

        #region Templates

        private static Action<T> ConvertCallback<T>(Action<AuthenticationOperation> callback) where T : AuthenticationOperation
        {
            return (callback == null) ? null : (Action<T>)(to => callback(to));
        }

        private static PropertyChangedEventHandler GetHandler(IEnumerable<string> propertyChanges)
        {
            IEnumerator<string> pcEnumerator = propertyChanges.GetEnumerator();
            return (sender, e) =>
                {
                    Assert.IsTrue(pcEnumerator.MoveNext(),
                        string.Format(
                        CultureInfo.InvariantCulture,
                        "An unexpected property change occurred. {0}",
                        e.PropertyName));
                    Assert.AreEqual(pcEnumerator.Current, e.PropertyName,
                        "PropertyNames should be equal.");
                };
        }

        private delegate AuthenticationOperation InvokeCallback(MockAuthentication mock, Action<AuthenticationOperation> callback, object state);
        private delegate void TestCallback(MockAuthentication mock, AuthenticationOperation operation);

        private enum VerificationType { Callback, Event, Poll }

        private void TestTemplate(InvokeCallback invoke, TestCallback proceed, TestCallback verify, VerificationType verificationType)
        {
            MockAuthentication mock = new MockAuthentication();
            object state = new object();

            bool testCompleted = false;
            Action<AuthenticationOperation> callback = (AuthenticationOperation ao) =>
            {
                AuthenticationServiceTest.SuppressErrors(ao);

                Assert.IsNotNull(ao,
                    "Operation should not be null.");
                Assert.AreEqual(state, ao.UserState,
                    "States should be equal.");
                Assert.IsTrue(ao.IsComplete,
                    "Operation should be complete.");
                Assert.IsFalse(ao.CanCancel,
                    "Operation should no longer be cancelable.");
                // TODO: DC_API - determine if this is the behavior we want
                //Assert.IsNotNull(ao.User,
                //    "User should not be null.");

                Assert.IsFalse(mock.IsBusy,
                    "Serice should not be busy.");

                verify(mock, ao);

                testCompleted = true;
            };

            AuthenticationOperation operation = invoke(mock, (verificationType == VerificationType.Callback) ? callback : AuthenticationServiceTest.SuppressErrors, state);
            if (verificationType == VerificationType.Event)
            {
                operation.Completed += (sender, e) => callback((AuthenticationOperation)sender);
            }

            Assert.IsNotNull(operation,
                "Operation should not be null.");
            Assert.AreEqual(state, operation.UserState,
                "States should be equal.");
            Assert.IsFalse(operation.IsComplete,
                "IAsyncResult should not be complete.");
            // TODO: DC_API - determine if this is the behavior we want
            //Assert.IsNotNull(operation.User,
            //    "User should not be null.");

            Assert.IsTrue(mock.IsBusy,
                "Authentication service should be busy.");

            proceed(mock, operation);

            if (verificationType == VerificationType.Poll)
            {
                this.EnqueueCallback(() => callback(operation));
            }

            this.EnqueueConditional(() => testCompleted);
            this.EnqueueCallback(() => mock.Dispose());
        }

        private static void SuppressErrors(AuthenticationOperation ao)
        {
            if (ao.HasError)
            {
                ao.MarkErrorAsHandled();
            }
        }

        private void AsyncTemplate(InvokeCallback invoke, TestCallback verify)
        {
            this.AsyncTemplate(invoke, verify, true);
        }

        private void AsyncTemplate(InvokeCallback invoke, TestCallback verify, bool testPropertyChanged)
        {
            TestCallback asyncVerify =
                (mock, operation) =>
                {
                    Assert.IsFalse(operation.HasError,
                        "Operation should not have an error.");
                    Assert.IsNull(operation.Error,
                        "Operation should not have an error.");
                    Assert.IsFalse(operation.IsCanceled,
                        "Operation should not be canceled.");
                    verify(mock, operation);
                };
            this.TestTemplate(
                invoke,
                (mock, operation) =>
                {
                    mock.RequestCallback();
                },
                asyncVerify,
                VerificationType.Callback);
            this.TestTemplate(
                invoke,
                (mock, operation) =>
                {
                    mock.RequestCallback();
                },
                asyncVerify,
                VerificationType.Event);
            this.TestTemplate(
                invoke,
                (mock, operation) =>
                {
                    mock.RequestCallback(AuthenticationServiceTest.Delay);
                    this.EnqueueCompletion(() => operation);
                },
                asyncVerify,
                VerificationType.Poll);
            if (testPropertyChanged)
            {
                this.TestTemplate(
                    invoke,
                    (mock, operation) =>
                    {
                        mock.RequestCallback(AuthenticationServiceTest.Delay);
                        bool userChanged = false;
                        ((INotifyPropertyChanged)operation).PropertyChanged += (sender, e) =>
                        {
                            if (e.PropertyName == "User")
                            {
                                Assert.IsFalse(userChanged,
                                    "Only a single user change event should occur.");
                                userChanged = true;
                            }
                        };
                        this.EnqueueConditional(() => userChanged);
                    },
                    asyncVerify,
                    VerificationType.Poll);
            }
            this.EnqueueTestComplete();
        }

        private void CancelTemplate(InvokeCallback invoke, TestCallback verify)
        {
            TestCallback cancelVerify =
                (mock, operation) =>
                {
                    Assert.IsFalse(operation.HasError,
                        "Operation should not have an error.");
                    Assert.IsNull(operation.Error,
                        "Operation should not have an error.");
                    Assert.IsTrue(operation.IsCanceled,
                        "Operation should be canceled.");
                    verify(mock, operation);
                };
            this.TestTemplate(
                invoke,
                (mock, operation) =>
                {
                    operation.Cancel();
                },
                cancelVerify,
                VerificationType.Callback);
            this.TestTemplate(
                invoke,
                (mock, operation) =>
                {
                    operation.Cancel();
                },
                cancelVerify,
                VerificationType.Event);
            this.TestTemplate(
                invoke,
                (mock, operation) =>
                {
                    operation.Cancel();
                    this.EnqueueCompletion(() => operation);
                },
                cancelVerify,
                VerificationType.Poll);
            this.EnqueueTestComplete();
        }

        private void ErrorTemplate(InvokeCallback invoke, TestCallback verify)
        {
            TestCallback errorVerify =
                (mock, operation) =>
                {
                    Assert.IsTrue(operation.HasError,
                        "Operation should have an error.");
                    Assert.AreEqual(mock.Error, operation.Error,
                        "Errors should be equal.");
                    Assert.IsFalse(operation.IsCanceled,
                        "Operation should not be canceled.");
                    verify(mock, operation);
                };
            this.TestTemplate(
                invoke,
                (mock, operation) =>
                {
                    mock.Error = new Exception(AuthenticationServiceTest.ErrorMessage);
                    mock.RequestCallback();
                },
                errorVerify,
                VerificationType.Callback);
            this.TestTemplate(
                invoke,
                (mock, operation) =>
                {
                    mock.Error = new Exception(AuthenticationServiceTest.ErrorMessage);
                    mock.RequestCallback();
                },
                errorVerify,
                VerificationType.Event);
            this.TestTemplate(
                invoke,
                (mock, operation) =>
                {
                    mock.Error = new Exception(AuthenticationServiceTest.ErrorMessage);
                    mock.RequestCallback(AuthenticationServiceTest.Delay);
                    this.EnqueueCompletion(() => operation);
                },
                errorVerify,
                VerificationType.Poll);
            this.EnqueueTestComplete();
        }

        #endregion



        private static async Task CancelAndCheckStatusAsync(AuthenticationOperation op)
        {
            Assert.IsFalse(op.IsComplete);
            op.Cancel();
            Assert.IsTrue(op.IsCancellationRequested);
            await op;
            Assert.IsTrue(op.IsComplete);
            Assert.IsTrue(op.IsCanceled);
            Assert.IsFalse(op.HasError);
        }

        private static async Task CompleteAndCheckStatusAsync(MockAuthenticationNoCancel mock, AuthenticationOperation op)
        {
            Assert.IsFalse(op.IsComplete);
            mock.RequestCallback();
            await op;
            Assert.IsTrue(op.IsComplete);
            Assert.IsFalse(op.IsCanceled);
            Assert.IsFalse(op.HasError);
        }

        private static async Task CompleteAndCheckErrorAsync(MockAuthenticationNoCancel mock, AuthenticationOperation op, Exception exception)
        {
            Assert.IsFalse(op.IsComplete);
            mock.RequestCallback();
            await op;
            Assert.IsTrue(op.IsComplete);
            Assert.IsFalse(op.IsCanceled);
            Assert.AreEqual(exception, op.Error);
            Assert.IsTrue(op.HasError);
        }
    }
}
