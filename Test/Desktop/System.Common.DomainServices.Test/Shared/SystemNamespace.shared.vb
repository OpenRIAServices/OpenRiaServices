' Used in codegen tests to test shared system namespace types.
Imports System.ComponentModel.DataAnnotations

Namespace System
    Public Enum SystemEnum
        SystemValue
    End Enum

    Public Class SystemNamespaceAttribute
        Inherits ValidationAttribute

        Protected Overrides Function IsValid(ByVal value As Object, ByVal validationContext As validationcontext) As ValidationResult
            Return ValidationResult.Success
        End Function
    End Class

    Namespace Subsystem
        Public Enum SubsystemEnum
            SubsystemValue
        End Enum

        Public Class SubsystemNamespaceAttribute
            Inherits ValidationAttribute

            Protected Overrides Function IsValid(ByVal value As Object, ByVal validationContext As ValidationContext) As ValidationResult
                Return ValidationResult.Success
            End Function
        End Class
    end namespace
End Namespace

Namespace SystemExtensions
    Public Enum SystemExtensionsEnum
        SystemExtensionsValue
    End Enum

    Public Class SystemExtensionsNamespaceAttribute
        Inherits ValidationAttribute

        Protected Overrides Function IsValid(ByVal value As Object, ByVal validationContext As validationcontext) As ValidationResult
            Return ValidationResult.Success
        End Function
    End Class
end namespace