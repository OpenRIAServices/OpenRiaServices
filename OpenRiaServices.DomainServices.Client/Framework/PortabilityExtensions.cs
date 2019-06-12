using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Threading.Tasks;

namespace OpenRiaServices.DomainServices.Client
{
    internal static class PortabilityExtensions
    {
        public static ReadOnlyCollection<T> AsReadOnly<T>(this List<T> list)
        {
            return new ReadOnlyCollection<T>(list);
        }

#if SILVERLIGHT
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

        public static TaskAwaiter<T> GetAwaiter<T>(this Task<T> task)
        {
            return new TaskAwaiter<T>() { Task = task };
        }
#endif
    }
}
