using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.ComponentModel.DataAnnotations
{
    /// <summary>
    /// Container class for the results of a authorization request.
    /// </summary>
    /// <remarks>
    /// See <see cref="AuthorizationAttribute"/> for details regarding the
    /// usage of this class.
    /// <para>
    /// Use the static <see cref="AuthorizationResult.Allowed"/> to represent successful authorization.
    /// Any other non-null instance of this class is considered to be a denial of an authorization request.
    /// </para>
    /// </remarks>
    public sealed class AuthorizationResult
    {
        private readonly string _errorMessage;

        /// <summary>
        /// Gets an <see cref="AuthorizationResult"/> that indicates the requested operation is allowed.
        /// </summary>
        /// <remarks>
        /// The <c>null</c> value is used to indicate authorization approval.  Consumers and providers of <see cref="AuthorizationResult"/>
        /// should use <see cref="AuthorizationResult.Allowed"/> rather than an explicit <c>null</c>.
        /// </remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes",
            Justification = "The type is immutable and the value here is null.")]
        public static readonly AuthorizationResult Allowed;

        /// <summary>
        /// Initializes a new instance of <see cref="AuthorizationResult"/>.
        /// </summary>
        /// <remarks>This form of the constructor is meant to be used for denial of authorization.
        /// Authorization approval is always done with <see cref="AuthorizationResult.Allowed"/>
        /// </remarks>
        /// <param name="errorMessage">The user-visible error message.</param>
        public AuthorizationResult(string errorMessage)
        {
            this._errorMessage = errorMessage;
        }
 
        /// <summary>
        /// Gets the error message describing why authorization was denied.
        /// </summary>
        public string ErrorMessage
        {
            get
            {
                return this._errorMessage;
            }
        }
    }
}
