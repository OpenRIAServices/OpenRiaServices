using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OrderedSynchronizationContext = global::System.Threading.OrderedSynchronizationContext;

namespace System.Windows.Ria.Test
{
    [TestClass]
    public class SyncContextTests : UnitTestBase
    {
        [Asynchronous]
        [TestMethod]
        public void VerifyOrder()
        {
            OrderedSynchronizationContext sc = new OrderedSynchronizationContext();
            int counter = 0;
            int increments = 0;
            for (int i = 0; i < 50; i++)
            {
                sc.Post(delegate(object state)
                {
                    lock (sc)
                    {
                        if (counter == (int)state)
                        {
                            counter++;
                        }
                        increments++;
                    }
                }, i);
            }

            EnqueueConditional(() => increments == 50);
            EnqueueCallback(delegate
            {
                Assert.AreEqual(50, increments);
            });
            EnqueueTestComplete();
        }
    }
}
