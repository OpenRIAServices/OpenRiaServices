using OpenRiaServices.Client;
using System.ComponentModel.DataAnnotations;

namespace EFCoreModels.AdventureWorks
{
    public sealed partial class Employee
    {
        private EntityCollection<Employee> _reports;

        /// <summary>
        /// Gets the employees reporting to this manager.
        /// </summary>
        // The EF Core client exposes Manager but not its inverse; retain bidirectional test coverage.
        [EntityAssociation("FK_Employee_Employee_ManagerID", "EmployeeID", "ManagerID")]
        public EntityCollection<Employee> Reports
        {
            get
            {
                if (_reports == null)
                {
                    _reports = new EntityCollection<Employee>(this, nameof(Reports),
                        employee => employee.ManagerID == EmployeeID,
                        employee => employee.Manager = this,
                        employee => employee.Manager = null);
                }

                return _reports;
            }
        }
    }
}
