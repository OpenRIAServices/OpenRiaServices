namespace CodeFirstModels
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using OpenRiaServices.DomainServices.Server;
    
    public partial class Order_Detail
    {
        [Key]
        [Column(Order=0)]
        [ConcurrencyCheck]
        public int OrderID { get; set; }
        [Key]
        [Column(Order = 1)]
        [ConcurrencyCheck]
        public int ProductID { get; set; }
        [ConcurrencyCheck]
        public decimal UnitPrice { get; set; }
        [ConcurrencyCheck]
        public short Quantity { get; set; }
        [ConcurrencyCheck]
        public float Discount { get; set; }
        
        [ForeignKey("OrderID")]
        public virtual Order Order { get; set; }
        [Include]
        [ForeignKey("ProductID")]
        public virtual Product Product { get; set; }
    }
}
