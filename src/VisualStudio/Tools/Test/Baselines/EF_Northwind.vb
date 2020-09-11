
Option Compare Binary
Option Infer On
Option Strict On
Option Explicit On

Imports NorthwindModel
Imports OpenRiaServices.EntityFramework
Imports OpenRiaServices.Hosting
Imports OpenRiaServices.Server
Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.ComponentModel.DataAnnotations
Imports System.Data.Entity
Imports System.Linq

Namespace BizLogic.Test
    
    'Implements application logic using the NorthwindEntities context.
    ' TODO: Add your application logic to these methods or in additional methods.
    ' TODO: Wire up authentication (Windows/ASP.NET Forms) and uncomment the following to disable anonymous access
    ' Also consider adding roles to restrict access as appropriate.
    '<RequiresAuthentication> _
    <EnableClientAccess()>  _
    Public Class EF_Northwind
        Inherits LinqToEntitiesDomainService(Of NorthwindEntities)
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'Categories' query.
        Public Function GetCategories() As IQueryable(Of Category)
            Return Me.ObjectContext.Categories
        End Function
        
        Public Sub InsertCategory(ByVal category As Category)
            If ((category.EntityState = EntityState.Detached)  _
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
            If ((category.EntityState = EntityState.Detached)  _
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
        'To support paging you will need to add ordering to the 'Customers' query.
        Public Function GetCustomers() As IQueryable(Of Customer)
            Return Me.ObjectContext.Customers
        End Function
        
        Public Sub InsertCustomer(ByVal customer As Customer)
            If ((customer.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(customer, EntityState.Added)
            Else
                Me.ObjectContext.Customers.AddObject(customer)
            End If
        End Sub
        
        Public Sub UpdateCustomer(ByVal currentCustomer As Customer)
            Me.ObjectContext.Customers.AttachAsModified(currentCustomer, Me.ChangeSet.GetOriginal(currentCustomer))
        End Sub
        
        Public Sub DeleteCustomer(ByVal customer As Customer)
            If ((customer.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(customer, EntityState.Deleted)
            Else
                Me.ObjectContext.Customers.Attach(customer)
                Me.ObjectContext.Customers.DeleteObject(customer)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'CustomerDemographics' query.
        Public Function GetCustomerDemographics() As IQueryable(Of CustomerDemographic)
            Return Me.ObjectContext.CustomerDemographics
        End Function
        
        Public Sub InsertCustomerDemographic(ByVal customerDemographic As CustomerDemographic)
            If ((customerDemographic.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(customerDemographic, EntityState.Added)
            Else
                Me.ObjectContext.CustomerDemographics.AddObject(customerDemographic)
            End If
        End Sub
        
        Public Sub UpdateCustomerDemographic(ByVal currentCustomerDemographic As CustomerDemographic)
            Me.ObjectContext.CustomerDemographics.AttachAsModified(currentCustomerDemographic, Me.ChangeSet.GetOriginal(currentCustomerDemographic))
        End Sub
        
        Public Sub DeleteCustomerDemographic(ByVal customerDemographic As CustomerDemographic)
            If ((customerDemographic.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(customerDemographic, EntityState.Deleted)
            Else
                Me.ObjectContext.CustomerDemographics.Attach(customerDemographic)
                Me.ObjectContext.CustomerDemographics.DeleteObject(customerDemographic)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'Employees' query.
        Public Function GetEmployees() As IQueryable(Of Employee)
            Return Me.ObjectContext.Employees
        End Function
        
        Public Sub InsertEmployee(ByVal employee As Employee)
            If ((employee.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(employee, EntityState.Added)
            Else
                Me.ObjectContext.Employees.AddObject(employee)
            End If
        End Sub
        
        Public Sub UpdateEmployee(ByVal currentEmployee As Employee)
            Me.ObjectContext.Employees.AttachAsModified(currentEmployee, Me.ChangeSet.GetOriginal(currentEmployee))
        End Sub
        
        Public Sub DeleteEmployee(ByVal employee As Employee)
            If ((employee.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(employee, EntityState.Deleted)
            Else
                Me.ObjectContext.Employees.Attach(employee)
                Me.ObjectContext.Employees.DeleteObject(employee)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'Orders' query.
        Public Function GetOrders() As IQueryable(Of Order)
            Return Me.ObjectContext.Orders
        End Function
        
        Public Sub InsertOrder(ByVal order As Order)
            If ((order.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(order, EntityState.Added)
            Else
                Me.ObjectContext.Orders.AddObject(order)
            End If
        End Sub
        
        Public Sub UpdateOrder(ByVal currentOrder As Order)
            Me.ObjectContext.Orders.AttachAsModified(currentOrder, Me.ChangeSet.GetOriginal(currentOrder))
        End Sub
        
        Public Sub DeleteOrder(ByVal order As Order)
            If ((order.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(order, EntityState.Deleted)
            Else
                Me.ObjectContext.Orders.Attach(order)
                Me.ObjectContext.Orders.DeleteObject(order)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'Order_Details' query.
        Public Function GetOrder_Details() As IQueryable(Of Order_Detail)
            Return Me.ObjectContext.Order_Details
        End Function
        
        Public Sub InsertOrder_Detail(ByVal order_Detail As Order_Detail)
            If ((order_Detail.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(order_Detail, EntityState.Added)
            Else
                Me.ObjectContext.Order_Details.AddObject(order_Detail)
            End If
        End Sub
        
        Public Sub UpdateOrder_Detail(ByVal currentOrder_Detail As Order_Detail)
            Me.ObjectContext.Order_Details.AttachAsModified(currentOrder_Detail, Me.ChangeSet.GetOriginal(currentOrder_Detail))
        End Sub
        
        Public Sub DeleteOrder_Detail(ByVal order_Detail As Order_Detail)
            If ((order_Detail.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(order_Detail, EntityState.Deleted)
            Else
                Me.ObjectContext.Order_Details.Attach(order_Detail)
                Me.ObjectContext.Order_Details.DeleteObject(order_Detail)
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
            If ((product.EntityState = EntityState.Detached)  _
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
            If ((product.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(product, EntityState.Deleted)
            Else
                Me.ObjectContext.Products.Attach(product)
                Me.ObjectContext.Products.DeleteObject(product)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'Regions' query.
        Public Function GetRegions() As IQueryable(Of Region)
            Return Me.ObjectContext.Regions
        End Function
        
        Public Sub InsertRegion(ByVal region As Region)
            If ((region.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(region, EntityState.Added)
            Else
                Me.ObjectContext.Regions.AddObject(region)
            End If
        End Sub
        
        Public Sub UpdateRegion(ByVal currentRegion As Region)
            Me.ObjectContext.Regions.AttachAsModified(currentRegion, Me.ChangeSet.GetOriginal(currentRegion))
        End Sub
        
        Public Sub DeleteRegion(ByVal region As Region)
            If ((region.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(region, EntityState.Deleted)
            Else
                Me.ObjectContext.Regions.Attach(region)
                Me.ObjectContext.Regions.DeleteObject(region)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'Shippers' query.
        Public Function GetShippers() As IQueryable(Of Shipper)
            Return Me.ObjectContext.Shippers
        End Function
        
        Public Sub InsertShipper(ByVal shipper As Shipper)
            If ((shipper.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(shipper, EntityState.Added)
            Else
                Me.ObjectContext.Shippers.AddObject(shipper)
            End If
        End Sub
        
        Public Sub UpdateShipper(ByVal currentShipper As Shipper)
            Me.ObjectContext.Shippers.AttachAsModified(currentShipper, Me.ChangeSet.GetOriginal(currentShipper))
        End Sub
        
        Public Sub DeleteShipper(ByVal shipper As Shipper)
            If ((shipper.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(shipper, EntityState.Deleted)
            Else
                Me.ObjectContext.Shippers.Attach(shipper)
                Me.ObjectContext.Shippers.DeleteObject(shipper)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'Suppliers' query.
        Public Function GetSuppliers() As IQueryable(Of Supplier)
            Return Me.ObjectContext.Suppliers
        End Function
        
        Public Sub InsertSupplier(ByVal supplier As Supplier)
            If ((supplier.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(supplier, EntityState.Added)
            Else
                Me.ObjectContext.Suppliers.AddObject(supplier)
            End If
        End Sub
        
        Public Sub UpdateSupplier(ByVal currentSupplier As Supplier)
            Me.ObjectContext.Suppliers.AttachAsModified(currentSupplier, Me.ChangeSet.GetOriginal(currentSupplier))
        End Sub
        
        Public Sub DeleteSupplier(ByVal supplier As Supplier)
            If ((supplier.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(supplier, EntityState.Deleted)
            Else
                Me.ObjectContext.Suppliers.Attach(supplier)
                Me.ObjectContext.Suppliers.DeleteObject(supplier)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'Territories' query.
        Public Function GetTerritories() As IQueryable(Of Territory)
            Return Me.ObjectContext.Territories
        End Function
        
        Public Sub InsertTerritory(ByVal territory As Territory)
            If ((territory.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(territory, EntityState.Added)
            Else
                Me.ObjectContext.Territories.AddObject(territory)
            End If
        End Sub
        
        Public Sub UpdateTerritory(ByVal currentTerritory As Territory)
            Me.ObjectContext.Territories.AttachAsModified(currentTerritory, Me.ChangeSet.GetOriginal(currentTerritory))
        End Sub
        
        Public Sub DeleteTerritory(ByVal territory As Territory)
            If ((territory.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(territory, EntityState.Deleted)
            Else
                Me.ObjectContext.Territories.Attach(territory)
                Me.ObjectContext.Territories.DeleteObject(territory)
            End If
        End Sub
    End Class
End Namespace
