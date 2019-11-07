using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenRiaServices.DomainServices.Client;

namespace Cities
{
    public sealed partial class CityDomainContext : DomainContext
    {
        internal bool InvokeOperationGenericCalled { get; set; }
        internal bool InvokeOperationAsyncGenericCalled { get; set; }
        internal bool LoadCalled { get; set; }
        internal bool LoadAsyncCalled { get; set; }
        internal bool SubmitChangesCalled { get; set; }
        internal bool SubmitChangesAsyncCalled { get; set; }

        public override InvokeOperation<TValue> InvokeOperation<TValue>(string operationName, Type returnType, IDictionary<string, object> parameters, bool hasSideEffects, Action<InvokeOperation<TValue>> callback, object userState)
        {
            this.InvokeOperationGenericCalled = true;
            return base.InvokeOperation<TValue>(operationName, returnType, parameters, hasSideEffects, callback, userState);
        }

        public override LoadOperation<TEntity> Load<TEntity>(EntityQuery<TEntity> query, LoadBehavior loadBehavior, Action<LoadOperation<TEntity>> callback, object userState)
        {
            this.LoadCalled = true;
            return base.Load(query, loadBehavior, callback, userState);
        }
        public override SubmitOperation SubmitChanges(Action<SubmitOperation> callback, object userState)
        {
            this.SubmitChangesCalled = true;
            return base.SubmitChanges(callback, userState);
        }

        protected override Task<InvokeResult<TValue>> InvokeOperationAsync<TValue>(string operationName, IDictionary<string, object> parameters, bool hasSideEffects, Type returnType, CancellationToken cancellationToken)
        {
            InvokeOperationAsyncGenericCalled = true;
            return base.InvokeOperationAsync<TValue>(operationName, parameters, hasSideEffects, returnType, cancellationToken);
        }

        public override Task<LoadResult<TEntity>> LoadAsync<TEntity>(EntityQuery<TEntity> query, LoadBehavior loadBehavior,
            CancellationToken cancellationToken = new CancellationToken())
        {
            LoadAsyncCalled = true;
            return base.LoadAsync(query, loadBehavior, cancellationToken);
        }

        public override Task<SubmitResult> SubmitChangesAsync(CancellationToken cancellationToken)
        {
            SubmitChangesAsyncCalled = true;
            return base.SubmitChangesAsync(cancellationToken);
        }
    }
}
