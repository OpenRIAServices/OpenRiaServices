﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace OpenRiaServices.DomainControllers.Server
{
    [DomainControllerConfiguration]
    public abstract class DomainController : ApiController
    {
        private ChangeSet _changeSet;
        private DomainControllerDescription _description;

        /// <summary>
        /// Gets the current <see cref="ChangeSet"/>. Returns null if no change operations are being performed.
        /// </summary>
        protected ChangeSet ChangeSet
        {
            get { return _changeSet; }
        }

        /// <summary>
        /// Gets the <see cref="DomainControllerDescription"/> for this <see cref="DomainController"/>.
        /// </summary>
        protected DomainControllerDescription Description
        {
            get { return _description; }
        }

        /// <summary>
        /// Gets the <see cref="HttpActionContext"/> for the currently executing action.
        /// </summary>
       // protected internal HttpActionContext ActionContext { get; internal set; }

        protected override void Initialize(HttpControllerContext controllerContext)
        {
            // ensure that the service is valid and all custom metadata providers
            // have been registered
            _description = DomainControllerDescription.GetDescription(controllerContext.ControllerDescriptor);

            base.Initialize(controllerContext);
        }

        /// <summary>
        /// Performs the operations indicated by the specified <see cref="ChangeSet"/> by invoking
        /// the corresponding actions for each.
        /// </summary>
        /// <param name="changeSet">The changeset to submit</param>
        /// <returns>True if the submit was successful, false otherwise.</returns>
        public virtual bool Submit(ChangeSet changeSet)
        {
            if (changeSet == null)
            {
                throw Error.ArgumentNull("changeSet");
            }
            _changeSet = changeSet;

            ResolveActions(_description, ChangeSet.ChangeSetEntries);

            if (!AuthorizeChangeSet())
            {
                // Don't try to save if there were any errors.
                return false;
            }

            // Before invoking any operations, validate the entire changeset
            if (!ValidateChangeSet())
            {
                return false;
            }

            // Now that we're validated, proceed to invoke the actions.
            if (!ExecuteChangeSet())
            {
                return false;
            }

            // persist the changes
            if (!PersistChangeSetInternal())
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// For all operations in the current changeset, validate that the operation exists, and
        /// set the operation entry.
        /// </summary>
        internal static void ResolveActions(DomainControllerDescription description, IEnumerable<ChangeSetEntry> changeSet)
        {
            // Resolve and set the action for each operation in the changeset
            foreach (ChangeSetEntry changeSetEntry in changeSet)
            {
                Type entityType = changeSetEntry.Entity.GetType();
                UpdateActionDescriptor actionDescriptor = null;
                if (changeSetEntry.Operation == ChangeOperation.Insert ||
                    changeSetEntry.Operation == ChangeOperation.Update ||
                    changeSetEntry.Operation == ChangeOperation.Delete)
                {
                    actionDescriptor = description.GetUpdateAction(entityType, changeSetEntry.Operation);
                }

                // if a custom method invocation is specified, validate that the method exists
                bool isCustomUpdate = false;
                if (changeSetEntry.EntityActions != null && changeSetEntry.EntityActions.Any())
                {
                    var entityAction = changeSetEntry.EntityActions.Single();
                    UpdateActionDescriptor customMethodOperation = description.GetCustomMethod(entityType, entityAction.Key);
                    if (customMethodOperation == null)
                    {
                        throw Error.InvalidOperation(Resource.DomainController_InvalidAction, entityAction.Key, entityType.Name);
                    }

                    // if the primary action for an update is null but the entry
                    // contains a valid custom update action, its considered a "custom update"
                    isCustomUpdate = actionDescriptor == null && customMethodOperation != null;
                }

                if (actionDescriptor == null && !isCustomUpdate)
                {
                    throw Error.InvalidOperation(Resource.DomainController_InvalidAction, changeSetEntry.Operation.ToString(), entityType.Name);
                }

                changeSetEntry.ActionDescriptor = actionDescriptor;
            }
        }

        /// <summary>
        /// Verifies the user is authorized to submit the current <see cref="ChangeSet"/>.
        /// </summary>
        /// <returns>True if the <see cref="ChangeSet"/> is authorized, false otherwise.</returns>
        protected virtual bool AuthorizeChangeSet()
        {
            foreach (ChangeSetEntry changeSetEntry in ChangeSet.ChangeSetEntries)
            {
                if (!changeSetEntry.ActionDescriptor.Authorize(ActionContext))
                {
                    return false;
                }

                // if there are any custom method invocations for this operation
                // we need to authorize them as well
                if (changeSetEntry.EntityActions != null && changeSetEntry.EntityActions.Any())
                {
                    Type entityType = changeSetEntry.Entity.GetType();
                    foreach (var entityAction in changeSetEntry.EntityActions)
                    {
                        UpdateActionDescriptor customAction = Description.GetCustomMethod(entityType, entityAction.Key);
                        if (!customAction.Authorize(ActionContext))
                        {
                            return false;
                        }
                    }
                }
            }

            return !ChangeSet.HasError;
        }

        /// <summary>
        /// Validates the current <see cref="ChangeSet"/>. Any errors should be set on the individual <see cref="ChangeSetEntry"/>s
        /// in the <see cref="ChangeSet"/>.
        /// </summary>
        /// <returns><c>True</c> if all operations in the <see cref="ChangeSet"/> passed validation, <c>false</c> otherwise.</returns>
        protected virtual bool ValidateChangeSet()
        {
            return ChangeSet.Validate(ActionContext);
        }

        /// <summary>
        /// This method invokes the action for each operation in the current <see cref="ChangeSet"/>.
        /// </summary>
        /// <returns>True if the <see cref="ChangeSet"/> was processed successfully, false otherwise.</returns>
        protected virtual bool ExecuteChangeSet()
        {
            InvokeCUDOperations();
            InvokeCustomUpdateOperations();

            return !ChangeSet.HasError;
        }

        private void InvokeCUDOperations()
        {
            foreach (ChangeSetEntry changeSetEntry in ChangeSet.ChangeSetEntries
                .Where(op => op.Operation == ChangeOperation.Insert ||
                             op.Operation == ChangeOperation.Update ||
                             op.Operation == ChangeOperation.Delete))
            {
                if (changeSetEntry.ActionDescriptor == null)
                {
                    continue;
                }

                InvokeAction(changeSetEntry.ActionDescriptor, new object[] { changeSetEntry.Entity }, changeSetEntry);
            }
        }

        private void InvokeCustomUpdateOperations()
        {
            foreach (ChangeSetEntry changeSetEntry in ChangeSet.ChangeSetEntries.Where(op => op.EntityActions != null && op.EntityActions.Any()))
            {
                Type entityType = changeSetEntry.Entity.GetType();
                foreach (var entityAction in changeSetEntry.EntityActions)
                {
                    UpdateActionDescriptor customUpdateAction = Description.GetCustomMethod(entityType, entityAction.Key);

                    List<object> customMethodParams = new List<object>(entityAction.Value);
                    customMethodParams.Insert(0, changeSetEntry.Entity);

                    InvokeAction(customUpdateAction, customMethodParams.ToArray(), changeSetEntry);
                }
            }
        }

        private void InvokeAction(HttpActionDescriptor action, object[] parameters, ChangeSetEntry changeSetEntry)
        {
            try
            {
                Collection<HttpParameterDescriptor> pds = action.GetParameters();
                Dictionary<string, object> paramMap = new Dictionary<string, object>(pds.Count);
                for (int i = 0; i < pds.Count; i++)
                {
                    paramMap.Add(pds[i].ParameterName, parameters[i]);
                }

                // TODO - Issue #103
                // This method is not correctly observing the execution results, the catch block below is wrong.
                // Submit should be Task<bool>, not bool, and should model bind for the CancellationToken which would then
                // be propagated through to all the helper methods (one or more of which might also need to be made async,
                // once we start respecting the fact that the read/write actions should be allowed to be async).
                action.ExecuteAsync(ActionContext.ControllerContext, paramMap, CancellationToken.None);
            }
            catch (TargetInvocationException tie)
            {
                ValidationException vex = tie.GetBaseException() as ValidationException;
                if (vex != null)
                {
                    ValidationResultInfo error = new ValidationResultInfo(vex.Message, 0, String.Empty, vex.ValidationResult.MemberNames);
                    if (changeSetEntry.ValidationErrors != null)
                    {
                        changeSetEntry.ValidationErrors = changeSetEntry.ValidationErrors.Concat(new ValidationResultInfo[] { error }).ToArray();
                    }
                    else
                    {
                        changeSetEntry.ValidationErrors = new ValidationResultInfo[] { error };
                    }
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// This method is called to finalize changes after all the operations in the current <see cref="ChangeSet"/>
        /// have been invoked. This method should commit the changes as necessary to the data store.
        /// Any errors should be set on the individual <see cref="ChangeSetEntry"/>s in the <see cref="ChangeSet"/>.
        /// </summary>
        /// <returns>True if the <see cref="ChangeSet"/> was persisted successfully, false otherwise.</returns>
        protected virtual bool PersistChangeSet()
        {
            return true;
        }

        /// <summary>
        /// This method invokes the user overridable <see cref="PersistChangeSet"/> method wrapping the call
        /// with the appropriate exception handling logic. All framework calls to <see cref="PersistChangeSet"/>
        /// must go through this method. Some data sources have their own validation hook points,
        /// so if a <see cref="ValidationException"/> is thrown at that level, we want to capture it.
        /// </summary>
        /// <returns>True if the <see cref="ChangeSet"/> was persisted successfully, false otherwise.</returns>
        private bool PersistChangeSetInternal()
        {
            try
            {
                PersistChangeSet();
            }
            catch (ValidationException e)
            {
                // if a validation exception is thrown for one of the entities in the changeset
                // set the error on the corresponding ChangeSetEntry
                if (e.Value != null && e.ValidationResult != null)
                {
                    IEnumerable<ChangeSetEntry> updateOperations =
                        ChangeSet.ChangeSetEntries.Where(
                            p => p.Operation == ChangeOperation.Insert ||
                                 p.Operation == ChangeOperation.Update ||
                                 p.Operation == ChangeOperation.Delete);

                    ChangeSetEntry operation = updateOperations.SingleOrDefault(p => Object.ReferenceEquals(p.Entity, e.Value));
                    if (operation != null)
                    {
                        ValidationResultInfo error = new ValidationResultInfo(e.ValidationResult.ErrorMessage, e.ValidationResult.MemberNames);
                        error.StackTrace = e.StackTrace;
                        operation.ValidationErrors = new List<ValidationResultInfo>() { error };
                    }
                }
                else
                {
                    throw;
                }
            }

            return !ChangeSet.HasError;
        }
    }
}
