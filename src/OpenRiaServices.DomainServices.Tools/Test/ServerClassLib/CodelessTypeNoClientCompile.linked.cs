using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace ServerClassLib
{
    // This type has no user code, which means the PDB
    // will have no sequence points for it.  We use it
    // to verify the code path where we cannot determine
    // whether this type is shared.
    // This type does not compile on the client, meaning
    // only the PDB can tell us whether it is shared
#if !SILVERLIGHT
    public class CodelessTypeNoClientCompile
    {
        public string AutoProp { get; set; }
    }
#endif
}
