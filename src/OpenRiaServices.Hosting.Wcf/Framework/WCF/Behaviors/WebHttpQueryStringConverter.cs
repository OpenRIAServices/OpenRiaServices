using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.ServiceModel.Dispatcher;
using System.Text;
#if SILVERLIGHT
using System.Windows.Browser;
#else
using System.Web;
#endif

#if SERVERFX
namespace OpenRiaServices.Hosting.Wcf.Behaviors
#else
namespace OpenRiaServices.Client.Web.Behaviors
#endif
{
    internal class WebHttpQueryStringConverter : QueryStringConverter
    {
        public override bool CanConvert(Type type)
        {
            // Allow everything.
            return true;
        }

        public override object ConvertStringToValue(string parameter, Type parameterType)
        {
            if (parameter == null)
            {
                return null;
            }

            if (base.CanConvert(parameterType))
            {
                return base.ConvertStringToValue(parameter, parameterType);
            }

            // Nullable types have historically not been handled explicitly
            // so they habe been serialized to json
            // some of them (which are not represented as text) can be parsed directly
            // other like guid, timespan etc can be parsed if quotation marks are removed
            // but date related types cannot be parsed "normally" so skip anything treated as text
            // This lets change the wire format later on and serialize values the same as when non-nullable
            if (TypeUtility.IsNullableType(parameterType)
                && !parameter.StartsWith(@"%22", StringComparison.Ordinal))
            {
                if (parameter == "null")
                    return null;

                var actualType = parameterType.GetGenericArguments()[0];
                if (base.CanConvert(actualType))
                {
                    try
                    {
                        return base.ConvertStringToValue(parameter, actualType);
                    }
                    catch (FormatException)
                    {
                        // fallback to json serializer in case of error
                    }
                }
            }

            parameter = HttpUtility.UrlDecode(parameter);
            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(parameter)))
            {
                return new DataContractJsonSerializer(parameterType).ReadObject(ms);
            }
        }

        public override string ConvertValueToString(object parameter, Type parameterType)
        {
            if (base.CanConvert(parameterType))
            {
                return base.ConvertValueToString(parameter, parameterType);
            }
            using (MemoryStream ms = new MemoryStream())
            {
                new DataContractJsonSerializer(parameterType)
                    .WriteObject(ms, parameter);
                byte[] result = ms.ToArray();
                string value = Encoding.UTF8.GetString(result, 0, result.Length);
                return HttpUtility.UrlEncode(value);
            }
        }
    }
}
