using System;
using System.Collections.Generic;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace DbContextModels
{
    public partial class VendorAddress
    {
        public int VendorId { get; set; }
        public int AddressId { get; set; }
        public int AddressTypeId { get; set; }
        public DateTime ModifiedDate { get; set; }
    }
}
