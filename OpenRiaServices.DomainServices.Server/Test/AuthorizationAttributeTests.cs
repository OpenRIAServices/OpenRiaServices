using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Security.Principal;
using OpenRiaServices.DomainServices.Client.Test;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenRiaServices.DomainServices.Server.Test
{
    [TestClass]
    public class AuthorizationAttributeTests
    {
        [TestMethod]
        [Description("AuthorizationAttribute ctor and properties work properly")]
        public void AuthorizationAttribute_Ctor_And_Properties()
        {
            AuthorizationAttribute attr = new TestAuthorizationAttribute();

            Assert.IsNull(attr.ErrorMessage, "AuthorizationAttribute.ErrorMessage should default to null");
            Assert.IsNull(attr.ResourceType, "AuthorizationAttribute.ResourceType should default to null");
        }

        [TestMethod]
        [Description("AuthorizationAttribute FormatErrorMessage handles all known combinations of ErrorMessage + ResourceType")]
        public void AuthorizationAttribute_FormatErrorMessage()
        {
            // Default attr with no error message generates default
            TestAuthorizationAttribute attr = new TestAuthorizationAttribute();
            string expectedMessage = string.Format(CultureInfo.CurrentCulture, Resource.AuthorizationAttribute_Default_Message, "testOp");
            string message = attr.InternalFormatErrorMessage("testOp");
            Assert.AreEqual(expectedMessage, message, "Default FormatErrorMessage was not correct");

            // Literal error message (no resource type) generates formatted message
            attr = new TestAuthorizationAttribute() { ErrorMessage = "testMessage: {0}" };
            expectedMessage = string.Format(CultureInfo.CurrentCulture, "testMessage: {0}", "testOp");
            message = attr.InternalFormatErrorMessage("testOp");
            Assert.AreEqual(expectedMessage, message, "Default FormatErrorMessage with literal error message was not correct");

            // ResourceType + legal message formats correctly
            attr = new TestAuthorizationAttribute() { ErrorMessage = "Message", ResourceType = typeof(AuthorizationResources) };
            expectedMessage = string.Format(CultureInfo.CurrentCulture, AuthorizationResources.Message, "testOp");
            message = attr.InternalFormatErrorMessage("testOp");
            Assert.AreEqual(expectedMessage, message, "FormatErrorMessage with type + error message was not correct");

            // Negative tests
            // ResourceType with empty message throws
            attr = new TestAuthorizationAttribute() { ResourceType = typeof(AuthorizationResources) };
            expectedMessage = string.Format(CultureInfo.CurrentCulture, Resource.AuthorizationAttribute_Requires_ErrorMessage, attr.GetType().FullName, typeof(AuthorizationResources).FullName);
            ExceptionHelper.ExpectInvalidOperationException(() => attr.InternalFormatErrorMessage("testOp"), expectedMessage);

            // ResourceType with non-resourced message throws
            attr = new TestAuthorizationAttribute() { ResourceType = typeof(AuthorizationResources), ErrorMessage = "NotAProperty" };
            expectedMessage = string.Format(CultureInfo.CurrentCulture, Resource.AuthorizationAttribute_Requires_Valid_Property, typeof(AuthorizationResources).FullName, "NotAProperty");
            ExceptionHelper.ExpectInvalidOperationException(() => attr.InternalFormatErrorMessage("testOp"), expectedMessage);

            // ResourceType with non-static message throws
            attr = new TestAuthorizationAttribute() { ResourceType = typeof(AuthorizationResources), ErrorMessage = "NotStaticMessage" };
            expectedMessage = string.Format(CultureInfo.CurrentCulture, Resource.AuthorizationAttribute_Requires_Valid_Property, typeof(AuthorizationResources).FullName, "NotStaticMessage");
            ExceptionHelper.ExpectInvalidOperationException(() => attr.InternalFormatErrorMessage("testOp"), expectedMessage);

            // ResourceType with non-string message throws
            attr = new TestAuthorizationAttribute() { ResourceType = typeof(AuthorizationResources), ErrorMessage = "NotStringMessage" };
            expectedMessage = string.Format(CultureInfo.CurrentCulture, Resource.AuthorizationAttribute_Requires_Valid_Property, typeof(AuthorizationResources).FullName, "NotStringMessage");
            ExceptionHelper.ExpectInvalidOperationException(() => attr.InternalFormatErrorMessage("testOp"), expectedMessage);

            // ResourceType with non-public message throws
            attr = new TestAuthorizationAttribute() { ResourceType = typeof(AuthorizationResources), ErrorMessage = "NotPublicMessage" };
            expectedMessage = string.Format(CultureInfo.CurrentCulture, Resource.AuthorizationAttribute_Requires_Valid_Property, typeof(AuthorizationResources).FullName, "NotPublicMessage");
            ExceptionHelper.ExpectInvalidOperationException(() => attr.InternalFormatErrorMessage("testOp"), expectedMessage);
        }

        [TestMethod]
        [Description("AuthorizationAttribute invoke with no authorization attributes is authorized")]
        public void AuthorizationAttribute_Invoke_Allowed_No_Attributes()
        {
            IPrincipal user = this.CreateIPrincipal("user1");

            // Instantiate a new DomainService to use for an Invoke
            using (AuthorizationTestDomainService testDomainService = new AuthorizationTestDomainService())
            {
                testDomainService.Initialize(new DomainServiceContext(new MockDataService(user), DomainOperationType.Invoke));

                // Get a DomainServiceDescription for that same domain service
                DomainServiceDescription description = DomainServiceDescription.GetDescription(typeof(AuthorizationTestDomainService));

                // Locate the invoke method
                DomainOperationEntry invokeEntry = description.DomainOperationEntries.SingleOrDefault(p => p.Name == "InvokeNoAuth");
                Assert.IsNotNull(invokeEntry, "Could not locate InvokeNoAuth invoke");
                Assert.AreEqual("Invoke", invokeEntry.OperationType, "Invoke operation entry should show Invoke operation type");

                // Ask the domain service to perform authorization.
                // The principal will be located via the mock data service created above.
                // Invokes do not expect an entity instance.
                AuthorizationResult result = testDomainService.IsAuthorized(invokeEntry, entity: null);

                Assert.AreSame(AuthorizationResult.Allowed, result, "Expected invoke with no auth attributes to be allowed.");
            }
        }

        [TestMethod]
        [Description("DomainService.IsAuthorized allows or denies an invoke with custom AuthorizationAttribute")]
        public void AuthorizationAttribute_Invoke_Custom_Attribute()
        {
            IPrincipal user = this.CreateIPrincipal("user1");

            // Instantiate a new DomainService to use for an Invoke
            using (AuthorizationTestDomainService testDomainService = new AuthorizationTestDomainService())
            {
                testDomainService.Initialize(new DomainServiceContext(new MockDataService(user), DomainOperationType.Invoke));

                // Get a DomainServiceDescription for that same domain service
                DomainServiceDescription description = DomainServiceDescription.GetDescription(typeof(AuthorizationTestDomainService));

                // Locate the invoke method
                DomainOperationEntry invokeEntry = description.DomainOperationEntries.Single(p => p.Name == "InvokeAllow");

                // Ask the domain service to perform authorization.
                // The principal will be located via the mock data service created above.
                // Invokes do not expect an entity instance.
                AuthorizationResult result = testDomainService.IsAuthorized(invokeEntry, entity: null);

                Assert.AreSame(AuthorizationResult.Allowed, result, "Expected invoke with custom auth attributes to be allowed.");

                // Do that again but using an Invoke that will deny based on its own name
                invokeEntry = description.DomainOperationEntries.Single(p => p.Name == "InvokeDeny");
                result = testDomainService.IsAuthorized(invokeEntry, entity: null);
                Assert.AreNotSame(AuthorizationResult.Allowed, result, "Expected invoke with denying custom attributes to be denied.");
            }
        }

        [TestMethod]
        [Description("DomainService.IsAuthorized allows or denies a query with a custom AuthorizationAttribute")]
        public void AuthorizationAttribute_Query_Custom_Attribute()
        {
            IPrincipal user = this.CreateIPrincipal("user1");

            // Instantiate a new DomainService to use for a Query
            using (AuthorizationTestDomainService testDomainService = new AuthorizationTestDomainService())
            {
                testDomainService.Initialize(new DomainServiceContext(new MockDataService(user), DomainOperationType.Query));

                // Get a DomainServiceDescription for that same domain service
                DomainServiceDescription description = DomainServiceDescription.GetDescription(typeof(AuthorizationTestDomainService));

                // Locate the QueryAllow query
                DomainOperationEntry entry = description.DomainOperationEntries.SingleOrDefault(p => p.Name == "QueryAllow");
                Assert.IsNotNull(entry, "Did not find QueryAllow entry");
                Assert.AreEqual("Query", entry.OperationType, "Query operation type expected for query operation");

                // Ask the domain service to perform authorization.
                // The principal will be located via the mock data service created above.
                AuthorizationResult result = testDomainService.IsAuthorized(entry, entity: null);

                if (result != AuthorizationResult.Allowed)
                    Assert.Fail("Expected QueryAllow to be approved: " + result.ErrorMessage);

                // Try again with a different query that will be denied
                entry = description.DomainOperationEntries.SingleOrDefault(p => p.Name == "QueryDeny");
                Assert.IsNotNull(entry, "Did not find QueryDeny entry");

                result = testDomainService.IsAuthorized(entry, entity: null);

                Assert.AreNotSame(AuthorizationResult.Allowed, result, "Expected QueryDeny to be denied");
            }
        }

        [TestMethod]
        [Description("DomainService.IsAuthorized allows or denies a custom method with a custom AuthorizationAttribute")]
        public void AuthorizationAttribute_Custom_Method_Custom_Attribute()
        {
            IPrincipal user = this.CreateIPrincipal("user1");

            // Instantiate a new DomainService to use for a custom method via a Submit
            using (AuthorizationTestDomainService testDomainService = new AuthorizationTestDomainService())
            {
                testDomainService.Initialize(new DomainServiceContext(new MockDataService(user), DomainOperationType.Submit));

                // Get a DomainServiceDescription for that same domain service
                DomainServiceDescription description = DomainServiceDescription.GetDescription(typeof(AuthorizationTestDomainService));

                // Locate the custom method.  It has an attribute that will deny only entities whose value is "Fred"
                DomainOperationEntry entry = description.DomainOperationEntries.SingleOrDefault(p => p.Name == "CustomUpdate");
                Assert.IsNotNull(entry, "Did not find CustomUpdate entry");
                Assert.AreEqual("Update", entry.OperationType, "Custom op entry should show op type Update");

                // Ask the domain service to perform authorization.
                // The principal will be located via the mock data service created above.
                AuthorizationTestEntity entity = new AuthorizationTestEntity() { TheValue = "Bob" };
                AuthorizationResult result = testDomainService.IsAuthorized(entry, entity);

                if (result != AuthorizationResult.Allowed)
                    Assert.Fail("Expected custom method to be approved: " + result.ErrorMessage);

                // Now set it to an illegal value and verify deny
                entity.TheValue = "Fred";
                result = testDomainService.IsAuthorized(entry, entity);

                Assert.AreNotSame(AuthorizationResult.Allowed, result, "Expected custom method to be denied");
                Assert.AreEqual("uhuh", result.ErrorMessage, "Expected denial to return explicit message from code");
            }
        }

        [TestMethod]
        [Description("DomainService.IsAuthorized allows a custom method with a null entity when used in a Metadata context")]
        public void AuthorizationAttribute_Custom_Method_Custom_Attribute_Metadata_Context()
        {
            IPrincipal user = this.CreateIPrincipal("user1");

            // Instantiate a new DomainService to use for a custom method via a Submit
            using (AuthorizationTestDomainService testDomainService = new AuthorizationTestDomainService())
            {
                // Note: Metadata context
                testDomainService.Initialize(new DomainServiceContext(new MockDataService(user), DomainOperationType.Metadata));
                DomainServiceDescription description = DomainServiceDescription.GetDescription(typeof(AuthorizationTestDomainService));
                DomainOperationEntry entry = description.DomainOperationEntries.SingleOrDefault(p => p.Name == "CustomUpdate");
                Assert.IsNotNull(entry, "Did not find CustomUpdate entry");

                AuthorizationTestEntity entity = null;
                AuthorizationResult result = testDomainService.IsAuthorized(entry, entity);

                if (result != AuthorizationResult.Allowed)
                    Assert.Fail("Expected custom method to be approved during metadata use: " + result.ErrorMessage);
            }
        }

        [TestMethod]
        [Description("DomainService.IsAuthorized of a custom method with a null entity throws in a Submit context")]
        public void AuthorizationAttribute_Custom_Method_Custom_Attribute_Null_Entity_Throws()
        {
            IPrincipal user = this.CreateIPrincipal("user1");

            // Instantiate a new DomainService to use for a custom method via a Submit
            using (AuthorizationTestDomainService testDomainService = new AuthorizationTestDomainService())
            {
                // Note: Submit context enforces null entity test
                testDomainService.Initialize(new DomainServiceContext(new MockDataService(user), DomainOperationType.Submit));

                DomainServiceDescription description = DomainServiceDescription.GetDescription(typeof(AuthorizationTestDomainService));
                DomainOperationEntry entry = description.DomainOperationEntries.SingleOrDefault(p => p.Name == "CustomUpdate");
                Assert.IsNotNull(entry, "Did not find CustomUpdate entry");

                // The null entity here should raise ArgNull because the context is Submit
                AuthorizationTestEntity entity = null;
                ExceptionHelper.ExpectArgumentNullExceptionStandard(() => testDomainService.IsAuthorized(entry, entity), "entity");
            }
        }

        [OpenRiaServices.DomainServices.Hosting.EnableClientAccess]
        public class AuthorizationTestDomainService : DomainService
        {
            [Invoke]
            public void InvokeNoAuth() { }

            [Invoke]
            [TestAuthorization(ExpectedOperation="InvokeAllow", ExpectedOperationType="Invoke")]
            public void InvokeAllow() { }

            [Invoke]
            [TestAuthorization(ExpectedOperation="InvokeDeny", ExpectedOperationType="Invoke", DenyOperation = "InvokeDeny")]
            public void InvokeDeny() { }

            [Query]
            public IEnumerable<AuthorizationTestEntity> Query_No_Auth() { return null; }

            // This query will allow authorization because its name does not match the attribute
            [Query]
            [TestAuthorization(ExpectedOperation="QueryAllow", ExpectedOperationType="Query", DenyOperation = "QueryDeny")]
            public IEnumerable<AuthorizationTestEntity> QueryAllow() { return null; }

            // This query will deny authorization because its name matches the attribute
            [Query]
            [TestAuthorization(ExpectedOperation="QueryDeny", ExpectedOperationType="Query", DenyOperation = "QueryDeny")]
            public IEnumerable<AuthorizationTestEntity> QueryDeny() { return null; }

            // This custom method will deny an entity containing "Fred" and will
            // deny with a message coming from outside normal AuthAttr values
            [Update(UsingCustomMethod = true)]
            [TestAuthorization(ExpectedOperation="CustomUpdate", ExpectedOperationType="Update", DenyInstanceValue = "Fred", DenyMessage="uhuh")]
            public void CustomUpdate(AuthorizationTestEntity entity) { }

        }

        public class AuthorizationTestEntity
        {
            [Key]
            public string TheValue { get; set; }
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

        /// <summary>
        /// Concrete form of AuthorizationAttribute we can instantiate and probe.
        /// It contains properties we can set to control what is denied.
        /// </summary>
        [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
        public class TestAuthorizationAttribute : AuthorizationAttribute
        {
            // If set, assert Operation is this at auth time
            public string ExpectedOperation { get; set; }

            // If set, assert OperationType is that at auth time
            public string ExpectedOperationType { get; set; }

            // Deny this specific operation
            public string DenyOperation { get; set; }

            // Deny this specific operation type
            public string DenyOperationType { get; set; }

            // Deny any instance with this value
            public string DenyInstanceValue { get; set; }

            // Custom message to use in denials (to verify it can ignore what is in ErrorMessag)
            public string DenyMessage { get; set; }

            public string InternalFormatErrorMessage(string operation)
            {
                return this.FormatErrorMessage(operation);
            }

            protected override AuthorizationResult IsAuthorized(IPrincipal principal, AuthorizationContext authorizationContext)
            {
                Assert.IsNotNull(principal, "Principal was null when custom authorization attribute was called");
                Assert.IsNotNull(authorizationContext, "AuthorizationContext was null when custom authorization attribute was called");
                Assert.IsFalse(string.IsNullOrEmpty(authorizationContext.Operation), "Operation was blank when custom authorization attribute was called");
                Assert.IsFalse(string.IsNullOrEmpty(authorizationContext.OperationType), "OperationType was blank when custom authorization attribute was called");

                if (!string.IsNullOrEmpty(this.ExpectedOperation))
                {
                    Assert.AreEqual(this.ExpectedOperation, authorizationContext.Operation, "AuthContext.Operation was not correct when attribute was evaluated");
                }

                if (!string.IsNullOrEmpty(this.ExpectedOperationType))
                {
                    Assert.AreEqual(this.ExpectedOperationType, authorizationContext.OperationType, "AuthContext.OperationType was not correct when attribute was evaluated");
                }

                bool isInvoke = authorizationContext.OperationType.Equals("Invoke");

                AuthorizationTestEntity entity = authorizationContext.Instance as AuthorizationTestEntity;
                object instanceType = null;
                bool haveType = authorizationContext.Items.TryGetValue(typeof(Type), out instanceType);

                Assert.IsTrue(haveType, "Did not find entity type element in Items for " + authorizationContext.Operation);

                // Invokes don't guarantee the Type in the dictionary
                Assert.IsTrue(isInvoke || instanceType != null, "Entity type was null in Items diction for an Invoke");
                
                bool deny = ((!string.IsNullOrEmpty(this.DenyOperation) && this.DenyOperation.Equals(authorizationContext.Operation)) ||
                               (!string.IsNullOrEmpty(this.DenyOperationType) && this.DenyOperationType.Equals(authorizationContext.OperationType)) ||
                               (!string.IsNullOrEmpty(this.DenyInstanceValue) && entity != null && this.DenyInstanceValue.Equals(entity.TheValue)));

 
                return deny
                        ? string.IsNullOrEmpty(this.DenyMessage)
                            ? new AuthorizationResult(this.FormatErrorMessage(authorizationContext.Operation))
                            : new AuthorizationResult(this.DenyMessage)
                        : AuthorizationResult.Allowed;
            }
        }

        public class AuthorizationResources
        {
            public static string Message { get { return "This is message: {0}"; } }

            public string NotStaticMessage { get { return "This is not a static message"; } }

            public int NotStringMessage { get { return 5; } }

            private static string NotPublicMessage { get { return "This is not a public message"; } }
        }
    }
}
