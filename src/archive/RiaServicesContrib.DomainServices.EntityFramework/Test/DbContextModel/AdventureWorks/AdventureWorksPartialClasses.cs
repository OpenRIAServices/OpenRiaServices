using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel.DomainServices.EntityFramework;
using System.ServiceModel.DomainServices.Server;
using System.ServiceModel.DomainServices.Hosting;

// These assembly attributes allow us to serialize different CLR types into the same contract
[assembly: ContractNamespace("http://schemas.datacontract.org/2004/07/DataTests.AdventureWorks",
                              ClrNamespace = "DbContextModels.AdventureWorks")]

namespace DbContextModels.AdventureWorks
{
    public partial class DbCtxAdventureWorksEntities
    {
        public DbCtxAdventureWorksEntities(string connectionString)
            : base(connectionString)
        {
        }
    }

    [MetadataType(typeof(PurchaseOrderMetadata))]
    public partial class PurchaseOrder
    {
    }

    [MetadataType(typeof(PurchaseOrderDetailMetadata))]
    public partial class PurchaseOrderDetail
    {
    }

    public static class PurchaseOrderMetadata
    {
        [Include]
        public static object PurchaseOrderDetails;
    }

    public static class PurchaseOrderDetailMetadata
    {
        [Include]
        public static object Product;
    }

    [MetadataType(typeof(ProductMetadata))]
    public partial class Product
    {
    }

    public static class ProductMetadata
    {
        [Exclude]
        public static object SafetyStockLevel;

        [RoundtripOriginal]
        public static object Weight;
    }

    [MetadataType(typeof(EmployeeMetadata))]
    public partial class Employee
    {
    }

    public static class EmployeeMetadata
    {
        [Include]
        public static object Manager;
    }

    public class EmployeeInfo
    {
        public static EmployeeInfo CreateEmployeeInfo(int employeeID, string firstName, string lastName, int territoryID)
        {
            EmployeeInfo empInfo = new EmployeeInfo();
            empInfo.EmployeeID = employeeID;
            empInfo.FirstName = firstName;
            empInfo.LastName = lastName;
            empInfo.TerritoryID = territoryID;
            return empInfo;
        }

        [Key]
        public int EmployeeID
        {
            get;
            set;
        }

        public string FirstName
        {
            get;
            set;
        }
        public string LastName
        {
            get;
            set;
        }
        public int TerritoryID
        {
            get;
            set;
        }
    }
}
