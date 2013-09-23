namespace CodeFirstModels
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;
    using OpenRiaServices.DomainServices.Server;
    
    public partial class Order
    {
        public Order()
        {
            this.Order_Details = new HashSet<Order_Detail>();
        }

        [ConcurrencyCheck]
        public int OrderID { get; set; }
        [StringLength(5)]
        [ConcurrencyCheck]
        public string CustomerID { get; set; }
        [ConcurrencyCheck]
        public Nullable<int> EmployeeID { get; set; }
        [ConcurrencyCheck]
        public Nullable<System.DateTime> OrderDate { get; set; }
        [ConcurrencyCheck]
        public Nullable<System.DateTime> RequiredDate { get; set; }
        [ConcurrencyCheck]
        public Nullable<System.DateTime> ShippedDate { get; set; }
        [ConcurrencyCheck]
        public Nullable<int> ShipVia { get; set; }
        [ConcurrencyCheck]
        public Nullable<decimal> Freight { get; set; }
        [StringLength(40)]
        [ConcurrencyCheck]
        public string ShipName { get; set; }
        [StringLength(60)]
        [ConcurrencyCheck]
        public string ShipAddress { get; set; }
        [StringLength(15)]
        [ConcurrencyCheck]
        public string ShipCity { get; set; }
        [StringLength(15)]
        [ConcurrencyCheck]
        public string ShipRegion { get; set; }
        [StringLength(10)]
        [ConcurrencyCheck]
        public string ShipPostalCode { get; set; }
        [StringLength(15)]
        [ConcurrencyCheck]
        public string ShipCountry { get; set; }
    
        public virtual Customer Customer { get; set; }
        public virtual Employee Employee { get; set; }
        [Include]
        public virtual ICollection<Order_Detail> Order_Details { get; set; }
        public virtual Shipper Shipper { get; set; }

        // Calculated property. Used to test that user-defined properties work properly.
        [NotMapped]
        public string FormattedName
        {
            get
            {
                return "OrderID: " + this.OrderID.ToString();
            }
        }
    }
}
