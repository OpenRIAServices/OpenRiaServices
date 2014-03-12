using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;

namespace Company.VisualStudio_MenuExtension.Helpers
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
