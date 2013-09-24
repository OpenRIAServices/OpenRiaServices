namespace CodeFirstModels
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    
    public partial class Customer
    {
        public Customer()
        {
            this.Orders = new HashSet<Order>();
            this.CustomerDemographics = new HashSet<CustomerDemographic>();
        }

        [StringLength(15)]
        [ConcurrencyCheck]
        public string CustomerID { get; set; }
        [StringLength(40)]
        [ConcurrencyCheck]
        public string CompanyName { get; set; }
        [StringLength(30)]
        [ConcurrencyCheck]
        public string ContactName { get; set; }
        [StringLength(30)]
        [ConcurrencyCheck]
        public string ContactTitle { get; set; }
        [StringLength(60)]
        [ConcurrencyCheck]
        public string Address { get; set; }
        [StringLength(15)]
        [ConcurrencyCheck]
        public string City { get; set; }
        [StringLength(15)]
        [ConcurrencyCheck]
        public string Region { get; set; }
        [StringLength(10)]
        [ConcurrencyCheck]
        public string PostalCode { get; set; }
        [StringLength(15)]
        [ConcurrencyCheck]
        public string Country { get; set; }
        [StringLength(24)]
        [ConcurrencyCheck]
        public string Phone { get; set; }
        [StringLength(24)]
        [ConcurrencyCheck]
        public string Fax { get; set; }
    
        public virtual ICollection<Order> Orders { get; set; }
        public virtual ICollection<CustomerDemographic> CustomerDemographics { get; set; }
    }
}
