using System.ComponentModel.DataAnnotations;
using System.Data.Linq;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel.DomainServices.Server;
using System.ServiceModel.DomainServices.Hosting;
using DataTests.AdventureWorks.LTS;
using Microsoft.ServiceModel.DomainServices.LinqToSql;

// These assembly attributes allow us to serialize different CLR types into the same contract
[assembly: ContractNamespace("http://schemas.datacontract.org/2004/07/DataTests.AdventureWorks",
                              ClrNamespace = "DataTests.AdventureWorks.LTS")]

namespace TestDomainServices.LTS {
    [EnableClientAccess]
    public partial class Catalog : LinqToSqlDomainService<AdventureWorks>
    {
        #region Product methods
        [Query]
        public IQueryable<Product> GetProducts()
        {
            return from p in DataContext.Products
                   select p;
        }

        [Query]
        [OutputCache(OutputCacheLocation.Server, duration: 5)]
        public IQueryable<Product> GetProductsWithCaching()
        {
            return from p in DataContext.Products
                   select p;
        }

        [Query]
        public IQueryable<Product> GetProductsWithCustomTotalCount(out int totalCount)
        {
            totalCount = 6;
            return from p in DataContext.Products
                   select p;
        }

        [Query]
        public IQueryable<Product> GetProductsByCategory(int subCategoryID)
        {
            return from p in DataContext.Products
                   where p.ProductSubcategoryID == subCategoryID
                   select p;
        }

        [Insert]
        public void AddProduct(Product product)
        {
            DataContext.Products.InsertOnSubmit(product);
        }

        [Update]
        public void UpdateProduct(Product current)
        {
            DataContext.Products.Attach(current);
        }

        [Delete]
        public void DeleteProduct(Product current)
        {
            DataContext.Products.Attach(current);
            DataContext.Products.DeleteOnSubmit(current);
        }
        #endregion

        #region PurchaseOrder methods
        [Query]
        public IQueryable<PurchaseOrder> GetPurchaseOrders()
        {
            DataLoadOptions loadOptions = new DataLoadOptions();
            loadOptions.LoadWith<PurchaseOrder>(p => p.PurchaseOrderDetails);
            loadOptions.LoadWith<PurchaseOrderDetail>(p => p.Product);
            DataContext.LoadOptions = loadOptions;

            return from p in DataContext.PurchaseOrders
                   select p;
        }

        [Insert]
        public void AddPurchaseOrder(PurchaseOrder purchaseOrder)
        {
            DataContext.PurchaseOrders.InsertOnSubmit(purchaseOrder);
        }

        [Update]
        public void UpdatePurchaseOrder(PurchaseOrder current)
        {
            DataContext.PurchaseOrders.Attach(current);
        }

        [Delete]
        public void DeletePurchaseOrder(PurchaseOrder current)
        {
            DataContext.PurchaseOrders.Attach(current);
            DataContext.PurchaseOrders.DeleteOnSubmit(current);
        }
        #endregion

        #region PurchaseOrderDetail methods
        [Insert]
        public void InsertPurchaseOrderDetail(PurchaseOrderDetail purchaseOrderDetail)
        {
            DataContext.PurchaseOrderDetails.InsertOnSubmit(purchaseOrderDetail);
        }

        [Update]
        public void UpdatePurchaseOrderDetail(PurchaseOrderDetail current)
        {
            DataContext.PurchaseOrderDetails.Attach(current);
        }

        [Delete]
        public void DeletePurchaseOrderDetail(PurchaseOrderDetail current)
        {
            DataContext.PurchaseOrderDetails.Attach(current);
            DataContext.PurchaseOrderDetails.DeleteOnSubmit(current);
        }
        #endregion

        #region Employee methods
        [Query]
        public IQueryable<Employee> GetEmployees()
        {
            return DataContext.Employees;
        }
        #endregion
    }
}

namespace DataTests.AdventureWorks.LTS {
    [MetadataType(typeof(PurchaseOrderMetadata))]
    public partial class PurchaseOrder {
    }

    [MetadataType(typeof(PurchaseOrderDetailMetadata))]
    public partial class PurchaseOrderDetail {
    }

    public static class PurchaseOrderMetadata {
        [Include]
        public static object PurchaseOrderDetails;
    }

    public static class PurchaseOrderDetailMetadata {
        [Include]
        public static object Product;
    }

    [MetadataType(typeof(ProductMetadata))]
    public partial class Product {
    }

    public static class ProductMetadata {
        [Exclude]
        public static object SafetyStockLevel;
    }

    [MetadataType(typeof(EmployeeMetadata))]
    public partial class Employee {
    }

    public static class EmployeeMetadata
    {
        [Include]
        public static object Manager;
    }
}
