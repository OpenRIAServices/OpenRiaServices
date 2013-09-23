using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.ServiceModel;
using OpenRiaServices;
using System.ServiceModel.Channels;
using System.ServiceModel.Syndication;
using System.ServiceModel.Web;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using Resx = OpenRiaServices.DomainServices.Hosting.EndpointResource;

namespace OpenRiaServices.DomainServices.Hosting
{
    /// <summary>
    /// A class implementing a WCF REST service that exposes WCF traces collected by <see cref="InMemoryTraceListener"/> as 
    /// an ATOM feed or an XML document. This class is not intended for direct use by application code. In order to enable the functionality
    /// for a WCF RIA service, please use <see cref="TracingDomainServiceEndpointFactory"/>.
    /// </summary>
    [ServiceContract]
    public class WcfTraceService
    {
        static WcfTraceService instance;

        WcfTraceService() { }

        internal static WcfTraceService Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new WcfTraceService();
                }
                return instance;
            }
        }

        /// <summary>
        /// A WCF REST service operation that returns WCF RIA service traces in the requested format. This method is not intended
        /// for direct use from application code. See <see cref="TracingDomainServiceEndpointFactory"/> to enable the functionality.
        /// </summary>
        /// <param name="format">Requested response format. Allowed values are: 'atom' (default), 'xml', and 'html'.</param>
        /// <returns>WCF traces from all services running in the application domain in the requested format (ATOM, XML, or HTML).</returns>
        [WebGet(UriTemplate = "?format={format}")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "reserving right to make this instance method")]
        public Message GetTrace(string format)
        {
            if ("xml".Equals(format, StringComparison.OrdinalIgnoreCase))
            {
                return WebOperationContext.Current.CreateStreamResponse(CreateTraceXml(), "text/xml");
            }
            else if ("html".Equals(format, StringComparison.OrdinalIgnoreCase))
            {
                return WebOperationContext.Current.CreateStreamResponse(CreateTraceHtml(), "text/html");
            }
            else if (string.IsNullOrEmpty(format) || "atom".Equals(format, StringComparison.OrdinalIgnoreCase))
            {
                return WebOperationContext.Current.CreateAtom10Response(CreateTraceSyndicationFeed());
            }
            else
            {
                throw new InvalidOperationException(Resx.InvalidTraceFormat);
            }
        }

        static Stream CreateTraceXml()
        {
            MemoryStream result = null;
            MemoryStream ms = null;
            try
            {
                ms = new MemoryStream();
                using (XmlWriter writer = XmlWriter.Create(ms, new XmlWriterSettings { OmitXmlDeclaration = true }))
                {
                    writer.WriteStartElement("Traces");
                    foreach (XElement element in InMemoryTraceListener.GetEntries())
                    {
                        element.WriteTo(writer);
                    }
                    writer.WriteEndElement();
                }
                ms.Seek(0, SeekOrigin.Begin);

                result = ms;
                ms = null;
            }
            finally
            {
                if (ms != null)
                {
                    ms.Dispose();
                }
            }
            return result;
        }

        static SyndicationFeed CreateTraceSyndicationFeed()
        {
            IEnumerable<SyndicationItem> items = from entry in InMemoryTraceListener.GetEntries()
                                                 select CreateTraceSyndicationItem(entry);
            SyndicationFeed feed = new SyndicationFeed(items);
            feed.Title = new TextSyndicationContent(Resx.WCFTraceFeedTitle);
            return feed;
        }

        static SyndicationItem CreateTraceSyndicationItem(XElement entry)
        {
            SyndicationItem result = new SyndicationItem();
            XElement traceRecord = entry.Descendants().First(e => e.Name.LocalName == "TraceRecord");
            result.Title = new TextSyndicationContent(
                traceRecord.Attributes().First(a => a.Name.LocalName == "Severity").Value + ": " +
                traceRecord.Descendants().First(e => e.Name.LocalName == "Description").Value);
            result.PublishDate = new DateTimeOffset(long.Parse(entry.Descendants().First(e => e.Name.LocalName == "Timestamp").Value, CultureInfo.InvariantCulture), TimeSpan.Zero);
            result.LastUpdatedTime = result.PublishDate;
            result.Summary = new TextSyndicationContent(HtmlEncode(traceRecord.ToString(), true), TextSyndicationContentKind.Html);
            result.Content = SyndicationContent.CreateXmlContent(traceRecord);
            return result;
        }

        static Stream CreateTraceHtml()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(string.Format(CultureInfo.InvariantCulture, "<html><head><title>{0}</title></head><body style=\"font-family: sans-serif\"><h1>{0}</h1>", Resx.WCFTraceFeedTitle));
            IEnumerable<string> items = from entry in InMemoryTraceListener.GetEntries()
                                        select CreateTraceHtmlEntry(entry);
            foreach (string item in items)
            {
                sb.Append(item);
            }
            sb.Append("</body></html>");
            return new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString()));
        }

        static string CreateTraceHtmlEntry(XElement entry)
        {
            XElement traceRecord = entry.Descendants().First(e => e.Name.LocalName == "TraceRecord");
            string result = string.Format(CultureInfo.InvariantCulture, "<h3>{0}</h3><p>{1}</p><p>{2}</p>",
                HtmlEncode(traceRecord.Attributes().First(a => a.Name.LocalName == "Severity").Value + ": " +
                    traceRecord.Descendants().First(e => e.Name.LocalName == "Description").Value, false),
                new DateTimeOffset(long.Parse(entry.Descendants().First(e => e.Name.LocalName == "Timestamp").Value, CultureInfo.InvariantCulture), TimeSpan.Zero).ToLocalTime().ToString(),
                HtmlEncode(traceRecord.ToString(), true));
            return result;
        }

        static string HtmlEncode(string s, bool preformatted)
        {
            s = HttpUtility.HtmlEncode(s);
            if (preformatted)
            {
                s = s.Replace("\n", "<br>").Replace(" ", "&nbsp;");
            }
            return s;
        }
    }
}
