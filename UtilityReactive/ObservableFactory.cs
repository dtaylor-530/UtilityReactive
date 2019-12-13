using DynamicData.Kernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UtilityReactive
{

    public static class ObservableFactory
    {
        public static IObservable<T> ToObservable<T>(this IEnumerable<T> enumerable, TimeSpan ts, IScheduler scheduler = null)
        {
            var x = enumerable.GetEnumerator();

            Func<IObserver<T>, Action> fca = observer => () =>
            {
                if (x.MoveNext())
                    observer.OnNext(x.Current);
                else
                    observer.OnCompleted();
            };

            return Build(fca, ts, scheduler, x);
        }

        public static IObservable<T> ToObservable<T>(this IEnumerable<T> enumerable, TimeSpan minTimeSpan, TimeSpan maxTimeSpan, IScheduler scheduler = null, Random random = null)
        {
            random = random ?? new Random();

            using (var x = enumerable.GetEnumerator())
            {
                Func<IObserver<T>, Action> fca = observer => () => { if (x.MoveNext()) observer.OnNext(x.Current); else observer.OnCompleted(); };

                return Build(fca, TimeSpan.FromMilliseconds(random.Next(minTimeSpan.Milliseconds, maxTimeSpan.Milliseconds)), scheduler);
            }
        }

        public static IObservable<T> MakeTimedObservable<T>(Func<T> act, Func<TimeSpan> fts, int limit, bool skipinitial = false)
        {
            return Observable.Create<T>(observer =>
            {
                if (!skipinitial) observer.OnNext(act());

                return Observable.Interval(fts())
                   .Take(limit)
                .Subscribe(a => observer.OnNext(act()), ex =>
                {
                    try { observer.OnError(ex); }
                    catch
                    {
                        Console.WriteLine(ex.Message);
                    }

                }, () => Console.WriteLine("Observer has unsubscribed from timed observable"));
            });

        }

        public static IObservable<T> MakeTimedObservable<T>(Func<T> act, Func<TimeSpan> fts, bool skipinitial = false)
        {
            return Observable.Create<T>(observer =>
            {
                if (!skipinitial) observer.OnNext(act());

                return Observable.Interval(fts())
                .Subscribe(a => observer.OnNext(act()), ex =>
                {
                    try { observer.OnError(ex); }
                    catch
                    {
                        Console.WriteLine(ex.Message);
                    }

                }, () => Console.WriteLine("Observer has unsubscribed from timed observable"));
            });

        }

        public static IObservable<T> MakeTimedObservable<T>(Func<T> act, TimeSpan ts, int limit, bool skipinitial = false)
        {
            return Observable.Create<T>(observer =>
            {
                if (!skipinitial)
                    observer.OnNext(act());

                return Observable.Interval(ts)
                   .Take(limit)
                .Subscribe(a => observer.OnNext(act()), ex =>
                {
                    try { observer.OnError(ex); }
                    catch
                    {
                        Console.WriteLine(ex.Message);
                    }

                }, () => Console.WriteLine("Observer has unsubscribed from timed observable"));
            });

        }


        public static IObservable<T> MakeTimedObservable<T>(Func<T> act, TimeSpan ts, bool skipinitial = false)
        {
            return Observable.Create<T>(observer =>
            {
                if (!skipinitial)
                    observer.OnNext(act());

                return Observable.Interval(ts)
                .Subscribe(a => observer.OnNext(act()), ex =>
                {
                    try { observer.OnError(ex); }
                    catch
                    {
                        Console.WriteLine(ex.Message);
                    }

                }, () => Console.WriteLine("Observer has unsubscribed from timed observable"));
            });

        }


        public static IObservable<T> MakeTimedObservable<T>(Func<long, T> act, TimeSpan ts, bool skipinitial = false)
        {
            return Observable.Create<T>(observer =>
            {
                if (!skipinitial)
                    observer.OnNext(act(0));

                return Observable.Interval(ts)
                .Subscribe(a => observer.OnNext(act(a)), ex =>
                {
                    try { observer.OnError(ex); }
                    catch
                    {
                        Console.WriteLine(ex.Message);
                    }

                }, () => Console.WriteLine("Observer has unsubscribed from timed observable"));
            });

        }



        //public static IObservable<T> BuildMany<T>(Func<IEnumerable<T>> f, int seconds, int limit = 0)
        //{
        //    return ObservableFactory
        //         .MakeTimedObservable(f, seconds, limit).SelectMany(_ => _);

        //}

        //public static IObservable<T> Build<T>(Func<IEnumerable<T>> f)
        //{
        //    return f().ToObservable();

        //}




        public static IObservable<T> Build<T>(Func<T> f, Func<TimeSpan> fts, int limit = 0)
        {
            if (limit == 0)
                return ObservableFactory
                 .MakeTimedObservable(f, fts);
            else
                return ObservableFactory
                     .MakeTimedObservable(f, fts, limit);

        }



        public static IObservable<T> Build<T>(Func<T> func, TimeSpan ts, IScheduler scheduler = null)
        {

            Func<IObserver<T>, Action> fca = observer => () => observer.OnNext(func());

            return Build(fca, ts, scheduler);

        }



        private static IObservable<T> Build<T>(Func<IObserver<T>, Action> fca, TimeSpan ts, IScheduler scheduler = null, params IDisposable[] disposable)
        {

            return Observable.Create<T>(observer =>
            {

                fca(observer)();
                scheduler = scheduler ?? TaskPoolScheduler.Default;
                return new System.Reactive.Disposables.CompositeDisposable(disposable.Concat(new[] { scheduler.ScheduleRecurringAction(ts, fca(observer)) }));

            });
        }


    }


}
