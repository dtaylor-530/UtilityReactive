using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;


namespace UtilityReactive
{



    public static class Statistic
    {
        public static IObservable<double> WeightedAverage<T>(this IObservable<T> records, Func<T, double> value, Func<T, double> weight)
        {
            return records.Scan(new List<T>(), (a, b) =>
            {
                a.Add(b);
                return a;
            }).Select(_ => WeightedAverage(_, value, weight));
        }


        public static IObservable<double> Average<T>(this IObservable<T> records, Func<T, double> value)
        {
            return records.Scan(new List<T>(), (a, b) =>
            {
                a.Add(b);
                return a;
            }).Select(_ => _.Average(value));
        }

        public static IObservable<R> Scan<T, R>(this IObservable<T> records, Func<List<T>, R> conversion)
        {
            return records.Scan(new List<T>(), (a, b) =>
            {
                a.Add(b);
                return a;
            }).Select(_ => conversion(_));
        }

        public static IObservable<KeyValuePair<R, double>> WeightedAverage<T, R>(this IObservable<T> records, Func<T, double> value, Func<T, double> weight, Func<List<T>, R> KeySelector)
        {
            return records.Scan(new List<T>(), (a, b) =>
            {
                a.Add(b);
                return a;
            }).Select(_ => new KeyValuePair<R, double>(KeySelector(_), WeightedAverage(_, value, weight)));
        }


        /// <summary>
        /// (every value minus the lastvalue) multiplied by weight (equivalent to profit if weights are quantities and values are prices)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="R"></typeparam>
        /// <param name="records"></param>
        /// <param name="value"></param>
        /// <param name="weight"></param>
        /// <param name="KeySelector"></param>
        /// <returns></returns>
        public static IObservable<KeyValuePair<R, double>> WeightedDeviationFromLast<T, R>(this IObservable<T> records, Func<T, double> value, Func<T, double> weight, Func<List<T>, R> KeySelector)
        {
            return records.Scan(new List<T>(), (a, b) =>
            {
                a.Add(b);
                return a;
            }).Select(_ => new KeyValuePair<R, double>(KeySelector(_), WeightedAverage(_, value, weight, value(_.Last()))));
        }





        public static double WeightedAverage<T>(IEnumerable<T> records, Func<T, double> value, Func<T, double> weight, double control = 0)
        {
            double weightedValueSum = records.Sum(x => (value(x) - control) * weight(x));
            double weightSum = records.Sum(x => weight(x));

            if (weightSum != 0)
                return weightedValueSum / weightSum;
            else
                throw new DivideByZeroException("Divide by zero exception calculating weighted average");
        }


    }

}
