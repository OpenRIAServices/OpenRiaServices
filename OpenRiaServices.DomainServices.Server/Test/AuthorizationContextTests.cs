using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.Design;
using OpenRiaServices.DomainServices.Client.Test;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DataAnnotationsResources = OpenRiaServices.DomainServices.Server.Resource;

namespace OpenRiaServices.DomainServices.Server.Test
{
    [TestClass]
    public class AuthorizationContextTests
    {
        [TestMethod]
        [Description("AuthorizationContext template ctor with no ServiceProvider initializes correctly and throws when properties are accessed.")]
        public void AuthorizationContext_Ctor_Template_No_ServiceProvider()
        {
            // Null is allowed for IServiceProvider
            using (AuthorizationContext context = new AuthorizationContext(null))
            {

                // Random GetService request does not throw and returns null
                object service = context.GetService(typeof(string));
                Assert.IsNull(service, "Expected GetService to return null");

                // Shared validation logic for templates and container
                this.Validate_AuthorizationContext_Template(context);
                this.Validate_AuthorizationContext_ServiceContainer(context);
            }
        }

        [TestMethod]
        [Description("AuthorizationContext template ctor with ServiceProvider initializes correctly and throws when properties are accessed.")]
        public void AuthorizationContext_Ctor_Template_With_ServiceProvider()
        {
            IServiceProvider provider = new AuthorizationContextServiceProvider();
            using (AuthorizationContext context = new AuthorizationContext(provider))
            {

                // GetService should delegate to supplied provider
                string mockStringService = context.GetService(typeof(string)) as string;
                Assert.AreEqual("mockStringService", mockStringService, "Expected GetService to delegate to mock provider");

                // Shared validation logic for templates and container
                this.Validate_AuthorizationContext_Template(context);
                this.Validate_AuthorizationContext_ServiceContainer(context);
            }
        }

        [TestMethod]
        [Description("AuthorizationContext ctor parameters are validated and captured correctly")]
        public void AuthorizationContext_Ctor_And_Properties()
        {
            // Operation param cannot be null or empty
            ExceptionHelper.ExpectArgumentNullExceptionStandard(() => new AuthorizationContext(/*instance*/ null, /*operation*/ null, "operationType", /*serviceProvider*/ null, items: null), "operation");
            ExceptionHelper.ExpectArgumentNullExceptionStandard(() => new AuthorizationContext(/*instance*/ null, /*operation*/ string.Empty, "operationType", /*serviceProvider*/ null, items: null), "operation");

            // Operation param cannot be null or empty
            ExceptionHelper.ExpectArgumentNullExceptionStandard(() => new AuthorizationContext(/*instance*/ null, "operation", /*operationType*/ null, /*serviceProvider*/ null, items: null), "operationType");
            ExceptionHelper.ExpectArgumentNullExceptionStandard(() => new AuthorizationContext(/*instance*/ null, "operation", /*operationType*/ string.Empty, /*serviceProvider*/ null, items: null), "operationType");

            string instance = "mockEntity";   // type is only required to be object
            string operation = "testOp";
            string operationType = "Query";
            IServiceProvider serviceProvider = new AuthorizationContextServiceProvider();
            Dictionary<object, object> items = new Dictionary<object, object>();
            items["mockItem"] = "mockValue";

            // Fully formed valid ctor
            using (AuthorizationContext context = new AuthorizationContext(instance, operation, operationType, serviceProvider, items))
            {

                // Instance and Operation should be what the ctor set
                Assert.AreEqual(instance, context.Instance, "AuthorizationContext.Instance property failure");
                Assert.AreEqual(operation, context.Operation, "AuthorizationContext.Operation property failure");
                Assert.AreEqual(operationType, context.OperationType, "AuthorizationContext.Operation property failure");

                // The service provider gets wrapped, so verify we can ask for a service it knew how to provide
                string mockStringService = (string)context.GetService(typeof(string));
                Assert.AreEqual("mockStringService", mockStringService, "AuthorizationContext.GetService() failed to respect input service provider");

                // The items param should have been cloned, so verify it has the same content
                Assert.IsNotNull(context.Items, "AuthorizationContext.Items property failure");
                Assert.IsTrue(context.Items.ContainsKey("mockItem"), "AuthorizationContext.Items failed to clone input items");
                Assert.AreEqual("mockValue", context.Items["mockItem"], "AuthorizationContext.Items failed to set initial value correctly");

                // Add item to original.  The snapshot in the context should be unaffected.
                items["mockItem2"] = "mockValue2";
                Assert.IsFalse(context.Items.ContainsKey("mockItem2"), "AuthorizationContext items should have copied snapshot input Items");

                // Add an item to the context's Items to verify it is mutable
                context.Items["mockItem3"] = "mockValue3";
                Assert.IsTrue(context.Items.ContainsKey("mockItem3") && (string)context.Items["mockItem3"] == "mockValue3", "Could not modify AuthorizationContext.Items");

                // Share logic to validate ServiceContainer
                this.Validate_AuthorizationContext_ServiceContainer(context);
            }
        }

        [TestMethod]
        [Description("AuthorizationContext ctor using template behaves correctly")]
        public void AuthorizationContext_Ctor_From_Template()
        {
            IServiceProvider serviceProvider = new AuthorizationContextServiceProvider();
            using (AuthorizationContext template = new AuthorizationContext(serviceProvider))
            {
                // Put a known value in the template's dictionary
                IDictionary<object, object> items = template.Items;
                Assert.IsNotNull(items, "template did not autocreate Items");
                items["mockItem"] = "mockValue";

                string instance = "mockEntity";   // type is only required to be object
                string operation = "testOp";
                string operationType = "Query";

                // Call template-based ctor
                using (AuthorizationContext context = new AuthorizationContext(instance, operation, operationType, template))
                {
                    // Instance and Operation should be what the ctor set
                    Assert.AreEqual(instance, context.Instance, "AuthorizationContext.Instance property failure");
                    Assert.AreEqual(operation, context.Operation, "AuthorizationContext.Operation property failure");
                    Assert.AreEqual(operationType, context.OperationType, "AuthorizationContext.Operation property failure");

                    // Verify context delegates back to template's service provider
                    string mockStringService = (string)context.GetService(typeof(string));
                    Assert.AreEqual("mockStringService", mockStringService, "AuthorizationContext.GetService() failed to respect input service provider");

                    // The items param should have been cloned, so verify it has the same content
                    Assert.IsNotNull(context.Items, "AuthorizationContext.Items property failure");
                    Assert.IsTrue(context.Items.ContainsKey("mockItem"), "AuthorizationContext.Items failed to clone input items");
                    Assert.AreEqual("mockValue", context.Items["mockItem"], "AuthorizationContext.Items failed to set initial value correctly");

                    // Add item to original.  The snapshot in the context should be unaffected.
                    items["mockItem2"] = "mockValue2";
                    Assert.IsFalse(context.Items.ContainsKey("mockItem2"), "AuthorizationContext items should have copied snapshot input Items");

                    // Add an item to the context's Items to verify it is mutable
                    context.Items["mockItem3"] = "mockValue3";
                    Assert.IsTrue(context.Items.ContainsKey("mockItem3") && (string)context.Items["mockItem3"] == "mockValue3", "Could not modify AuthorizationContext.Items");

                    // Share logic to validate ServiceContainer
                    this.Validate_AuthorizationContext_ServiceContainer(context);
                }
            }
        }


        // Helper method to validate templates behavior
        public void Validate_AuthorizationContext_Template(AuthorizationContext context)
        {
            // Asking for Items is permitted and lazily creates a dictionary
            IDictionary<object, object> items = context.Items;
            Assert.IsNotNull(items, "Expected Items to lazily initialize");

            // Asking for Instance or Operation on a template should throw
            string expectedMessage = DataAnnotationsResources.AuthorizationContext_Template_Only;
            ExceptionHelper.ExpectInvalidOperationException(() => { var ignored = context.Instance; }, expectedMessage);
            ExceptionHelper.ExpectInvalidOperationException(() => { var ignored = context.Operation; }, expectedMessage);
            ExceptionHelper.ExpectInvalidOperationException(() => { var ignored = context.OperationType; }, expectedMessage);
        }

        // Helper method to validate all forms of AuthorizationContext honor ServiceContainer
        public void Validate_AuthorizationContext_ServiceContainer(AuthorizationContext context)
        {
            // Asking for a ServiceContainer creates one
            IServiceContainer container = context.GetService(typeof(IServiceContainer)) as IServiceContainer;
            Assert.IsNotNull(container, "Expected lazily created ServiceContainer");

            // The ServiceContainer should work.  Add a service, test it, and remove it
            Guid guid = Guid.NewGuid();
            container.AddService(typeof(Guid), guid);
            object service = container.GetService(typeof(Guid));
            Assert.AreEqual(guid, service, "ServiceContainer did not honor addService");

            container.RemoveService(typeof(Guid));
            service = container.GetService(typeof(Guid));
            Assert.IsNull(service, "ServiceContainer did not honor removeService");
        }

        public class AuthorizationContextServiceProvider : IServiceProvider
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
