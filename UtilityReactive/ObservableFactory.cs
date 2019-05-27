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

        public static IObservable<T> MakeTimedObservable<T>(Func<T> act, TimeSpan ts, int limit, bool skipinitial = false)
        {
            return Observable.Create<T>(observer =>
            {
                if (!skipinitial) observer.OnNext(act());

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
                if (!skipinitial) observer.OnNext(act());

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


        public static IObservable<T> MakeTimedObservable<T>(Func<long, T> act, TimeSpan ts,bool skipinitial=false)
        {
            return Observable.Create<T>(observer =>
            {
                if(!skipinitial)observer.OnNext(act(0));

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



        public static IObservable<T> Build<T>(Func<T> f, TimeSpan ts, int limit = 0)
        {
            if (limit == 0)
                return ObservableFactory
                 .MakeTimedObservable(f, ts);
            else
                return ObservableFactory
                     .MakeTimedObservable(f, ts, limit);

        }





        public static IObservable<T> Build<T>(Func<T> func, TimeSpan ts, IScheduler scheduler)
        {
       
            Func<IObserver<T>, Action> fca = observer => () => observer.OnNext(func());

            return Build(fca, ts, scheduler);

        }



        private static IObservable<T> Build<T>(Func<IObserver<T>, Action> fca, TimeSpan ts, IScheduler scheduler)
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
