using System;
using System.Collections;
using System.Collections.Generic;

namespace OpenRiaServices.DomainServices.Client.Data
{
    /// <summary>
    /// List with constant-time add/contains. Remove is O(n). Cannot store values
    /// with duplicate hashes.
    /// </summary>
    internal class IndexedList<T> : IList
    {
        public bool IsReadOnly => false;
        public bool IsFixedSize => false;
        public int Count => _list.Count;
        public object SyncRoot => ((IList) _list).SyncRoot;
        public bool IsSynchronized => ((IList) _list).IsSynchronized;

        private const int NotContainedIndex = -1;

        private readonly List<T> _list = new List<T>();

        /// <summary>
        /// Dictionary of { item, index in _list }
        /// NOTE: this restricts usage to items with unique hashes
        /// </summary>
        private readonly Dictionary<T, int> _dict = new Dictionary<T, int>();

        /// <summary>
        /// Get/set value by index.
        /// </summary>
        public object this[int index]
        {
            get { return _list[index]; }
            set
            {
                _list[index] = (T)value;
                _dict[(T)value] = index;
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

            var tval = (T) value;

            if (_dict.ContainsKey(tval))
                throw new InvalidOperationException("Cannot add duplicate values");

            var idx = _list.Count;
            _list.Add(tval);
            _dict[tval] = idx;
            return idx;
        }

        /// <summary>
        /// True if the list contains the given item.
        /// </summary>
        public bool Contains(object value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            return _dict.ContainsKey((T)value);
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
            if (_dict.TryGetValue((T)value, out idx))
                return idx;
            return NotContainedIndex;
        }

        /// <summary>
        /// Insert the given item into the list at the given index. Note that
        /// this is an O(n) operation.
        /// </summary>
        public void Insert(int index, object value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            _list.Insert(index, (T)value);
            // update indexes of all affected items in list
            // todo: performance could be made constant-time with the 'swap to the end'
            //       strategy used by RemoveAt
            for (var i = index; i < _list.Count; i++)
            {
                var item = _list[i];
                _dict[item] = i;
            }
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
        public List<T> AsList()
        {
            return _list;
        }
    }
}
