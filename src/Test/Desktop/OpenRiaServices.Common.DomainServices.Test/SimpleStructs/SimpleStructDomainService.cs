using System.Collections.Generic;
using OpenRiaServices.Server;

namespace SimpleStructs
{
    [EnableClientAccess]
    public class SimpleStructDomainService : DomainService
    {
        [Invoke]
        public SimpleStruct RoundtripSimpleStruct(SimpleStruct value) => value;

        [Invoke]
        public CompositeSimpleStruct RoundtripCompositeSimpleStruct(CompositeSimpleStruct value) => value;

        [Invoke]
        public IEnumerable<SimpleStruct> RoundtripSimpleStructCollection(IEnumerable<SimpleStruct> values) => values;

        [Invoke]
        public IEnumerable<CompositeSimpleStruct> RoundtripCompositeSimpleStructCollection(IEnumerable<CompositeSimpleStruct> values) => values;
    }
}
