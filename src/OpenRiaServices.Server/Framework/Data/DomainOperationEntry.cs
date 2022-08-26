using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenRiaServices.Server
{
    /// <summary>
    /// Represents a domain operation method within a DomainService
    /// </summary>
    public abstract class DomainOperationEntry
    {
        private readonly string _methodName;
        private Attribute _operationAttribute;
        private AttributeCollection _attributes;
        private Type _associatedType;
        private readonly Type _actualReturnType;
        private readonly Type _domainServiceType;
        private LazyBool _requiresValidation;
        private LazyBool _requiresAuthorization;

        /// <summary>
        /// Initializes a new instance of the DomainOperationEntry class
        /// </summary>
        /// <param name="domainServiceType">The <see cref="DomainService"/> Type this operation is a member of.</param>
        /// <param name="name">The name of the operation</param>
        /// <param name="operation">The <see cref="DomainOperation"/></param>
        /// <param name="returnType">The return Type of the operation</param>
        /// <param name="parameters">The parameter definitions for the operation</param>
        /// <param name="attributes">The method level attributes for the operation</param>  
        protected DomainOperationEntry(Type domainServiceType, string name, DomainOperation operation, Type returnType, IEnumerable<DomainOperationParameter> parameters, AttributeCollection attributes)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (returnType == null)
            {
                throw new ArgumentNullException(nameof(returnType));
            }
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }
            if (attributes == null)
            {
                throw new ArgumentNullException(nameof(attributes));
            }
            if (domainServiceType == null)
            {
                throw new ArgumentNullException(nameof(domainServiceType));
            }

            if (operation == DomainOperation.None)
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resource.InvalidDomainOperationEntryType, Enum.GetName(typeof(DomainOperation), operation)));
            }

            bool isTaskType = TypeUtility.IsTaskType(returnType);

            this._methodName = isTaskType ? RemoveAsyncFromName(name) : name;
            this._actualReturnType = returnType;
            this.ReturnType = isTaskType ? TypeUtility.GetTaskReturnType(returnType) : returnType;
            this._attributes = attributes;
            this.Operation = operation;
            this._domainServiceType = domainServiceType;

            List<DomainOperationParameter> effectiveParameters = parameters.ToList();
            int paramCount = effectiveParameters.Count;
            if (paramCount > 0)
            {
                DomainOperationParameter lastParameter = effectiveParameters[paramCount - 1];
                if (lastParameter.IsOut && lastParameter.ParameterType.HasElementType && lastParameter.ParameterType.GetElementType() == typeof(int))
                {
                    this.HasOutCountParameter = true;
                    effectiveParameters = effectiveParameters.Take(paramCount - 1).ToList();
                }
            }
            this.Parameters = effectiveParameters.AsReadOnly();
        }

        /// <summary>
        /// Removes any trailing "Async" from the specific name.
        /// </summary>
        /// <param name="name">A name.</param>
        /// <returns>name, but without "Async" at the end</returns>
        private static string RemoveAsyncFromName(string name)
        {
            const string asyncPostfix = "Async";
            if (name.EndsWith(asyncPostfix) && name.Length > asyncPostfix.Length)
                return name.Substring(0, name.Length - asyncPostfix.Length);
            else
                return name;
        }

        /// <summary>
        /// Gets a string value indicating the logical operation type
        /// corresponding to the current <see cref="Operation"/> value.
        /// </summary>
        /// <value>
        /// The value returned by this property is used in <see cref="System.ComponentModel.DataAnnotations.AuthorizationContext.OperationType"/>
        /// to describe the category of operation being authorized.
        /// <para>This helper property exists to avoid the overhead of <see cref="Enum.GetName"/> and
        /// to map"Custom" into "Update".  These strings are not localized because they are meant
        /// to be used in authorization rules that work independent of culture.
        /// </para>
        /// </value>
        internal string OperationType
        {
            get
            {
                switch (this.Operation)
                {
                    case DomainOperation.Query:
                        return "Query";

                    case DomainOperation.Insert:
                        return "Insert";

                    case DomainOperation.Update:
                    case DomainOperation.Custom:
                        return "Update";

                    case DomainOperation.Delete:
                        return "Delete";

                    case DomainOperation.Invoke:
                        return "Invoke";

                    default:
                        System.Diagnostics.Debug.Fail("Unknown DomainOperation type");
                        return "Unknown";
                }
            }
        }

        /// <summary>
        /// Gets the <see cref="DomainService"/> Type this operation is a member of.
        /// </summary>
        public Type DomainServiceType => this._domainServiceType;

        /// <summary>
        /// Gets the name of the operation
        /// </summary>
        public string Name => this._methodName;

        /// <summary>
        /// Gets the attribute that contains metadata about the operation.
        /// </summary>
        public Attribute OperationAttribute
        {
            get
            {
                this.InitializeOperationAttribute();
                return this._operationAttribute;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this operation requires validation.
        /// </summary>
        internal bool RequiresValidation
        {
            get
            {
                LazyBool value = this._requiresValidation;
                if (value == LazyBool.Unknown)
                {
                    value = CalculateRequiresValidation() ? LazyBool.True : LazyBool.False;
                    _requiresValidation = value;
                }
                // value will only be True or False here
                return value == LazyBool.True;
            }
        }

        /// <summary>
        /// Determines if the current operation requires validation
        /// </summary>
        /// <returns><c>true</c> if validation is required</returns>
        private bool CalculateRequiresValidation()
        {
            return this._attributes[typeof(ValidationAttribute)] != null
                || this.Parameters.Any(p =>
                    {
                        if (p.Attributes[typeof(ValidationAttribute)] != null)
                            return true;

                        // Complex parameters need to be validated if validation occurs on the
                        // type itself.
                        if (TypeUtility.IsSupportedComplexType(p.ParameterType))
                        {
                            Type complexType = TypeUtility.GetElementType(p.ParameterType);
                            MetaType metaType = MetaType.GetMetaType(complexType);
                            return metaType.RequiresValidation;
                        }

                        return false;
                    });
        }

        /// <summary>
        /// Gets a value indicating whether this operation requires authorization.
        /// </summary>
        internal bool RequiresAuthorization
        {
            get
            {
                LazyBool value = this._requiresAuthorization;
                if (value == LazyBool.Unknown)
                {
                    // Determine whether this operation requires authorization. AuthorizationAttributes may appear on
                    // the DomainService type as well as the DomainOperationEntry method.
                    bool requiresAuthorization = this._attributes[typeof(AuthorizationAttribute)] != null
                        || DomainServiceDescription.GetDescription(this._domainServiceType).Attributes[typeof(AuthorizationAttribute)] != null;

                    value = requiresAuthorization ? LazyBool.True : LazyBool.False;
                    _requiresAuthorization = value;
                }
                // value will only be True or False here
                return value == LazyBool.True;
            }
        }

        /// <summary>
        /// Based on the operation type specified, create the default corresponding attribute
        /// if it hasn't been specified explicitly, and add it to the attributes collection.
        /// </summary>
        private void InitializeOperationAttribute()
        {
            if (this._operationAttribute != null)
            {
                return;
            }

            bool attributeCreated = false;
            switch (this.Operation)
            {
                case DomainOperation.Query:
                    this._operationAttribute = this._attributes[typeof(QueryAttribute)];
                    if (this._operationAttribute == null)
                    {
                        QueryAttribute qa = new QueryAttribute();
                        // singleton returning query methods aren't composable
                        qa.IsComposable = TypeUtility.FindIEnumerable(this.ReturnType) != null;
                        this._operationAttribute = qa;
                        attributeCreated = true;
                    }
                    break;
                case DomainOperation.Insert:
                    this._operationAttribute = this._attributes[typeof(InsertAttribute)];
                    if (this._operationAttribute == null)
                    {
                        this._operationAttribute = new InsertAttribute();
                        attributeCreated = true;
                    }
                    break;
                case DomainOperation.Update:
                    this._operationAttribute = this._attributes[typeof(UpdateAttribute)];
                    if (this._operationAttribute == null)
                    {
                        this._operationAttribute = new UpdateAttribute();
                        attributeCreated = true;
                    }
                    break;
                case DomainOperation.Delete:
                    this._operationAttribute = this._attributes[typeof(DeleteAttribute)];
                    if (this._operationAttribute == null)
                    {
                        this._operationAttribute = new DeleteAttribute();
                        attributeCreated = true;
                    }
                    break;
                case DomainOperation.Invoke:
                    this._operationAttribute = this._attributes[typeof(InvokeAttribute)];
                    if (this._operationAttribute == null)
                    {
                        this._operationAttribute = new InvokeAttribute();
                        attributeCreated = true;
                    }
                    break;
                case DomainOperation.Custom:
                    this._operationAttribute = this._attributes[typeof(EntityActionAttribute)];
                    if (this._operationAttribute == null)
                    {
                        this._operationAttribute = new EntityActionAttribute();
                        attributeCreated = true;
                    }
                    break;
                default:
                    break;
            }

            if (attributeCreated)
            {
                if (this._attributes == null)
                {
                    this._attributes = new AttributeCollection(this._operationAttribute);
                }
                else
                {
                    this._attributes = AttributeCollection.FromExisting(this._attributes, this._operationAttribute);
                }
            }
        }

        /// <summary>
        /// Gets the attributes for the operation
        /// </summary>
        public AttributeCollection Attributes
        {
            get
            {
                this.InitializeOperationAttribute();
                return this._attributes;
            }
            internal set
            {
                this._attributes = value;

                // need to reset computed flags that are based
                // on operation attributes so they will be recomputed
                this._requiresValidation = LazyBool.Unknown;
                this._requiresAuthorization = LazyBool.Unknown;
            }
        }

        /// <summary>
        /// Gets the return Type of the operation
        /// </summary>
        public Type ReturnType { get; }

        /// <summary>
        /// Gets a value indicating whether the actual return type is a Task or Task{T}.
        /// </summary>
        public bool IsTaskAsync => TypeUtility.IsTaskType(this._actualReturnType);

        /// <summary>
        /// Gets the parameters of the operation
        /// </summary>
        public ReadOnlyCollection<DomainOperationParameter> Parameters { get; }

        /// <summary>
        /// Invokes this <see cref="DomainOperationEntry" />.
        /// </summary>
        /// <param name="domainService">The <see cref="DomainService"/> instance the operation is being invoked on.</param>
        /// <param name="parameters">The parameters to pass to the method.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to signal cancellation of this operation</param>
        /// <returns>The return value of the invoked method.</returns>
        public abstract ValueTask<object> InvokeAsync(DomainService domainService, object[] parameters, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the type of domain operation implemented by the method.
        /// </summary>
        public DomainOperation Operation { get; internal set; }

        /// <summary>
        /// Returns the associated Type this DomainOperation operates on. For query methods
        /// this will be the element type of the return type (or the singleton return Type),
        /// and for all other methods this will be the Type of the first method parameter.
        /// </summary>
        public Type AssociatedType
        {
            get
            {
                if (this._associatedType == null)
                {
                    if (this.Operation == DomainOperation.Query)
                    {
                        Type entityType = TypeUtility.FindIEnumerable(this.ReturnType);
                        if (entityType != null)
                        {
                            entityType = entityType.GetGenericArguments()[0];
                        }
                        else
                        {
                            entityType = this.ReturnType;
                        }
                        this._associatedType = entityType;
                    }
                    else
                    {
                        if (this.Parameters.Count > 0)
                        {
                            this._associatedType = this.Parameters[0].ParameterType;
                        }
                    }
                }

                return this._associatedType;
            }
        }

        private bool HasOutCountParameter { get; }

        /// <summary>
        /// Invokes this <see cref="DomainOperationEntry" />.
        /// </summary>
        /// <param name="domainService">The <see cref="DomainService"/> instance the operation is being invoked on.</param>
        /// <param name="parameters">The parameters to pass to the method.</param>
        /// <param name="totalCount">The total number of rows for the input query without any paging applied to it.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to signal cancellation of this operation</param>
        /// <returns>The return value of the invoked method.</returns>
        internal ValueTask<object> InvokeAsync(DomainService domainService, object[] parameters, out int totalCount, CancellationToken cancellationToken)
        {
            if (this.HasOutCountParameter)
            {
                object[] parametersWithCount = new object[parameters.Length + 1];
                parameters.CopyTo(parametersWithCount, 0);
                parametersWithCount[parameters.Length] = 0;

                object result = this.InvokeAsync(domainService, parametersWithCount, cancellationToken)
                    .GetAwaiter() // Cant use await since method has out parameter
                    .GetResult();

                totalCount = (int)parametersWithCount[parameters.Length];
                return new ValueTask<object>(result);
            }
            else
            {
                totalCount = DomainService.TotalCountUndefined;
                return this.InvokeAsync(domainService, parameters, cancellationToken);
            }
        }

        /// <summary>
        /// Returns a textual description of the <see cref="DomainOperationEntry"/>.
        /// </summary>
        /// <returns>A string representation of the <see cref="DomainOperationEntry"/>.</returns>
        public override string ToString()
        {
            StringBuilder output = new StringBuilder();
            output.AppendFormat(CultureInfo.InvariantCulture, "{0} {1}(", this.ReturnType, this.Name);
            for (int i = 0; i < this.Parameters.Count; i++)
            {
                if (i > 0)
                {
                    output.Append(", ");
                }
                output.Append(this.Parameters[i].ToString());
            }
            output.Append(')');
            return output.ToString();
        }

        /// <summary>
        /// Lazily initialized boolean
        /// It is used for lazy initialized boolean properties, 
        /// since read/writes of primitive types less than or equal to wordsizze is
        /// atomic. But bool? is not guaranteed to be atomic and multithread safe 
        /// (even it it probably is since it should be 2 bytes large)
        /// 
        /// See C# specication at
        /// https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/variables#atomicity-of-variable-references
        /// </summary>
        private enum LazyBool
        {
            Unknown,
            False,
            True
        }
    }
}
