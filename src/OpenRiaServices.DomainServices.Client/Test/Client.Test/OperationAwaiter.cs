using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace OpenRiaServices.DomainServices.Client
{
    public static class OperationExtensions
    {
        private static readonly Func<Task> s_getCurrentTask = GetCurrentTaskGetter();

        public static OperationAwaiter GetAwaiter(this OperationBase operation)
        {
            return new OperationAwaiter(operation);
        }

        public class OperationAwaiter : INotifyCompletion
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
                // Capture syncContext and scheduler from await location
                var syncContext = SynchronizationContext.Current;
                TaskScheduler scheduler;
                var executionContext = ExecutionContext.Capture();
                ContextCallback action = (object o) => ((Action)o)();

                if (syncContext is null /*|| syncContext is Test.TestSynchronizationContext*/)
                {
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
                    _domainContextCompletionTask = s_getCurrentTask();
                    if (_domainContextCompletionTask != null)
                    {
                        _domainContextCompletionTask.ContinueWith((Task _, object o) =>
                        {
                            ExecutionContext.Run(executionContext, action, o);
                        }
                        , continuation
                        , CancellationToken.None
                        , TaskContinuationOptions.None
                        , scheduler);
                    }
                    else
                    {
                        ExecutionContext.Run(executionContext, action, continuation);
                    }
                };
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
