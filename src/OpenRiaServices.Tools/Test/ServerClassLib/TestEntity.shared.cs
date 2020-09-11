using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServerClassLib
{
    // this code is shared by client and server via shared file
    public partial class TestEntity
    {
        public int TheSharedValue
        {
            get
            {
                return 1;
            }
            set
            {
                ;
            }
        }

        // This property is here to test how the code generator
        // deals with properties for which there is no PDB info.
        // Automatic properties have no sequence points in the PDB file.
        public string AutomaticProperty { get; set; }
    }
}
