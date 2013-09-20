using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Silverlight.Testing
{
    /// <summary>
    /// Abstract base class for all Unit Tests in WCF RIA Services.
    /// </summary>
    [DebuggerStepThrough]
    public abstract class UnitTestBase : SilverlightTest
    {
        // The number of timeouts we allow by default
        protected const int DefaultTimeoutThreshold = 2;

        // The length of time we wait by default before timing out
        protected const int DefaultTimeoutInSeconds = 60;
        protected const int DebuggingTimeoutInSeconds = 600;

        // The number of timeouts
        private static int NumberOfTimeouts;

        public override void EnqueueConditional(Func<bool> conditionalDelegate)
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

        public virtual void EnqueueConditional(Func<bool> conditionalDelegate, int timeoutInSeconds, string timeoutMessage)
        {
            DateTime endTime = DateTime.Now.AddSeconds(timeoutInSeconds);

            base.EnqueueConditional(() =>
            {
                bool conditionSatisfied = conditionalDelegate();

                if (!conditionSatisfied)
                {
                    if (DateTime.Now >= endTime)
                    {
                        UnitTestBase.NumberOfTimeouts++;
                        Assert.Fail(UnitTestBase.ComposeTimeoutMessage(timeoutInSeconds, timeoutMessage));
                    }
                }

                return conditionSatisfied;
            });
        }

        private static string ComposeTimeoutMessage(int timeoutInSeconds, string timeoutMessage)
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

        public override void EnqueueWorkItem(IWorkItem testTaskObject)
        {
            if (UnitTestBase.NumberOfTimeouts >= UnitTestBase.DefaultTimeoutThreshold)
            {
                Assert.Inconclusive("The test was not attempted because the number of tests that have timed-out has exceeded the threshold.");
            }

            base.EnqueueWorkItem(testTaskObject);
        }
    }
}
