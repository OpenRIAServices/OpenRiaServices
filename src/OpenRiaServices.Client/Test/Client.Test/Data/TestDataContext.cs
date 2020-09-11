using System;
using System.Collections.Generic;
using System.ServiceModel;
using DataTests.AdventureWorks.LTS;

namespace OpenRiaServices.Client.Test
{
    /// <summary>
    /// Test context that can be used to perform load operations directly for testing purposes
    /// </summary>
    public class TestDataContext : DomainContext
    {
        public TestDataContext(Uri serviceUri)
            : base(new WebDomainClient<TestDomainServiceContract>(serviceUri))
        {
        }

        public TestDataContext(DomainClient domainClient)
        : base(domainClient)
        {
        }

        public EntitySet<Product> Products
        {
            get
            {
                return EntityContainer.GetEntitySet<Product>();
            }
        }

        public EntitySet<PurchaseOrder> PurchaseOrders
        {
            get
            {
                return EntityContainer.GetEntitySet<PurchaseOrder>();
            }
        }

        public EntityQuery<TEntity> CreateQuery<TEntity>(string operation, IDictionary<string, object> parameters) where TEntity : Entity
        {
            return base.CreateQuery<TEntity>(operation, parameters, false, true);
        }

        public new EntityQuery<TEntity> CreateQuery<TEntity>(string operation, IDictionary<string, object> parameters, bool hasSideEffects, bool isComposable) where TEntity : Entity
        {
            return base.CreateQuery<TEntity>(operation, parameters, hasSideEffects, isComposable);
        }

        protected override EntityContainer CreateEntityContainer()
        {
            return new DummyEntityContainer();
        }

        [ServiceContract()]
        public interface TestDomainServiceContract
        {
            [OperationContract(AsyncPattern = true, Action = "http://tempuri.org/TestDomainService/GetProducts", ReplyAction = "http://tempuri.org/TestDomainService/GetProductsResponse")]
            [FaultContract(typeof(DomainServiceFault), Action = "http://tempuri.org/TestDomainService/GetProductsDomainServiceFault", Name = "DomainServiceFault", Namespace = "DomainServices")]
            [HasSideEffects(false)]
            IAsyncResult BeginGetProducts(AsyncCallback callback, object asyncState);

            QueryResult<Product> EndGetProducts(IAsyncResult result);

            [OperationContract(AsyncPattern = true, Action = "http://tempuri.org/TestDomainService/ThrowDataOperationException", ReplyAction = "http://tempuri.org/TestDomainService/ThrowDataOperationExceptionResponse")]
            [FaultContract(typeof(DomainServiceFault), Action = "http://tempuri.org/TestDomainService/ThrowDataOperationExceptionDomainServiceFault", Name = "DomainServiceFault", Namespace = "DomainServices")]
            [HasSideEffects(false)]
            IAsyncResult BeginThrowDataOperationException(AsyncCallback callback, object asyncState);

            QueryResult<Product> EndThrowDataOperationException(IAsyncResult result);

            [OperationContract(AsyncPattern = true, Action = "http://tempuri.org/TestDomainService/ThrowGeneralException", ReplyAction = "http://tempuri.org/TestDomainService/ThrowGeneralExceptionResponse")]
            [FaultContract(typeof(DomainServiceFault), Action = "http://tempuri.org/TestDomainService/ThrowGeneralExceptionDomainServiceFault", Name = "DomainServiceFault", Namespace = "DomainServices")]
            [HasSideEffects(false)]
            IAsyncResult BeginThrowGeneralException(AsyncCallback callback, object asyncState);

            QueryResult<Product> EndThrowGeneralException(IAsyncResult result);

            [OperationContract(AsyncPattern = true, Action = "http://tempuri.org/TestDomainService/GetProducts_ReturnNull", ReplyAction = "http://tempuri.org/TestDomainService/GetProducts_ReturnNullResponse")]
            [FaultContract(typeof(DomainServiceFault), Action = "http://tempuri.org/TestDomainService/GetProducts_ReturnNullDomainServiceFault", Name = "DomainServiceFault", Namespace = "DomainServices")]
            [HasSideEffects(false)]
            IAsyncResult BeginGetProducts_ReturnNull(AsyncCallback callback, object asyncState);

            QueryResult<Product> EndGetProducts_ReturnNull(IAsyncResult result);

            [OperationContract(AsyncPattern = true, Action = "http://tempuri.org/TestDomainService/GetProducts_Enumerable_Composable", ReplyAction = "http://tempuri.org/TestDomainService/GetProducts_Enumerable_ComposableResponse")]
            [FaultContract(typeof(DomainServiceFault), Action = "http://tempuri.org/TestDomainService/GetProducts_Enumerable_ComposableDomainServiceFault", Name = "DomainServiceFault", Namespace = "DomainServices")]
            [HasSideEffects(false)]
            IAsyncResult BeginGetProducts_Enumerable_Composable(AsyncCallback callback, object asyncState);

            QueryResult<Product> EndGetProducts_Enumerable_Composable(IAsyncResult result);

            [OperationContract(AsyncPattern = true, Action = "http://tempuri.org/TestDomainService/GetProductsMultipleParams", ReplyAction = "http://tempuri.org/TestDomainService/GetProductsMultipleParamsResponse")]
            [FaultContract(typeof(DomainServiceFault), Action = "http://tempuri.org/TestDomainService/GetProductsMultipleParamsDomainServiceFault", Name = "DomainServiceFault", Namespace = "DomainServices")]
            [HasSideEffects(false)]
            IAsyncResult BeginGetProductsMultipleParams(int subCategoryID, int minListPrice, string color, AsyncCallback callback, object asyncState);

            QueryResult<Product> EndGetProductsMultipleParams(IAsyncResult result);

            [OperationContract(AsyncPattern = true, Action = "http://tempuri.org/TestDomainService/NonExistentMethod", ReplyAction = "http://tempuri.org/TestDomainService/NonExistentMethodResponse")]
            [FaultContract(typeof(DomainServiceFault), Action = "http://tempuri.org/TestDomainService/NonExistentMethodDomainServiceFault", Name = "DomainServiceFault", Namespace = "DomainServices")]
            [HasSideEffects(false)]
            IAsyncResult BeginNonExistentMethod(AsyncCallback callback, object asyncState);

            QueryResult<Product> EndNonExistentMethod(IAsyncResult result);
        }

        private sealed class DummyEntityContainer : EntityContainer
        {
            public DummyEntityContainer()
            {
                CreateEntitySet<Product>();
                CreateEntitySet<PurchaseOrder>();
                CreateEntitySet<PurchaseOrderDetail>();
            }
        }
    }
}
