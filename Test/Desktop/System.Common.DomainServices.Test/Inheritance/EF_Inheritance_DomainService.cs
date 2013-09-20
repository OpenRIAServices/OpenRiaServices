namespace DataTests.Inheritance.EF
{
    using System.Linq;
    using System.ServiceModel.DomainServices.EntityFramework;
    using System.ServiceModel.DomainServices.Hosting;
    using System.ServiceModel.DomainServices.Server;

    /// <summary>
    /// DomainService that demonstrates simple inheritance using EF models
    /// </summary>
    [EnableClientAccess()]
    public class EF_Inheritance_DomainService : LinqToEntitiesDomainService<InheritanceScenarios>
    {
        // Exposes abstract base
        public IQueryable<A> GetA()
        {
            return this.ObjectContext.A;
        }

        // Exposes 1st derived type
        public IQueryable<B> GetB()
        {
            return this.ObjectContext.A.OfType<B>();
        }

        // exposes 2nd derived type, sibling to B
        public IQueryable<C> GetC()
        {
            return this.ObjectContext.A.OfType<C>();
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
}


