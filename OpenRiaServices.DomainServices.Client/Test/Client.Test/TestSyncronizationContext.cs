using System;
using System.Linq;
using System.Threading;

namespace OpenRiaServices.DomainServices.Client.Test
{
    /// <summary>
    /// A <see cref="SynchronizationContext"/> which executes all actions immediately.
    /// </summary>
    class TestSynchronizationContext : SynchronizationContext
    {
        /// <summary>
        /// Turn async execution into sync execution
        /// </summary>
        /// <param name="d"></param>
        /// <param name="state"></param>
        public override void Post(SendOrPostCallback d, object state)
        {
            d(state);
        }

        public override void Send(SendOrPostCallback d, object state)
        {
            d(state);
        }

        public override SynchronizationContext CreateCopy()
        {
            return new TestSynchronizationContext();
        }
    }
}
