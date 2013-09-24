namespace OpenRiaServices.DomainServices.Hosting.OData
{
    #region Namespace
    using System;
    using System.Data.Services.Providers;
    using System.Diagnostics;
    using System.Globalization;
    using System.Net;
    using System.ServiceModel.Dispatcher;
    using System.Text;
    using System.Xml;
    using System.Xml.Linq;
    #endregion

    /// <summary>
    /// This class converts a parameter in a query string to an object of the appropriate type. 
    /// It can also convert a parameter from an object to its query string representation. 
    /// </summary>
    internal class DomainDataServiceQueryStringConverter : QueryStringConverter
    {
        /// <summary>
        /// Determines whether the specified type can be converted to and from a string representation.
        /// </summary>
        /// <param name="type">The Type to convert.</param>
        /// <returns>true if the input type represents one of the primitive resource types supported by WCF Data Services.</returns>
        public override bool CanConvert(Type type)
        {
            return ResourceType.GetPrimitiveResourceType(type) != null;
        }

        /// <summary>
        /// Converts a query string parameter to the specified type.
        /// </summary>
        /// <param name="parameter">The string form of the parameter and value.</param>
        /// <param name="parameterType">The Type to convert the parameter to.</param>
        /// <returns>The converted parameter.</returns>
        public override object ConvertStringToValue(string parameter, Type parameterType)
        {
            object convertedValue;
            
            Type underlyingType = Nullable.GetUnderlyingType(parameterType);

            if (String.IsNullOrEmpty(parameter))
            {
                DomainDataServiceQueryStringConverter.CheckSyntaxValid(parameterType.IsClass || underlyingType != null);
                convertedValue = null;
            }
            else
            {
                // We choose to be a little more flexible than with keys and allow surrounding whitespace (which is never significant).
                parameter = parameter.Trim();

                Type targetType = underlyingType ?? parameterType;

                if (DomainDataServiceQueryStringConverter.IsKeyTypeQuoted(targetType))
                {
                    DomainDataServiceQueryStringConverter.CheckSyntaxValid(
                        DomainDataServiceQueryStringConverter.IsKeyValueQuoted(parameter));
                }

                DomainDataServiceQueryStringConverter.CheckSyntaxValid(
                    DomainDataServiceQueryStringConverter.TryKeyStringToPrimitive(
                        parameter, 
                        targetType, 
                        out convertedValue));
            }

            return convertedValue;
        }

        /// <summary>Converts a string to a primitive value.</summary>
        /// <param name="text">String text to convert.</param>
        /// <param name="targetType">Type to convert string to.</param>
        /// <param name="targetValue">After invocation, converted value.</param>
        /// <returns>true if the value was converted; false otherwise.</returns>
        private static bool TryKeyStringToPrimitive(string text, Type targetType, out object targetValue)
        {
            Debug.Assert(text != null, "text != null");
            Debug.Assert(targetType != null, "targetType != null");

            targetType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            byte[] byteArrayValue;
            bool binaryResult = DomainDataServiceQueryStringConverter.TryKeyStringToByteArray(text, out byteArrayValue);
            if (targetType == typeof(byte[]) || targetType == typeof(System.Data.Linq.Binary))
            {
                // The object cast is required because otherwise the compiler uses the implicit byte[]
                // to Binary conversion and always returns Binary.
                targetValue =
                    (byteArrayValue != null && targetType == typeof(System.Data.Linq.Binary)) ?
                    (object)new System.Data.Linq.Binary(byteArrayValue) : (object)byteArrayValue;
                return binaryResult;
            }
            else if (binaryResult)
            {
                string keyValue = Encoding.UTF8.GetString(byteArrayValue);
                return DomainDataServiceQueryStringConverter.TryKeyStringToPrimitive(keyValue, targetType, out targetValue);
            }
            // These have separate handlers for convenience - reuse them.
            else if (targetType == typeof(Guid))
            {
                Guid guidValue;
                bool result = DomainDataServiceQueryStringConverter.TryKeyStringToGuid(text, out guidValue);
                targetValue = guidValue;
                return result;
            }
            else if (targetType == typeof(DateTime))
            {
                DateTime dateTimeValue;
                bool result = DomainDataServiceQueryStringConverter.TryKeyStringToDateTime(text, out dateTimeValue);
                targetValue = dateTimeValue;
                return result;
            }

            bool quoted = DomainDataServiceQueryStringConverter.IsKeyTypeQuoted(targetType);
            if (quoted != DomainDataServiceQueryStringConverter.IsKeyValueQuoted(text))
            {
                targetValue = null;
                return false;
            }

            if (quoted)
            {
                Debug.Assert(
                    DomainDataServiceQueryStringConverter.IsKeyValueQuoted(text), 
                    "IsKeyValueQuoted(text) - otherwise caller didn't check this before");
                text = DomainDataServiceQueryStringConverter.RemoveQuotes(text);
            }

            try
            {
                if (typeof(String) == targetType)
                {
                    targetValue = text;
                }
                else if (typeof(Boolean) == targetType)
                {
                    targetValue = XmlConvert.ToBoolean(text);
                }
                else if (typeof(Byte) == targetType)
                {
                    targetValue = XmlConvert.ToByte(text);
                }
                else if (typeof(SByte) == targetType)
                {
                    targetValue = XmlConvert.ToSByte(text);
                }
                else if (typeof(Int16) == targetType)
                {
                    targetValue = XmlConvert.ToInt16(text);
                }
                else if (typeof(Int32) == targetType)
                {
                    targetValue = XmlConvert.ToInt32(text);
                }
                else if (typeof(Int64) == targetType)
                {
                    if (DomainDataServiceQueryStringConverter.TryRemoveLiteralSuffix(TypeUtils.XmlInt64LiteralSuffix, ref text))
                    {
                        targetValue = XmlConvert.ToInt64(text);
                    }
                    else
                    {
                        targetValue = default(Int64);
                        return false;
                    }
                }
                else if (typeof(Single) == targetType)
                {
                    if (DomainDataServiceQueryStringConverter.TryRemoveLiteralSuffix(TypeUtils.XmlSingleLiteralSuffix, ref text))
                    {
                        targetValue = XmlConvert.ToSingle(text);
                    }
                    else
                    {
                        targetValue = default(Single);
                        return false;
                    }
                }
                else if (typeof(Double) == targetType)
                {
                    DomainDataServiceQueryStringConverter.TryRemoveLiteralSuffix(TypeUtils.XmlDoubleLiteralSuffix, ref text);
                    targetValue = XmlConvert.ToDouble(text);
                }
                else if (typeof(Decimal) == targetType)
                {
                    if (DomainDataServiceQueryStringConverter.TryRemoveLiteralSuffix(TypeUtils.XmlDecimalLiteralSuffix, ref text))
                    {
                        try
                        {
                            targetValue = XmlConvert.ToDecimal(text);
                        }
                        catch (FormatException)
                        {
                            // we need to support exponential format for decimals since we used to support them in V1
                            decimal result;
                            if (Decimal.TryParse(text, NumberStyles.Float, NumberFormatInfo.InvariantInfo, out result))
                            {
                                targetValue = result;
                            }
                            else
                            {
                                targetValue = default(Decimal);
                                return false;
                            }
                        }
                    }
                    else
                    {
                        targetValue = default(Decimal);
                        return false;
                    }
                }
                else
                {
                    Debug.Assert(typeof(XElement) == targetType, "XElement == " + targetType);
                    targetValue = XElement.Parse(text, LoadOptions.PreserveWhitespace);
                }

                return true;
            }
            catch (FormatException)
            {
                targetValue = null;
                return false;
            }
        }

        /// <summary>Converts a string to a byte[] value.</summary>
        /// <param name="text">String text to convert.</param>
        /// <param name="targetValue">After invocation, converted value.</param>
        /// <returns>true if the value was converted; false otherwise.</returns>
        private static bool TryKeyStringToByteArray(string text, out byte[] targetValue)
        {
            Debug.Assert(text != null, "text != null");

            if (!DomainDataServiceQueryStringConverter.TryRemoveLiteralPrefix(TypeUtils.LiteralPrefixBinary, ref text) &&
                !DomainDataServiceQueryStringConverter.TryRemoveLiteralPrefix(TypeUtils.XmlBinaryPrefix, ref text))
            {
                targetValue = null;
                return false;
            }

            if (!DomainDataServiceQueryStringConverter.TryRemoveQuotes(ref text))
            {
                targetValue = null;
                return false;
            }

            if ((text.Length % 2) != 0)
            {
                targetValue = null;
                return false;
            }

            byte[] result = new byte[text.Length / 2];
            int resultIndex = 0;
            int textIndex = 0;
            while (resultIndex < result.Length)
            {
                char ch0 = text[textIndex];
                char ch1 = text[textIndex + 1];
                if (!DomainDataServiceQueryStringConverter.IsCharHexDigit(ch0) ||
                    !DomainDataServiceQueryStringConverter.IsCharHexDigit(ch1))
                {
                    targetValue = null;
                    return false;
                }

                result[resultIndex] = (byte)((byte)(DomainDataServiceQueryStringConverter.HexCharToNibble(ch0) << 4) +
                                                    DomainDataServiceQueryStringConverter.HexCharToNibble(ch1));
                textIndex += 2;
                resultIndex++;
            }

            targetValue = result;
            return true;
        }

        /// <summary>Converts a string to a GUID value.</summary>
        /// <param name="text">String text to convert.</param>
        /// <param name="targetValue">After invocation, converted value.</param>
        /// <returns>true if the value was converted; false otherwise.</returns>
        private static bool TryKeyStringToGuid(string text, out Guid targetValue)
        {
            if (!DomainDataServiceQueryStringConverter.TryRemoveLiteralPrefix(TypeUtils.LiteralPrefixGuid, ref text))
            {
                targetValue = default(Guid);
                return false;
            }

            if (!DomainDataServiceQueryStringConverter.TryRemoveQuotes(ref text))
            {
                targetValue = default(Guid);
                return false;
            }

            try
            {
                targetValue = XmlConvert.ToGuid(text);
                return true;
            }
            catch (FormatException)
            {
                targetValue = default(Guid);
                return false;
            }
        }

        /// <summary>Converts a string to a DateTime value.</summary>
        /// <param name="text">String text to convert.</param>
        /// <param name="targetValue">After invocation, converted value.</param>
        /// <returns>true if the value was converted; false otherwise.</returns>
        private static bool TryKeyStringToDateTime(string text, out DateTime targetValue)
        {
            if (!DomainDataServiceQueryStringConverter.TryRemoveLiteralPrefix(TypeUtils.LiteralPrefixDateTime, ref text))
            {
                targetValue = default(DateTime);
                return false;
            }

            if (!DomainDataServiceQueryStringConverter.TryRemoveQuotes(ref text))
            {
                targetValue = default(DateTime);
                return false;
            }

            try
            {
                targetValue = XmlConvert.ToDateTime(text, XmlDateTimeSerializationMode.RoundtripKind);
                return true;
            }
            catch (FormatException)
            {
                targetValue = default(DateTime);
                return false;
            }
        }

        /// <summary>
        /// Tries to remove a literal <paramref name="prefix"/> from the specified <paramref name="text"/>.
        /// </summary>
        /// <param name="prefix">Prefix to remove; one-letter prefixes are case-sensitive, others insensitive.</param>
        /// <param name="text">Text to attempt to remove prefix from.</param>
        /// <returns>true if the prefix was found and removed; false otherwise.</returns>
        private static bool TryRemoveLiteralPrefix(string prefix, ref string text)
        {
            Debug.Assert(prefix != null, "prefix != null");
            if (text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                text = text.Remove(0, prefix.Length);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Check and strip the input <paramref name="text"/> for literal <paramref name="suffix"/>
        /// </summary>
        /// <param name="suffix">The suffix value</param>
        /// <param name="text">The string to check</param>
        /// <returns>True if <paramref name="text"/> has been striped of the <paramref name="suffix"/>.</returns>
        private static bool TryRemoveLiteralSuffix(string suffix, ref string text)
        {
            Debug.Assert(text != null, "text != null");
            Debug.Assert(suffix != null, "suffix != null");

            text = text.Trim(TypeUtils.XmlWhitespaceChars);
            if (text.Length <= suffix.Length || !text.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            else
            {
                text = text.Substring(0, text.Length - suffix.Length);
                return true;
            }
        }

        /// <summary>
        /// Determines whether the values for the specified types should be 
        /// quoted in URI keys.
        /// </summary>
        /// <param name='type'>Type to check.</param>
        /// <returns>
        /// true if values of <paramref name='type' /> require quotes; false otherwise.
        /// </returns>
        private static bool IsKeyTypeQuoted(Type type)
        {
            Debug.Assert(type != null, "type != null");
            return type == typeof(XElement) || type == typeof(string);
        }

        /// <summary>Checks whether the specified text is a correctly formatted quoted value.</summary>
        /// <param name='text'>Text to check.</param>
        /// <returns>true if the text is correctly formatted, false otherwise.</returns>
        private static bool IsKeyValueQuoted(string text)
        {
            Debug.Assert(text != null, "text != null");
            if (text.Length < 2 || text[0] != '\'' || text[text.Length - 1] != '\'')
            {
                return false;
            }
            else
            {
                int startIndex = 1;
                while (startIndex < text.Length - 1)
                {
                    int match = text.IndexOf('\'', startIndex, text.Length - startIndex - 1);
                    if (match == -1)
                    {
                        break;
                    }
                    else if (match == text.Length - 2 || text[match + 1] != '\'')
                    {
                        return false;
                    }
                    else
                    {
                        startIndex = match + 2;
                    }
                }

                return true;
            }
        }

        /// <summary>Removes quotes from the single-quotes text.</summary>
        /// <param name="text">Text to remove quotes from.</param>
        /// <returns>The specified <paramref name="text"/> with single quotes removed.</returns>
        private static string RemoveQuotes(string text)
        {
            Debug.Assert(!String.IsNullOrEmpty(text), "!String.IsNullOrEmpty(text)");

            char quote = text[0];
            Debug.Assert(quote == '\'', "quote == '\''");
            Debug.Assert(text[text.Length - 1] == '\'', "text should end with '\''.");

            string s = text.Substring(1, text.Length - 2);
            int start = 0;
            while (true)
            {
                int i = s.IndexOf(quote, start);
                if (i < 0)
                {
                    break;
                }

                Debug.Assert(i + 1 < s.Length && s[i + 1] == '\'', @"Each single quote should be propertly escaped with double single quotes.");
                s = s.Remove(i, 1);
                start = i + 1;
            }

            return s;
        }

        /// <summary>Removes quotes from the single-quotes text.</summary>
        /// <param name="text">Text to remove quotes from.</param>
        /// <returns>Whether quotes were successfully removed.</returns>
        private static bool TryRemoveQuotes(ref string text)
        {
            if (text.Length < 2)
            {
                return false;
            }

            char quote = text[0];
            if (quote != '\'' || text[text.Length - 1] != quote)
            {
                return false;
            }

            string s = text.Substring(1, text.Length - 2);
            int start = 0;
            while (true)
            {
                int i = s.IndexOf(quote, start);
                if (i < 0)
                {
                    break;
                }

                s = s.Remove(i, 1);
                if (s.Length < i + 1 || s[i] != quote)
                {
                    return false;
                }

                start = i + 1;
            }

            text = s;
            return true;
        }

        /// <summary>Checks the specific value for syntax validity.</summary>
        /// <param name="valid">Whether syntax is valid.</param>
        /// <remarks>This helper method is used to keep syntax check code more terse.</remarks>
        private static void CheckSyntaxValid(bool valid)
        {
            if (!valid)
            {
                throw new DomainDataServiceException((int)HttpStatusCode.BadRequest, Resource.DomainDataService_RequestParameter_SyntaxError);
            }
        }

        /// <summary>Returns the 4 bits that correspond to the specified character.</summary>
        /// <param name="c">Character in the 0-F range to be converted.</param>
        /// <returns>The 4 bits that correspond to the specified character.</returns>
        /// <exception cref="FormatException">Thrown when 'c' is not in the '0'-'9','a'-'f' range.</exception>
        private static byte HexCharToNibble(char c)
        {
            Debug.Assert(DomainDataServiceQueryStringConverter.IsCharHexDigit(c), "DomainDataServiceQueryStringConverter.IsCharHexDigit(c)");
            switch (c)
            {
                case '0':
                    return 0;
                case '1':
                    return 1;
                case '2':
                    return 2;
                case '3':
                    return 3;
                case '4':
                    return 4;
                case '5':
                    return 5;
                case '6':
                    return 6;
                case '7':
                    return 7;
                case '8':
                    return 8;
                case '9':
                    return 9;
                case 'a':
                case 'A':
                    return 10;
                case 'b':
                case 'B':
                    return 11;
                case 'c':
                case 'C':
                    return 12;
                case 'd':
                case 'D':
                    return 13;
                case 'e':
                case 'E':
                    return 14;
                case 'f':
                case 'F':
                    return 15;
                default:
                    throw new InvalidOperationException();
            }
        }

        /// <summary>Determines whether the specified character is a valid hexadecimal digit.</summary>
        /// <param name="c">Character to check.</param>
        /// <returns>true if <paramref name="c"/> is a valid hex digit; false otherwise.</returns>
        private static bool IsCharHexDigit(char c)
        {
            return (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
        }
    }
}
