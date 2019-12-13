using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Linq;
using System.Text;

namespace UtilityReactive
{
    public static class CollectionChangedHelper
    {


        public static IObservable<T> ToNewItemsObservable<T>(this INotifyCollectionChanged notifyCollectionChanged)
        {
            return notifyCollectionChanged
              .GetChanges()
              .SelectMany(x => x.NewItems?.Cast<T>() ?? new T[] { });

        }

        public static IObservable<T> ToOldItemsObservable<T>(this INotifyCollectionChanged notifyCollectionChanged)
        {
            return notifyCollectionChanged
              .GetChanges()
              .SelectMany(x => x.OldItems?.Cast<T>() ?? new T[] { });
        }

        public static IObservable<NotifyCollectionChangedAction> ToActionsObservable<T>(this INotifyCollectionChanged notifyCollectionChanged)
        {
            return notifyCollectionChanged
              .GetChanges()
              .Select(x => x.Action);
        }

        public static IObservable<NotifyCollectionChangedEventArgs> GetChanges(this INotifyCollectionChanged collection)
        {
            return Observable
                   .FromEvent<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                      handler => (sender, args) => handler(args),
                        handler => collection.CollectionChanged += handler,
                        handler => collection.CollectionChanged -= handler);
        }


    }
}