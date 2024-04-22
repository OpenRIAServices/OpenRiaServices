using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.EntityFrameworkCore;

#nullable enable

#if NET8_0_OR_GREATER

namespace EFCoreModels.OwnedTypes
{

    public class Address
    {
        [StringLength(100)] public required string AddressLine { get; set; }
        [StringLength(50)] public required string City { get; set; }
    }

    public class ContactInfo
    {
        public required Address Address { get; set; }

        [StringLength(24)]
        public required string HomePhone { get; set; }
    }

    public class Employee
    {
        public int EmployeeId { get; set; }
        public required ContactInfo ContactInfo { get; set; }
    }

    // TODO: Owned typed with "backwards" FK
    public class OwnedTypesDbContext : DbContext
    {
        public OwnedTypesDbContext()
        {
        }

        public OwnedTypesDbContext(DbContextOptions<OwnedTypesDbContext> options)
            : base(options)
        {
        }

        public DbSet<Employee> Employees { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // add concurrencymode = Fixed on all "Owned types" ??

            modelBuilder.Owned<Address>();
            modelBuilder.Owned<ContactInfo>();

            modelBuilder.Entity<Employee>()
                .HasKey(e => e.EmployeeId);

            base.OnModelCreating(modelBuilder);
        }


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=AdventureWorks;Integrated Security=True;MultipleActiveResultSets=True");
            }
        }
    }
}
#endif
