using System;

namespace OpenRiaServices.DomainServices.Tools
{
    /// <summary>
    /// Enum type that describes how code elements are shared
    /// between two projects.
    /// </summary>
    /// <remarks>Shared code elements are those that are visible
    /// to both projects, either though shared source files
    /// or through assemblies referenced by both projects.
    /// </remarks>
    [Flags]
    public enum CodeMemberShareKind
    {
        /// <summary>
        /// Insufficient information is available to determine
        /// whether the code element is shared or not.
        /// </summary>
        /// <value>
        /// This value typically results when the common source files
        /// or assemblies cannot be determined or accessed.
        /// In most cases, it is safest to assume it means the
        /// same as <see cref="NotShared"/>.
        /// </value>
        Unknown = 0,

        /// <summary>
        /// The code element is not shared between the projects.
        /// </summary>
        NotShared = 1,

        /// <summary>
        /// The code element is shared between the projects
        /// through common source files.
        /// </summary>
        SharedBySource = 2,

        /// <summary>
        /// The code element is shared between the projects
        /// through the equivalent types in assemblies
        /// referenced by both projects.
        /// </summary>
        SharedByReference = 4,

        /// <summary>
        /// Bit mask to use to check for any kind of sharing
        /// exists, either by common source files or equivalent
        /// assemblies.
        /// </summary>
        Shared = SharedBySource | SharedByReference
    }
}
