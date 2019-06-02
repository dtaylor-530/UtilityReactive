﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Linq;
using System.Text;

namespace UtilityReactive
{
    public static class CollectionChanged
    {

        public static IObservable<NotifyCollectionChangedEventArgs> GetChanges<T>(this ObservableCollection<T> collection)
        {
            return Observable
                   .FromEvent<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                      handler => (sender, args) => handler(args),
                        handler => collection.CollectionChanged += handler,
                        handler => collection.CollectionChanged -= handler);
        }

        public static IObservable<T> GetAdditions<T>(this ObservableCollection<T> collection)
        {
            return GetChanges(collection, NotifyCollectionChangedAction.Add);
        }

        public static IObservable<T> GetSubtractions<T>(this ObservableCollection<T> collection,NotifyCollectionChangedAction action)
        {
            return GetChanges(collection, NotifyCollectionChangedAction.Remove);
        }

        public static IObservable<T> GetChanges<T>(this ObservableCollection<T> collection, NotifyCollectionChangedAction action)
        {
            return GetChanges(collection)
                .Where(_ => _.Action == action)
                .SelectMany(_ => _.NewItems.Cast<T>().ToArray());
        }
    }
}