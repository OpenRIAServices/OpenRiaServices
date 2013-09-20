extern alias EntityFramework;

using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Runtime.Serialization;

// These assembly attributes allow us to serialize different CLR types into the same contract
[assembly: ContractNamespace("http://schemas.datacontract.org/2004/07/DataTests.Northwind",
                              ClrNamespace = "CodeFirstModels")]

namespace CodeFirstModels
{
    using DatabaseGeneratedOption = EntityFramework::System.ComponentModel.DataAnnotations.DatabaseGeneratedOption;
    public partial class EFCFNorthwindEntities : DbContext
    {
        public EFCFNorthwindEntities()
            : base("Server=AlexAppFxSS02;Initial Catalog=Northwind;Persist Security Info=True;User ID=dbi;Password=!Password1;MultipleActiveResultSets=True")
        {
        }

        public EFCFNorthwindEntities(string connection)
            : base(connection)
        {
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.Entity<Shipper>()
                        .HasMany(c => c.Orders)
                        .WithOptional(c => c.Shipper)
                        .HasForeignKey(c => c.ShipVia);
            modelBuilder.Entity<Order_Detail>().ToTable("Order Details");
            modelBuilder.Entity<Region>().ToTable("Region");

            modelBuilder.Entity<Territory>().HasMany(t => t.Employees)
                        .WithMany(e => e.Territories)
                        .Map(m =>
                            {
                                m.ToTable("EmployeeTerritories");
                                m.MapLeftKey("TerritoryID");
                                m.MapRightKey("EmployeeID");
                            });
            
            modelBuilder.Entity<Region>().HasKey(r => r.RegionID)
                        .Property(r => r.RegionID)
                        .HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);

        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<CustomerDemographic> CustomerDemographics { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Order_Detail> Order_Details { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Region> Regions { get; set; }
        public DbSet<Shipper> Shippers { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Territory> Territories { get; set; }
    }
}