using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DataTests.AdventureWorks.LTS;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using System.Diagnostics;
using TestDomainServices;

namespace OpenRiaServices.Client.Test
{

    public static class TestHelperMethods
    {
        /// <summary>
        /// Perform a submit on the specified context using the DomainClient directy. This bypasses all context operations,
        /// validation, etc.
        /// </summary>
        /// <param name="ctxt">The context to submit on</param>
        /// <param name="callback">The callback to execute when the submit completes</param>
        public static void SubmitDirect(DomainContext ctxt, Action<SubmitCompletedResult> callback)
        {
            EntityChangeSet cs = ctxt.EntityContainer.GetChanges();
            Debug.Assert(!cs.IsEmpty, "No changes to submit!");


            SynchronizationContext syncContext = SynchronizationContext.Current ?? new SynchronizationContext();

            ctxt.DomainClient.SubmitAsync(cs, CancellationToken.None)
                .ContinueWith(task =>
                {
                    SubmitCompletedResult submitResults = task.GetAwaiter().GetResult();
                    syncContext.Post(
                        delegate
                        {
                            callback(submitResults);
                        },
                        null);
                });
        }

        /// <summary>
        /// Method used to turn property level validation on or off for an entity. This allows invalid
        /// data to be pumped into the entity, which can be useful in testing.
        /// </summary>
        /// <param name="entity">The entity</param>
        /// <param name="enable">True to turn validation on, false to turn it off.</param>
        public static void EnableValidation(Entity entity, bool enable)
        {
            if (enable)
            {
                entity.OnDeserialized(new System.Runtime.Serialization.StreamingContext());
            }
            else
            {
                entity.OnDeserializing(new System.Runtime.Serialization.StreamingContext());
            }
        }

        /// <summary>
        /// Method used to turn property level validation on or off for a complex object. This allows invalid
        /// data to be pumped into the instance, which can be useful in testing.
        /// </summary>
        /// <param name="entity">The complex object</param>
        /// <param name="enable">True to turn validation on, false to turn it off.</param>
        public static void EnableValidation(ComplexObject complexObject, bool enable)
        {
            if (enable)
            {
                complexObject.OnDeserialized(new System.Runtime.Serialization.StreamingContext());
            }
            else
            {
                complexObject.OnDeserializing(new System.Runtime.Serialization.StreamingContext());
            }
        }

        public static void DefaultOperationAction<T>(T operation) where T : OperationBase
        {
            if (operation.HasError)
            {
                operation.MarkErrorAsHandled();
            }
        }

        public static void AssertOperationSuccess(OperationBase operation)
        {
            string errorMsg = string.Empty;
            if (operation.HasError)
            {
                errorMsg = operation.Error.Message + " : " + operation.Error.StackTrace;
            }
            Assert.IsFalse(operation.HasError, errorMsg);
        }

        public static T CloneEntity<T>(T entity) where T : Entity, new()
        {
            T clone = new T();
            clone.ApplyState(entity.ExtractState());
            return clone;
        }

        /// <summary>
        /// Returns true if the current entity state equals the specified expected state
        /// </summary>
        public static bool VerifyEntityState(IDictionary<string, object> expectedState, IDictionary<string, object> currentState)
        {
            foreach (var item in currentState)
            {
                object originalValue = expectedState[item.Key];
                object currentValue = item.Value;
                if (originalValue == null && currentValue == null)
                {
                    continue;
                }
                if ((originalValue == null && currentValue != null) ||
                    (currentValue == null && originalValue != null))
                {
                    return false;
                }
                if (!currentValue.Equals(originalValue))
                {
                    return false;
                }
            }
            return true;
        }
    }

    /// <summary>
    /// An dynamic EntityContainer class that allows external configuration of
    /// EntitySets for testing purposes.
    /// </summary>
    public class DynamicEntityContainer : EntityContainer
    {
        public EntitySet<T> AddEntitySet<T>(EntitySetOperations operations) where T : Entity
        {
            base.CreateEntitySet<T>(operations);
            return GetEntitySet<T>();
        }
    }

    public class TestEntityContainer : EntityContainer
    {
        public TestEntityContainer()
        {
            CreateEntitySet<Product>(EntitySetOperations.Add | EntitySetOperations.Edit | EntitySetOperations.Remove);
            CreateEntitySet<PurchaseOrder>(EntitySetOperations.Add | EntitySetOperations.Edit | EntitySetOperations.Remove);
            CreateEntitySet<PurchaseOrderDetail>(EntitySetOperations.Add | EntitySetOperations.Edit | EntitySetOperations.Remove);
            CreateEntitySet<Employee>(EntitySetOperations.Add | EntitySetOperations.Edit | EntitySetOperations.Remove);
            CreateEntitySet<MixedType>(EntitySetOperations.Add | EntitySetOperations.Edit | EntitySetOperations.Remove);
        }

        public EntitySet<MixedType> MixedTypes
        {
            get
            {
                return GetEntitySet<MixedType>();
            }
        }

        public EntitySet<Product> Products
        {
            get
            {
                return GetEntitySet<Product>();
            }
        }

        public EntitySet<PurchaseOrder> PurchaseOrders
        {
            get
            {
                return GetEntitySet<PurchaseOrder>();
            }
        }

        public EntitySet<PurchaseOrderDetail> PurchaseOrderDetails
        {
            get
            {
                return GetEntitySet<PurchaseOrderDetail>();
            }
        }

        public EntitySet<Employee> Employees
        {
            get
            {
                return GetEntitySet<Employee>();
            }
        }
    }

}
