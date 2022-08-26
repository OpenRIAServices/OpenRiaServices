using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace CodeFirstModels
{
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
}
