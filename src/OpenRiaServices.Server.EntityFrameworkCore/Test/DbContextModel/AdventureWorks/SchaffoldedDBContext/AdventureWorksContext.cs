using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace DbContextModels.AdventureWorksEFCoreScaffolded
{
    public partial class AdventureWorksContext : DbContext
    {
        public AdventureWorksContext()
        {
        }

        public AdventureWorksContext(DbContextOptions<AdventureWorksContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Address> Address { get; set; }
        public virtual DbSet<AddressType> AddressType { get; set; }
        public virtual DbSet<AwbuildVersion> AwbuildVersion { get; set; }
        public virtual DbSet<BillOfMaterials> BillOfMaterials { get; set; }
        public virtual DbSet<Contact> Contact { get; set; }
        public virtual DbSet<ContactCreditCard> ContactCreditCard { get; set; }
        public virtual DbSet<ContactType> ContactType { get; set; }
        public virtual DbSet<CountryRegion> CountryRegion { get; set; }
        public virtual DbSet<CountryRegionCurrency> CountryRegionCurrency { get; set; }
        public virtual DbSet<CreditCard> CreditCard { get; set; }
        public virtual DbSet<Culture> Culture { get; set; }
        public virtual DbSet<Currency> Currency { get; set; }
        public virtual DbSet<CurrencyRate> CurrencyRate { get; set; }
        public virtual DbSet<Customer> Customer { get; set; }
        public virtual DbSet<CustomerAddress> CustomerAddress { get; set; }
        public virtual DbSet<DatabaseLog> DatabaseLog { get; set; }
        public virtual DbSet<Department> Department { get; set; }
        public virtual DbSet<Document> Document { get; set; }
        public virtual DbSet<Employee> Employee { get; set; }
        public virtual DbSet<EmployeeAddress> EmployeeAddress { get; set; }
        public virtual DbSet<EmployeeDepartmentHistory> EmployeeDepartmentHistory { get; set; }
        public virtual DbSet<EmployeePayHistory> EmployeePayHistory { get; set; }
        public virtual DbSet<ErrorLog> ErrorLog { get; set; }
        public virtual DbSet<Illustration> Illustration { get; set; }
        public virtual DbSet<Individual> Individual { get; set; }
        public virtual DbSet<JobCandidate> JobCandidate { get; set; }
        public virtual DbSet<Location> Location { get; set; }
        public virtual DbSet<Product> Product { get; set; }
        public virtual DbSet<ProductCategory> ProductCategory { get; set; }
        public virtual DbSet<ProductCostHistory> ProductCostHistory { get; set; }
        public virtual DbSet<ProductDescription> ProductDescription { get; set; }
        public virtual DbSet<ProductDocument> ProductDocument { get; set; }
        public virtual DbSet<ProductInventory> ProductInventory { get; set; }
        public virtual DbSet<ProductListPriceHistory> ProductListPriceHistory { get; set; }
        public virtual DbSet<ProductModel> ProductModel { get; set; }
        public virtual DbSet<ProductModelIllustration> ProductModelIllustration { get; set; }
        public virtual DbSet<ProductModelProductDescriptionCulture> ProductModelProductDescriptionCulture { get; set; }
        public virtual DbSet<ProductPhoto> ProductPhoto { get; set; }
        public virtual DbSet<ProductProductPhoto> ProductProductPhoto { get; set; }
        public virtual DbSet<ProductReview> ProductReview { get; set; }
        public virtual DbSet<ProductSubcategory> ProductSubcategory { get; set; }
        public virtual DbSet<ProductVendor> ProductVendor { get; set; }
        public virtual DbSet<PurchaseOrderDetail> PurchaseOrderDetail { get; set; }
        public virtual DbSet<PurchaseOrderHeader> PurchaseOrderHeader { get; set; }
        public virtual DbSet<SalesOrderDetail> SalesOrderDetail { get; set; }
        public virtual DbSet<SalesOrderHeader> SalesOrderHeader { get; set; }
        public virtual DbSet<SalesOrderHeaderSalesReason> SalesOrderHeaderSalesReason { get; set; }
        public virtual DbSet<SalesPerson> SalesPerson { get; set; }
        public virtual DbSet<SalesPersonQuotaHistory> SalesPersonQuotaHistory { get; set; }
        public virtual DbSet<SalesReason> SalesReason { get; set; }
        public virtual DbSet<SalesTaxRate> SalesTaxRate { get; set; }
        public virtual DbSet<SalesTerritory> SalesTerritory { get; set; }
        public virtual DbSet<SalesTerritoryHistory> SalesTerritoryHistory { get; set; }
        public virtual DbSet<ScrapReason> ScrapReason { get; set; }
        public virtual DbSet<Shift> Shift { get; set; }
        public virtual DbSet<ShipMethod> ShipMethod { get; set; }
        public virtual DbSet<ShoppingCartItem> ShoppingCartItem { get; set; }
        public virtual DbSet<SpecialOffer> SpecialOffer { get; set; }
        public virtual DbSet<SpecialOfferProduct> SpecialOfferProduct { get; set; }
        public virtual DbSet<StateProvince> StateProvince { get; set; }
        public virtual DbSet<Store> Store { get; set; }
        public virtual DbSet<StoreContact> StoreContact { get; set; }
        public virtual DbSet<TransactionHistory> TransactionHistory { get; set; }
        public virtual DbSet<TransactionHistoryArchive> TransactionHistoryArchive { get; set; }
        public virtual DbSet<UnitMeasure> UnitMeasure { get; set; }
        public virtual DbSet<Vendor> Vendor { get; set; }
        public virtual DbSet<VendorAddress> VendorAddress { get; set; }
        public virtual DbSet<VendorContact> VendorContact { get; set; }
        public virtual DbSet<WorkOrder> WorkOrder { get; set; }
        public virtual DbSet<WorkOrderRouting> WorkOrderRouting { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. See http://go.microsoft.com/fwlink/?LinkId=723263 for guidance on storing connection strings.
                optionsBuilder.UseSqlServer("server=.\\SQLEXPRESS;initial catalog=AdventureWorks;Integrated Security=SSPI; MultipleActiveResultSets=true");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Address>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("Address", "Person");

                entity.Property(e => e.AddressId).HasColumnName("AddressID");

                entity.Property(e => e.AddressLine1)
                    .IsRequired()
                    .HasMaxLength(60);

                entity.Property(e => e.AddressLine2).HasMaxLength(60);

                entity.Property(e => e.City)
                    .IsRequired()
                    .HasMaxLength(30);

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.PostalCode)
                    .IsRequired()
                    .HasMaxLength(15);

                entity.Property(e => e.Rowguid).HasColumnName("rowguid");

                entity.Property(e => e.StateProvinceId).HasColumnName("StateProvinceID");
            });

            modelBuilder.Entity<AddressType>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("AddressType", "Person");

                entity.Property(e => e.AddressTypeId).HasColumnName("AddressTypeID");

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Rowguid).HasColumnName("rowguid");
            });

            modelBuilder.Entity<AwbuildVersion>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("AWBuildVersion");

                entity.Property(e => e.DatabaseVersion)
                    .IsRequired()
                    .HasColumnName("Database Version")
                    .HasMaxLength(25);

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.SystemInformationId).HasColumnName("SystemInformationID");

                entity.Property(e => e.VersionDate).HasColumnType("smalldatetime");
            });

            modelBuilder.Entity<BillOfMaterials>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("BillOfMaterials", "Production");

                entity.Property(e => e.BillOfMaterialsId).HasColumnName("BillOfMaterialsID");

                entity.Property(e => e.Bomlevel).HasColumnName("BOMLevel");

                entity.Property(e => e.ComponentId).HasColumnName("ComponentID");

                entity.Property(e => e.EndDate).HasColumnType("smalldatetime");

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.PerAssemblyQty).HasColumnType("decimal(8, 2)");

                entity.Property(e => e.ProductAssemblyId).HasColumnName("ProductAssemblyID");

                entity.Property(e => e.StartDate).HasColumnType("smalldatetime");

                entity.Property(e => e.UnitMeasureCode)
                    .IsRequired()
                    .HasMaxLength(3)
                    .IsFixedLength();
            });

            modelBuilder.Entity<Contact>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("Contact", "Person");

                entity.Property(e => e.AdditionalContactInfo).HasColumnType("xml");

                entity.Property(e => e.ContactId).HasColumnName("ContactID");

                entity.Property(e => e.EmailAddress).HasMaxLength(50);

                entity.Property(e => e.FirstName)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.LastName)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.MiddleName).HasMaxLength(50);

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.PasswordHash)
                    .IsRequired()
                    .HasMaxLength(128)
                    .IsUnicode(false);

                entity.Property(e => e.PasswordSalt)
                    .IsRequired()
                    .HasMaxLength(10)
                    .IsUnicode(false);

                entity.Property(e => e.Phone).HasMaxLength(25);

                entity.Property(e => e.Rowguid).HasColumnName("rowguid");

                entity.Property(e => e.Suffix).HasMaxLength(10);

                entity.Property(e => e.Title).HasMaxLength(8);
            });

            modelBuilder.Entity<ContactCreditCard>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("ContactCreditCard", "Sales");

                entity.Property(e => e.ContactId).HasColumnName("ContactID");

                entity.Property(e => e.CreditCardId).HasColumnName("CreditCardID");

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");
            });

            modelBuilder.Entity<ContactType>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("ContactType", "Person");

                entity.Property(e => e.ContactTypeId).HasColumnName("ContactTypeID");

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(50);
            });

            modelBuilder.Entity<CountryRegion>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("CountryRegion", "Person");

                entity.Property(e => e.CountryRegionCode)
                    .IsRequired()
                    .HasMaxLength(3);

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(50);
            });

            modelBuilder.Entity<CountryRegionCurrency>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("CountryRegionCurrency", "Sales");

                entity.Property(e => e.CountryRegionCode)
                    .IsRequired()
                    .HasMaxLength(3);

                entity.Property(e => e.CurrencyCode)
                    .IsRequired()
                    .HasMaxLength(3)
                    .IsFixedLength();

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");
            });

            modelBuilder.Entity<CreditCard>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("CreditCard", "Sales");

                entity.Property(e => e.CardNumber)
                    .IsRequired()
                    .HasMaxLength(25);

                entity.Property(e => e.CardType)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.CreditCardId).HasColumnName("CreditCardID");

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");
            });

            modelBuilder.Entity<Culture>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("Culture", "Production");

                entity.Property(e => e.CultureId)
                    .IsRequired()
                    .HasColumnName("CultureID")
                    .HasMaxLength(6)
                    .IsFixedLength();

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(50);
            });

            modelBuilder.Entity<Currency>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("Currency", "Sales");

                entity.Property(e => e.CurrencyCode)
                    .IsRequired()
                    .HasMaxLength(3)
                    .IsFixedLength();

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(50);
            });

            modelBuilder.Entity<CurrencyRate>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("CurrencyRate", "Sales");

                entity.Property(e => e.AverageRate).HasColumnType("money");

                entity.Property(e => e.CurrencyRateDate).HasColumnType("smalldatetime");

                entity.Property(e => e.CurrencyRateId).HasColumnName("CurrencyRateID");

                entity.Property(e => e.EndOfDayRate).HasColumnType("money");

                entity.Property(e => e.FromCurrencyCode)
                    .IsRequired()
                    .HasMaxLength(3)
                    .IsFixedLength();

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.ToCurrencyCode)
                    .IsRequired()
                    .HasMaxLength(3)
                    .IsFixedLength();
            });

            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("Customer", "Sales");

                entity.Property(e => e.AccountNumber)
                    .IsRequired()
                    .HasMaxLength(10)
                    .IsUnicode(false);

                entity.Property(e => e.CustomerId).HasColumnName("CustomerID");

                entity.Property(e => e.CustomerType)
                    .IsRequired()
                    .HasMaxLength(1)
                    .IsFixedLength();

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.Rowguid).HasColumnName("rowguid");

                entity.Property(e => e.TerritoryId).HasColumnName("TerritoryID");
            });

            modelBuilder.Entity<CustomerAddress>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("CustomerAddress", "Sales");

                entity.Property(e => e.AddressId).HasColumnName("AddressID");

                entity.Property(e => e.AddressTypeId).HasColumnName("AddressTypeID");

                entity.Property(e => e.CustomerId).HasColumnName("CustomerID");

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.Rowguid).HasColumnName("rowguid");
            });

            modelBuilder.Entity<DatabaseLog>(entity =>
            {
                entity.HasNoKey();

                entity.Property(e => e.DatabaseLogId).HasColumnName("DatabaseLogID");

                entity.Property(e => e.DatabaseUser)
                    .IsRequired()
                    .HasMaxLength(128);

                entity.Property(e => e.Event)
                    .IsRequired()
                    .HasMaxLength(128);

                entity.Property(e => e.Object).HasMaxLength(128);

                entity.Property(e => e.PostTime).HasColumnType("smalldatetime");

                entity.Property(e => e.Schema).HasMaxLength(128);

                entity.Property(e => e.Tsql)
                    .IsRequired()
                    .HasColumnName("TSQL");

                entity.Property(e => e.XmlEvent)
                    .IsRequired()
                    .HasColumnType("xml");
            });

            modelBuilder.Entity<Department>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("Department", "HumanResources");

                entity.Property(e => e.DepartmentId).HasColumnName("DepartmentID");

                entity.Property(e => e.GroupName)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(50);
            });

            modelBuilder.Entity<Document>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("Document", "Production");

                entity.Property(e => e.Document1).HasColumnName("Document");

                entity.Property(e => e.DocumentId).HasColumnName("DocumentID");

                entity.Property(e => e.FileExtension)
                    .IsRequired()
                    .HasMaxLength(8);

                entity.Property(e => e.FileName)
                    .IsRequired()
                    .HasMaxLength(400);

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.Revision)
                    .IsRequired()
                    .HasMaxLength(5)
                    .IsFixedLength();

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(50);
            });

            modelBuilder.Entity<Employee>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("Employee", "HumanResources");

                entity.Property(e => e.BirthDate).HasColumnType("smalldatetime");

                entity.Property(e => e.ContactId).HasColumnName("ContactID");

                entity.Property(e => e.EmployeeId).HasColumnName("EmployeeID");

                entity.Property(e => e.Gender)
                    .IsRequired()
                    .HasMaxLength(1)
                    .IsFixedLength();

                entity.Property(e => e.HireDate).HasColumnType("smalldatetime");

                entity.Property(e => e.LoginId)
                    .IsRequired()
                    .HasColumnName("LoginID")
                    .HasMaxLength(256);

                entity.Property(e => e.ManagerId).HasColumnName("ManagerID");

                entity.Property(e => e.MaritalStatus)
                    .IsRequired()
                    .HasMaxLength(1)
                    .IsFixedLength();

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.NationalIdnumber)
                    .IsRequired()
                    .HasColumnName("NationalIDNumber")
                    .HasMaxLength(15);

                entity.Property(e => e.Rowguid).HasColumnName("rowguid");

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(50);
            });

            modelBuilder.Entity<EmployeeAddress>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("EmployeeAddress", "HumanResources");

                entity.Property(e => e.AddressId).HasColumnName("AddressID");

                entity.Property(e => e.EmployeeId).HasColumnName("EmployeeID");

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.Rowguid).HasColumnName("rowguid");
            });

            modelBuilder.Entity<EmployeeDepartmentHistory>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("EmployeeDepartmentHistory", "HumanResources");

                entity.Property(e => e.DepartmentId).HasColumnName("DepartmentID");

                entity.Property(e => e.EmployeeId).HasColumnName("EmployeeID");

                entity.Property(e => e.EndDate).HasColumnType("smalldatetime");

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.ShiftId).HasColumnName("ShiftID");

                entity.Property(e => e.StartDate).HasColumnType("smalldatetime");
            });

            modelBuilder.Entity<EmployeePayHistory>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("EmployeePayHistory", "HumanResources");

                entity.Property(e => e.EmployeeId).HasColumnName("EmployeeID");

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.Rate).HasColumnType("money");

                entity.Property(e => e.RateChangeDate).HasColumnType("smalldatetime");
            });

            modelBuilder.Entity<ErrorLog>(entity =>
            {
                entity.HasNoKey();

                entity.Property(e => e.ErrorLogId).HasColumnName("ErrorLogID");

                entity.Property(e => e.ErrorMessage)
                    .IsRequired()
                    .HasMaxLength(4000);

                entity.Property(e => e.ErrorProcedure).HasMaxLength(126);

                entity.Property(e => e.ErrorTime).HasColumnType("smalldatetime");

                entity.Property(e => e.UserName)
                    .IsRequired()
                    .HasMaxLength(128);
            });

            modelBuilder.Entity<Illustration>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("Illustration", "Production");

                entity.Property(e => e.Diagram).HasColumnType("xml");

                entity.Property(e => e.IllustrationId).HasColumnName("IllustrationID");

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");
            });

            modelBuilder.Entity<Individual>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("Individual", "Sales");

                entity.Property(e => e.ContactId).HasColumnName("ContactID");

                entity.Property(e => e.CustomerId).HasColumnName("CustomerID");

                entity.Property(e => e.Demographics).HasColumnType("xml");

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");
            });

            modelBuilder.Entity<JobCandidate>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("JobCandidate", "HumanResources");

                entity.Property(e => e.EmployeeId).HasColumnName("EmployeeID");

                entity.Property(e => e.JobCandidateId).HasColumnName("JobCandidateID");

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.Resume).HasColumnType("xml");
            });

            modelBuilder.Entity<Location>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("Location", "Production");

                entity.Property(e => e.Availability).HasColumnType("decimal(8, 2)");

                entity.Property(e => e.CostRate).HasColumnType("money");

                entity.Property(e => e.LocationId).HasColumnName("LocationID");

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(50);
            });

            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("Product", "Production");

                entity.Property(e => e.Class)
                    .HasMaxLength(2)
                    .IsFixedLength();

                entity.Property(e => e.Color).HasMaxLength(15);

                entity.Property(e => e.DiscontinuedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.ListPrice).HasColumnType("money");

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.ProductId).HasColumnName("ProductID");

                entity.Property(e => e.ProductLine)
                    .HasMaxLength(2)
                    .IsFixedLength();

                entity.Property(e => e.ProductModelId).HasColumnName("ProductModelID");

                entity.Property(e => e.ProductNumber)
                    .IsRequired()
                    .HasMaxLength(25);

                entity.Property(e => e.ProductSubcategoryId).HasColumnName("ProductSubcategoryID");

                entity.Property(e => e.Rowguid).HasColumnName("rowguid");

                entity.Property(e => e.SellEndDate).HasColumnType("smalldatetime");

                entity.Property(e => e.SellStartDate).HasColumnType("smalldatetime");

                entity.Property(e => e.Size).HasMaxLength(5);

                entity.Property(e => e.SizeUnitMeasureCode)
                    .HasMaxLength(3)
                    .IsFixedLength();

                entity.Property(e => e.StandardCost).HasColumnType("money");

                entity.Property(e => e.Style)
                    .HasMaxLength(2)
                    .IsFixedLength();

                entity.Property(e => e.Weight).HasColumnType("decimal(8, 2)");

                entity.Property(e => e.WeightUnitMeasureCode)
                    .HasMaxLength(3)
                    .IsFixedLength();
            });

            modelBuilder.Entity<ProductCategory>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("ProductCategory", "Production");

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.ProductCategoryId).HasColumnName("ProductCategoryID");

                entity.Property(e => e.Rowguid).HasColumnName("rowguid");
            });

            modelBuilder.Entity<ProductCostHistory>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("ProductCostHistory", "Production");

                entity.Property(e => e.EndDate).HasColumnType("smalldatetime");

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.ProductId).HasColumnName("ProductID");

                entity.Property(e => e.StandardCost).HasColumnType("money");

                entity.Property(e => e.StartDate).HasColumnType("smalldatetime");
            });

            modelBuilder.Entity<ProductDescription>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("ProductDescription", "Production");

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(400);

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.ProductDescriptionId).HasColumnName("ProductDescriptionID");

                entity.Property(e => e.Rowguid).HasColumnName("rowguid");
            });

            modelBuilder.Entity<ProductDocument>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("ProductDocument", "Production");

                entity.Property(e => e.DocumentId).HasColumnName("DocumentID");

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.ProductId).HasColumnName("ProductID");
            });

            modelBuilder.Entity<ProductInventory>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("ProductInventory", "Production");

                entity.Property(e => e.LocationId).HasColumnName("LocationID");

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.ProductId).HasColumnName("ProductID");

                entity.Property(e => e.Rowguid).HasColumnName("rowguid");

                entity.Property(e => e.Shelf)
                    .IsRequired()
                    .HasMaxLength(10);
            });

            modelBuilder.Entity<ProductListPriceHistory>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("ProductListPriceHistory", "Production");

                entity.Property(e => e.EndDate).HasColumnType("smalldatetime");

                entity.Property(e => e.ListPrice).HasColumnType("money");

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.ProductId).HasColumnName("ProductID");

                entity.Property(e => e.StartDate).HasColumnType("smalldatetime");
            });

            modelBuilder.Entity<ProductModel>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("ProductModel", "Production");

                entity.Property(e => e.CatalogDescription).HasColumnType("xml");

                entity.Property(e => e.Instructions).HasColumnType("xml");

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.ProductModelId).HasColumnName("ProductModelID");

                entity.Property(e => e.Rowguid).HasColumnName("rowguid");
            });

            modelBuilder.Entity<ProductModelIllustration>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("ProductModelIllustration", "Production");

                entity.Property(e => e.IllustrationId).HasColumnName("IllustrationID");

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.ProductModelId).HasColumnName("ProductModelID");
            });

            modelBuilder.Entity<ProductModelProductDescriptionCulture>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("ProductModelProductDescriptionCulture", "Production");

                entity.Property(e => e.CultureId)
                    .IsRequired()
                    .HasColumnName("CultureID")
                    .HasMaxLength(6)
                    .IsFixedLength();

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.ProductDescriptionId).HasColumnName("ProductDescriptionID");

                entity.Property(e => e.ProductModelId).HasColumnName("ProductModelID");
            });

            modelBuilder.Entity<ProductPhoto>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("ProductPhoto", "Production");

                entity.Property(e => e.LargePhotoFileName).HasMaxLength(50);

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.ProductPhotoId).HasColumnName("ProductPhotoID");

                entity.Property(e => e.ThumbnailPhotoFileName).HasMaxLength(50);
            });

            modelBuilder.Entity<ProductProductPhoto>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("ProductProductPhoto", "Production");

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.ProductId).HasColumnName("ProductID");

                entity.Property(e => e.ProductPhotoId).HasColumnName("ProductPhotoID");
            });

            modelBuilder.Entity<ProductReview>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("ProductReview", "Production");

                entity.Property(e => e.Comments).HasMaxLength(3850);

                entity.Property(e => e.EmailAddress)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.ProductId).HasColumnName("ProductID");

                entity.Property(e => e.ProductReviewId).HasColumnName("ProductReviewID");

                entity.Property(e => e.ReviewDate).HasColumnType("smalldatetime");

                entity.Property(e => e.ReviewerName)
                    .IsRequired()
                    .HasMaxLength(50);
            });

            modelBuilder.Entity<ProductSubcategory>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("ProductSubcategory", "Production");

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.ProductCategoryId).HasColumnName("ProductCategoryID");

                entity.Property(e => e.ProductSubcategoryId).HasColumnName("ProductSubcategoryID");

                entity.Property(e => e.Rowguid).HasColumnName("rowguid");
            });

            modelBuilder.Entity<ProductVendor>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("ProductVendor", "Purchasing");

                entity.Property(e => e.LastReceiptCost).HasColumnType("money");

                entity.Property(e => e.LastReceiptDate).HasColumnType("smalldatetime");

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.ProductId).HasColumnName("ProductID");

                entity.Property(e => e.StandardPrice).HasColumnType("money");

                entity.Property(e => e.UnitMeasureCode)
                    .IsRequired()
                    .HasMaxLength(3)
                    .IsFixedLength();

                entity.Property(e => e.VendorId).HasColumnName("VendorID");
            });

            modelBuilder.Entity<PurchaseOrderDetail>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("PurchaseOrderDetail", "Purchasing");

                entity.Property(e => e.DueDate).HasColumnType("smalldatetime");

                entity.Property(e => e.LineTotal).HasColumnType("money");

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.ProductId).HasColumnName("ProductID");

                entity.Property(e => e.PurchaseOrderDetailId).HasColumnName("PurchaseOrderDetailID");

                entity.Property(e => e.PurchaseOrderId).HasColumnName("PurchaseOrderID");

                entity.Property(e => e.ReceivedQty).HasColumnType("decimal(8, 2)");

                entity.Property(e => e.RejectedQty).HasColumnType("decimal(8, 2)");

                entity.Property(e => e.StockedQty).HasColumnType("decimal(9, 2)");

                entity.Property(e => e.UnitPrice).HasColumnType("money");
            });

            modelBuilder.Entity<PurchaseOrderHeader>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("PurchaseOrderHeader", "Purchasing");

                entity.Property(e => e.EmployeeId).HasColumnName("EmployeeID");

                entity.Property(e => e.Freight).HasColumnType("money");

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.OrderDate).HasColumnType("smalldatetime");

                entity.Property(e => e.PurchaseOrderId).HasColumnName("PurchaseOrderID");

                entity.Property(e => e.ShipDate).HasColumnType("smalldatetime");

                entity.Property(e => e.ShipMethodId).HasColumnName("ShipMethodID");

                entity.Property(e => e.SubTotal).HasColumnType("money");

                entity.Property(e => e.TaxAmt).HasColumnType("money");

                entity.Property(e => e.TotalDue).HasColumnType("money");

                entity.Property(e => e.VendorId).HasColumnName("VendorID");
            });

            modelBuilder.Entity<SalesOrderDetail>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("SalesOrderDetail", "Sales");

                entity.Property(e => e.CarrierTrackingNumber).HasMaxLength(25);

                entity.Property(e => e.LineTotal).HasColumnType("decimal(38, 6)");

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.ProductId).HasColumnName("ProductID");

                entity.Property(e => e.Rowguid).HasColumnName("rowguid");

                entity.Property(e => e.SalesOrderDetailId).HasColumnName("SalesOrderDetailID");

                entity.Property(e => e.SalesOrderId).HasColumnName("SalesOrderID");

                entity.Property(e => e.SpecialOfferId).HasColumnName("SpecialOfferID");

                entity.Property(e => e.UnitPrice).HasColumnType("money");

                entity.Property(e => e.UnitPriceDiscount).HasColumnType("money");
            });

            modelBuilder.Entity<SalesOrderHeader>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("SalesOrderHeader", "Sales");

                entity.Property(e => e.AccountNumber).HasMaxLength(15);

                entity.Property(e => e.BillToAddressId).HasColumnName("BillToAddressID");

                entity.Property(e => e.Comment).HasMaxLength(128);

                entity.Property(e => e.ContactId).HasColumnName("ContactID");

                entity.Property(e => e.CreditCardApprovalCode)
                    .HasMaxLength(15)
                    .IsUnicode(false);

                entity.Property(e => e.CreditCardId).HasColumnName("CreditCardID");

                entity.Property(e => e.CurrencyRateId).HasColumnName("CurrencyRateID");

                entity.Property(e => e.CustomerId).HasColumnName("CustomerID");

                entity.Property(e => e.DueDate).HasColumnType("smalldatetime");

                entity.Property(e => e.Freight).HasColumnType("money");

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.OrderDate).HasColumnType("smalldatetime");

                entity.Property(e => e.PurchaseOrderNumber).HasMaxLength(25);

                entity.Property(e => e.Rowguid).HasColumnName("rowguid");

                entity.Property(e => e.SalesOrderId).HasColumnName("SalesOrderID");

                entity.Property(e => e.SalesOrderNumber)
                    .IsRequired()
                    .HasMaxLength(25);

                entity.Property(e => e.SalesPersonId).HasColumnName("SalesPersonID");

                entity.Property(e => e.ShipDate).HasColumnType("smalldatetime");

                entity.Property(e => e.ShipMethodId).HasColumnName("ShipMethodID");

                entity.Property(e => e.ShipToAddressId).HasColumnName("ShipToAddressID");

                entity.Property(e => e.SubTotal).HasColumnType("money");

                entity.Property(e => e.TaxAmt).HasColumnType("money");

                entity.Property(e => e.TerritoryId).HasColumnName("TerritoryID");

                entity.Property(e => e.TotalDue).HasColumnType("money");
            });

            modelBuilder.Entity<SalesOrderHeaderSalesReason>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("SalesOrderHeaderSalesReason", "Sales");

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.SalesOrderId).HasColumnName("SalesOrderID");

                entity.Property(e => e.SalesReasonId).HasColumnName("SalesReasonID");
            });

            modelBuilder.Entity<SalesPerson>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("SalesPerson", "Sales");

                entity.Property(e => e.Bonus).HasColumnType("money");

                entity.Property(e => e.CommissionPct).HasColumnType("money");

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.Rowguid).HasColumnName("rowguid");

                entity.Property(e => e.SalesLastYear).HasColumnType("money");

                entity.Property(e => e.SalesPersonId).HasColumnName("SalesPersonID");

                entity.Property(e => e.SalesQuota).HasColumnType("money");

                entity.Property(e => e.SalesYtd)
                    .HasColumnName("SalesYTD")
                    .HasColumnType("money");

                entity.Property(e => e.TerritoryId).HasColumnName("TerritoryID");
            });

            modelBuilder.Entity<SalesPersonQuotaHistory>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("SalesPersonQuotaHistory", "Sales");

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.QuotaDate).HasColumnType("smalldatetime");

                entity.Property(e => e.Rowguid).HasColumnName("rowguid");

                entity.Property(e => e.SalesPersonId).HasColumnName("SalesPersonID");

                entity.Property(e => e.SalesQuota).HasColumnType("money");
            });

            modelBuilder.Entity<SalesReason>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("SalesReason", "Sales");

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.ReasonType)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.SalesReasonId).HasColumnName("SalesReasonID");
            });

            modelBuilder.Entity<SalesTaxRate>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("SalesTaxRate", "Sales");

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Rowguid).HasColumnName("rowguid");

                entity.Property(e => e.SalesTaxRateId).HasColumnName("SalesTaxRateID");

                entity.Property(e => e.StateProvinceId).HasColumnName("StateProvinceID");

                entity.Property(e => e.TaxRate).HasColumnType("money");
            });

            modelBuilder.Entity<SalesTerritory>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("SalesTerritory", "Sales");

                entity.Property(e => e.CostLastYear).HasColumnType("money");

                entity.Property(e => e.CostYtd)
                    .HasColumnName("CostYTD")
                    .HasColumnType("money");

                entity.Property(e => e.CountryRegionCode)
                    .IsRequired()
                    .HasMaxLength(3);

                entity.Property(e => e.Group)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Rowguid).HasColumnName("rowguid");

                entity.Property(e => e.SalesLastYear).HasColumnType("money");

                entity.Property(e => e.SalesYtd)
                    .HasColumnName("SalesYTD")
                    .HasColumnType("money");

                entity.Property(e => e.TerritoryId).HasColumnName("TerritoryID");
            });

            modelBuilder.Entity<SalesTerritoryHistory>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("SalesTerritoryHistory", "Sales");

                entity.Property(e => e.EndDate).HasColumnType("smalldatetime");

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.Rowguid).HasColumnName("rowguid");

                entity.Property(e => e.SalesPersonId).HasColumnName("SalesPersonID");

                entity.Property(e => e.StartDate).HasColumnType("smalldatetime");

                entity.Property(e => e.TerritoryId).HasColumnName("TerritoryID");
            });

            modelBuilder.Entity<ScrapReason>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("ScrapReason", "Production");

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.ScrapReasonId).HasColumnName("ScrapReasonID");
            });

            modelBuilder.Entity<Shift>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("Shift", "HumanResources");

                entity.Property(e => e.EndTime).HasColumnType("smalldatetime");

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.ShiftId).HasColumnName("ShiftID");

                entity.Property(e => e.StartTime).HasColumnType("smalldatetime");
            });

            modelBuilder.Entity<ShipMethod>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("ShipMethod", "Purchasing");

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Rowguid).HasColumnName("rowguid");

                entity.Property(e => e.ShipBase).HasColumnType("money");

                entity.Property(e => e.ShipMethodId).HasColumnName("ShipMethodID");

                entity.Property(e => e.ShipRate).HasColumnType("money");
            });

            modelBuilder.Entity<ShoppingCartItem>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("ShoppingCartItem", "Sales");

                entity.Property(e => e.DateCreated).HasColumnType("smalldatetime");

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.ProductId).HasColumnName("ProductID");

                entity.Property(e => e.ShoppingCartId)
                    .IsRequired()
                    .HasColumnName("ShoppingCartID")
                    .HasMaxLength(50);

                entity.Property(e => e.ShoppingCartItemId).HasColumnName("ShoppingCartItemID");
            });

            modelBuilder.Entity<SpecialOffer>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("SpecialOffer", "Sales");

                entity.Property(e => e.Category)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.DiscountPct).HasColumnType("money");

                entity.Property(e => e.EndDate).HasColumnType("smalldatetime");

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.Rowguid).HasColumnName("rowguid");

                entity.Property(e => e.SpecialOfferId).HasColumnName("SpecialOfferID");

                entity.Property(e => e.StartDate).HasColumnType("smalldatetime");

                entity.Property(e => e.Type)
                    .IsRequired()
                    .HasMaxLength(50);
            });

            modelBuilder.Entity<SpecialOfferProduct>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("SpecialOfferProduct", "Sales");

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.ProductId).HasColumnName("ProductID");

                entity.Property(e => e.Rowguid).HasColumnName("rowguid");

                entity.Property(e => e.SpecialOfferId).HasColumnName("SpecialOfferID");
            });

            modelBuilder.Entity<StateProvince>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("StateProvince", "Person");

                entity.Property(e => e.CountryRegionCode)
                    .IsRequired()
                    .HasMaxLength(3);

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Rowguid).HasColumnName("rowguid");

                entity.Property(e => e.StateProvinceCode)
                    .IsRequired()
                    .HasMaxLength(3)
                    .IsFixedLength();

                entity.Property(e => e.StateProvinceId).HasColumnName("StateProvinceID");

                entity.Property(e => e.TerritoryId).HasColumnName("TerritoryID");
            });

            modelBuilder.Entity<Store>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("Store", "Sales");

                entity.Property(e => e.CustomerId).HasColumnName("CustomerID");

                entity.Property(e => e.Demographics).HasColumnType("xml");

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Rowguid).HasColumnName("rowguid");

                entity.Property(e => e.SalesPersonId).HasColumnName("SalesPersonID");
            });

            modelBuilder.Entity<StoreContact>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("StoreContact", "Sales");

                entity.Property(e => e.ContactId).HasColumnName("ContactID");

                entity.Property(e => e.ContactTypeId).HasColumnName("ContactTypeID");

                entity.Property(e => e.CustomerId).HasColumnName("CustomerID");

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.Rowguid).HasColumnName("rowguid");
            });

            modelBuilder.Entity<TransactionHistory>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("TransactionHistory", "Production");

                entity.Property(e => e.ActualCost).HasColumnType("money");

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.ProductId).HasColumnName("ProductID");

                entity.Property(e => e.ReferenceOrderId).HasColumnName("ReferenceOrderID");

                entity.Property(e => e.ReferenceOrderLineId).HasColumnName("ReferenceOrderLineID");

                entity.Property(e => e.TransactionDate).HasColumnType("smalldatetime");

                entity.Property(e => e.TransactionId).HasColumnName("TransactionID");

                entity.Property(e => e.TransactionType)
                    .IsRequired()
                    .HasMaxLength(1)
                    .IsFixedLength();
            });

            modelBuilder.Entity<TransactionHistoryArchive>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("TransactionHistoryArchive", "Production");

                entity.Property(e => e.ActualCost).HasColumnType("money");

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.ProductId).HasColumnName("ProductID");

                entity.Property(e => e.ReferenceOrderId).HasColumnName("ReferenceOrderID");

                entity.Property(e => e.ReferenceOrderLineId).HasColumnName("ReferenceOrderLineID");

                entity.Property(e => e.TransactionDate).HasColumnType("smalldatetime");

                entity.Property(e => e.TransactionId).HasColumnName("TransactionID");

                entity.Property(e => e.TransactionType)
                    .IsRequired()
                    .HasMaxLength(1)
                    .IsFixedLength();
            });

            modelBuilder.Entity<UnitMeasure>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("UnitMeasure", "Production");

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.UnitMeasureCode)
                    .IsRequired()
                    .HasMaxLength(3)
                    .IsFixedLength();
            });

            modelBuilder.Entity<Vendor>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("Vendor", "Purchasing");

                entity.Property(e => e.AccountNumber)
                    .IsRequired()
                    .HasMaxLength(15);

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.PurchasingWebServiceUrl)
                    .HasColumnName("PurchasingWebServiceURL")
                    .HasMaxLength(1024);

                entity.Property(e => e.VendorId).HasColumnName("VendorID");
            });

            modelBuilder.Entity<VendorAddress>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("VendorAddress", "Purchasing");

                entity.Property(e => e.AddressId).HasColumnName("AddressID");

                entity.Property(e => e.AddressTypeId).HasColumnName("AddressTypeID");

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.VendorId).HasColumnName("VendorID");
            });

            modelBuilder.Entity<VendorContact>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("VendorContact", "Purchasing");

                entity.Property(e => e.ContactId).HasColumnName("ContactID");

                entity.Property(e => e.ContactTypeId).HasColumnName("ContactTypeID");

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.VendorId).HasColumnName("VendorID");
            });

            modelBuilder.Entity<WorkOrder>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("WorkOrder", "Production");

                entity.Property(e => e.DueDate).HasColumnType("smalldatetime");

                entity.Property(e => e.EndDate).HasColumnType("smalldatetime");

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.ProductId).HasColumnName("ProductID");

                entity.Property(e => e.ScrapReasonId).HasColumnName("ScrapReasonID");

                entity.Property(e => e.StartDate).HasColumnType("smalldatetime");

                entity.Property(e => e.WorkOrderId).HasColumnName("WorkOrderID");
            });

            modelBuilder.Entity<WorkOrderRouting>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("WorkOrderRouting", "Production");

                entity.Property(e => e.ActualCost).HasColumnType("money");

                entity.Property(e => e.ActualEndDate).HasColumnType("smalldatetime");

                entity.Property(e => e.ActualResourceHrs).HasColumnType("decimal(9, 4)");

                entity.Property(e => e.ActualStartDate).HasColumnType("smalldatetime");

                entity.Property(e => e.LocationId).HasColumnName("LocationID");

                entity.Property(e => e.ModifiedDate).HasColumnType("smalldatetime");

                entity.Property(e => e.PlannedCost).HasColumnType("money");

                entity.Property(e => e.ProductId).HasColumnName("ProductID");

                entity.Property(e => e.ScheduledEndDate).HasColumnType("smalldatetime");

                entity.Property(e => e.ScheduledStartDate).HasColumnType("smalldatetime");

                entity.Property(e => e.WorkOrderId).HasColumnName("WorkOrderID");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
