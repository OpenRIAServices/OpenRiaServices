using System;
using System.ServiceModel.Dispatcher;

namespace OpenRiaServices.Client.DomainClients.Http
{
    /// <summary>
    /// This file is based on the OpenRiaServices.Client/Hosting.WebHttpQueryStringConverter 
    /// </summary>
    static class WebQueryStringConverter
    {
        static readonly QueryStringConverter s_systemConverter = new QueryStringConverter();

        /// <summary>
        /// Determines whether a parameter type can be represented in the query string.
        /// </summary>
        /// <param name="parameterType">The declared parameter type.</param>
        /// <returns>Whether the type or its nullable underlying type has a string conversion.</returns>
        public static bool CanConvert(Type parameterType)
        {
            return s_systemConverter.CanConvert(parameterType)
                || (TypeUtility.IsNullableType(parameterType)
                    && s_systemConverter.CanConvert(TypeUtility.GetNonNullableType(parameterType)));
        }

        /// <summary>
        /// Converts a supported, non-null parameter to its query string representation.
        /// </summary>
        /// <param name="parameter">The parameter value.</param>
        /// <param name="parameterType">The declared parameter type.</param>
        /// <returns>The string representation of the parameter.</returns>
        public static string ConvertValueToString(object parameter, Type parameterType)
        {
            System.Diagnostics.Debug.Assert(parameter != null, "parameter is checked before calling this method");

            if (s_systemConverter.CanConvert(parameterType))
            {
                return s_systemConverter.ConvertValueToString(parameter, parameterType);
            }

            return s_systemConverter.ConvertValueToString(parameter, TypeUtility.GetNonNullableType(parameterType));
        }
    }
}
