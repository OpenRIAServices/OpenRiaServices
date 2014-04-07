// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace OpenRiaServices.DomainControllers.Server
{
    /// <summary>
    /// Enumeration of the types of operations a <see cref="DomainController"/> can perform.
    /// </summary>
    public enum ChangeOperation
    {
        /// <summary>
        /// Indicates that no operation is to be performed
        /// </summary>
        None,

        /// <summary>
        /// Indicates an operation that inserts new data
        /// </summary>
        Insert,

        /// <summary>
        /// Indicates an operation that updates existing data
        /// </summary>
        Update,

        /// <summary>
        /// Indicates an operation that deletes existing data
        /// </summary>
        Delete,

        /// <summary>
        /// Indicates a custom update operation
        /// </summary>
        Custom
    }
}
