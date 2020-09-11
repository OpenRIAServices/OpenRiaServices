using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections.Specialized;

namespace OpenRiaServices.Client.Data
{
    /// <summary>
    /// Class used for representing resettable collections used to optimize <see cref="LoadOperation"/> collections.
    /// It is similar to <see cref="ReadOnlyObservableCollection{T}"/> but allows changing the contents when a load has happened.
    /// </summary>
    /// <typeparam name="T">type of entity</typeparam>
    internal class ReadOnlyObservableLoaderCollection<T> : ReadOnlyCollection<T>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        public ReadOnlyObservableLoaderCollection()
            : base(new List<T>())
        {

        }

        public ReadOnlyObservableLoaderCollection(IEnumerable<T> list)
            : base(new List<T>(list))
        {

        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        public void Reset(IEnumerable<T> collection)
        {
            var list = (List<T>)base.Items;

            list.Clear();
            list.AddRange(collection);

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}
