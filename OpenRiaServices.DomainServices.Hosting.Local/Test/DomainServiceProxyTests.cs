using System;
using System.Collections.Generic;
using OpenRiaServices.Common.Test;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Threading;
using OpenRiaServices.DomainServices.Client.Test;
using OpenRiaServices.DomainServices.Server;
using System.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenRiaServices.DomainServices.Hosting.Local.Test
{
    using TestDescription = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;
    using IgnoreAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.IgnoreAttribute;

    [TestClass, Serializable]
    public class DomainServiceProxyTests
    {
        #region Common Test Facets

        public OperationInvokedEventArgs OperationInvokedArgs
        {
            get;
            set;
        }

        [TestInitialize]
        public void TestInitialize()
        {
            this.OperationInvokedArgs = null;

            List<MockEntity> entities = new List<MockEntity>();
            for (int i = 0; i < 25; ++i)
            {
                entities.Add(new MockEntity() { ID = i, Name = "Entity" + i.ToString() });
            }

            MockDomainService.QueryReturnValue = entities;
            MockDomainService.OperationInvoked += OnInvoke;
        }

        [TestCleanup]
        public void TestCleanup()
        {
            MockDomainService.OperationInvoked -= OnInvoke;
        }

        private void OnInvoke(object sender, OperationInvokedEventArgs args)
        {
            this.OperationInvokedArgs = args;
        }

        #endregion // Common Test Facets

        #region Test Methods

        #region Create Tests

        [TestMethod]
        [TestDescription("Verifies the DomainServiceProxy.Create(HttpContextBase) method flows the provided context references properly.")]
        public void Create_HttpContext()
        {
            var context = new MockHttpContext();

            // Create proxy, verify it's not null.
            var proxy = DomainServiceProxy.Create<IMockDomainServiceContract, MockDomainService>(context);
            Assert.IsNotNull(proxy);

            // Examine its Context property
            PropertyInfo contextProp = proxy.GetType().GetProperty("Context");
            DomainServiceContext proxyContext = (DomainServiceContext)contextProp.GetGetMethod().Invoke(proxy, null);
            Assert.AreSame(context, proxyContext.GetService(typeof(HttpContextBase)));

            // Examine its DomainServiceType property
            PropertyInfo typeProp = proxy.GetType().GetProperty("DomainServiceType");
            Type proxyType = (Type)typeProp.GetGetMethod().Invoke(proxy, null);
            Assert.AreEqual(typeof(MockDomainService), proxyType);
        }

        [TestMethod]
        [TestDescription("Verifies the DomainServiceProxy.Create(DomainServiceContext) method flows the provided context references properly.")]
        public void Create_DomainServiceContext()
        {
            var context = new MockDomainServiceContext(DomainOperationType.Query);

            // Create proxy, verify it's not null.
            var proxy = DomainServiceProxy.Create<IMockDomainServiceContract, MockDomainService>(context);
            Assert.IsNotNull(proxy);

            // Examine its Context property
            PropertyInfo contextProp = proxy.GetType().GetProperty("Context");
            DomainServiceContext proxyContext = (DomainServiceContext)contextProp.GetGetMethod().Invoke(proxy, null);
            Assert.AreSame(context, proxyContext);

            // Examine its DomainServiceType property
            PropertyInfo typeProp = proxy.GetType().GetProperty("DomainServiceType");
            Type proxyType = (Type)typeProp.GetGetMethod().Invoke(proxy, null);
            Assert.AreEqual(typeof(MockDomainService), proxyType);
        }

        // TODO:  enable for web scenarios where HttpContext.Current is not null
        //[Ignore]
        //[TestMethod]
        //[TestDescription("Verifies the DomainServiceProxy.Create(void) method flows creates the expected default context.")]
        //public void Create_DefaultContext()
        //{
        //    // Create proxy, verify it's not null.
        //    var proxy = DomainServiceProxy.Create<IMockDomainServiceContract, MockDomainService>();
        //    Assert.IsNotNull(proxy);
        //}

        [TestMethod]
        [TestDescription("Verifies the DomainServiceProxy.Create(void) method reports errors for contracts with invalid methods.")]
        public void Create_InvalidContractMethods()
        {
            ExceptionHelper.ExpectInvalidOperationException(
                () => DomainServiceProxy.Create<IMockDomainServiceContract_InvalidMethods, MockDomainService>(new MockDomainServiceContext(DomainOperationType.Query)),
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resource.DomainServiceProxyGenerator_MethodCountMismatch,
                    typeof(IMockDomainServiceContract_InvalidMethods),
                    typeof(MockDomainService),
                    "ThisIsNotADomainInvokeOperation"));
        }

        [TestMethod]
        [TestDescription("Verifies the DomainServiceProxy.Create(void) method reports errors for contracts with invalid properties.")]
        public void Create_InvalidContractProperties()
        {
            ExceptionHelper.ExpectInvalidOperationException(
                () => DomainServiceProxy.Create<IMockDomainServiceContract_InvalidProperties, MockDomainService>(new MockDomainServiceContext(DomainOperationType.Query)),
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resource.DomainServiceProxyGenerator_MethodCountMismatch,
                    typeof(IMockDomainServiceContract_InvalidProperties),
                    typeof(MockDomainService),
                    "get_InvalidProperty, set_InvalidProperty"));
        }

        [TestMethod]
        [TestDescription("Verifies the DomainServiceProxy.Create(void) method reports errors for inaccessible contracts.")]
        public void Create_InvalidContractAccessibility()
        {
            ExceptionHelper.ExpectArgumentException(
                () => DomainServiceProxy.Create<IMockDomainServiceContract_InvalidAccessibility, MockDomainService>(new MockDomainServiceContext(DomainOperationType.Query)),
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resource.DomainServiceProxyGenerator_ExpectedPublicType,
                    typeof(IMockDomainServiceContract_InvalidAccessibility)),
                "domainServiceContract");
        }

        [TestMethod]
        [TestDescription("Verifies the DomainServiceProxy.Create(void) method reports errors for inaccessible DomainService types.")]
        public void Create_InvalidDomainServiceAccessibility()
        {
            ExceptionHelper.ExpectArgumentException(
                () => DomainServiceProxy.Create<IMockDomainServiceContract_ValidAccessibility, MockDomainService_InvalidAccessibility>(new MockDomainServiceContext(DomainOperationType.Query)),
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resource.DomainServiceProxyGenerator_ExpectedPublicType,
                    typeof(MockDomainService_InvalidAccessibility)),
                "domainService");
        }

        [TestMethod]
        [TestDescription("Verifies the DomainServiceProxy.Create(void) method reports errors for contracts with invalid events.")]
        public void Create_InvalidContractEvents()
        {
            ExceptionHelper.ExpectInvalidOperationException(
                () => DomainServiceProxy.Create<IMockDomainServiceContract_InvalidEvents, MockDomainService>(new MockDomainServiceContext(DomainOperationType.Query)),
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resource.DomainServiceProxyGenerator_MethodCountMismatch,
                    typeof(IMockDomainServiceContract_InvalidEvents),
                    typeof(MockDomainService),
                    "add_InvalidEvent, remove_InvalidEvent"));
        }

        [TestMethod]
        [TestDescription("Verifies the DomainServiceProxy.Create(void) method reports errors for contracts with overloaded methods.")]
        public void Create_InvalidContract_Overloads()
        {
            ExceptionHelper.ExpectInvalidOperationException(
                () => DomainServiceProxy.Create<IMockDomainServiceContract_InvalidOverrides, MockDomainService>(new MockDomainServiceContext(DomainOperationType.Query)),
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resource.DomainServiceProxyGenerator_OverridesNotSupported,
                    typeof(IMockDomainServiceContract_InvalidOverrides),
                    "GetEntities"));
        }

        [TestMethod]
        [TestDescription("Verifies the DomainServiceProxy.Create(void) method reports errors for contracts with return type mismatches.")]
        public void Create_InvalidContract_ReturnTypeMismatch()
        {
            ExceptionHelper.ExpectInvalidOperationException(
                () => DomainServiceProxy.Create<IMockDomainServiceContract_InvalidReturnType, MockDomainService>(new MockDomainServiceContext(DomainOperationType.Query)),
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resource.DomainServiceProxyGenerator_OperationMismatch,
                    typeof(IMockDomainServiceContract_InvalidReturnType),
                    "GetEntities"));
        }

        [TestMethod]
        [TestDescription("Verifies the DomainServiceProxy.Create(void) method reports errors for contracts with parameter mismatches.")]
        public void Create_InvalidContract_ParameterMismatch()
        {
            ExceptionHelper.ExpectInvalidOperationException(
                () => DomainServiceProxy.Create<IMockDomainServiceContract_InvalidParameters, MockDomainService>(new MockDomainServiceContext(DomainOperationType.Query)),
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resource.DomainServiceProxyGenerator_OperationMismatch,
                    typeof(IMockDomainServiceContract_InvalidParameters),
                    "GetEntities"));
        }

        #endregion // Create Tests

        #region Query Tests

        [TestMethod]
        [TestDescription("Verifies the behavior of DomainService proxy query methods without parameters.")]
        public void Query_NoParams()
        {
            var proxy = DomainServiceProxy.Create<IMockDomainServiceContract, MockDomainService>(new MockDomainServiceContext(DomainOperationType.Query));
            Assert.IsNull(this.OperationInvokedArgs);

            var result = proxy.GetEntities();

            Assert.IsNotNull(this.OperationInvokedArgs);
            Assert.IsNotNull(result);
            Assert.IsTrue(MockDomainService.QueryReturnValue.SequenceEqual(result));
            Assert.AreEqual("GetEntities", this.OperationInvokedArgs.Name);
            Assert.AreEqual(0, this.OperationInvokedArgs.Parameters.Count);
        }

        [TestMethod]
        [TestDescription("Verifies the behavior of DomainService proxy query methods with parameters.")]
        public void Query_WithParams()
        {
            var proxy = DomainServiceProxy.Create<IMockDomainServiceContract, MockDomainService>(new MockDomainServiceContext(DomainOperationType.Query));
            Assert.IsNull(this.OperationInvokedArgs);

            var result = proxy.GetEntitiesWithParams("123", 499, 501, new Uri("http://param4", UriKind.Absolute));

            // Verify results
            Assert.IsNotNull(this.OperationInvokedArgs);
            Assert.IsNotNull(result);
            Assert.IsTrue(MockDomainService.QueryReturnValue.SequenceEqual(result));
            Assert.AreEqual("GetEntitiesWithParams", this.OperationInvokedArgs.Name);

            // Verify parameters
            Assert.AreEqual(4, this.OperationInvokedArgs.Parameters.Count);
            Assert.AreEqual("123", this.OperationInvokedArgs.Parameters["param1"]);
            Assert.AreEqual(499, this.OperationInvokedArgs.Parameters["param2"]);
            Assert.AreEqual((long)501, this.OperationInvokedArgs.Parameters["param3"]);
            Assert.AreEqual("http://param4", ((Uri)this.OperationInvokedArgs.Parameters["param4"]).OriginalString);
        }

        [TestMethod]
        [TestDescription("Verifies the behavior of DomainService proxy singleton query methods without parameters.")]
        public void Query_Singleton_NoParams()
        {
            var proxy = DomainServiceProxy.Create<IMockDomainServiceContract, MockDomainService>(new MockDomainServiceContext(DomainOperationType.Query));
            Assert.IsNull(this.OperationInvokedArgs);

            var result = proxy.GetSingletonEntity();

            Assert.IsNotNull(this.OperationInvokedArgs);
            Assert.IsNotNull(result);
            Assert.AreSame(MockDomainService.QueryReturnValue.First(), result);
            Assert.AreEqual("GetSingletonEntity", this.OperationInvokedArgs.Name);
            Assert.AreEqual(0, this.OperationInvokedArgs.Parameters.Count);
        }

        [TestMethod]
        [TestDescription("Verifies the behavior of DomainService proxy singleton query methods with parameters.")]
        public void Query_Singleton_WithParams()
        {
            var proxy = DomainServiceProxy.Create<IMockDomainServiceContract, MockDomainService>(new MockDomainServiceContext(DomainOperationType.Query));
            Assert.IsNull(this.OperationInvokedArgs);

            var result = proxy.GetSingletonEntityWithParams("param1", 2, 3, new Uri("http://param4", UriKind.Absolute));

            Assert.IsNotNull(this.OperationInvokedArgs);
            Assert.IsNotNull(result);
            Assert.AreSame(MockDomainService.QueryReturnValue.First(), result);
            Assert.AreEqual("GetSingletonEntityWithParams", this.OperationInvokedArgs.Name);

            // Verify parameters
            Assert.AreEqual(4, this.OperationInvokedArgs.Parameters.Count);
            Assert.AreEqual("param1", this.OperationInvokedArgs.Parameters["param1"]);
            Assert.AreEqual(2, this.OperationInvokedArgs.Parameters["param2"]);
            Assert.AreEqual((long)3, this.OperationInvokedArgs.Parameters["param3"]);
            Assert.AreEqual("http://param4", ((Uri)this.OperationInvokedArgs.Parameters["param4"]).OriginalString);
        }

        [TestMethod]
        [TestDescription("Verifies the behavior of DomainService proxy query methods when continuable errors are thrown.")]
        public void Query_ContinuableOperationError()
        {
            var proxy = DomainServiceProxy.Create<IMockDomainServiceContract, MockDomainService>(new MockDomainServiceContext(DomainOperationType.Query));
            Assert.IsNull(this.OperationInvokedArgs);


            var ex = ExceptionHelper.ExpectException<OperationException>(
                () => proxy.GetEntitiesWithParams("abc", 499, 501, new Uri("http://param4", UriKind.Absolute)),
                Resource.DomainServiceProxy_OperationError);

            Assert.AreEqual(1, ex.OperationErrors.Count());
            Assert.AreEqual(@"The field param1 must match the regular expression '\d{1,3}'.", ex.OperationErrors.Single().Message);
        }

        #endregion // Query Tests

        #region CUD Tests

        [TestMethod]
        [TestDescription("Verifies the behavior of DomainService proxy update methods.")]
        public void Update()
        {
            var proxy = DomainServiceProxy.Create<IMockDomainServiceContract, MockDomainService>(new MockDomainServiceContext(DomainOperationType.Submit));
            Assert.IsNull(this.OperationInvokedArgs);

            var entity = proxy.GetSingletonEntity();

            proxy.UpdateEntity(entity);
            Assert.IsNotNull(this.OperationInvokedArgs);
            Assert.AreEqual("UpdateEntity", this.OperationInvokedArgs.Name);
            Assert.AreEqual(1, this.OperationInvokedArgs.Parameters.Count);
            Assert.AreSame(entity, this.OperationInvokedArgs.Parameters.Values.Single());
        }

        [TestMethod]
        [TestDescription("Verifies the behavior of DomainService proxy update methods when continuable exceptions are thrown.")]
        public void Update_ContinuableOperationError()
        {
            var proxy = DomainServiceProxy.Create<IMockDomainServiceContract, MockDomainService>(new MockDomainServiceContext(DomainOperationType.Submit));
            Assert.IsNull(this.OperationInvokedArgs);

            var entity = proxy.GetSingletonEntity();
            entity.Name = "ThrowError";

            var ex = ExceptionHelper.ExpectException<OperationException>(
                () => proxy.UpdateEntity(entity),
                Resource.DomainServiceProxy_OperationError);

            Assert.AreEqual(1, ex.OperationErrors.Count());
            Assert.AreEqual("ThrowError", ex.OperationErrors.Single().Message);
        }

        [TestMethod]
        [TestDescription("Verifies the behavior of DomainService proxy delete methods.")]
        public void Delete()
        {
            var proxy = DomainServiceProxy.Create<IMockDomainServiceContract, MockDomainService>(new MockDomainServiceContext(DomainOperationType.Submit));
            Assert.IsNull(this.OperationInvokedArgs);

            var entity = proxy.GetSingletonEntity();

            proxy.DeleteEntity(entity);
            Assert.IsNotNull(this.OperationInvokedArgs);
            Assert.AreEqual("DeleteEntity", this.OperationInvokedArgs.Name);
            Assert.AreEqual(1, this.OperationInvokedArgs.Parameters.Count);
            Assert.AreSame(entity, this.OperationInvokedArgs.Parameters.Values.Single());
        }

        [TestMethod]
        [TestDescription("Verifies the behavior of DomainService proxy delete methods when continuable exceptions are thrown.")]
        public void Delete_ContinuableOperationError()
        {
            var proxy = DomainServiceProxy.Create<IMockDomainServiceContract, MockDomainService>(new MockDomainServiceContext(DomainOperationType.Submit));
            Assert.IsNull(this.OperationInvokedArgs);

            var entity = proxy.GetSingletonEntity();
            entity.Name = "ThrowError";

            var ex = ExceptionHelper.ExpectException<OperationException>(
                () => proxy.DeleteEntity(entity),
                Resource.DomainServiceProxy_OperationError);

            Assert.AreEqual(1, ex.OperationErrors.Count());
            Assert.AreEqual("ThrowError", ex.OperationErrors.Single().Message);
        }

        [TestMethod]
        [TestDescription("Verifies the behavior of DomainService proxy insert methods.")]
        public void Insert()
        {
            var proxy = DomainServiceProxy.Create<IMockDomainServiceContract, MockDomainService>(new MockDomainServiceContext(DomainOperationType.Submit));
            Assert.IsNull(this.OperationInvokedArgs);

            var entity = proxy.GetSingletonEntity();

            proxy.InsertEntity(entity);
            Assert.IsNotNull(this.OperationInvokedArgs);
            Assert.AreEqual("InsertEntity", this.OperationInvokedArgs.Name);
            Assert.AreEqual(1, this.OperationInvokedArgs.Parameters.Count);
            Assert.AreSame(entity, this.OperationInvokedArgs.Parameters.Values.Single());
        }

        [TestMethod]
        [TestDescription("Verifies the behavior of DomainService proxy insert methods when continuable exceptions are thrown.")]
        public void Insert_ContinuableOperationError()
        {
            var proxy = DomainServiceProxy.Create<IMockDomainServiceContract, MockDomainService>(new MockDomainServiceContext(DomainOperationType.Submit));
            Assert.IsNull(this.OperationInvokedArgs);

            var entity = proxy.GetSingletonEntity();
            entity.Name = "ThrowError";

            var ex = ExceptionHelper.ExpectException<OperationException>(
                () => proxy.InsertEntity(entity),
                Resource.DomainServiceProxy_OperationError);

            Assert.AreEqual(1, ex.OperationErrors.Count());
            Assert.AreEqual("ThrowError", ex.OperationErrors.Single().Message);
        }

        #endregion // CUD Tests

        #region Custom Method Tests

        [TestMethod]
        [TestDescription("Verifies the behavior of DomainService proxy custom methods without parameters.")]
        public void Custom_NoParams()
        {
            var proxy = DomainServiceProxy.Create<IMockDomainServiceContract, MockDomainService>(new MockDomainServiceContext(DomainOperationType.Submit));
            Assert.IsNull(this.OperationInvokedArgs);

            var entity = proxy.GetSingletonEntity();
            proxy.CustomMethod(entity);

            Assert.IsNotNull(this.OperationInvokedArgs);
            Assert.AreEqual("CustomMethod", this.OperationInvokedArgs.Name);
            Assert.AreEqual(1, this.OperationInvokedArgs.Parameters.Count);
            Assert.AreSame(entity, this.OperationInvokedArgs.Parameters["entity"]);
        }

        [TestMethod]
        [TestDescription("Verifies the behavior of DomainService proxy custom methods with parameters.")]
        public void Custom_WithParams()
        {
            var proxy = DomainServiceProxy.Create<IMockDomainServiceContract, MockDomainService>(new MockDomainServiceContext(DomainOperationType.Submit));
            Assert.IsNull(this.OperationInvokedArgs);

            var entity = proxy.GetSingletonEntity();
            proxy.CustomMethodWithParams(entity, 123, new[] { "1", "2", "3" }, null, new Uri("http://param4", UriKind.Absolute));

            Assert.IsNotNull(this.OperationInvokedArgs);
            Assert.AreEqual("CustomMethodWithParams", this.OperationInvokedArgs.Name);
            Assert.AreEqual(5, this.OperationInvokedArgs.Parameters.Count);
            Assert.AreSame(entity, this.OperationInvokedArgs.Parameters["entity"]);
            Assert.AreEqual((long)123, this.OperationInvokedArgs.Parameters["param1"]);
            Assert.AreEqual("123", string.Join("", (string[])this.OperationInvokedArgs.Parameters["param2"]));
            Assert.AreEqual(null, this.OperationInvokedArgs.Parameters["param3"]);
            Assert.AreEqual("http://param4", ((Uri)this.OperationInvokedArgs.Parameters["param4"]).OriginalString);
        }

        [TestMethod]
        [TestDescription("Verifies the behavior of DomainService proxy custom methods when continuable exceptions are thrown.")]
        public void Custom_ContinuableOperationError()
        {
            var proxy = DomainServiceProxy.Create<IMockDomainServiceContract, MockDomainService>(new MockDomainServiceContext(DomainOperationType.Submit));
            Assert.IsNull(this.OperationInvokedArgs);

            var entity = proxy.GetSingletonEntity();
            entity.Name = "ThrowError";

            var ex = ExceptionHelper.ExpectException<OperationException>(
                () => proxy.CustomMethod(entity),
                Resource.DomainServiceProxy_OperationError);

            Assert.AreEqual(1, ex.OperationErrors.Count());
            Assert.AreEqual("ThrowError", ex.OperationErrors.Single().Message);
        }

        #endregion // Custom Method Tests

        #region Invoke Operation Tests

        [TestMethod]
        [TestDescription("Verifies the behavior of DomainService proxy invoke operation methods without parameters.")]
        public void InvokeOperation_NoParams()
        {
            var proxy = DomainServiceProxy.Create<IMockDomainServiceContract, MockDomainService>(new MockDomainServiceContext(DomainOperationType.Invoke));
            Assert.IsNull(this.OperationInvokedArgs);

            var result = proxy.InvokeOperation();
            Assert.IsNotNull(this.OperationInvokedArgs);
            Assert.AreEqual(123, result);

            Assert.AreEqual("InvokeOperation", this.OperationInvokedArgs.Name);
            Assert.AreEqual(0, this.OperationInvokedArgs.Parameters.Count);
        }

        [TestMethod]
        [TestDescription("Verifies the behavior of DomainService proxy invoke operation methods with parameters.")]
        public void InvokeOperation_WithParams()
        {
            var proxy = DomainServiceProxy.Create<IMockDomainServiceContract, MockDomainService>(new MockDomainServiceContext(DomainOperationType.Invoke));
            Assert.IsNull(this.OperationInvokedArgs);

            var result = proxy.InvokeOperationWithParams(123, new[] { "a", "b", "c" }, null, null);
            Assert.IsNotNull(this.OperationInvokedArgs);
            Assert.AreEqual("123", result);

            Assert.AreEqual("InvokeOperationWithParams", this.OperationInvokedArgs.Name);
            Assert.AreEqual(4, this.OperationInvokedArgs.Parameters.Count);
            Assert.AreEqual((long)123, this.OperationInvokedArgs.Parameters["param1"]);
            Assert.AreEqual("abc", string.Join("", (string[])this.OperationInvokedArgs.Parameters["param2"]));
            Assert.AreEqual(null, this.OperationInvokedArgs.Parameters["param3"]);
            Assert.AreEqual(null, this.OperationInvokedArgs.Parameters["param4"]);
        }

        #endregion // Invoke Operation Tests

        #region AssociateOriginal Tests

        [TestMethod]
        [TestDescription("Verifies the expected behavior of DomainServiceProxy.AssociateOriginal.")]
        public void AssociateOriginal_HelperMethod()
        {
            var proxy = this.CreateMockDomainServiceProxy();
            var current = new object();
            var original = new object();

            DomainServiceProxy.AssociateOriginal<object>(proxy, current, original);

            Assert.AreEqual(1, proxy.CurrentOriginalEntityMap.Count);
            Assert.AreSame(current, proxy.CurrentOriginalEntityMap.Keys.Single());
            Assert.AreSame(original, proxy.CurrentOriginalEntityMap.Values.Single());
        }

        // TODO: uncomment if we enable support for AssociateOriginal extension method.
        //[Ignore]
        //[TestMethod]
        //[TestDescription("Verifies the expected behavior of DomainServiceProxy.AssociateOriginal extension method.")]
        //public void AssociateOriginal_ExtensionMethod()
        //{
        //    var proxy = this.CreateMockDomainServiceProxy();
        //    var current = new object();
        //    var original = new object();
        //
        //    proxy.AssociateOriginal(current, original);
        //
        //    Assert.AreEqual(1, proxy.CurrentOriginalEntityMap.Count);
        //    Assert.AreSame(current, proxy.CurrentOriginalEntityMap.Keys.Single());
        //    Assert.AreSame(original, proxy.CurrentOriginalEntityMap.Values.Single());
        //}

        [TestMethod]
        [TestDescription("Verifies the expected behavior of DomainServiceProxy.AssociateOriginal when null parameters are passed.")]
        public void AssociateOriginal_HelperMethod_NullParameters()
        {
            // Null proxy argument
            ExceptionHelper.ExpectArgumentNullException(
                () => DomainServiceProxy.AssociateOriginal<object>(null, new object(), new object()),
                "domainServiceProxy");

            // Null current argument
            ExceptionHelper.ExpectArgumentNullException(
                () => DomainServiceProxy.AssociateOriginal<object>(new object(), null, new object()),
                "current");

            // Null original argument
            ExceptionHelper.ExpectArgumentNullException(
                () => DomainServiceProxy.AssociateOriginal<object>(new object(), new object(), null),
                "original");
        }

        // TODO: uncomment if we enable support for AssociateOriginal extension method.
        //[Ignore]
        //[TestMethod]
        //[TestDescription("Verifies the expected behavior of DomainServiceProxy.AssociateOriginal extension method when null parameters are passed.")]
        //public void AssociateOriginal_ExtensionMethod_NullParameters()
        //{
        //    var proxy = this.CreateMockDomainServiceProxy();
        //
        //    // Null current argument
        //    ExceptionHelper.ExpectArgumentNullException(
        //        () => proxy.AssociateOriginal(null, new object()),
        //        "current");
        //
        //    // Null original argument
        //    ExceptionHelper.ExpectArgumentNullException(
        //        () => proxy.AssociateOriginal(new object(), null),
        //        "original");
        //}

        [TestMethod]
        [TestDescription("Verifies the expected behavior of DomainServiceProxy.AssociateOriginal when an invalid proxy instance is passed.")]
        public void AssociateOriginal_HelperMethod_InvalidProxy()
        {
            ExceptionHelper.ExpectArgumentException(
                () => DomainServiceProxy.AssociateOriginal<object>(new object(), new object(), new object()),
                Resource.DomainServiceProxy_InvalidProxyType,
                "domainServiceProxy");
        }

        [TestMethod]
        [TestDescription("Verifies the behavior of DomainService proxy operations that use ChangeSet.GetOriginal().")]
        public void AssociateOriginal_ChangeSet_GetOriginal()
        {
            var proxy = DomainServiceProxy.Create<IMockDomainServiceContract, MockDomainService>(new MockDomainServiceContext(DomainOperationType.Submit));
            Assert.IsNull(this.OperationInvokedArgs);

            var entity = proxy.GetSingletonEntity();
            entity.Name = "CurrentEntity";
            Assert.AreEqual("CurrentEntity", entity.Name);

            var original = new MockEntity() { ID = entity.ID, Name = "OriginalEntity" };
            DomainServiceProxy.AssociateOriginal(proxy, entity, original);

            proxy.UpdateEntity(entity);
            Assert.IsNotNull(this.OperationInvokedArgs);
            Assert.AreEqual("UpdateEntity", this.OperationInvokedArgs.Name);
            Assert.AreEqual(1, this.OperationInvokedArgs.Parameters.Count);
            Assert.AreSame(entity, this.OperationInvokedArgs.Parameters.Values.Single());
            Assert.AreEqual("OriginalEntity", entity.Name);
        }

        #endregion // AssociateOriginal Tests

        #region IDisposable Tests

        [TestMethod]
        [TestDescription("Verifies that DomainService proxy instances track internal DomainService instances and dispose of them properly")]
        public void DisposalTracking()
        {
            var proxy = DomainServiceProxy.Create<IMockDomainServiceContract, MockDomainService>(new MockDomainServiceContext(DomainOperationType.Submit));
            var entity = new MockEntity();

            var args = new List<OperationInvokedEventArgs>();
            EventHandler<OperationInvokedEventArgs> handler = (s, a) => args.Add(a);

            try
            {
                MockDomainService.Disposed += handler;

                // Invoke a operation multiple times
                proxy.CustomMethod(entity);
                proxy.CustomMethod(entity);
                proxy.CustomMethod(entity);

                proxy.Dispose();

                Assert.AreEqual(3, args.Count);
                Assert.AreEqual("Dispose", args[0].Name);
                Assert.AreEqual("Dispose", args[1].Name);
                Assert.AreEqual("Dispose", args[2].Name);
            }
            finally
            {
                MockDomainService.Disposed -= handler;
            }
        }

        #endregion // IDisposable Tests

        #region MediumTrust Tests

#if !CODECOV

        [TestMethod]
        [Ignore]
        // TODO: [roncain] Need to work with CLR team to understand why we require SecurityPermission.Unrestricted to be
        // run this test.  With only Internet zone permissions, OperationException throws TypeLoadException due to its
        // GetObjectData being SecuritySafeCritical.
        [Description("Verifies the proxies in partial trust")]
        public void DomainServiceProxy_MediumTrust_Proxies()
        {
            SandBoxer.ExecuteInMediumTrust(Callback_MediumTrust_Proxies);
        }

        public static void Callback_MediumTrust_Proxies()
        {
            var proxy = DomainServiceProxy.Create<IMockDomainServiceContract, MockDomainService>(new MockDomainServiceContext(DomainOperationType.Query));
            var results = proxy.GetEntities();
        }

#endif // !CODECOV

        #endregion // MediumTrust Tests

        #endregion Test Methods

        #region Helper Methods

        private MockDomainServiceProxy CreateMockDomainServiceProxy()
        {
            return new MockDomainServiceProxy()
            {
                Context = new DomainServiceContext(new MockServiceProvider(), DomainOperationType.Query),
                CurrentOriginalEntityMap = new Dictionary<object, object>(),
                DomainServiceType = typeof(object)
            };
        }

        #endregion // Helper Methods

        #region Nested Types

        public interface IMockDomainServiceContract_InvalidMethods : IDisposable
        {
            IEnumerable<MockEntity> GetEntities();
            void ThisIsNotADomainInvokeOperation();
        }

        public interface IMockDomainServiceContract_InvalidProperties : IDisposable
        {
            IEnumerable<MockEntity> GetEntities();
            int InvalidProperty { get; set; }
        }

        public interface IMockDomainServiceContract_InvalidEvents : IDisposable
        {
            IEnumerable<MockEntity> GetEntities();
            event EventHandler InvalidEvent;
        }

        internal interface IMockDomainServiceContract_InvalidAccessibility : IDisposable
        {
            IEnumerable<MockEntity> GetEntities();
        }

        public interface IMockDomainServiceContract_ValidAccessibility : IDisposable
        {
            IEnumerable<MockEntity> GetEntities();
        }

        public interface IMockDomainServiceContract_InvalidOverrides : IDisposable
        {
            IEnumerable<MockEntity> GetEntities();
            IEnumerable<MockEntity> GetEntities(int i);
        }

        public interface IMockDomainServiceContract_InvalidReturnType : IDisposable
        {
            string GetEntities();
        }

        public interface IMockDomainServiceContract_InvalidParameters : IDisposable
        {
            IEnumerable<MockEntity> GetEntities(long a);
        }

        public interface IMockDomainServiceContract : IDisposable
        {
            // Query methods
            IEnumerable<MockEntity> GetEntities();
            IEnumerable<MockEntity> GetEntitiesWithParams(string param1, int param2, long param3, Uri param4);
            MockEntity GetSingletonEntity();
            MockEntity GetSingletonEntityWithParams(string param1, int param2, long param3, Uri param4);

            // CUD methods
            void UpdateEntity(MockEntity entity);
            void DeleteEntity(MockEntity entity);
            void InsertEntity(MockEntity entity);

            // Custom methods
            void CustomMethod(MockEntity entity);
            void CustomMethodWithParams(MockEntity entity, long param1, IEnumerable<string> param2, Dictionary<byte, string> param3, Uri param4);

            // Invoke operation methods
            int InvokeOperation();
            string InvokeOperationWithParams(long param1, IEnumerable<string> param2, Dictionary<byte, string> param3, Uri param4);
        }

        internal class MockDomainService_InvalidAccessibility : MockDomainService
        {
        }

        public class MockDomainService : DomainService
        {
            public static IEnumerable<MockEntity> QueryReturnValue { get; set; }
            public static event EventHandler<OperationInvokedEventArgs> Disposed;
            public static event EventHandler<OperationInvokedEventArgs> OperationInvoked;

            #region Query methods

            [Query]
            public IEnumerable<MockEntity> GetEntities()
            {
                this.OnOperationInvoked("GetEntities", new Dictionary<string, object>());
                return QueryReturnValue;
            }

            [Query]
            public IEnumerable<MockEntity> GetEntitiesWithParams(
                [RegularExpression(@"\d{1,3}")] string param1,
                [Range(0, 500)] int param2,
                [Range(500, 1000)] long param3,
                Uri param4)
            {
                var parameters = new Dictionary<string, object>();
                parameters.Add("param1", param1);
                parameters.Add("param2", param2);
                parameters.Add("param3", param3);
                parameters.Add("param4", param4);
                this.OnOperationInvoked("GetEntitiesWithParams", parameters);
                return QueryReturnValue;
            }

            [Query(IsComposable = false)]
            public MockEntity GetSingletonEntity()
            {
                this.OnOperationInvoked("GetSingletonEntity", new Dictionary<string, object>());
                return QueryReturnValue.FirstOrDefault();
            }

            [Query(IsComposable = false)]
            public MockEntity GetSingletonEntityWithParams(string param1, int param2, long param3, Uri param4)
            {
                var parameters = new Dictionary<string, object>();
                parameters.Add("param1", param1);
                parameters.Add("param2", param2);
                parameters.Add("param3", param3);
                parameters.Add("param4", param4);
                this.OnOperationInvoked("GetSingletonEntityWithParams", parameters);
                return QueryReturnValue.FirstOrDefault();
            }

            #endregion // Query methods

            #region CUD methods

            [Update]
            public void UpdateEntity(MockEntity entity)
            {
                var parameters = new Dictionary<string, object>();
                parameters.Add("entity", entity);
                this.OnOperationInvoked("UpdateEntity", parameters);

                if (entity.Name == "ThrowError")
                {
                    throw new ValidationException(entity.Name);
                }
                else if (entity.Name == "CurrentEntity")
                {
                    var original = this.ChangeSet.GetOriginal(entity);
                    Assert.IsNotNull(original);

                    entity.ID = original.ID;
                    entity.Name = original.Name;
                }
            }

            [Delete]
            public void DeleteEntity(MockEntity entity)
            {
                var parameters = new Dictionary<string, object>();
                parameters.Add("entity", entity);
                this.OnOperationInvoked("DeleteEntity", parameters);

                if (entity.Name == "ThrowError")
                {
                    throw new ValidationException(entity.Name);
                }
            }

            [Insert]
            public void InsertEntity(MockEntity entity)
            {
                var parameters = new Dictionary<string, object>();
                parameters.Add("entity", entity);
                this.OnOperationInvoked("InsertEntity", parameters);

                if (entity.Name == "ThrowError")
                {
                    throw new ValidationException(entity.Name);
                }
            }

            #endregion // CUD methods

            #region Custom methods

            [Update(UsingCustomMethod = true)]
            public void CustomMethod(MockEntity entity)
            {
                var parameters = new Dictionary<string, object>();
                parameters.Add("entity", entity);
                this.OnOperationInvoked("CustomMethod", parameters);

                if (entity.Name == "ThrowError")
                {
                    throw new ValidationException(entity.Name);
                }
            }

            [Update(UsingCustomMethod = true)]
            public void CustomMethodWithParams(MockEntity entity, long param1, IEnumerable<string> param2, Dictionary<byte, string> param3, Uri param4)
            {
                var parameters = new Dictionary<string, object>();
                parameters.Add("entity", entity);
                parameters.Add("param1", param1);
                parameters.Add("param2", param2);
                parameters.Add("param3", param3);
                parameters.Add("param4", param4);
                this.OnOperationInvoked("CustomMethodWithParams", parameters);

                if (entity.Name == "ThrowError")
                {
                    throw new ValidationException(entity.Name);
                }
            }

            #endregion // Custom methods

            #region Invoke operation methods

            [Invoke]
            public int InvokeOperation()
            {
                this.OnOperationInvoked("InvokeOperation", new Dictionary<string, object>());
                return 123;
            }

            [Invoke]
            public string InvokeOperationWithParams(long param1, IEnumerable<string> param2, Dictionary<byte, string> param3, Uri param4)
            {
                var parameters = new Dictionary<string, object>();
                parameters.Add("param1", param1);
                parameters.Add("param2", param2);
                parameters.Add("param3", param3);
                parameters.Add("param4", param4);
                this.OnOperationInvoked("InvokeOperationWithParams", parameters);
                return param1.ToString();
            }

            #endregion Invoke operation methods

            protected override void Dispose(bool disposing)
            {
                var d = new Dictionary<string, object>();
                d.Add("dispoing", disposing);
                Disposed(this, new OperationInvokedEventArgs() { Name = "Dispose", Parameters = d });
            }

            private void OnOperationInvoked(string operationName, Dictionary<string, object> parameters)
            {
                if (MockDomainService.OperationInvoked != null)
                {
                    MockDomainService.OperationInvoked(
                        this,
                        new OperationInvokedEventArgs()
                        {
                            Name = operationName,
                            Parameters = parameters
                        });
                }
            }
        }

        public class MockEntity
        {
            [Key]
            public int ID { get; set; }
            public string Name { get; set; }
        }

        public class MockServiceProvider : IServiceProvider
        {
            public object GetService(Type serviceType)
            {
                if (serviceType == typeof(IPrincipal))
                {
                    return Thread.CurrentPrincipal;
                }

                return null;
            }
        }

        public class MockDomainServiceProxy : IDisposable
        {
            public IList<DomainService> DomainServiceInstances
            {
                get;
                set;
            }

            public DomainServiceContext Context
            {
                get;
                set;
            }

            public IDictionary<object, object> CurrentOriginalEntityMap
            {
                get;
                set;
            }

            public Type DomainServiceType
            {
                get;
                set;
            }

            public void Initialize(Type type, DomainServiceContext context)
            {
                return;
            }

            public void Dispose()
            {
                return;
            }
        }

        public class MockDomainServiceContext : DomainServiceContext
        {
            public MockDomainServiceContext(DomainOperationType operationType)
                : base(new MockServiceProvider(), operationType)
            {
            }
        }

        public class MockHttpContext : HttpContextBase
        {
            public override IPrincipal User
            {
                get
                {
                    return Thread.CurrentPrincipal;
                }
                set
                {
                    throw new NotSupportedException();
                }
            }

            public override object GetService(Type serviceType)
            {
                if (serviceType == typeof(HttpContextBase))
                {
                    return this;
                }

                return base.GetService(serviceType);
            }
        }

        public class OperationInvokedEventArgs : EventArgs
        {
            public string Name { get; set; }
            public Dictionary<string, object> Parameters { get; set; }
        }

        #endregion // Nested Types
    }
}
