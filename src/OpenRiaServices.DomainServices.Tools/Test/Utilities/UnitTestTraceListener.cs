using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenRiaServices.DomainServices.Server.Test.Utilities
{
    /// <summary>
    /// This class is meant to be used with unit tests to disable the Debug.Assert dialog
    /// and instead log the Debug.Assert message to the test context log file and failing
    /// the test as an option.
    /// </summary>
    public class UnitTestTraceListener : global::System.Diagnostics.DefaultTraceListener
    {
        private static UnitTestTraceListener Listener;
        private static TestContext Context;
        private static bool FailOnDebugAssert;
        private static bool OriginalAssertUiEnabled;

        /// <summary>
        /// Class constructor.
        /// </summary>
        private UnitTestTraceListener()
        {
            this.Name = "UnitTestListener";
            this.AssertUiEnabled = false;
        }

        /// <summary>
        /// Initializes the Debug class with this trace listener and disables the Debug.Assert UI.
        /// </summary>
        /// <param name="context">the TestContext for the unit tests.</param>
        /// <param name="failOnDebugAssert">True to fail the test when hitting a Debug.Assert failure, false to continue the test.</param>
        public static void Initialize(TestContext context, bool failOnDebugAssert)
        {
            if (Listener == null)
            {
                Context = context;
                FailOnDebugAssert = failOnDebugAssert;
                Listener = new UnitTestTraceListener();
                if (Debug.Listeners.Count > 0)
                {
                    DefaultTraceListener defaultListener = Debug.Listeners[0] as DefaultTraceListener;
                    if (defaultListener != null)
                    {
                        OriginalAssertUiEnabled = defaultListener.AssertUiEnabled;
                        defaultListener.AssertUiEnabled = false;
                    }
                }
                Debug.Listeners.Add(Listener);
            }
        }

        /// <summary>
        /// Resets this and the Debug class to its original settings.
        /// </summary>
        public static void Reset()
        {
            if (Listener != null)
            {
                Debug.Listeners.Remove(Listener);
                if (Debug.Listeners.Count > 0)
                {
                    DefaultTraceListener defaultListener = Debug.Listeners[0] as DefaultTraceListener;
                    if (defaultListener != null)
                    {
                        defaultListener.AssertUiEnabled = OriginalAssertUiEnabled;
                    }
                }
                Listener = null;
            }
        }

        /// <summary>
        /// TraceListener method override.  This method is in charge of logging the Debug.Assert message
        /// into the test context and optionally failing the test.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="detailMessage"></param>
        public override void Fail(string message, string detailMessage)
        {
            if (FailOnDebugAssert)
            {
                Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail("Debug.Assert Failed: " + message + " " + detailMessage);
            }
            else
            {
                Context.WriteLine("Hit a Debug.Assert failure: " + message + "\r\n" + detailMessage);
            }
        }
    }
}
