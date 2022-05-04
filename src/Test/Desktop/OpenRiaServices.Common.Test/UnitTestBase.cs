using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Security;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenRiaServices.Silverlight.Testing
{
    /// <summary>
    /// Abstract base class for all Unit Tests in WCF RIA Services.
    /// </summary>
    [DebuggerStepThrough]
    public abstract class UnitTestBase
    {
        // The number of timeouts we allow by default
        protected const int DefaultTimeoutThreshold = 2;

        // The length of time we wait by default before timing out
        protected internal const int DefaultTimeoutInSeconds = 60;
        protected internal const int DebuggingTimeoutInSeconds = 600;

        // Delay between conditional evaluations
        private const int DefaultStepInMilliseconds = 10;

        // The number of timeouts
        private static int NumberOfTimeouts;

        private Queue<Action> _asyncQueue;

        protected UnitTestBase()
        {
        }

        private Queue<Action> AsyncQueue
        {
            get
            {
                if (this._asyncQueue == null)
                {
                    this._asyncQueue = new Queue<Action>();
                }

                return this._asyncQueue;
            }
        }

        public virtual void Enqueue(Action d)
        {
            if (UnitTestBase.NumberOfTimeouts >= UnitTestBase.DefaultTimeoutThreshold)
            {
                Assert.Inconclusive("The test was not attempted because the number of tests that have timed-out has exceeded the threshold.");
            }

            this.AsyncQueue.Enqueue(d);
        }

        public virtual void EnqueueCallback(Action testCallbackDelegate)
        {
            this.Enqueue(testCallbackDelegate);
        }

        public virtual void EnqueueConditional(Func<bool> conditionalDelegate)
        {
            this.EnqueueConditional(conditionalDelegate, string.Empty);
        }

        public virtual void EnqueueConditional(Func<bool> conditionalDelegate, string timeoutMessage)
        {
            this.EnqueueConditional(conditionalDelegate,
                Debugger.IsAttached ? UnitTestBase.DebuggingTimeoutInSeconds : UnitTestBase.DefaultTimeoutInSeconds,
                timeoutMessage);
        }

        public virtual void EnqueueConditional(Func<bool> conditionalDelegate, int timeoutInSeconds)
        {
            this.EnqueueConditional(conditionalDelegate, timeoutInSeconds, string.Empty);
        }

        public void EnqueueConditional(Func<bool> conditionalDelegate, int timeoutInSeconds, string timeoutMessage)
        {
            DateTime endTime = DateTime.UtcNow.AddSeconds(timeoutInSeconds);

            this.Enqueue(
                () =>
                {
                    while (!conditionalDelegate())
                    {
                        if (DateTime.UtcNow >= endTime)
                        {
                            UnitTestBase.NumberOfTimeouts++;
                            Assert.Fail(UnitTestBase.ComposeTimeoutMessage(timeoutInSeconds, timeoutMessage));
                        }

                        Thread.Sleep(UnitTestBase.DefaultStepInMilliseconds);
                    }
                });
        }

        internal static string ComposeTimeoutMessage(int timeoutInSeconds, string timeoutMessage)
        {
            string failureMessage =
                "The test was unable to satisfy the condition within the specified " +
                timeoutInSeconds + " second timeout.";
            if (!string.IsNullOrEmpty(timeoutMessage))
            {
                failureMessage += "\n " + timeoutMessage;
            }

            return failureMessage;
        }

        public virtual void EnqueueDelay(TimeSpan delay)
        {
            this.Enqueue(() => Thread.Sleep((int)delay.TotalMilliseconds));
        }

        public void EnqueueDelay(int milliseconds)
        {
            this.EnqueueDelay(TimeSpan.FromMilliseconds(milliseconds));
        }

        public virtual void EnqueueTestComplete()
        {
            this.ProcessQueue();
        }

        private void ProcessQueue()
        {
            while (this.AsyncQueue.Count > 0)
            {
                Action action = this.AsyncQueue.Dequeue();
                action();
            }
        }
    }
}
