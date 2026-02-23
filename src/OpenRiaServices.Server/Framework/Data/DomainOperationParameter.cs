using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace OpenRiaServices.Server
{
    /// <summary>
    /// Represents a parameter to a domain operation
    /// </summary>
    public sealed class DomainOperationParameter
    {

        /// <summary>
        /// Initializes a new instance of the DomainOperationParameter class
        /// </summary>
        /// <param name="name">The name of the parameter</param>
        /// <param name="parameterType">The Type of the parameter</param>
        /// <param name="attributes">The set of attributes for the parameter</param>
        public DomainOperationParameter(string name, Type parameterType, AttributeCollection attributes)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (parameterType == null)
            {
                throw new ArgumentNullException(nameof(parameterType));
            }

            if (attributes == null)
            {
                throw new ArgumentNullException(nameof(attributes));
            }

            Name = name;
            ParameterType = parameterType;
            Attributes = attributes;
        }

        /// <summary>
        /// Initializes a new instance of the DomainOperationParameter class
        /// </summary>
        /// <param name="name">The name of the parameter</param>
        /// <param name="parameterType">The Type of the parameter</param>   
        /// <param name="attributes">The set of attributes for the parameter</param>
        /// <param name="isOut">Indicates whether the parameter is a out parameter</param>
        public DomainOperationParameter(string name, Type parameterType, AttributeCollection attributes, bool isOut)
            : this(name, parameterType, attributes)
        {
            IsOut = isOut;
        }

        /// <summary>
        /// Gets the name of the parameter
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the Type of the parameter
        /// </summary>
        public Type ParameterType { get; }

        /// <summary>
        /// Gets the set of attributes for the parameter
        /// </summary>
        public AttributeCollection Attributes { get; }

        /// <summary>
        /// <see langword="true" /> if the parameter can have the value <see langword="null" />
        /// </summary>
        internal bool IsNullable => !ParameterType.IsValueType || TypeUtility.IsNullableType(ParameterType);

        /// <summary>
        /// Gets a value indicating whether the parameter is an out parameter
        /// </summary>
        public bool IsOut { get; }

        /// <summary>
        /// Returns a textual description of the parameter.
        /// </summary>
        /// <returns>A string representation of the parameter.</returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} {1}", this.ParameterType, this.Name);
        }
    }
}
