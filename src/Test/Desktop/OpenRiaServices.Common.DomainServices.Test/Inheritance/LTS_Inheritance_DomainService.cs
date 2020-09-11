namespace DataTests.Inheritance.LTS
{
    using System.Linq;
    using System.Runtime.Serialization;
    using OpenRiaServices.Hosting;
    using OpenRiaServices.Server;
    using OpenRiaServices.LinqToSql;

    /// <summary>
    /// DomainService that demonstrates simple inheritance using EF models
    /// </summary>
    [EnableClientAccess()]
    public class LTS_Inheritance_DomainService : LinqToSqlDomainService<InheritanceScenarios>
    {
        // Exposes abstract base
        public IQueryable<A> GetA()
        {
            return null;
        }

        // Exposes 1st derived type
        public IQueryable<B> GetB()
        {
            return null;
        }

        // exposes 2nd derived type, sibling to B
        public IQueryable<C> GetC()
        {
            return null;
        }

        // Verify singleton queries on derived types
        public B GetOneB()
        {
            return null;
        }

        public C GetOneC()
        {
            return null;
        }

        // Verify Invokes on derived
        [Invoke]
        public int InvokeOnB(B b)
        {
            return 1;
        }

        // Verifies update CUD method base
        [Update]
        public void UpdateA(A c)
        {
        }

        // Ditto, but CUD method itself can exist only because A's does
        [Update]
        public void UpdateC(C c)
        {
        }
    }

    // Required because LTS doesn't appear to do this
    [KnownType(typeof(B))]
    [KnownType(typeof(C))]
    public partial class A
    {
    }
}


