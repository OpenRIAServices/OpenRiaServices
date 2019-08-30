using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Resx = OpenRiaServices.DomainServices.Hosting.EndpointResource;

namespace OpenRiaServices.DomainServices.Hosting
{
    /// <summary>
    /// An implementation of <see cref="System.Diagnostics.TraceListener"/> that collects WCF traces in memory. The class is intended to be used in 
    /// conjunction with <see cref="TracingDomainServiceEndpointFactory"/> to expose WCF RIA trace messages as an ATOM feed, an XML document or an HTML document. 
    /// This class is not intended to be used directly from code. It can be referenced from the system.diagnostics section of the configuration file.
    /// </summary>
    public class InMemoryTraceListener : TraceListener
    {
        static int maxEntries = 200;
        static ConcurrentQueue<XElement> entries = new ConcurrentQueue<XElement>();
        static string currentEntry;

        internal static int MaxEntries 
        {
            get 
            {
                return maxEntries;
            }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), Resx.MaxEntriesMustBePositiveInteger);
                }
                maxEntries = value;
            }
        }

        /// <summary>
        /// This method is not intended for use from application code.
        /// </summary>
        public InMemoryTraceListener() : base() 
        {
        }

        /// <summary>
        /// This method is not intended for use from application code.
        /// </summary>
        /// <param name="name">TraceListener name</param>
        public InMemoryTraceListener(string name) : base(name) 
        {
        }

        /// <summary>
        /// This method is not intended for use from application code.
        /// </summary>
        /// <param name="message">Message to trace.</param>
        public override void Write(string message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            currentEntry = currentEntry == null ? message : currentEntry + message;
        }

        /// <summary>
        /// This method is not intended for use from application code.
        /// </summary>
        /// <param name="message">Message to trace.</param>
        public override void WriteLine(string message)
        {
            this.Write(message);

            Match wcfTrace = Regex.Match(currentEntry, "System.ServiceModel [^:]+: (\\d+) : ", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            if (wcfTrace.Success)
            {
                int traceRecordEnd = currentEntry.IndexOf(@"</TraceRecord>", StringComparison.Ordinal);
                if (traceRecordEnd > 0)
                {
                    int traceRecordStart = wcfTrace.Index + wcfTrace.Groups[0].Length;
                    int traceRecordLength = traceRecordEnd + 14 - traceRecordStart;
                    string xml = string.Format(CultureInfo.InvariantCulture, @"<Trace><Timestamp>{0}</Timestamp><Code>{1}</Code>{2}</Trace>",
                        DateTimeOffset.Now.UtcTicks, wcfTrace.Groups[1], 
                        currentEntry.Substring(traceRecordStart, traceRecordLength));
                    entries.Enqueue(XElement.Parse(xml));
                    currentEntry = currentEntry.Substring(traceRecordStart + traceRecordLength);
                    XElement trash;
                    while (entries.Count > maxEntries)
                    {
                        entries.TryDequeue(out trash);
                    }
                }
            }
        }

        internal static XElement[] GetEntries()
        {
            return entries.Reverse().ToArray();
        }

        /// <summary>
        /// Provide unit tests with a way to reset the trace entries
        /// </summary>
        internal static void Clear()
        {
            entries = new ConcurrentQueue<XElement>();
        }
    }
}
