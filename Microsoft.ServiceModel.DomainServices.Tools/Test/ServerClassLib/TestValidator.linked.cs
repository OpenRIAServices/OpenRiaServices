using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.ComponentModel.DataAnnotations;

namespace ServerClassLib
{
    public class TestValidator
    {
        // Tests shared ctor
        public TestValidator(string notUsed)
        {
        }

        public static ValidationResult IsValid(TestEntity testEntity, ValidationContext validationContext)
        {
            return ValidationResult.Success;
        }
    }
}
