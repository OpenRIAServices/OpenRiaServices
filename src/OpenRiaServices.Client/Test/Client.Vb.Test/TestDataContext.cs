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
            : base(DomainContext.CreateDomainClient(typeof(TestDomainServiceContract), serviceUri, false))
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

        public interface TestDomainServiceContract
        {
            [HasSideEffects(false)]
            IAsyncResult BeginGetProducts(AsyncCallback callback, object asyncState);

            QueryResult<Product> EndGetProducts(IAsyncResult result);

            [HasSideEffects(false)]
            IAsyncResult BeginThrowDataOperationException(AsyncCallback callback, object asyncState);

            QueryResult<Product> EndThrowDataOperationException(IAsyncResult result);

            [HasSideEffects(false)]
            IAsyncResult BeginThrowGeneralException(AsyncCallback callback, object asyncState);

            QueryResult<Product> EndThrowGeneralException(IAsyncResult result);

            [HasSideEffects(false)]
            IAsyncResult BeginGetProducts_ReturnNull(AsyncCallback callback, object asyncState);

            QueryResult<Product> EndGetProducts_ReturnNull(IAsyncResult result);

            [HasSideEffects(false)]
            IAsyncResult BeginGetProducts_Enumerable_Composable(AsyncCallback callback, object asyncState);

            QueryResult<Product> EndGetProducts_Enumerable_Composable(IAsyncResult result);

            [HasSideEffects(false)]
            IAsyncResult BeginGetProductsMultipleParams(int subCategoryID, int minListPrice, string color, AsyncCallback callback, object asyncState);

            QueryResult<Product> EndGetProductsMultipleParams(IAsyncResult result);

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
