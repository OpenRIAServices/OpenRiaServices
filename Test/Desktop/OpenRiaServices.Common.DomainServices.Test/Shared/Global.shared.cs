// Used in codegen tests to test shared global types.

using System;
using System.ComponentModel.DataAnnotations;

public static class GlobalNamespaceTest_Validation
{
    public static ValidationResult Validate(string input)
    {
        return ValidationResult.Success;
    }
}

public class GlobalNamespaceTest_ValidationAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        return ValidationResult.Success;
    }
}

public class GlobalNamespaceTest_Attribute : Attribute
{
    public GlobalNamespaceTest_Enum EnumProperty { get; set; }
}

public enum GlobalNamespaceTest_Enum
{
    DefaultValue,
    NonDefaultValue,
}