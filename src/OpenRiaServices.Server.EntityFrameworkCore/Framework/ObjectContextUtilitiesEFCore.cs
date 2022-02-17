using System;
using System.Linq;
using Microsoft.EntityFrameworkCore.ChangeTracking;

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
            
            // TODO: The code below may be needed based on how RoundTripOriginalAttribute behaves in EF Core

            // For any members that don't have RoundtripOriginal applied, EF can't determine modification
            // state by doing value comparisons. To avoid losing updates in these cases, we must explicitly
            // mark such members as modified.
            //Type entityType = current.GetType();
            //PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(entityType);
            //AttributeCollection attributes = TypeDescriptor.GetAttributes(entityType);
            //bool isRoundtripType = attributes[typeof(RoundtripOriginalAttribute)] != null;
            //foreach (var fieldMetadata in stateEntry.CurrentValues.DataRecordInfo.FieldMetadata)
            //{
            //    string memberName = stateEntry.CurrentValues.GetName(fieldMetadata.Ordinal);
            //    PropertyDescriptor property = properties[memberName];
            //    if (property != null &&
            //        (property.Attributes[typeof(RoundtripOriginalAttribute)] == null && !isRoundtripType) &&
            //        property.Attributes[typeof(ExcludeAttribute)] == null)
            //    {
            //        stateEntry.SetModifiedProperty(memberName);
            //    }
            //}
            return stateEntry;
        }
    }
}
