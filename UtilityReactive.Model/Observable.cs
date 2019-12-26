using System;
using System.Collections.Generic;
using System.Text;

namespace UtilityReactive.Model
{
    public abstract class Observable<T> : IObservable<T>
    {
        protected ICollection<IObserver<T>> observers = new List<IObserver<T>>();
        object obj = new object();
        
        public Observable() : base()
        {
   
        }

        protected virtual void Update(T t)
        {
            lock (obj)
            {
                foreach (var x in observers)
                    x.OnNext(t);
            }
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            lock (obj)
            {
                observers.Add(observer);
                return Disposable.Create(() => observers.Remove(observer));
            }
        }
    }
}
