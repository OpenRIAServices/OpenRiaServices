using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using \u004fpenRiaServices.Server;

namespace ForwardVersioning
{
    [EnableClientAccess]
    public class ForwardVersioningDomainService : DomainService
    {
        public IQueryable<ForwardVersioningEntity> GetEntities(int version)
        {
            return new[]
            {
                new ForwardVersioningEntity
                {
                    Id = 1,
                    Name = $"entity-{version}",
                    UnknownEntityValue = $"unknown-entity-{version}",
                    Details = new ForwardVersioningComplex
                    {
                        Name = $"complex-{version}",
                        UnknownComplexValue = $"unknown-complex-{version}",
                        Child = new ForwardVersioningNestedComplex
                        {
                            Name = $"nested-{version}",
                            UnknownNestedValue = $"unknown-nested-{version}"
                        }
                    }
                }
            }.AsQueryable();
        }

        public void UpdateEntity(ForwardVersioningEntity entity)
        {
            var original = ChangeSet.GetOriginal(entity);
            RequireEqual("entity-2", original?.Name, "original.Name");
            RequireEqual("complex-2", original?.Details?.Name, "original.Details.Name");
            RequireEqual("nested-2", original?.Details?.Child?.Name, "original.Details.Child.Name");
            RequireEqual("client-entity", entity.Name, "current.Name");
            RequireEqual("complex-2", entity.Details?.Name, "current.Details.Name");
            RequireEqual("client-nested", entity.Details?.Child?.Name, "current.Details.Child.Name");
            VerifyUnknownValues(entity, "current");
            VerifyUnknownValues(original, "original");

            entity.Name = "verified";
        }

        private static void VerifyUnknownValues(ForwardVersioningEntity entity, string prefix)
        {
            RequireEqual("unknown-entity-2", entity?.UnknownEntityValue, prefix + ".UnknownEntityValue");
            RequireEqual("unknown-complex-2", entity?.Details?.UnknownComplexValue, prefix + ".Details.UnknownComplexValue");
            RequireEqual("unknown-nested-2", entity?.Details?.Child?.UnknownNestedValue, prefix + ".Details.Child.UnknownNestedValue");
        }

        private static void RequireEqual(string expected, string actual, string property)
        {
            if (actual != expected)
                throw new InvalidOperationException($"Expected {property} to be '{expected}', but received '{actual ?? "<null>"}'.");
        }
    }

    [RoundtripOriginal]
    public class ForwardVersioningEntity
    {
        [Key]
        public int Id { get; set; }

        public string Name { get; set; }

        public string UnknownEntityValue { get; set; }

        public ForwardVersioningComplex Details { get; set; }
    }

    [RoundtripOriginal]
    public class ForwardVersioningComplex
    {
        public string Name { get; set; }

        public string UnknownComplexValue { get; set; }

        public ForwardVersioningNestedComplex Child { get; set; }
    }

    [RoundtripOriginal]
    public class ForwardVersioningNestedComplex
    {
        public string Name { get; set; }

        public string UnknownNestedValue { get; set; }
    }
}
