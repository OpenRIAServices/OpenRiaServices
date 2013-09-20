using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel.DomainServices.EntityFramework;
using System.ServiceModel.DomainServices.Server;
using System.ServiceModel.DomainServices.Hosting;
using AdventureWorksModel;

// These assembly attributes allow us to serialize different CLR types into the same contract
[assembly: ContractNamespace("http://schemas.datacontract.org/2004/07/DataTests.AdventureWorks",
                              ClrNamespace = "AdventureWorksModel")]

namespace TestDomainServices.EF {
    [EnableClientAccess]
    public partial class Catalog : LinqToEntitiesDomainService<AdventureWorksEntities>
    {
        #region Product methods
        [Query]
        public IQueryable<Product> GetProducts() {
            return from p in ObjectContext.Products
                   select p;
        }

        [Query]
        [OutputCache(OutputCacheLocation.Server, duration: 5)]
        public IQueryable<Product> GetProductsWithCaching()
        {
            return from p in ObjectContext.Products
                   select p;
        }

        [Query]
        public IQueryable<Product> GetProductsWithCustomTotalCount(out int totalCount)
        {
            totalCount = 6;
            return from p in ObjectContext.Products
                   select p;
        }

        [Query]
        public IQueryable<Product> GetProductsByCategory(int subCategoryID)
        {
            return from p in ObjectContext.Products
                   where p.ProductSubcategory.ProductSubcategoryID == subCategoryID
                   select p;
        }

        [Insert]
        public void AddProduct(Product product)
        {
            ObjectContext.Products.AddObject(product);   
        }

        [Update]
        public void UpdateProduct(Product current)
        {
            ObjectContext.Products.AttachAsModified(current, this.ChangeSet.GetOriginal(current));
        }

        [Delete]
        public void DeleteProduct(Product current)
        {
            ObjectContext.Products.Attach(current);
            ObjectContext.Products.DeleteObject(current);
        }
        #endregion

        #region PurchaseOrder methods
        [Query]
        public IQueryable<PurchaseOrder> GetPurchaseOrders()
        {
            return ObjectContext.PurchaseOrders.Include("PurchaseOrderDetails").Include("PurchaseOrderDetails.Product");
        }

        [Insert]
        public void AddPurchaseOrder(PurchaseOrder purchaseOrder)
        {
            ObjectContext.PurchaseOrders.AddObject(purchaseOrder);
        }

        [Update]
        public void UpdatePurchaseOrder(PurchaseOrder current)
        {
            ObjectContext.PurchaseOrders.AttachAsModified(current, this.ChangeSet.GetOriginal(current));
        }

        [Delete]
        public void DeletePurchaseOrder(PurchaseOrder current)
        {
            ObjectContext.PurchaseOrders.Attach(current);
            ObjectContext.PurchaseOrders.DeleteObject(current);
        }
        #endregion
        
        #region PurchaseOrderDetail methods
        [Insert]
        public void InsertPurchaseOrderDetail(PurchaseOrderDetail purchaseOrderDetail)
        {
            ObjectContext.AddToPurchaseOrderDetails(purchaseOrderDetail);
        }

        [Update]
        public void UpdatePurchaseOrderDetail(PurchaseOrderDetail current)
        {
            ObjectContext.PurchaseOrderDetails.AttachAsModified(current, this.ChangeSet.GetOriginal(current));
        }

        [Delete]
        public void DeletePurchaseOrderDetail(PurchaseOrderDetail current)
        {
            ObjectContext.PurchaseOrderDetails.Attach(current);
            ObjectContext.PurchaseOrderDetails.DeleteObject(current);
        }
        #endregion

        #region Employee methods
        [Query]
        public IQueryable<Employee> GetEmployees()
        {
            return ObjectContext.Employees;
        }

        [Query]
        public IEnumerable<EmployeeInfo> GetEmployeeInfos()
        {
            var results = from e in this.ObjectContext.Employees
                          //select EmployeeInfo.CreateEmployeeInfo(e.EmployeeID, e.Contact.FirstName, e.Contact.LastName, e.SalesPerson.SalesTerritory.TerritoryID);
                          select new
                          {
                              e.EmployeeID, e.Contact.FirstName, e.Contact.LastName, e.SalesPerson.SalesTerritory.TerritoryID
                          };
            return results as IEnumerable<EmployeeInfo>;
        }
        #endregion
    }
}

namespace AdventureWorksModel {
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

        [RoundtripOriginal]
        public static object Weight;
    }

    [MetadataType(typeof(EmployeeMetadata))]
    public partial class Employee
    {
    }

    public static class EmployeeMetadata
    {
        [Include]
        public static object Manager;
    }

    public class EmployeeInfo
    {
        public static EmployeeInfo CreateEmployeeInfo(int employeeID, string firstName, string lastName, int territoryID)
        {
            EmployeeInfo empInfo = new EmployeeInfo();
            empInfo.EmployeeID = employeeID;
            empInfo.FirstName = firstName;
            empInfo.LastName = lastName;
            empInfo.TerritoryID = territoryID;
            return empInfo;
        }

        [Key]
        public int EmployeeID
        {
            get;
            set;
        }

        public string FirstName
        {
            get;
            set;
        }
        public string LastName
        {
            get;
            set;
        }
        public int TerritoryID
        {
            get;
            set;
        }
    }
}
