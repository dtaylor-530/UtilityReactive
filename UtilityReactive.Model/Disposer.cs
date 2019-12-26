using System;
using System.Collections.Generic;
using System.Text;

namespace UtilityReactive.Model
{
    public class Disposer<T> : IDisposable where T : IComparable<T>
    {
        // The observers list recieved from the observable
        private List<IObserver<T>> subjectObservers;

        // The observer instance to unsubscribe
        private IObserver<T> observer;

        public Disposer(List<IObserver<T>> subObservers, IObserver<T> observer)
        {
            this.subjectObservers = subObservers;
            this.observer = observer;
        }

        public void Dispose()
        {
            if (this.subjectObservers.Contains(observer))
            {
                this.subjectObservers.Remove(observer);
            }
        }
    }
}
