using System;
using System.Diagnostics;
using System.Globalization;
using OpenRiaServices.DomainServices;

namespace OpenRiaServices.DomainServices.Tools
{
    /// <summary>
    /// An exception that provides contextual information for exceptions
    /// that occur during attribute building.
    /// </summary>
    internal class AttributeBuilderException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AttributeBuilderException"/> class.
        /// </summary>
        /// <param name="innerException">
        /// The <see cref="Exception"/> that was thrown during the attribute building process.
        /// </param>
        /// <param name="attributeType">The type of attribute that caused the exception.</param>
        /// <param name="attributePropertyName">
        /// The name of the property on the <see cref="Attribute"/> that caused the exception.
        /// </param>
        /// <remarks>
        /// The message for this exception represents the code comment error as well as the
        /// prefix for the build warning.
        /// <para>
        /// The message of the <see cref="Exception.InnerException"/> is used for the exception
        /// details.
        /// </para>
        /// </remarks>
        public AttributeBuilderException(Exception innerException, Type attributeType, string attributePropertyName)
            : base(string.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_Attribute_ThrewException, attributeType, attributePropertyName), innerException)
        {
            Debug.Assert(!innerException.IsFatal(), "Fatal exception passed in as InnerException");
        }
    }
}
