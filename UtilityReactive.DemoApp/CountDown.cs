using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Text;

namespace UtilityReactive.DemoApp
{
    public class CountDown : INotifyPropertyChanged
    {
        private DateTime dateTime;
        private TimeSpan rate;
        private TimeSpan timeRemaining;


        public CountDown(DateTime dateTime, TimeSpan rate)
        {
            this.OnAnyPropertyChange().StartWith(this).Subscribe(a =>
            {
                IDisposable dis = null;

                dis = Observable.Interval(Rate).Subscribe(a =>
                 {
                     var diff = dateTime- DateTime.Now;

                     if (diff.Ticks > 0)
                     {
                         TimeRemaining = diff;
                        // Debug.WriteLine(TimeRemaining);
                     }
                     else
                     {
                         TimeRemaining = default(TimeSpan);
                         //Debug.WriteLine(dateTime + "  " + TimeRemaining);
                         dis?.Dispose();
                     }
                 });
            });

            DateTime = dateTime;
            Rate = rate;
        }

        public DateTime DateTime
        {
            get => dateTime;
            set
            {
                dateTime = value;

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DateTime)));
            }
        }

        public TimeSpan TimeRemaining
        {
            get => timeRemaining;
            set
            {
                timeRemaining = value;

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TimeRemaining)));
            }
        }

        public TimeSpan Rate
        {
            get => rate;
            set
            {
                rate = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Rate)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;


    }

    public static class Helper
    {
        /// <summary>
        /// Returns an observable sequence of the source any time the <c>PropertyChanged</c> event is raised.
        /// </summary>
        /// <typeparam name="T">The type of the source object. Type must implement <seealso cref="INotifyPropertyChanged"/>.</typeparam>
        /// <param name="source">The object to observe property changes on.</param>
        /// <returns>Returns an observable sequence of the value of the source when ever the <c>PropertyChanged</c> event is raised.</returns>
        public static IObservable<T> OnAnyPropertyChange<T>(this T source)
            where T : INotifyPropertyChanged
        {
            return System.Reactive.Linq.Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                                handler => handler.Invoke,
                                h => source.PropertyChanged += h,
                                h => source.PropertyChanged -= h)
                            .Select(_ => source);
        }
    }
}