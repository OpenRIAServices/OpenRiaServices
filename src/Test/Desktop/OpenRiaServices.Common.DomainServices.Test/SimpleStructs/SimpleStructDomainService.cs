using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using OpenRiaServices.Server;

namespace SimpleStructs
{
    public struct Mock_CG_ExcludedMemberSimpleStruct : System.IEquatable<Mock_CG_ExcludedMemberSimpleStruct>
    {
        public int Value { get; set; }

        [Exclude]
        public int ExcludedValue { get; set; }

        public bool Equals(Mock_CG_ExcludedMemberSimpleStruct other) => Value == other.Value;
    }

    [EnableClientAccess]
    public class SimpleStructDomainService : DomainService
    {
        [Query]
        public IQueryable<SimpleStructEntity> GetSimpleStructEntities()
        {
            return new[]
            {
                new SimpleStructEntity
                {
                    Id = new SimpleStruct(1),
                    CompositeValue = new CompositeSimpleStruct(2, new System.Guid("207b1d8f-1f78-4f35-b262-b2787e1289f1")),
                    Name = "First"
                }
            }.AsQueryable();
        }

        [Invoke(HasSideEffects = false)]
        public SimpleStruct RoundtripSimpleStruct(SimpleStruct value) => value;

        [Invoke(HasSideEffects = false)]
        public CompositeSimpleStruct RoundtripCompositeSimpleStruct(CompositeSimpleStruct value) => value;

        [Invoke]
        public IEnumerable<SimpleStruct> RoundtripSimpleStructCollection(IEnumerable<SimpleStruct> values) => values;

        [Invoke]
        public IEnumerable<CompositeSimpleStruct> RoundtripCompositeSimpleStructCollection(IEnumerable<CompositeSimpleStruct> values) => values;
    }

    public class SimpleStructEntity
    {
        [Key]
        public SimpleStruct Id { get; set; }

        public CompositeSimpleStruct CompositeValue { get; set; }

        public string Name { get; set; }
    }
}
