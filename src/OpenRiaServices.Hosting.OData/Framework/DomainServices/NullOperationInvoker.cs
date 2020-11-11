using System;

namespace OpenRiaServices.Hosting.WCF.OData
{
    #region Namespace
    using System.ServiceModel.Dispatcher;

    #endregion

    /// <summary>
    /// Operation invoker that does nothing.    
    /// </summary>
    internal class NullOperationInvoker : IOperationInvoker
    {
        /// <summary>
        /// Returns an array of parameter objects.
        /// </summary>
        /// <returns>The parameters that are to be used as arguments to the operation.</returns>
        public object[] AllocateInputs()
        {
            return Array.Empty<object>();
        }

        /// <summary>
        /// Returns an object and a set of output objects from an instance and set of input objects. 
        /// </summary>
        /// <param name="instance">The object to be invoked.</param>
        /// <param name="inputs">The inputs to the method.</param>
        /// <param name="outputs">The outputs from the method.</param>
        /// <returns>The return value.</returns>
        public object Invoke(object instance, object[] inputs, out object[] outputs)
        {
            outputs = Array.Empty<object>();
            return new object();
        }

        /// <summary>
        /// An asynchronous implementation of the Invoke method.
        /// </summary>
        /// <param name="instance">The object to be invoked.</param>
        /// <param name="inputs">The inputs to the method.</param>
        /// <param name="callback">The asynchronous callback object.</param>
        /// <param name="state">Associated state data.</param>
        /// <returns>A System.IAsyncResult used to complete the asynchronous call.</returns>
        public IAsyncResult InvokeBegin(object instance, object[] inputs, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// The asynchronous end method.
        /// </summary>
        /// <param name="instance">The object invoked.</param>
        /// <param name="outputs">The outputs from the method.</param>
        /// <param name="result">The System.IAsyncResult object.</param>
        /// <returns>The return value.</returns>
        public object InvokeEnd(object instance, out object[] outputs, IAsyncResult result)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets a value that specifies whether the Invoke or InvokeBegin method is called by the dispatcher.
        /// </summary>
        public bool IsSynchronous
        {
            get { return true; }
        }
    }
}
