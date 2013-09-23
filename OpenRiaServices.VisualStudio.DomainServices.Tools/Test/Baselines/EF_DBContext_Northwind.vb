
Option Compare Binary
Option Infer On
Option Strict On
Option Explicit On

Imports DbContextModels.Northwind
Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.ComponentModel.DataAnnotations
Imports System.Data
Imports System.Data.Entity.Infrastructure
Imports System.Linq
Imports OpenRiaServices.DomainServices.EntityFramework
Imports OpenRiaServices.DomainServices.Hosting
Imports OpenRiaServices.DomainServices.Server

Namespace BizLogic.Test
    
    'Implements application logic using the DbCtxNorthwindEntities context.
    ' TODO: Add your application logic to these methods or in additional methods.
    ' TODO: Wire up authentication (Windows/ASP.NET Forms) and uncomment the following to disable anonymous access
    ' Also consider adding roles to restrict access as appropriate.
    '<RequiresAuthentication> _
    <EnableClientAccess()>  _
    Public Class EF_DBContext_Northwind
        Inherits DbDomainService(Of DbCtxNorthwindEntities)
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'Categories' query.
        Public Function GetCategories() As IQueryable(Of Category)
            Return Me.DbContext.Categories
        End Function
        
        Public Sub InsertCategory(ByVal category As Category)
            Dim entityEntry As DbEntityEntry(Of Category) = Me.DbContext.Entry(category)
            If ((entityEntry.State = EntityState.Detached)  _
                        = false) Then
                entityEntry.State = EntityState.Added
            Else
                Me.DbContext.Categories.Add(category)
            End If
        End Sub
        
        Public Sub UpdateCategory(ByVal currentCategory As Category)
            Me.DbContext.Categories.AttachAsModified(currentCategory, Me.ChangeSet.GetOriginal(currentCategory), Me.DbContext)
        End Sub
        
        Public Sub DeleteCategory(ByVal category As Category)
            Dim entityEntry As DbEntityEntry(Of Category) = Me.DbContext.Entry(category)
            If ((entityEntry.State = EntityState.Deleted)  _
                        = false) Then
                entityEntry.State = EntityState.Deleted
            Else
                Me.DbContext.Categories.Attach(category)
                Me.DbContext.Categories.Remove(category)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'Customers' query.
        Public Function GetCustomers() As IQueryable(Of Customer)
            Return Me.DbContext.Customers
        End Function
        
        Public Sub InsertCustomer(ByVal customer As Customer)
            Dim entityEntry As DbEntityEntry(Of Customer) = Me.DbContext.Entry(customer)
            If ((entityEntry.State = EntityState.Detached)  _
                        = false) Then
                entityEntry.State = EntityState.Added
            Else
                Me.DbContext.Customers.Add(customer)
            End If
        End Sub
        
        Public Sub UpdateCustomer(ByVal currentCustomer As Customer)
            Me.DbContext.Customers.AttachAsModified(currentCustomer, Me.ChangeSet.GetOriginal(currentCustomer), Me.DbContext)
        End Sub
        
        Public Sub DeleteCustomer(ByVal customer As Customer)
            Dim entityEntry As DbEntityEntry(Of Customer) = Me.DbContext.Entry(customer)
            If ((entityEntry.State = EntityState.Deleted)  _
                        = false) Then
                entityEntry.State = EntityState.Deleted
            Else
                Me.DbContext.Customers.Attach(customer)
                Me.DbContext.Customers.Remove(customer)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'CustomerDemographics' query.
        Public Function GetCustomerDemographics() As IQueryable(Of CustomerDemographic)
            Return Me.DbContext.CustomerDemographics
        End Function
        
        Public Sub InsertCustomerDemographic(ByVal customerDemographic As CustomerDemographic)
            Dim entityEntry As DbEntityEntry(Of CustomerDemographic) = Me.DbContext.Entry(customerDemographic)
            If ((entityEntry.State = EntityState.Detached)  _
                        = false) Then
                entityEntry.State = EntityState.Added
            Else
                Me.DbContext.CustomerDemographics.Add(customerDemographic)
            End If
        End Sub
        
        Public Sub UpdateCustomerDemographic(ByVal currentCustomerDemographic As CustomerDemographic)
            Me.DbContext.CustomerDemographics.AttachAsModified(currentCustomerDemographic, Me.ChangeSet.GetOriginal(currentCustomerDemographic), Me.DbContext)
        End Sub
        
        Public Sub DeleteCustomerDemographic(ByVal customerDemographic As CustomerDemographic)
            Dim entityEntry As DbEntityEntry(Of CustomerDemographic) = Me.DbContext.Entry(customerDemographic)
            If ((entityEntry.State = EntityState.Deleted)  _
                        = false) Then
                entityEntry.State = EntityState.Deleted
            Else
                Me.DbContext.CustomerDemographics.Attach(customerDemographic)
                Me.DbContext.CustomerDemographics.Remove(customerDemographic)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'Employees' query.
        Public Function GetEmployees() As IQueryable(Of Employee)
            Return Me.DbContext.Employees
        End Function
        
        Public Sub InsertEmployee(ByVal employee As Employee)
            Dim entityEntry As DbEntityEntry(Of Employee) = Me.DbContext.Entry(employee)
            If ((entityEntry.State = EntityState.Detached)  _
                        = false) Then
                entityEntry.State = EntityState.Added
            Else
                Me.DbContext.Employees.Add(employee)
            End If
        End Sub
        
        Public Sub UpdateEmployee(ByVal currentEmployee As Employee)
            Me.DbContext.Employees.AttachAsModified(currentEmployee, Me.ChangeSet.GetOriginal(currentEmployee), Me.DbContext)
        End Sub
        
        Public Sub DeleteEmployee(ByVal employee As Employee)
            Dim entityEntry As DbEntityEntry(Of Employee) = Me.DbContext.Entry(employee)
            If ((entityEntry.State = EntityState.Deleted)  _
                        = false) Then
                entityEntry.State = EntityState.Deleted
            Else
                Me.DbContext.Employees.Attach(employee)
                Me.DbContext.Employees.Remove(employee)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'Orders' query.
        Public Function GetOrders() As IQueryable(Of Order)
            Return Me.DbContext.Orders
        End Function
        
        Public Sub InsertOrder(ByVal order As Order)
            Dim entityEntry As DbEntityEntry(Of Order) = Me.DbContext.Entry(order)
            If ((entityEntry.State = EntityState.Detached)  _
                        = false) Then
                entityEntry.State = EntityState.Added
            Else
                Me.DbContext.Orders.Add(order)
            End If
        End Sub
        
        Public Sub UpdateOrder(ByVal currentOrder As Order)
            Me.DbContext.Orders.AttachAsModified(currentOrder, Me.ChangeSet.GetOriginal(currentOrder), Me.DbContext)
        End Sub
        
        Public Sub DeleteOrder(ByVal order As Order)
            Dim entityEntry As DbEntityEntry(Of Order) = Me.DbContext.Entry(order)
            If ((entityEntry.State = EntityState.Deleted)  _
                        = false) Then
                entityEntry.State = EntityState.Deleted
            Else
                Me.DbContext.Orders.Attach(order)
                Me.DbContext.Orders.Remove(order)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'Order_Details' query.
        Public Function GetOrder_Details() As IQueryable(Of Order_Detail)
            Return Me.DbContext.Order_Details
        End Function
        
        Public Sub InsertOrder_Detail(ByVal order_Detail As Order_Detail)
            Dim entityEntry As DbEntityEntry(Of Order_Detail) = Me.DbContext.Entry(order_Detail)
            If ((entityEntry.State = EntityState.Detached)  _
                        = false) Then
                entityEntry.State = EntityState.Added
            Else
                Me.DbContext.Order_Details.Add(order_Detail)
            End If
        End Sub
        
        Public Sub UpdateOrder_Detail(ByVal currentOrder_Detail As Order_Detail)
            Me.DbContext.Order_Details.AttachAsModified(currentOrder_Detail, Me.ChangeSet.GetOriginal(currentOrder_Detail), Me.DbContext)
        End Sub
        
        Public Sub DeleteOrder_Detail(ByVal order_Detail As Order_Detail)
            Dim entityEntry As DbEntityEntry(Of Order_Detail) = Me.DbContext.Entry(order_Detail)
            If ((entityEntry.State = EntityState.Deleted)  _
                        = false) Then
                entityEntry.State = EntityState.Deleted
            Else
                Me.DbContext.Order_Details.Attach(order_Detail)
                Me.DbContext.Order_Details.Remove(order_Detail)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'Products' query.
        Public Function GetProducts() As IQueryable(Of Product)
            Return Me.DbContext.Products
        End Function
        
        Public Sub InsertProduct(ByVal product As Product)
            Dim entityEntry As DbEntityEntry(Of Product) = Me.DbContext.Entry(product)
            If ((entityEntry.State = EntityState.Detached)  _
                        = false) Then
                entityEntry.State = EntityState.Added
            Else
                Me.DbContext.Products.Add(product)
            End If
        End Sub
        
        Public Sub UpdateProduct(ByVal currentProduct As Product)
            Me.DbContext.Products.AttachAsModified(currentProduct, Me.ChangeSet.GetOriginal(currentProduct), Me.DbContext)
        End Sub
        
        Public Sub DeleteProduct(ByVal product As Product)
            Dim entityEntry As DbEntityEntry(Of Product) = Me.DbContext.Entry(product)
            If ((entityEntry.State = EntityState.Deleted)  _
                        = false) Then
                entityEntry.State = EntityState.Deleted
            Else
                Me.DbContext.Products.Attach(product)
                Me.DbContext.Products.Remove(product)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'Regions' query.
        Public Function GetRegions() As IQueryable(Of Region)
            Return Me.DbContext.Regions
        End Function
        
        Public Sub InsertRegion(ByVal region As Region)
            Dim entityEntry As DbEntityEntry(Of Region) = Me.DbContext.Entry(region)
            If ((entityEntry.State = EntityState.Detached)  _
                        = false) Then
                entityEntry.State = EntityState.Added
            Else
                Me.DbContext.Regions.Add(region)
            End If
        End Sub
        
        Public Sub UpdateRegion(ByVal currentRegion As Region)
            Me.DbContext.Regions.AttachAsModified(currentRegion, Me.ChangeSet.GetOriginal(currentRegion), Me.DbContext)
        End Sub
        
        Public Sub DeleteRegion(ByVal region As Region)
            Dim entityEntry As DbEntityEntry(Of Region) = Me.DbContext.Entry(region)
            If ((entityEntry.State = EntityState.Deleted)  _
                        = false) Then
                entityEntry.State = EntityState.Deleted
            Else
                Me.DbContext.Regions.Attach(region)
                Me.DbContext.Regions.Remove(region)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'Shippers' query.
        Public Function GetShippers() As IQueryable(Of Shipper)
            Return Me.DbContext.Shippers
        End Function
        
        Public Sub InsertShipper(ByVal shipper As Shipper)
            Dim entityEntry As DbEntityEntry(Of Shipper) = Me.DbContext.Entry(shipper)
            If ((entityEntry.State = EntityState.Detached)  _
                        = false) Then
                entityEntry.State = EntityState.Added
            Else
                Me.DbContext.Shippers.Add(shipper)
            End If
        End Sub
        
        Public Sub UpdateShipper(ByVal currentShipper As Shipper)
            Me.DbContext.Shippers.AttachAsModified(currentShipper, Me.ChangeSet.GetOriginal(currentShipper), Me.DbContext)
        End Sub
        
        Public Sub DeleteShipper(ByVal shipper As Shipper)
            Dim entityEntry As DbEntityEntry(Of Shipper) = Me.DbContext.Entry(shipper)
            If ((entityEntry.State = EntityState.Deleted)  _
                        = false) Then
                entityEntry.State = EntityState.Deleted
            Else
                Me.DbContext.Shippers.Attach(shipper)
                Me.DbContext.Shippers.Remove(shipper)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'Suppliers' query.
        Public Function GetSuppliers() As IQueryable(Of Supplier)
            Return Me.DbContext.Suppliers
        End Function
        
        Public Sub InsertSupplier(ByVal supplier As Supplier)
            Dim entityEntry As DbEntityEntry(Of Supplier) = Me.DbContext.Entry(supplier)
            If ((entityEntry.State = EntityState.Detached)  _
                        = false) Then
                entityEntry.State = EntityState.Added
            Else
                Me.DbContext.Suppliers.Add(supplier)
            End If
        End Sub
        
        Public Sub UpdateSupplier(ByVal currentSupplier As Supplier)
            Me.DbContext.Suppliers.AttachAsModified(currentSupplier, Me.ChangeSet.GetOriginal(currentSupplier), Me.DbContext)
        End Sub
        
        Public Sub DeleteSupplier(ByVal supplier As Supplier)
            Dim entityEntry As DbEntityEntry(Of Supplier) = Me.DbContext.Entry(supplier)
            If ((entityEntry.State = EntityState.Deleted)  _
                        = false) Then
                entityEntry.State = EntityState.Deleted
            Else
                Me.DbContext.Suppliers.Attach(supplier)
                Me.DbContext.Suppliers.Remove(supplier)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'Territories' query.
        Public Function GetTerritories() As IQueryable(Of Territory)
            Return Me.DbContext.Territories
        End Function
        
        Public Sub InsertTerritory(ByVal territory As Territory)
            Dim entityEntry As DbEntityEntry(Of Territory) = Me.DbContext.Entry(territory)
            If ((entityEntry.State = EntityState.Detached)  _
                        = false) Then
                entityEntry.State = EntityState.Added
            Else
                Me.DbContext.Territories.Add(territory)
            End If
        End Sub
        
        Public Sub UpdateTerritory(ByVal currentTerritory As Territory)
            Me.DbContext.Territories.AttachAsModified(currentTerritory, Me.ChangeSet.GetOriginal(currentTerritory), Me.DbContext)
        End Sub
        
        Public Sub DeleteTerritory(ByVal territory As Territory)
            Dim entityEntry As DbEntityEntry(Of Territory) = Me.DbContext.Entry(territory)
            If ((entityEntry.State = EntityState.Deleted)  _
                        = false) Then
                entityEntry.State = EntityState.Deleted
            Else
                Me.DbContext.Territories.Attach(territory)
                Me.DbContext.Territories.Remove(territory)
            End If
        End Sub
    End Class
End Namespace
