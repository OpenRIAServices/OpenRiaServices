using System.Linq;
using System.Security.Principal;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenRiaServices.DomainServices.Server.Authentication.AspNetMembership.Test
{
    /// <summary>
    /// Tests <see cref="UserBase"/> members.
    /// </summary>
    [TestClass]
    public class UserBaseTest
    {
        private class MockUserBase : UserBase { }

        [TestMethod]
        [Description("Tests that the properties can be accessed and mutated.")]
        public void Properties()
        {
            UserBase user = new MockUserBase();

            string name = "name";

            user.Name = name;

            Assert.IsTrue(user.IsAuthenticated,
                "A user with a valid name should be authenticated.");
            Assert.AreEqual(name, user.Name,
                "Names should be equal.");

            user.Name = null;

            Assert.IsFalse(user.IsAuthenticated,
                "A user with an invalid name should be authenticated.");
            Assert.IsNull(user.Name,
                "Name should be null.");

            IPrincipal principal = user;

            Assert.IsNotNull(principal.Identity,
                "Identity should not be null.");
            Assert.AreEqual(string.Empty, principal.Identity.AuthenticationType,
                "Authentication type should be empty.");
        }

        [TestMethod]
        [Description("Tests that the Roles property can be accessed and mutated.")]
        public void Roles()
        {
            UserBase user = new MockUserBase();
            IPrincipal principal = user;

            string[] roles = new string[] { "role1", "role2" };

            user.Roles = roles;

            Assert.IsTrue(roles.SequenceEqual(user.Roles),
                "Roles should be equal.");
            foreach (string role in roles)
            {
                Assert.IsTrue(user.IsInRole(role),
                    "User should be in role");
                Assert.IsTrue(principal.IsInRole(role),
                    "Principal should be in role");
            }

            user.Roles = null;

            Assert.IsNull(user.Roles,
                "Roles should be null.");
            foreach (string role in roles)
            {
                Assert.IsFalse(user.IsInRole(role),
                    "User should not be in role");
                Assert.IsFalse(principal.IsInRole(role),
                    "Principal should not be in role");
            }
        }
    }
}
