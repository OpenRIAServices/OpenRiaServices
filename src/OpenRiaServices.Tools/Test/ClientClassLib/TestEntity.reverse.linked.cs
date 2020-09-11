using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServerClassLib
{
    // This code is shared between client and server via linked file
    // but the link is from the server to the client project!
    public partial class TestEntity
    {
        public int ClientAndServerValue
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
        public string ClientAndServerMethod()
        {
            return null;
        }
    }
}
