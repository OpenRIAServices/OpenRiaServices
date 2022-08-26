using System;
using System.Collections.Generic;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace EFCoreModels.Northwind
{
    public partial class EmployeeTerritories
    {
        public int EmployeeID { get; set; }
        public string TerritoryID { get; set; }

        public virtual Employees Employee { get; set; }
        public virtual Territory Territory { get; set; }
    }
}
