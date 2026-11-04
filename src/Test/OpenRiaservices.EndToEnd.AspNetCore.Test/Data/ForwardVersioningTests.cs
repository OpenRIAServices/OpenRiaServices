#if NET10_0
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using ForwardVersioning;
using OpenRiaServices;
using OpenRiaServices.Client.DomainClients;
using RiaClient = OpenRiaServices.Client;

namespace OpenRiaServices.Client.Test
{
    [TestClass]
    public class ForwardVersioningTests
    {
        [TestMethod]
        public async Task MessagePack_MergeIntoCurrent_SubmitPreservesUnknownPropertiesAndRoundtripOriginal()
        {
            using var httpClient = new HttpClient();
            var factory = new MessagePackHttpDomainClientFactory(
                TestURIs.RootURI, uri =>
                {
                    httpClient.BaseAddress = uri;
                    return httpClient;
                });
            var domainClient = factory.CreateDomainClient(
                typeof(ForwardVersioningDomainContext.IServiceContract),
                new Uri("ForwardVersioning-ForwardVersioningDomainService", UriKind.Relative), false);
            var context = new ForwardVersioningDomainContext(domainClient);

            var entity = (await context.LoadAsync(context.GetEntitiesQuery(1))).Single();
            entity.Name = "client-entity";
            entity.Details.Child.Name = "client-nested";

            var merged = (await context.LoadAsync(context.GetEntitiesQuery(2), LoadBehavior.MergeIntoCurrent)).Single();
            Assert.AreSame(entity, merged);
            Assert.AreEqual("client-entity", entity.Name);
            Assert.AreEqual("complex-2", entity.Details.Name);
            Assert.AreEqual("client-nested", entity.Details.Child.Name);

            // The newer server checks properties absent from this client, including the original entity.
            await context.SubmitChangesAsync();

            Assert.AreEqual("verified", entity.Name);
            Assert.AreEqual("client-nested", entity.Details.Child.Name);
            Assert.AreEqual(EntityState.Unmodified, entity.EntityState);
            Assert.IsFalse(context.HasChanges);
        }
    }
}

namespace ForwardVersioning
{
    public sealed class ForwardVersioningDomainContext : RiaClient.DomainContext
    {
        public ForwardVersioningDomainContext(RiaClient.DomainClient domainClient)
            : base(domainClient)
        {
        }

        public RiaClient.EntityQuery<ForwardVersioningEntity> GetEntitiesQuery(int version)
            => CreateQuery<ForwardVersioningEntity>("GetEntities",
                new Dictionary<string, object> { { "version", version } }, false, true);

        protected override RiaClient.EntityContainer CreateEntityContainer()
            => new ForwardVersioningEntityContainer();

        public interface IServiceContract
        {
            IAsyncResult BeginGetEntities(int version, AsyncCallback callback, object asyncState);
            RiaClient.QueryResult<ForwardVersioningEntity> EndGetEntities(IAsyncResult result);

            IAsyncResult BeginSubmitChanges(IEnumerable<RiaClient.ChangeSetEntry> changeSet, AsyncCallback callback, object asyncState);
            IEnumerable<RiaClient.ChangeSetEntry> EndSubmitChanges(IAsyncResult result);
        }

        private sealed class ForwardVersioningEntityContainer : RiaClient.EntityContainer
        {
            public ForwardVersioningEntityContainer()
            {
                CreateEntitySet<ForwardVersioningEntity>(RiaClient.EntitySetOperations.Edit);
            }
        }
    }

    // Deliberately model an older client with no declarations for the server's unknown properties.
    [DataContract]
    [RoundtripOriginal]
    public sealed class ForwardVersioningEntity : RiaClient.Entity
    {
        private string _name;
        private ForwardVersioningComplex _details;

        [Key]
        [DataMember]
        public int Id { get; set; }

        [DataMember]
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    RaiseDataMemberChanging(nameof(Name));
                    _name = value;
                    RaiseDataMemberChanged(nameof(Name));
                }
            }
        }

        [DataMember]
        public ForwardVersioningComplex Details
        {
            get => _details;
            set
            {
                if (_details != value)
                {
                    RaiseDataMemberChanging(nameof(Details));
                    _details = value;
                    RaiseDataMemberChanged(nameof(Details));
                }
            }
        }

        public override object GetIdentity() => Id;
    }

    [DataContract]
    [RoundtripOriginal]
    public sealed class ForwardVersioningComplex : RiaClient.ComplexObject
    {
        private string _name;
        private ForwardVersioningNestedComplex _child;

        [DataMember]
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    RaiseDataMemberChanging(nameof(Name));
                    _name = value;
                    RaiseDataMemberChanged(nameof(Name));
                }
            }
        }

        [DataMember]
        public ForwardVersioningNestedComplex Child
        {
            get => _child;
            set
            {
                if (_child != value)
                {
                    RaiseDataMemberChanging(nameof(Child));
                    _child = value;
                    RaiseDataMemberChanged(nameof(Child));
                }
            }
        }
    }

    [DataContract]
    [RoundtripOriginal]
    public sealed class ForwardVersioningNestedComplex : RiaClient.ComplexObject
    {
        private string _name;

        [DataMember]
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    RaiseDataMemberChanging(nameof(Name));
                    _name = value;
                    RaiseDataMemberChanged(nameof(Name));
                }
            }
        }
    }
}
#endif
