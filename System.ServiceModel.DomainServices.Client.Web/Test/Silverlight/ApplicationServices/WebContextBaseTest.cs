using System.ComponentModel;
using System.Security.Principal;
using OpenRiaServices.DomainServices.Client.Test;
using System.Windows;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DescriptionAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

namespace OpenRiaServices.DomainServices.Client.ApplicationServices.Test
{
    /// <summary>
    /// Tests <see cref="WebContextBase"/> members.
    /// </summary>
    [TestClass]
    public class WebContextBaseTest : UnitTestBase
    {
        private class WebContextMock : WebContextBase
        {
            public WebContextMock() : this(false) { }
            public WebContextMock(bool setAsCurrent) : base(setAsCurrent) { }

            public IPrincipal UserMock { get { return base.User; } }

            public void RaisePropertyChangedMock(string propertyName)
            {
                base.RaisePropertyChanged(propertyName);
            }

            public void OnPropertyChangedMock(PropertyChangedEventArgs e)
            {
                base.OnPropertyChanged(e);
            }
        }

        private class IdentityMock : IIdentity
        {
            public string AuthenticationType
            {
                get { return string.Empty; }
            }

            public bool IsAuthenticated
            {
                get { return false; }
            }

            public string Name
            {
                get { return string.Empty; }
            }
        }

        private class PrincipalMock : IPrincipal
        {
            private readonly IIdentity _identity = new IdentityMock();

            public IIdentity Identity
            {
                get { return this._identity; }
            }

            public bool IsInRole(string role)
            {
                return false; ;
            }
        }

        private class AuthenticationMock : AuthenticationService
        {
            private const string WebContext_AuthenticationNotSet = "Operation not supported.";

            protected override IPrincipal CreateDefaultUser()
            {
                return new PrincipalMock();
            }

            protected override IAsyncResult BeginLogin(LoginParameters parameters, AsyncCallback callback, object state)
            {
                throw new NotSupportedException(WebContext_AuthenticationNotSet);
            }

            protected override LoginResult EndLogin(IAsyncResult asyncResult)
            {
                throw new NotSupportedException(WebContext_AuthenticationNotSet);
            }

            protected override IAsyncResult BeginLogout(AsyncCallback callback, object state)
            {
                throw new NotSupportedException(WebContext_AuthenticationNotSet);
            }

            protected override LogoutResult EndLogout(IAsyncResult asyncResult)
            {
                throw new NotSupportedException(WebContext_AuthenticationNotSet);
            }

            protected override IAsyncResult BeginLoadUser(AsyncCallback callback, object state)
            {
                throw new NotSupportedException(WebContext_AuthenticationNotSet);
            }

            protected override LoadUserResult EndLoadUser(IAsyncResult asyncResult)
            {
                throw new NotSupportedException(WebContext_AuthenticationNotSet);
            }

            protected override IAsyncResult BeginSaveUser(IPrincipal user, AsyncCallback callback, object state)
            {
                throw new NotSupportedException(WebContext_AuthenticationNotSet);
            }

            protected override SaveUserResult EndSaveUser(IAsyncResult asyncResult)
            {
                throw new NotSupportedException(WebContext_AuthenticationNotSet);
            }
        }

        [TestMethod]
        [Description("Tests that the Current property raises an exception when no contexts are present.")]
        public void CurrentThrowsWithZeroInstances()
        {
            Application app = Application.Current;
            Assert.IsNotNull(app,
                "Application should not be null.");

            ExceptionHelper.ExpectException<InvalidOperationException>(
                () => Assert.IsNull(WebContextBase.Current, "This should fail."));

            WebContextMock context = new WebContextMock(true);

            Assert.AreEqual(context, WebContextBase.Current,
                "Contexts should be equal.");

            ExceptionHelper.ExpectException<InvalidOperationException>(
                () => new WebContextMock(true));
        }

        [TestMethod]
        [Description("Tests that the Authentication property raises change notification.")]
        public void Authentication()
        {
            WebContextMock context = new WebContextMock();
            AuthenticationService authentication = new AuthenticationMock();

            PropertyChangedEventArgs authenticationArgs = null;
            PropertyChangedEventArgs userArgs = null;
            PropertyChangedEventHandler handler = (sender, e) =>
            {
                if (e.PropertyName == "Authentication")
                {
                    Assert.IsNull(authenticationArgs,
                        "There should only be a single \"Authentication\" event. The args should be null.");
                    authenticationArgs = e;

                    Assert.AreEqual(authentication, context.Authentication,
                        "Authentication contexts should be equal.");
                    Assert.AreEqual(authentication.User, context.UserMock,
                        "Users should be equal.");
                }
                else if (e.PropertyName == "User")
                {
                    Assert.IsNull(userArgs,
                        "There should only be a single \"User\" event. The args should be null.");
                    userArgs = e;

                    Assert.AreEqual(authentication, context.Authentication,
                        "Authentication contexts should be equal.");
                    Assert.AreEqual(authentication.User, context.UserMock,
                        "Users should be equal.");
                }
                else
                {
                    Assert.Fail("There should not be any other property change events.");
                }
            };
            ((INotifyPropertyChanged)context).PropertyChanged += handler;

            Assert.IsNotNull(context.Authentication,
                "Authentication should not be null.");

            AuthenticationService original = context.Authentication;

            context.Authentication = authentication;

            Assert.IsNotNull(authenticationArgs,
                "An Authentication property change should have occurred.");
            Assert.IsNotNull(userArgs,
                "A User property change should have occurred.");

            // Reset values for the event handler
            authentication = original;
            authenticationArgs = null;
            userArgs = null;

            context.Authentication = original;

            Assert.IsNotNull(authenticationArgs,
                "An Authentication property change should have occurred.");
            Assert.IsNotNull(userArgs,
                "A User property change should have occurred.");

            ((INotifyPropertyChanged)context).PropertyChanged -= handler;
        }

        [TestMethod]
        [Description("Tests that the User property is equal to the value in Authentication.User.")]
        public void User()
        {
            WebContextMock context = new WebContextMock();
            AuthenticationMock authentication = new AuthenticationMock();

            PropertyChangedEventArgs args = null;
            PropertyChangedEventHandler handler = (sender, e) =>
            {
                if (e.PropertyName == "User")
                {
                    Assert.IsNull(args,
                        "There should only be a single \"User\" event. The args should be null.");
                    args = e;

                    Assert.AreEqual(authentication.User, context.UserMock,
                        "Users should be equal.");
                    Assert.AreEqual(context.Authentication.User, context.UserMock,
                        "User and Authentication.User should be identical.");
                }
            };
            ((INotifyPropertyChanged)context).PropertyChanged += handler;

            Assert.IsNotNull(context.UserMock,
                "User should not be null.");
            Assert.AreEqual(context.Authentication.User, context.UserMock,
                "User and Authentication.User should be identical.");

            context.Authentication = authentication;

            Assert.IsNotNull(args,
                "A User property change should have occurred.");

            ((INotifyPropertyChanged)context).PropertyChanged -= handler;
        }

        [TestMethod]
        [Description("Tests that the RaisePropertyChanged method raises an event.")]
        public void RaisePropertyChanged()
        {
            WebContextMock context = new WebContextMock();
            string propertyName = string.Empty;

            PropertyChangedEventArgs args = null;
            PropertyChangedEventHandler handler = (sender, e) =>
            {
                Assert.IsNull(args,
                    "There should only be a single event. The args should be null.");
                args = e;

                Assert.AreEqual(propertyName, e.PropertyName,
                    "Property names should be equal.");
            };
            ((INotifyPropertyChanged)context).PropertyChanged += handler;

            ExceptionHelper.ExpectArgumentNullException(
                () => context.RaisePropertyChangedMock(null), "propertyName");
            ExceptionHelper.ExpectArgumentNullException(
                () => context.RaisePropertyChangedMock(string.Empty), "propertyName");

            propertyName = "Property 1";

            context.RaisePropertyChangedMock(propertyName);
            Assert.IsNotNull(args,
                "A change event should have occurred.");

            args = null;

            propertyName = "Property 2";
            context.RaisePropertyChangedMock(propertyName);
            Assert.IsNotNull(args,
                "A change event should have occurred.");

            ((INotifyPropertyChanged)context).PropertyChanged -= handler;
        }

        [TestMethod]
        [Description("Tests that Authentication and User are valid in the default case.")]
        public void DefaultAuthentication()
        {
            WebContextMock context = new WebContextMock();

            Assert.IsNotNull(context.Authentication,
                "Authentication should not be null.");

            ExceptionHelper.ExpectException<NotSupportedException>(
                () => context.Authentication.Login(null));
            ExceptionHelper.ExpectException<NotSupportedException>(
                () => context.Authentication.Logout(false));
            ExceptionHelper.ExpectException<NotSupportedException>(
                () => context.Authentication.LoadUser());
            ExceptionHelper.ExpectException<NotSupportedException>(
                () => context.Authentication.SaveUser(false));

            Assert.AreEqual(context.Authentication.User, context.UserMock,
                "Users should be equal.");
            Assert.IsNotNull(context.UserMock,
                "User should not be null.");
            Assert.IsNotNull(context.UserMock.Identity,
                "Identity should not be null.");
            Assert.IsNotNull(context.UserMock.Identity.AuthenticationType,
                "Authentication type should not be null.");
            Assert.IsFalse(context.UserMock.Identity.IsAuthenticated,
                "Authentication state should be false.");
            Assert.IsNotNull(context.UserMock.Identity.Name,
                "Name should not be null.");

            Assert.IsFalse(context.UserMock.IsInRole("Role"),
                "This method should not throw.");
        }

        public void DefaultAsyncCompleted(object sender, AsyncCompletedEventArgs args) { }
    }
}
