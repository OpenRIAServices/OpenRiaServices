using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace OpenRiaServices.DomainServices.Client.Data
{
    /// <summary>
    /// List with constant-time add/contains. Remove is O(n). Cannot store values
    /// with duplicate hashes.
    /// </summary>
    internal class IndexedList : IList
    {
        public bool IsReadOnly => false;
        public bool IsFixedSize => false;
        public int Count => _list.Count;
        public object SyncRoot => ((IList) _list).SyncRoot;
        public bool IsSynchronized => ((IList) _list).IsSynchronized;

        private const int NotContainedIndex = -1;

        private readonly List<object> _list = new List<object>();

        /// <summary>
        /// Dictionary of { item, index in _list }
        /// NOTE: this restricts usage to items with unique hashes
        /// </summary>
        private readonly Dictionary<object, int> _dict = new Dictionary<object, int>();

        /// <summary>
        /// Get/set value by index.
        /// </summary>
        public object this[int index]
        {
            get { return _list[index]; }
            set
            {
                _list[index] = value;
                _dict[value] = index;
            }
        }

        /// <summary>
        /// Get an enumerator for items in the list.
        /// </summary>
        public IEnumerator GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        /// <summary>
        /// Copy entire list contents to the given array, starting at the
        /// given index in the array.
        /// </summary>
        /// <param name="array">Array to copy this list to</param>
        /// <param name="index">Index of given array to start copying</param>
        public void CopyTo(Array array, int index)
        {
            ((IList)_list).CopyTo(array, index);
        }

        /// <summary>
        /// Add an item to the list.
        /// </summary>
        /// <exception cref="ArgumentNullException">If the passed item is null</exception>
        /// <exception cref="InvalidOperationException">If the passed item is already in the list</exception>
        public int Add(object value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (_dict.ContainsKey(value))
                throw new InvalidOperationException("Cannot add duplicate values");

            var idx = _list.Count;
            _list.Add(value);
            _dict[value] = idx;
            return idx;
        }

        /// <summary>
        /// True if the list contains the given item.
        /// </summary>
        public bool Contains(object value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            return _dict.ContainsKey(value);
        }

        /// <summary>
        /// Remove all items from the list.
        /// </summary>
        public void Clear()
        {
            _list.Clear();
            _dict.Clear();
        }

        /// <summary>
        /// Get the index of the given item in the list.
        /// </summary>
        /// <returns>List index, or -1 if item is not in the list.</returns>
        /// <exception cref="ArgumentNullException">If the passed item is null</exception>
        public int IndexOf(object value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            int idx;
            if (_dict.TryGetValue(value, out idx))
                return idx;
            return NotContainedIndex;
        }

        public void Insert(int index, object value)
        {
            throw new NotSupportedException(
                string.Format(CultureInfo.CurrentCulture, Resource.IsNotSupported, "Insert"));
        }

        /// <summary>
        /// Remove the given item from the list. Does nothing if the item is
        /// not in the list.
        /// </summary>
        /// <exception cref="ArgumentNullException">If the given item is null</exception>
        public void Remove(object value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (_list.Count == 0) return;

            var idx = IndexOf(value);
            if (idx != NotContainedIndex) RemoveAt(idx);
        }

        /// <summary>
        /// Remove the item at the given index from the list
        /// </summary>
        public void RemoveAt(int index)
        {
            var item = _list[index];
            _list.RemoveAt(index);
            _dict.Remove(item);

            // update list indexes in dictionary for all subsequent items
            for (var i = index; i < _list.Count; i++)
            {
                item = _list[i];
                _dict[item] = i;
            }
        }

        /// <summary>
        /// Get the backing list
        /// </summary>
        public List<object> AsList()
        {
            return _list;
        }
    }
}
