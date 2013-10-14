using System.Globalization;
using System.Reflection;
using System.Security.Principal;
using DataAnnotationsResources = OpenRiaServices.DomainController.Server.Resource;

namespace System.ComponentModel.DataAnnotations
{
    /// <summary>
    /// Abstract base class used to control authorization through custom metadata.
    /// </summary>
    /// <remarks>
    /// Subclasses of this attribute class are created for different authorization policies, and then these
    /// attributes can be applied to operations to ask that these policies be enforced.
    /// <para>Each subclass provides its own logic in its <see cref="IsAuthorized"/>
    /// method which is provided with an <see cref="IPrincipal"/> and an <see cref="AuthorizationContext"/>
    /// to make its decision.  These subclasses may also declare their own properties that can be
    /// specified in the attribute declaration and which can affect the authorization logic.
    /// </para>
    /// </remarks>
    public abstract class AuthorizationAttribute : Attribute
    {
        private Func<string> _errorMessageAccessor;
        private string _errorMessage;
        private Type _resourceType;

        /// <summary>
        /// Determines whether the given <paramref name="principal"/> is authorized to perform a specific operation
        /// described by the given <paramref name="authorizationContext"/>.
        /// </summary>
        /// <remarks>This method is the concrete entry point for authorization.  It delegates to the derived class's
        /// <see cref="IsAuthorized"/> method for implementation-specific authorization.
        /// </remarks>
        /// <param name="principal">The <see cref="IPrincipal"/> to be authorized.</param>
        /// <param name="authorizationContext">The <see cref="AuthorizationContext"/> describing the context in which
        /// authorization has been requested.</param>
        /// <returns>An <see cref="AuthorizationResult"/> that indicates whether the operation is allowed or denied.
        /// A return of <see cref="AuthorizationResult.Allowed"/> indicates the operation is allowed.
        /// A return of any other (non-null) <see cref="AuthorizationResult"/> indicates the request has been denied.
        /// The user visible error message for the denial is found in <see cref="AuthorizationResult.ErrorMessage"/>.
        /// </returns>
        public AuthorizationResult Authorize(IPrincipal principal, AuthorizationContext authorizationContext)
        {
            if (principal == null)
            {
                throw new ArgumentNullException("principal");
            }
            if (authorizationContext == null)
            {
                throw new ArgumentNullException("authorizationContext");
            }
            return this.IsAuthorized(principal, authorizationContext);
        }

        /// <summary>
        /// Gets an accessor that may be invoked to retrieve the runtime error message
        /// based on <see cref="ErrorMessage"/> and <see cref="ResourceType"/>
        /// </summary>
        private Func<string> ErrorMessageAccessor
        {
            get
            {
                if (this._errorMessageAccessor == null)
                {
                    this._errorMessageAccessor = this.CreateErrorMessageAccessor();
                }
                return this._errorMessageAccessor;
            }
        }

        /// <summary>
        /// Implementation specific method to determine whether the given <paramref name="principal"/>
        /// is authorized to perform a specific operation described by 
        /// the given <paramref name="authorizationContext"/>.
        /// </summary>
        /// <remarks>This protected abstract method contains the implementation-specific logic for this particular
        /// subclass of <see cref="AuthorizationAttribute"/>.  It is invoked strictly by the public <see cref="Authorize"/> method.
        /// </remarks>
        /// <param name="principal">The <see cref="IPrincipal"/> to be authorized.</param>
        /// <param name="authorizationContext">The <see cref="AuthorizationContext"/> describing the context in which
        /// authorization has been requested.</param>
        /// <returns>An <see cref="AuthorizationResult"/> that indicates whether the operation is allowed or denied.
        /// A return of <see cref="AuthorizationResult.Allowed"/> indicates the operation is allowed.
        /// A return of any other non-null <see cref="AuthorizationResult"/> indicates the request has been denied.
        /// The user visible error message for the denial is found in <see cref="AuthorizationResult.ErrorMessage"/>
        /// </returns>
        protected abstract AuthorizationResult IsAuthorized(IPrincipal principal, AuthorizationContext authorizationContext);

        /// <summary>
        /// Gets or sets the literal error message or resource key intended to be returned
        /// in a <see cref="AuthorizationResult.ErrorMessage"/>.
        /// </summary>
        /// <value>This property is meant to be set in the attribute declaration, and it serves as either a literal string or
        /// as a resource key.
        /// If <see cref="ResourceType"/> is non-null, this value is interpreted as the name of a property
        /// declared in <see cref="ResourceType"/> that will return the actual error message at runtime.  This is
        /// the mechanism that allows localization of error messages.  If <see cref="ResourceType"/> is null, this value
        /// is assumed to be a literal non-localized error message that can be used verbatim.
        /// </value>
        public string ErrorMessage
        {
            get
            {
                return this._errorMessage;
            }
            set
            {
                this._errorMessage = value;
                this._errorMessageAccessor = null;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="Type"/> to use as the resource manager for <see cref="ErrorMessage"/>.
        /// </summary>
        /// <value>This property is optional.  If null, <see cref="ErrorMessage"/> is treated as a literal string.
        /// But if it is not null, <see cref="ErrorMessage"/> is treated as the name of a static property within
        /// the specified <see cref="Type"/> that can be retrieved to yield the actual error message.
        /// </value>
        public Type ResourceType
        {
            get
            {
                return this._resourceType;
            }
            set
            {
                this._resourceType = value;
                this._errorMessageAccessor = null;
            }
        }

        /// <summary>
        /// Gets the formatted error message for the current <see cref="AuthorizationAttribute"/> to present to the user.
        /// </summary>
        /// <remarks>
        /// Classes derived from <see cref="AuthorizationAttribute"/> are encouraged to use this helper
        /// method to retrieve the user-visible message for <see cref="ValidationResult.ErrorMessage"/>
        /// because it encapsulates the logic to evaluate <see cref="ResourceType"/> and <see cref="ErrorMessage"/>.
        /// <para>
        /// If <see cref="ErrorMessage"/> and <see cref="ResourceType"/> are both non-null, this method
        /// will use Reflection to access that respective property in the respective resource type to obtain the message.
        /// If <see cref="ResourceType"/> is null, it will return the literal value from <see cref="ErrorMessage"/>.  
        /// But if that is blank, it will use a default localized message.
        /// </para>
        /// <para>
        /// The specified <paramref name="operation"/> will be included in the generated message if
        /// format specifiers are present in the computed message.
        /// </para>
        /// </remarks>
        /// <param name="operation">Name of the operation that was denied.</param>
        /// <returns>The error message to present to the user.</returns>
        protected string FormatErrorMessage(string operation)
        {
            // Create and cache an accessor for this.
            // We guarantee a non-null accessor or an InvalidOperationException if the properties are incorrect to create one.
            string message = this.ErrorMessageAccessor();

            // Optionally include the operation if the string contains {0} formatting information
            return string.Format(CultureInfo.CurrentCulture, message, operation);
        }

        #region Private Methods

        /// <summary>
        /// This factory method creates a func that returns a string for the error message.
        /// </summary>
        /// <returns>A new string func that can be used by <see cref="FormatErrorMessage"/></returns>
        private Func<string> CreateErrorMessageAccessor()
        {
            // If no resource type...
            if (this.ResourceType == null)
            {
                // An empty ErrorMessage returns a default message
                if (string.IsNullOrEmpty(this.ErrorMessage))
                {
                    return () => DataAnnotationsResources.AuthorizationAttribute_Default_Message;
                }
                // Else returns the non-empty ErrorMessage
                return () => this.ErrorMessage;
            }

            // If there is a ResourceType, validate and return Reflection-based property getter
            return this.CreateErrorMessagePropertyAccessor();
        }

        /// <summary>
        /// This factory method creates a Reflection-based string func to retrieve
        /// the named resource from the current resource type.
        /// </summary>
        /// <returns>A new string func that will invoke the respective property getter.</returns>
        private Func<string> CreateErrorMessagePropertyAccessor()
        {
            System.Diagnostics.Debug.Assert(this.ResourceType != null, "Null ResourceType");

            // The lack of an error message string here means the attribute was incorrectly declared.
            // This is the first reasonable point to detect this problem, and we raise an
            // InvalidOperationException to force the developer to fix the problem.
            if (string.IsNullOrEmpty(this.ErrorMessage))
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture,
                      DataAnnotationsResources.AuthorizationAttribute_Requires_ErrorMessage, this.GetType().FullName, this.ResourceType.FullName));
            }

            // Find the static public/internal property identified by ErrorMessage
            PropertyInfo property = this.ResourceType.GetProperty(this.ErrorMessage, BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
            if (property != null)
            {
                MethodInfo propertyGetter = property.GetGetMethod(true /*nonPublic*/);
                // We only support internal and public properties
                if (propertyGetter == null || (!propertyGetter.IsAssembly && !propertyGetter.IsPublic))
                {
                    // Set the property to null so the exception is thrown as if the property wasn't found
                    property = null;
                }
            }

            // Invalid property means the attribute was declared incorrectly.  Again, fail early.
            if (property == null || property.PropertyType != typeof(string))
            {
                throw new InvalidOperationException(
                    String.Format(
                    CultureInfo.CurrentCulture,
                    DataAnnotationsResources.AuthorizationAttribute_Requires_Valid_Property,
                    this.ResourceType.FullName,
                    this.ErrorMessage));
            }

            // Looks good, give them a func that will eval dynamically in case it changes
            return () => (string)property.GetValue(null, null);
        }
        #endregion
    }
}
