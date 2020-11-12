using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel.Channels;
using System.Xml;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Hosting.Wcf;

namespace OpenRiaServices.Hosting.Test.Data
{
    [TestClass]
    public class MessageUtilityTest
    {
        [TestMethod]
        [Description("Tests if elements are retrieved correctly from the body of the message with service query options")]
        public void GetElementsFromBodyTest_WithServiceQuery()
        {
            Message originalMessage;
            Message message;
            originalMessage = message = GetSampleMessage(true);
            ServiceQuery serviceQuery = MessageUtility.GetServiceQuery(ref message);

            Assert.IsNotNull(serviceQuery);
            Assert.AreEqual(2, serviceQuery.QueryParts.Count());
            ServiceQueryPart[] parts = serviceQuery.QueryParts.ToArray();
            Assert.AreEqual(parts[0].ToString(), @"where=(it.City.StartsWith(""R"")&&(it.AddressID<400))");
            Assert.AreEqual(parts[1].ToString(), "orderby=it.AddressId");
            Assert.AreEqual(true, serviceQuery.IncludeTotalCount);
            Assert.AreEqual(2, serviceQuery.QueryParts.Count());

            XmlDictionaryReader reader = MessageUtilityTest.CreateReaderFromMessage(message);
            MessageUtilityTest.CompareSampleMessage(reader);

            // cleanup
            reader.Close();
            originalMessage.Close();
        }
        
        [TestMethod]
        [Description("Tests if elements are retrieved correctly from the body of the message without service query options")]
        public void GetElementsFromBodyTest_NoServiceQuery()
        {
            Message originalMessage;
            Message message;
            originalMessage = message = GetSampleMessage(false);
            ServiceQuery serviceQuery = MessageUtility.GetServiceQuery(ref message);

            // no service query
            Assert.AreEqual(null, serviceQuery, "service query not null");

            // the message did not change, it should compare
            XmlDictionaryReader reader = MessageUtilityTest.CreateReaderFromMessage(message);
            MessageUtilityTest.CompareSampleMessage(reader);

            // cleanup
            reader.Close();
            originalMessage.Close();
        }

        private static Message GetSampleMessage(bool withServiceQuery)
        {
            string body =
@"<GetCustomers xmlns=""http://tempuri.org/"">
  <name>TestName</name>
</GetCustomers>";

            if (withServiceQuery)
            {
                body =
@"<MessageRoot>
  <QueryOptions>
    <QueryOption Name=""where"" Value=""(it.City.StartsWith(&quot;R&quot;)&amp;&amp;(it.AddressID&lt;400))""></QueryOption>
    <QueryOption Name=""orderby"" Value=""it.AddressId""></QueryOption>
    <QueryOption Name=""includeTotalCount"" Value=""true""></QueryOption>
  </QueryOptions>" + body +
@"</MessageRoot>";
            }

            XmlReader reader = XmlReader.Create(new StringReader(body), new XmlReaderSettings() { IgnoreWhitespace = true });
            Message message = Message.CreateMessage(MessageVersion.None, "GetCustomers", reader);
            return message;
        }

        private static XmlDictionaryReader CreateReaderFromMessage(Message message)
        {
            MemoryStream ms = new MemoryStream();
            byte[] buffer;
            using (message)
            {
                using (XmlDictionaryWriter writer = XmlDictionaryWriter.CreateBinaryWriter(ms))
                {
                    message.WriteBodyContents(writer);
                    writer.Flush();

                    buffer = ms.ToArray();
#if DEBUG
                    // Below will output text to console including control characters which sonarqube cannot handle
                    foreach (byte b in buffer)
                    {
                        Console.Write((char)b);
                    }
#endif
                }
            }

            XmlDictionaryReader xmlDictionaryReader = XmlDictionaryReader.CreateBinaryReader(buffer, XmlDictionaryReaderQuotas.Max);
            return xmlDictionaryReader;
        }

        private static void CompareSampleMessage(XmlDictionaryReader xmlDictionaryReader)
        {
            XDocument xDoc = XDocument.Load(xmlDictionaryReader);

            string expectedMessageBody =
@"<GetCustomers xmlns=""http://tempuri.org/"">
  <name>TestName</name>
</GetCustomers>";

            Assert.AreEqual(xDoc.ToString(), expectedMessageBody);
        }
    }
}
