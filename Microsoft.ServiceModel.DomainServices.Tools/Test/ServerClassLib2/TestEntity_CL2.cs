using System.ComponentModel.DataAnnotations;

namespace ServerClassLib2
{
    /// <summary>
    /// Entity used to test shared code detection for types included via
    /// a project reference
    /// </summary>
    public partial class TestEntity_CL2
    {
        [Key]
        public int Id { get; set; }
    }
}
