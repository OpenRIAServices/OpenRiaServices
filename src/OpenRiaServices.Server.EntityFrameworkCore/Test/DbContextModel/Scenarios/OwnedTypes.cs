﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.EntityFrameworkCore;

#nullable enable

#if NET8_0_OR_GREATER

namespace EFCoreModels.Scenarios.OwnedTypes
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
            modelBuilder.Entity<Employee>()
                .OwnsOne(typeof(ContactInfo), nameof(Employee.ContactInfo), x =>
                {
                    x.Property(nameof(ContactInfo.HomePhone)).HasMaxLength(24);
                    x.OwnsOne(typeof(Address), nameof(ContactInfo.Address), address =>
                    {
                        address.Property(nameof(Address.AddressLine)).HasMaxLength(100);
                        address.Property(nameof(Address.City)).HasMaxLength(50);
                    });
                })
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
