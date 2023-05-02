using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace OpenRiaServices.Tools.Test
{
    public class Resolver : PathAssemblyResolver
    {
        public Resolver(IEnumerable<string> assemblyPaths) : base(assemblyPaths)
        {

        }

        public override Assembly Resolve(MetadataLoadContext context, AssemblyName assemblyName)
        {
            var assemblies = context.GetAssemblies();
            var asm = assemblies.FirstOrDefault(a => a.FullName == assemblyName.FullName);
            if (asm != default) return asm;

            return base.Resolve(context, assemblyName);
        }
    }
}
