
Option Compare Binary
Option Infer On
Option Strict On
Option Explicit On

Imports DataTests.Northwind.LTS
Imports OpenRiaServices.LinqToSql
Imports OpenRiaServices.Server
Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.ComponentModel.DataAnnotations
Imports System.Data.Linq
Imports System.Linq

Namespace BizLogic.Test
    
    'Implements application logic using the NorthwindDataContext context.
    ' TODO: Add your application logic to these methods or in additional methods.
    ' TODO: Wire up authentication (Windows/ASP.NET Forms) and uncomment the following to disable anonymous access
    ' Also consider adding roles to restrict access as appropriate.
    '<RequiresAuthentication> _
    <EnableClientAccess()>  _
    Public Class LTS_Northwind
        Inherits LinqToSqlDomainService(Of NorthwindDataContext)
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetCategories() As IQueryable(Of Category)
            Return Me.DataContext.Categories
        End Function
        
        Public Sub InsertCategory(ByVal category As Category)
            Me.DataContext.Categories.InsertOnSubmit(category)
        End Sub
        
        Public Sub UpdateCategory(ByVal currentCategory As Category)
            Me.DataContext.Categories.Attach(currentCategory, Me.ChangeSet.GetOriginal(currentCategory))
        End Sub
        
        Public Sub DeleteCategory(ByVal category As Category)
            Me.DataContext.Categories.Attach(category)
            Me.DataContext.Categories.DeleteOnSubmit(category)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetCustomers() As IQueryable(Of Customer)
            Return Me.DataContext.Customers
        End Function
        
        Public Sub InsertCustomer(ByVal customer As Customer)
            Me.DataContext.Customers.InsertOnSubmit(customer)
        End Sub
        
        Public Sub UpdateCustomer(ByVal currentCustomer As Customer)
            Me.DataContext.Customers.Attach(currentCustomer, Me.ChangeSet.GetOriginal(currentCustomer))
        End Sub
        
        Public Sub DeleteCustomer(ByVal customer As Customer)
            Me.DataContext.Customers.Attach(customer)
            Me.DataContext.Customers.DeleteOnSubmit(customer)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetCustomerCustomerDemos() As IQueryable(Of CustomerCustomerDemo)
            Return Me.DataContext.CustomerCustomerDemos
        End Function
        
        Public Sub InsertCustomerCustomerDemo(ByVal customerCustomerDemo As CustomerCustomerDemo)
            Me.DataContext.CustomerCustomerDemos.InsertOnSubmit(customerCustomerDemo)
        End Sub
        
        Public Sub UpdateCustomerCustomerDemo(ByVal currentCustomerCustomerDemo As CustomerCustomerDemo)
            Me.DataContext.CustomerCustomerDemos.Attach(currentCustomerCustomerDemo, Me.ChangeSet.GetOriginal(currentCustomerCustomerDemo))
        End Sub
        
        Public Sub DeleteCustomerCustomerDemo(ByVal customerCustomerDemo As CustomerCustomerDemo)
            Me.DataContext.CustomerCustomerDemos.Attach(customerCustomerDemo)
            Me.DataContext.CustomerCustomerDemos.DeleteOnSubmit(customerCustomerDemo)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetCustomerDemographics() As IQueryable(Of CustomerDemographic)
            Return Me.DataContext.CustomerDemographics
        End Function
        
        Public Sub InsertCustomerDemographic(ByVal customerDemographic As CustomerDemographic)
            Me.DataContext.CustomerDemographics.InsertOnSubmit(customerDemographic)
        End Sub
        
        Public Sub UpdateCustomerDemographic(ByVal currentCustomerDemographic As CustomerDemographic)
            Me.DataContext.CustomerDemographics.Attach(currentCustomerDemographic, Me.ChangeSet.GetOriginal(currentCustomerDemographic))
        End Sub
        
        Public Sub DeleteCustomerDemographic(ByVal customerDemographic As CustomerDemographic)
            Me.DataContext.CustomerDemographics.Attach(customerDemographic)
            Me.DataContext.CustomerDemographics.DeleteOnSubmit(customerDemographic)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetEmployees() As IQueryable(Of Employee)
            Return Me.DataContext.Employees
        End Function
        
        Public Sub InsertEmployee(ByVal employee As Employee)
            Me.DataContext.Employees.InsertOnSubmit(employee)
        End Sub
        
        Public Sub UpdateEmployee(ByVal currentEmployee As Employee)
            Me.DataContext.Employees.Attach(currentEmployee, Me.ChangeSet.GetOriginal(currentEmployee))
        End Sub
        
        Public Sub DeleteEmployee(ByVal employee As Employee)
            Me.DataContext.Employees.Attach(employee)
            Me.DataContext.Employees.DeleteOnSubmit(employee)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetEmployeeTerritories() As IQueryable(Of EmployeeTerritory)
            Return Me.DataContext.EmployeeTerritories
        End Function
        
        Public Sub InsertEmployeeTerritory(ByVal employeeTerritory As EmployeeTerritory)
            Me.DataContext.EmployeeTerritories.InsertOnSubmit(employeeTerritory)
        End Sub
        
        Public Sub UpdateEmployeeTerritory(ByVal currentEmployeeTerritory As EmployeeTerritory)
            Me.DataContext.EmployeeTerritories.Attach(currentEmployeeTerritory, Me.ChangeSet.GetOriginal(currentEmployeeTerritory))
        End Sub
        
        Public Sub DeleteEmployeeTerritory(ByVal employeeTerritory As EmployeeTerritory)
            Me.DataContext.EmployeeTerritories.Attach(employeeTerritory)
            Me.DataContext.EmployeeTerritories.DeleteOnSubmit(employeeTerritory)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetOrders() As IQueryable(Of Order)
            Return Me.DataContext.Orders
        End Function
        
        Public Sub InsertOrder(ByVal order As Order)
            Me.DataContext.Orders.InsertOnSubmit(order)
        End Sub
        
        Public Sub UpdateOrder(ByVal currentOrder As Order)
            Me.DataContext.Orders.Attach(currentOrder, Me.ChangeSet.GetOriginal(currentOrder))
        End Sub
        
        Public Sub DeleteOrder(ByVal order As Order)
            Me.DataContext.Orders.Attach(order)
            Me.DataContext.Orders.DeleteOnSubmit(order)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetOrder_Details() As IQueryable(Of Order_Detail)
            Return Me.DataContext.Order_Details
        End Function
        
        Public Sub InsertOrder_Detail(ByVal order_Detail As Order_Detail)
            Me.DataContext.Order_Details.InsertOnSubmit(order_Detail)
        End Sub
        
        Public Sub UpdateOrder_Detail(ByVal currentOrder_Detail As Order_Detail)
            Me.DataContext.Order_Details.Attach(currentOrder_Detail, Me.ChangeSet.GetOriginal(currentOrder_Detail))
        End Sub
        
        Public Sub DeleteOrder_Detail(ByVal order_Detail As Order_Detail)
            Me.DataContext.Order_Details.Attach(order_Detail)
            Me.DataContext.Order_Details.DeleteOnSubmit(order_Detail)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetProducts() As IQueryable(Of Product)
            Return Me.DataContext.Products
        End Function
        
        Public Sub InsertProduct(ByVal product As Product)
            Me.DataContext.Products.InsertOnSubmit(product)
        End Sub
        
        Public Sub UpdateProduct(ByVal currentProduct As Product)
            Me.DataContext.Products.Attach(currentProduct, Me.ChangeSet.GetOriginal(currentProduct))
        End Sub
        
        Public Sub DeleteProduct(ByVal product As Product)
            Me.DataContext.Products.Attach(product)
            Me.DataContext.Products.DeleteOnSubmit(product)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetRegions() As IQueryable(Of Region)
            Return Me.DataContext.Regions
        End Function
        
        Public Sub InsertRegion(ByVal region As Region)
            Me.DataContext.Regions.InsertOnSubmit(region)
        End Sub
        
        Public Sub UpdateRegion(ByVal currentRegion As Region)
            Me.DataContext.Regions.Attach(currentRegion, Me.ChangeSet.GetOriginal(currentRegion))
        End Sub
        
        Public Sub DeleteRegion(ByVal region As Region)
            Me.DataContext.Regions.Attach(region)
            Me.DataContext.Regions.DeleteOnSubmit(region)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetShippers() As IQueryable(Of Shipper)
            Return Me.DataContext.Shippers
        End Function
        
        Public Sub InsertShipper(ByVal shipper As Shipper)
            Me.DataContext.Shippers.InsertOnSubmit(shipper)
        End Sub
        
        Public Sub UpdateShipper(ByVal currentShipper As Shipper)
            Me.DataContext.Shippers.Attach(currentShipper, Me.ChangeSet.GetOriginal(currentShipper))
        End Sub
        
        Public Sub DeleteShipper(ByVal shipper As Shipper)
            Me.DataContext.Shippers.Attach(shipper)
            Me.DataContext.Shippers.DeleteOnSubmit(shipper)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetSuppliers() As IQueryable(Of Supplier)
            Return Me.DataContext.Suppliers
        End Function
        
        Public Sub InsertSupplier(ByVal supplier As Supplier)
            Me.DataContext.Suppliers.InsertOnSubmit(supplier)
        End Sub
        
        Public Sub UpdateSupplier(ByVal currentSupplier As Supplier)
            Me.DataContext.Suppliers.Attach(currentSupplier, Me.ChangeSet.GetOriginal(currentSupplier))
        End Sub
        
        Public Sub DeleteSupplier(ByVal supplier As Supplier)
            Me.DataContext.Suppliers.Attach(supplier)
            Me.DataContext.Suppliers.DeleteOnSubmit(supplier)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetTerritories() As IQueryable(Of Territory)
            Return Me.DataContext.Territories
        End Function
        
        Public Sub InsertTerritory(ByVal territory As Territory)
            Me.DataContext.Territories.InsertOnSubmit(territory)
        End Sub
        
        Public Sub UpdateTerritory(ByVal currentTerritory As Territory)
            Me.DataContext.Territories.Attach(currentTerritory, Me.ChangeSet.GetOriginal(currentTerritory))
        End Sub
        
        Public Sub DeleteTerritory(ByVal territory As Territory)
            Me.DataContext.Territories.Attach(territory)
            Me.DataContext.Territories.DeleteOnSubmit(territory)
        End Sub
    End Class
End Namespace
