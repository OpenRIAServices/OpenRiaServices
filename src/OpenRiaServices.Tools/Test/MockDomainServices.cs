using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using OpenRiaServices.Server;

namespace OpenRiaServices.Tools.Test
{
    /// <summary>
    /// A generic <see cref="DomainService"/> used for unit testing.
    /// </summary>
    /// <typeparam name="T">The Type of entity to return in a Query method.</typeparam>
#pragma warning disable CS0618
    [EnableClientAccess]
    public class GenericDomainService<T> : DomainService
    {
        [Query]
        public IEnumerable<T> Get()
        {
            throw new NotImplementedException();
        }
    }

    [EnableClientAccess]
    public class EntityRefCodeGenDomainService : DomainService
    {
        [Query]
        public IEnumerable<EntityRefCodeGenSource> GetSources() => throw new NotImplementedException();

        [Query]
        public IEnumerable<EntityRefCodeGenTarget> GetTargets() => throw new NotImplementedException();
    }

    public class EntityRefCodeGenSource
    {
        [Key]
        public int Id { get; set; }

        public int TargetId { get; set; }

        public string TargetCode { get; set; }

        [Association("ByPrimaryKey", nameof(TargetId), nameof(EntityRefCodeGenTarget.Id), IsForeignKey = true)]
        public EntityRefCodeGenTarget ByPrimaryKey { get; set; }

        [Association("ByNonKey", nameof(TargetCode), nameof(EntityRefCodeGenTarget.Code), IsForeignKey = true)]
        public EntityRefCodeGenTarget ByNonKey { get; set; }
    }

    public class EntityRefCodeGenTarget
    {
        [Key]
        public int Id { get; set; }

        public string Code { get; set; }
    }
#pragma warning restore CS0618
}
