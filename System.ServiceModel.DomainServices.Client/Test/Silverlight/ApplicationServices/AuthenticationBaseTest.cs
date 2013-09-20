using System.Linq;
using System.Web.Ria.Data;
using System.Windows.Ria.Data;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DescriptionAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;
using System.Windows.Data.Test.Utilities;

namespace System.Windows.Ria.ApplicationServices.UnitTests
{
    /// <summary>
    /// Tests <see cref="AuthenticationBase"/> members.
    /// </summary>
    [TestClass]
    public class AuthenticationBaseTest : SilverlightTest
    {
        public class MockUser : UserBase
        {
            public override object GetIdentity() { return string.Empty; }
        }

        public class MockEntityContainer : EntityContainer
        {
            public MockEntityContainer()
            {
                this.CreateEntityList<MockUser>(EntityListOperations.Edit);
            }
        }

        public class MockAuthenticationBase : AuthenticationBase
        {
            [LoadMethod(typeof(MockUser))]
            public void LoadUser() { }

            protected override EntityContainer CreateEntityContainer()
            {
                return new MockEntityContainer();
            }
        }

        public class InvalidEntityContainer : EntityContainer { }

        public class InvalidAuthenticationBase : AuthenticationBase
        {
            protected override EntityContainer CreateEntityContainer()
            {
                return new InvalidEntityContainer();
            }
        }

        [TestMethod]
        [Description("Tests that UserType can determine the correct user type")]
        public void UserType()
        {
            MockAuthenticationBase mock = new MockAuthenticationBase();
            Assert.AreEqual(typeof(MockUser), mock.UserType,
                "UserType should be MockUser.");

            InvalidAuthenticationBase invalid = new InvalidAuthenticationBase();
            ExceptionHelper.ExpectException<InvalidOperationException>(
                () => Assert.IsNull(invalid.UserType, "UserType should throw an exception."));
        }

        [TestMethod]
        [Description("Tests that UserList can determine the correct user list")]
        public void UserList()
        {
            MockAuthenticationBase mock = new MockAuthenticationBase();
            Assert.IsNotNull(mock.UserList,
                "UserList should not be null.");

            InvalidAuthenticationBase invalid = new InvalidAuthenticationBase();
            ExceptionHelper.ExpectException<InvalidOperationException>(
                () => Assert.IsNull(invalid.UserList, "UserType should throw an exception."));
        }
    }
}
