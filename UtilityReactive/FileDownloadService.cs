
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;

namespace UtilityReactive
{




    public class FileDownloadService : IFileDownloadService<int, Tuple<Uri, string, bool>>
    {


        public IObservable<int> Progress { get; }
        public IObservable<Tuple<Uri, string, bool>> Completed { get; }

        private Queue<Tuple<Uri, string>> queue = new Queue<Tuple<Uri, string>>();

        public FileDownloadService(IObservable<Tuple<Uri, string>> files)
        {
            using (var client = new WebClient())
            {
                var ddd = Observable.FromEventPattern<AsyncCompletedEventHandler, AsyncCompletedEventArgs>(
                h => client.DownloadFileCompleted += h,
                h => client.DownloadFileCompleted -= h);

                Completed = Observable.Create<Tuple<Uri, string, bool>>(observer =>
                 ddd.Zip(files, (a, b) => new { a, b })
                 .Subscribe(_ => Task.Run(() => FileHelper.CheckFile(_.b.Item2)).ToObservable()
                                                 .Subscribe(__ => observer.OnNext(Tuple.Create(_.b.Item1, _.b.Item2, __)))));



                Progress = Observable.FromEventPattern<DownloadProgressChangedEventHandler, DownloadProgressChangedEventArgs>(
         h => client.DownloadProgressChanged += h,
         h => client.DownloadProgressChanged -= h)
         .Select(e => e.EventArgs.ProgressPercentage)
         .Merge(ddd.Select(_ => 0));


                files.Subscribe(_ =>
                {
                    if (!client.IsBusy & queue.Count == 0)
                        client.DownloadFileAsync(_.Item1, _.Item2);
                    else
                        queue.Enqueue(_);
                });

                Completed.Subscribe(_ =>
                {
                    if (queue.Count > 0)
                    {
                        if (!client.IsBusy & queue.Count > 0)
                        {
                            var p = queue.Dequeue();
                            client.DownloadFileAsync(p.Item1, p.Item2);
                        }
                    }
                });


            }
        }
    }



    public class FileDownloadServiceDefault : IFileDownloadService<DownloadProgressChangedEventArgs, AsyncCompletedEventArgs>
    {


        public IObservable<DownloadProgressChangedEventArgs> Progress { get; }
        public IObservable<AsyncCompletedEventArgs> Completed { get; }


        public FileDownloadServiceDefault(IObservable<Tuple<Uri, string>> files)
        {
            using (var client = new WebClient())
            {
                var Completed = Observable.FromEventPattern<AsyncCompletedEventHandler, AsyncCompletedEventArgs>(
                h => client.DownloadFileCompleted += h,
                h => client.DownloadFileCompleted -= h).Select(_ => _.EventArgs);


                Progress = Observable.FromEventPattern<DownloadProgressChangedEventHandler, DownloadProgressChangedEventArgs>(
         h => client.DownloadProgressChanged += h,
         h => client.DownloadProgressChanged -= h)
         .Select(e => e.EventArgs);


                files.Subscribe(_ =>
                {
                    client.DownloadFileAsync(_.Item1, _.Item2);
                });
            }
        }
    }


    public interface IFileDownloadService<R, T>
    {

        IObservable<R> Progress { get; }
        IObservable<T> Completed { get; }
    }



    public static class FileHelper
    {

        public static bool CheckFile(string sink)
        {

            System.IO.FileInfo info = new System.IO.FileInfo(sink);
            if (info.Length > 0)
            {
                return true;
                //Kaliko.Logger.Write(string.Format("File {0} downloaded to {1} @ {2}", source, sink, DateTime.Now), Kaliko.Logger.Severity.Info);
            }
            else
            {
                System.IO.File.Delete(sink);
                return false;
            }

        }
    }
}