using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.ServiceModel;
using OpenRiaServices.Server;
using TestDomainServices.Testing;
using System.Configuration;
using OpenRiaServices.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using EFCoreModels.Northwind;

namespace TestDomainServices.EFCore
{
    [EnableClientAccess]
    public class Northwind : DbDomainServiceEFCore<EFCoreDbCtxNorthwindEntities>
    {
        #region Product methods
        public Product GetProductById(int id)
        {
            return this.DbContext.Products.SingleOrDefault(p => p.ProductID == id);
        }

        public IQueryable<Product> GetProducts()
        {
            return this.DbContext.Products.Include("Supplier").Include("Category");
        }

        public void InsertProduct(Product product)
        {
            EntityEntry<Product> entityEntry = this.DbContext.Entry(product);
            if (entityEntry.State != EntityState.Detached)
            {
                entityEntry.State = EntityState.Added;
            }
            else
            {
                this.DbContext.Products.Add(product);
            }
        }

        public void UpdateProduct(Product current)
        {
            this.DbContext.Products.AttachAsModified(current, this.ChangeSet.GetOriginal(current), this.DbContext);
        }

        public void DeleteProduct(Product product)
        {
            EntityEntry<Product> entityEntry = this.DbContext.Entry(product);
            if (entityEntry.State != EntityState.Detached)
            {
                entityEntry.State = EntityState.Deleted;
            }
            else
            {
                this.DbContext.Products.Attach(product);
                this.DbContext.Products.Remove(product);
            }
        }

        public void DiscontinueProduct(Product product)
        {
            // Don't allow discontinued products to be discontinued again.
            if (product.Discontinued)
            {
                throw new ValidationException("Discontinued products can't be discontinued again.");
            }
            EntityEntry entityEntry = this.DbContext.Entry(product);
            if (entityEntry.State == EntityState.Detached)
            {
                this.DbContext.Products.Attach(product);
            }

            product.Discontinued = true;

            if (entityEntry.State == EntityState.Unchanged)
            {
                // transition the entity to the modified state
                entityEntry.State = EntityState.Modified;
            }
            else if (entityEntry.State == EntityState.Modified)
            {
                entityEntry.State = EntityState.Modified;
            }
        }        

        protected override bool ResolveConflicts(IEnumerable<EntityEntry> conflicts)
        {
            // TODO - resolve conflicts
            foreach (EntityEntry entityEntry in conflicts)
            {
                var stateEntry = DbContext.Entry(entityEntry.Entity);
                if (entityEntry.State == EntityState.Detached) {
                    continue;
                }

                Type entityType = stateEntry.Entity.GetType();
                if (entityType == typeof(Product))
                {
                    if (!this.ResolveProductConflict(entityEntry))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool ResolveProductConflict(EntityEntry entryInConflict)
        {
            Product product = (Product)entryInConflict.Entity;
            switch (product.ResolveMethod)
            {
                case "ThrowValidationEx":
                    throw new ValidationException("testing");
                case "ThrowDomainServiceEx":
                    throw new DomainException("testing");
                case "MergeIntoCurrent":
                    return this.ResolveProductWithMerge(entryInConflict);
                case "KeepCurrent":
                    // Client Wins
                    //objectContext.Refresh(RefreshMode.ClientWins, product); // TODO
                    ResolveProductWithMerge(entryInConflict); // TODO: Is this correcy???
                    break;
                case "RefreshCurrent":
                    // Store wins
                    DbContext.Entry(product).Reload();
                    // objectContext.Refresh(RefreshMode.StoreWins, product); // TODO
                    break;
                case "ReturnTrueNoResolve":
                    return true;
                case "ReturnFalse":
                    return false;
                case "ReturnFalseWithResolve":
                    // Store Wins
                    DbContext.Entry(product).Reload();
                    // objectContext.Refresh(RefreshMode.StoreWins, product); // TODO
                    return false;
                case "":
                    ResolveProductWithMerge(entryInConflict); // TODO: Is this correcy???
                    // objectContext.Refresh(RefreshMode.ClientWins, product); // TODO
                    break;
                default:
                    {
                        throw new NotImplementedException(string.Format("ResolveMethod {0} is not defined", product.ResolveMethod));
                    }
            }

            return true;
        }

        private void RefreshClientWins(EntityEntry entry)
        {
            // Keep a collection of all modified properties and their values.
            PropertyValues currentValues = entry.CurrentValues;
            PropertyValues originalValues = entry.OriginalValues;
            PropertyValues dbValues = entry.GetDatabaseValues();
            if (dbValues == null) 
                return;

            // Reset all properties to database values, except modified values which are keept
            List<Tuple<string, object>> modifiedMembers = new List<Tuple<string, object>>();
            foreach (var property in currentValues.Properties)
            {
                object currValue = currentValues[property.Name];
                object origValue = originalValues[property.Name];
                object dbValue = dbValues[property.Name];

                originalValues[property.Name] = dbValue;
                if (object.Equals(currValue, origValue))
                {
                    currentValues[property.Name] = dbValue;
                }
            }
        }

        private bool ResolveProductWithMerge(EntityEntry entry)
        {
            // Keep a collection of all modified properties and their values.
            PropertyValues currentValues = entry.CurrentValues;
            PropertyValues originalValues = entry.OriginalValues;

            List<Tuple<string, object>> modifiedMembers = new List<Tuple<string, object>>();
            foreach (var property in currentValues.Properties)
            {
                object currValue = currentValues[property.Name];
                object origValue = originalValues[property.Name];
                if (!object.Equals(currValue, origValue))
                {
                    modifiedMembers.Add(new Tuple<string, object>(property.Name, currValue));
                }
            }

            // refresh from store to get the updated values - StoreWins
            entry.Reload();

            if (entry.State == EntityState.Detached)
            {
                // if the refresh results in the entity becoming
                // detached, it means the entity no longer exists
                // in the store
                return false;
            }

            // now play back changes
            currentValues = entry.CurrentValues;
            foreach (var modifiedMember in modifiedMembers)
            {
                currentValues[modifiedMember.Item1] = modifiedMember.Item2;
            }
            return true;
        }
        #endregion

        #region Order methods
        public IQueryable<Order> GetOrders()
        {
            return this.DbContext.Orders.Include("Order_Details").Include("Order_Details.Product");
        }
        
        public void InsertOrder(Order order)
        {
            EntityEntry<Order> entityEntry = this.DbContext.Entry(order);
            if (entityEntry.State != EntityState.Detached)
            {
                entityEntry.State = EntityState.Added;
            }
            else
            {
                this.DbContext.Orders.Add(order);
            }
        }

        public void UpdateOrder(Order current)
        {
            this.DbContext.Orders.AttachAsModified(current, this.ChangeSet.GetOriginal(current), this.DbContext);
        }

        public void DeleteOrder(Order order)
        {
            EntityEntry<Order> entityEntry = this.DbContext.Entry(order);
            if (entityEntry.State != EntityState.Detached)
            {
                entityEntry.State = EntityState.Deleted;
            }
            else
            {
                this.DbContext.Orders.Attach(order);
                this.DbContext.Orders.Remove(order);
            }
        }
        #endregion

        #region OrderDetail methods
        public IQueryable<Order_Detail> GetOrderDetails()
        {
            return this.DbContext.Order_Details.Include("Product");
        }

        public void InsertOrderDetail(Order_Detail detail)
        {
            EntityEntry<Order_Detail> entityEntry = this.DbContext.Entry(detail);
            if (entityEntry.State != EntityState.Detached)
            {
                entityEntry.State = EntityState.Added;
            }
            else
            {
                this.DbContext.Order_Details.Add(detail);
            }
        }

        public void UpdateOrderDetail(Order_Detail current)
        {
            this.DbContext.Order_Details.AttachAsModified(current, this.ChangeSet.GetOriginal(current), this.DbContext);
        }

        public void DeleteOrderDetail(Order_Detail detail)
        {
            EntityEntry<Order_Detail> entityEntry = this.DbContext.Entry(detail);
            if (entityEntry.State != EntityState.Detached)
            {
                entityEntry.State = EntityState.Deleted;
            }
            else
            {
                this.DbContext.Order_Details.Attach(detail);
                this.DbContext.Order_Details.Remove(detail);
            }
        }
        #endregion

        #region Customer methods
        public IQueryable<Customer> GetCustomers()
        {
            return this.DbContext.Customers;
        }

        public void InsertCustomer(Customer customer)
        {
            EntityEntry<Customer> entityEntry = this.DbContext.Entry(customer);
            if (entityEntry.State != EntityState.Detached)
            {
                entityEntry.State = EntityState.Added;
            }
            else
            {
                this.DbContext.Customers.Add(customer);
            }
        }

        public void UpdateCustomer(Customer current)
        {
            this.DbContext.Customers.AttachAsModified(current, this.ChangeSet.GetOriginal(current), this.DbContext);
        }

        public void DeleteCustomer(Customer customer)
        {
            EntityEntry<Customer> entityEntry = this.DbContext.Entry(customer);
            if (entityEntry.State != EntityState.Detached)
            {
                entityEntry.State = EntityState.Deleted;
            }
            else
            {
                this.DbContext.Customers.Attach(customer);
                this.DbContext.Customers.Remove(customer);
            }
        }
        #endregion

        #region Category methods
        public IQueryable<Category> GetCategories()
        {
            return this.DbContext.Categories;
        }

        public void InsertCategory(Category category)
        {
            EntityEntry<Category> entityEntry = this.DbContext.Entry(category);
            if (entityEntry.State != EntityState.Detached)
            {
                entityEntry.State = EntityState.Added;
            }
            else
            {
                this.DbContext.Categories.Add(category);
            }
        }

        public void UpdateCategory(Category current)
        {
            this.DbContext.Categories.AttachAsModified(current, this.ChangeSet.GetOriginal(current), this.DbContext);
        }

        public void DeleteCategory(Category category)
        {
            EntityEntry<Category> entityEntry = this.DbContext.Entry(category);
            if (entityEntry.State != EntityState.Detached)
            {
                entityEntry.State = EntityState.Deleted;
            }
            else
            {
                this.DbContext.Categories.Attach(category);
                this.DbContext.Categories.Remove(category);
            }
        }
        #endregion

        #region Region/Territory composition
        public Region GetRegionById(int id)
        {
            return this.GetRegions().Where(p => p.RegionID == id).FirstOrDefault();
        }

        public IQueryable<Region> GetRegions()
        {
            return this.DbContext.Regions.Include("Territories");
        }

        public void InsertRegion(Region region)
        {
            this.DbContext.Regions.Add(region);

            foreach (Territory territory in this.ChangeSet.GetAssociatedChanges(region, p => p.Territories, ChangeOperation.Insert))
            {
                this.DbContext.Territories.Add(territory);
            }
        }

        /// <summary>
        /// Update the region by processing all child modifications.
        /// <remarks>Note: The order of operations below is very important. Only by
        /// processing the related changes in this order will the update succeed.</remarks>
        /// </summary>
        /// <param name="region">The region to update.</param>
        public void UpdateRegion(Region region)
        {
            Region originalRegion = this.ChangeSet.GetOriginal(region);

            // Attach all unmodified entities
            foreach (Territory territory in this.ChangeSet.GetAssociatedChanges(region, p => p.Territories, ChangeOperation.None))
            {
                this.DbContext.Territories.Attach(territory);
            }
            if (originalRegion == null)
            {
                this.DbContext.Regions.Attach(region);
            }

            // Attach and apply changes to modified entities
            if (originalRegion != null)
            {
                this.DbContext.Regions.AttachAsModified(region, originalRegion, this.DbContext);
            }
            foreach (Territory territory in this.ChangeSet.GetAssociatedChanges(region, p => p.Territories, ChangeOperation.Update))
            {
                this.DbContext.Territories.AttachAsModified(territory, this.ChangeSet.GetOriginal(territory), this.DbContext);
            }

            // Add new entities
            foreach (Territory territory in this.ChangeSet.GetAssociatedChanges(region, p => p.Territories, ChangeOperation.Insert))
            {
                EntityEntry<Territory> entityEntry = this.DbContext.Entry(territory);
                if (entityEntry.State != EntityState.Detached)
                {
                    // need to change the object state if the entity was already eagerly added
                    entityEntry.State = EntityState.Added;
                }
                else
                {
                    this.DbContext.Territories.Add(territory);
                }
            }

            // Finally, process any deleted entites and relationships
            foreach (Territory territory in this.ChangeSet.GetAssociatedChanges(region, p => p.Territories, ChangeOperation.Delete))
            {
                Territory origTerrigory = this.ChangeSet.GetOriginal(territory);
                if (origTerrigory != null)
                {
                    this.DbContext.Territories.AttachAsModified(territory, origTerrigory, this.DbContext);
                }
                else
                {
                    this.DbContext.Territories.Attach(territory);
                }
                
                // need to remove any employee territory rows
                EntityEntry tEntityEntry = this.DbContext.Entry(territory);
                CollectionEntry employeesCollection = tEntityEntry.Collection(nameof(territory.Employees));
                employeesCollection.Load();
                territory.Employees.Clear();

                this.DbContext.Territories.Remove(territory);
            }
        }

        public void DeleteRegion(Region region)
        {
            foreach (Territory territory in this.ChangeSet.GetAssociatedChanges(region, p => p.Territories, ChangeOperation.Delete))
            {
                Territory origTerrigory = this.ChangeSet.GetOriginal(territory);
                if (origTerrigory != null)
                {
                    this.DbContext.Territories.AttachAsModified(territory, origTerrigory, this.DbContext);
                }
                else
                {
                    this.DbContext.Territories.Attach(territory);
                }

                // need to remove any employee territory rows
                EntityEntry tEntityEntry = this.DbContext.Entry(territory);
                CollectionEntry employeesCollection = tEntityEntry.Collection(nameof(territory.Employees));
                employeesCollection.Load();
                territory.Employees.Clear();

                this.DbContext.Territories.Remove(territory);
            }

            EntityEntry<Region> entityEntry = this.DbContext.Entry(region);
            if (entityEntry.State != EntityState.Detached)
            {
                entityEntry.State = EntityState.Deleted;
            }            
            else
            {
                this.DbContext.Regions.Attach(region);
                this.DbContext.Regions.Remove(region);
            }
        }
        #endregion

        public IQueryable<ProductInfo> GetProductInfos()
        {
            var results = from p in this.DbContext.Products.Include("Supplier").Include("Category")
                          select new ProductInfo
                          {
                              ProductID = p.ProductID,
                              CategoryName = p.Category.CategoryName,
                              ProductName = p.ProductName,
                              SupplierName = p.Supplier.CompanyName
                          };
            return results;
        }

        public void UpdateProductInfo(ProductInfo current)
        {
            // load the corresponding product to modify and copy
            // the new values
            Product product = this.DbContext.Products.First(p => p.ProductID == current.ProductID);
            product.ProductName = current.ProductName;
        }

        protected override EFCoreDbCtxNorthwindEntities CreateDbContext()
        {
            // TODO: May not be possible to use connection strings
            //var builder = new EntityConnectionStringBuilder("metadata=res://*/Northwind.NorthwindDbCtx.csdl|res://*/Northwind.NorthwindDbCtx.ssdl|res://*/Northwind.NorthwindDbCtx.msl;provider=System.Data.SqlClient;");
            var configuration = ConfigurationManager.ConnectionStrings["Northwind"].ConnectionString;
            return new EFCoreDbCtxNorthwindEntities(configuration);
        }
    }

    /// <summary>
    /// Derived provider that overrides Context creation to use the current
    /// active connection.
    /// </summary>
    [EnableClientAccess]
    [ServiceContract(Name = "Northwind")]
    public class Northwind_CUD : Northwind
    {
        protected override EFCoreDbCtxNorthwindEntities CreateDbContext()
        {
            EFCoreDbCtxNorthwindEntities context = null;

            string connection = DBImager.GetNewDatabaseConnectionString("Northwind");
            if (!string.IsNullOrEmpty(connection))
            {
                // if there is an active connection in scope use it
                // Here we have to append the mapping file info to the connection string
                context = new EFCoreDbCtxNorthwindEntities(connection);
            }
            else
            {
                context = base.CreateDbContext();
            }

            return context;
        }
    }
}
