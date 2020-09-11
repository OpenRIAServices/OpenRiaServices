using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.DomainServices.Client;

namespace OpenRiaServices.Silverlight.Testing
{
    internal static class UnitTestBaseExtensions
    {
        public static void EnqueueCompletion(this UnitTestBase testBase, Func<Task> func)
        {
            EnqueueCompletion(testBase, func, Debugger.IsAttached ? UnitTestBase.DebuggingTimeoutInSeconds : UnitTestBase.DefaultTimeoutInSeconds);
        }

        public static void EnqueueCompletion(this UnitTestBase testBase, Func<Task> func, int timeoutInSeconds)
        {
            testBase.Enqueue(() =>
                {
                    var task = func();
                    if (!task.IsCompleted)
                    {
                        try
                        {
                            if (!task.Wait(TimeSpan.FromSeconds(timeoutInSeconds)))
                                Assert.Fail(UnitTestBase.ComposeTimeoutMessage(timeoutInSeconds, string.Empty));
                        }
                        catch (OperationCanceledException)
                        {
                            // cancelled tasks are also completed
                        }
                        Assert.IsTrue(task.IsCompleted);
                    }
                });
        }

        public static void EnqueueCompletion(this UnitTestBase testBase, Func<OperationBase> func)
        {
            EnqueueCompletion(testBase, func, Debugger.IsAttached ? UnitTestBase.DebuggingTimeoutInSeconds : UnitTestBase.DefaultTimeoutInSeconds);
        }

        public static void EnqueueCompletion(this UnitTestBase testBase, Func<OperationBase> func, int timeoutInSeconds)
        {
            testBase.Enqueue(() =>
            {
                var operation = func();
                if (!operation.IsComplete)
                {
                    using var semaphore = new SemaphoreSlim(0);
                    // The awaiter has some extra code to wait a bit more after Completed event
                    // so more property changes etc will have time to trigger
                    operation.Completed += (op, args) =>
                    {
                        var task = OperationExtensions.OperationAwaiter.GetCurrentExecutingTask();
                        if (task != null)
                        {
                            task.ConfigureAwait(false)
                                .GetAwaiter()
                                .UnsafeOnCompleted(() => semaphore.Release());
                        }
                        else
                        {
                            Debug.Assert(false, "no task");
                            semaphore.Release();
                        }
                    };

                    if (!semaphore.Wait(TimeSpan.FromSeconds(timeoutInSeconds)))
                        Assert.Fail(UnitTestBase.ComposeTimeoutMessage(timeoutInSeconds, string.Empty));
                }

                Assert.IsTrue(operation.IsComplete);
            });
        }
    }
}
