using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRiaServices.DomainServices.Client;

namespace Cities
{
    public sealed partial class CityDomainContext : DomainContext
    {
        internal bool InvokeOperationCalled { get; set; }
        internal bool InvokeOperationGenericCalled { get; set; }
        internal bool LoadCalled { get; set; }
        internal bool SubmitChangesCalled { get; set; }

        public override InvokeOperation InvokeOperation(string operationName, Type returnType, IDictionary<string, object> parameters, bool hasSideEffects, Action<InvokeOperation> callback, object userState)
        {
            this.InvokeOperationCalled = true;
            return base.InvokeOperation(operationName, returnType, parameters, hasSideEffects, callback, userState);
        }
        
        public override InvokeOperation<TValue> InvokeOperation<TValue>(string operationName, Type returnType, IDictionary<string, object> parameters, bool hasSideEffects, Action<InvokeOperation<TValue>> callback, object userState)
        {
            this.InvokeOperationGenericCalled = true;
            return base.InvokeOperation<TValue>(operationName, returnType, parameters, hasSideEffects, callback, userState);
        }
        
        public override LoadOperation Load(EntityQuery query, LoadBehavior loadBehavior, Action<LoadOperation> callback, object userState)
        {
            this.LoadCalled = true;
            return base.Load(query, loadBehavior, callback, userState);
        }

        public override SubmitOperation SubmitChanges(Action<SubmitOperation> callback, object userState)
        {
            this.SubmitChangesCalled = true;
            return base.SubmitChanges(callback, userState);
        }
    }
}
