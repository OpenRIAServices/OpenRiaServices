namespace OpenRiaServices.Hosting.Wcf.OData
{
    #region Namespaces.
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using OpenRiaServices.Server;
    using OpenRiaServices.Hosting.Local;

    #endregion Namespaces.

    [DebuggerDisplay("{statusCode}: {Message}")]
    internal sealed class DomainDataServiceException : InvalidOperationException
    {
        #region Private fields.

        /// <summary>Language for the exception message.</summary>
        private readonly string messageLanguage;

        /// <summary>Error code to be used in payloads.</summary>
        private readonly string errorCode;

        /// <summary>HTTP response status code for this exception.</summary>
        private readonly int statusCode;

        /// <summary>'Allow' response for header.</summary>
        private string responseAllowHeader;

        #endregion Private fields.

        #region Constructors.

        /// <summary>
        /// Initializes a new instance of the DomainDataServiceException class.
        /// </summary>
        /// <remarks>
        /// The Message property is initialized to a system-supplied message 
        /// that describes the error. This message takes into account the 
        /// current system culture. The StatusCode property is set to 500
        /// (Internal Server Error).
        /// </remarks>
        public DomainDataServiceException()
            : this(500, Resource.DomainDataService_General_Error)
        {
        }

        /// <summary>
        /// Initializes a new instance of the DomainDataServiceException class.
        /// </summary>
        /// <param name="message">Plain text error message for this exception.</param>
        /// <remarks>
        /// The StatusCode property is set to 500 (Internal Server Error).
        /// </remarks>
        public DomainDataServiceException(string message)
            : this(500, message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the DomainDataServiceException class.
        /// </summary>
        /// <param name="message">Plain text error message for this exception.</param>
        /// <param name="innerException">Exception that caused this exception to be thrown.</param>
        /// <remarks>
        /// The StatusCode property is set to 500 (Internal Server Error).
        /// </remarks>
        public DomainDataServiceException(string message, Exception innerException)
            : this(500, message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the DomainDataServiceException class.
        /// </summary>
        /// <param name="statusCode">HTTP response status code for this exception.</param>
        /// <param name="message">Plain text error message for this exception.</param>
        public DomainDataServiceException(int statusCode, string message)
            : this(statusCode, message, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the DomainDataServiceException class.
        /// </summary>
        /// <param name="statusCode">HTTP response status code for this exception.</param>
        /// <param name="message">Plain text error message for this exception.</param>
        /// <param name="innerException">Exception that caused this exception to be thrown.</param>
        public DomainDataServiceException(int statusCode, string message, Exception innerException)
            : base(message, innerException)
        {
            Debug.Assert(!innerException.IsFatal(), "Fatal exception passed in as InnerException");

            this.errorCode = String.Empty;
            this.messageLanguage = CultureInfo.CurrentCulture.Name;
            this.statusCode = statusCode;
        }

        #endregion Constructors.

        #region Public properties.

        /// <summary>Error code to be used in payloads.</summary>
        public string ErrorCode
        {
            get { return this.errorCode; }
        }

        /// <summary>Language for the exception Message.</summary>
        public string MessageLanguage
        {
            get { return this.messageLanguage; }
        }

        /// <summary>Response status code for this exception.</summary>
        public int StatusCode
        {
            get { return this.statusCode; }
        }

        #endregion Public properties.

        #region Internal properties.

        /// <summary>'Allow' response for header.</summary>
        internal string ResponseAllowHeader
        {
            get { return this.responseAllowHeader; }
            set { this.responseAllowHeader = value; }
        }

        #endregion Internal properties.

        #region Internal Methods.

        /// <summary>
        /// Raise the proper DomainDataServiceException if we encounter an error during execution of the DomainService operation.
        /// </summary>
        /// <param name="validationErrors">ValidationResults from a DomainService operation execution.</param>
        internal static void HandleValidationErrors(IEnumerable<ValidationResult> validationErrors)
        {
            if (validationErrors != null && validationErrors.Any())
            {
                IEnumerable<ValidationResultInfo> operationErrors = validationErrors.Select(ve => new ValidationResultInfo(ve.ErrorMessage, ve.MemberNames));
                OperationException oe = new OperationException(Resource.DomainDataService_OperationError, operationErrors);
                throw new DomainDataServiceException(Resource.DomainDataService_General_Error, oe);
            }
        }

        #endregion Internal Methods.
    }
}