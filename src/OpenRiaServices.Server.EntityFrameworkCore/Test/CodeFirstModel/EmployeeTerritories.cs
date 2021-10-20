namespace CodeFirstModels
{    
    public partial class EmployeeTerritory
    {
        public int EmployeeID { get; set; }
       
        public Employee Employee { get; set; }

        public int TerritoryID { get; set; }

        public Territory Territory { get; set; }

    }
}
