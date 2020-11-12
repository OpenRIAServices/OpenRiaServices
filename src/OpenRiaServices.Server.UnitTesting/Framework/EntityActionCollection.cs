using System.Collections.Generic;
using OpenRiaServices.Serialization;

namespace OpenRiaServices.Server.UnitTesting
{
    /// <summary>
    /// Collection representing number of EntityAction invocations
    /// Usefull helper for setting <see cref="ChangeSetEntry.EntityActions"/>
    /// </summary>
    public class EntityActionCollection : List<KeyValue<string, object[]>>
    {
        /// <summary>
        /// Adds a KeyValue to the specified list.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public void Add(string key, object[] value)
        {
            base.Add(new KeyValue<string, object[]>(key, value));
        }
    }
}
