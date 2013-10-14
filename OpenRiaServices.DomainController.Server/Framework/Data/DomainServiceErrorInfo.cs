using System;

namespace OpenRiaServices.DomainController.Server
{
    /// <summary>
    /// Represents an unrecoverable error that occurred during the
    /// processing of a <see cref="DomainController"/> operation.
    /// </summary>
    public sealed class DomainControllerErrorInfo
    {
        private Exception _exception;

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainControllerErrorInfo"/> class.
        /// </summary>
        /// <param name="exception">The exception that occurred.</param>
        public DomainControllerErrorInfo(Exception exception)
        {
            this._exception = exception;
        }

        /// <summary>
        /// Gets or sets the exception that occurred.
        /// </summary>
        public Exception Error
        {
            get
            {
                return this._exception;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                this._exception = value;
            }
        }
    }
}
