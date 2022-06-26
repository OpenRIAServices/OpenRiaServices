extern alias SystemWebDomainServices;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Core.Metadata.Edm;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security;
using OpenRiaServices.Client.Test;
using OpenRiaServices.Server.EntityFrameworkCore;
using OpenRiaServices.Hosting.Wcf;
using OpenRiaServices.Server;
using System.Threading;
using Cities;
using OpenRiaServices.LinqToSql;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Silverlight.Testing;
using TestDomainServices;
using DescriptionAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;
using TheTypeDescriptorExtensions = SystemWebDomainServices::OpenRiaServices.Server.TypeDescriptorExtensions;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Metadata;

namespace OpenRiaServices.Server.Test
{

    public partial class DomainServiceDescriptionTest
    {

        [TestMethod]
        public void EFCoreTypeDescriptor_ExcludedEntityMembers()
        {
            PropertyDescriptor pd = TypeDescriptor.GetProperties(typeof(EFCorePocoEntity_IEntityChangeTracker))["EntityState"];
            Assert.IsTrue(EntityFrameworkCore.EFCoreTypeDescriptor.ShouldExcludeEntityMember(pd));
        }

    }

    [EnableClientAccess]
    public class ValidAssociationCoreDbDomainService : DbDomainService<EFCoreModels.Northwind.EFCoreDbCtxNorthwindEntities>
    {
        [Query]
        public IEnumerable<EFCoreModels.Northwind.Category> GetAs()
        {
            return null;
        }
    }

    public class EFCorePocoEntity_IEntityChangeTracker : IChangeDetector
    {
        void IChangeDetector.DetectChanges(IStateManager stateManager)
        {
            throw new NotImplementedException();
        }

        void IChangeDetector.DetectChanges(InternalEntityEntry entry)
        {
            throw new NotImplementedException();
        }

        void IChangeDetector.PropertyChanged(InternalEntityEntry entry, IPropertyBase propertyBase, bool setModified)
        {
            throw new NotImplementedException();
        }

        void IChangeDetector.PropertyChanging(InternalEntityEntry entry, IPropertyBase propertyBase)
        {
            throw new NotImplementedException();
        }

        void IChangeDetector.Resume()
        {
            throw new NotImplementedException();
        }

        void IChangeDetector.Suspend()
        {
            throw new NotImplementedException();
        }

        public Microsoft.EntityFrameworkCore.EntityState EntityState
        {
            get { throw new NotImplementedException(); }
        }
    }
}
