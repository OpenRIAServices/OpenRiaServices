extern alias SSmDsClient;

namespace OpenRiaServices.Client.Test
{
    public class NorthwindEntityContainer : EntityContainer
    {
        public NorthwindEntityContainer()
        {
            this.CreateEntitySet<EFCoreModels.Northwind.Product>(((EntitySetOperations.Add | EntitySetOperations.Edit)
                            | EntitySetOperations.Remove));
            this.CreateEntitySet<EFCoreModels.Northwind.Order>(((EntitySetOperations.Add | EntitySetOperations.Edit)
                            | EntitySetOperations.Remove));
            this.CreateEntitySet<EFCoreModels.Northwind.Order_Detail>(((EntitySetOperations.Add | EntitySetOperations.Edit)
                            | EntitySetOperations.Remove));
            this.CreateEntitySet<EFCoreModels.Northwind.Customer>(((EntitySetOperations.Add | EntitySetOperations.Edit)
                            | EntitySetOperations.Remove));
            this.CreateEntitySet<EFCoreModels.Northwind.Category>(((EntitySetOperations.Add | EntitySetOperations.Edit)
                            | EntitySetOperations.Remove));
        }
    }
}
