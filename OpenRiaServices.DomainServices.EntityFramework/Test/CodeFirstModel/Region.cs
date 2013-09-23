namespace CodeFirstModels
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using OpenRiaServices.DomainServices.Server;
    
    public partial class Region
    {
        public Region()
        {
            this.Territories = new HashSet<Territory>();
        }
    
        public int RegionID { get; set; }
        [StringLength(50)]
        public string RegionDescription { get; set; }

        [Composition]
        [Include]
        public virtual ICollection<Territory> Territories { get; set; }
    }
}
