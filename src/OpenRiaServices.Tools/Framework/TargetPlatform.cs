using System;

namespace OpenRiaServices.Tools
{
    /// <summary>
    /// Target platform
    /// </summary>
    [Obsolete("This enum is not used and will be removed in a future version")]
    public enum TargetPlatform
    {
        /// <summary>
        /// Unknown platform
        /// </summary>
        Unknown,

        /// <summary>
        /// Target Silverlight 5
        /// </summary>
        Silverlight,

        /// <summary>
        /// Target PCL
        /// </summary>
        Portable,

        /// <summary>
        /// Targets the full .Net framework
        /// </summary>
        Desktop,

        /// <summary>
        /// A windows 8 app
        /// </summary>
        Win8,
    }
}
