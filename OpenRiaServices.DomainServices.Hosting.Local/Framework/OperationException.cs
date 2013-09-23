using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Permissions;
using OpenRiaServices.DomainServices.Server;

namespace OpenRiaServices.DomainServices.Hosting.Local
{
    // TODO: use these summary comments (or similar) when bug 597393 is addressed.
    ///// <summary>
    ///// This exception is used to indicate operational errors occurring during the
    ///// invocation of <see cref="OpenRiaServices.DomainServices.Server.DomainService"/> operations.
    ///// </summary>

    /// <summary>
    /// This exception is raised by generated <see cref="OpenRiaServices.DomainServices.Server.DomainService"/> proxies 
    /// when errors are encountered during invocation of <see cref="OpenRiaServices.DomainServices.Server.DomainService"/> 
    /// operations.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2240:ImplementISerializableCorrectly",
        Justification = "FxCop should not insist SecurityTransparent code implement GetObjectData.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors",
        Justification = "The exception is not publicly constructable and is sealed.")]
    [Serializable]
    public sealed class OperationException : Exception
    {
        // To comply with the CLR 4.0 safe serialization contract, we maintain
        // a private state member which is not serialized implicitly.
        // [NonSerialized] -- TODO: uncomment when CLR fixes 851783
        private OperationExceptionData _data = new OperationExceptionData();

        // This is the state object that is explicitly serialized and
        // deserialized to use the new CLR 4.0 safe serialization feature.
        // [Serializable] -- TODO: uncomment when CLR fixes 851783
        private struct OperationExceptionData // : ISafeSerializationData  -- TODO: uncomment when CLR fixes 851783
        {
            public ValidationResultInfo[] OperationErrors;

            //  TODO: uncomment when CLR fixes 851783
            ///// <summary>
            ///// Called by the deserializer with an newly instantiated
            ///// but uninitialized exception instance to populate.
            ///// </summary>
            ///// <param name="obj"></param>
            //void ISafeSerializationData.CompleteDeserialization(object obj)
            //{
            //    OperationException exception = obj as OperationException;
            //    exception._data = this;
            //}
        }

        /// <summary>
        /// Initializes a new <see cref="OperationException"/> instance.
        /// </summary>
        /// <param name="message">A message describing the exception.</param>
        /// <param name="operationError">The <see cref="ValidationResultInfo"/> associated with this exception.</param>
        /// <exception cref="ArgumentNullException">if <paramref name="operationError"/> is null.</exception>
        internal OperationException(string message, ValidationResultInfo operationError)
            : this(message, new[] { operationError })
        {
            if (operationError == null)
            {
                throw new ArgumentNullException("operationError");
            }
        }

        /// <summary>
        /// Initializes a new <see cref="OperationException"/> instance.
        /// </summary>
        /// <param name="message">A message describing the exception.</param>
        /// <param name="operationErrors">The <see cref="ValidationResultInfo"/>s associated with this exception.</param>
        /// <exception cref="ArgumentNullException">if <paramref name="operationErrors"/> is null.</exception>
        internal OperationException(string message, IEnumerable<ValidationResultInfo> operationErrors)
            : base(message)
        {
            if (operationErrors == null)
            {
                throw new ArgumentNullException("operationErrors");
            }

            this._data.OperationErrors = operationErrors.ToArray();

            //  TODO: uncomment when CLR fixes 851783
            //// The new CLR 4.0 safe serialization model accepts custom data through
            //// this pattern.  We are called back during serialization to provide
            //// our custom data.
            //SerializeObjectState += delegate(object exception, SafeSerializationEventArgs eventArgs)
            //{
            //    eventArgs.AddSerializedState(this._data);
            //};
        }

        /// <summary>
        /// Gets a collection of <see cref="ValidationResultInfo"/> associated with this exception. 
        /// </summary>
        public IEnumerable<ValidationResultInfo> OperationErrors
        {
            get
            {
                return this._data.OperationErrors.ToArray();
            }
        }
    }
}
