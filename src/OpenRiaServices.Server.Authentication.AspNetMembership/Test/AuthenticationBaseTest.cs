using System;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Security.Principal;
using OpenRiaServices.Client.Test;
using OpenRiaServices.Server.Test;
using System.Web.Profile;
using System.Web.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenRiaServices.Server.Authentication.AspNetMembership.Test
{
    /// <summary>
    /// Summary description for AuthenticationBaseTest
    /// </summary>
    [TestClass]
    public class AuthenticationBaseTest
    {
        // Unfortunately we cannot invoke UpdateUser from these tests to test end-
        // to-end functionality due to the demanded principal permission. Also, 
        // we're unable to invoke GetUser directly because it depends on the
        // authenticated identity. In most cases, we'll just invoke UpdateUserCore 
        // or GetUser(IIdentity) directly instead.

        private class MockAuthentication : AuthenticationBase<MockUser>
        {
            public bool WasClearAuthenticationTokenInvoked { get; set; }
            public bool WasCreateUserInvoked { get; set; }
            public bool WasGetAnonymousUserInvoked { get; set; }
            public bool WasGetAuthenticatedUserInvoked { get; set; }
            public bool WasIssueAuthenticationTokenInvoked { get; set; }
            public bool WasUpdateUserCoreInvoked { get; set; }
            public bool WasValidateUserInvoked { get; set; }

            public bool ValidateUserMock(string username, string password)
            {
                return ValidateUser(username, password);
            }

            protected override bool ValidateUser(string username, string password)
            {
                WasValidateUserInvoked = true;
                return base.ValidateUser(username, password);
            }

            public void IssueAuthenticationTokenMock(IPrincipal principal, bool isPersistent)
            {
                IssueAuthenticationToken(principal, isPersistent);
            }

            protected override void IssueAuthenticationToken(IPrincipal principal, bool isPersistent)
            {
                WasIssueAuthenticationTokenInvoked = true;
            }

            public void ClearAuthenticationTokenMock()
            {
                ClearAuthenticationToken();
            }

            protected override void ClearAuthenticationToken()
            {
                WasClearAuthenticationTokenInvoked = true;
            }

            public MockUser GetAuthenticatedUserMock(IPrincipal principal)
            {
                return GetAuthenticatedUser(principal);
            }

            protected override MockUser GetAuthenticatedUser(IPrincipal principal)
            {
                WasGetAuthenticatedUserInvoked = true;
                return base.GetAuthenticatedUser(principal);
            }

            public MockUser GetAnonymousUserMock()
            {
                WasGetAnonymousUserInvoked = true;
                return base.GetAnonymousUser();
            }

            protected override MockUser GetAnonymousUser()
            {
                WasGetAnonymousUserInvoked = true;
                return base.GetAnonymousUser();
            }

            public MockUser CreateUserMock()
            {
                return CreateUser();
            }

            protected override MockUser CreateUser()
            {
                WasCreateUserInvoked = true;
                return base.CreateUser();
            }

            public void UpdateUserCoreMock(MockUser user)
            {
                UpdateUserCore(user);
            }

            protected override void UpdateUserCore(MockUser user)
            {
                WasUpdateUserCoreInvoked = true;
                base.UpdateUserCore(user);
            }
        }

        private class InvalidAuthentication : AuthenticationBase<MockUser>
        {
            protected override MockUser CreateUser()
            {
                return null;
            }
        }

        [TestMethod]
        [Description("Tests that Login returns an empty enumerable on failure.")]
        public void LoginFailure()
        {
            var provider = new MockAuthentication();

            SetUserInProviders(null);
            MockUser user = provider.Login(string.Empty, string.Empty, false, null);

            Assert.IsNull(user,
                "The user should be null to indicate the Login was unsuccessful.");
        }

        [TestMethod]
        [Description(
            "Tests that Login authenticates and loads a single user that matches the mock. " +
            "This test also executes the same source path as GetUser. Since GetUser relies " +
            "on the authenticated identity, it cannot otherwise be unit tested.")]
        public void LoginAndGetUser()
        {
            MockUser mockUser = MockUser.CreateInitializedUser();
            var provider = new MockAuthentication();

            SetUserInProviders(mockUser);
            MockUser user = provider.Login(mockUser.Name, mockUser.Name, false, null);

            CompareUsers(mockUser, user, true);
        }

        [TestMethod]
        [Description("Tests that Logout returns a single default user.")]
        public void Logout()
        {
            MockUser mockUser = MockUser.CreateDefaultUser();
            var provider = new MockAuthentication();

            SetUserInProviders(mockUser);
            MockUser user = provider.Logout();

            CompareUsers(mockUser, user, true);
        }

        // [TestMethod] The security demand prevents us from actually testing this
        [Description("Tests that UpdateUser updates the user data.")]
        public void UpdateUser()
        {
            MockUser original = MockUser.CreateDefaultUser();
            MockUser user = MockUser.CreateInitializedUser();
            var provider = new MockAuthentication();

            SetUserInProviders(original);
            provider.UpdateUser(user);

            CompareUsers(user, original, false);
        }

        [TestMethod]
        [Description("Tests that UpdateUser throws when called by an user different from the one being updated.")]
        public void UpdateUserThrows()
        {
            MockUser original = MockUser.CreateDefaultUser();
            MockUser user = MockUser.CreateInitializedUser();
            MockAuthentication provider =
                ServerTestHelper.CreateInitializedDomainService<MockAuthentication>(DomainOperationType.Submit);

            ExceptionHelper.ExpectException<UnauthorizedAccessException>(
                () => provider.UpdateUser(user));
        }

        [TestMethod]
        [Description("Tests that ValidateUser is invoked from Login and delegates to Membership.ValidateUser.")]
        public void ValidateUser()
        {
            MockUser mockUser = MockUser.CreateInitializedUser();
            var provider = new MockAuthentication();

            // Failure
            SetUserInProviders(null);
            MockUser user = provider.Login(string.Empty, string.Empty, false, null);

            Assert.IsTrue(provider.WasValidateUserInvoked,
                "ValidateUser should have been invoked from Login.");

            Assert.IsFalse(provider.ValidateUserMock(string.Empty, string.Empty),
                "ValidateUser should return false.");
            Assert.IsNull(user,
                "Users should be null when ValidateUser returns false.");

            provider.WasValidateUserInvoked = false;

            // Success
            SetUserInProviders(mockUser);
            user = provider.Login(mockUser.Name, mockUser.Name, false, null);

            Assert.IsTrue(provider.WasValidateUserInvoked,
                "ValidateUser should have been invoked from Login.");

            Assert.IsTrue(provider.ValidateUserMock(mockUser.Name, mockUser.Name),
                "ValidateUser should return true.");
            Assert.IsNotNull(user,
                "Users should not be null when ValidateUser returns true.");
        }

        [TestMethod]
        [Description("Tests that IssueAuthenticationToken is invoked from a successful Login.")]
        public void IssueAuthenticationToken()
        {
            MockUser mockUser = MockUser.CreateInitializedUser();
            var provider = new MockAuthentication();

            // Failure
            SetUserInProviders(null);
            MockUser user = provider.Login(string.Empty, string.Empty, false, null);

            Assert.IsFalse(provider.WasIssueAuthenticationTokenInvoked,
                "IssueAuthenticationToken should not have been invoked from a failed Login.");

            provider.WasIssueAuthenticationTokenInvoked = false;

            // Success
            SetUserInProviders(mockUser);
            user = provider.Login(mockUser.Name, mockUser.Name, false, null);

            Assert.IsTrue(provider.WasIssueAuthenticationTokenInvoked,
                "ValidateUser should have been invoked from Login.");
        }

        [TestMethod]
        [Description("Tests that ClearAuthenticationToken is invoked Logout.")]
        public void ClearAuthenticationToken()
        {
            MockUser mockUser = MockUser.CreateDefaultUser();
            var provider = new MockAuthentication();

            // Failure
            SetUserInProviders(null);
            MockUser user = provider.Logout();

            Assert.IsTrue(provider.WasClearAuthenticationTokenInvoked,
                "ClearAuthenticationToken should have been invoked from Logout.");
        }

        [TestMethod]
        [Description("Tests that GetAuthenticatedUser is invoked from a successful Login and GetUser and populates the user with values provided by the identity, Roles, and ProfileBase.")]
        public void GetAuthenticatedUser()
        {
            MockUser mockUser = MockUser.CreateInitializedUser();
            var provider = new MockAuthentication();

            SetUserInProviders(mockUser);

            // Login
            MockUser user = provider.Login(mockUser.Name, mockUser.Name, false, null);

            Assert.IsTrue(provider.WasGetAuthenticatedUserInvoked,
                "GetAuthenticatedUser should have been invoked from Login.");

            provider.WasGetAuthenticatedUserInvoked = false;

            // GetUser will always invoke GetAnonymousUser when testing

            // Login (and GetUser. See explanation on LoginAndGetUser) should return the same value as GetUser(IIdentity)
            CompareUsers(mockUser, provider.GetAuthenticatedUserMock(mockUser), true);
            CompareUsers(mockUser, user, true);
        }

        [TestMethod]
        [Description("Tests that GetAnonymousUser is invoked from Logout and GetUser and populates the user with values provided by the identity, Roles, and ProfileBase.")]
        public void GetAnonymousUser()
        {
            MockUser mockUser = MockUser.CreateDefaultUser();
            var provider = new MockAuthentication();

            SetUserInProviders(mockUser);

            // Logout
            MockUser userL = provider.Logout();

            Assert.IsTrue(provider.WasGetAnonymousUserInvoked,
                "GetAnonymousUser should have been invoked from Logout.");

            provider.WasGetAnonymousUserInvoked = false;

            // GetUser
            MockUser userGU = provider.GetUser();

            Assert.IsTrue(provider.WasGetAnonymousUserInvoked,
                "GetAnonymousUser should have been invoked from GetUser.");

            provider.WasGetAnonymousUserInvoked = false;

            // Logout should return the same value as GetAnonymousUser
            CompareUsers(mockUser, provider.GetAnonymousUserMock(), true);
            CompareUsers(mockUser, userL, true);
            CompareUsers(mockUser, userGU, true);
        }

        [TestMethod]
        [Description("Tests that GetUser throws when GetUser(IIdentity) returns null.")]
        public void GetUserThrowsOnNull()
        {
            var provider = new InvalidAuthentication();

            ExceptionHelper.ExpectException<InvalidOperationException>(
                () => provider.GetUser());
        }

        [TestMethod]
        [Description("Tests that CreateUser is invoked from GetUser(IIdentity) and return a non-null value.")]
        public void CreateUser()
        {
            MockUser mockUser = MockUser.CreateDefaultUser();
            var provider = new MockAuthentication();

            SetUserInProviders(mockUser);

            // GetUser(IIdentity)
            MockUser userGUM = provider.GetAuthenticatedUserMock(mockUser);

            Assert.IsTrue(provider.WasCreateUserInvoked,
                "CreateUser should have been invoked from GetUser(IIdentity).");

            provider.WasCreateUserInvoked = false;

            // GetAnonymousUser
            MockUser userGAU = provider.GetAnonymousUserMock();

            Assert.IsTrue(provider.WasCreateUserInvoked,
                "GetAnonymousUser should have been invoked from GetUser.");

            provider.WasGetAnonymousUserInvoked = false;

            Assert.IsNotNull(provider.CreateUserMock(),
                "User should not be null.");
        }

        [TestMethod]
        [Description("Tests that UpdateUser persists the user using ProfileBase.")]
        public void UpdateUserCore()
        {
            MockUser original = MockUser.CreateDefaultUser();
            MockUser user = MockUser.CreateInitializedUser();
            var provider = new MockAuthentication();

            SetUserInProviders(original);
            provider.UpdateUserCoreMock(user);

            CompareUsers(user, original, false);

            // These tests are only valid when we can call SetUser directly
            //MockUser originalUU = MockUser.CreateDefaultUser();

            //AspNetUserDomainServiceTest.SetUserInProviders(originalUU);
            //provider.UpdateUser(user, originalUU);

            //Assert.IsTrue(provider.WasUpdateUserCoreInvoked,
            //    "UpdateUserCore should have been invoked from UpdateUser.");

            //AspNetUserDomainServiceTest.CompareUsers(original, originalUU, false);
        }

        [TestMethod]
        [Description("Tests that Login and LoadUser correctly wrap SqlExceptions with more context.")]
        public void ConvertSqlExceptions()
        {
            MockUser mockUser = MockUser.CreateInitializedUser();
            var provider = new MockAuthentication();

            SqlException sqlEx = null;
            try
            {
                using (var badConnection = new SqlConnection("Data Source=Nosource"))
                {
                    badConnection.Open();
                }
            }
            catch (SqlException sqlEx2)
            {
                sqlEx = sqlEx2;
            }

            // Membership
            string message = string.Format(
                CultureInfo.InvariantCulture,
                Resources.ApplicationServices_ProviderError,
                "Membership", sqlEx.Message);
            ((MockMembershipProvider)Membership.Provider).Error = sqlEx;
            ExceptionHelper.ExpectException<DomainException>(() =>
                provider.Login(mockUser.Name, mockUser.Name, false, null), message);
            ((MockMembershipProvider)Membership.Provider).Error = null;

            // Roles
            message = string.Format(
                CultureInfo.InvariantCulture,
                Resources.ApplicationServices_ProviderError,
                "Role", sqlEx.Message);
            ((MockRoleProvider)Roles.Provider).Error = sqlEx;
            ExceptionHelper.ExpectException<DomainException>(() =>
                provider.GetAuthenticatedUserMock(mockUser), message);
            ((MockRoleProvider)Roles.Provider).Error = null;

            // Profile
            message = string.Format(
                CultureInfo.InvariantCulture,
                Resources.ApplicationServices_ProviderError,
                "Profile", sqlEx.Message);
            ((MockProfileProvider)ProfileManager.Provider).Error = sqlEx;
            ExceptionHelper.ExpectException<DomainException>(() =>
                provider.GetAuthenticatedUserMock(mockUser), message);
            ((MockProfileProvider)ProfileManager.Provider).Error = null;
        }

        /// <summary>
        /// Compares the expected mock with the actual value.
        /// </summary>
        /// <param name="mockUser">The expected mock user</param>
        /// <param name="user">The actual user</param>
        /// <param name="isLoadingUser">Whether the comparison is being performed
        /// for a load or save. A load will pull values from all the providers
        /// while a save will simply persist the profile.
        /// </param>
        private static void CompareUsers(MockUser mockUser, MockUser user, bool isLoadingUser)
        {
            if (isLoadingUser)
            {
                // Principal values
                Assert.IsTrue(mockUser.Roles.SequenceEqual(user.Roles),
                    "Roles should be equal.");
                Assert.AreEqual(mockUser.IsAuthenticated, user.IsAuthenticated,
                    "Authentication states should be equal.");
                Assert.AreEqual(mockUser.Name, user.Name,
                    "Names should be equal.");
            }

            // Basic types
            Assert.AreEqual(mockUser.UserBoolean, user.UserBoolean,
                "User bools should be equal.");
            Assert.AreEqual(mockUser.UserDouble, user.UserDouble,
                "User doubles should be equal.");
            Assert.AreEqual(mockUser.UserInt32, user.UserInt32,
                "User ints should be equal.");
            Assert.AreEqual(mockUser.UserString, user.UserString,
                "User strings should be equal.");

            // Calculated types
            Assert.AreEqual(mockUser.UserInt32IsGreaterThan10, user.UserInt32IsGreaterThan10,
                "Calculated user values should be equal.");

            // Aliased values
            if (isLoadingUser)
            {
                Assert.AreEqual(mockUser.AliasedString, user.UserStringAliased,
                    "Aliased user strings should be equal to alias value.");
                Assert.IsNull(user.AliasedString,
                    "The alias string should not be updated by the provider.");
            }
            else
            {
                Assert.AreEqual(mockUser.UserStringAliased, user.AliasedString,
                    "Aliased user strings should be equal to alias value.");
                Assert.IsNull(user.UserStringAliased,
                    "The alias string should not be updated by the provider.");
            }

            // ReadOnly values
            if (isLoadingUser)
            {
                Assert.AreEqual(mockUser.UserStringReadOnly, user.UserStringReadOnly,
                    "Strings should be equal.");
            }
            Assert.AreEqual(mockUser.UserStringNotReadOnly, user.UserStringNotReadOnly,
                "Strings should be equal.");

            // Overridden values
            Assert.AreEqual(mockUser.VirtualNotAliased, user.VirtualNotAliased,
                "Strings should be equal.");
            Assert.AreEqual(mockUser.VirtualInProfile, user.VirtualInProfile,
                "Strings should be equal.");
            Assert.AreEqual(mockUser.VirtualReadOnly, user.VirtualReadOnly,
                "Strings should be equal.");
            Assert.AreEqual(mockUser.VirtualNotReadOnly, user.VirtualNotReadOnly,
                "Strings should be equal.");
        }

        private static void SetUserInProviders(MockUser mockUser)
        {
            ((MockMembershipProvider)Membership.Provider).User = mockUser;
            if (Roles.Enabled)
            {
                ((MockRoleProvider)Roles.Provider).User = mockUser;
            }
            if (ProfileManager.Enabled)
            {
                ((MockProfileProvider)ProfileManager.Provider).User = mockUser;
            }
        }
    }
}
