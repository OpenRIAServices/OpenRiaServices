using System;
using System.Threading;
using System.Threading.Tasks;

namespace OpenRiaServices.Hosting
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
            }
            else
            {
                // We use OnCompleted rather than ContinueWith in order to avoid running synchronously
                // if the task has already completed by the time we get here. 
                // This will allocate a delegate and some extra data to add it as a TaskContinuation
                task.ConfigureAwait(false)
                    .GetAwaiter()
                    .OnCompleted(result.ExecuteCallback);
            }

            return result;
        }

        private class AsyncResult<T> : IAsyncResult
        {
            private readonly ValueTask<T> _valueTask;
            private readonly object _asyncState;
            private readonly AsyncCallback _asyncCallback;

            public AsyncResult(ValueTask<T> valueTask, AsyncCallback asyncCallback, object asyncState)
            {
                _valueTask = valueTask;
                _asyncCallback = asyncCallback;
                _asyncState = asyncState;
                CompletedSynchronously = valueTask.IsCompleted;
            }

            public T GetResult() => _valueTask.GetAwaiter().GetResult();

            // Calls the async callback with this
            public void ExecuteCallback() => _asyncCallback(this);

            #region IAsyncResult implementation forwarded to Task implementation
            object IAsyncResult.AsyncState => _asyncState;
            WaitHandle IAsyncResult.AsyncWaitHandle => !CompletedSynchronously ? ((IAsyncResult)_valueTask.AsTask()).AsyncWaitHandle : throw new NotImplementedException();

            public bool CompletedSynchronously { get; }
            bool IAsyncResult.IsCompleted => _valueTask.IsCompleted;
            #endregion

        }
    }
}
