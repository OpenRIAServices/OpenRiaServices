
namespace BizLogic.Test
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.Data.Entity;
    using System.Data.Entity.Core.Objects;
    using System.Linq;
    using NorthwindPOCOModel;
    using OpenRiaServices.DomainServices.EntityFramework;
    using OpenRiaServices.DomainServices.Hosting;
    using OpenRiaServices.DomainServices.Server;
    
    
    // Implements application logic using the NorthwindEntities context.
    // TODO: Add your application logic to these methods or in additional methods.
    // TODO: Wire up authentication (Windows/ASP.NET Forms) and uncomment the following to disable anonymous access
    // Also consider adding roles to restrict access as appropriate.
    // [RequiresAuthentication]
    [EnableClientAccess()]
    public class EF_Northwind_POCO : LinqToEntitiesDomainService<NorthwindEntities>
    {
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'Categories' query.
        public IQueryable<Category> GetCategories()
        {
            return this.ObjectContext.Categories;
        }
        
        public void InsertCategory(Category category)
        {
            if ((this.GetEntityState(category) != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(category, EntityState.Added);
            }
            else
            {
                this.ObjectContext.Categories.AddObject(category);
            }
        }
        
        public void UpdateCategory(Category currentCategory)
        {
            this.ObjectContext.Categories.AttachAsModified(currentCategory, this.ChangeSet.GetOriginal(currentCategory));
        }
        
        public void DeleteCategory(Category category)
        {
            if ((this.GetEntityState(category) != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(category, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.Categories.Attach(category);
                this.ObjectContext.Categories.DeleteObject(category);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'Products' query.
        public IQueryable<Product> GetProducts()
        {
            return this.ObjectContext.Products;
        }
        
        public void InsertProduct(Product product)
        {
            if ((this.GetEntityState(product) != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(product, EntityState.Added);
            }
            else
            {
                this.ObjectContext.Products.AddObject(product);
            }
        }
        
        public void UpdateProduct(Product currentProduct)
        {
            this.ObjectContext.Products.AttachAsModified(currentProduct, this.ChangeSet.GetOriginal(currentProduct));
        }
        
        public void DeleteProduct(Product product)
        {
            if ((this.GetEntityState(product) != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(product, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.Products.Attach(product);
                this.ObjectContext.Products.DeleteObject(product);
            }
        }
        
        
        private EntityState GetEntityState(object entity)
        {
            ObjectStateEntry stateEntry = null;
            if ((this.ObjectContext.ObjectStateManager.TryGetObjectStateEntry(entity, out stateEntry) == false))
            {
                return EntityState.Detached;
            }
            return stateEntry.State;
        }

    }
}
