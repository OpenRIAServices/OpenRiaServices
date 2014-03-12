// Guids.cs
// MUST match guids.h
using System;

namespace Company.VisualStudio_MenuExtension
{
    static class GuidList
    {
        public const string guidVisualStudio_MenuExtensionPkgString = "2e8f696d-9cdb-4fcb-8fbd-e9ebbc3c5479";
        public const string guidVisualStudio_MenuExtensionCmdSetString = "d1570eae-26b7-46ee-a73c-96e528dd094a";

        public static readonly Guid guidVisualStudio_MenuExtensionCmdSet = new Guid(guidVisualStudio_MenuExtensionCmdSetString);
    };
}