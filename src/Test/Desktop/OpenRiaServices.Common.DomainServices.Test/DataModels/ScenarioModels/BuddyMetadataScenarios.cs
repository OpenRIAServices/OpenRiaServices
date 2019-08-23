using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Data.Linq.Mapping;
using System.Data.Linq;

namespace DataModels.ScenarioModels
{
    public partial class BuddyMetadataScenariosDataContext : DataContext
    {
        private static System.Data.Linq.Mapping.MappingSource mappingSource = new AttributeMappingSource();

        public BuddyMetadataScenariosDataContext()
            : base(string.Empty)
        {
        }

        public BuddyMetadataScenariosDataContext(string connection)
            : base(connection, mappingSource)
        {
        }

        public System.Data.Linq.Table<EntityPropertyNamedPublic> Entities
        {
            get
            {
                return null;
            }
        }
    }

    [DataContract]
    [Table]
    public partial class EntityPropertyNamedPublic
    {
        [DataMember]
        [Column(IsPrimaryKey = true)]
        public int publicPublic { get; set; }
    }
}

