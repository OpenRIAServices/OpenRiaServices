' Used in codegen tests to test shared global types.

Imports System
Imports System.ComponentModel.DataAnnotations

Public Class GlobalNamespaceTest_Validation
    Public Shared Function Validate(ByVal input As String) As ValidationResult
        Return ValidationResult.Success
    End Function
End Class

Public Class GlobalNamespaceTest_ValidationAttribute
    Inherits ValidationAttribute
    Protected Overrides Function IsValid(ByVal value As Object, ByVal validationContext As ValidationContext) As ValidationResult
        Return ValidationResult.Success
    End Function
End Class

Public Class GlobalNamespaceTest_Attribute
    Inherits Attribute
    Public Property EnumProperty As GlobalNamespaceTest_Enum
End Class


Public Enum GlobalNamespaceTest_Enum
    DefaultValue
    NonDefaultValue
End Enum