using System;
using System.Collections.Generic;

#nullable enable

namespace OpenRiaServices.Client
{
    /// <summary>
    /// Represents a custom method invocation on an entity.
    /// </summary>
    public class EntityAction
    {
        private readonly object?[] _parameters;

        /// <summary>
        /// Initializes a new instance of the EntityAction class
        /// </summary>
        /// <param name="name">Name of the entity action</param>
        /// <param name="parameters">The parameters to pass to the entity action</param>
        public EntityAction(string name, params object?[] parameters)
        {
            this.Name = name;
            this._parameters = (parameters is not null) ? [.. parameters] : [];
        }

#if NET
        /// <summary>
        /// Initializes a new instance of the EntityAction class
        /// </summary>
        /// <param name="name">Name of the entity action</param>
        /// <param name="parameters">The parameters to pass to the entity action</param>
        public EntityAction(string name, params ReadOnlySpan<object?> parameters)
        {
            this.Name = name;
            this._parameters = parameters.Length > 0 ? [.. parameters] : [];
        }
#endif

        /// <summary>
        /// Gets the name of the entity action
        /// </summary>
        public string Name
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the parameters to pass to the entity action
        /// </summary>
        public IEnumerable<object?> Parameters
        {
            get
            {
                return this._parameters;
            }
        }

        /// <summary>
        /// Gets a value indicating whether any parameters were associated with this action.
        /// </summary>
        public bool HasParameters
        {
            get
            {
                return (this._parameters.Length > 0);
            }
        }
    }
}
