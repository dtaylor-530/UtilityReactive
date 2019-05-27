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
    public static class ObservableServiceFactory
    {

        //public static IObservable<T> BuildMany<T>(Func<IEnumerable<T>> f, int seconds, int limit = 0)
        //{
        //    return ObservableFactory
        //         .MakeTimedObservable(f, seconds, limit).SelectMany(_ => _);

        //}

        public static IObservable<T> Build<T>(Func<IEnumerable<T>> f)
        {
            return f().ToObservable().Replay(1).RefCount();

        }



        public static IObservable<T> Build<T>(Func<T> f, int seconds, int limit = 0)
        {
            if (limit == 0)
                return ObservableFactory
                 .MakeTimedObservable(f, TimeSpan.FromSeconds(seconds));
            else
                return ObservableFactory
                     .MakeTimedObservable(f, TimeSpan.FromSeconds(seconds), limit);

        }



        //public static IObservable<T>[] Build<T>(IEnumerable<Tuple<Func<T>, int, int>> f)
        //{
        //    return BuildMulti(f).ToArray();

        //}



        //private static IEnumerable<IObservable<T>> BuildMulti<T>(IEnumerable<Tuple<Func<T>, int, int>> f)
        //{
        //    using (var e = f.GetEnumerator())
        //    {
        //        while (e.MoveNext())

        //            yield return ObservableFactory.MakeTimedObservable(e.Current.Item1, e.Current.Item2, e.Current.Item3);

        //    }

        //}

        public static IObservable<T> MakeService<T>(Func<T> func, TimeSpan ts, IScheduler scheduler)
        {
            //if (limit == 0)
            //    return ObservableFactory
            //     .MakeTimedObservable(f, seconds);
            //else
            //    return ObservableFactory
            //         .MakeTimedObservable(f, seconds, limit);


            Func<IObserver<T>, Action> fca = observer => () =>  observer.OnNext(func());

            return ObserverHelper.Build(fca, ts,scheduler);

        }

        //public static IObservable<T> MakeService<S, T>(S complex, Func<S, T> func, TimeSpan ts, IScheduler scheduler)
        //{
        //    Func<IObserver<T>, Action> fca = observer => () =>
        //    {
        //        observer.OnNext(func());

        //    };

        //    return ObserverHelper.Build(fca, ts,scheduler);
        //}


    }



    public class ObserverHelper
    {
        public static IObservable<T> Build<T>(Func<IObserver<T>, Action> fca, TimeSpan ts,IScheduler scheduler)
        {

            return Observable.Create<T>(observer =>
            {
                fca(observer)();
                scheduler = scheduler ?? TaskPoolScheduler.Default;
                return scheduler.ScheduleRecurringAction(ts, fca(observer));
            });
        }



    }

}
