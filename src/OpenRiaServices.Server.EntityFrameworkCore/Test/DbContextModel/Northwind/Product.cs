using System;
using System.Collections.Generic;
using OpenRiaServices.Server;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace EFCoreModels.Northwind
{
    public partial class Product
    {
        public Product()
        {
            Order_Details = new HashSet<Order_Detail>();
        }

        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public int? SupplierID { get; set; }
        public int? CategoryID { get; set; }
        public string QuantityPerUnit { get; set; }
        public decimal? UnitPrice { get; set; }
        public short? UnitsInStock { get; set; }
        public short? UnitsOnOrder { get; set; }
        public short? ReorderLevel { get; set; }
        public bool Discontinued { get; set; }

        [Include("CategoryName", "CategoryName")]
        public virtual Category Category { get; set; }
        [Include("CompanyName", "SupplierName")]
        public virtual Supplier Supplier { get; set; }
        public virtual ICollection<Order_Detail> Order_Details { get; set; }
    }
}
