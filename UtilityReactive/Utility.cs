using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive;

namespace UtilityReactive
{

    public static class Utility
    {
        //public static IScheduler MakeUIScheduler(this System.Windows.Threading.Dispatcher dispatcher)
        //{

        //    return new System.Reactive.Concurrency.DispatcherScheduler(dispatcher);

        //}

        public static IObservable<IEnumerable<T>> Limit<T>(this IObservable<IEnumerable<T>> ss, IObservable<int> bs)
        {
            return ss.CombineLatest(bs, (a, b) => a.Take(b));

        }


        public static IObservable<R> SelectNotNull<T,R>(this IObservable<T> obs,Func<T,R> f)
        {
            return obs.Select(_ => f(_)).Where(__ => __ != null);

        }

        public static IObservable<T> TakeWhile<T>(this IObservable<T> ss, IObservable<bool> bs, bool bl = true)
        {
            return ss.CombineLatest(bs, (a, b) => new { a, b })
                .Where(c => c.b == bl)
                .Select(_ => _.a);

        }


        //How to Merge two Observables so the result completes when the any of the Observables completes?
        //Ray Booysen
        public static IObservable<T> MergeWithCompleteOnEither<T>(this IObservable<T> source, IObservable<T> right)
            {
                return Observable.Create<T>(obs =>
                {
                    var compositeDisposable = new CompositeDisposable();
                    var subject = new System.Reactive.Subjects.Subject<T>();

                    compositeDisposable.Add(subject.Subscribe(obs));
                    compositeDisposable.Add(source.Subscribe(subject));
                    compositeDisposable.Add(right.Subscribe(subject));


                    return compositeDisposable;

                });
            }



        //Takes source until right stops emitting values
        public static IObservable<T> TakeUntilEnd<T,R>(this IObservable<T> source, IObservable<R> right)
        {
            return source.TakeUntil(right.GetAwaiter());
        }





        public static IObservable<T> BufferUntilInactive<T>(this IObservable<T> stream, TimeSpan delay)
        {
            var closes = stream.Throttle(delay);
            return stream.Window(() => closes)
                .SelectMany(window => window.ToList())
                .Where(a => a != null & a.Count > 0)
                .SelectMany(_ => _)
                .Where(df => df != null);

        }

        public static IObservable<T> BufferUntilInactive<T>(this IObservable<T> t)
        {
            return t.BufferUntilInactive(TimeSpan.FromMilliseconds(300));

        }


        public static IObservable<T> WaitFor<T>(this IObservable<T> source, Func<T, bool> pred)
        {
            return
                source
                    .Where(pred)
                    .DistinctUntilChanged()
                    .Take(1);

        }

        public static IObservable<IObservable<KeyValuePair<int, T>>> RangeToRandomObservable<T>(this IObservable<Savage.Range.Range<int>> output, Func<int, T> func, int size=10) 
        {
            Random r = new Random();
            var obs = Observable.Create<IObservable<KeyValuePair<int, T>>>(observer =>
              output.Where(a => !a.Equals(default(Savage.Range.Range<int>))).Subscribe(_ =>
              {
                  var diff = _.Ceiling - _.Floor;

                  observer.OnNext(Observable.Generate(_.Floor,
                   i => i < _.Ceiling, i => i + 1,
                   i =>
                   {
                       return new KeyValuePair<int, T>(i, func(i));
                   },
                   i => TimeSpan.FromSeconds(r.Next(0, size))));

              }));

            return obs;

        }


    }



    public static partial class ObservableEx
    {
        // Thursday, August 28, 2014 9:59 AM  Dave Sexton
        //https://social.msdn.microsoft.com/Forums/en-US/cb1f83b0-5fc5-47b3-ad28-465ba4a5d140/how-to-combine-n-observables-dynamically-with-combinelatest-semantics?forum=rx
        // Merges multiple observable sequences
        public static IObservable<IList<TSource>> CombineLatest<TSource>(this IEnumerable<IObservable<TSource>> sources)
        {
            return Observable.Create<IList<TSource>>(
                observer =>
                {
                    object gate = new object();
                    var disposables = new CompositeDisposable();
                    var list = new List<TSource>();
                    var hasValueFlags = new List<bool>();
                    var actionSubscriptions = 0;
                    bool hasSources, hasValueFromEach = false;

                    using (var e = sources.GetEnumerator())
                    {
                        bool subscribing = hasSources = e.MoveNext();

                        while (subscribing)
                        {
                            var source = e.Current;
                            int index;

                            lock (gate)
                            {
                                actionSubscriptions++;

                                list.Add(default(TSource));
                                hasValueFlags.Add(false);

                                index = list.Count - 1;

                                subscribing = e.MoveNext();
                            }

                            disposables.Add(
                                source.Subscribe(
                                    value =>
                                    {
                                        IList<TSource> snapshot;

                                        lock (gate)
                                        {
                                            list[index] = value;

                                            if (!hasValueFromEach)
                                            {
                                                hasValueFlags[index] = true;

                                                if (!subscribing)
                                                {
                                                    hasValueFromEach = hasValueFlags.All(b => b);
                                                }
                                            }

                                            if (subscribing || !hasValueFromEach)
                                            {
                                                snapshot = null;
                                            }
                                            else
                                            {
                                                snapshot = list.ToList().AsReadOnly();
                                            }
                                        }

                                        if (snapshot != null)
                                        {
                                            observer.OnNext(snapshot);
                                        }
                                    },
                                    observer.OnError,
                                    () =>
                                    {
                                        bool completeNow;

                                        lock (gate)
                                        {
                                            actionSubscriptions--;

                                            completeNow = actionSubscriptions == 0 && !subscribing;
                                        }

                                        if (completeNow)
                                        {
                                            observer.OnCompleted();
                                        }
                                    }));
                        }
                    }

                    if (!hasSources)
                    {
                        observer.OnCompleted();
                    }

                    return disposables;
                });
        }


        //public static IObservable<IEnumerable<T>> CombineGroups<T>(this IObservable<IEnumerable<T>> elements, int size)
        //{


        //    return elements.Select(a =>
        //    {

        //        // var enm = Enumerable.Range(0, a.Count());
        //        //var x= Combinatorics.Combine(enm, size,GenerateOptions.WithoutRepetition)
        //        //          // Combinatorics creates new object so have to match with original object which is easier by simply giving it numbers
        //        //          .Select(_ => _.Select(__ => Enumerable.ElementAt(a, __)));
        //        return a.CombineDistinct(size);


        //        // return x;
        //    }
        //    ).SelectMany(_ => _);

        //}








    }




    public static partial class ObservableEx2
    {
        public static IObservable<TResult> Zip<TSource, TResult>(
            this IEnumerable<IObservable<TSource>> sources,
            Func<IList<TSource>, TResult> selector)
        {
            return Observable.Defer(() =>
            {
                IObservable<List<TSource>> intermediate = null;

                foreach (var source in sources)
                {
                    if (intermediate == null)
                        intermediate = source.Select(value => new List<TSource>() { value });
                    else
                        intermediate = intermediate.Zip(source,
                            (values, next) =>
                            {
                                values.Add(next);
                                return values;
                            });
                }

                return intermediate.Select(values => selector(values.AsReadOnly()));
            });
        }
    }
}

    //public static IEnumerable<List<T>> ToRuns<T, TKey>(
    //        this IEnumerable<T> source,
    //        Func<T, TKey> keySelector)
    //{
    //    using (var enumerator = source.GetEnumerator())
    //    {
    //        if (!enumerator.MoveNext())
    //            yield break;

    //        var currentSet = new List<T>();

    //        // inspect the first item
    //        var lastKey = keySelector(enumerator.Current);
    //        currentSet.Add(enumerator.Current);

    //        while (enumerator.MoveNext())
    //        {
    //            var newKey = keySelector(enumerator.Current);
    //            if (!Equals(newKey, lastKey))
    //            {
    //                // A difference == new run; return what we've got thus far
    //                yield return currentSet;
    //                lastKey = newKey;
    //                currentSet = new List<T>();
    //            }
    //            currentSet.Add(enumerator.Current);
    //        }

    //        // Return the last run.
    //        yield return currentSet;

    //        // and clean up
    //        currentSet = new List<T>();
    //        lastKey = default(TKey);
    //    }
    //}




