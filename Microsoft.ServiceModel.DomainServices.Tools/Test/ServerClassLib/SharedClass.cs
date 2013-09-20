#if !SILVERLIGHT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace ServerClassLib
{
    /// <summary>
    /// This class will compile only in the server project.
    /// This allows us to test for file-level sharing independent
    /// of assembly level type sharing
    /// </summary>
    public class SharedClass
    {
        public void SharedMethod() { }
    }
}

#endif
