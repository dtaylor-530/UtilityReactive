using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;

namespace UtilityReactive
{


    public static class BufferHelper
    {
        /// <summary>
        /// https://stackoverflow.com/questions/53152134/buffer-by-time-or-running-sum-for-reactive-extensions/53193641#53193641
        ///  answered Nov 7 '18 at 16:24
        /// Shlomo
        /// Buffer by size or timespan
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="source"></param>
        /// <param name="sizeSelector"></param>
        /// <param name="maxSize"></param>
        /// <param name="bufferTimeSpan"></param>
        /// <returns></returns>
        public static IObservable<IList<TSource>> Buffer<TSource>(this IObservable<TSource> source, Func<TSource, int> sizeSelector, int maxSize, TimeSpan bufferTimeSpan)
        {
            BehaviorSubject<Unit> queue = new BehaviorSubject<Unit>(new Unit()); //our time-out mechanism

            return source
                .Union(queue.Delay(bufferTimeSpan))
                .ScanUnion(
                    (list: ImmutableList<TSource>.Empty, size: 0, emitValue: (ImmutableList<TSource>)null),
                    (state, item) => { // item handler
                    var itemSize = sizeSelector(item);
                        var newSize = state.size + itemSize;
                        if (newSize > maxSize)
                        {
                            queue.OnNext(Unit.Default);
                            return (ImmutableList<TSource>.Empty.Add(item), itemSize, state.list);
                        }
                        else
                            return (state.list.Add(item), newSize, null);
                    },
                    (state, _) => { // time out handler
                    queue.OnNext(Unit.Default);
                        return (ImmutableList<TSource>.Empty, 0, state.list);
                    }
                )
                .Where(t => t.emitValue != null)
                .Select(t => t.emitValue.ToArray());
        }


        /// <summary>
        /// https://stackoverflow.com/questions/7597773/does-reactive-extensions-support-rolling-buffers?noredirect=1
        /// answered Sep 30 '11 at 0:32
        /// Enigmativity
        /// </summary>
        public static IObservable<IEnumerable<T>> BufferWithInactivity<T>(
    this IObservable<T> source,
    TimeSpan inactivity,
    int maximumBufferSize)
        {
            return Observable.Create<IEnumerable<T>>(o =>
            {
                var gate = new object();
                var buffer = new List<T>();
                var mutable = new SerialDisposable();
                var subscription = (IDisposable)null;
                var scheduler = Scheduler.Default;

                Action dump = () =>
                {
                    var bts = buffer.ToArray();
                    buffer = new List<T>();
                    if (o != null)
                    {
                        o.OnNext(bts);
                    }
                };

                Action dispose = () =>
                {
                    if (subscription != null)
                    {
                        subscription.Dispose();
                    }
                    mutable.Dispose();
                };

                Action<Action<IObserver<IEnumerable<T>>>> onErrorOrCompleted =
                    onAction =>
                    {
                        lock (gate)
                        {
                            dispose();
                            dump();
                            if (o != null)
                            {
                                onAction(o);
                            }
                        }
                    };

                Action<Exception> onError = ex =>
                    onErrorOrCompleted(x => x.OnError(ex));

                Action onCompleted = () => onErrorOrCompleted(x => x.OnCompleted());

                Action<T> onNext = t =>
                {
                    lock (gate)
                    {
                        buffer.Add(t);
                        if (buffer.Count == maximumBufferSize)
                        {
                            dump();
                            mutable.Disposable = Disposable.Empty;
                        }
                        else
                        {
                            mutable.Disposable = scheduler.Schedule(inactivity, () =>
                            {
                                lock (gate)
                                {
                                    dump();
                                }
                            });
                        }
                    }
                };

                subscription =
                    source
                        .ObserveOn(scheduler)
                        .Subscribe(onNext, onError, onCompleted);

                return () =>
                {
                    lock (gate)
                    {
                        o = null;
                        dispose();
                    }
                };
            });
        }

     
    }

    public static class DUnionExtensions
    {
        public class DUnion<T1, T2>
        {
            public DUnion(T1 t1)
            {
                Type1Item = t1;
                Type2Item = default(T2);
                IsType1 = true;
            }

            public DUnion(T2 t2, bool ignored) //extra parameter to disambiguate in case T1 == T2
            {
                Type2Item = t2;
                Type1Item = default(T1);
                IsType1 = false;
            }

            public bool IsType1 { get; }
            public bool IsType2 => !IsType1;

            public T1 Type1Item { get; }
            public T2 Type2Item { get; }
        }

        public static IObservable<DUnion<T1, T2>> Union<T1, T2>(this IObservable<T1> a, IObservable<T2> b)
        {
            return a.Select(x => new DUnion<T1, T2>(x))
                .Merge(b.Select(x => new DUnion<T1, T2>(x, false)));
        }

        public static IObservable<TState> ScanUnion<T1, T2, TState>(this IObservable<DUnion<T1, T2>> source,
                TState initialState,
                Func<TState, T1, TState> type1Handler,
                Func<TState, T2, TState> type2Handler)
        {
            return source.Scan(initialState, (state, u) => u.IsType1
                ? type1Handler(state, u.Type1Item)
                : type2Handler(state, u.Type2Item)
            );
        }
    }
}

