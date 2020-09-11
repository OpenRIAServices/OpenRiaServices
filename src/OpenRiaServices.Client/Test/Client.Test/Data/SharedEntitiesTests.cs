extern alias SSmDsClient;

using System.Globalization;
using System.Linq;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Silverlight.Testing;
using SharedEntities;
using TestDescription = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

namespace OpenRiaServices.DomainServices.Client.Test
{
    using Resource = SSmDsClient::OpenRiaServices.DomainServices.Client.Resource;

    [TestClass]
    public class SharedEntitiesTests : UnitTestBase
    {
        private ExposeChildEntityDomainContext DomainChildContext { get; set; }
        private ExposeParentEntityDomainContext DomainParentContext { get; set; }
        private LoadOperation LoadedChildEntity { get; set; }
        private LoadOperation LoadedParentEntity { get; set; }
        private SubmitOperation SubmittedChildEntity { get; set; }
        private SubmitOperation SubmittedParentEntity { get; set; }

        [TestInitialize]
        [TestDescription("Initialize context before each test")]
        public void Initialize()
        {
            this.DomainChildContext = new ExposeChildEntityDomainContext(TestURIs.SharedEntitiesChild);
            this.DomainParentContext = new ExposeParentEntityDomainContext(TestURIs.SharedEntitiesParent);
            this.LoadedChildEntity = null;
            this.LoadedParentEntity = null;
            this.SubmittedChildEntity = null;
            this.SubmittedParentEntity = null;
        }

        [TestMethod]
        [TestDescription("Testing inheritance with serialization in the shared case.")]
        [Asynchronous]
        public void SharedEntities_AccessSharedInheritance()
        {
            this.EnqueueLoadEntityX();

            this.EnqueueCallback(
                () =>
                {
                    EntityX x = (EntityY)this.DomainChildContext.EntityXes.First();
                    EntityY y = (EntityY)x;
                    Assert.IsNotNull(y.EntityZ, "y.EntityZ should be set");
                    Assert.AreEqual(y.IdZ, y.EntityZ.Id, "y.IdZ should reflect EntityZ.Id");
                    Assert.AreNotEqual(y.IdZ, 0, "y.IdZ should not be 0");
                    // When Y is serialized DCS serializes X' properties then Y's properties.
                    // If the surrogates were loaded correctly, X' ZProp should be read before Y's IdZ.
                    // If surrogates were not loaded correctly, ZProp would not be set.
                    Assert.AreNotEqual(x.ZProp, 0, "x.ZProp cannot be zero.");
                });

            this.EnqueueTestComplete();
        }

        [TestMethod]
        [TestDescription("Accessing association properties exposed by the DomainService returns an object")]
        [Asynchronous]
        public void SharedEntities_AccessRightPropertySucceeds()
        {
            this.EnqueueLoadEntityA();

            this.EnqueueCallback(
                () =>
                {
                    EntityB b = this.DomainChildContext.EntityAs.First().EntityB;
                    Assert.IsNotNull(b);
                    EntityC c = this.DomainParentContext.EntityAs.First().EntityC;
                    Assert.IsNotNull(c);
                });

            this.EnqueueTestComplete();
        }

        [TestMethod]
        [TestDescription("Accessing association properties not exposed by the DomainService returns null")]
        [Asynchronous]
        public void SharedEntities_AccessWrongPropertyReturnsNull()
        {
            this.EnqueueLoadEntityA();

            this.EnqueueCallback(
                () =>
                {
                    EntityC c = this.DomainChildContext.EntityAs.First().EntityC;
                    Assert.IsNull(c);
                    EntityB b = this.DomainParentContext.EntityAs.First().EntityB;
                    Assert.IsNull(b);
                });

            this.EnqueueTestComplete();
        }

        [TestMethod]
        [TestDescription("Accessing named update method from the correct DomainContext succeeds")]
        [Asynchronous]
        public void SharedEntities_CallRightMethodSucceeds()
        {
            this.EnqueueLoadEntityA();

            this.EnqueueCallback(
                () =>
                {
                    EntityA a1 = ((LoadOperation<EntityA>)this.LoadedChildEntity).Entities.First();
                    EntityA a2 = ((LoadOperation<EntityA>)this.LoadedParentEntity).Entities.First();

                    a1.UpdateAThroughChild();
                    a2.UpdateAThroughParent();

                    this.DomainChildContext.SubmitChanges(this.OnSubmitABXCallback, null);
                    this.DomainParentContext.SubmitChanges(this.OnSubmitACYCallback, null);
                });

            this.EnqueueConditional(
                () =>
                {
                    if (this.SubmittedChildEntity == null || this.SubmittedParentEntity == null)
                    {
                        return false;
                    }

                    this.VerifyOperationSucceeded(this.SubmittedChildEntity);
                    this.VerifyOperationSucceeded(this.SubmittedParentEntity);
                    return true;
                });

            this.EnqueueTestComplete();
        }

        [TestMethod]
        [TestDescription("Accessing named update method from the wrong DomainContext fails on Submit")]
        [Asynchronous]
        public void SharedEntities_CallWrongMethodFails()
        {
            this.EnqueueLoadEntityA();

            this.EnqueueCallback(
                () =>
                {
                    EntityA a1 = ((LoadOperation<EntityA>)this.LoadedChildEntity).Entities.First();
                    EntityA a2 = ((LoadOperation<EntityA>)this.LoadedParentEntity).Entities.First();

                    a1.UpdateAThroughParent();
                    a2.UpdateAThroughChild();

                    ExceptionHelper.ExpectInvalidOperationException(delegate
                    {
                        this.DomainChildContext.SubmitChanges(this.OnSubmitABXCallback, null);
                    }, string.Format(CultureInfo.CurrentCulture, Resource.DomainContext_NamedUpdateMethodDoesNotExist, "UpdateAThroughParent", a1.GetType(), this.DomainChildContext.GetType()));

                    ExceptionHelper.ExpectInvalidOperationException(delegate
                    {
                        this.DomainParentContext.SubmitChanges(this.OnSubmitACYCallback, null);
                    }, string.Format(CultureInfo.CurrentCulture, Resource.DomainContext_NamedUpdateMethodDoesNotExist, "UpdateAThroughChild", a2.GetType(), this.DomainParentContext.GetType()));
                });

            this.EnqueueTestComplete();
        }

        private void EnqueueLoadEntityA()
        {
            this.EnqueueLoadEntities<EntityA, EntityA>(this.DomainChildContext.GetAQuery(), this.DomainParentContext.GetAQuery());
        }

        private void EnqueueLoadEntityX()
        {
            this.EnqueueLoadEntities<EntityY, EntityX>(this.DomainChildContext.GetYQuery(), this.DomainParentContext.GetXQuery());
        }

        private void EnqueueLoadEntities<TEntityABX, TEntityACY>(EntityQuery<TEntityABX> abx, EntityQuery<TEntityACY> acy)
            where TEntityABX : Entity
            where TEntityACY : Entity
        {
            this.EnqueueCallback(
                () => this.DomainChildContext.Load(abx, this.OnLoadABXCallback, null));

            this.EnqueueCallback(
                () => this.DomainParentContext.Load(acy, this.OnLoadACYCallback, null));

            this.EnqueueConditional(
                () =>
                {
                    if (this.LoadedChildEntity == null || this.LoadedParentEntity == null)
                    {
                        return false;
                    }

                    this.VerifyOperationSucceeded(this.LoadedChildEntity);
                    this.VerifyOperationSucceeded(this.LoadedParentEntity);
                    return true;
                });
        }


        private void OnLoadABXCallback<TEntityABX>(LoadOperation<TEntityABX> loadOperation)
            where TEntityABX : Entity
        {
            this.LoadedChildEntity = loadOperation;
        }

        private void OnLoadACYCallback<TEntityACY>(LoadOperation<TEntityACY> loadOperation)
            where TEntityACY : Entity
        {
            this.LoadedParentEntity = loadOperation;
        }

        private void OnSubmitABXCallback(SubmitOperation submitOperation)
        {
            this.SubmittedChildEntity = submitOperation;
        }

        private void OnSubmitACYCallback(SubmitOperation submitOperation)
        {
            this.SubmittedParentEntity = submitOperation;
        }

        private void VerifyOperationSucceeded(OperationBase operation)
        {
            Assert.IsFalse(operation.HasError, string.Format("Operation of type {0} contains error {1}", operation.GetType(), operation.Error));
        }
    }
}
