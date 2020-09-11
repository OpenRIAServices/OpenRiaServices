using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRiaServices.Hosting;
using System.ComponentModel.DataAnnotations;

namespace ServerClassLib
{
    public partial class TestEntity
    {
        [Key]
        public string TheKey
        {
            get
            {
                return null;
            }
        }

        [CustomValidation(typeof(TestValidator), "IsValid")]
        [CustomValidation(typeof(TestValidatorServer), "IsValid")]
        public int TheValue 
        {
            get
            {
                return 0;
            }
            set
            {
                // nothing
            }
        }

        // This method exists only on server
        public string ServerMethod()
        {
            return null;
        }
    }
}
