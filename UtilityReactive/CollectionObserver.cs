using System;
using System.Collections.Generic;
using System.Text;

namespace UtilityReactive
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



    
}
