Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations

Namespace TestDomainServices
    ''' <summary>
    ''' This custom validator can be used in testing to verify custom validation is
    ''' being called correctly. Call Monitor in your test to start tracking and be sure
    ''' to turn it off afterwards. After validation, all the calls can be accessed via
    ''' the ValidationCalls member.
    ''' </summary>
    Public Class DynamicTestValidator
        Shared _monitor As Boolean
        Shared _validationCalls As New List(Of ValidationContext)()
        Shared _validationResultsMap As New Dictionary(Of Object, ValidationResult)()

        Public Shared ReadOnly Property ForcedValidationResults As Dictionary(Of Object, ValidationResult)
            Get
                Return _validationResultsMap
            End Get
        End Property

        Public Shared ReadOnly Property ValidationCalls As List(Of ValidationContext)
            Get
                Return _validationCalls
            End Get
        End Property

        Public Shared Sub Monitor(ByVal monitor As Boolean)
            If Not monitor Then
                _validationCalls.Clear()
            End If

            _monitor = monitor
        End Sub

        Public Shared Sub Reset()
            ForcedValidationResults.Clear()
            Monitor(False)
        End Sub

        Public Shared Function Validate(ByVal o As Object, ByVal context As ValidationContext)
            If _monitor Then
                Dim copy As New ValidationContext(context.ObjectInstance, context, context.Items)
                copy.MemberName = context.MemberName
                copy.DisplayName = context.DisplayName
                ValidationCalls.Add(copy)
            End If

            If o <> Nothing Then
                Dim result As ValidationResult
                result = Nothing
                Dim msg As String
                msg = String.Empty

                If _validationResultsMap.TryGetValue(o, result) Then
                    msg += result.ErrorMessage + "-" + context.MemberName
                    Return New ValidationResult(msg, result.MemberNames)
                ElseIf _validationResultsMap.TryGetValue(o.GetType(), result) Then
                    msg += result.ErrorMessage + "-" + "TypeLevel"
                    Return New ValidationResult(msg, result.MemberNames)
                End If
            End If

            Return ValidationResult.Success
        End Function
    End Class
End Namespace