namespace OpenRiaServices.VisualStudio.Installer
{
    /// <summary>
    /// Determine how ".shared" files should be handled by code generation
    /// </summary>
    public enum OpenRiaSharedFilesMode
    {
        /// <summary>
        /// Reference the server version of the file (same as "Add existing file as link")
        /// </summary>
        Link = 0,

        /// <summary>
        /// Copy all shared files and reference copy
        /// </summary>
        Copy = 1,
    }

}
