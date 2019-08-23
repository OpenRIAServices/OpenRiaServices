using System;
using System.Collections.Generic;
using System.Reflection;

namespace OpenRiaServices.DomainServices.Tools
{
    /// <summary>
    /// Type used to represent an <see cref="Attribute"/> declaration and its requirements.
    /// </summary>
    internal sealed class AttributeDeclaration
    {
        private readonly Type _attributeType;
        private List<object> _constructorArguments;
        private Dictionary<string, object> _namedParameters;
        private readonly List<string> _errors;
        private List<Type> _requiredTypes;
        private List<PropertyInfo> _requiredProperties;
        private List<MethodInfo> _requiredMethods;

        /// <summary>
        /// Constructor accepting the <see cref="Attribute"/> <see cref="Type"/>.
        /// </summary>
        /// <param name="attributeType">The <see cref="Attribute"/> <see cref="Type"/> to represent. Cannot be null.</param>
        public AttributeDeclaration(Type attributeType)
        {
            if (attributeType == null)
            {
                throw new ArgumentNullException("attributeType");
            }

            this._attributeType = attributeType;
            this._errors = new List<string>();
        }

        /// <summary>
        /// Gets a boolean indicating whether or not the <see cref="AttributeDeclaration"/> has errors.
        /// </summary>
        public bool HasErrors
        {
            get
            {
                return this._errors.Count > 0;
            }
        }

        /// <summary>
        /// Gets the <see cref="Attribute"/> <see cref="Type"/>.
        /// </summary>
        public Type AttributeType
        {
            get
            {
                return this._attributeType;
            }
        }

        /// <summary>
        /// Gets a collection of constructor arguments.
        /// </summary>
        public IList<object> ConstructorArguments
        {
            get
            {
                if (this._constructorArguments == null)
                {
                    this._constructorArguments = new List<object>();
                }

                return this._constructorArguments;
            }
        }

        /// <summary>
        /// Gets a collection of error messages.
        /// </summary>
        public IList<string> Errors
        {
            get
            {
                return this._errors;
            }
        }

        /// <summary>
        /// Gets a dictionary of named parameters.
        /// </summary>
        public IDictionary<string, object> NamedParameters
        {
            get
            {
                if (this._namedParameters == null)
                {
                    this._namedParameters = new Dictionary<string, object>(StringComparer.Ordinal);
                }

                return this._namedParameters;
            }
        }

        /// <summary>
        /// Gets a collection of required shared types.
        /// </summary>
        public IList<Type> RequiredTypes
        {
            get
            {
                if (this._requiredTypes == null)
                {
                    this._requiredTypes = new List<Type>();
                }

                return this._requiredTypes;
            }
        }

        /// <summary>
        /// Gets a collection of required shared methods.
        /// </summary>
        public IList<MethodInfo> RequiredMethods
        {
            get
            {
                if (this._requiredMethods == null)
                {
                    this._requiredMethods = new List<MethodInfo>();
                }

                return this._requiredMethods;
            }
        }

        /// <summary>
        /// Gets a collection of required shared properties.
        /// </summary>
        public IList<PropertyInfo> RequiredProperties
        {
            get
            {
                if (this._requiredProperties == null)
                {
                    this._requiredProperties = new List<PropertyInfo>();
                }

                return this._requiredProperties;
            }
        }
    }
}
