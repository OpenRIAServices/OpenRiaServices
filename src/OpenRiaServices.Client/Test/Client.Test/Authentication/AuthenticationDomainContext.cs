using System.Collections.Generic;
using MockUser = OpenRiaServices.Client.Authentication.Test.AuthenticationDomainClient.MockUser;

namespace OpenRiaServices.Client.Authentication.Test
{
    public class AuthenticationDomainContext : AuthenticationDomainContextBase
    {
        public AuthenticationDomainContext() : base(new AuthenticationDomainClient()) { }

        internal new AuthenticationDomainClient DomainClient
        {
            get { return base.DomainClient as AuthenticationDomainClient; }
        }

        public EntityQuery<MockUser> LoginQuery(string userName, string password, bool isPersistent, string customData)
        {
            return base.CreateQuery<MockUser>(
                "Login",
                new Dictionary<string, object>()
                    {
                        { "UserName", userName },
                        { "Password", password },
                        { "IsPersistent", isPersistent },
                        { "CustomData", customData },
                    },
                /* hasSideEffects */ true,
                /* isComposable */  false);
        }

        public EntityQuery<MockUser> LogoutQuery()
        {
            return base.CreateQuery<MockUser>(
                "Logout",
                new Dictionary<string, object>(),
                /* hasSideEffects */ true,
                /* isComposable */ false);
        }

        public EntityQuery<MockUser> GetUserQuery()
        {
            return base.CreateQuery<MockUser>(
                "GetUser",
                new Dictionary<string, object>(),
                /* hasSideEffects */ false,
                /* isComposable */ false);
        }

        protected override EntityContainer CreateEntityContainer()
        {
            return new MockEntityContainer();
        }

        public class MockEntityContainer : EntityContainer
        {
            public MockEntityContainer()
            {
                this.CreateEntitySet<MockUser>(EntitySetOperations.Edit);
            }
        }
    }
}
