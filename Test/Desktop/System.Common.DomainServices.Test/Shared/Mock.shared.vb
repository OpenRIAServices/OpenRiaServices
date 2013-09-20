Imports System
Imports System.ComponentModel.DataAnnotations
Imports System.Runtime.Serialization

Namespace TestDomainServices
    <Flags()> _
    <DataContract()> _
    Public Enum TestEnum
        <EnumMember()> _
        Value0 = 0

        <EnumMember()> _
        Value1 = 1

        <EnumMember()> _
        Value2 = 2

        <EnumMember()> _
        Value3 = 4
    End Enum

    <AttributeUsage(AttributeTargets.All, AllowMultiple:=False)> _
    Public Class MockAttributeAllowOnce
        Inherits Attribute
        Dim _value As String

        Public Sub New(ByVal value As String)
            Me.Value = value
        End Sub

        Public Property Value() As String
            Get
                Return Me._value
            End Get
            Set(ByVal value As String)
                Me._value = value
            End Set
        End Property
    End Class

    <AttributeUsage(AttributeTargets.All, AllowMultiple:=False)> _
    Public Class MockAttributeAllowOnce_AppliedToInterfaceOnly
        Inherits Attribute
        Dim _value As String

        Public Sub New(ByVal value As String)
            Me.Value = value
        End Sub

        Public Property Value() As String
            Get
                Return Me._value
            End Get
            Set(ByVal value As String)
                Me._value = value
            End Set
        End Property
    End Class

    <AttributeUsage(AttributeTargets.Interface, AllowMultiple:=False)> _
    Public Class MockAttributeAllowOnce_InterfaceOnly
        Inherits Attribute
        Dim _value As String

        Public Sub New(ByVal value As String)
            Me.Value = value
        End Sub

        Public Property Value() As String
            Get
                Return Me._value
            End Get
            Set(ByVal value As String)
                Me._value = value
            End Set
        End Property
    End Class

    <AttributeUsage(AttributeTargets.All, AllowMultiple:=True)> _
    Public Class MockAttributeAllowMultiple
        Inherits Attribute
        Dim _value As String

        Public Sub New(ByVal value As String)
            Me.Value = value
        End Sub

        Public Property Value() As String
            Get
                Return Me._value
            End Get
            Set(ByVal value As String)
                Me._value = value
            End Set
        End Property
    End Class

    <AttributeUsage(AttributeTargets.Interface, AllowMultiple:=True)> _
    Public Class MockAttributeAllowMultiple_InterfaceOnly
        Inherits Attribute
        Dim _value As String

        Public Sub New(ByVal value As String)
            Me.Value = value
        End Sub

        Public Property Value() As String
            Get
                Return Me._value
            End Get
            Set(ByVal value As String)
                Me._value = value
            End Set
        End Property
    End Class
End Namespace

' Used to verify that the code-generator properly generates code for attributes defined in unrelated namespaces.
Namespace CustomNamespace
    <AttributeUsage(AttributeTargets.All)> _
    Public Class CustomAttribute
        Inherits Attribute
    End Class
End Namespace