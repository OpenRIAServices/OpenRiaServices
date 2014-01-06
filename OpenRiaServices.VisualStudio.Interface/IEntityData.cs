using System;
using System.Reflection;
using System.Runtime.Remoting;

namespace OpenRiaServices.VisualStudio.DomainServices.Tools
{
    public interface IEntityData
    {
        /// <summary>
        /// Gets or sets the user-visible name of the entity.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this entity
        /// should be included in the generated code.
        /// </summary>
        bool IsIncluded { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether it is legal
        /// to include this entity in generated code.
        /// </summary>
        bool CanBeIncluded { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity
        /// code gen should include CUD methods.
        /// </summary>
        bool IsEditable { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether it is legal
        /// for this entity to be edited.
        /// </summary>
        bool CanBeEdited { get; set; }

        /// <summary>
        /// Gets or sets the short name of this assembly
        /// (see <see cref="Assembly.GetName().Name"/>).
        /// It is used to determine whether a metadata
        /// partial class can be generated.
        /// </summary>
        string AssemblyName { get; set; }

        object GetLifetimeService();
        object InitializeLifetimeService();
        ObjRef CreateObjRef(Type requestedType);
    }
}