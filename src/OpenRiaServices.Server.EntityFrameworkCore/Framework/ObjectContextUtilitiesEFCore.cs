using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using OpenRiaServices.Server;

namespace OpenRiaServices.EntityFrameworkCore
{

    /// <summary>
    /// Internal utility functions for dealing with EF types and metadata
    /// </summary>
    internal static class ObjectContextUtilitiesEFCore
    {
        public static EntityEntry AttachAsModifiedInternal(object current, object original, ChangeTracker objectContext)
        {
            var stateEntry = objectContext.Context.Entry(current); // ObjectStateManager.GetObjectStateEntry(current);
            // Apply original vaules
            var originalValues = objectContext.Context.Entry(original).CurrentValues;

            // For any members that don't have RoundtripOriginal applied, EF can't determine modification
            // state by doing value comparisons. To avoid losing updates in these cases, we must explicitly
            // mark such members as modified.
            Type entityType = current.GetType();
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(entityType);
            AttributeCollection attributes = TypeDescriptor.GetAttributes(entityType);
            bool isRoundtripType = attributes[typeof(RoundtripOriginalAttribute)] != null;

            foreach (var memberName in stateEntry.CurrentValues.Properties)
            {
                stateEntry.OriginalValues[memberName.Name] = originalValues[memberName];

                PropertyDescriptor property = properties[memberName.Name];
                if (property != null &&
                    (property.Attributes[typeof(RoundtripOriginalAttribute)] == null && !isRoundtripType) &&
                    property.Attributes[typeof(ExcludeAttribute)] == null)
                {
                    stateEntry.Property(memberName.Name).IsModified = true;
                }
            }
            return stateEntry;
        }
    }
}
