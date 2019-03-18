// Guids.cs
// MUST match guids.h

using System;

namespace OpenRiaServices.VisualStudio.Installer
{
    static class GuidList
    {
        public const string guidVisualStudio_MenuExtensionPkgString = "83014b5f-3f89-4eff-a7fa-b5ec62657247";
        public const string guidVisualStudio_MenuExtensionCmdSetString = "d1570eae-26b7-46ee-a73c-96e528dd094a";

        public static readonly Guid guidVisualStudio_MenuExtensionCmdSet = new Guid(guidVisualStudio_MenuExtensionCmdSetString);
    };
}