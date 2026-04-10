#if HAS_COLLECTIONVIEW

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Data;

namespace OpenRiaServices.Client
{
    /// <summary>
    /// Fix for WPF leaking memory unless DetachFromSourceCollection is called,
    /// https://www.eidias.com/blog/2014/2/24/wpf-collectionview-can-leak-memory
    /// </summary>
    class NonLeakingListCollectionView : ListCollectionView, IWeakEventListener
    {
        public NonLeakingListCollectionView(IList list)
            : base(list)
        {
            // Replace the strong event handler with a weak event handler to avoid ListCollectionView holding a strong reference to the EntityCollection through the event handler, which would cause a memory leak since ListCollectionView doesn't unsubscribe from the event.
            if (list is INotifyCollectionChanged notifyCollectionChanged)
            {
                notifyCollectionChanged.CollectionChanged -= this.OnCollectionChanged;
                CollectionChangedEventManager.AddListener(notifyCollectionChanged, this);
            }
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            // we need to override this method to avoid ListCollectionView holding a strong reference to the EntityCollection through the event handler, which would cause a memory leak since ListCollectionView doesn't unsubscribe from the event.
            base.OnCollectionChanged(args);
        }

        bool IWeakEventListener.ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
        {
            if (e is NotifyCollectionChangedEventArgs collectionChangedEventArgs)
            {
                this.OnCollectionChanged(sender, collectionChangedEventArgs);
                return true;
            }

            return false;
        }
    }
}
#endif

