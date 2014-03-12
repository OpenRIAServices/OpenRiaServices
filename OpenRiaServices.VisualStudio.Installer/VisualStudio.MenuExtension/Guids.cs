// Guids.cs
// MUST match guids.h
using System;

namespace Company.VisualStudio_MenuExtension
{
    static class GuidList
    {
        public const string guidVisualStudio_MenuExtensionPkgString = "5a3e8482-06df-4524-b6bf-ad9b761b2c66";
        public const string guidVisualStudio_MenuExtensionCmdSetString = "ad424aab-90b6-4197-9b79-f36b968f8618";

        public static readonly Guid guidVisualStudio_MenuExtensionCmdSet = new Guid(guidVisualStudio_MenuExtensionCmdSetString);
    };
}