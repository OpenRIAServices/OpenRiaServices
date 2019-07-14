using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRiaServices.DomainServices.Hosting
{
    static class TaskExtensions
    {
        /// <summary>
        /// Helper method to convert from Task async method to "APM" (IAsyncResult with Begin/End calls)
        /// Copied from 
        /// https://docs.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/interop-with-other-asynchronous-patterns-and-types#TapToApm
        /// </summary>
        public static IAsyncResult BeginApm<T>(Task<T> task,
                                    AsyncCallback callback,
                                    object state)
        {
            var tcs = new TaskCompletionSource<T>(state);
            task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                    tcs.TrySetException(t.Exception.InnerExceptions);
                else if (t.IsCanceled)
                    tcs.TrySetCanceled();
                else
                    tcs.TrySetResult(t.Result);

                callback?.Invoke(tcs.Task);
            }, TaskScheduler.Default);
            return tcs.Task;
        }

        public static T EndApm<T>(IAsyncResult asyncResult)
        {
            return ((Task<T>)asyncResult).GetAwaiter().GetResult();
        }

        public static IAsyncResult BeginApm<T>(ValueTask<T> task,
                                    AsyncCallback callback,
                                    object state)
        {
            if(task.IsCompletedSuccessfully)
            {
                var tcs = new TaskCompletionSource<T>(state);
                tcs.TrySetResult(task.Result);
                callback.Invoke(tcs.Task);
                return tcs.Task;
            }
            else
            {
                return BeginApm<T>(task.AsTask(), callback, state);
            }
        }
    }
}
