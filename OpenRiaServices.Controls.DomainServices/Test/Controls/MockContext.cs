using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls.Test;
using OpenRiaServices.DomainServices.Client;

namespace System.Windows.Controls.DomainServices.Test
{
    public abstract class MockContext : DomainContext
    {
        #region Member Fields

        private Action<EntityQuery, LoadBehavior, object> _loadCallback;
        private Action<object> _submitChangesCallback;

        #endregion Member Fields

        #region All Constructors

        public MockContext(Action<EntityQuery, LoadBehavior, object> loadCallback, Action<object> submitChangesCallback)
            : base(new NullReturningMockDomainClient())
        {
            this._loadCallback = loadCallback;
            this._submitChangesCallback = submitChangesCallback;
        }

        public override LoadOperation Load(EntityQuery query, LoadBehavior loadBehavior, Action<LoadOperation> callback, object userState)
        {
            if (this._loadCallback != null)
            {
                this._loadCallback(query, loadBehavior, userState);
            }

            return base.Load(query, loadBehavior, callback, userState);
        }

        public override SubmitOperation SubmitChanges(Action<SubmitOperation> callback, object userState)
        {
            if (this._submitChangesCallback != null)
            {
                this._submitChangesCallback(userState);
            }

            return base.SubmitChanges(callback, userState);
        }

        #endregion All Constructors

        #region Nested Classes / Enums / Delegates

        /// <summary>
        /// Sample DomainClient implementation that returns null from all methods.
        /// </summary>
        private class NullReturningMockDomainClient : DomainClient
        {
            public NullReturningMockDomainClient()
            {
            }

            protected override IAsyncResult BeginQueryCore(EntityQuery query, AsyncCallback callback, object userState)
            {
                return null;
            }

            protected override QueryCompletedResult EndQueryCore(IAsyncResult asyncResult)
            {
                return null;
            }

            protected override IAsyncResult BeginSubmitCore(EntityChangeSet changeSet, AsyncCallback callback, object userState)
            {
                return null;
            }

            protected override SubmitCompletedResult EndSubmitCore(IAsyncResult asyncResult)
            {
                return null;
            }

            protected override IAsyncResult BeginInvokeCore(InvokeArgs invokeArgs, AsyncCallback callback, object userState)
            {
                return null;
            }

            protected override InvokeCompletedResult EndInvokeCore(IAsyncResult asyncResult)
            {
                return null;
            }
        }

        #endregion Nested Classes / Enums / Delegates
    }

    public class MockContext<T> : MockContext where T : Entity, new()
    {
        #region Member Fields

        EntitySetOperations _operationsSupported = EntitySetOperations.All;

        #endregion Member Fields

        #region All Constructors

        public MockContext(Action<EntityQuery, LoadBehavior, object> loadCallback, Action<object> submitChangesCallback)
            : base(loadCallback, submitChangesCallback)
        {
        }

        public MockContext(EntitySetOperations operationsSupported, Action<EntityQuery, LoadBehavior, object> loadCallback, Action<object> submitChangesCallback)
            : base(loadCallback, submitChangesCallback)
        {
            this._operationsSupported = operationsSupported;
        }

        public MockContext(EntitySetOperations operationsSupported)
            : this(operationsSupported, null, null)
        {
        }

        #endregion All Constructors

        #region Properties

        public EntitySet<T> Items { get { return this.EntityContainer.GetEntitySet<T>(); } }

        #endregion Properties

        #region Overrides

        protected override EntityContainer CreateEntityContainer()
        {
            MockEntityContainer container = new MockEntityContainer();
            container.CreateSet<T>(this._operationsSupported);

            return container;
        }

        #endregion Overrides

        #region Query Methods

        public EntityQuery<T> BasicQuery()
        {
            return base.CreateQuery<T>("BasicQuery", null, false, true);
        }

        public EntityQuery<T> OverloadQuery()
        {
            return base.CreateQuery<T>("OverloadQuery", null, false, true);
        }

        public EntityQuery<T> OverloadQuery(object parameter1)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("parameter1", parameter1);

            return base.CreateQuery<T>("OverloadQuery", parameters, false, true);
        }

        public EntityQuery<T> NoQuerySuffix()
        {
            return base.CreateQuery<T>("NoQuerySuffix", null, false, true);
        }

        public EntityQuery<T> WithAndWithoutSuffix()
        {
            return base.CreateQuery<T>("WithAndWithoutSuffix", null, false, true);
        }

        public EntityQuery<T> WithAndWithoutSuffixQuery()
        {
            return base.CreateQuery<T>("WithAndWithoutSuffixQuery", null, false, true);
        }

        #endregion Query Methods
    }
}
