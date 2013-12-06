using System;

namespace OpenRiaServices.VisualStudio.DomainServices.Tools
{
    public interface IBusinessLogicEntity
    {
        /// <summary>
        /// Gets or sets the data object used to share state across
        /// AppDomain boundaries with <see cref="EntityViewModel"/>.
        /// </summary>
        IEntityData EntityData { get; set; }

        /// <summary>
        /// Gets the BusinessLogicContext that owns this entity
        /// </summary>
        IBusinessLogicContext BusinessLogicContext { get; }

        /// <summary>
        /// Gets the user visible name of the entity
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the CLR type of the entity
        /// </summary>
        Type ClrType { get; }

        /// <summary>
        /// Gets the short name of the assembly to which this entity type belongs.
        /// </summary>
        string AssemblyName { get; }

        /// <summary>
        /// Gets and sets a value indicating whether this
        /// entity should be included in code generation.
        /// </summary>
        bool IsIncluded { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the code generator
        /// should generate CUD methods for this entity.
        /// </summary>
        bool IsEditable { get; set; }

        /// <summary>
        /// Gets a value indicating whether it is legal to include this entity
        /// </summary>
        bool CanBeIncluded { get; }

        /// <summary>
        /// Gets a value indicating whether it is legal for this entity to be edited.
        /// </summary>
        bool CanBeEdited { get; }
    }
}