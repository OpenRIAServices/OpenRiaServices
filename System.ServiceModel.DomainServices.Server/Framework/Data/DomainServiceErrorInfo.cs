namespace System.ServiceModel.DomainServices.Server
{
    /// <summary>
    /// Represents an unrecoverable error that occurred during the
    /// processing of a <see cref="DomainService"/> operation.
    /// </summary>
    public sealed class DomainServiceErrorInfo
    {
        private Exception _exception;

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainServiceErrorInfo"/> class.
        /// </summary>
        /// <param name="exception">The exception that occurred.</param>
        public DomainServiceErrorInfo(Exception exception)
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
