using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;

// These assembly attributes allow us to serialize different CLR types into the same contract
[assembly: ContractNamespace("http://schemas.datacontract.org/2004/07/DataTests.AdventureWorks",
                              ClrNamespace = "EFCoreModels.AdventureWorks")]

namespace EFCoreModels.AdventureWorks
{

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
