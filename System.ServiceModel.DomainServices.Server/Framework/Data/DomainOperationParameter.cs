using System.ComponentModel;
using System.Globalization;

namespace System.ServiceModel.DomainServices.Server
{
    /// <summary>
    /// Represents a parameter to a domain operation
    /// </summary>
    public sealed class DomainOperationParameter
    {
        private string _name;
        private Type _parameterType;
        private AttributeCollection _attributes;
        private bool _isOut;

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
                throw new ArgumentNullException("name");
            }

            if (parameterType == null)
            {
                throw new ArgumentNullException("parameterType");
            }

            if (attributes == null)
            {
                throw new ArgumentNullException("attributes");
            }

            this._name = name;
            this._parameterType = parameterType;
            this._attributes = attributes;
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
            this._isOut = isOut;
        }

        /// <summary>
        /// Gets the name of the parameter
        /// </summary>
        public string Name
        {
            get
            {
                return this._name;
            }
        }

        /// <summary>
        /// Gets the Type of the parameter
        /// </summary>
        public Type ParameterType
        {
            get
            {
                return this._parameterType;
            }
        }

        /// <summary>
        /// Gets the set of attributes for the parameter
        /// </summary>
        public AttributeCollection Attributes
        {
            get
            {
                return this._attributes;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the parameter is an out parameter
        /// </summary>
        public bool IsOut
        {
            get
            {
                return this._isOut;
            }
        }

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
