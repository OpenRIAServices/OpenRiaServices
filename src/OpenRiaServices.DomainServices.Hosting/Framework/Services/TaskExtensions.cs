using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenRiaServices.DomainServices.Hosting
{
    static class TaskExtensions
    {
        public static T EndApm<T>(IAsyncResult asyncResult)
        {
            return ((AsyncResult<T>)asyncResult).GetResult();
        }

        /// <summary>
        /// Helper method to convert from Task async method to "APM" (IAsyncResult with Begin/End calls)
        /// </summary>
        public static IAsyncResult BeginApm<T>(ValueTask<T> task,
                                    AsyncCallback callback,
                                    object state)
        {

            var result = new AsyncResult<T>(task, callback, state);
            if (result.CompletedSynchronously)
            {
                result.ExecuteCallback();
                return result;
            }
            else
            {
                task.AsTask()
                    .ContinueWith((res, asyncResult) =>
                {
                    ((AsyncResult<T>)asyncResult).ExecuteCallback();
                },
                result,
                CancellationToken.None,
                TaskContinuationOptions.None,
                TaskScheduler.Default);
            }

            return result;
        }

        private class AsyncResult<T> : IAsyncResult
        {
            private readonly ValueTask<T> _task;
            private readonly object _asyncState;
            private readonly AsyncCallback _asyncCallback;

            public AsyncResult(ValueTask<T> task, AsyncCallback asyncCallback, object asyncState)
            {
                _task = task;
                _asyncCallback = asyncCallback;
                _asyncState = asyncState;
                CompletedSynchronously = task.IsCompleted;
            }

            public T GetResult() => _task.GetAwaiter().GetResult();

            // Calls the async callback with this
            public void ExecuteCallback() => _asyncCallback(this);

            #region IAsyncResult implementation forwarded to Task implementation
            object IAsyncResult.AsyncState => _asyncState;
            WaitHandle IAsyncResult.AsyncWaitHandle => !CompletedSynchronously ? ((IAsyncResult)_task.AsTask()).AsyncWaitHandle : throw new NotImplementedException();

            public bool CompletedSynchronously { get; }
            bool IAsyncResult.IsCompleted => _task.IsCompleted;
            #endregion

        }
    }
}
