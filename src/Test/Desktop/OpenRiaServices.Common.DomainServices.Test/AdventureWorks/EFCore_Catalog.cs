﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using OpenRiaServices.Server.EntityFrameworkCore;
using OpenRiaServices.Server;
using EFCoreModels.AdventureWorks;
using Microsoft.EntityFrameworkCore;

namespace TestDomainServices.EFCore
{
    [EnableClientAccess]
    public partial class Catalog : DbDomainService<AdventureworksContext>
    {
        protected override AdventureworksContext CreateDbContext()
        {
            return new AdventureworksContext();
        }

        #region Product methods
        [Query]
        public IQueryable<Product> GetProducts()
        {
            return from p in this.DbContext.Products
                   orderby p.ProductID
                   select p;
        }

        [Query]
        [OutputCache(OutputCacheLocation.Server, duration: 5)]
        public IQueryable<Product> GetProductsWithCaching()
        {
            return from p in this.DbContext.Products
                   select p;
        }

        [Query]
        public IQueryable<Product> GetProductsWithCustomTotalCount(out int totalCount)
        {
            totalCount = 6;
            return from p in this.DbContext.Products
                   select p;
        }

        [Query]
        public IQueryable<Product> GetProductsByCategory(int subCategoryID)
        {
            return from p in this.DbContext.Products
                    where p.ProductSubcategoryID == subCategoryID
                    select p;
        }

        [Insert]
        public void AddProduct(Product product)
        {
            this.DbContext.Products.Add(product);
        }

        [Update]
        public void UpdateProduct(Product current)
        {
            AttachAsModified(current);
        }

        [Delete]
        public void DeleteProduct(Product current)
        {
            this.DbContext.Products.Attach(current);
            this.DbContext.Products.Remove(current);
        }
        #endregion

        #region PurchaseOrder methods
        [Query]
        public IQueryable<PurchaseOrder> GetPurchaseOrders()
        {
            return this.DbContext.PurchaseOrders.Include("PurchaseOrderDetails").Include("PurchaseOrderDetails.Product");
        }

        [Insert]
        public void AddPurchaseOrder(PurchaseOrder purchaseOrder)
        {
            this.DbContext.PurchaseOrders.Add(purchaseOrder);
        }

        [Update]
        public void UpdatePurchaseOrder(PurchaseOrder current)
        {
            this.DbContext.PurchaseOrders.AttachAsModified(current, this.ChangeSet.GetOriginal(current), this.DbContext);
        }

        [Delete]
        public void DeletePurchaseOrder(PurchaseOrder current)
        {
            this.DbContext.PurchaseOrders.Attach(current);
            this.DbContext.PurchaseOrders.Remove(current);
        }
        #endregion

        #region PurchaseOrderDetail methods
        [Insert]
        public void InsertPurchaseOrderDetail(PurchaseOrderDetail purchaseOrderDetail)
        {
            this.DbContext.PurchaseOrderDetails.Add(purchaseOrderDetail);
        }

        [Update]
        public void UpdatePurchaseOrderDetail(PurchaseOrderDetail current)
        {
            this.DbContext.PurchaseOrderDetails.AttachAsModified(current, this.ChangeSet.GetOriginal(current), this.DbContext);
        }

        [Delete]
        public void DeletePurchaseOrderDetail(PurchaseOrderDetail current)
        {
            this.DbContext.PurchaseOrderDetails.Attach(current);
            this.DbContext.PurchaseOrderDetails.Remove(current);
        }
        #endregion

        #region Employee methods
        [Query]
        public IQueryable<Employee> GetEmployees()
        {
            return this.DbContext.Employees;
        }

        [Query]
        public IEnumerable<EmployeeInfo> GetEmployeeInfos()
        {
            var results = from e in this.DbContext.Employees
                          select new
                          {
                              e.EmployeeID,
                              e.Contact.FirstName,
                              e.Contact.LastName,
                              e.SalesPerson.SalesTerritory.TerritoryID
                          };
            return results as IEnumerable<EmployeeInfo>;
        }
        #endregion
    }
}
