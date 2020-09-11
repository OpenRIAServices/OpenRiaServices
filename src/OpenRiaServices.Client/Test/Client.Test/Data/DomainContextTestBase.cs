using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Silverlight.Testing;

namespace OpenRiaServices.Client.Test
{
    public enum ProviderType
    {
        /// <summary>
        /// Linq To Sql provider
        /// </summary>
        LTS,
        /// <summary>
        /// Linq to Entities provider
        /// </summary>
        EF,
        /// <summary>
        /// Unspecified, non-DAL provider
        /// </summary>
        Unspecified
    }

    /// <summary>
    /// Base class used for testing a single DomainContext.
    /// </summary>
    /// <typeparam name="T">Type of the DomainContext</typeparam>
    public abstract class DomainContextTestBase<T> : UnitTestBase where T : DomainContext
    {
        private readonly Uri serviceUri;
        private readonly ProviderType providerType;
        private T ctxt;
        private LoadOperation loadOperation;
        private SubmitOperation submitOperation;

        protected Uri ServiceUri
        {
            get
            {
                return this.serviceUri;
            }
        }

        public DomainContextTestBase(Uri serviceUri, ProviderType providerType) {
            this.serviceUri = serviceUri;
            this.providerType = providerType;
        }

        public ProviderType ProviderType
        {
            get
            {
                return providerType;
            }
        }

        /// <summary>
        /// For cross provider unit tests, this method can be used to skip a test
        /// for a particular provider
        /// </summary>
        /// <param name="currProviderType">The provider type to ignore the test for</param>
        /// <returns>True if the test should be ignored</returns>
        public bool IgnoreForProvider(ProviderType providerType)
        {
            return this.providerType == providerType;
        }

        protected T CreateDomainContext() {
            this.ctxt = (T)Activator.CreateInstance(typeof(T), serviceUri);
            return ctxt;
        }

        public void Load(EntityQuery query)
        {
            this.Load(query, LoadBehavior.KeepCurrent);
        }

        public void Load(EntityQuery query, LoadBehavior loadBehavior)
        {
            Action<LoadOperation> callback = delegate(LoadOperation lo)
            {
                if (lo.HasError)
                {
                    lo.MarkErrorAsHandled();
                }
            };

            this.loadOperation = this.ctxt.Load(query, loadBehavior, callback, null);
        }

        public void SubmitChanges()
        {
            this.SubmitChanges(this.SubmitComplete, null);
        }

        public void SubmitChanges(Action<SubmitOperation> callback, object userState)
        {
            this.submitOperation = this.ctxt.SubmitChanges(callback, userState);
        }

        private void SubmitComplete(SubmitOperation submitOperation)
        {
            if (submitOperation.HasError)
            {
                submitOperation.MarkErrorAsHandled();
            }
        }

        public LoadOperation LoadOperation
        {
            get
            {
                return this.loadOperation;
            }
        }

        public SubmitOperation SubmitOperation
        {
            get
            {
                return this.submitOperation;
            }
        }

        protected bool IsLoadComplete {
            get {
                return this.loadOperation != null && this.loadOperation.IsComplete;
            }
        }

        protected bool IsSubmitComplete
        {
            get
            {
                return this.submitOperation!= null && this.submitOperation.IsComplete;
            }
        }

        /// <summary>
        /// For the last query or submit operation performed, assert that there
        /// was no error
        /// </summary>
        protected void AssertSuccess()
        {
            if (submitOperation != null && submitOperation.Error != null)
            {
                Assert.Fail(submitOperation.Error.Message + ": " + submitOperation.Error.StackTrace);
            }
            else if (loadOperation != null && loadOperation.Error != null)
            {
                Assert.Fail(loadOperation.Error.Message + ": " + loadOperation.Error.StackTrace);
            }
        }
    }
}
