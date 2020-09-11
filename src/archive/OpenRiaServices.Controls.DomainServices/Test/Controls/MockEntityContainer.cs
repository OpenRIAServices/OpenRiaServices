using OpenRiaServices.Client;

namespace OpenRiaServices.Controls.DomainServices.Test
{
    public class MockEntityContainer : EntityContainer
    {
        public void CreateSet<TEntity>(EntitySetOperations operations) where TEntity : Entity, new()
        {
            base.CreateEntitySet<TEntity>(operations);
        }
    }
}
