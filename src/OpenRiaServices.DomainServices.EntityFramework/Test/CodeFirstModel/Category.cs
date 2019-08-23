namespace CodeFirstModels
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    
    public partial class Category
    {
        public Category()
        {
            this.Products = new HashSet<Product>();
        }

        [ConcurrencyCheck]
        public int CategoryID { get; set; }
        [Required]
        [StringLength(15)]
        public string CategoryName { get; set; }
        public string Description { get; set; }
        public byte[] Picture { get; set; }
    
        public virtual ICollection<Product> Products { get; set; }
    }
}
