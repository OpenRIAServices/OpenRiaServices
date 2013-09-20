using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Objects;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.DomainServices.EntityFramework;
using System.ServiceModel.DomainServices.Hosting;
using System.ServiceModel.DomainServices.Server;
using NorthwindModel;
using TestDomainServices.Testing;

// These assembly attributes allow us to serialize different CLR types into the same contract
[assembly: ContractNamespace("http://schemas.datacontract.org/2004/07/DataTests.Northwind",
                              ClrNamespace = "NorthwindModel")]

namespace TestDomainServices.EF
{
    [EnableClientAccess]
    public class Northwind : LinqToEntitiesDomainService<NorthwindEntities>
    {
        #region Product methods
        public Product GetProductById(int id)
        {
            return this.ObjectContext.Products.SingleOrDefault(p => p.ProductID == id);
        }

        public IQueryable<Product> GetProducts()
        {
            return ObjectContext.Products.Include("Supplier").Include("Category");
        }

        public void InsertProduct(Product product)
        {
            if (product.EntityState != EntityState.Detached)
            {
                ObjectContext.ObjectStateManager.ChangeObjectState(product, EntityState.Added);
            }
            else
            {
                ObjectContext.Products.AddObject(product);
            }
        }

        public void UpdateProduct(Product current)
        {
            ObjectContext.Products.AttachAsModified(current, this.ChangeSet.GetOriginal(current));
        }

        public void DeleteProduct(Product product)
        {
            if (product.EntityState != EntityState.Detached)
            {
                ObjectContext.ObjectStateManager.ChangeObjectState(product, EntityState.Deleted);
            }
            else
            {
                ObjectContext.Products.Attach(product);
                ObjectContext.Products.DeleteObject(product);
            }
        }

        public void DiscontinueProduct(Product product)
        {
            // Don't allow discontinued products to be discontinued again.
            if (product.Discontinued)
            {
                throw new ValidationException("Discontinued products can't be discontinued again.");
            }

            if (product.EntityState == EntityState.Detached)
            {
                ObjectContext.Products.Attach(product);
            }
            product.Discontinued = true;
        }

        public IQueryable<ProductInfo> GetProductInfos()
        {
            var results = from p in ObjectContext.Products.Include("Supplier").Include("Category")
                          select new ProductInfo
                          {
                              ProductID = p.ProductID, CategoryName = p.Category.CategoryName, ProductName = p.ProductName, SupplierName = p.Supplier.CompanyName
                          };
            return results;
        }

        public void UpdateProductInfo(ProductInfo current)
        {
            // load the corresponding product to modify and copy
            // the new values
            Product product = ObjectContext.Products.First(p => p.ProductID == current.ProductID);
            product.ProductName = current.ProductName;
        }

        protected override bool ResolveConflicts(IEnumerable<ObjectStateEntry> conflicts)
        {
            foreach (ObjectStateEntry stateEntry in conflicts)
            {
                if (stateEntry.State == EntityState.Detached ||
                    stateEntry.IsRelationship)
                {
                    continue;
                }

                Type entityType = stateEntry.Entity.GetType();
                if (entityType == typeof(Product))
                {
                    if (!this.ResolveProductConflict(stateEntry))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool ResolveProductConflict(ObjectStateEntry entryInConflict)
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
                    this.ObjectContext.Refresh(RefreshMode.ClientWins, product);
                    break;
                case "RefreshCurrent":
                    this.ObjectContext.Refresh(RefreshMode.StoreWins, product);
                    break;
                case "ReturnTrueNoResolve":
                    return true;
                case "ReturnFalse":
                    return false;
                case "ReturnFalseWithResolve":
                    this.ObjectContext.Refresh(RefreshMode.StoreWins, product);
                    return false;
                case "":
                    this.ObjectContext.Refresh(RefreshMode.ClientWins, product);
                    break;
                default:
                    {
                        throw new NotImplementedException(string.Format("ResolveMethod {0} is not defined", product.ResolveMethod));
                    }
            }

            return true;
        }

        private bool ResolveProductWithMerge(ObjectStateEntry entryInConflict)
        {
            Product current = (Product)entryInConflict.Entity;
            Product original = this.ChangeSet.GetOriginal(current);

            // Keep a collection of all modified properties and their values.
            List<Tuple<string, object>> modifiedMembers = new List<Tuple<string,object>>();
            for (int i=0;i<entryInConflict.CurrentValues.FieldCount;i++)
            {
                object currValue = entryInConflict.CurrentValues[i];
                object origValue = entryInConflict.OriginalValues[i];
                if (!object.Equals(currValue, origValue))
                {
                    modifiedMembers.Add(new Tuple<string, object>(entryInConflict.CurrentValues.GetName(i), currValue));
                }
            }
            
            // keep track of any modified associations
            EntityKey currCategoryKey = current.CategoryReference.EntityKey;
            EntityKey origCategoryKey = original.CategoryReference.EntityKey;

            // refresh from store to get the updated values
            this.ObjectContext.Refresh(RefreshMode.StoreWins, current);

            if (current.EntityState == EntityState.Detached)
            {
                // if the refresh results in the entity becoming
                // detached, it means the entity no longer exists
                // in the store
                return false;
            }

            // now play back changes
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(typeof(Product));
            foreach (var modifiedMember in modifiedMembers)
            {
                PropertyDescriptor modifiedProperty = properties[modifiedMember.Item1];
                modifiedProperty.SetValue(current, modifiedMember.Item2);
            }

            // play back any association member changes
            if (currCategoryKey != origCategoryKey)
            {
                current.CategoryReference.EntityKey = currCategoryKey;
            }

            return true;
        }
        #endregion

        #region Order methods
        public IQueryable<Order> GetOrders()
        {
            return ObjectContext.Orders.Include("Order_Details").Include("Order_Details.Product");
        }

        public void InsertOrder(Order order)
        {
            if (order.EntityState != EntityState.Detached)
            {
                ObjectContext.ObjectStateManager.ChangeObjectState(order, EntityState.Added);
            }
            else
            {
                ObjectContext.Orders.AddObject(order);
            }
        }

        public void UpdateOrder(Order current)
        {
            ObjectContext.Orders.AttachAsModified(current, this.ChangeSet.GetOriginal(current));
        }

        public void DeleteOrder(Order order)
        {
            if (order.EntityState != EntityState.Detached)
            {
                ObjectContext.ObjectStateManager.ChangeObjectState(order, EntityState.Deleted);
            }
            else
            {
                ObjectContext.Orders.Attach(order);
                ObjectContext.Orders.DeleteObject(order);
            }
        }
        #endregion

        #region OrderDetail methods
        public IQueryable<Order_Detail> GetOrderDetails()
        {
            return ObjectContext.Order_Details.Include("Product");
        }

        public void InsertOrderDetail(Order_Detail detail)
        {
            if (detail.EntityState != EntityState.Detached)
            {
                ObjectContext.ObjectStateManager.ChangeObjectState(detail, EntityState.Added);
            }
            else
            {
                ObjectContext.Order_Details.AddObject(detail);
            }
        }

        public void UpdateOrderDetail(Order_Detail current)
        {
            ObjectContext.Order_Details.AttachAsModified(current, this.ChangeSet.GetOriginal(current));
        }

        public void DeleteOrderDetail(Order_Detail detail)
        {
            if (detail.EntityState != EntityState.Detached)
            {
                ObjectContext.ObjectStateManager.ChangeObjectState(detail, EntityState.Deleted);
            }
            else
            {
                ObjectContext.Order_Details.Attach(detail);
                ObjectContext.Order_Details.DeleteObject(detail);
            }
        }
        #endregion

        #region Customer methods
        public IQueryable<Customer> GetCustomers()
        {
            return ObjectContext.Customers;
        }

        public void InsertCustomer(Customer customer)
        {
            if (customer.EntityState != EntityState.Detached)
            {
                ObjectContext.ObjectStateManager.ChangeObjectState(customer, EntityState.Added);
            }
            else
            {
                ObjectContext.Customers.AddObject(customer);
            }
        }

        public void UpdateCustomer(Customer current)
        {
            ObjectContext.Customers.AttachAsModified(current, this.ChangeSet.GetOriginal(current));
        }

        public void DeleteCustomer(Customer customer)
        {
            if (customer.EntityState != EntityState.Detached)
            {
                ObjectContext.ObjectStateManager.ChangeObjectState(customer, EntityState.Deleted);
            }
            else
            {
                ObjectContext.Customers.Attach(customer);
                ObjectContext.Customers.DeleteObject(customer);
            }
        }
        #endregion

        #region Category methods
        public IQueryable<Category> GetCategories()
        {
            return ObjectContext.Categories;
        }

        public void InsertCategory(Category category)
        {
            if (category.EntityState != EntityState.Detached)
            {
                ObjectContext.ObjectStateManager.ChangeObjectState(category, EntityState.Added);
            }
            else
            {
                ObjectContext.Categories.AddObject(category);
            }
        }

        public void UpdateCategory(Category current)
        {
            ObjectContext.Categories.AttachAsModified(current, this.ChangeSet.GetOriginal(current));
        }

        public void DeleteCategory(Category category)
        {
            if (category.EntityState != EntityState.Detached)
            {
                ObjectContext.ObjectStateManager.ChangeObjectState(category, EntityState.Deleted);
            }
            else
            {
                ObjectContext.Categories.Attach(category);
                ObjectContext.Categories.DeleteObject(category);
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
            return this.ObjectContext.Regions.Include("Territories");
        }

        public void InsertRegion(Region region)
        {
            this.ObjectContext.Regions.AddObject(region);

            foreach (Territory territory in this.ChangeSet.GetAssociatedChanges(region, p => p.Territories, ChangeOperation.Insert))
            {
                this.ObjectContext.Territories.AddObject(territory);
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
                this.ObjectContext.Territories.Attach(territory);
            }
            if (originalRegion == null)
            {
                this.ObjectContext.Regions.Attach(region);
            }

            // Attach and apply changes to modified entities
            if (originalRegion != null)
            {
                this.ObjectContext.Regions.AttachAsModified(region, originalRegion);
            }
            foreach (Territory territory in this.ChangeSet.GetAssociatedChanges(region, p => p.Territories, ChangeOperation.Update))
            {
                this.ObjectContext.Territories.AttachAsModified(territory, this.ChangeSet.GetOriginal(territory));
            }

            // Add new entities
            foreach (Territory territory in this.ChangeSet.GetAssociatedChanges(region, p => p.Territories, ChangeOperation.Insert))
            {
                if (territory.EntityState != EntityState.Detached)
                {
                    // need to change the object state if the entity was already eagerly added
                    this.ObjectContext.ObjectStateManager.ChangeObjectState(territory, EntityState.Added);
                }
                else
                {
                    this.ObjectContext.Territories.AddObject(territory);
                }
            }

            // Finally, process any deleted entites and relationships
            foreach (Territory territory in this.ChangeSet.GetAssociatedChanges(region, p => p.Territories, ChangeOperation.Delete))
            {
                Territory origTerrigory = this.ChangeSet.GetOriginal(territory);
                if (origTerrigory != null)
                {
                    this.ObjectContext.Territories.AttachAsModified(territory, origTerrigory);
                }
                else
                {
                    this.ObjectContext.Territories.Attach(territory);
                }

                // need to remove any employee territory rows
                territory.Employees.Load();
                territory.Employees.Clear();

                this.ObjectContext.Territories.DeleteObject(territory);
            }
        }

        public void DeleteRegion(Region region)
        {
            foreach (Territory territory in this.ChangeSet.GetAssociatedChanges(region, p => p.Territories, ChangeOperation.Delete))
            {
                Territory origTerrigory = this.ChangeSet.GetOriginal(territory);
                if (origTerrigory != null)
                {
                    this.ObjectContext.Territories.AttachAsModified(territory, origTerrigory);
                }
                else
                {
                    this.ObjectContext.Territories.Attach(territory);
                }

                // need to remove any employee territory rows
                territory.Employees.Load();
                territory.Employees.Clear();

                this.ObjectContext.Territories.DeleteObject(territory);
            }

            if (region.EntityState != EntityState.Detached)
            {
                ObjectContext.ObjectStateManager.ChangeObjectState(region, EntityState.Deleted);
            }
            else
            {
                ObjectContext.Regions.Attach(region);
                ObjectContext.Regions.DeleteObject(region);
            }
        }
        #endregion
    }

    /// <summary>
    /// Derived provider that overrides Context creation to use the current
    /// active connection.
    /// </summary>
    [EnableClientAccess]
    [ServiceContract(Name = "Northwind")]
    public class Northwind_CUD : Northwind
    {
        protected override NorthwindEntities CreateObjectContext()
        {
            NorthwindEntities context = null;

            string connection = ActiveConnections.Get("Northwind");
            if (!string.IsNullOrEmpty(connection))
            {
                // if there is an active connection in scope use it
                // Here we have to append the mapping file info to the connection string
                connection = string.Format("metadata=res://*/DataModels.Northwind.csdl|res://*/DataModels.Northwind.ssdl|res://*/DataModels.Northwind.msl;provider=System.Data.SqlClient;provider connection string=\"{0}\"", connection);
                context = new NorthwindEntities(connection);
            }
            else
            {
                context = base.CreateObjectContext();
            }

            return context;
        }
    }
}

namespace NorthwindModel
{
    [MetadataType(typeof(OrderMetadata))]
    public partial class Order
    {
        // Calculated property. Used to test that user-defined properties work properly.
        [DataMember]
        public string FormattedName
        {
            get
            {
                return "OrderID: " + this.OrderID.ToString();
            }
        }
    }

    [MetadataType(typeof(OrderDetailMetadata))]
    public partial class Order_Detail
    {
    }

    public static class OrderMetadata
    {
        [Include]
        public static object Order_Details;
    }

    public static class OrderDetailMetadata
    {
        [Include]
        public static object Product;
    }

    [MetadataType(typeof(ProductMetadata))]
    public partial class Product
    {
        private string _resolveMethod = String.Empty;
        
        // Additional data member to enable resolve logic to differ based on the test scenario string being passed in
        [DataMember]
        public string ResolveMethod
        {
            get
            {
                return _resolveMethod;
            }
            set
            {
                _resolveMethod = value;
            }
        }
    }

    public static class ProductMetadata
    {
        [Include("CategoryName", "CategoryName")]
        public static object Category;

        [Include("CompanyName", "SupplierName")]
        public static object Supplier;
    }

    /// <summary>
    /// Non DAL projection type used to verify that such types can be returned
    /// from the provider
    /// </summary>
    public class ProductInfo
    {
        public static ProductInfo CreateProductInfo(int productID, string productName, string categoryName, string supplierName)
        {
            ProductInfo prodInfo = new ProductInfo();
            prodInfo.ProductID = productID;
            prodInfo.ProductName = productName;
            prodInfo.CategoryName = categoryName;
            prodInfo.SupplierName = supplierName;
            return prodInfo;
        }

        [Key]
        public int ProductID
        {
            get;
            set;
        }

        public string ProductName
        {
            get;
            set;
        }

        [Editable(false)]
        public string CategoryName
        {
            get;
            set;
        }

        [Editable(false)]
        public string SupplierName
        {
            get;
            set;
        }
    }

    [MetadataType(typeof(RegionMetadata))]
    public partial class Region
    {
    }

    public static class RegionMetadata
    {
        [Composition]
        [Include]
        public static object Territories;
    }
}
