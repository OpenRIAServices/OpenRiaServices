﻿using System;
using System.Threading.Tasks;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace OpenRiaServices.Client
{
    public static class OperationExtensions
    {
        private static readonly Func<Task> s_getCurrentTask = GetCurrentTaskGetter();

        public static OperationAwaiter GetAwaiter(this OperationBase operation)
        {
            return new OperationAwaiter(operation);
        }

        public class OperationAwaiter : ICriticalNotifyCompletion
        {
            private readonly OperationBase _operation;
            private Task _domainContextCompletionTask;

            public OperationAwaiter(OperationBase control)
            {
                _operation = control;
            }
            public bool IsCompleted
            {
                get { return _operation.IsComplete; }
            }

            public void OnCompleted(Action continuation)
            {
                var executionContext = ExecutionContext.Capture();
                ContextCallback action = (object o) => ((Action)o)();
                UnsafeOnCompleted(() => ExecutionContext.Run(executionContext, action, continuation));
            }

            public void UnsafeOnCompleted(Action continuation)
            {
                if (_operation.IsComplete)
                {
                    continuation();
                    return;
                }

                // Capture syncContext and scheduler from await location
                var syncContext = SynchronizationContext.Current;
                TaskScheduler scheduler;

                if (syncContext is null /*|| syncContext is Test.TestSynchronizationContext*/)
                {
                    // Note we use Current instead of Default in case we want to change
                    // the test runner to set a limited concurrency scheduler to mimic
                    // a main "ui" thread
                    scheduler = TaskScheduler.Current;
                }
                else
                {
                    scheduler = TaskScheduler.FromCurrentSynchronizationContext();
                }

                _operation.Completed += (sender, args) =>
                {
                    // The operation is completed in a ContinueWith callback
                    // Get the task associated with the callback
                    // so that continuation is run after the whole completion
                    // Since the currentTask is currently executing
                    // This will not complete now, but later when it is finished
                    _domainContextCompletionTask = GetCurrentExecutingTask();
                    if (_domainContextCompletionTask != null)
                    {
                        _domainContextCompletionTask.ContinueWith((Task _, object o) =>
                        {
                            ((Action)o)();
                        }
                        , continuation
                        , CancellationToken.None
                        , TaskContinuationOptions.ExecuteSynchronously
                        , scheduler);
                    }
                    else
                    {
                        continuation();
                    }
                };
            }

            
            internal static Task GetCurrentExecutingTask()
            {
                return s_getCurrentTask();
            }

            public void GetResult()
            {
                if (!_operation.IsComplete)
                    throw new InvalidOperationException();

                // Pass any exception from the callbacks which were not handled
                if (_domainContextCompletionTask != null)
                    _domainContextCompletionTask.GetAwaiter().GetResult();
            }
        }

        private static Func<Task> GetCurrentTaskGetter()
        {
            var property =
typeof(Task).GetProperty("InternalCurrent", BindingFlags.Static | BindingFlags.NonPublic);

            var getMethod = property.GetGetMethod(nonPublic: true);
            return (Func<Task>)Delegate.CreateDelegate(typeof(Func<Task>), getMethod);
        }
    }
}
