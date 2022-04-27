
namespace BizLogic.Test
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.Data.Linq;
    using System.Linq;
    using DataTests.Scenarios.LTS.Northwind;
    using OpenRiaServices.LinqToSql;
    using OpenRiaServices.Server;
    
    
    // Implements application logic using the NorthwindScenarios context.
    // TODO: Add your application logic to these methods or in additional methods.
    // TODO: Wire up authentication (Windows/ASP.NET Forms) and uncomment the following to disable anonymous access
    // Also consider adding roles to restrict access as appropriate.
    // [RequiresAuthentication]
    [EnableClientAccess()]
    public class LTS_Northwind_Scenarios : LinqToSqlDomainService<NorthwindScenarios>
    {
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<Bug843965_A> GetBug843965_As()
        {
            return this.DataContext.Bug843965_As;
        }
        
        public void InsertBug843965_A(Bug843965_A bug843965_A)
        {
            this.DataContext.Bug843965_As.InsertOnSubmit(bug843965_A);
        }
        
        public void UpdateBug843965_A(Bug843965_A currentBug843965_A)
        {
            this.DataContext.Bug843965_As.Attach(currentBug843965_A, this.ChangeSet.GetOriginal(currentBug843965_A));
        }
        
        public void DeleteBug843965_A(Bug843965_A bug843965_A)
        {
            this.DataContext.Bug843965_As.Attach(bug843965_A);
            this.DataContext.Bug843965_As.DeleteOnSubmit(bug843965_A);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<Customer_Bug479436> GetCustomer_Bug479436s()
        {
            return this.DataContext.Customer_Bug479436s;
        }
        
        public void InsertCustomer_Bug479436(Customer_Bug479436 customer_Bug479436)
        {
            this.DataContext.Customer_Bug479436s.InsertOnSubmit(customer_Bug479436);
        }
        
        public void UpdateCustomer_Bug479436(Customer_Bug479436 currentCustomer_Bug479436)
        {
            this.DataContext.Customer_Bug479436s.Attach(currentCustomer_Bug479436, this.ChangeSet.GetOriginal(currentCustomer_Bug479436));
        }
        
        public void DeleteCustomer_Bug479436(Customer_Bug479436 customer_Bug479436)
        {
            this.DataContext.Customer_Bug479436s.Attach(customer_Bug479436);
            this.DataContext.Customer_Bug479436s.DeleteOnSubmit(customer_Bug479436);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<Order_Bug479436> GetOrder_Bug479436s()
        {
            return this.DataContext.Order_Bug479436s;
        }
        
        public void InsertOrder_Bug479436(Order_Bug479436 order_Bug479436)
        {
            this.DataContext.Order_Bug479436s.InsertOnSubmit(order_Bug479436);
        }
        
        public void UpdateOrder_Bug479436(Order_Bug479436 currentOrder_Bug479436)
        {
            this.DataContext.Order_Bug479436s.Attach(currentOrder_Bug479436, this.ChangeSet.GetOriginal(currentOrder_Bug479436));
        }
        
        public void DeleteOrder_Bug479436(Order_Bug479436 order_Bug479436)
        {
            this.DataContext.Order_Bug479436s.Attach(order_Bug479436);
            this.DataContext.Order_Bug479436s.DeleteOnSubmit(order_Bug479436);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<TimestampEntity> GetTimestampEntities()
        {
            return this.DataContext.TimestampEntities;
        }
        
        public void InsertTimestampEntity(TimestampEntity timestampEntity)
        {
            this.DataContext.TimestampEntities.InsertOnSubmit(timestampEntity);
        }
        
        public void UpdateTimestampEntity(TimestampEntity currentTimestampEntity)
        {
            this.DataContext.TimestampEntities.Attach(currentTimestampEntity, true);
        }
        
        public void DeleteTimestampEntity(TimestampEntity timestampEntity)
        {
            this.DataContext.TimestampEntities.Attach(timestampEntity);
            this.DataContext.TimestampEntities.DeleteOnSubmit(timestampEntity);
        }
    }
}
