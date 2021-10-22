using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Configuration;

// These assembly attributes allow us to serialize different CLR types into the same contract
[assembly: ContractNamespace("http://schemas.datacontract.org/2004/07/DataTests.Northwind",
                              ClrNamespace = "CodeFirstModels")]

namespace CodeFirstModels
{
    public partial class EFCoreCFNorthwindEntities : DbContext
    {
        private string _connection;

        public EFCoreCFNorthwindEntities()
            : this("name=Northwind")
        {

        }

        public EFCoreCFNorthwindEntities(string connection)
        {
            _connection = connection;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(_connection);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.Entity<Shipper>()
                        .HasMany(c => c.Orders)
                        .WithOne(c => c.Shipper)
                        .HasForeignKey(c => c.ShipVia);

            modelBuilder.Entity<Order_Detail>().ToTable("Order Details");

            modelBuilder.Entity<Region>().ToTable("Region");

            modelBuilder.Entity<EmployeeTerritory>()
                .HasKey(et => new { et.EmployeeID, et.TerritoryID });

            // In EF Core 3.1 a M2M link entity is required. This should not be needed in EF Core 5 and upwards
            modelBuilder.Entity<EmployeeTerritory>()
                .HasOne(bc => bc.Employee)
                .WithMany(b => b.EmployeeTerritories)
                .HasForeignKey(bc => bc.EmployeeID);

            modelBuilder.Entity<EmployeeTerritory>()
                .HasOne(bc => bc.Territory)
                .WithMany(c => c.EmployeeTerritories)
                .HasForeignKey(bc => bc.TerritoryID);

            modelBuilder.Entity<Region>()
                        .Property(r => r.RegionID)
                        .ValueGeneratedNever();

            modelBuilder.Entity<Region>()
                        .HasKey(r => r.RegionID);

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
