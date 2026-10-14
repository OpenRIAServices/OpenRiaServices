using System.ComponentModel.DataAnnotations;
using System.ComponentModel.Design;
using System.Reflection;
using System.Security.Principal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace OpenRiaServices.Server.EntityFrameworkCore.Test
{
    [TestClass]
    public class ConflictDisclosureTests
    {
        [ClassInitialize]
        public static async Task InitializeDatabase(TestContext testContext)
        {
            ConflictContext.ConnectionString = $"Server=(localdb)\\MSSQLLocalDB;Database=OpenRiaServices_ConflictDisclosure_{Guid.NewGuid():N};Integrated Security=true;TrustServerCertificate=true";
            using var context = new ConflictContext();
            await context.Database.EnsureCreatedAsync();
            context.AddRange(
                new ConflictEntity { Id = 1, Name = "Stored", OwnerId = "owner" },
                new ConflictEntity { Id = 2, Name = "Secret", OwnerId = "other" });
            await context.SaveChangesAsync();
        }

        [ClassCleanup]
        public static async Task DeleteDatabase()
        {
            using var context = new ConflictContext();
            await context.Database.EnsureDeletedAsync();
        }

        [TestMethod]
        public async Task DefaultImplementation_PreservesConflictDetails()
        {
            using var service = new ConflictService();
            var operation = CreateOperation(1);
            Initialize(service);

            Assert.IsFalse(await service.SubmitAsync(new ChangeSet(new[] { operation }), CancellationToken.None));

            var snapshot = (ConflictEntity)operation.StoreEntity;
            Assert.AreEqual("Stored", snapshot.Name);
            Assert.AreEqual("owner", snapshot.OwnerId);
            CollectionAssert.Contains(operation.ConflictMembers.ToArray(), nameof(ConflictEntity.Name));
            Assert.IsFalse(operation.IsDeleteConflict);
            Assert.AreEqual(1, service.ResolveCalls);
        }

        [TestMethod]
        public async Task DefaultImplementation_PreservesDeleteConflict()
        {
            using var service = new ConflictService();
            var operation = CreateOperation(99);
            Initialize(service);

            Assert.IsFalse(await service.SubmitAsync(new ChangeSet(new[] { operation }), CancellationToken.None));

            Assert.IsTrue(operation.IsDeleteConflict);
            Assert.IsNull(operation.StoreEntity);
            Assert.IsNull(operation.ConflictMembers);
        }

        [TestMethod]
        public async Task Override_ReceivesSnapshotAndCancellationToken()
        {
            using var service = new AuthorizedConflictService();
            var operation = CreateOperation(1);
            using var cancellation = new CancellationTokenSource();
            Initialize(service);
            service.Authorize = async (entry, snapshot, token) =>
            {
                await Task.Yield();
                Assert.AreSame(operation, entry);
                Assert.AreEqual(cancellation.Token, token);
                Assert.AreEqual("attacker", ((ConflictEntity)entry.Entity).OwnerId);
                Assert.AreEqual("attacker", ((ConflictEntity)entry.OriginalEntity).OwnerId);
                Assert.AreEqual("owner", ((ConflictEntity)snapshot!).OwnerId);
                Assert.AreNotSame(entry.Entity, snapshot);
                Assert.IsNull(entry.StoreEntity);
                Assert.IsNull(entry.ConflictMembers);
                Assert.IsFalse(entry.IsDeleteConflict);
                return true;
            };

            Assert.IsFalse(await service.SubmitAsync(new ChangeSet(new[] { operation }), cancellation.Token));
            Assert.AreEqual(1, service.DisclosureCalls);
            Assert.IsNotNull(operation.StoreEntity);
        }

        [TestMethod]
        [DataRow(1)]
        [DataRow(99)]
        public async Task Denial_AbortsSubmitWithoutRetry(int id)
        {
            using var service = new AuthorizedConflictService { ShouldResolve = true };
            var operation = CreateOperation(id);
            Initialize(service);
            service.Authorize = (entry, snapshot, token) =>
            {
                Assert.AreEqual(id == 99, snapshot is null);
                return new ValueTask<bool>(false);
            };

            await Assert.ThrowsExactlyAsync<UnauthorizedAccessException>(async () =>
                await service.SubmitAsync(new ChangeSet(new[] { operation }), CancellationToken.None));
            Assert.AreEqual(1, service.DisclosureCalls);
            Assert.AreEqual(0, service.ResolveCalls);
            Assert.AreEqual(1, service.SaveCalls);
            Assert.IsInstanceOfType<UnauthorizedAccessException>(service.ReportedError);
        }

        private static ChangeSetEntry CreateOperation(int id)
        {
            return new ChangeSetEntry(id,
                new ConflictEntity { Id = id, Name = "Submitted", OwnerId = "attacker" },
                new ConflictEntity { Id = id, Name = "Original", OwnerId = "attacker" },
                DomainOperation.Update);
        }

        private static void Initialize(DomainService service)
        {
            service.Initialize(new DomainServiceContext(new ServiceContainer(),
                new GenericPrincipal(new GenericIdentity("owner"), Array.Empty<string>()), DomainOperationType.Submit));
        }

        public class ConflictEntity
        {
            [Key]
            public int Id { get; set; }

            [ConcurrencyCheck]
            public string Name { get; set; } = string.Empty;

            public string OwnerId { get; set; } = string.Empty;
        }

        public class ConflictContext : DbContext
        {
            public static string ConnectionString { get; set; } = string.Empty;
            public int SaveCalls { get; private set; }

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                optionsBuilder.UseSqlServer(ConnectionString);
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<ConflictEntity>().Property(entity => entity.Id).ValueGeneratedNever();
            }

            public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            {
                SaveCalls++;
                return base.SaveChangesAsync(cancellationToken);
            }
        }

        [EnableClientAccess]
        public class ConflictService : DbDomainService<ConflictContext>
        {
            public int ResolveCalls { get; private set; }
            public bool ShouldResolve { get; set; }
            public int SaveCalls => DbContext.SaveCalls;
            public Exception? ReportedError { get; private set; }

            public IQueryable<ConflictEntity> GetEntities() => DbContext.Set<ConflictEntity>();

            public void UpdateEntity(ConflictEntity entity) => AttachAsModified(entity);

            protected override bool ResolveConflicts(IEnumerable<EntityEntry> conflicts)
            {
                ResolveCalls++;
                return ShouldResolve;
            }

            protected override void OnError(DomainServiceErrorInfo errorInfo)
            {
                ReportedError = errorInfo.Error;
                base.OnError(errorInfo);
            }
        }

        public class AuthorizedConflictService : ConflictService
        {
            public Func<ChangeSetEntry, object?, CancellationToken, ValueTask<bool>> Authorize { get; set; }
                = (entry, snapshot, token) => new ValueTask<bool>(false);
            public int DisclosureCalls { get; private set; }

            protected override ValueTask<bool> CanDiscloseConflictAsync(ChangeSetEntry changeSetEntry, object storeEntity, CancellationToken cancellationToken)
            {
                DisclosureCalls++;
                return Authorize(changeSetEntry, storeEntity, cancellationToken);
            }
        }
    }
}
