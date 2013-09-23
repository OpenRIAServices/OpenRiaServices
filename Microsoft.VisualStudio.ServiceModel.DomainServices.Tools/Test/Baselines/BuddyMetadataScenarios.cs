
namespace BizLogic.Test
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.Data.Linq;
    using System.Linq;
    using OpenRiaServices.DomainServices.Hosting;
    using OpenRiaServices.DomainServices.Server;
    using DataModels.ScenarioModels;
    using OpenRiaServices.DomainServices.LinqToSql;
    
    
    // Implements application logic using the BuddyMetadataScenariosDataContext context.
    // TODO: Add your application logic to these methods or in additional methods.
    // TODO: Wire up authentication (Windows/ASP.NET Forms) and uncomment the following to disable anonymous access
    // Also consider adding roles to restrict access as appropriate.
    // [RequiresAuthentication]
    [EnableClientAccess()]
    public class BuddyMetadataScenarios : LinqToSqlDomainService<BuddyMetadataScenariosDataContext>
    {
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<EntityPropertyNamedPublic> GetEntities()
        {
            return this.DataContext.Entities;
        }
        
        public void InsertEntityPropertyNamedPublic(EntityPropertyNamedPublic entityPropertyNamedPublic)
        {
            this.DataContext.Entities.InsertOnSubmit(entityPropertyNamedPublic);
        }
        
        public void UpdateEntityPropertyNamedPublic(EntityPropertyNamedPublic currentEntityPropertyNamedPublic)
        {
            this.DataContext.Entities.Attach(currentEntityPropertyNamedPublic, this.ChangeSet.GetOriginal(currentEntityPropertyNamedPublic));
        }
        
        public void DeleteEntityPropertyNamedPublic(EntityPropertyNamedPublic entityPropertyNamedPublic)
        {
            this.DataContext.Entities.Attach(entityPropertyNamedPublic);
            this.DataContext.Entities.DeleteOnSubmit(entityPropertyNamedPublic);
        }
    }
}
