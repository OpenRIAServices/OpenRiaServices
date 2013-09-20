using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.Design;
using System.Globalization;
using System.Security.Principal;
using System.ServiceModel.DomainServices.Client.Test;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

using DataAnnotationsResources = System.ServiceModel.DomainServices.Server.Resource;

namespace System.ServiceModel.DomainServices.Server.Test
{
    [TestClass]
    public class RequiresRoleAttributeTests
    {
        [TestMethod]
        [Description("RequiresRoleAttribute throws if attempt to authorize with incorrectly set Roles")]
        public void RequiresRoleAttribute_NoRolesDefined_Throws()
        {
            // Validate that we can access all forms of the Roles property without an exception
            var ignored = new RequiresRoleAttribute().Roles;
            ignored = new RequiresRoleAttribute((string)null).Roles;
            ignored = new RequiresRoleAttribute((string[])null).Roles;

            // But attempting do authorization with null roles throws
            using (AuthorizationContext context = new AuthorizationContext(/*instance*/ null, "testOp", "testOpType", /*IServiceProvider*/ null, /*items*/ null))
            {
                ExceptionHelper.ExpectInvalidOperationException(
                    () => { new RequiresRoleAttribute((string[])null).Authorize(this.CreateIPrincipal("John Doe"), context); },
                    Resource.RequiresRoleAttribute_MustSpecifyRole);
            }
        }

        [TestMethod]
        [Description("RequiresRoleAttribute authorization allows and denies properly for single attribute")]
        public void RequiresRoleAttribute_Authorize_SingleAttribute()
        {
            IPrincipal user1 = this.CreateIPrincipal("user1", "role1");
            IPrincipal user2 = this.CreateIPrincipal("user1", "role2");
            RequiresRoleAttribute requireRole1 = new RequiresRoleAttribute("role1");
            using (AuthorizationContext context = new AuthorizationContext(/*instance*/ null, "testOp", "testOpType", /*IServiceProvider*/ null, /*items*/ null))
            {

                // user in role1 should be allowed
                AuthorizationResult result = requireRole1.Authorize(user1, context);
                Assert.AreSame(AuthorizationResult.Allowed, result, "Expected user in role1 to be authorized when only role1 is permitted");

                // user in role2 should be denied
                result = requireRole1.Authorize(user2, context);
                Assert.AreNotSame(AuthorizationResult.Allowed, result, "Expected user in role2 to be denied when only role1 is permitted");

                // Denial error message should reflect default plus operation
                string expectedMessage = String.Format(CultureInfo.CurrentCulture, Resource.AuthorizationAttribute_Default_Message, context.Operation);
                Assert.AreEqual(expectedMessage, result.ErrorMessage, "Expected to see default denial error message");

                // user in role1 should be allowed if role1 + role2 + role3 are permitted
                RequiresRoleAttribute requireRole123 = new RequiresRoleAttribute(new string[] { "role1", "role2", "role3" });
                result = requireRole123.Authorize(user1, context);
                Assert.AreSame(AuthorizationResult.Allowed, result, "Expected user1 in role1 to be authorized when role1, role2, and role3 are all permitted");

                // user is in multiple roles (1, 2, and 3) should be allowed if any of these 3 roles are allowed
                IPrincipal user13 = this.CreateIPrincipal("user1", "role1", "role3");
                result = requireRole123.Authorize(user13, context);
                Assert.AreSame(AuthorizationResult.Allowed, result, "Expected user1 in role1 and role3 to be authorized when role1, role2, and role3 are all permitted");

                // user is in none of the required roles
                RequiresRoleAttribute requireRole567 = new RequiresRoleAttribute(new string[] { "role5", "role6", "role7" });
                result = requireRole567.Authorize(user1, context);
                Assert.AreNotSame(AuthorizationResult.Allowed, result, "Expected user in role1 to be denied when only roles 5, 6, and 7 are allowed");
            }
        }

        [TestMethod]
        [Description("RequiresRoleAttribute authorization is denied when roles  AND'ed across multiple attributes in a DomainService invoke are insufficient")]
        public void RequiresRoleAttribute_Authorize_MultipleAttributes_Denied()
        {
            // Create user in only role1, which should be denied because we require (1 or 2) AND (3 or 4)
            IPrincipal user = this.CreateIPrincipal("user1", "role1");

            // Instantiate a new DomainService to use for an Invoke
            using (RequiresRoleTestService testDomainService = new RequiresRoleTestService())
            {
                testDomainService.Initialize(new DomainServiceContext(new MockDataService(user), DomainOperationType.Invoke));

                // Get a DomainServiceDescription for that same domain service
                DomainServiceDescription description = DomainServiceDescription.GetDescription(typeof(RequiresRoleTestService));

                // Locate the invoke method
                DomainOperationEntry invokeEntry = description.DomainOperationEntries.Single(p => p.Name == "Method1");

                // Ask the domain service to perform authorization.
                // The principal will be located via the mock data service created above.
                // Invokes do not expect an entity instance.
                AuthorizationResult result = testDomainService.IsAuthorized(invokeEntry, entity: null);

                Assert.AreNotSame(AuthorizationResult.Allowed, result, "Expected user in role1 to be denied against invoke requiring roles (1 or 2) && (3 or 4) in multiple attributes");

                // Validate the formatted denial message includes the invoke we attempted
                string expectedMessage = String.Format(CultureInfo.CurrentCulture, Resource.AuthorizationAttribute_Default_Message, "Method1");
                Assert.AreEqual(expectedMessage, result.ErrorMessage, "Expected default denial message plus name of the invoke method");
            }
        }

        [TestMethod]
        [Description("RequiresRoleAttribute authorization is allowed when roles AND'ed across multiple attributes in a DomainService are met")]
        public void RequiresRoleAttribute_Authorize_MultipleAttributes_Allowed()
        {
            IPrincipal user = this.CreateIPrincipal("user1", "role1", "role4");

            // Instantiate a new DomainService to use for an Invoke
            using (RequiresRoleTestService testDomainService = new RequiresRoleTestService())
            {
                testDomainService.Initialize(new DomainServiceContext(new MockDataService(user), DomainOperationType.Invoke));

                // Get a DomainServiceDescription for that same domain service
                DomainServiceDescription description = DomainServiceDescription.GetDescription(typeof(RequiresRoleTestService));

                // Locate the invoke method
                DomainOperationEntry invokeEntry = description.DomainOperationEntries.Single(p => p.Name == "Method1");

                AuthorizationResult result = testDomainService.IsAuthorized(invokeEntry, entity: null);

                Assert.AreSame(AuthorizationResult.Allowed, result, "Expected user in role1 and role4 to be allowed against invoke requiring roles (1 or 2) && (3 or 4) in multiple attributes");
            }
        }

        [TestMethod]
        [Description("RequiresRoleAttribute overrides TypeID for AllowMultiple true")]
        public void RequiresRoleAttribute_TypeId()
        {
            // TypeDescriptionProvider
            RequiresRoleAttribute attr1 = new RequiresRoleAttribute("role1");
            RequiresRoleAttribute attr2 = new RequiresRoleAttribute("role2");

            Assert.AreNotEqual(attr1.TypeId, attr2.TypeId, "TypeID should be different for different attributes");
        }

        [System.ServiceModel.DomainServices.Hosting.EnableClientAccess]
        public class RequiresRoleTestService : DomainService
        {
            [RequiresRole("role1", "role2")]
            [RequiresRole("role3", "role4")]
            [Invoke]
            public void Method1() { }
        }

        private IPrincipal CreateIPrincipal(string name, params string[] roles)
        {
            return new GenericPrincipal(new GenericIdentity(name), roles);
        }

        public class MockServiceProvider : IServiceProvider
        {
            public object GetService(Type serviceType)
            {
                if (serviceType == typeof(string))
                {
                    return "mockStringService";
                }
                return null;
            }
        }
    }
}
