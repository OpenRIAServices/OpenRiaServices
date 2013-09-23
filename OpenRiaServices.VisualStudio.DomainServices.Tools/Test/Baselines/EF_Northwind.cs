
namespace BizLogic.Test
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.Data;
    using System.Linq;
    using OpenRiaServices.DomainServices.EntityFramework;
    using OpenRiaServices.DomainServices.Hosting;
    using OpenRiaServices.DomainServices.Server;
    using NorthwindModel;
    
    
    // Implements application logic using the NorthwindEntities context.
    // TODO: Add your application logic to these methods or in additional methods.
    // TODO: Wire up authentication (Windows/ASP.NET Forms) and uncomment the following to disable anonymous access
    // Also consider adding roles to restrict access as appropriate.
    // [RequiresAuthentication]
    [EnableClientAccess()]
    public class EF_Northwind : LinqToEntitiesDomainService<NorthwindEntities>
    {
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'Categories' query.
        public IQueryable<Category> GetCategories()
        {
            return this.ObjectContext.Categories;
        }
        
        public void InsertCategory(Category category)
        {
            if ((category.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(category, EntityState.Added);
            }
            else
            {
                this.ObjectContext.Categories.AddObject(category);
            }
        }
        
        public void UpdateCategory(Category currentCategory)
        {
            this.ObjectContext.Categories.AttachAsModified(currentCategory, this.ChangeSet.GetOriginal(currentCategory));
        }
        
        public void DeleteCategory(Category category)
        {
            if ((category.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(category, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.Categories.Attach(category);
                this.ObjectContext.Categories.DeleteObject(category);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'Customers' query.
        public IQueryable<Customer> GetCustomers()
        {
            return this.ObjectContext.Customers;
        }
        
        public void InsertCustomer(Customer customer)
        {
            if ((customer.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(customer, EntityState.Added);
            }
            else
            {
                this.ObjectContext.Customers.AddObject(customer);
            }
        }
        
        public void UpdateCustomer(Customer currentCustomer)
        {
            this.ObjectContext.Customers.AttachAsModified(currentCustomer, this.ChangeSet.GetOriginal(currentCustomer));
        }
        
        public void DeleteCustomer(Customer customer)
        {
            if ((customer.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(customer, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.Customers.Attach(customer);
                this.ObjectContext.Customers.DeleteObject(customer);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'CustomerDemographics' query.
        public IQueryable<CustomerDemographic> GetCustomerDemographics()
        {
            return this.ObjectContext.CustomerDemographics;
        }
        
        public void InsertCustomerDemographic(CustomerDemographic customerDemographic)
        {
            if ((customerDemographic.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(customerDemographic, EntityState.Added);
            }
            else
            {
                this.ObjectContext.CustomerDemographics.AddObject(customerDemographic);
            }
        }
        
        public void UpdateCustomerDemographic(CustomerDemographic currentCustomerDemographic)
        {
            this.ObjectContext.CustomerDemographics.AttachAsModified(currentCustomerDemographic, this.ChangeSet.GetOriginal(currentCustomerDemographic));
        }
        
        public void DeleteCustomerDemographic(CustomerDemographic customerDemographic)
        {
            if ((customerDemographic.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(customerDemographic, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.CustomerDemographics.Attach(customerDemographic);
                this.ObjectContext.CustomerDemographics.DeleteObject(customerDemographic);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'Employees' query.
        public IQueryable<Employee> GetEmployees()
        {
            return this.ObjectContext.Employees;
        }
        
        public void InsertEmployee(Employee employee)
        {
            if ((employee.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(employee, EntityState.Added);
            }
            else
            {
                this.ObjectContext.Employees.AddObject(employee);
            }
        }
        
        public void UpdateEmployee(Employee currentEmployee)
        {
            this.ObjectContext.Employees.AttachAsModified(currentEmployee, this.ChangeSet.GetOriginal(currentEmployee));
        }
        
        public void DeleteEmployee(Employee employee)
        {
            if ((employee.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(employee, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.Employees.Attach(employee);
                this.ObjectContext.Employees.DeleteObject(employee);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'Orders' query.
        public IQueryable<Order> GetOrders()
        {
            return this.ObjectContext.Orders;
        }
        
        public void InsertOrder(Order order)
        {
            if ((order.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(order, EntityState.Added);
            }
            else
            {
                this.ObjectContext.Orders.AddObject(order);
            }
        }
        
        public void UpdateOrder(Order currentOrder)
        {
            this.ObjectContext.Orders.AttachAsModified(currentOrder, this.ChangeSet.GetOriginal(currentOrder));
        }
        
        public void DeleteOrder(Order order)
        {
            if ((order.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(order, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.Orders.Attach(order);
                this.ObjectContext.Orders.DeleteObject(order);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'Order_Details' query.
        public IQueryable<Order_Detail> GetOrder_Details()
        {
            return this.ObjectContext.Order_Details;
        }
        
        public void InsertOrder_Detail(Order_Detail order_Detail)
        {
            if ((order_Detail.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(order_Detail, EntityState.Added);
            }
            else
            {
                this.ObjectContext.Order_Details.AddObject(order_Detail);
            }
        }
        
        public void UpdateOrder_Detail(Order_Detail currentOrder_Detail)
        {
            this.ObjectContext.Order_Details.AttachAsModified(currentOrder_Detail, this.ChangeSet.GetOriginal(currentOrder_Detail));
        }
        
        public void DeleteOrder_Detail(Order_Detail order_Detail)
        {
            if ((order_Detail.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(order_Detail, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.Order_Details.Attach(order_Detail);
                this.ObjectContext.Order_Details.DeleteObject(order_Detail);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'Products' query.
        public IQueryable<Product> GetProducts()
        {
            return this.ObjectContext.Products;
        }
        
        public void InsertProduct(Product product)
        {
            if ((product.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(product, EntityState.Added);
            }
            else
            {
                this.ObjectContext.Products.AddObject(product);
            }
        }
        
        public void UpdateProduct(Product currentProduct)
        {
            this.ObjectContext.Products.AttachAsModified(currentProduct, this.ChangeSet.GetOriginal(currentProduct));
        }
        
        public void DeleteProduct(Product product)
        {
            if ((product.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(product, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.Products.Attach(product);
                this.ObjectContext.Products.DeleteObject(product);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'Regions' query.
        public IQueryable<Region> GetRegions()
        {
            return this.ObjectContext.Regions;
        }
        
        public void InsertRegion(Region region)
        {
            if ((region.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(region, EntityState.Added);
            }
            else
            {
                this.ObjectContext.Regions.AddObject(region);
            }
        }
        
        public void UpdateRegion(Region currentRegion)
        {
            this.ObjectContext.Regions.AttachAsModified(currentRegion, this.ChangeSet.GetOriginal(currentRegion));
        }
        
        public void DeleteRegion(Region region)
        {
            if ((region.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(region, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.Regions.Attach(region);
                this.ObjectContext.Regions.DeleteObject(region);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'Shippers' query.
        public IQueryable<Shipper> GetShippers()
        {
            return this.ObjectContext.Shippers;
        }
        
        public void InsertShipper(Shipper shipper)
        {
            if ((shipper.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(shipper, EntityState.Added);
            }
            else
            {
                this.ObjectContext.Shippers.AddObject(shipper);
            }
        }
        
        public void UpdateShipper(Shipper currentShipper)
        {
            this.ObjectContext.Shippers.AttachAsModified(currentShipper, this.ChangeSet.GetOriginal(currentShipper));
        }
        
        public void DeleteShipper(Shipper shipper)
        {
            if ((shipper.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(shipper, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.Shippers.Attach(shipper);
                this.ObjectContext.Shippers.DeleteObject(shipper);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'Suppliers' query.
        public IQueryable<Supplier> GetSuppliers()
        {
            return this.ObjectContext.Suppliers;
        }
        
        public void InsertSupplier(Supplier supplier)
        {
            if ((supplier.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(supplier, EntityState.Added);
            }
            else
            {
                this.ObjectContext.Suppliers.AddObject(supplier);
            }
        }
        
        public void UpdateSupplier(Supplier currentSupplier)
        {
            this.ObjectContext.Suppliers.AttachAsModified(currentSupplier, this.ChangeSet.GetOriginal(currentSupplier));
        }
        
        public void DeleteSupplier(Supplier supplier)
        {
            if ((supplier.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(supplier, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.Suppliers.Attach(supplier);
                this.ObjectContext.Suppliers.DeleteObject(supplier);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'Territories' query.
        public IQueryable<Territory> GetTerritories()
        {
            return this.ObjectContext.Territories;
        }
        
        public void InsertTerritory(Territory territory)
        {
            if ((territory.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(territory, EntityState.Added);
            }
            else
            {
                this.ObjectContext.Territories.AddObject(territory);
            }
        }
        
        public void UpdateTerritory(Territory currentTerritory)
        {
            this.ObjectContext.Territories.AttachAsModified(currentTerritory, this.ChangeSet.GetOriginal(currentTerritory));
        }
        
        public void DeleteTerritory(Territory territory)
        {
            if ((territory.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(territory, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.Territories.Attach(territory);
                this.ObjectContext.Territories.DeleteObject(territory);
            }
        }
    }
}
