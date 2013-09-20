using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Microsoft.VisualStudio.ServiceModel.DomainServices.Tools
{
    /// <summary>
    /// Data-only class used to share state across AppDomain
    /// boundaries between <see cref="BusinessLogicEntity"/> and
    /// <see cref="EntityViewModel"/>
    /// </summary>
    [Serializable]
    internal class EntityData : MarshalByRefObject
    {
        /// <summary>
        /// Gets or sets the user-visible name of the entity.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this entity
        /// should be included in the generated code.
        /// </summary>
        public bool IsIncluded { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether it is legal
        /// to include this entity in generated code.
        /// </summary>
        public bool CanBeIncluded { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity
        /// code gen should include CUD methods.
        /// </summary>
        public bool IsEditable { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether it is legal
        /// for this entity to be edited.
        /// </summary>
        public bool CanBeEdited { get; set; }

        /// <summary>
        /// Gets or sets the short name of this assembly
        /// (see <see cref="Assembly.GetName().Name"/>).
        /// It is used to determine whether a metadata
        /// partial class can be generated.
        /// </summary>
        public string AssemblyName { get; set; }
    }
}
