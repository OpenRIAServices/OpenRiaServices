using System;
using System.ComponentModel.DataAnnotations;
using System.Data.Linq;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.DomainServices.Hosting;
using System.ServiceModel.DomainServices.Server;
using DataTests.Northwind.LTS;
using Microsoft.ServiceModel.DomainServices.LinqToSql;
using TestDomainServices.Testing;

// These assembly attributes allow us to serialize different CLR types into the same contract
[assembly: ContractNamespace("http://schemas.datacontract.org/2004/07/DataTests.Northwind",
                              ClrNamespace = "DataTests.Northwind.LTS")]

namespace TestDomainServices.LTS
{
    [EnableClientAccess]
    public class Northwind : LinqToSqlDomainService<NorthwindDataContext>
    {
        #region Product methods
        public Product GetProductById(int id)
        {
            return this.DataContext.Products.SingleOrDefault(p => p.ProductID == id);
        }

        public IQueryable<Product> GetProducts()
        {
            DataLoadOptions loadOptions = new DataLoadOptions();
            loadOptions.LoadWith<Product>(p => p.Supplier);
            loadOptions.LoadWith<Product>(p => p.Category);
            DataContext.LoadOptions = loadOptions;

            return DataContext.Products;
        }

        public void InsertProduct(Product product)
        {
            DataContext.Products.InsertOnSubmit(product);
        }

        public void UpdateProduct(Product current)
        {
            DataContext.Products.Attach(current, this.ChangeSet.GetOriginal(current));
        }

        public void DeleteProduct(Product product)
        {
            DataContext.Products.Attach(product);
            DataContext.Products.DeleteOnSubmit(product);
        }

        public void DiscontinueProduct(Product product)
        {
            // Don't allow discontinued products to be discontinued again.
            if (product.Discontinued)
            {
                throw new ValidationException("Discontinued products can't be discontinued again.");
            }
            
            if (!DataContext.Products.IsAttached(product))
            {
                DataContext.Products.Attach(product);
            }
            product.Discontinued = true;
        }

        public IQueryable<ProductInfo> GetProductInfos()
        {
            DataLoadOptions loadOptions = new DataLoadOptions();
            loadOptions.LoadWith<Product>(p => p.Supplier);
            loadOptions.LoadWith<Product>(p => p.Category);
            DataContext.LoadOptions = loadOptions;

            var results = from p in DataContext.Products
                          select new ProductInfo
                          {
                              ProductID = p.ProductID, ProductName = p.ProductName, CategoryName = p.Category.CategoryName, SupplierName = p.Supplier.CompanyName
                          };
            return results;
        }

        public void UpdateProductInfo(ProductInfo current)
        {
            // load the corresponding product to modify and copy
            // the new values
            Product product = DataContext.Products.Single(p => p.ProductID == current.ProductID);
            product.ProductName = current.ProductName;
        }

        protected override bool ResolveConflicts(ChangeConflictCollection conflicts)
        {
            foreach (ObjectChangeConflict conflict in conflicts)
            {
                Type entityType = conflict.Object.GetType();
                if (entityType == typeof(Product))
                {
                    if (!this.ResolveProductConflict(conflict))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool ResolveProductConflict(ObjectChangeConflict conflict)
        {
            Product current = (Product)conflict.Object;

            if (conflict.IsDeleted)
            {
                // we don't attempt to resolve delete conflicts -
                // we send those back to the client
                return false;
            }

            switch (current.ResolveMethod)
            {
                case "ThrowValidationEx":
                    throw new ValidationException("testing");
                case "ThrowDomainServiceEx":
                    throw new DomainException("testing");
                case "MergeIntoCurrent":
                    conflict.Resolve(RefreshMode.KeepChanges);
                    break;
                case "KeepCurrent":
                    conflict.Resolve(RefreshMode.KeepCurrentValues);
                    break;
                case "RefreshCurrent":
                    conflict.Resolve(RefreshMode.OverwriteCurrentValues);
                    break;
                case "ReturnTrueNoResolve":
                    return true;
                case "ReturnFalse":
                    return false;
                case "ReturnFalseWithResolve":
                    conflict.Resolve(RefreshMode.OverwriteCurrentValues);
                    return false;
                case "":
                    conflict.Resolve(RefreshMode.KeepCurrentValues);
                    break;
                default:
                    {
                        throw new NotImplementedException(string.Format("ResolveMethod {0} is not defined", current.ResolveMethod));
                    }
            }

            return conflict.IsResolved;
        }

        #endregion

        #region Order methods
        public IQueryable<Order> GetOrders()
        {
            DataLoadOptions loadOptions = new DataLoadOptions();
            loadOptions.LoadWith<Order>(p => p.Order_Details);
            loadOptions.LoadWith<Order_Detail>(p => p.Product);
            DataContext.LoadOptions = loadOptions;

            return DataContext.Orders;
        }

        public void InsertOrder(Order order)
        {
            DataContext.Orders.InsertOnSubmit(order);
        }

        public void UpdateOrder(Order current)
        {
            DataContext.Orders.Attach(current, this.ChangeSet.GetOriginal(current));
        }

        public void DeleteOrder(Order current)
        {
            DataContext.Orders.Attach(current);
            DataContext.Orders.DeleteOnSubmit(current);
        }
        #endregion

        #region OrderDetail methods
        public IQueryable<Order_Detail> GetOrderDetails()
        {
            DataLoadOptions loadOptions = new DataLoadOptions();
            loadOptions.LoadWith<Order_Detail>(p => p.Product);
            DataContext.LoadOptions = loadOptions;

            return DataContext.Order_Details;
        }

        public void InsertOrderDetail(Order_Detail detail)
        {
            DataContext.Order_Details.InsertOnSubmit(detail);
        }

        public void UpdateOrderDetail(Order_Detail current)
        {
            DataContext.Order_Details.Attach(current, this.ChangeSet.GetOriginal(current));
        }

        public void DeleteOrderDetail(Order_Detail current)
        {
            DataContext.Order_Details.Attach(current);
            DataContext.Order_Details.DeleteOnSubmit(current);
        }
        #endregion

        #region Customer methods
        public IQueryable<Customer> GetCustomers()
        {
            return DataContext.Customers;
        }

        public void InsertCustomer(Customer customer)
        {
            DataContext.Customers.InsertOnSubmit(customer);
        }

        public void UpdateCustomer(Customer current)
        {
            DataContext.Customers.Attach(current, this.ChangeSet.GetOriginal(current));
        }

        public void DeleteCustomer(Customer current)
        {
            DataContext.Customers.Attach(current);
            DataContext.Customers.DeleteOnSubmit(current);
        }
        #endregion

        #region Category methods
        public IQueryable<Category> GetCategories()
        {
            return DataContext.Categories;
        }

        public void InsertCategory(Category category)
        {
            DataContext.Categories.InsertOnSubmit(category);
        }

        public void UpdateCategory(Category current)
        {
            DataContext.Categories.Attach(current, this.ChangeSet.GetOriginal(current));
        }

        public void DeleteCategory(Category category)
        {
            DataContext.Categories.Attach(category);
            DataContext.Categories.DeleteOnSubmit(category);
        }
        #endregion

        #region Region/Territory composition
        public Region GetRegionById(int id)
        {
            return this.GetRegions().Where(p => p.RegionID == id).SingleOrDefault();
        }

        public IQueryable<Region> GetRegions()
        {
            DataLoadOptions opts = new DataLoadOptions();
            opts.LoadWith<Region>(p => p.Territories);
            this.DataContext.LoadOptions = opts;

            return this.DataContext.Regions;
        }

        public void InsertRegion(Region region)
        {
            this.DataContext.Regions.InsertOnSubmit(region);

            foreach (Territory territory in this.ChangeSet.GetAssociatedChanges(region, p => p.Territories, ChangeOperation.Insert))
            {
                this.DataContext.Territories.InsertOnSubmit(territory);
            }
        }

        public void UpdateRegion(Region region)
        {
            Region originalRegion = this.ChangeSet.GetOriginal(region);
            if (originalRegion != null)
            {
                this.DataContext.Regions.Attach(region, originalRegion);
            }

            foreach (Territory territory in this.ChangeSet.GetAssociatedChanges(region, p => p.Territories))
            {
                ChangeOperation op = this.ChangeSet.GetChangeOperation(territory);
                if (op == ChangeOperation.Insert)
                {
                    this.DataContext.Territories.InsertOnSubmit(territory);
                }
                else if (op == ChangeOperation.Update)
                {
                    this.DataContext.Territories.Attach(territory, this.ChangeSet.GetOriginal(territory));
                }
                else if (op == ChangeOperation.Delete)
                {
                    Territory origTerrigory = this.ChangeSet.GetOriginal(territory);
                    if (origTerrigory != null)
                    {
                        this.DataContext.Territories.Attach(territory, origTerrigory);
                    }
                    else
                    {
                        this.DataContext.Territories.Attach(territory);
                    }
                    this.DataContext.Territories.DeleteOnSubmit(territory);

                    // need to remove any employee territory rows
                    this.DataContext.EmployeeTerritories.DeleteAllOnSubmit(territory.EmployeeTerritories); 
                }
            }
        }

        public void DeleteRegion(Region region)
        {
            this.DataContext.Regions.Attach(region);
            this.DataContext.Regions.DeleteOnSubmit(region);

            foreach (Territory territory in this.ChangeSet.GetAssociatedChanges(region, p => p.Territories, ChangeOperation.Delete))
            {  
                Territory origTerrigory = this.ChangeSet.GetOriginal(territory);
                if (origTerrigory != null)
                {
                    this.DataContext.Territories.Attach(territory, origTerrigory);
                }
                else
                {
                    this.DataContext.Territories.Attach(territory);
                }
                this.DataContext.Territories.DeleteOnSubmit(territory);

                // need to remove any employee territory rows
                this.DataContext.EmployeeTerritories.DeleteAllOnSubmit(territory.EmployeeTerritories);
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
        protected override NorthwindDataContext CreateDataContext()
        {
            NorthwindDataContext context = null;

            string connection = ActiveConnections.Get("Northwind");
            if (!string.IsNullOrEmpty(connection))
            {
                // if there is an active connection in scope use it
                context = new NorthwindDataContext(connection);
            }
            else {
                context = base.CreateDataContext();
            }

            return context;
        }
    }
}

namespace DataTests.Northwind.LTS
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

        // DAL level entity validation added to test that the LTS provider
        // correctly handles validation exceptions from that layer
        partial void OnValidate(System.Data.Linq.ChangeAction action)
        {
            if(action == ChangeAction.Update)
            {
                if (this.ReorderLevel < 0)
                {
                    ValidationResult vr = new ValidationResult("Invalid Product Update!", new string[] { "ReorderLevel" });
                    throw new ValidationException(vr, null, this);
                }
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
