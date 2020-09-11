using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.Design;
using System.Globalization;
using System.Security.Principal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using DataAnnotationsResources = OpenRiaServices.Server.Resource;

namespace OpenRiaServices.Server.Test
{
    [TestClass]
    public class RequiresAuthentiationAttributeTests
    {
        [TestMethod]
        [Description("RequiresAuthenticationAttribute.IsAuthorized is allowed for authenticated user")]
        public void RequiresAuthenticationAttribute_Authorize_Allowed_Authenticated_User()
        {
            RequiresAuthenticationAttribute attr = new RequiresAuthenticationAttribute();
            using (AuthorizationContext context = new AuthorizationContext(/*instance*/ null, "testOp", "testOpType", /*IServiceProvider*/ null, /*items*/ null))
            {
                AuthorizationResult result = attr.Authorize(this.CreateIPrincipal("name"), context);
                Assert.AreSame(result, AuthorizationResult.Allowed, "Expected authorization to be allowed on new principal");
            }
        }

        [TestMethod]
        [Description("RequiresAuthenticationAttribute.IsAuthorized is denied for anonymous user")]
        public void RequiresAuthenticationAttribute_Authorize_Denied_Anonymous_User()
        {
            RequiresAuthenticationAttribute attr = new RequiresAuthenticationAttribute();
            using (AuthorizationContext context = new AuthorizationContext(/*instance*/ null, "testOp", "testOpType", /*IServiceProvider*/ null, /*items*/ null))
            {
                AuthorizationResult result = attr.Authorize(new GenericPrincipal(WindowsIdentity.GetAnonymous(), null), context);
                Assert.AreNotSame(result, AuthorizationResult.Allowed, "Expected denied result for anon user");

                string expectedMessage = String.Format(CultureInfo.CurrentCulture, Resource.AuthorizationAttribute_Default_Message, context.Operation);
                Assert.AreEqual(expectedMessage, result.ErrorMessage, "Expected to see default denial error message");
            }
        }

        private IPrincipal CreateIPrincipal(string name, params string[] roles)
        {
            return new GenericPrincipal(new GenericIdentity(name), roles);
        }
    }
}
