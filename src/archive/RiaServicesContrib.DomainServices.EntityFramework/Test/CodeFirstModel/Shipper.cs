namespace CodeFirstModels
{
    using System;
    using System.Collections.Generic;
    
    public partial class Shipper
    {
        public Shipper()
        {
            this.Orders = new HashSet<Order>();
        }
    
        public int ShipperID { get; set; }
        public string CompanyName { get; set; }
        public string Phone { get; set; }
    
        public virtual ICollection<Order> Orders { get; set; }
    }
}
