using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;

namespace Company.VisualStudio_MenuExtension.Helpers
{
    /// <summary>
    /// 
    /// </summary>
    /// <from>NuGet</from>
    public static class ProjectExtensions
    {
        public static bool IsUnloaded(this Project project)
        {
            return VsConstants.UnloadedProjectTypeGuid.Equals(project.Kind, StringComparison.OrdinalIgnoreCase);
        }
    }
}
