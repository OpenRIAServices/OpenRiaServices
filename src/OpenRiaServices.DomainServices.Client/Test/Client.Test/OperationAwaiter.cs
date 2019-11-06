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

        public struct OperationAwaiter : INotifyCompletion
        {
            private readonly OperationBase _operation;
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
                var scheduler = TaskScheduler.Current;
                var executionContext = ExecutionContext.Capture();

                _operation.Completed += (sender, args) =>
                {
                    ContextCallback action;
                    if (syncContext is null /*|| syncContext is Test.TestSynchronizationContext*/)
                    {
                        action = (object o) => ((Action)o)();
                    }
                    else
                    {
                        action = (object o) =>
                        {
                            SynchronizationContext.Current.Post((object s) => { ((Action)s)(); }, o);
                        };
                    }

                    // The operation is completed in a ContinueWith callback
                    // Get the task associated with the callback
                    // so that continuation is run after the whole completion
                    // Since the currentTask is currently executing
                    // This will not complete now, but later when it is finished
                    var currentTask = s_getCurrentTask();
                    currentTask.ContinueWith((Task _, object o) =>
                    {
                        ExecutionContext.Run(executionContext, action, o);
                    }
                    , continuation
                    , CancellationToken.None
                    , TaskContinuationOptions.None
                    , scheduler);
                };
            }

            public void GetResult()
            {
                if (!_operation.IsComplete)
                    throw new InvalidOperationException();
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
