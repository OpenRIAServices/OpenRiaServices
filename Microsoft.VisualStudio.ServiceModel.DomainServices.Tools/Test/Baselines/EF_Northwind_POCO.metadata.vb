
Option Compare Binary
Option Infer On
Option Strict On
Option Explicit On

Imports NorthwindPOCOModel
Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.ComponentModel.DataAnnotations
Imports System.Linq
Imports System.ServiceModel.DomainServices.Hosting
Imports System.ServiceModel.DomainServices.Server

Namespace NorthwindPOCOModelBuddy
    
    'The MetadataTypeAttribute identifies CategoryBuddyMetadata as the class
    ' that carries additional metadata for the CategoryBuddy class.
    <MetadataTypeAttribute(GetType(CategoryBuddy.CategoryBuddyMetadata))>  _
    Partial Public Class CategoryBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the Category class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class CategoryBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property CategoryID As Integer
            
            Public Property CategoryName As String
            
            Public Property Description As String
            
            Public Property Picture() As Byte
            
            Public Property Products As List(Of Product)
        End Class
    End Class
    
    'The MetadataTypeAttribute identifies ProductBuddyMetadata as the class
    ' that carries additional metadata for the ProductBuddy class.
    <MetadataTypeAttribute(GetType(ProductBuddy.ProductBuddyMetadata))>  _
    Partial Public Class ProductBuddy
        
        'This class allows you to attach custom attributes to properties
        ' of the Product class.
        '
        'For example, the following marks the Xyz property as a
        ' required property and specifies the format for valid values:
        '    <Required()>
        '    <RegularExpression("[A-Z][A-Za-z0-9]*")>
        '    <StringLength(32)>
        '    Public Property Xyz As String
        Friend NotInheritable Class ProductBuddyMetadata
            
            'Metadata classes are not meant to be instantiated.
            Private Sub New()
                MyBase.New
            End Sub
            
            Public Property Category As Category
            
            Public Property CategoryID As Nullable(Of Integer)
            
            Public Property Discontinued As Boolean
            
            Public Property ProductID As Integer
            
            Public Property ProductName As String
            
            Public Property QuantityPerUnit As String
            
            Public Property ReorderLevel As Nullable(Of Short)
            
            Public Property SupplierID As Nullable(Of Integer)
            
            Public Property UnitPrice As Nullable(Of Decimal)
            
            Public Property UnitsInStock As Nullable(Of Short)
            
            Public Property UnitsOnOrder As Nullable(Of Short)
        End Class
    End Class
End Namespace
