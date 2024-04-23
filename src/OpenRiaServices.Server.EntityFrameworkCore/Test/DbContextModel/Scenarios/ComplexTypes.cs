using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.EntityFrameworkCore;

#nullable enable

#if NET8_0_OR_GREATER

namespace EFCoreModels.Scenarios.ComplexTypes
{

    public class Address
    {
        public required string AddressLine { get; set; }
        public required string City { get; set; }
    }

    public class ContactInfo
    {
        public required Address Address { get; set; }

        public required string HomePhone { get; set; }

        public int PossibleId { get; set; }
    }

    public class Employee
    {
        public int EmployeeId { get; set; }
        public required ContactInfo ContactInfo { get; set; }
    }

    public class ComplexTypesDbContext : DbContext
    {
        public ComplexTypesDbContext()
        {
        }

        public ComplexTypesDbContext(DbContextOptions<ComplexTypesDbContext> options)
            : base(options)
        {
        }

        public DbSet<Employee> Employees { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // add concurrencymode = Fixed on all "Owned types" ??

            modelBuilder.Owned<Address>();
            //modelBuilder.Owned<ContactInfo>();

            modelBuilder.Entity<Employee>()
                .ComplexProperty(typeof(ContactInfo), nameof(Employee.ContactInfo), x =>
                {
                    x.Property(nameof(ContactInfo.HomePhone)).HasMaxLength(24);
                    x.ComplexProperty(typeof(Address), nameof(ContactInfo.Address), address =>
                    {
                        address.Property(nameof(Address.AddressLine)).HasMaxLength(100);
                        address.Property(nameof(Address.City)).HasMaxLength(50);
                    });
                })
                .HasKey(e => e.EmployeeId)
                ;

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
