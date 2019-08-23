Imports System.ComponentModel.DataAnnotations

Public Class GenericEntity

    Private _key As Integer

    <Key()> _
    Public Property Key() As Integer
        Get
            Return _key
        End Get
        Set(ByVal value As Integer)
            _key = value
        End Set
    End Property

    Private _title As String
    Public Property Title() As String
        Get
            Return _title
        End Get
        Set(ByVal value As String)
            _title = value
        End Set
    End Property

    Private _nullableInt As Nullable(Of Integer)
    Public Property NullableInt() As Nullable(Of Integer)
        Get
            Return _nullableInt
        End Get
        Set(ByVal value As Nullable(Of Integer))
            _nullableInt = value
        End Set
    End Property

End Class
