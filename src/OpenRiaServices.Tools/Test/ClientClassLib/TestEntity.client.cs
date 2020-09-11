using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace ServerClassLib
{
    // This code exists only on the client
    public partial class TestEntity
    {
        public int ClientValue
        {
            get
            {
                return 0;
            }
            set
            {
            }
        }

        // This method exists only on client
        public string ClientMethod()
        {
            return null;
        }
    }
}
