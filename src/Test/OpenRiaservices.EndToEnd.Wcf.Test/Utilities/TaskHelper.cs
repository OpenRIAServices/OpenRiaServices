using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRiaServices.Client.Test.Utilities
{
    internal static class TaskHelper
    {
        /// <summary>
        /// Returns an error task of the given type. The task is Completed, IsCanceled = False, IsFaulted = True
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        public static Task<TResult> FromException<TResult>(Exception exception)
        {
            TaskCompletionSource<TResult> tcs = new TaskCompletionSource<TResult>();
            tcs.SetException(exception);
            return tcs.Task;
        }

        public static Task<TResult> FromResult<TResult>(TResult value)
        {
#if SILVERLIGHT
            TaskCompletionSource<TResult> tcs = new TaskCompletionSource<TResult>();
            tcs.SetResult(value);
            return tcs.Task;
#else
            return Task.FromResult(value);
#endif
        }
    }
}
