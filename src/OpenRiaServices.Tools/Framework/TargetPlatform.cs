namespace OpenRiaServices.Tools
{
    /// <summary>
    /// Target platform
    /// </summary>
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
