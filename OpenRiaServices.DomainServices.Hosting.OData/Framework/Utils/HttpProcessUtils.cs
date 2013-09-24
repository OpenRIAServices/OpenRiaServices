namespace OpenRiaServices.DomainServices.Hosting.OData
{
    #region Namespaces
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Net;
    using System.Text;
    #endregion

    /// <summary>
    /// Types of requests supported by OData endpoint.
    /// </summary>
    internal enum RequestKind
    {
        /// <summary>
        /// Unsupported request.
        /// </summary>
        Unsupported,

        /// <summary>
        /// Service document request.
        /// </summary>
        ServiceDocument,

        /// <summary>
        /// Metadata document request.
        /// </summary>
        MetadataDocument,

        /// <summary>
        /// Resource set request.
        /// </summary>
        ResourceSet,

        /// <summary>
        /// Service operation request.
        /// </summary>
        ServiceOperation
    }

    /// <summary>Provides helper methods for processing HTTP requests.</summary>
    internal static class HttpProcessUtils
    {
        /// <summary>MIME type for ATOM Service Documents (http://tools.ietf.org/html/rfc5023#section-8).</summary>
        private const string MimeApplicationAtomService = "application/atomsvc+xml";

        /// <summary>MIME type for ATOM bodies (http://www.iana.org/assignments/media-types/application/).</summary>
        private const string MimeApplicationAtom = "application/atom+xml";

        /// <summary>MIME type for XML bodies.</summary>
        private const string MimeApplicationXml = "application/xml";

        /// <summary>MIME type for XML bodies (deprecated).</summary>
        private const string MimeTextXml = "text/xml";

        /// <summary>MIME type for JSON bodies (http://www.iana.org/assignments/media-types/application/).</summary>
        private const string MimeApplicationJson = "application/json";

        /// <summary>'q' - HTTP q-value parameter name.</summary>
        private const string HttpQValueParameter = "q";

        /// <summary>
        /// Mime types for service documents.
        /// </summary>
        private static readonly string[] ServiceDocumentMimeTypes = new string[] 
            { 
                HttpProcessUtils.MimeApplicationAtomService, 
                HttpProcessUtils.MimeApplicationJson, 
                HttpProcessUtils.MimeApplicationXml 
            };

        /// <summary>
        /// Mime types for metadata documents.
        /// </summary>
        private static readonly string[] MetadataDocumentMimeTypes = new string[] 
            { 
                HttpProcessUtils.MimeApplicationXml,
                HttpProcessUtils.MimeApplicationJson
            };

        /// <summary>
        /// Mime types for resource set response.
        /// </summary>
        private static readonly string[] ResourceSetMimeTypes = new string[]
            {
                HttpProcessUtils.MimeApplicationAtom,
                HttpProcessUtils.MimeApplicationJson
            };

        /// <summary>
        /// Mime types for service operation response.
        /// </summary>
        private static readonly string[] ServiceOperationMimeTypes = new string[]
            {
                HttpProcessUtils.MimeApplicationAtom,
                HttpProcessUtils.MimeApplicationXml,
                HttpProcessUtils.MimeTextXml,
                HttpProcessUtils.MimeApplicationJson
            };

        /// <summary>Disallows requests that would like the response in json format.</summary>
        /// <param name="requestKind">Type of request.</param>
        /// <param name="acceptHeader">Accept header value.</param>
        /// <returns>True if request is accepting json response.</returns>
        internal static bool IsJsonRequest(RequestKind requestKind, string acceptHeader)
        {
            string mimeType = null;
            switch (requestKind)
            {
                case RequestKind.ServiceDocument:
                    mimeType = HttpProcessUtils.SelectRequiredMimeType(
                        acceptHeader,
                        HttpProcessUtils.ServiceDocumentMimeTypes,
                        HttpProcessUtils.MimeApplicationXml);
                    break;
                case RequestKind.MetadataDocument:
                    mimeType = HttpProcessUtils.SelectRequiredMimeType(
                        acceptHeader,
                        HttpProcessUtils.MetadataDocumentMimeTypes,
                        HttpProcessUtils.MimeApplicationXml);
                    break;
                case RequestKind.ResourceSet:
                    mimeType = HttpProcessUtils.SelectRequiredMimeType(
                        acceptHeader,
                        HttpProcessUtils.ResourceSetMimeTypes,
                        HttpProcessUtils.MimeApplicationAtom);
                    break;
                case RequestKind.ServiceOperation:
                    mimeType = HttpProcessUtils.SelectRequiredMimeType(
                        acceptHeader,
                        HttpProcessUtils.ServiceOperationMimeTypes,
                        HttpProcessUtils.MimeApplicationXml);
                    break;
                default:
                    Debug.Assert(false, "Must never receive any other kind of request.");
                    break;
            }

            return HttpProcessUtils.CompareMimeType(mimeType, HttpProcessUtils.MimeApplicationJson);
        }

        /// <summary>Gets the appropriate MIME type for the request, throwing if there is none.</summary>
        /// <param name='acceptTypesText'>Text as it appears in an HTTP Accepts header.</param>
        /// <param name='exactContentType'>Preferred content type to match if an exact media type is given - this is in descending order of preference.</param>
        /// <param name='inexactContentType'>Preferred fallback content type for inexact matches.</param>
        /// <returns>One of exactContentType or inexactContentType.</returns>
        private static string SelectRequiredMimeType(
            string acceptTypesText,
            string[] exactContentType,
            string inexactContentType)
        {
            Debug.Assert(exactContentType != null && exactContentType.Length != 0, "exactContentType != null && exactContentType.Length != 0");
            Debug.Assert(inexactContentType != null, "inexactContentType != null");

            string selectedContentType = null;
            int selectedMatchingParts = -1;
            int selectedQualityValue = 0;
            bool acceptable = false;
            bool acceptTypesEmpty = true;
            bool foundExactMatch = false;

            if (!String.IsNullOrEmpty(acceptTypesText))
            {
                IEnumerable<MediaType> acceptTypes = MimeTypesFromAcceptHeader(acceptTypesText);
                foreach (MediaType acceptType in acceptTypes)
                {
                    acceptTypesEmpty = false;
                    for (int i = 0; i < exactContentType.Length; i++)
                    {
                        if (HttpProcessUtils.CompareMimeType(acceptType.MimeType, exactContentType[i]))
                        {
                            selectedContentType = exactContentType[i];
                            selectedQualityValue = acceptType.SelectQualityValue();
                            acceptable = selectedQualityValue != 0;
                            foundExactMatch = true;
                            break;
                        }
                    }

                    if (foundExactMatch)
                    {
                        break;
                    }

                    int matchingParts = acceptType.GetMatchingParts(inexactContentType);
                    if (matchingParts < 0)
                    {
                        continue;
                    }

                    if (matchingParts > selectedMatchingParts)
                    {
                        // A more specific type wins.
                        selectedContentType = inexactContentType;
                        selectedMatchingParts = matchingParts;
                        selectedQualityValue = acceptType.SelectQualityValue();
                        acceptable = selectedQualityValue != 0;
                    }
                    else if (matchingParts == selectedMatchingParts)
                    {
                        // A type with a higher q-value wins.
                        int candidateQualityValue = acceptType.SelectQualityValue();
                        if (candidateQualityValue > selectedQualityValue)
                        {
                            selectedContentType = inexactContentType;
                            selectedQualityValue = candidateQualityValue;
                            acceptable = selectedQualityValue != 0;
                        }
                    }
                }
            }

            if (!acceptable && !acceptTypesEmpty)
            {
                throw new DomainDataServiceException((int)HttpStatusCode.UnsupportedMediaType, Resource.HttpProcessUtility_UnsupportedMediaType);
            }

            if (acceptTypesEmpty)
            {
                Debug.Assert(selectedContentType == null, "selectedContentType == null - otherwise accept types were not empty");
                selectedContentType = inexactContentType;
            }

            Debug.Assert(selectedContentType != null, "selectedContentType != null - otherwise no selection was made");
            return selectedContentType;
        }

        /// <summary>
        /// Does a ordinal ignore case comparision of the given mime types.
        /// </summary>
        /// <param name="mimeType1">mime type1.</param>
        /// <param name="mimeType2">mime type2.</param>
        /// <returns>returns true if the mime type are the same.</returns>
        private static bool CompareMimeType(string mimeType1, string mimeType2)
        {
            return String.Equals(mimeType1, mimeType2, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Returns all MIME types from the specified (non-blank) <paramref name='text' />.</summary>
        /// <param name='text'>Non-blank text, as it appears on an HTTP Accepts header.</param>
        /// <returns>An enumerable object with media type descriptions.</returns>
        private static IEnumerable<MediaType> MimeTypesFromAcceptHeader(string text)
        {
            Debug.Assert(!String.IsNullOrEmpty(text), "!String.IsNullOrEmpty(text)");
            List<MediaType> mediaTypes = new List<MediaType>();
            int textIndex = 0;
            while (!SkipWhitespace(text, ref textIndex))
            {
                string type;
                string subType;
                ReadMediaTypeAndSubtype(text, ref textIndex, out type, out subType);

                KeyValuePair<string, string>[] parameters = null;
                while (!SkipWhitespace(text, ref textIndex))
                {
                    if (text[textIndex] == ',')
                    {
                        textIndex++;
                        break;
                    }

                    if (text[textIndex] != ';')
                    {
                        throw HttpProcessUtils.CreateParsingException(Resource.HttpProcessUtility_MediaTypeRequiresSemicolonBeforeParameter);
                    }

                    textIndex++;
                    if (SkipWhitespace(text, ref textIndex))
                    {
                        // ';' should be a leading separator, but we choose to be a 
                        // bit permissive and allow it as a final delimiter as well.
                        break;
                    }

                    ReadMediaTypeParameter(text, ref textIndex, ref parameters);
                }

                mediaTypes.Add(new MediaType(type, subType, parameters));
            }

            return mediaTypes;
        }

        /// <summary>Reads the type and subtype specifications for a MIME type.</summary>
        /// <param name='text'>Text in which specification exists.</param>
        /// <param name='textIndex'>Pointer into text.</param>
        /// <param name='type'>Type of media found.</param>
        /// <param name='subType'>Subtype of media found.</param>
        private static void ReadMediaTypeAndSubtype(string text, ref int textIndex, out string type, out string subType)
        {
            Debug.Assert(text != null, "text != null");
            int textStart = textIndex;
            if (ReadToken(text, ref textIndex))
            {
                throw HttpProcessUtils.CreateParsingException(Resource.HttpProcessUtility_MediaTypeUnspecified);
            }

            if (text[textIndex] != '/')
            {
                throw HttpProcessUtils.CreateParsingException(Resource.HttpProcessUtility_MediaTypeRequiresSlash);
            }

            type = text.Substring(textStart, textIndex - textStart);
            textIndex++;

            int subTypeStart = textIndex;
            ReadToken(text, ref textIndex);

            if (textIndex == subTypeStart)
            {
                throw HttpProcessUtils.CreateParsingException(Resource.HttpProcessUtility_MediaTypeRequiresSubType);
            }

            subType = text.Substring(subTypeStart, textIndex - subTypeStart);
        }

        /// <summary>Read a parameter for a media type/range.</summary>
        /// <param name="text">Text to read from.</param>
        /// <param name="textIndex">Pointer in text.</param>
        /// <param name="parameters">Array with parameters to grow as necessary.</param>
        private static void ReadMediaTypeParameter(string text, ref int textIndex, ref KeyValuePair<string, string>[] parameters)
        {
            int startIndex = textIndex;
            if (ReadToken(text, ref textIndex))
            {
                throw HttpProcessUtils.CreateParsingException(Resource.HttpProcessUtility_MediaTypeMissingValue);
            }

            string parameterName = text.Substring(startIndex, textIndex - startIndex);
            if (text[textIndex] != '=')
            {
                throw HttpProcessUtils.CreateParsingException(Resource.HttpProcessUtility_MediaTypeMissingValue);
            }

            textIndex++;

            string parameterValue = ReadQuotedParameterValue(parameterName, text, ref textIndex);

            // Add the parameter name/value pair to the list.
            if (parameters == null)
            {
                parameters = new KeyValuePair<string, string>[1];
            }
            else
            {
                KeyValuePair<string, string>[] grow = new KeyValuePair<string, string>[parameters.Length + 1];
                Array.Copy(parameters, grow, parameters.Length);
                parameters = grow;
            }

            parameters[parameters.Length - 1] = new KeyValuePair<string, string>(parameterName, parameterValue);
        }

        /// <summary>
        /// Reads Mime type parameter value for a particular parameter in the Content-Type/Accept headers.
        /// </summary>
        /// <param name="parameterName">Name of parameter.</param>
        /// <param name="headerText">Header text.</param>
        /// <param name="textIndex">Parsing index in <paramref name="headerText"/>.</param>
        /// <returns>String representing the value of the <paramref name="parameterName"/> parameter.</returns>
        private static string ReadQuotedParameterValue(string parameterName, string headerText, ref int textIndex)
        {
            StringBuilder parameterValue = new StringBuilder();

            // Check if the value is quoted.
            bool valueIsQuoted = false;
            if (textIndex < headerText.Length)
            {
                if (headerText[textIndex] == '\"')
                {
                    textIndex++;
                    valueIsQuoted = true;
                }
            }

            while (textIndex < headerText.Length)
            {
                char currentChar = headerText[textIndex];

                if (currentChar == '\\' || currentChar == '\"')
                {
                    if (!valueIsQuoted)
                    {
                        throw HttpProcessUtils.CreateParsingException(string.Format(
                            CultureInfo.InvariantCulture,
                            Resource.HttpProcessUtility_EscapeCharWithoutQuotes,
                            parameterName));
                    }

                    textIndex++;

                    // End of quoted parameter value.
                    if (currentChar == '\"')
                    {
                        valueIsQuoted = false;
                        break;
                    }

                    if (textIndex >= headerText.Length)
                    {
                        throw HttpProcessUtils.CreateParsingException(string.Format(
                            CultureInfo.InvariantCulture,
                            Resource.HttpProcessUtility_EscapeCharAtEnd,
                            parameterName));
                    }

                    currentChar = headerText[textIndex];
                }
                else
                    if (!IsHttpToken(currentChar))
                    {
                        // If the given character is special, we stop processing.
                        break;
                    }

                parameterValue.Append(currentChar);
                textIndex++;
            }

            if (valueIsQuoted)
            {
                throw HttpProcessUtils.CreateParsingException(string.Format(
                    CultureInfo.InvariantCulture,
                    Resource.HttpProcessUtility_ClosingQuoteNotFound,
                    parameterName));
            }

            return parameterValue.ToString();
        }

        /// <summary>
        /// Reads the numeric part of a quality value substring, normalizing it to 0-1000
        /// rather than the standard 0.000-1.000 ranges.
        /// </summary>
        /// <param name="text">Text to read qvalue from.</param>
        /// <param name="textIndex">Index into text where the qvalue starts.</param>
        /// <param name="qualityValue">After the method executes, the normalized qvalue.</param>
        /// <remarks>
        /// For more information, see RFC 2616.3.8.
        /// </remarks>
        private static void ReadQualityValue(string text, ref int textIndex, out int qualityValue)
        {
            char digit = text[textIndex++];
            if (digit == '0')
            {
                qualityValue = 0;
            }
            else if (digit == '1')
            {
                qualityValue = 1;
            }
            else
            {
                throw HttpProcessUtils.CreateParsingException(Resource.HttpContextServiceHost_MalformedHeaderValue);
            }

            if (textIndex < text.Length && text[textIndex] == '.')
            {
                textIndex++;

                int adjustFactor = 1000;
                while (adjustFactor > 1 && textIndex < text.Length)
                {
                    char c = text[textIndex];
                    int charValue = DigitToInt32(c);
                    if (charValue >= 0)
                    {
                        textIndex++;
                        adjustFactor /= 10;
                        qualityValue *= 10;
                        qualityValue += charValue;
                    }
                    else
                    {
                        break;
                    }
                }

                qualityValue = qualityValue *= adjustFactor;
                if (qualityValue > 1000)
                {
                    // Too high of a value in qvalue.
                    throw HttpProcessUtils.CreateParsingException(Resource.HttpContextServiceHost_MalformedHeaderValue);
                }
            }
            else
            {
                qualityValue *= 1000;
            }
        }

        /// <summary>
        /// Reads a token on the specified text by advancing an index on it.
        /// </summary>
        /// <param name="text">Text to read token from.</param>
        /// <param name="textIndex">Index for the position being scanned on text.</param>
        /// <returns>true if the end of the text was reached; false otherwise.</returns>
        private static bool ReadToken(string text, ref int textIndex)
        {
            while (textIndex < text.Length && IsHttpToken(text[textIndex]))
            {
                textIndex++;
            }

            return (textIndex == text.Length);
        }

        /// <summary>
        /// Skips whitespace in the specified text by advancing an index to
        /// the next non-whitespace character.
        /// </summary>
        /// <param name="text">Text to scan.</param>
        /// <param name="textIndex">Index to begin scanning from.</param>
        /// <returns>true if the end of the string was reached, false otherwise.</returns>
        private static bool SkipWhitespace(string text, ref int textIndex)
        {
            Debug.Assert(text != null, "text != null");
            Debug.Assert(text.Length >= 0, "text >= 0");
            Debug.Assert(textIndex <= text.Length, "text <= text.Length");

            while (textIndex < text.Length && Char.IsWhiteSpace(text, textIndex))
            {
                textIndex++;
            }

            return (textIndex == text.Length);
        }

        /// <summary>
        /// Verfies whether the specified character is a valid separator in
        /// an HTTP header list of element.
        /// </summary>
        /// <param name="c">Character to verify.</param>
        /// <returns>true if c is a valid character for separating elements; false otherwise.</returns>
        private static bool IsHttpElementSeparator(char c)
        {
            return c == ',' || c == ' ' || c == '\t';
        }

        /// <summary>
        /// Determines whether the specified character is a valid HTTP separator.
        /// </summary>
        /// <param name="c">Character to verify.</param>
        /// <returns>true if c is a separator; false otherwise.</returns>
        /// <remarks>
        /// See RFC 2616 2.2 for further information.
        /// </remarks>
        private static bool IsHttpSeparator(char c)
        {
            return
                c == '(' || c == ')' || c == '<' || c == '>' || c == '@' ||
                c == ',' || c == ';' || c == ':' || c == '\\' || c == '"' ||
                c == '/' || c == '[' || c == ']' || c == '?' || c == '=' ||
                c == '{' || c == '}' || c == ' ' || c == '\x9';
        }

        /// <summary>
        /// Determines whether the specified character is a valid HTTP header token character.
        /// </summary>
        /// <param name="c">Character to verify.</param>
        /// <returns>true if c is a valid HTTP header token character; false otherwise.</returns>
        private static bool IsHttpToken(char c)
        {
            // A token character is any character (0-127) except control (0-31) or
            // separators. 127 is DEL, a control character.
            return c < '\x7F' && c > '\x1F' && !IsHttpSeparator(c);
        }

        /// <summary>
        /// Converts the specified character from the ASCII range to a digit.
        /// </summary>
        /// <param name="c">Character to convert.</param>
        /// <returns>
        /// The Int32 value for c, or -1 if it is an element separator.
        /// </returns>
        private static int DigitToInt32(char c)
        {
            if (c >= '0' && c <= '9')
            {
                return (int)(c - '0');
            }
            else
            {
                if (IsHttpElementSeparator(c))
                {
                    return -1;
                }
                else
                {
                    throw HttpProcessUtils.CreateParsingException(Resource.HttpContextServiceHost_MalformedHeaderValue);
                }
            }
        }

        /// <summary>Creates a new exception for parsing errors.</summary>
        /// <param name="message">Message for error.</param>
        /// <returns>A new exception that can be thrown for a parsing error.</returns>
        private static DomainDataServiceException CreateParsingException(string message)
        {
            // Status code "400"  ; Section 10.4.1: Bad Request
            return new DomainDataServiceException((int)HttpStatusCode.BadRequest, message);
        }

        /// <summary>Use this class to represent a media type definition.</summary>
        [DebuggerDisplay("MediaType [{type}/{subType}]")]
        private sealed class MediaType
        {
            /// <summary>Parameters specified on the media type.</summary>
            private readonly KeyValuePair<string, string>[] parameters;

            /// <summary>Sub-type specification (for example, 'plain').</summary>
            private readonly string subType;

            /// <summary>Type specification (for example, 'text').</summary>
            private readonly string type;

            /// <summary>
            /// Initializes a new <see cref="MediaType"/> read-only instance.
            /// </summary>
            /// <param name="type">Type specification (for example, 'text').</param>
            /// <param name="subType">Sub-type specification (for example, 'plain').</param>
            /// <param name="parameters">Parameters specified on the media type.</param>
            internal MediaType(string type, string subType, KeyValuePair<string, string>[] parameters)
            {
                Debug.Assert(type != null, "type != null");
                Debug.Assert(subType != null, "subType != null");

                this.type = type;
                this.subType = subType;
                this.parameters = parameters;
            }

            /// <summary>Returns the MIME type in standard type/subtype form, without parameters.</summary>
            internal string MimeType
            {
                get { return this.type + "/" + this.subType; }
            }

            /// <summary>media type parameters</summary>
            internal KeyValuePair<string, string>[] Parameters
            {
                get { return this.parameters; }
            }

            /// <summary>Gets a number of non-* matching types, or -1 if not matching at all.</summary>
            /// <param name="candidate">Candidate MIME type to match.</param>
            /// <returns>The number of non-* matching types, or -1 if not matching at all.</returns>
            internal int GetMatchingParts(string candidate)
            {
                Debug.Assert(candidate != null, "candidate must not be null.");

                int result = -1;
                if (candidate.Length > 0)
                {
                    if (this.type == "*")
                    {
                        result = 0;
                    }
                    else
                    {
                        int separatorIdx = candidate.IndexOf('/');
                        if (separatorIdx >= 0)
                        {
                            string candidateType = candidate.Substring(0, separatorIdx);
                            if (HttpProcessUtils.CompareMimeType(this.type, candidateType))
                            {
                                if (this.subType == "*")
                                {
                                    result = 1;
                                }
                                else
                                {
                                    string candidateSubType = candidate.Substring(candidateType.Length + 1);
                                    if (HttpProcessUtils.CompareMimeType(this.subType, candidateSubType))
                                    {
                                        result = 2;
                                    }
                                }
                            }
                        }
                    }
                }

                return result;
            }

            /// <summary>Selects a quality value for the specified type.</summary>
            /// <returns>The quality value, in range from 0 through 1000.</returns>
            /// <remarks>See http://tools.ietf.org/html/rfc2616#section-14.1 for further details.</remarks>
            internal int SelectQualityValue()
            {
                if (this.parameters != null)
                {
                    foreach (KeyValuePair<string, string> parameter in this.parameters)
                    {
                        if (String.Equals(parameter.Key, HttpProcessUtils.HttpQValueParameter, StringComparison.OrdinalIgnoreCase))
                        {
                            string qvalueText = parameter.Value.Trim();
                            if (qvalueText.Length > 0)
                            {
                                int result;
                                int textIndex = 0;
                                ReadQualityValue(qvalueText, ref textIndex, out result);
                                return result;
                            }
                        }
                    }
                }

                return 1000;
            }
        }
    }
}