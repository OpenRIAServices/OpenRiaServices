Imports System
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.Linq
Imports System.Runtime.Serialization
Imports System.Text
Imports OpenRiaServices.Client
Imports System.Runtime.InteropServices
Imports System.Windows

Namespace Cities


    ' Shared resource class to demonstrate globalized strings moving across pipeline
    Public Class Cities_Resources
        Public ReadOnly Property CityCaption() As String
            Get
                Return "Name of City"
            End Get
        End Property

        Public ReadOnly Property CityName() As String
            Get
                Return "CityName"
            End Get
        End Property

        Public ReadOnly Property CityPrompt() As String
            Get
                Return "Enter the city name"
            End Get
        End Property

        Public ReadOnly Property CityHelpText() As String
            Get
                Return "This is the name of the city"
            End Get
        End Property

    End Class


    ' Custom validation class for zip codes.
    ' It ensures a zip code begins with a specific prefix
    ' The [Shared] attribute is required for code gen to trust it can duplicate it
    Public Class MustStartWithAttribute
        Inherits ValidationAttribute

        Private _prefix As Integer

        Public Sub New(ByVal prefix As Integer)
            MyBase.New()
            Me.Prefix = prefix
        End Sub

        Public Property Prefix() As Integer
            Get
                Return Me._prefix
            End Get
            Set(ByVal value As Integer)
                Me._prefix = value
            End Set
        End Property

        Protected Overrides Function IsValid(ByVal value As Object, ByVal context As ValidationContext) As ValidationResult
            If (value Is Nothing) Then
                Return ValidationResult.Success
            End If

            Dim valueAsString As String = value.ToString
            Dim prefixAsString As String = Me.Prefix.ToString

            If valueAsString.StartsWith(prefixAsString, StringComparison.Ordinal) Then
                Return ValidationResult.Success
            Else
                Return New ValidationResult(Me.FormatErrorMessage(context.MemberName))
            End If
        End Function

        Public Overrides Function FormatErrorMessage(ByVal name As String) As String
            Return (name & (" must start with the prefix " & Me.Prefix))
        End Function

    End Class

    ' Class we can use inside a [CustomValidation] attribute to validate state names
    Public Class StateNameValidator

        Public Shared Function IsStateNameValid(ByVal stateNameObject As Object, ByVal context As ValidationContext, <Out()> ByRef validationResult As ValidationResult) As Boolean
            validationResult = Nothing
            Dim stateName As String = CType(stateNameObject, String)
            If (stateName Is Nothing) Then
                Return True
            End If
            Dim result As Boolean = (stateName.Length > 1)
            If Not result Then
                validationResult = New ValidationResult("The value for {0} must have exactly 2 letters")
            End If
            Return result
        End Function
    End Class

    ' Class we can use inside a [CustomValidation] attribute to validate state names
    Public Class CountiesValidator

        Public Shared Function AreCountiesValid(ByVal counties As List(Of County), ByVal context As ValidationContext, <Out()> ByRef validationResult As ValidationResult) As Boolean
            validationResult = Nothing
            Dim result As Boolean = counties.Any(Function(c) c.Name = "Invalid")
            If Not result Then
                validationResult = New ValidationResult("The value must not contain invalid counties", {context.MemberName})
            End If
            Return result
        End Function
    End Class

    ' Class we can use inside a [CustomValidation] attribute to validate Zip class (cross-field validation)
    Public Class ZipValidator

        Public Shared Function IsZipValid(ByVal zipObject As Object, ByVal context As ValidationContext, <Out()> ByRef validationResult As ValidationResult) As Boolean
            validationResult = Nothing
            Dim zip As Zip = CType(zipObject, Zip)
            Dim result As Boolean = Not String.Equals(zip.StateName, zip.CityName, StringComparison.Ordinal)
            If Not result Then
                validationResult = New ValidationResult("Zip codes cannot have matching city and state names")
            End If
            Return result
        End Function
    End Class

    ' Class we can use inside a [CustomValidation] attribute to validate the City class (cross-field validation)
    Public Class CityPropertyValidator

        Public Shared Function IsCityValid(ByVal cityObject As Object, ByVal context As ValidationContext, <Out()> ByRef validationResult As ValidationResult) As Boolean
            validationResult = Nothing
            Dim city As City = CType(cityObject, City)
            If Not city Is Nothing And Not Validator.IsValidCity(city) Then
                validationResult = New ValidationResult(String.Format("Cannot set '{0}.{1}' to an Invalid City!", context.ObjectType, context.MemberName))
                Return False
            End If
            Return True
        End Function
    End Class

    Public Class ThrowExValidator

        Public Shared Function IsThrowExValid(ByVal zipObject As Object, ByVal context As ValidationContext, <Out()> ByRef validationResult As ValidationResult) As Boolean
            validationResult = Nothing
            Dim zip As Zip = CType(zipObject, Zip)
            If (zip.Code = 99999) Then
                If zip.GetType.Assembly.FullName.Contains("Client") Then
                    Return True
                End If
                validationResult = New ValidationResult("Server fails validation")
                Return False
            Else
                Return True
            End If
        End Function
    End Class

    ' Demonstrates shared business logic -- will be copied onto client
    Public Class Validator

        Public Shared Function IsValidCity(ByVal city As City) As Boolean
            Return ((Not (city.Name) Is Nothing) _
                        AndAlso (city.Name.Length > 0))
        End Function
    End Class

    Public Class City

        Public ReadOnly Property IsValid() As Boolean
            Get
                Return Me.Validate
            End Get
        End Property

        ' Demonstration of a shared method
        Public Function Validate() As Boolean
            Return Validator.IsValidCity(Me)
        End Function
    End Class

    Public Enum TimeZone
        Central
        Mountain
        Eastern
        Pacific
    End Enum

End Namespace
