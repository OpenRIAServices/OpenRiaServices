
namespace BizLogic.Test
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.Data;
    using System.Linq;
    using OpenRiaServices.DomainServices.EntityFramework;
    using OpenRiaServices.DomainServices.Hosting;
    using OpenRiaServices.DomainServices.Server;
    using DataTests.Scenarios.EF.Northwind;
    
    
    // Implements application logic using the NorthwindEntities_Scenarios context.
    // TODO: Add your application logic to these methods or in additional methods.
    // TODO: Wire up authentication (Windows/ASP.NET Forms) and uncomment the following to disable anonymous access
    // Also consider adding roles to restrict access as appropriate.
    // [RequiresAuthentication]
    [EnableClientAccess()]
    public class EF_Northwind_Scenarios : LinqToEntitiesDomainService<NorthwindEntities_Scenarios>
    {
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'CustomerSet' query.
        public IQueryable<Customer> GetCustomerSet()
        {
            return this.ObjectContext.CustomerSet;
        }
        
        public void InsertCustomer(Customer customer)
        {
            if ((customer.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(customer, EntityState.Added);
            }
            else
            {
                this.ObjectContext.CustomerSet.AddObject(customer);
            }
        }
        
        public void UpdateCustomer(Customer currentCustomer)
        {
            this.ObjectContext.CustomerSet.AttachAsModified(currentCustomer, this.ChangeSet.GetOriginal(currentCustomer));
        }
        
        public void DeleteCustomer(Customer customer)
        {
            if ((customer.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(customer, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.CustomerSet.Attach(customer);
                this.ObjectContext.CustomerSet.DeleteObject(customer);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'EmployeeSet' query.
        public IQueryable<Employee> GetEmployeeSet()
        {
            return this.ObjectContext.EmployeeSet;
        }
        
        public void InsertEmployee(Employee employee)
        {
            if ((employee.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(employee, EntityState.Added);
            }
            else
            {
                this.ObjectContext.EmployeeSet.AddObject(employee);
            }
        }
        
        public void UpdateEmployee(Employee currentEmployee)
        {
            this.ObjectContext.EmployeeSet.AttachAsModified(currentEmployee, this.ChangeSet.GetOriginal(currentEmployee));
        }
        
        public void DeleteEmployee(Employee employee)
        {
            if ((employee.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(employee, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.EmployeeSet.Attach(employee);
                this.ObjectContext.EmployeeSet.DeleteObject(employee);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'EmployeeWithCTs' query.
        public IQueryable<EmployeeWithCT> GetEmployeeWithCTs()
        {
            return this.ObjectContext.EmployeeWithCTs;
        }
        
        public void InsertEmployeeWithCT(EmployeeWithCT employeeWithCT)
        {
            if ((employeeWithCT.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(employeeWithCT, EntityState.Added);
            }
            else
            {
                this.ObjectContext.EmployeeWithCTs.AddObject(employeeWithCT);
            }
        }
        
        public void UpdateEmployeeWithCT(EmployeeWithCT currentEmployeeWithCT)
        {
            this.ObjectContext.EmployeeWithCTs.AttachAsModified(currentEmployeeWithCT, this.ChangeSet.GetOriginal(currentEmployeeWithCT));
        }
        
        public void DeleteEmployeeWithCT(EmployeeWithCT employeeWithCT)
        {
            if ((employeeWithCT.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(employeeWithCT, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.EmployeeWithCTs.Attach(employeeWithCT);
                this.ObjectContext.EmployeeWithCTs.DeleteObject(employeeWithCT);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'EntitiesWithNullFacetValuesForTimestampComparison' query.
        public IQueryable<EntityWithNullFacetValuesForTimestampComparison> GetEntitiesWithNullFacetValuesForTimestampComparison()
        {
            return this.ObjectContext.EntitiesWithNullFacetValuesForTimestampComparison;
        }
        
        public void InsertEntityWithNullFacetValuesForTimestampComparison(EntityWithNullFacetValuesForTimestampComparison entityWithNullFacetValuesForTimestampComparison)
        {
            if ((entityWithNullFacetValuesForTimestampComparison.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(entityWithNullFacetValuesForTimestampComparison, EntityState.Added);
            }
            else
            {
                this.ObjectContext.EntitiesWithNullFacetValuesForTimestampComparison.AddObject(entityWithNullFacetValuesForTimestampComparison);
            }
        }
        
        public void UpdateEntityWithNullFacetValuesForTimestampComparison(EntityWithNullFacetValuesForTimestampComparison currentEntityWithNullFacetValuesForTimestampComparison)
        {
            this.ObjectContext.EntitiesWithNullFacetValuesForTimestampComparison.AttachAsModified(currentEntityWithNullFacetValuesForTimestampComparison);
        }
        
        public void DeleteEntityWithNullFacetValuesForTimestampComparison(EntityWithNullFacetValuesForTimestampComparison entityWithNullFacetValuesForTimestampComparison)
        {
            if ((entityWithNullFacetValuesForTimestampComparison.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(entityWithNullFacetValuesForTimestampComparison, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.EntitiesWithNullFacetValuesForTimestampComparison.Attach(entityWithNullFacetValuesForTimestampComparison);
                this.ObjectContext.EntitiesWithNullFacetValuesForTimestampComparison.DeleteObject(entityWithNullFacetValuesForTimestampComparison);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'RequiredAttributeTestEntities' query.
        public IQueryable<RequiredAttributeTestEntity> GetRequiredAttributeTestEntities()
        {
            return this.ObjectContext.RequiredAttributeTestEntities;
        }
        
        public void InsertRequiredAttributeTestEntity(RequiredAttributeTestEntity requiredAttributeTestEntity)
        {
            if ((requiredAttributeTestEntity.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(requiredAttributeTestEntity, EntityState.Added);
            }
            else
            {
                this.ObjectContext.RequiredAttributeTestEntities.AddObject(requiredAttributeTestEntity);
            }
        }
        
        public void UpdateRequiredAttributeTestEntity(RequiredAttributeTestEntity currentRequiredAttributeTestEntity)
        {
            this.ObjectContext.RequiredAttributeTestEntities.AttachAsModified(currentRequiredAttributeTestEntity, this.ChangeSet.GetOriginal(currentRequiredAttributeTestEntity));
        }
        
        public void DeleteRequiredAttributeTestEntity(RequiredAttributeTestEntity requiredAttributeTestEntity)
        {
            if ((requiredAttributeTestEntity.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(requiredAttributeTestEntity, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.RequiredAttributeTestEntities.Attach(requiredAttributeTestEntity);
                this.ObjectContext.RequiredAttributeTestEntities.DeleteObject(requiredAttributeTestEntity);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'TimestampEntities' query.
        public IQueryable<TimestampEntity> GetTimestampEntities()
        {
            return this.ObjectContext.TimestampEntities;
        }
        
        public void InsertTimestampEntity(TimestampEntity timestampEntity)
        {
            if ((timestampEntity.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(timestampEntity, EntityState.Added);
            }
            else
            {
                this.ObjectContext.TimestampEntities.AddObject(timestampEntity);
            }
        }
        
        public void UpdateTimestampEntity(TimestampEntity currentTimestampEntity)
        {
            this.ObjectContext.TimestampEntities.AttachAsModified(currentTimestampEntity);
        }
        
        public void DeleteTimestampEntity(TimestampEntity timestampEntity)
        {
            if ((timestampEntity.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(timestampEntity, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.TimestampEntities.Attach(timestampEntity);
                this.ObjectContext.TimestampEntities.DeleteObject(timestampEntity);
            }
        }
    }
}
