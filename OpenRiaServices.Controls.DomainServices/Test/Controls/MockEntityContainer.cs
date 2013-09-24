using OpenRiaServices.DomainServices.Client;

namespace System.Windows.Controls.Test
{
    public class MockEntityContainer : EntityContainer
    {
        public void CreateSet<TEntity>(EntitySetOperations operations) where TEntity : Entity, new()
        {
            base.CreateEntitySet<TEntity>(operations);
        }
    }
}
