using System;
using System.Collections.Generic;
using System.Text;

namespace UtilityReactive.Model
{

    public class CollectionSubject<T> : CollectionObserver<T>, IObservable<T>
    {
        private ICollection<IObserver<T>> observers;

        public CollectionSubject(ICollection<T> collection) : base(collection)
        {
        }


        public override void OnNext(T value)
        {
            base.OnNext(value);
            foreach (var observer in observers)
            {
                observer.OnNext(value);
            }
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            return Disposable.Create(() => observers.Remove(observer));
        }
    }
}
