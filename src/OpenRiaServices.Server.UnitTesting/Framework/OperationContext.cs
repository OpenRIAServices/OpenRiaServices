using System;

namespace OpenRiaServices.Server.UnitTesting
{
    internal class OperationContext
    {
        private readonly DomainServiceContext _domainServiceContext;
        private readonly DomainService _domainService;
        private readonly DomainServiceDescription _domainServiceDescription;

        public OperationContext(DomainServiceContext domainServiceContext, DomainService domainService, DomainServiceDescription domainServiceDescription)
        {
            if (domainServiceContext == null)
            {
                throw new ArgumentNullException(nameof(domainServiceContext));
            }
            if (domainService == null)
            {
                throw new ArgumentNullException(nameof(domainService));
            }
            if (domainServiceDescription == null)
            {
                throw new ArgumentNullException(nameof(domainServiceDescription));
            }

            this._domainServiceContext = domainServiceContext;
            this._domainService = domainService;
            this._domainServiceDescription = domainServiceDescription;
        }

        public DomainServiceContext DomainServiceContext
        {
            get { return this._domainServiceContext; }
        }

        public DomainService DomainService
        {
            get { return this._domainService; }
        }

        public DomainServiceDescription DomainServiceDescription
        {
            get { return this._domainServiceDescription; }
        }

        public string OperationName { get; set; }
    }
}
