using System.ComponentModel.DataAnnotations;
using OpenRiaServices.DomainServices;

namespace DataTests.Scenarios.EF.Northwind
{
    /// <summary>
    /// POCO class used to verify external references work in EF scenarios.
    /// </summary>
    public class PersonalDetails
    {
        [Key]
        public int UniqueID { get; set; }
        public string MsnMessengerId { get; set; }
    }

    /// <summary>
    /// Extends the Northwind Employee to add a new POCO PersonalDetails property.
    /// </summary>
    public partial class Employee
    {
        [ExternalReference]
        [Association("Employee_PersonalDetails", "EmployeeID", "UniqueID", IsForeignKey = true)]
        public PersonalDetails PersonalDetails_MarkedAsExternal
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Extends the Northwind Customer to add a new POCO PersonalDetails property that is missing an ExternalReferenceAttribute.
    /// </summary>
    public partial class Customer
    {
        [Association("Customer_PersonalDetails", "CustomerID", "UniqueID", IsForeignKey = true)]
        public PersonalDetails PersonalDetails_NotMarkedAsExternal
        {
            get;
            set;
        }
    }
}
