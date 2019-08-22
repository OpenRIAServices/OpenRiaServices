using System.Diagnostics;

namespace System.Windows.Common
{
    /// <summary>
    /// Simple debug tracing utility.
    /// See sample usage in DomainDataSource.cs
    /// </summary>
    internal static class DebugTrace
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Used in debug mode only")]
        internal class TraceSwitch
        {            
            /// <summary>
            /// Internal TraceSwitch constructor.
            /// </summary>
            /// <param name="displayName">The display name.</param>
            public TraceSwitch(string displayName)
            {
                this.DisplayName = displayName;
            }

            /// <summary>
            /// Gets the DisplayName.
            /// </summary>
            public string DisplayName
            {
                get;
                private set;
            }
        }

        [ConditionalAttribute("TRACE")]
        internal static void Trace(TraceSwitch traceSwitch, string message)
        {
            if (traceSwitch != null)
            {
                Debug.WriteLine(traceSwitch.DisplayName + ": " + message);
            }
        }
    }
}
