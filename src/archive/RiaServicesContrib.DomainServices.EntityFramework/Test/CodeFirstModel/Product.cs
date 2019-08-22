namespace CodeFirstModels
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;
    using System.ServiceModel.DomainServices.Server;
    
    public partial class Product
    {
        public Product()
        {
            this.Order_Details = new HashSet<Order_Detail>();
        }
    
        public int ProductID { get; set; }
        [Required]
        [StringLength(40)]
        [ConcurrencyCheck]
        public string ProductName { get; set; }
        [ConcurrencyCheck]
        public Nullable<int> SupplierID { get; set; }
        [ConcurrencyCheck]
        public Nullable<int> CategoryID { get; set; }
        [StringLength(20)]
        [ConcurrencyCheck]
        public string QuantityPerUnit { get; set; }
        [ConcurrencyCheck]
        public Nullable<decimal> UnitPrice { get; set; }
        [ConcurrencyCheck]
        public Nullable<short> UnitsInStock { get; set; }
        [ConcurrencyCheck]
        public Nullable<short> UnitsOnOrder { get; set; }
        [ConcurrencyCheck]
        public Nullable<short> ReorderLevel { get; set; }
        [ConcurrencyCheck]
        public bool Discontinued { get; set; }

        [Include("CategoryName", "CategoryName")]
        public virtual Category Category { get; set; }

        public virtual ICollection<Order_Detail> Order_Details { get; set; }

        [Include("CompanyName", "SupplierName")]
        public virtual Supplier Supplier { get; set; }

        private string _resolveMethod = String.Empty;
        [DataMember]
        [NotMapped]
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
}
