using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using OpenRiaServices.Server;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace EFCoreModels.Northwind
{
    public partial class Region
    {
        public Region()
        {
            Territories = new HashSet<Territory>();
        }

        public int RegionID { get; set; }
        public string RegionDescription { get; set; }

        [Composition]
        [Include]
        public virtual ICollection<Territory> Territories { get; set; }
    }
}
