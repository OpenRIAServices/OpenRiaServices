using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace EFCoreModels.Northwind
{
    public partial class EFCoreDbCtxNorthwindEntities : DbContext
    {
        public EFCoreDbCtxNorthwindEntities()
        {
        }

        public EFCoreDbCtxNorthwindEntities(string connectionString)
            : this(BuildOptions(connectionString))
        {
        }

        public EFCoreDbCtxNorthwindEntities(DbContextOptions<EFCoreDbCtxNorthwindEntities> options)
            : base(options)
        {
        }

        private static DbContextOptions<EFCoreDbCtxNorthwindEntities> BuildOptions(string connectionString)
        {
            var optionsBuilder = new DbContextOptionsBuilder<EFCoreDbCtxNorthwindEntities>();
            optionsBuilder.UseSqlServer(connectionString);
            return optionsBuilder.Options;
        }

        public virtual DbSet<Category> Categories { get; set; }
        public virtual DbSet<CustomerCustomerDemo> CustomerCustomerDemo { get; set; }
        public virtual DbSet<CustomerDemographics> CustomerDemographics { get; set; }
        public virtual DbSet<Customer> Customers { get; set; }
        public virtual DbSet<EmployeeTerritories> EmployeeTerritories { get; set; }
        public virtual DbSet<Employees> Employees { get; set; }
        public virtual DbSet<Order_Detail> Order_Details { get; set; }
        public virtual DbSet<Order> Orders { get; set; }
        public virtual DbSet<Product> Products { get; set; }
        public virtual DbSet<Region> Regions { get; set; }
        public virtual DbSet<Shipper> Shippers { get; set; }
        public virtual DbSet<Supplier> Suppliers { get; set; }
        public virtual DbSet<Territory> Territories { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=Northwind;Integrated Security=True;MultipleActiveResultSets=True;Encrypt=false");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(e => e.CategoryID);

                entity.HasIndex(e => e.CategoryName)
                    .HasName("CategoryName");

                entity.Property(e => e.CategoryID).HasColumnName("CategoryID");

                entity.Property(e => e.CategoryName)
                    .IsRequired()
                    .HasMaxLength(15);

                entity.Property(e => e.Description).HasColumnType("ntext");

                entity.Property(e => e.Picture).HasColumnType("image");
            });

            modelBuilder.Entity<CustomerCustomerDemo>(entity =>
            {
                entity.HasKey(e => new { e.CustomerID, e.CustomerTypeID })
                    .IsClustered(false);

                entity.Property(e => e.CustomerID)
                    .HasColumnName("CustomerID")
                    .HasMaxLength(5)
                    .IsFixedLength();

                entity.Property(e => e.CustomerTypeID)
                    .HasColumnName("CustomerTypeID")
                    .HasMaxLength(10)
                    .IsFixedLength();

                entity.HasOne(d => d.Customer)
                    .WithMany(p => p.CustomerCustomerDemo)
                    .HasForeignKey(d => d.CustomerID)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_CustomerCustomerDemo_Customers");

                entity.HasOne(d => d.CustomerType)
                    .WithMany(p => p.CustomerCustomerDemo)
                    .HasForeignKey(d => d.CustomerTypeID)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_CustomerCustomerDemo");
            });

            modelBuilder.Entity<CustomerDemographics>(entity =>
            {
                entity.HasKey(e => e.CustomerTypeID)
                    .IsClustered(false);

                entity.Property(e => e.CustomerTypeID)
                    .HasColumnName("CustomerTypeID")
                    .HasMaxLength(10)
                    .IsFixedLength();

                entity.Property(e => e.CustomerDesc).HasColumnType("ntext");
            });

            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasKey(e => e.CustomerID);

                entity.HasIndex(e => e.City)
                    .HasName("City");

                entity.HasIndex(e => e.CompanyName)
                    .HasName("CompanyName");

                entity.HasIndex(e => e.PostalCode)
                    .HasName("PostalCode");

                entity.HasIndex(e => e.Region)
                    .HasName("Region");

                entity.Property(e => e.CustomerID)
                    .HasColumnName("CustomerID")
                    .HasMaxLength(5)
                    .IsFixedLength()
                    .IsConcurrencyToken();

                entity.Property(e => e.Address).HasMaxLength(60)
                    .IsConcurrencyToken();

                entity.Property(e => e.City).HasMaxLength(15)
                    .IsConcurrencyToken();

                entity.Property(e => e.CompanyName)
                    .IsRequired()
                    .HasMaxLength(40)
                    .IsConcurrencyToken();

                entity.Property(e => e.ContactName).HasMaxLength(30)
                    .IsConcurrencyToken();

                entity.Property(e => e.ContactTitle).HasMaxLength(30)
                    .IsConcurrencyToken();

                entity.Property(e => e.Country).HasMaxLength(15)
                    .IsConcurrencyToken();

                entity.Property(e => e.Fax).HasMaxLength(24)
                    .IsConcurrencyToken();

                entity.Property(e => e.Phone).HasMaxLength(24)
                    .IsConcurrencyToken();

                entity.Property(e => e.PostalCode).HasMaxLength(10)
                    .IsConcurrencyToken();

                entity.Property(e => e.Region).HasMaxLength(15)
                    .IsConcurrencyToken();
            });

            modelBuilder.Entity<EmployeeTerritories>(entity =>
            {
                entity.HasKey(e => new { e.EmployeeID, e.TerritoryID })
                    .IsClustered(false);

                entity.Property(e => e.EmployeeID).HasColumnName("EmployeeID");

                entity.Property(e => e.TerritoryID)
                    .HasColumnName("TerritoryID")
                    .HasMaxLength(20);

                entity.HasOne(d => d.Employee)
                    .WithMany(p => p.EmployeeTerritories)
                    .HasForeignKey(d => d.EmployeeID)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_EmployeeTerritories_Employees");

                entity.HasOne(d => d.Territory)
                    .WithMany(p => p.EmployeeTerritories)
                    .HasForeignKey(d => d.TerritoryID)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_EmployeeTerritories_Territories");
            });

            modelBuilder.Entity<Employees>(entity =>
            {
                entity.HasKey(e => e.EmployeeID);

                entity.HasIndex(e => e.LastName)
                    .HasName("LastName");

                entity.HasIndex(e => e.PostalCode)
                    .HasName("PostalCode");

                entity.Property(e => e.EmployeeID).HasColumnName("EmployeeID");

                entity.Property(e => e.Address).HasMaxLength(60);

                entity.Property(e => e.BirthDate).HasColumnType("datetime");

                entity.Property(e => e.City).HasMaxLength(15);

                entity.Property(e => e.Country).HasMaxLength(15);

                entity.Property(e => e.Extension).HasMaxLength(4);

                entity.Property(e => e.FirstName)
                    .IsRequired()
                    .HasMaxLength(10);

                entity.Property(e => e.HireDate).HasColumnType("datetime");

                entity.Property(e => e.HomePhone).HasMaxLength(24);

                entity.Property(e => e.LastName)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.Notes).HasColumnType("ntext");

                entity.Property(e => e.Photo).HasColumnType("image");

                entity.Property(e => e.PhotoPath).HasMaxLength(255);

                entity.Property(e => e.PostalCode).HasMaxLength(10);

                entity.Property(e => e.Region).HasMaxLength(15);

                entity.Property(e => e.Title).HasMaxLength(30);

                entity.Property(e => e.TitleOfCourtesy).HasMaxLength(25);

                entity.HasOne(d => d.ReportsToNavigation)
                    .WithMany(p => p.InverseReportsToNavigation)
                    .HasForeignKey(d => d.ReportsTo)
                    .HasConstraintName("FK_Employees_Employees");
            });

            modelBuilder.Entity<Order_Detail>(entity =>
            {
                entity.HasKey(e => new { e.OrderID, e.ProductID })
                    .HasName("PK_Order_Details");

                entity.ToTable("Order Details");

                entity.HasIndex(e => e.OrderID)
                    .HasName("OrdersOrder_Details");

                entity.HasIndex(e => e.ProductID)
                    .HasName("ProductsOrder_Details");

                entity.Property(e => e.OrderID).HasColumnName("OrderID");

                entity.Property(e => e.ProductID).HasColumnName("ProductID");

                entity.Property(e => e.Discount).HasDefaultValueSql("(0)");

                entity.Property(e => e.Quantity).HasDefaultValueSql("(1)");

                entity.Property(e => e.UnitPrice)
                    .HasColumnType("money")
                    .HasDefaultValueSql("(0)");

                entity.HasOne(d => d.Order)
                    .WithMany(p => p.Order_Details)
                    .HasForeignKey(d => d.OrderID)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Order_Details_Orders");

                entity.HasOne(d => d.Product)
                    .WithMany(p => p.Order_Details)
                    .HasForeignKey(d => d.ProductID)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Order_Details_Products");
            });

            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(e => e.OrderID);

                entity.HasIndex(e => e.CustomerID)
                    .HasName("CustomersOrders");

                entity.HasIndex(e => e.EmployeeID)
                    .HasName("EmployeesOrders");

                entity.HasIndex(e => e.OrderDate)
                    .HasName("OrderDate");

                entity.HasIndex(e => e.ShipPostalCode)
                    .HasName("ShipPostalCode");

                entity.HasIndex(e => e.ShipVia)
                    .HasName("ShippersOrders");

                entity.HasIndex(e => e.ShippedDate)
                    .HasName("ShippedDate");

                entity.Property(e => e.OrderID).HasColumnName("OrderID");

                entity.Property(e => e.CustomerID)
                    .HasColumnName("CustomerID")
                    .HasMaxLength(5)
                    .IsFixedLength();

                entity.Property(e => e.EmployeeID).HasColumnName("EmployeeID");

                entity.Property(e => e.Freight)
                    .HasColumnType("money")
                    .HasDefaultValueSql("(0)");

                entity.Property(e => e.OrderDate).HasColumnType("datetime");

                entity.Property(e => e.RequiredDate).HasColumnType("datetime");

                entity.Property(e => e.ShipAddress).HasMaxLength(60);

                entity.Property(e => e.ShipCity).HasMaxLength(15);

                entity.Property(e => e.ShipCountry).HasMaxLength(15);

                entity.Property(e => e.ShipName).HasMaxLength(40);

                entity.Property(e => e.ShipPostalCode).HasMaxLength(10);

                entity.Property(e => e.ShipRegion).HasMaxLength(15);

                entity.Property(e => e.ShippedDate).HasColumnType("datetime");

                entity.HasOne(d => d.Customer)
                    .WithMany(p => p.Orders)
                    .HasForeignKey(d => d.CustomerID)
                    .HasConstraintName("FK_Orders_Customers");

                entity.HasOne(d => d.Employee)
                    .WithMany(p => p.Orders)
                    .HasForeignKey(d => d.EmployeeID)
                    .HasConstraintName("FK_Orders_Employees");

                entity.HasOne(d => d.ShipViaNavigation)
                    .WithMany(p => p.Orders)
                    .HasForeignKey(d => d.ShipVia)
                    .HasConstraintName("FK_Orders_Shippers");

                entity.Ignore(e => e.FormattedName);
            });

            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.ProductID);

                entity.HasIndex(e => e.CategoryID)
                    .HasName("CategoryID");

                entity.HasIndex(e => e.ProductName)
                    .HasName("ProductName");

                entity.HasIndex(e => e.SupplierID)
                    .HasName("SuppliersProducts");

                entity.Property(e => e.ProductID).HasColumnName("ProductID");

                entity.Property(e => e.CategoryID).HasColumnName("CategoryID");

                entity.Property(e => e.Discontinued)
                    .IsRequired();

                entity.Property(e => e.ProductName)
                    .IsRequired()
                    .HasMaxLength(40);

                entity.Property(e => e.QuantityPerUnit).HasMaxLength(20);

                entity.Property(e => e.ReorderLevel).HasDefaultValueSql("(0)");

                entity.Property(e => e.SupplierID).HasColumnName("SupplierID");

                entity.Property(e => e.UnitPrice)
                    .HasColumnType("money")
                    .HasDefaultValueSql("(0)");

                entity.Property(e => e.UnitsInStock).HasDefaultValueSql("(0)");

                entity.Property(e => e.UnitsOnOrder).HasDefaultValueSql("(0)");

                entity.HasOne(d => d.Category)
                    .WithMany(p => p.Products)
                    .HasForeignKey(d => d.CategoryID)
                    .HasConstraintName("FK_Products_Categories");

                entity.HasOne(d => d.Supplier)
                    .WithMany(p => p.Products)
                    .HasForeignKey(d => d.SupplierID)
                    .HasConstraintName("FK_Products_Suppliers");

                // Only used for tets
                entity.Ignore(e => e.ResolveMethod);
            });

            modelBuilder.Entity<Region>(entity =>
            {
                entity.ToTable("Region");

                entity.HasKey(e => e.RegionID)
                    .IsClustered(false);

                entity.Property(e => e.RegionID)
                    .HasColumnName("RegionID")
                    .ValueGeneratedNever();

                entity.Property(e => e.RegionDescription)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsFixedLength();
            });

            modelBuilder.Entity<Shipper>(entity =>
            {
                entity.HasKey(e => e.ShipperID);

                entity.Property(e => e.ShipperID).HasColumnName("ShipperID");

                entity.Property(e => e.CompanyName)
                    .IsRequired()
                    .HasMaxLength(40);

                entity.Property(e => e.Phone).HasMaxLength(24);
            });

            modelBuilder.Entity<Supplier>(entity =>
            {
                entity.HasKey(e => e.SupplierID);

                entity.HasIndex(e => e.CompanyName)
                    .HasName("CompanyName");

                entity.HasIndex(e => e.PostalCode)
                    .HasName("PostalCode");

                entity.Property(e => e.SupplierID).HasColumnName("SupplierID");

                entity.Property(e => e.Address).HasMaxLength(60);

                entity.Property(e => e.City).HasMaxLength(15);

                entity.Property(e => e.CompanyName)
                    .IsRequired()
                    .HasMaxLength(40);

                entity.Property(e => e.ContactName).HasMaxLength(30);

                entity.Property(e => e.ContactTitle).HasMaxLength(30);

                entity.Property(e => e.Country).HasMaxLength(15);

                entity.Property(e => e.Fax).HasMaxLength(24);

                entity.Property(e => e.HomePage).HasColumnType("ntext");

                entity.Property(e => e.Phone).HasMaxLength(24);

                entity.Property(e => e.PostalCode).HasMaxLength(10);

                entity.Property(e => e.Region).HasMaxLength(15);
            });

            modelBuilder.Entity<Territory>(entity =>
            {
                entity.HasKey(e => e.TerritoryID)
                    .IsClustered(false);

                entity.Property(e => e.TerritoryID)
                    .HasColumnName("TerritoryID")
                    .HasMaxLength(20);

                entity.Property(e => e.RegionID).HasColumnName("RegionID");

                entity.Property(e => e.TerritoryDescription)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsFixedLength();

                entity.HasOne(d => d.Region)
                    .WithMany(p => p.Territories)
                    .HasForeignKey(d => d.RegionID)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Territories_Region");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
