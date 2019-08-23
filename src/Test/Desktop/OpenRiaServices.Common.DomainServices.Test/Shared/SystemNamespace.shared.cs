// Used in codegen tests to test shared system namespace types.
using System.ComponentModel.DataAnnotations;

namespace System
{
    public enum SystemEnum
    {
        SystemValue
    }

    public class SystemNamespaceAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            return ValidationResult.Success;
        }
    }

    namespace Subsystem
    {
        public enum SubsystemEnum
        {
            SubsystemValue
        }

        public class SubsystemNamespaceAttribute : ValidationAttribute
        {
            protected override ValidationResult IsValid(object value, ValidationContext validationContext)
            {
                return ValidationResult.Success;
            }
        }
    }
}

namespace SystemExtensions
{
    public enum SystemExtensionsEnum
    {
        SystemExtensionsValue
    }

    public class SystemExtensionsNamespaceAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            return ValidationResult.Success;
        }
    }
}