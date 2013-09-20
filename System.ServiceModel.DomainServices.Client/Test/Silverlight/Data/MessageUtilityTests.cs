extern alias SSmDsClient;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.ServiceModel.Channels;
using System.Xml;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.ServiceModel.DomainServices.Client.Test
{
    [TestClass]
    public class MessageUtilityTests
    {
        [TestMethod]
        [Description("Tests if elements get added correctly to the body of the message")]
        public void AddElementsToBodyTest()
        {
            Message message = MessageUtilityTests.GetSampleMessage();
            List<KeyValuePair<string, string>> queryOptions = MessageUtilityTests.GetSampleQueryOptions();
            MessageUtility.AddMessageQueryOptions(ref message, queryOptions);
            MessageUtilityTests.CompareSampleMessage(message);
        }

        [TestMethod]
        [Description("Tests if the message created from MessageUtility obeys the message contract")]
        public void BodyWriter_UnitTests()
        {
            Message originalMessage = MessageUtilityTests.GetSampleMessage();
            List<KeyValuePair<string, string>> queryOptions = MessageUtilityTests.GetSampleQueryOptions();
            Message newMessage = originalMessage;
            MessageUtility.AddMessageQueryOptions(ref newMessage, queryOptions);

            // messages are not the same
            Assert.AreNotEqual(originalMessage, newMessage);

            // headers are the same
            UnitTestHelper.AssertListsAreEqual<MessageHeaderInfo>(originalMessage.Headers, newMessage.Headers,
                (s, h) =>
                {
                    return s + ", " + h.Name;
                });

            // properties are the same
            // workaround for a strange property bug
            originalMessage.Properties.AllowOutputBatching = false;
            UnitTestHelper.AssertListsAreEqual<KeyValuePair<string, object>>(originalMessage.Properties, newMessage.Properties,
                (s, p) =>
                {
                    return s + ", " + p.ToString();
                });

            // version matches
            Assert.AreEqual(originalMessage.Version, newMessage.Version);

            // initial state check
            Assert.AreEqual(MessageState.Created, originalMessage.State);
            Assert.AreEqual(originalMessage.State, newMessage.State);

            // write message
            MessageUtilityTests.CompareSampleMessage(newMessage);

            // state, the original message should have been closed due to the write
            Assert.AreEqual(MessageState.Closed, originalMessage.State);

            // make sure to clean up
            newMessage.Close();
        }

        private static Message GetSampleMessage()
        {
            string body =
@"<GetCustomers xmlns=""http://tempuri.org/"">
    <name>TestName</name>
  </GetCustomers>";

            XmlReader reader = XmlReader.Create(new StringReader(body));
            Message message = Message.CreateMessage(MessageVersion.Soap11, "GetCustomers", reader);
            return message;
        }

        private static List<KeyValuePair<string, string>> GetSampleQueryOptions()
        {
            List<KeyValuePair<string, string>> queryOptions = new List<KeyValuePair<string, string>>();
            queryOptions.Add(new KeyValuePair<string, string>("where", @"(it.City.StartsWith(""R"")&&(it.AddressID<400))"));
            queryOptions.Add(new KeyValuePair<string, string>("orderby", "it.AddressId"));
            queryOptions.Add(new KeyValuePair<string, string>("includeTotalCount", "true"));
            return queryOptions;
        }

        private static void CompareSampleMessage(Message message)
        {
            MemoryStream ms = new MemoryStream();
            XmlDictionaryWriter writer = XmlDictionaryWriter.CreateBinaryWriter(ms);
            message.WriteBodyContents(writer);
            writer.Flush();

            ms.Position = 0;
            XmlDictionaryReader xmlDictionaryReader = XmlDictionaryReader.CreateBinaryReader(ms, XmlDictionaryReaderQuotas.Max);
            XDocument xDoc = XDocument.Load(xmlDictionaryReader);

            string expectedMessageBody =
@"<MessageRoot>
  <QueryOptions>
    <QueryOption Name=""where"" Value=""(it.City.StartsWith(&quot;R&quot;)&amp;&amp;(it.AddressID&lt;400))""></QueryOption>
    <QueryOption Name=""orderby"" Value=""it.AddressId""></QueryOption>
    <QueryOption Name=""includeTotalCount"" Value=""true""></QueryOption>
  </QueryOptions>
  <GetCustomers xmlns=""http://tempuri.org/"">
    <name>TestName</name>
  </GetCustomers>
</MessageRoot>";

            Assert.AreEqual(expectedMessageBody, xDoc.ToString());
        }
    }
}
