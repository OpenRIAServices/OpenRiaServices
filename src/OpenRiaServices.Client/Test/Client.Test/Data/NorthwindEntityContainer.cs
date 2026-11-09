extern alias SSmDsClient;

namespace OpenRiaServices.Client.Test
{
    public class NorthwindEntityContainer : EntityContainer
    {
        public NorthwindEntityContainer()
        {
            this.CreateEntitySet<DataTests.Northwind.LTS.Product>(((EntitySetOperations.Add | EntitySetOperations.Edit)
                            | EntitySetOperations.Remove));
            this.CreateEntitySet<DataTests.Northwind.LTS.Order>(((EntitySetOperations.Add | EntitySetOperations.Edit)
                            | EntitySetOperations.Remove));
            this.CreateEntitySet<DataTests.Northwind.LTS.Order_Detail>(((EntitySetOperations.Add | EntitySetOperations.Edit)
                            | EntitySetOperations.Remove));
            this.CreateEntitySet<DataTests.Northwind.LTS.Customer>(((EntitySetOperations.Add | EntitySetOperations.Edit)
                            | EntitySetOperations.Remove));
            this.CreateEntitySet<DataTests.Northwind.LTS.Category>(((EntitySetOperations.Add | EntitySetOperations.Edit)
                            | EntitySetOperations.Remove));
        }
    }
}
