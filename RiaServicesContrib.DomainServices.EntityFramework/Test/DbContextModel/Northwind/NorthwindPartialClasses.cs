using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.ServiceModel.DomainServices.Server;

// These assembly attributes allow us to serialize different CLR types into the same contract
[assembly: ContractNamespace("http://schemas.datacontract.org/2004/07/DataTests.Northwind",
                              ClrNamespace = "DbContextModels.Northwind")]

namespace DbContextModels.Northwind
{
    public partial class DbCtxNorthwindEntities
    {
        public DbCtxNorthwindEntities(string connection)
            : base(connection)
        {
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
}
