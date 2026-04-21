using System;
using System.Linq;

#nullable enable

namespace OpenRiaServices.Client
{
    /// <summary>
    /// The result of a sucessfully completed submit operation
    /// </summary>
    public class SubmitResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SubmitResult"/> class.
        /// </summary>
        /// <param name="changeSet">The changeset which was submitted.</param>
        public SubmitResult(EntityChangeSet changeSet)
        {
            ArgumentNullException.ThrowIfNull(changeSet);

            ChangeSet = changeSet;
        }

        /// <summary>
        /// Gets the changeset which was submitted.
        /// </summary>
        /// <value>
        /// The changeset which was submitted.
        /// </value>
        public EntityChangeSet ChangeSet { get; }
    }
}
