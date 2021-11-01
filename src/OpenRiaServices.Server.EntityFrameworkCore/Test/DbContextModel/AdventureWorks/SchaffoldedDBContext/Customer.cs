using System;
using System.Collections.Generic;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace DbContextModels.AdventureWorksEFCoreScaffolded
{
    public partial class Customer
    {
        public int CustomerId { get; set; }
        public int? TerritoryId { get; set; }
        public string AccountNumber { get; set; }
        public string CustomerType { get; set; }
        public Guid Rowguid { get; set; }
        public DateTime ModifiedDate { get; set; }
    }
}
