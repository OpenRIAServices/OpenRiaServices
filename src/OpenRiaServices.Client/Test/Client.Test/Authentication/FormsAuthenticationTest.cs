﻿using OpenRiaServices.Client.Test;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Silverlight.Testing;
using RootNamespace.TestNamespace;

namespace OpenRiaServices.Client.Authentication.Test
{
    /// <summary>
    /// Tests <see cref="FormsAuthentication"/> members.
    /// </summary>
    [TestClass]
    public class FormsAuthenticationTest : UnitTestBase
    {
        [TestMethod]
        [Description("Tests that the user can be authenticated with the server.")]
        [Asynchronous]
        public void LoginThenLogout()
        {
            AuthenticationService1 context = new AuthenticationService1(TestURIs.AuthenticationService1);
            FormsAuthentication service = new FormsAuthentication();

            service.DomainContext = context;

            AuthenticationOperation ao = service.Login("manager", "manager");

            this.EnqueueCompletion(() => ao);

            this.EnqueueCallback(() =>
            {
                Assert.IsTrue(ao.User.Identity.IsAuthenticated,
                    "Logged in user should be authenticated.");
                ao = service.Logout(false);
            });

            this.EnqueueCompletion(() => ao);

            this.EnqueueCallback(() =>
            {
                Assert.IsFalse(ao.User.Identity.IsAuthenticated,
                    "Logged out user should not be authenticated.");
                ao = service.Logout(false);
            });

            this.EnqueueTestComplete();
        }

        [TestMethod]
        [Description("Tests that the user default is valid before anything has been loaded from the server.")]
        public void Defaults()
        {
            AuthenticationService1 context = new AuthenticationService1(TestURIs.AuthenticationService1);
            FormsAuthentication service = new FormsAuthentication();

            service.DomainContext = context;
            Assert.IsNotNull(service.User,
                "User should not be null.");
            Assert.IsNotNull(service.User.Identity,
                "Identity should not be null.");
            Assert.IsNotNull(service.User.Identity.AuthenticationType,
                "Authentication type should not be null.");
            Assert.IsFalse(service.User.Identity.IsAuthenticated,
                "Authentication state should be false.");
            Assert.IsNotNull(service.User.Identity.Name,
                "Name should not be null.");

            Assert.IsFalse(service.User.IsInRole("Role"),
                "This method should not throw.");
        }
    }
}
