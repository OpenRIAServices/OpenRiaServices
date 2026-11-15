using EFCoreModels.Northwind;

namespace OpenRiaServices.Client.Test
{
    public class NorthwindEntityContainer : EntityContainer
    {
        public NorthwindEntityContainer()
        {
            this.CreateEntitySet<Product>(((EntitySetOperations.Add | EntitySetOperations.Edit)
                            | EntitySetOperations.Remove));
            this.CreateEntitySet<Order>(((EntitySetOperations.Add | EntitySetOperations.Edit)
                            | EntitySetOperations.Remove));
            this.CreateEntitySet<Order_Detail>(((EntitySetOperations.Add | EntitySetOperations.Edit)
                            | EntitySetOperations.Remove));
            this.CreateEntitySet<Customer>(((EntitySetOperations.Add | EntitySetOperations.Edit)
                            | EntitySetOperations.Remove));
            this.CreateEntitySet<Category>(((EntitySetOperations.Add | EntitySetOperations.Edit)
                            | EntitySetOperations.Remove));
        }
    }
}
