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
        // Specify datetime format so that the DateTimeKind can roundtrip, otherwise unspecified values are treated incorrect by server
        // https://github.com/OpenRIAServices/OpenRiaServices/issues/75
        const string DateTimeFormat = @"yyyy\-MM\-ddTHH\:mm\:ss.FFFFFFFK";
        private static readonly DataContractJsonSerializerSettings s_jsonSettings
            = new DataContractJsonSerializerSettings()
            {
                DateTimeFormat = new System.Runtime.Serialization.DateTimeFormat(DateTimeFormat, System.Globalization.CultureInfo.InvariantCulture)
            };

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

            if (parameter == "null")
                return null;

            // Nullable types have historically not been handled explicitly
            // so they habe been serialized to json
            // some of them (which are not represented as text) can be parsed directly
            // other like guid, timespan etc can be parsed if quotation marks are removed
            // but date related types cannot be parsed "normally" so skip anything treated as text
            // This lets change the wire format later on and serialize values the same as when non-nullable
            if (TypeUtility.IsNullableType(parameterType)
                && !parameter.StartsWith(@"%22", StringComparison.Ordinal))
            {
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
                try
                {
                    return new DataContractJsonSerializer(parameterType, s_jsonSettings)
                        .ReadObject(ms);
                }
                catch (Exception)
                {
                    // Fallback to old serialization format (default settings)
                    ms.Seek(0, SeekOrigin.Begin);
                    return new DataContractJsonSerializer(parameterType)
                        .ReadObject(ms);
                }
            }
        }

        public override string ConvertValueToString(object parameter, Type parameterType)
        {
            if (base.CanConvert(parameterType))
            {
                return base.ConvertValueToString(parameter, parameterType);
            }

            // Strings are handled above, so it should be save to return null here
            // without giving wrong result for string
            if (parameter == null)
                return "null";

            // For nullable types (which are not null), try to just serialize it as the underlying type
            if (TypeUtility.IsNullableType(parameterType))
            {
                parameterType = TypeUtility.GetNonNullableType(parameterType);
                if (base.CanConvert(parameterType))
                {
                    return base.ConvertValueToString(parameter, parameterType);
                }
            }

            using (MemoryStream ms = new MemoryStream())
            {
                new DataContractJsonSerializer(parameterType, s_jsonSettings)
                    .WriteObject(ms, parameter);

                if (ms.TryGetBuffer(out var buffer))
                {
                    string value = Encoding.UTF8.GetString(buffer.Array, index: buffer.Offset, count: buffer.Count);
                    return HttpUtility.UrlEncode(value);
                }
                else
                    return string.Empty;
            }
        }
    }
}
