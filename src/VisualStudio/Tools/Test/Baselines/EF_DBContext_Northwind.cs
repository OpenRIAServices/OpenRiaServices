
namespace BizLogic.Test
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.Data;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    using System.Linq;
    using DbContextModels.Northwind;
    using OpenRiaServices.EntityFramework;
    using OpenRiaServices.Hosting;
    using OpenRiaServices.Server;
    
    
    // Implements application logic using the DbCtxNorthwindEntities context.
    // TODO: Add your application logic to these methods or in additional methods.
    // TODO: Wire up authentication (Windows/ASP.NET Forms) and uncomment the following to disable anonymous access
    // Also consider adding roles to restrict access as appropriate.
    // [RequiresAuthentication]
    [EnableClientAccess()]
    public class EF_DBContext_Northwind : DbDomainService<DbCtxNorthwindEntities>
    {
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'Categories' query.
        public IQueryable<Category> GetCategories()
        {
            return this.DbContext.Categories;
        }
        
        public void InsertCategory(Category category)
        {
            DbEntityEntry<Category> entityEntry = this.DbContext.Entry(category);
            if ((entityEntry.State != EntityState.Detached))
            {
                entityEntry.State = EntityState.Added;
            }
            else
            {
                this.DbContext.Categories.Add(category);
            }
        }
        
        public void UpdateCategory(Category currentCategory)
        {
            this.DbContext.Categories.AttachAsModified(currentCategory, this.ChangeSet.GetOriginal(currentCategory), this.DbContext);
        }
        
        public void DeleteCategory(Category category)
        {
            DbEntityEntry<Category> entityEntry = this.DbContext.Entry(category);
            if ((entityEntry.State != EntityState.Deleted))
            {
                entityEntry.State = EntityState.Deleted;
            }
            else
            {
                this.DbContext.Categories.Attach(category);
                this.DbContext.Categories.Remove(category);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'Customers' query.
        public IQueryable<Customer> GetCustomers()
        {
            return this.DbContext.Customers;
        }
        
        public void InsertCustomer(Customer customer)
        {
            DbEntityEntry<Customer> entityEntry = this.DbContext.Entry(customer);
            if ((entityEntry.State != EntityState.Detached))
            {
                entityEntry.State = EntityState.Added;
            }
            else
            {
                this.DbContext.Customers.Add(customer);
            }
        }
        
        public void UpdateCustomer(Customer currentCustomer)
        {
            this.DbContext.Customers.AttachAsModified(currentCustomer, this.ChangeSet.GetOriginal(currentCustomer), this.DbContext);
        }
        
        public void DeleteCustomer(Customer customer)
        {
            DbEntityEntry<Customer> entityEntry = this.DbContext.Entry(customer);
            if ((entityEntry.State != EntityState.Deleted))
            {
                entityEntry.State = EntityState.Deleted;
            }
            else
            {
                this.DbContext.Customers.Attach(customer);
                this.DbContext.Customers.Remove(customer);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'CustomerDemographics' query.
        public IQueryable<CustomerDemographic> GetCustomerDemographics()
        {
            return this.DbContext.CustomerDemographics;
        }
        
        public void InsertCustomerDemographic(CustomerDemographic customerDemographic)
        {
            DbEntityEntry<CustomerDemographic> entityEntry = this.DbContext.Entry(customerDemographic);
            if ((entityEntry.State != EntityState.Detached))
            {
                entityEntry.State = EntityState.Added;
            }
            else
            {
                this.DbContext.CustomerDemographics.Add(customerDemographic);
            }
        }
        
        public void UpdateCustomerDemographic(CustomerDemographic currentCustomerDemographic)
        {
            this.DbContext.CustomerDemographics.AttachAsModified(currentCustomerDemographic, this.ChangeSet.GetOriginal(currentCustomerDemographic), this.DbContext);
        }
        
        public void DeleteCustomerDemographic(CustomerDemographic customerDemographic)
        {
            DbEntityEntry<CustomerDemographic> entityEntry = this.DbContext.Entry(customerDemographic);
            if ((entityEntry.State != EntityState.Deleted))
            {
                entityEntry.State = EntityState.Deleted;
            }
            else
            {
                this.DbContext.CustomerDemographics.Attach(customerDemographic);
                this.DbContext.CustomerDemographics.Remove(customerDemographic);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'Employees' query.
        public IQueryable<Employee> GetEmployees()
        {
            return this.DbContext.Employees;
        }
        
        public void InsertEmployee(Employee employee)
        {
            DbEntityEntry<Employee> entityEntry = this.DbContext.Entry(employee);
            if ((entityEntry.State != EntityState.Detached))
            {
                entityEntry.State = EntityState.Added;
            }
            else
            {
                this.DbContext.Employees.Add(employee);
            }
        }
        
        public void UpdateEmployee(Employee currentEmployee)
        {
            this.DbContext.Employees.AttachAsModified(currentEmployee, this.ChangeSet.GetOriginal(currentEmployee), this.DbContext);
        }
        
        public void DeleteEmployee(Employee employee)
        {
            DbEntityEntry<Employee> entityEntry = this.DbContext.Entry(employee);
            if ((entityEntry.State != EntityState.Deleted))
            {
                entityEntry.State = EntityState.Deleted;
            }
            else
            {
                this.DbContext.Employees.Attach(employee);
                this.DbContext.Employees.Remove(employee);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'Orders' query.
        public IQueryable<Order> GetOrders()
        {
            return this.DbContext.Orders;
        }
        
        public void InsertOrder(Order order)
        {
            DbEntityEntry<Order> entityEntry = this.DbContext.Entry(order);
            if ((entityEntry.State != EntityState.Detached))
            {
                entityEntry.State = EntityState.Added;
            }
            else
            {
                this.DbContext.Orders.Add(order);
            }
        }
        
        public void UpdateOrder(Order currentOrder)
        {
            this.DbContext.Orders.AttachAsModified(currentOrder, this.ChangeSet.GetOriginal(currentOrder), this.DbContext);
        }
        
        public void DeleteOrder(Order order)
        {
            DbEntityEntry<Order> entityEntry = this.DbContext.Entry(order);
            if ((entityEntry.State != EntityState.Deleted))
            {
                entityEntry.State = EntityState.Deleted;
            }
            else
            {
                this.DbContext.Orders.Attach(order);
                this.DbContext.Orders.Remove(order);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'Order_Details' query.
        public IQueryable<Order_Detail> GetOrder_Details()
        {
            return this.DbContext.Order_Details;
        }
        
        public void InsertOrder_Detail(Order_Detail order_Detail)
        {
            DbEntityEntry<Order_Detail> entityEntry = this.DbContext.Entry(order_Detail);
            if ((entityEntry.State != EntityState.Detached))
            {
                entityEntry.State = EntityState.Added;
            }
            else
            {
                this.DbContext.Order_Details.Add(order_Detail);
            }
        }
        
        public void UpdateOrder_Detail(Order_Detail currentOrder_Detail)
        {
            this.DbContext.Order_Details.AttachAsModified(currentOrder_Detail, this.ChangeSet.GetOriginal(currentOrder_Detail), this.DbContext);
        }
        
        public void DeleteOrder_Detail(Order_Detail order_Detail)
        {
            DbEntityEntry<Order_Detail> entityEntry = this.DbContext.Entry(order_Detail);
            if ((entityEntry.State != EntityState.Deleted))
            {
                entityEntry.State = EntityState.Deleted;
            }
            else
            {
                this.DbContext.Order_Details.Attach(order_Detail);
                this.DbContext.Order_Details.Remove(order_Detail);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'Products' query.
        public IQueryable<Product> GetProducts()
        {
            return this.DbContext.Products;
        }
        
        public void InsertProduct(Product product)
        {
            DbEntityEntry<Product> entityEntry = this.DbContext.Entry(product);
            if ((entityEntry.State != EntityState.Detached))
            {
                entityEntry.State = EntityState.Added;
            }
            else
            {
                this.DbContext.Products.Add(product);
            }
        }
        
        public void UpdateProduct(Product currentProduct)
        {
            this.DbContext.Products.AttachAsModified(currentProduct, this.ChangeSet.GetOriginal(currentProduct), this.DbContext);
        }
        
        public void DeleteProduct(Product product)
        {
            DbEntityEntry<Product> entityEntry = this.DbContext.Entry(product);
            if ((entityEntry.State != EntityState.Deleted))
            {
                entityEntry.State = EntityState.Deleted;
            }
            else
            {
                this.DbContext.Products.Attach(product);
                this.DbContext.Products.Remove(product);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'Regions' query.
        public IQueryable<Region> GetRegions()
        {
            return this.DbContext.Regions;
        }
        
        public void InsertRegion(Region region)
        {
            DbEntityEntry<Region> entityEntry = this.DbContext.Entry(region);
            if ((entityEntry.State != EntityState.Detached))
            {
                entityEntry.State = EntityState.Added;
            }
            else
            {
                this.DbContext.Regions.Add(region);
            }
        }
        
        public void UpdateRegion(Region currentRegion)
        {
            this.DbContext.Regions.AttachAsModified(currentRegion, this.ChangeSet.GetOriginal(currentRegion), this.DbContext);
        }
        
        public void DeleteRegion(Region region)
        {
            DbEntityEntry<Region> entityEntry = this.DbContext.Entry(region);
            if ((entityEntry.State != EntityState.Deleted))
            {
                entityEntry.State = EntityState.Deleted;
            }
            else
            {
                this.DbContext.Regions.Attach(region);
                this.DbContext.Regions.Remove(region);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'Shippers' query.
        public IQueryable<Shipper> GetShippers()
        {
            return this.DbContext.Shippers;
        }
        
        public void InsertShipper(Shipper shipper)
        {
            DbEntityEntry<Shipper> entityEntry = this.DbContext.Entry(shipper);
            if ((entityEntry.State != EntityState.Detached))
            {
                entityEntry.State = EntityState.Added;
            }
            else
            {
                this.DbContext.Shippers.Add(shipper);
            }
        }
        
        public void UpdateShipper(Shipper currentShipper)
        {
            this.DbContext.Shippers.AttachAsModified(currentShipper, this.ChangeSet.GetOriginal(currentShipper), this.DbContext);
        }
        
        public void DeleteShipper(Shipper shipper)
        {
            DbEntityEntry<Shipper> entityEntry = this.DbContext.Entry(shipper);
            if ((entityEntry.State != EntityState.Deleted))
            {
                entityEntry.State = EntityState.Deleted;
            }
            else
            {
                this.DbContext.Shippers.Attach(shipper);
                this.DbContext.Shippers.Remove(shipper);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'Suppliers' query.
        public IQueryable<Supplier> GetSuppliers()
        {
            return this.DbContext.Suppliers;
        }
        
        public void InsertSupplier(Supplier supplier)
        {
            DbEntityEntry<Supplier> entityEntry = this.DbContext.Entry(supplier);
            if ((entityEntry.State != EntityState.Detached))
            {
                entityEntry.State = EntityState.Added;
            }
            else
            {
                this.DbContext.Suppliers.Add(supplier);
            }
        }
        
        public void UpdateSupplier(Supplier currentSupplier)
        {
            this.DbContext.Suppliers.AttachAsModified(currentSupplier, this.ChangeSet.GetOriginal(currentSupplier), this.DbContext);
        }
        
        public void DeleteSupplier(Supplier supplier)
        {
            DbEntityEntry<Supplier> entityEntry = this.DbContext.Entry(supplier);
            if ((entityEntry.State != EntityState.Deleted))
            {
                entityEntry.State = EntityState.Deleted;
            }
            else
            {
                this.DbContext.Suppliers.Attach(supplier);
                this.DbContext.Suppliers.Remove(supplier);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'Territories' query.
        public IQueryable<Territory> GetTerritories()
        {
            return this.DbContext.Territories;
        }
        
        public void InsertTerritory(Territory territory)
        {
            DbEntityEntry<Territory> entityEntry = this.DbContext.Entry(territory);
            if ((entityEntry.State != EntityState.Detached))
            {
                entityEntry.State = EntityState.Added;
            }
            else
            {
                this.DbContext.Territories.Add(territory);
            }
        }
        
        public void UpdateTerritory(Territory currentTerritory)
        {
            this.DbContext.Territories.AttachAsModified(currentTerritory, this.ChangeSet.GetOriginal(currentTerritory), this.DbContext);
        }
        
        public void DeleteTerritory(Territory territory)
        {
            DbEntityEntry<Territory> entityEntry = this.DbContext.Entry(territory);
            if ((entityEntry.State != EntityState.Deleted))
            {
                entityEntry.State = EntityState.Deleted;
            }
            else
            {
                this.DbContext.Territories.Attach(territory);
                this.DbContext.Territories.Remove(territory);
            }
        }
    }
}
