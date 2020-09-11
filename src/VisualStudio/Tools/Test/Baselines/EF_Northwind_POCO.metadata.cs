
namespace NorthwindPOCOModelBuddy
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using NorthwindPOCOModel;
    using OpenRiaServices.Hosting;
    using OpenRiaServices.Server;
    
    
    // The MetadataTypeAttribute identifies CategoryBuddyMetadata as the class
    // that carries additional metadata for the CategoryBuddy class.
    [MetadataTypeAttribute(typeof(CategoryBuddy.CategoryBuddyMetadata))]
    public partial class CategoryBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the Category class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class CategoryBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private CategoryBuddyMetadata()
            {
            }
            
            public int CategoryID { get; set; }
            
            public string CategoryName { get; set; }
            
            public string Description { get; set; }
            
            public byte[] Picture { get; set; }
            
            public List<Product> Products { get; set; }
        }
    }
    
    // The MetadataTypeAttribute identifies ProductBuddyMetadata as the class
    // that carries additional metadata for the ProductBuddy class.
    [MetadataTypeAttribute(typeof(ProductBuddy.ProductBuddyMetadata))]
    public partial class ProductBuddy
    {
        
        // This class allows you to attach custom attributes to properties
        // of the Product class.
        //
        // For example, the following marks the Xyz property as a
        // required property and specifies the format for valid values:
        //    [Required]
        //    [RegularExpression("[A-Z][A-Za-z0-9]*")]
        //    [StringLength(32)]
        //    public string Xyz { get; set; }
        internal sealed class ProductBuddyMetadata
        {
            
            // Metadata classes are not meant to be instantiated.
            private ProductBuddyMetadata()
            {
            }
            
            public Category Category { get; set; }
            
            public Nullable<int> CategoryID { get; set; }
            
            public bool Discontinued { get; set; }
            
            public int ProductID { get; set; }
            
            public string ProductName { get; set; }
            
            public string QuantityPerUnit { get; set; }
            
            public Nullable<short> ReorderLevel { get; set; }
            
            public Nullable<int> SupplierID { get; set; }
            
            public Nullable<decimal> UnitPrice { get; set; }
            
            public Nullable<short> UnitsInStock { get; set; }
            
            public Nullable<short> UnitsOnOrder { get; set; }
        }
    }
}
