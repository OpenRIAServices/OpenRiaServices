using System;
using System.IO;
using System.Text;
using System.Runtime.Serialization.Json;
using System.ServiceModel.Dispatcher;
using System.Globalization;

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
            if (parameterType.IsValueType 
                && parameter == null
                && !TypeUtility.IsNullableType(parameterType))
            {
                throw new ArgumentNullException(nameof(parameter));
            }

            if (s_systemConverter.CanConvert(parameterType))
            {
                return s_systemConverter.ConvertValueToString(parameter, parameterType);
            }
            using (MemoryStream ms = new MemoryStream())
            {
                new DataContractJsonSerializer(parameterType, new DataContractJsonSerializerSettings()
                {
                     DateTimeFormat = new System.Runtime.Serialization.DateTimeFormat("o", CultureInfo.InvariantCulture)
                }).WriteObject(ms, parameter);

                byte[] result = ms.ToArray();
                string value = Encoding.UTF8.GetString(result, 0, result.Length);

                // TODO: JsonSerializerSettings? , and ensure it is correctly configured
                //string value = Newtonsoft.Json.JsonConvert.SerializeObject(parameter, parameterType, new Newtonsoft.Json.JsonSerializerSettings()
                //{
                //     DateFormatHandling = Newtonsoft.Json.DateFormatHandling.MicrosoftDateFormat
                //});


                // https://weblog.west-wind.com/posts/2009/Feb/05/Html-and-Uri-String-Encoding-without-SystemWeb
                return Uri.EscapeDataString(value);
            }
        }
    }
}
