using System;
using System.Collections.Generic;
using System.Text;

namespace UtilityReactive.Model
{
    public abstract class Observable<T> : IObservable<T>
    {
        ICollection<IObserver<T>> observers = new List<IObserver<T>>();

        public Observable() : base()
        {
        }

        protected virtual void Update(T t)
        {
            foreach (var x in observers)
                x.OnNext(t);
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            observers.Add(observer);
            return Disposable.Create(() => observers.Remove(observer));
        }
    }
}
