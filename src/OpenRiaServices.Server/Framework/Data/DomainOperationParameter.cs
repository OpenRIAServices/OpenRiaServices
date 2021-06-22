using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace OpenRiaServices.Server
{
    /// <summary>
    /// Represents a parameter to a domain operation
    /// </summary>
    public sealed class DomainOperationParameter
    {
        private readonly string _name;
        private readonly Type _parameterType;
        private readonly AttributeCollection _attributes;
        private readonly bool _isOut;

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

        private Nullable<bool> _HasValidationAttribute;
        /// <summary>
        /// Returns true if this type has validation attribute
        /// </summary>
        public bool HasValidationAttributeOnProperties
        {
            get
            {
                if (TypeUtility.IsPredefinedSimpleType(this._parameterType))
                    _HasValidationAttribute = false;
                else if (!_HasValidationAttribute.HasValue)
                {
                    _HasValidationAttribute = false;
                    foreach (var Prop in _parameterType.GetProperties())
                    {
                        var Attributes = Prop.GetCustomAttributes<System.ComponentModel.DataAnnotations.ValidationAttribute>();
                        if (Attributes.Any())
                        {
                            _HasValidationAttribute = true;
                            break;
                        }
                    }
                    if (!_HasValidationAttribute.HasValue)
                    {
                        var Metadata = this._parameterType.GetCustomAttribute<System.ComponentModel.DataAnnotations.MetadataTypeAttribute>();
                        if (Metadata != null)
                            foreach (var Prop in Metadata.MetadataClassType.GetProperties())
                            {
                                var Attributes = Prop.GetCustomAttributes<System.ComponentModel.DataAnnotations.ValidationAttribute>();
                                if (Attributes.Any())
                                {
                                    _HasValidationAttribute = true;
                                    break;
                                }
                            }
                    }
                }
                return _HasValidationAttribute.Value;
            }
        }
    }
}
