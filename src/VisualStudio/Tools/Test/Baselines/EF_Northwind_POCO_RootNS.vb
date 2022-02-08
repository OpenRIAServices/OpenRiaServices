
Option Compare Binary
Option Infer On
Option Strict On
Option Explicit On

Imports NorthwindPOCOModel
Imports OpenRiaServices.EntityFramework
Imports OpenRiaServices.Server
Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.ComponentModel.DataAnnotations
Imports System.Data.Entity
Imports System.Data.Entity.Core.Objects
Imports System.Linq


'Implements application logic using the NorthwindEntities context.
' TODO: Add your application logic to these methods or in additional methods.
' TODO: Wire up authentication (Windows/ASP.NET Forms) and uncomment the following to disable anonymous access
' Also consider adding roles to restrict access as appropriate.
'<RequiresAuthentication> _
<EnableClientAccess()>  _
Public Class EF_Northwind_POCO_RootNS
    Inherits LinqToEntitiesDomainService(Of NorthwindEntities)
    
    'TODO:
    ' Consider constraining the results of your query method.  If you need additional input you can
    ' add parameters to this method or create additional query methods with different names.
    'To support paging you will need to add ordering to the 'Categories' query.
    Public Function GetCategories() As IQueryable(Of Category)
        Return Me.ObjectContext.Categories
    End Function
    
    Public Sub InsertCategory(ByVal category As Category)
        If ((Me.GetEntityState(category) = EntityState.Detached)  _
                    = false) Then
            Me.ObjectContext.ObjectStateManager.ChangeObjectState(category, EntityState.Added)
        Else
            Me.ObjectContext.Categories.AddObject(category)
        End If
    End Sub
    
    Public Sub UpdateCategory(ByVal currentCategory As Category)
        Me.ObjectContext.Categories.AttachAsModified(currentCategory, Me.ChangeSet.GetOriginal(currentCategory))
    End Sub
    
    Public Sub DeleteCategory(ByVal category As Category)
        If ((Me.GetEntityState(category) = EntityState.Detached)  _
                    = false) Then
            Me.ObjectContext.ObjectStateManager.ChangeObjectState(category, EntityState.Deleted)
        Else
            Me.ObjectContext.Categories.Attach(category)
            Me.ObjectContext.Categories.DeleteObject(category)
        End If
    End Sub
    
    'TODO:
    ' Consider constraining the results of your query method.  If you need additional input you can
    ' add parameters to this method or create additional query methods with different names.
    'To support paging you will need to add ordering to the 'Products' query.
    Public Function GetProducts() As IQueryable(Of Product)
        Return Me.ObjectContext.Products
    End Function
    
    Public Sub InsertProduct(ByVal product As Product)
        If ((Me.GetEntityState(product) = EntityState.Detached)  _
                    = false) Then
            Me.ObjectContext.ObjectStateManager.ChangeObjectState(product, EntityState.Added)
        Else
            Me.ObjectContext.Products.AddObject(product)
        End If
    End Sub
    
    Public Sub UpdateProduct(ByVal currentProduct As Product)
        Me.ObjectContext.Products.AttachAsModified(currentProduct, Me.ChangeSet.GetOriginal(currentProduct))
    End Sub
    
    Public Sub DeleteProduct(ByVal product As Product)
        If ((Me.GetEntityState(product) = EntityState.Detached)  _
                    = false) Then
            Me.ObjectContext.ObjectStateManager.ChangeObjectState(product, EntityState.Deleted)
        Else
            Me.ObjectContext.Products.Attach(product)
            Me.ObjectContext.Products.DeleteObject(product)
        End If
    End Sub
    
        
        Private Function GetEntityState(ByVal entity As Object) As EntityState
            Dim stateEntry As ObjectStateEntry = Nothing
            If (Me.ObjectContext.ObjectStateManager.TryGetObjectStateEntry(entity, stateEntry) = false) Then
                Return EntityState.Detached
            End If
            Return stateEntry.State
        End Function

End Class
