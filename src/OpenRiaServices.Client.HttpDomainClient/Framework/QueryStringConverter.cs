using System;
using System.IO;
using System.Text;
using System.Runtime.Serialization.Json;
using System.ServiceModel.Dispatcher;

namespace OpenRiaServices.Client.HttpDomainClient
{
    /// <summary>
    /// This file is based on the OpenRiaServices.Client/Hosting.WebHttpQueryStringConverter 
    /// </summary>
    static class WebQueryStringConverter
    {
        static readonly QueryStringConverter s_systemConverter = new QueryStringConverter();
        
        public static string ConvertValueToString(object parameter, Type parameterType)
        {
            if (parameterType == null)
            {
                throw new ArgumentNullException("parameterType");
            }
            if (parameterType.IsValueType 
                && parameter == null
                && !TypeUtility.IsNullableType(parameterType))
            {
                throw new ArgumentNullException("parameter");
            }

            if (s_systemConverter.CanConvert(parameterType))
            {
                return s_systemConverter.ConvertValueToString(parameter, parameterType);
            }
            using (MemoryStream ms = new MemoryStream())
            {
                new DataContractJsonSerializer(parameterType).WriteObject(ms, parameter);
                byte[] result = ms.ToArray();
                string value = Encoding.UTF8.GetString(result, 0, result.Length);

                // TODO: JsonSerializerSettings , and ensure it is correctly configured
                //string value = Newtonsoft.Json.JsonConvert.SerializeObject(parameter, parameterType, new Newtonsoft.Json.JsonSerializerSettings()
                //{
                //     DateFormatHandling = Newtonsoft.Json.DateFormatHandling.MicrosoftDateFormat
                //});

                return Uri.EscapeDataString(value);
            }
        }
    }
}
