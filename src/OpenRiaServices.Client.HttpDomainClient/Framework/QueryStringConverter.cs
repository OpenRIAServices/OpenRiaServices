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
        // Specify datetime format so that the DateTimeKind can roundtrip, otherwise unspecified values are treated incorrect by server
        // https://github.com/OpenRIAServices/OpenRiaServices/issues/75
        const string DateTimeFormat = @"yyyy\-MM\-ddTHH\:mm\:ss.FFFFFFFK";
        private static readonly DataContractJsonSerializerSettings s_jsonSettings
            = new DataContractJsonSerializerSettings()
            {
                DateTimeFormat = new System.Runtime.Serialization.DateTimeFormat(DateTimeFormat, System.Globalization.CultureInfo.InvariantCulture)
            };

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
                new DataContractJsonSerializer(parameterType, s_jsonSettings)
                    .WriteObject(ms, parameter);

                if (ms.TryGetBuffer(out var buffer))
                {
                    string value = Encoding.UTF8.GetString(buffer.Array, index: buffer.Offset, count: buffer.Count);
                    return Uri.EscapeDataString(value);
                }
                else
                    return string.Empty;
            }
        }
    }
}
