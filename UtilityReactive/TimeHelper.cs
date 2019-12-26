using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;


namespace UtilityReactive
{
    public static class TimeHelper
    {




        // James World
        //http://www.zerobugbuild.com/?p=323
        ///The events should be output at a maximum rate specified by a TimeSpan, but otherwise as soon as possible.
        public static IObservable<T> Pace<T>(this IObservable<T> source, TimeSpan rate)
        {
            var paced = source.Select(i => Observable.Empty<T>()

                                      .Delay(rate)
                                      .StartWith(i)).Concat();

            return paced;
        }



        // do first set of work immediately, and then every 5 seconds do it again
        //    m_interval = Observable
        //        .FromAsync(DoWork)
        //.RepeatAfterDelay(TimeSpan.FromSeconds(5), scheduler)
        //.Subscribe();

        //    // wait 5 seconds, then do first set of work, then again every 5 seconds
        //    m_interval = Observable
        //        .Timer(TimeSpan.FromSeconds(5), scheduler)
        //.SelectMany(_ => Observable
        //    .FromAsync(DoWork)
        //    .RepeatAfterDelay(TimeSpan.FromSeconds(5), scheduler))
        //.Subscribe();

        public static IObservable<T> RepeatAfterDelay<T>(this IObservable<T> source, TimeSpan delay, IScheduler scheduler)
        {
            var repeatSignal = Observable
                .Empty<T>()
                .Delay(delay, scheduler);

            // when source finishes, wait for the specified
            // delay, then repeat.
            return source.Concat(repeatSignal).Repeat();
        }



        public static IObservable<long> ToCountDowns<T>(this IObservable<T> obs, int seconds, int interval = 1, int delay = 0)
        {

            return obs
            .Select(a =>
            Observable.Timer(TimeSpan.FromSeconds(delay), TimeSpan.FromSeconds(interval)))
            .Switch()
            .Select(lp => seconds - interval * lp);

        }


        public static IObservable<KeyValuePair<DateTime, T>> ByTimeStamp<T>(this IEnumerable<KeyValuePair<DateTime, T>> scheduledTimes, TimeSpan offset = default(TimeSpan))
        {
            return scheduledTimes.ByTimeStamp(kvp => kvp.Key, offset);

        }

        public static IObservable<DateTime> ByTimeStamp(this IEnumerable<DateTime> scheduledTimes, TimeSpan offset = default(TimeSpan))
        {
            return scheduledTimes.ByTimeStamp(k => k, offset);
        }

        public static IObservable<T> ByTimeStamp<T>(this IEnumerable<T> scheduledTimes, Func<T, DateTime> func, TimeSpan offset = default(TimeSpan))
        {
            return Observable.Generate(
                scheduledTimes.GetEnumerator(),
                e => e.MoveNext(),
                e => e,
  e => e.Current,
  e => func(e.Current) + offset);

        }

        /// <summary>
        /// Speeds up slows down <see cref="scheduledTimes"/>.
        /// </summary>
        /// <param name="scheduledTimes"></param>
        /// <param name="rate"></param>
        /// <returns></returns>
        public static IEnumerable<DateTime> ChangeRate(this IEnumerable<DateTime> scheduledTimes, double rate)
        {
            using (var enmr = scheduledTimes.GetEnumerator())
            {
                enmr.MoveNext();
                DateTime last = enmr.Current;
                DateTime last2 = enmr.Current;
                while (enmr.MoveNext())
                {
                    last = last.Add(new TimeSpan((long)((enmr.Current - last2).Ticks / rate)));
                    last2 = enmr.Current;
                    yield return last;
                }
            }
        }
        /// <summary>
        /// Speeds up slows down <see cref="scheduledTimes"/>.
        /// </summary>
        /// <param name="sequence"></param>
        /// <param name="rate"></param>
        /// <returns></returns>
        public static IEnumerable<double> ChangeRate(this IEnumerable<double> scheduledTimes, double rate)
        {
            using (var enmr = scheduledTimes.GetEnumerator())
            {
                enmr.MoveNext();
                double last = enmr.Current;
                yield return last;
                double last2 = enmr.Current;
                while (enmr.MoveNext())
                {
                    last = last + ((enmr.Current - last2) / rate);
                    last2 = enmr.Current;
                    yield return last;
                }
            }
        }

        /// <summary>
        ///  Adjusts all dates so that the the first date starts at <see cref="startDateTime"></see> 
        ///  whilst the difference between dates  is preserved.
        /// </summary>
        /// <param name="scheduledTimes"></param>
        /// <param name="startDateTime"></param>
        /// <returns></returns>
        public static IEnumerable<DateTime> AdjustAllByFirstTo(this IEnumerable<DateTime> scheduledTimes, DateTime startDateTime)
        {
            yield return startDateTime;

            using (var enmr = scheduledTimes.GetEnumerator())
            {
                enmr.MoveNext();
                TimeSpan diff = enmr.Current - startDateTime;
                while (enmr.MoveNext())
                {
                    yield return enmr.Current - diff;
                }
            }
        }


        /// <summary>
        ///  Adjusts all dates so that the the first date starts at <see cref="startDateTime"></see> 
        ///  whilst the difference between dates  is preserved.
        /// </summary>
        /// <param name="scheduledTimes"></param>
        /// <param name="startDateTime"></param>
        /// <returns></returns>
        public static IEnumerable<KeyValuePair<DateTime, DateTime>> SelectCountDowns(this IEnumerable<DateTime> scheduledTimes, TimeSpan timeSpan)
        {

            using (var enmr = scheduledTimes.GetEnumerator())
            {
                enmr.MoveNext();
                var current = enmr.Current;

                while (enmr.MoveNext())
                {
                    var diff = enmr.Current - current;

                    double totalSize = diff.Ticks;

                    int i = 0;
                    while (totalSize > 0)
                    {
                        yield return new KeyValuePair<DateTime, DateTime>(enmr.Current - TimeSpan.FromTicks((long)totalSize), enmr.Current);
                        totalSize -= timeSpan.Ticks;
                    }
                }
            }
        }

        /// <summary>
        ///  Adjusts all dates so that the the first date starts at <see cref="startDateTime"></see> 
        ///  whilst the difference between dates  is preserved.
        /// </summary>
        /// <param name="scheduledTimes"></param>
        /// <param name="startDateTime"></param>
        /// <returns></returns>
        public static IObservable<KeyValuePair<TimeSpan, DateTime>> SelectCountDowns(this IObservable<DateTime> scheduledTimes, TimeSpan timeSpan)
        {

            return scheduledTimes.Select(a =>
             {
                 var dtn = DateTime.Now;
                 int sum = (int)((dtn - a).Ticks / timeSpan.Ticks);
                 var obs = Observable
                 .Interval(timeSpan)
                 .Take(sum)
                 .Select(itvl =>
                 {
                     return new KeyValuePair<TimeSpan, DateTime>(DateTime.Now - a, a);
                 });

                 return sum > 0 ? obs : obs.Prepend(new KeyValuePair<TimeSpan, DateTime>(DateTime.Now - a, a));

             }).Switch();
        }

        /// <summary>
        ///  Adjusts all numbers so that the the first number starts at <see cref="start"></see> 
        ///  whilst the difference between numbers is preserved.
        /// </summary>
        /// <param name="sequence"></param>
        /// <param name="start"></param>
        /// <returns></returns>
        public static IEnumerable<double> AdjustAllByFirstTo(this IEnumerable<double> sequence, double start)
        {
            yield return start;

            using (var enmr = sequence.GetEnumerator())
            {
                enmr.MoveNext();
                var diff = enmr.Current - start;
                while (enmr.MoveNext())
                {
                    yield return enmr.Current - diff;
                }
            }
        }

        public static IObservable<KeyValuePair<DateTime, TimeSpan>> TimeOffsets(this IObservable<DateTime> scheduledTimes)
        {


            return scheduledTimes
                 .Scan(
                new KeyValuePair<DateTime, TimeSpan>(default(DateTime), new TimeSpan(0)),
                (acc, nw) => new KeyValuePair<DateTime, TimeSpan>(nw, nw - (acc.Key == default(DateTime) ? nw : acc.Key)));



        }

        public static IObservable<KeyValuePair<Tuple<DateTime, TimeSpan>, T>> IncrementalTimeOffsets<T>(this IObservable<KeyValuePair<DateTime, T>> scheduledTimes)
        {
            return scheduledTimes
                 .Scan(new KeyValuePair<Tuple<DateTime, TimeSpan>, T>(Tuple.Create(default(DateTime), default(TimeSpan)), default(T)), (acc, nw) =>
                 {
                     var ts = (acc.Key.Item1 == default(DateTime)) ? new TimeSpan(0) : nw.Key - acc.Key.Item1;
                     return new KeyValuePair<Tuple<DateTime, TimeSpan>, T>(new Tuple<DateTime, TimeSpan>(nw.Key, ts), nw.Value);
                 });
        }


        public static IObservable<KeyValuePair<T, Tuple<double, double>>> IncrementalPositionOffsets<T>(this IObservable<KeyValuePair<T, double>> scheduledTimes)
        {
            return scheduledTimes
                 .Scan(new KeyValuePair<T, Tuple<double, double>>(default(T), Tuple.Create(0d, 0d)),
                 (acc, nw) =>
                 {
                     var ts = nw.Value - acc.Value.Item1;
                     return new KeyValuePair<T, Tuple<double, double>>(nw.Key, new Tuple<double, double>(nw.Value, ts));
                 });
        }

        public static IObservable<KeyValuePair<Tuple<DateTime, TimeSpan>, T>> TotalTimeOffsets<T>(this IObservable<KeyValuePair<DateTime, T>> scheduledTimes)
        {
            //var first= scheduledTimes.First().Key;
            DateTime dt = default(DateTime);
            return scheduledTimes
                 .Scan(new KeyValuePair<Tuple<DateTime, TimeSpan>, T>(Tuple.Create(default(DateTime), default(TimeSpan)), default(T)), (acc, nw) =>
                 {
                     if (acc.Key.Item1 == default(DateTime)) dt = nw.Key;
                     return new KeyValuePair<Tuple<DateTime, TimeSpan>, T>(new Tuple<DateTime, TimeSpan>(nw.Key, acc.Key.Item1 - dt), nw.Value);
                 });
        }

        public static IObservable<Tuple<TimeSpan, T>> TotalTimeOffsets<T>(this IObservable<T> scheduledTimes, Func<T, DateTime> func)
        {
            //var first= scheduledTimes.First().Key;
            DateTime dt = default(DateTime);
            return scheduledTimes
                 .Scan(Tuple.Create(default(TimeSpan), default(T)), (acc, nw) =>
                {
                    if (acc.Item1 == default(TimeSpan)) dt = func(nw);
                    return Tuple.Create(func(nw) - dt, nw);
                });
        }




        public static IObservable<KeyValuePair<T, Tuple<double?, double>>> IncrementalPositionOffsets<T>(this IObservable<KeyValuePair<T, double?>> scheduledTimes)
        {
            return scheduledTimes
                 .Scan(new KeyValuePair<T, Tuple<double?, double>>(default(T), new Tuple<double?, double>(0d, 0d)),
                 (acc, nw) =>
                 {
                     var ts = (nw.Value - acc.Value.Item1) ?? 0;
                     return new KeyValuePair<T, Tuple<double?, double>>(nw.Key, new Tuple<double?, double>(nw.Value, ts));
                 });
        }





        // convert IEnumerable to IObservable using Scheduler
        //        answered Dec 13 '16 at 7:22        // Lee Campbell
        //https://stackoverflow.com/questions/41072709/how-to-convert-ienumerable-to-iobservable-using-historicalscheduler

        public static IObservable<T> Playback<T>(
               this IEnumerable<Timestamped<T>> enumerable,
               IScheduler scheduler)
        {
            return Observable.Create<T>(observer =>
            {
                var enumerator = enumerable.GetEnumerator();

                //declare a recursive function 
                Action<Action> scheduleNext = (self) =>
                {
                    //move
                    if (!enumerator.MoveNext())
                    {
                        //no more items (or we have been disposed)
                        //sequence has completed
                        scheduler.Schedule(() => observer.OnCompleted());
                        return;
                    }

                    //current item of enumerable sequence
                    var current = enumerator.Current;

                    //schedule the item to run at the timestamp specified
                    scheduler.Schedule(current.Timestamp, () =>
                    {
                        //push the value forward
                        observer.OnNext(current.Value);

                        //Recursively call self (via the scheduler API)
                        self();
                    });
                };

                //start the process by scheduling the recursive calls.
                // return the scheduled handle to allow disposal.
                var scheduledTask = scheduler.Schedule(scheduleNext);
                return StableCompositeDisposable.Create(scheduledTask, enumerator);
            });
        }


        //        IObservable<int> ob =
        //    Observable.Create<int>(o =>
        //    {
        //        var cancel = new CancellationDisposable(); // internally creates a new CancellationTokenSource
        //        NewThreadScheduler.Default.Schedule(() =>
        //        {
        //            int i = 0;
        //            for (; ; )
        //            {
        //                Thread.Sleep(200);  // here we do the long lasting background operation
        //                if (!cancel.Token.IsCancellationRequested)    // check cancel token periodically
        //                    o.OnNext(i++);
        //                else
        //                {
        //                    Console.WriteLine("Aborting because cancel event was signaled!");
        //                    o.OnCompleted(); // will not make it to the subscriber
        //                    return;
        //                }
        //            }
        //        }
        //        );

        //        return cancel;
        //    }
        //    );

        //        IDisposable subscription = ob.Subscribe(i => Console.WriteLine(i));
        //        Console.WriteLine("Press any key to cancel");
        //Console.ReadKey();
        //subscription.Dispose();
        //Console.WriteLine("Press any key to quit");
        //Console.ReadKey();  // give background thread chance to write the cancel acknowledge message
    }
}
