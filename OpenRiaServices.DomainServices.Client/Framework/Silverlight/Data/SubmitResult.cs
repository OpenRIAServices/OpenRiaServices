using System;
using System.Linq;

namespace OpenRiaServices.DomainServices.Client
{
    public class SubmitResult
    {
        private EntityChangeSet _changeSet;

        public SubmitResult(EntityChangeSet changeSet)
        {
            _changeSet = changeSet;
        }

        public EntityChangeSet ChangeSet { get { return _changeSet; } }
    }
}
