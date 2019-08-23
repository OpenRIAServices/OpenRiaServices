﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reflection;
using OpenRiaServices.DomainServices;
using OpenRiaServices.DomainServices.Server;
using System.Web;

namespace OpenRiaServices.DomainServices.Hosting
{
    /// <summary>
    /// Class used to process a changeset.
    /// </summary>
    internal class ChangeSetProcessor
    {
        /// <summary>
        /// Process the specified change set operations and return the results.
        /// </summary>
        /// <param name="domainService">The domain service that will process the changeset.</param>
        /// <param name="changeSetEntries">The change set entries to be processed.</param>
        /// <returns>Collection of results from the submit operation.</returns>
        public static IEnumerable<ChangeSetEntry> Process(DomainService domainService, IEnumerable<ChangeSetEntry> changeSetEntries)
        {
            ChangeSet changeSet = CreateChangeSet(changeSetEntries);
            
            domainService.Submit(changeSet);

            // Process the submit results and build the result list to be sent back
            // to the client
            return GetSubmitResults(changeSet);
        }

        /// <summary>
        /// Examine the list of operations after the service has finished, and determine what needs to
        /// be sent back to the client.
        /// </summary>
        /// <param name="changeSet">The change set processed.</param>
        /// <returns>The results list.</returns>
        private static List<ChangeSetEntry> GetSubmitResults(ChangeSet changeSet)
        {
            List<ChangeSetEntry> results = new List<ChangeSetEntry>();
            foreach (ChangeSetEntry changeSetEntry in changeSet.ChangeSetEntries)
            {
                results.Add(changeSetEntry);

                if (changeSetEntry.HasError)
                {
                    // if customErrors is turned on, clear out the stacktrace.
                    // This is an additional step here so that ValidationResultInfo
                    // and DomainService can remain agnostic to http-concepts
                    HttpContext context = HttpContext.Current;
                    if (context != null && context.IsCustomErrorEnabled)
                    {
                        if (changeSetEntry.ValidationErrors != null)
                        {
                            foreach (ValidationResultInfo error in changeSetEntry.ValidationErrors.Where(e => e.StackTrace != null))
                            {
                                error.StackTrace = null;
                            }
                        }
                    }
                }

                // Don't round-trip data that the client doesn't care about.
                changeSetEntry.Associations = null;
                changeSetEntry.EntityActions = null;
                changeSetEntry.OriginalAssociations = null;
                changeSetEntry.OriginalEntity = null;
            }

            return results;
        }

        /// <summary>
        /// Adds the specified associated entities to the specified association member for the specified entity.
        /// </summary>
        /// <param name="entity">The entity</param>
        /// <param name="associationProperty">The association member (singleton or collection)</param>
        /// <param name="associatedEntities">Collection of associated entities</param>
        private static void SetAssociationMember(object entity, PropertyDescriptor associationProperty, IEnumerable<object> associatedEntities)
        {
            if (associatedEntities.Count() == 0)
            {
                return;
            }

            object associationValue = associationProperty.GetValue(entity);
            if (typeof(IEnumerable).IsAssignableFrom(associationProperty.PropertyType))
            {
                if (associationValue == null)
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.DomainService_AssociationCollectionPropertyIsNull, associationProperty.ComponentType.Name, associationProperty.Name));
                }

                IList list = associationValue as IList;
                IEnumerable<object> associationSequence = null;
                MethodInfo addMethod = null;
                if (list == null)
                {
                    // not an IList, so we have to use reflection
                    Type associatedEntityType = TypeUtility.GetElementType(associationValue.GetType());
                    addMethod = associationValue.GetType().GetMethod("Add", BindingFlags.Public | BindingFlags.Instance, null, new Type[] { associatedEntityType }, null);
                    if (addMethod == null)
                    {
                        throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.DomainService_InvalidCollectionMember, associationProperty.Name));
                    }
                    associationSequence = ((IEnumerable)associationValue).Cast<object>();
                }

                foreach (object associatedEntity in associatedEntities)
                {
                    // add the entity to the collection if it's not already there
                    if (list != null)
                    {
                        if (!list.Contains(associatedEntity))
                        {
                            list.Add(associatedEntity);
                        }
                    }
                    else
                    {
                        if (!associationSequence.Contains(associatedEntity))
                        {
                            addMethod.Invoke(associationValue, new object[] { associatedEntity });
                        }
                    }
                }
            }
            else
            {
                // set the reference if it's not already set
                object associatedEntity = associatedEntities.Single();
                object currentValue = associationProperty.GetValue(entity);
                if (!object.Equals(currentValue, associatedEntity))
                {
                    associationProperty.SetValue(entity, associatedEntity);
                }
            }
        }

        /// <summary>
        /// Create and initialize a ChangeSet from the specified entries.
        /// </summary>
        /// <param name="changeSetEntries">The changeset operations</param>
        /// <returns>The changeset.</returns>
        private static ChangeSet CreateChangeSet(IEnumerable<ChangeSetEntry> changeSetEntries)
        {
            // For deleted entities with original values, we want to replace
            // current and its associations with original.
            foreach (ChangeSetEntry entry in changeSetEntries)
            {
                if (entry.Operation == DomainOperation.Delete && entry.OriginalEntity != null)
                {
                    entry.Entity = entry.OriginalEntity;
                    entry.Associations = entry.OriginalAssociations;
                    entry.OriginalEntity = null;
                    entry.OriginalAssociations = null;
                }
            }

            ChangeSet changeSet = new ChangeSet(changeSetEntries);

            // after the changeset has been validated reestablish
            // entity references
            SetEntityAssociations(changeSetEntries);

            return changeSet;
        }

        /// <summary>
        /// Reestablish associations based on Id lists by adding the referenced entities
        /// to their association members
        /// </summary>
        /// <param name="changeSetEntries">The changeset operations</param>
        private static void SetEntityAssociations(IEnumerable<ChangeSetEntry> changeSetEntries)
        {
            // create a unique map from Id to entity instances, and update operations
            // so Ids map to the same instances, since during deserialization reference
            // identity is not maintained.
            var entityIdMap = changeSetEntries.ToDictionary(p => p.Id, p => new { Entity = p.Entity, OriginalEntity = p.OriginalEntity });
            foreach (ChangeSetEntry changeSetEntry in changeSetEntries)
            {
                object entity = entityIdMap[changeSetEntry.Id].Entity;
                if (changeSetEntry.Entity != entity)
                {
                    changeSetEntry.Entity = entity;
                }

                object original = entityIdMap[changeSetEntry.Id].OriginalEntity;
                if (original != null && changeSetEntry.OriginalEntity != original)
                {
                    changeSetEntry.OriginalEntity = original;
                }
            }

            // for all entities with associations, reestablish the associations by mapping the Ids
            // to entity instances and adding them to the association members
            HashSet<int> visited = new HashSet<int>();
            foreach (var entityGroup in changeSetEntries.Where(p => (p.Associations != null && p.Associations.Count > 0) || (p.OriginalAssociations != null && p.OriginalAssociations.Count > 0)).GroupBy(p => p.Entity.GetType()))
            {
                Dictionary<string, PropertyDescriptor> associationMemberMap = TypeDescriptor.GetProperties(entityGroup.Key).Cast<PropertyDescriptor>().Where(p => p.Attributes[typeof(AssociationAttribute)] != null).ToDictionary(p => p.Name);
                foreach (ChangeSetEntry changeSetEntry in entityGroup)
                {
                    if (visited.Contains(changeSetEntry.Id))
                    {
                        continue;
                    }
                    visited.Add(changeSetEntry.Id);

                    // set current associations
                    if (changeSetEntry.Associations != null)
                    {
                        foreach (var associationItem in changeSetEntry.Associations)
                        {
                            PropertyDescriptor assocMember = associationMemberMap[associationItem.Key];
                            IEnumerable<object> children = associationItem.Value.Select(p => entityIdMap[p].Entity);
                            SetAssociationMember(changeSetEntry.Entity, assocMember, children);
                        }
                    }
                }
            }
        }
    }
}
