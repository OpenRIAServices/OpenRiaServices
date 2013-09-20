using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServerClassLib
{
    // This code is shared between client and server via linked file
    public partial class TestEntity
    {
        public int ServerAndClientValue
        {
            get
            {
                return -1;
            }
            set
            {
            }
        }

        // This method exists on both server and client
        public string ServerAndClientMethod()
        {
            return null;
        }
    }
}
