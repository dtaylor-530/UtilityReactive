using DynamicData;
using DynamicData.Kernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;


namespace UtilityReactive
{
    public static class ChangeSetFactory
    {


            public static IObservable<IChangeSet<T, R>> Build<T,R>(Func<T, R> getkey, IObservable<T> adds, IObservable<T> removals, IObservable<object> ClearedSubject = null, IObservable<Predicate<T>> filter= null)
            {
                //ViewModel.InteractiveCollectionViewModel<T, object> interactivecollection = null;
                System.Reactive.Subjects.ISubject<Exception> exs = new System.Reactive.Subjects.Subject<Exception>();

     
                var changeset = ObservableChangeSet.Create(cache =>
                {
                    var dels = removals/*.WithLatestFrom(RemoveSubject.StartWith(Remove).DistinctUntilChanged(), (d, r) => new { d, r })*/.Subscribe(_ =>
                    {
                        try
                        {
                            cache.Remove(_);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("error removing " + _.ToString() + " from cache");
                            Console.WriteLine(ex.Message);
                            exs.OnNext(ex);
                            //ArgumentNullException
                        }
                    });

                    ClearedSubject?.Subscribe(_ => cache.Clear());

                    adds.Subscribe(_ =>
                    {
                            try
                            {
                                cache.AddOrUpdate(_);

                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("error adding " + _.ToString() + " from cache");
                                Console.WriteLine(ex);
                                exs.OnNext(ex);
                            }
                    });
                    return new System.Reactive.Disposables.CompositeDisposable(dels);

                }, getkey);

                if (filter != null)
                    return changeset.Filter(filter.Select(_ => { Func<T, bool> f = aa => _(aa); return f; }).StartWith(ft));
                else
                    return changeset;

            }

            private static bool ft<T>(T o) => true;
        

        public static IObservable<IChangeSet<T, TKey>> Build<T, TKey>(Func<IEnumerable<T>> creatorfunc, TimeSpan ts, Func<T, TKey> selectorfunc, IScheduler scheduler, Func<T, bool> expirationfunc)
        {

            return ObservableChangeSet.Create(cache =>
            {

                cache.AddOrUpdate(creatorfunc());
                // generate items  according to scheduled function
                var tradeGenerator = scheduler
            .ScheduleRecurringAction(ts, () => cache.AddOrUpdate(creatorfunc()));

                //expire closed items from the cache to avoid unbounded data  according to to expiration func
                var expirer = cache
            .ExpireAfter(t => expirationfunc(t) ? TimeSpan.FromMinutes(1) : (TimeSpan?)null, TimeSpan.FromMinutes(1), scheduler);
                //.Subscribe(x => _logger.Info("{0} filled trades have been removed from memory", x.Count()));

                return tradeGenerator;
            },
            // update/add items according to selector function
            selectorfunc);


        }



        public static IObservable<IChangeSet<T, TKey>> Build<T, TKey>(Func<IEnumerable<T>> creatorfunc, TimeSpan ts, Func<T, TKey> selectorfunc, IScheduler scheduler)
        {

            return ObservableChangeSet.Create(cache =>
            {
                cache.AddOrUpdate(creatorfunc());
                return scheduler.ScheduleRecurringAction(ts, () => cache.AddOrUpdate(creatorfunc()));
            }
            , selectorfunc);


        }



        public static IObservable<IChangeSet<T, R>> Build<T, R>(Func<IEnumerable<T>> func, Func<T, R> selector, TimeSpan ts, string message = null)
        {

            Func<ISourceCache<T, R>, Action> fca = cache => () =>
            {
                if (func() != null)
                {
                    cache.AddOrUpdate(func().ToList());
                   Console.WriteLine(message??$"updated {typeof(T).Name}s");
                }
            };
            return Build(fca, selector, ts);
        }


        public static IObservable<IChangeSet<T, R>> Build<S, T, R, U>(S complex, U key, Func<S, U, IEnumerable<T>> func, Func<T, R> selector, TimeSpan ts, string message = null)
        {
            Func<ISourceCache<T, R>, Action> fca = cache => () =>
            {
                var a = func(complex, key);
                if (a != null)
                {
                    cache.AddOrUpdate(a.ToList());
                   Console.WriteLine(message ?? $"updated {typeof(T).Name}s");
                }
            };

            return Build(fca, selector, ts);
        }



        static IObservable<IChangeSet<T, R>> Build<T, R>(Func<ISourceCache<T, R>, Action> fca, Func<T, R> selector, TimeSpan ts)
        {

            return ObservableChangeSet.Create<T, R>(cache =>
            {
                fca(cache)();
                return TaskPoolScheduler.Default.ScheduleRecurringAction(ts, fca(cache));
            }, selector);
        }



    }




}
