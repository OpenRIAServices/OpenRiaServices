using System.Collections.Generic;
using System.Data.Entity.Core.Objects;
using System.Runtime.Serialization;
using System;

// These assembly attributes allow us to serialize different CLR types into the same contract
[assembly: ContractNamespace("http://schemas.datacontract.org/2004/07/DataTests.Northwind",
                              ClrNamespace = "NorthwindPOCOModel")]
namespace NorthwindPOCOModel
{
    /// <summary>
    /// Hand crafted POCO ObjectContext. These entity types are matched to the edmx metadata
    /// by convention - the names and members must match up.
    /// </summary>
    public class NorthwindEntities : ObjectContext
    {
        private readonly ObjectSet<Category> _categories;
        private readonly ObjectSet<Product> _products;

        public NorthwindEntities(string connectionString) 
            : base(connectionString)
        {
            this.ContextOptions.LazyLoadingEnabled = true;
            _categories = CreateObjectSet<Category>();
            _products = CreateObjectSet<Product>();
        }

        public NorthwindEntities()
            : base("name=NorthwindPOCOEntities", "NorthwindEntities")
        {
            this.ContextOptions.LazyLoadingEnabled = true;
            _products = CreateObjectSet<Product>();
            _categories = CreateObjectSet<Category>();
        }

        public ObjectSet<Category> Categories
        {
            get
            {
                return _categories;
            }
        }


        public ObjectSet<Product> Products
        {
            get
            {
                return _products;
            }
        }

    }

    public class Category
    {
        private List<Product> _products = new List<Product>();

        public int CategoryID { get; set; }
        public string CategoryName { get; set; }
        public string Description { get; set; }
        public byte[] Picture { get; set; }
        public List<Product> Products 
        {
            get
            {
                return this._products;
            }
            set
            {
                this._products = value;
            }
        }
    }

    public class Product
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public Nullable<int> SupplierID { get; set; }
        public string QuantityPerUnit { get; set; }
        public Nullable<decimal> UnitPrice { get; set; }
        public Nullable<short> UnitsInStock { get; set; }
        public Nullable<short> UnitsOnOrder { get; set; }
        public Nullable<short> ReorderLevel { get; set; }
        public bool Discontinued { get; set; }
        public Nullable<int> CategoryID { get; set; }
        public Category Category { get; set; }
    }
}
