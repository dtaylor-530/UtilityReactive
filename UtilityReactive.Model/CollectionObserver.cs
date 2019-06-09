using System;
using System.Collections.Generic;
using System.Text;

namespace UtilityReactive.Model
{


        public class CollectionObserver<T> : IObserver<T>
        {
            private ICollection<T> collection;

            public CollectionObserver(ICollection<T> collection)
            {
                this.collection = collection;
            }



            public virtual void OnCompleted()
            {
                throw new NotImplementedException();
            }

            public virtual void OnError(Exception error)
            {
                throw new NotImplementedException();
            }

            public virtual void OnNext(T value)
            {
                collection.Add(value);
            }
        }


    public abstract class CollectionObserver<T,R> : IObserver<T>
    {
        private ICollection<R> collection;

        public CollectionObserver(ICollection<R> collection)
        {
            this.collection = collection;
        }

        protected abstract R Convert(T t);

        public virtual void OnCompleted()
        {
            throw new NotImplementedException();
        }

        public virtual void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public virtual void OnNext(T value)
        {
            collection.Add(Convert(value));
        }
    }


}
