namespace CodeFirstModels
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    
    public partial class Employee
    {
        public Employee()
        {
            this.Employees1 = new HashSet<Employee>();
            this.Orders = new HashSet<Order>();
            this.Territories = new HashSet<Territory>();
        }
    
        [Key]
        [Column("EmployeeID")]
        public int EmployeeID { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string Title { get; set; }
        public string TitleOfCourtesy { get; set; }
        public Nullable<System.DateTime> BirthDate { get; set; }
        public Nullable<System.DateTime> HireDate { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Region { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
        public string HomePhone { get; set; }
        public string Extension { get; set; }
        public byte[] Photo { get; set; }
        public string Notes { get; set; }
        [ForeignKey("Employees1")]
        public Nullable<int> ReportsTo { get; set; }
        public string PhotoPath { get; set; }
    
        public virtual ICollection<Employee> Employees1 { get; set; }
        public virtual Employee Employee1 { get; set; }

        public virtual ICollection<Order> Orders { get; set; }
        public virtual ICollection<Territory> Territories { get; set; }
    }
}
