using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Client.Test;
using TestDomainServices;

namespace OpenRiaServices.Server.Test
{
    /// <summary>
    /// Summary description for InvokeOperationServerTest
    /// </summary>
    [TestClass]
    public class InvokeOperationServerTest
    {
        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        [TestMethod]
        [Description("Verify getting description for domain service with valid invoke operation signatures does not throw")]
        public void DomainServiceWithMultipleValidOnlineMethods()
        {
            DomainServiceDescription description = DomainServiceDescription.GetDescription(typeof(OnlineMethod_ValidProvider_MultipleMethods));
            Assert.IsNotNull(description);

            // verify GetOnlineMethod returns the correct DomainOperationEntry
            DomainOperationEntry returnedMethod = description.GetInvokeOperation("Process_VoidReturn");
            Assert.IsNotNull(returnedMethod);
            Assert.AreEqual(DomainOperation.Invoke, returnedMethod.Operation);
            Assert.AreEqual("Process_VoidReturn", returnedMethod.Name);

            // verify GetOnlineMethods return all the invoke operations on this provider
            IEnumerable<DomainOperationEntry> returnedMethods = description.DomainOperationEntries.Where(p => p.Operation == DomainOperation.Invoke);
            Assert.IsNotNull(returnedMethods);
            Assert.AreEqual(10, returnedMethods.Count());
            Assert.IsTrue(returnedMethods.Any(p => p.Name == "Process_EntitiesAndSimpleParams"));

            Assert.IsTrue(returnedMethods.Any(p => p.Name == "Process_Return_EntityListParam"));
        }

        [TestMethod]
        [Description("Verify defining multiple invoke operations with the same name throws")]
        public void InvalidProvider_DupMethodName()
        {
            ExceptionHelper.ExpectException<InvalidOperationException>(delegate
            {
                DomainServiceDescription description = DomainServiceDescription.GetDescription(typeof(OnlineMethod_InvalidProvider_DupMethodName));
            }, string.Format(CultureInfo.CurrentCulture, Resource.DomainOperationEntryOverload_NotSupported, "TestMethod"));
        }

        [TestMethod]
        [Description("Verify invoke operation parameter and return types must be an entity type or a predefined serializable type")]
        public void InvalidProvider_BadParamAndReturnTypes()
        {
            // param type is not an entity defined on the provider
            ExceptionHelper.ExpectException<InvalidOperationException>(delegate
            {
                DomainServiceDescription description = DomainServiceDescription.GetDescription(typeof(OnlineMethod_InvalidProvider_NonEntityParam));
            }, string.Format(CultureInfo.CurrentCulture, Resource.InvalidInvokeOperation_ParameterType, "TestMethod"));

            // return type is not an entity defined on the provider
            ExceptionHelper.ExpectException<InvalidOperationException>(delegate
            {
                DomainServiceDescription description = DomainServiceDescription.GetDescription(typeof(OnlineMethod_InvalidProvider_NonEntityReturn));
            }, string.Format(CultureInfo.CurrentCulture, Resource.InvalidInvokeOperation_ReturnType, "TestMethod"));

            // return type is not one of the predefined types
            ExceptionHelper.ExpectException<InvalidOperationException>(delegate
            {
                DomainServiceDescription description = DomainServiceDescription.GetDescription(typeof(OnlineMethod_InvalidProvider_NonSimpleReturn));
            }, string.Format(CultureInfo.CurrentCulture, Resource.InvalidInvokeOperation_ReturnType, "TestMethod"));

            // param type is not one of the predefined types
            ExceptionHelper.ExpectException<InvalidOperationException>(delegate
            {
                DomainServiceDescription description = DomainServiceDescription.GetDescription(typeof(OnlineMethod_InvalidProvider_NonSimpleParam));
            }, string.Format(CultureInfo.CurrentCulture, Resource.InvalidInvokeOperation_ParameterType, "TestMethod"));

            ExceptionHelper.ExpectException<InvalidOperationException>(delegate
            {
                DomainServiceDescription description = DomainServiceDescription.GetDescription(typeof(OnlineMethod_InvalidProvider_ArgOfTypeIntPtr));
            }, string.Format(CultureInfo.CurrentCulture, Resource.InvalidInvokeOperation_ParameterType, "TestMethod"));

            ExceptionHelper.ExpectException<InvalidOperationException>(delegate
            {
                DomainServiceDescription description = DomainServiceDescription.GetDescription(typeof(OnlineMethod_InvalidProvider_ArgOfTypeUIntPtr));
            }, string.Format(CultureInfo.CurrentCulture, Resource.InvalidInvokeOperation_ParameterType, "TestMethod"));

            ExceptionHelper.ExpectException<InvalidOperationException>(delegate
            {
                DomainServiceDescription description = DomainServiceDescription.GetDescription(typeof(OnlineMethod_InvalidProvider_ArgOfComplexTypeIEnumerable));
            }, string.Format(CultureInfo.CurrentCulture, Resource.InvalidInvokeOperation_ParameterType, "TestMethod"));

            ExceptionHelper.ExpectException<InvalidOperationException>(delegate
            {
                DomainServiceDescription description = DomainServiceDescription.GetDescription(typeof(OnlineMethod_InvalidProvider_ArgOfComplexTypeList));
            }, string.Format(CultureInfo.CurrentCulture, Resource.InvalidInvokeOperation_ParameterType, "TestMethod"));
        }

        [TestMethod]
        [Description("Invoking an invoke operation should validate parameter validation attributes")]
        public async Task InvokeOperation_ServerValidationError_Parameter_ValidationAttributes()
        {
            TestProvider_Scenarios provider = ServerTestHelper.CreateInitializedDomainService<TestProvider_Scenarios>(DomainOperationType.Invoke);
            DomainServiceDescription serviceDescription = DomainServiceDescription.GetDescription(typeof(TestProvider_Scenarios));
            DomainOperationEntry operationEntry = serviceDescription.GetInvokeOperation("VariousParameterTypes");
            Assert.IsNotNull(operationEntry);

            // parameters string, int, bool and only string has validation attribute
            var invokeResult = await provider.InvokeAsync(new InvokeDescription(operationEntry, new object[] { string.Empty, 2, false }), CancellationToken.None);
            Assert.IsNull(invokeResult.Result);
            Assert.IsNotNull(invokeResult.ValidationErrors);
            Assert.AreEqual(1, invokeResult.ValidationErrors.Count);
            Assert.AreEqual("The str field is required.", invokeResult.ValidationErrors.ElementAt(0).ErrorMessage);
        }

        [TestMethod]
        [Description("Invoking an invoke operation with single entity should perform object level validation of the entity")]
        public async Task InvokeOperation_ServerValidationError_Entity()
        {
            TestProvider_Scenarios provider = ServerTestHelper.CreateInitializedDomainService<TestProvider_Scenarios>(DomainOperationType.Invoke);
            DomainServiceDescription serviceDescription = DomainServiceDescription.GetDescription(typeof(TestProvider_Scenarios));
            DomainOperationEntry operationEntry = serviceDescription.GetInvokeOperation(nameof(TestProvider_Scenarios.IncrementBid1ForA));
            Assert.IsNotNull(operationEntry);

            TestDomainServices.A inputA = new TestDomainServices.A();
            var invokeResult = await provider.InvokeAsync(new InvokeDescription(operationEntry, new object[] { inputA }), CancellationToken.None);
            Assert.IsNull(invokeResult.Result, "Should have validation errors instead");
            Assert.IsNotNull(invokeResult.ValidationErrors, "Should have validation errors");
            Assert.AreEqual(1, invokeResult.ValidationErrors.Count);
            Assert.AreEqual("The RequiredString field is required.", invokeResult.ValidationErrors.First().ErrorMessage);
        }

        [TestMethod]
        [Description("Invoking an invoke operation with invalid parameters")]
        public async Task InvokeOperation_ServerValidationException()
        {
            TestProvider_Scenarios provider = ServerTestHelper.CreateInitializedDomainService<TestProvider_Scenarios>(DomainOperationType.Invoke);
            DomainServiceDescription serviceDescription = DomainServiceDescription.GetDescription(typeof(TestProvider_Scenarios));
            DomainOperationEntry throwValidationExceptionMethod = serviceDescription.GetInvokeOperation("ThrowValidationException");
            Assert.IsNotNull(throwValidationExceptionMethod);

            var invokeResult = await provider.InvokeAsync(new InvokeDescription(throwValidationExceptionMethod, Array.Empty<object>()), CancellationToken.None);
            Assert.IsNull(invokeResult.Result);
            Assert.IsNotNull(invokeResult.ValidationErrors);
            Assert.AreEqual(1, invokeResult.ValidationErrors.Count);
            Assert.AreEqual("Validation error.", invokeResult.ValidationErrors.ElementAt(0).ErrorMessage);
        }
    }
}
