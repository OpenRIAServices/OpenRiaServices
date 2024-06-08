using System;
using System.Collections.Generic;

#pragma warning disable CS3015 // Type has no accessible constructors which use only CLS-compliant types

namespace OpenRiaServices
{
    /// <summary>
    /// Used to mark an Entity member as an association
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]

    public sealed class EntityAssociationAttribute : Attribute
#pragma warning restore CS3015 // Type has no accessible constructors which use only CLS-compliant types
    {
        /// <summary>
        /// Full form of constructor
        /// </summary>
        /// <param name="name">The name of the association. For bi-directional associations,
        /// the name must be the same on both sides of the association</param>
        /// <param name="thisKey">List of the property names of the key values on this side of the association</param>
        /// <param name="otherKey">List of the property names of the key values on the other side of the association</param>
        public EntityAssociationAttribute(string name, string[] thisKey, string[] otherKey)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            ThisKeyMembers = thisKey ?? throw new ArgumentNullException(nameof(thisKey));
            OtherKeyMembers = otherKey ?? throw new ArgumentNullException(nameof(otherKey));

            if (name .Length == 0)
                throw new ArgumentException("Name cannot be empty", nameof(name));
            if (thisKey.Length == 0)
                throw new ArgumentException("ThisKey cannot be empty", nameof(thisKey));
            if (otherKey.Length == 0)
                throw new ArgumentException("OtherKey cannot be empty", nameof(otherKey));
        }

        /// <summary>
        /// Gets the name of the association. For bi-directional associations, the name must
        /// be the same on both sides of the association
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets or sets a value indicating whether this association member represents
        /// the foreign key side of an association
        /// </summary>
        public bool IsForeignKey { get; set; }

        /// <summary>
        /// Gets the collection of individual key members specified in the ThisKey string.
        /// </summary>
        public IReadOnlyCollection<string> ThisKeyMembers { get; }

        /// <summary>
        /// Gets the collection of individual key members specified in the OtherKey string.
        /// </summary>
        public IReadOnlyCollection<string> OtherKeyMembers { get; }

        /// <summary>
        /// Gets or sets the key value on this side of the association
        /// </summary>
        public string ThisKey => string.Join(",", ThisKeyMembers);
        
        /// <summary>
        /// <see langword="string"/> representation of the key value on the other side of the association
        /// </summary>
        public string OtherKey => string.Join(",", OtherKeyMembers);
    }
}
