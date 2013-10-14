using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Objects;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using OpenRiaServices;
using OpenRiaServices.DomainServices.EntityFramework;
using OpenRiaServices.DomainServices.Hosting;
using OpenRiaServices.DomainServices.Server;
using NorthwindPOCOModel;
using TestDomainServices.Testing;

namespace TestDomainServices.EF
{
    /// <summary>
    /// Test DomainService that targets an EF POCO model.
    /// </summary>
    [EnableClientAccess]
    public class NorthwindPOCO : LinqToEntitiesDomainService<NorthwindEntities>
    {
        #region Product methods
        public Product GetProductById(int id)
        {
            return this.ObjectContext.Products.SingleOrDefault(p => p.ProductID == id);
        }

        public IQueryable<Product> GetProducts()
        {
            return ObjectContext.Products;
        }

        public void InsertProduct(Product product)
        {
            if (GetEntityState(product) != EntityState.Detached)
            {
                ObjectContext.ObjectStateManager.ChangeObjectState(product, EntityState.Added);
            }
            else
            {
                ObjectContext.Products.AddObject(product);
            }
        }

        public void UpdateProduct(Product current)
        {
            ObjectContext.Products.AttachAsModified(current, this.ChangeSet.GetOriginal(current));
        }

        public void DeleteProduct(Product product)
        {
            if (GetEntityState(product) == EntityState.Detached)
            {
                ObjectContext.Products.Attach(product);
            }
            ObjectContext.Products.DeleteObject(product);
        }
        #endregion

        public IQueryable<Category> GetCategories()
        {
            return ObjectContext.Categories;
        }

        private EntityState GetEntityState(object entity)
        {
            ObjectStateEntry stateEntry = null;
            if (!this.ObjectContext.ObjectStateManager.TryGetObjectStateEntry(entity, out stateEntry))
            {
                return EntityState.Detached;
            }
            return stateEntry.State;
        }
    }

    /// <summary>
    /// Derived provider that overrides Context creation to use the current
    /// active connection.
    /// </summary>
    [EnableClientAccess]
    [ServiceContract(Name = "Northwind")]
    public class NorthwindPOCO_CUD : NorthwindPOCO
    {
        protected override NorthwindEntities CreateObjectContext()
        {
            NorthwindEntities context = null;

            string connection = ActiveConnections.Get("Northwind");
            if (!string.IsNullOrEmpty(connection))
            {
                // if there is an active connection in scope use it
                // Here we have to append the mapping file info to the connection string
                connection = string.Format("metadata=res://*/Northwind.csdl|res://*/Northwind.ssdl|res://*/Northwind.msl;provider=System.Data.SqlClient;provider connection string=\"{0}\"", connection);
                context = new NorthwindEntities(connection);
            }
            else
            {
                context = base.CreateObjectContext();
            }

            return context;
        }
    }
}
