using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Threading.Tasks
{
    internal static class TaskPortabilityExtensions
    {
        public static TaskAwaiter<T> GetAwaiter<T>(this Task<T> task)
        {
            return new TaskAwaiter<T>() { Task = task };
        }

        public static Task ContinueWith<T>(this Task<T> task, Action<Task<T>, object> action,
            object state, CancellationToken ct, TaskContinuationOptions tco, TaskScheduler taskScheduler)
        {
            Action<Task<T>> callback = (t) => action(t, state);
            return task.ContinueWith(callback, ct, tco, taskScheduler);
        }

        public static Task<TRes> ContinueWith<T, TRes>(this Task<T> task, Func<Task<T>, object, TRes> func,
            object state, CancellationToken ct, TaskContinuationOptions tco, TaskScheduler taskScheduler)
        {
            Func<Task<T>, TRes> callback = (t) => func(t, state);
            return task.ContinueWith(callback, ct, tco, taskScheduler);
        }

        public struct TaskAwaiter<T>
        {
            public Task<T> Task;

            public T GetResult()
            {
                if (Task.IsFaulted)
                    throw UnwrapException(Task.Exception);
                return Task.Result;
            }

            private Exception UnwrapException(AggregateException exception)
            {
                while (exception.InnerException is AggregateException aggregateException)
                    exception = aggregateException;

                return exception.InnerException;
            }
        }
    }
}
