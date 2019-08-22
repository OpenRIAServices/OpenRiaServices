namespace CodeFirstModels
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    
    public partial class Territory
    {
        public Territory()
        {
            this.Employees = new HashSet<Employee>();
        }

        [StringLength(20)]
        public string TerritoryID { get; set; }
        [StringLength(50)]
        public string TerritoryDescription { get; set; }
        public int RegionID { get; set; }
    
        public virtual Region Region { get; set; }
        public virtual ICollection<Employee> Employees { get; set; }
    }
}
