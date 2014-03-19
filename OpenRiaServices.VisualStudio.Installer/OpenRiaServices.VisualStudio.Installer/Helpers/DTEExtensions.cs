using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;

namespace OpenRiaServices.VisualStudio.Installer.Helpers
{
    /// <summary>
    /// 
    /// </summary>
    /// <from>NuGet</from>
    public static class DTEExtensions
    {
        public static Project GetActiveProject(this IVsMonitorSelection vsMonitorSelection)
        {
            return VsUtility.GetActiveProject(vsMonitorSelection);
        }
    }
}
