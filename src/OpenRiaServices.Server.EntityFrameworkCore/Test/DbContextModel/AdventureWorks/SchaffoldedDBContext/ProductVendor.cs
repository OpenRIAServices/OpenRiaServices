using System;
using System.Collections.Generic;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace DbContextModels
{
    public partial class ProductVendor
    {
        public int ProductId { get; set; }
        public int VendorId { get; set; }
        public int AverageLeadTime { get; set; }
        public decimal StandardPrice { get; set; }
        public decimal? LastReceiptCost { get; set; }
        public DateTime? LastReceiptDate { get; set; }
        public int MinOrderQty { get; set; }
        public int MaxOrderQty { get; set; }
        public int? OnOrderQty { get; set; }
        public string UnitMeasureCode { get; set; }
        public DateTime ModifiedDate { get; set; }
    }
}
