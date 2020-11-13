using System;

namespace OpenRiaServices.Hosting.Wcf.OData
{
    #region Namespace
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    #endregion

    /// <summary>
    /// Contains utility methods for working with URIs.
    /// </summary>
    internal static class UriUtils
    {
        /// <summary>
        /// Replaces the last segment in the given URI with <paramref name="identifier"/>
        /// </summary>
        /// <param name="input">Input URI.</param>
        /// <param name="identifier">Identifier that replaces last segment.</param>
        /// <returns>New URI with last segment replaced.</returns>
        internal static Uri ReplaceLastSegment(Uri input, string identifier)
        {
            if (Uri.UnescapeDataString(input.Segments.Last()).EndsWith(")", StringComparison.Ordinal))
            {
                UriBuilder u = new UriBuilder(input);
                u.Path = u.Path.Remove(u.Path.LastIndexOf(input.Segments.Last(), StringComparison.Ordinal)) + identifier;
                return u.Uri;
            }

            return input;
        }

        /// <summary>
        /// Returns an object that can enumerate the segments in the specified path (eg: /foo/bar -&gt; foo, bar).
        /// </summary>
        /// <param name="absoluteRequestUri">A valid path portion of an uri.</param>
        /// <param name="baseUri">baseUri for the request that is getting processed.</param>
        /// <returns>An enumerable object of unescaped segments.</returns>
        internal static string[] EnumerateSegments(Uri absoluteRequestUri, Uri baseUri)
        {
            Debug.Assert(absoluteRequestUri != null, "absoluteRequestUri != null");
            Debug.Assert(absoluteRequestUri.IsAbsoluteUri, "absoluteRequestUri.IsAbsoluteUri(" + absoluteRequestUri.IsAbsoluteUri + ")");
            Debug.Assert(baseUri != null, "baseUri != null");
            Debug.Assert(baseUri.IsAbsoluteUri, "baseUri.IsAbsoluteUri(" + baseUri + ")");

            if (!UriUtils.UriInvariantInsensitiveIsBaseOf(baseUri, absoluteRequestUri))
            {
                throw new DomainDataServiceException((int)HttpStatusCode.BadRequest, Resource.DomainDataService_RequestUri_IncorrectBase);
            }

            try
            {
                Uri uri = absoluteRequestUri;
                int numberOfSegmentsToSkip = 0;

                // Since there is a svc part in the segment, we will need to skip 2 segments
                numberOfSegmentsToSkip = baseUri.Segments.Length;

                string[] uriSegments = uri.Segments;
                int populatedSegmentCount = 0;
                for (int i = numberOfSegmentsToSkip; i < uriSegments.Length; i++)
                {
                    string segment = uriSegments[i];
                    if (segment.Length != 0 && segment != "/")
                    {
                        populatedSegmentCount++;
                    }
                }

                string[] segments = new string[populatedSegmentCount];
                int segmentIndex = 0;
                for (int i = numberOfSegmentsToSkip; i < uriSegments.Length; i++)
                {
                    string segment = uriSegments[i];
                    if (segment.Length != 0 && segment != "/")
                    {
                        if (segment[segment.Length - 1] == '/')
                        {
                            segment = segment.Substring(0, segment.Length - 1);
                        }

                        segments[segmentIndex++] = Uri.UnescapeDataString(segment);
                    }
                }

                Debug.Assert(segmentIndex == segments.Length, "segmentIndex == segments.Length -- otherwise we mis-counted populated/skipped segments.");
                return segments;
            }
            catch (UriFormatException)
            {
                throw new DomainDataServiceException((int)HttpStatusCode.BadRequest, Resource.DomainDataService_RequestUri_SyntaxError);
            }
        }

        /// <summary>Extracts the identifier part of the unescaped Astoria segment.</summary>
        /// <param name="segment">Unescaped Astoria segment.</param>
        /// <param name="identifier">On returning, the identifier in the segment.</param>
        /// <returns>true if keys follow the identifier.</returns>
        internal static bool ExtractSegmentIdentifier(string segment, out string identifier)
        {
            Debug.Assert(segment != null, "segment != null");

            int filterStart = 0;
            while (filterStart < segment.Length && segment[filterStart] != '(')
            {
                filterStart++;
            }

            identifier = segment.Substring(0, filterStart);
            return filterStart < segment.Length;
        }

        /// <summary>
        /// Merges the inputs to create a single URI string.
        /// </summary>
        /// <param name="baseUri">Base URI string.</param>
        /// <param name="path">Relative path.</param>
        /// <returns>Combined URI string.</returns>
        internal static string CombineUriStrings(string baseUri, string path)
        {
            if (Uri.IsWellFormedUriString(path, UriKind.Absolute) || String.IsNullOrEmpty(path))
            {
                return path;
            }
            if (baseUri.EndsWith("/", StringComparison.Ordinal))
            {
                return (baseUri + (path.StartsWith("/", StringComparison.Ordinal) ? path.Substring(1) : path));
            }
            return (baseUri + (path.StartsWith("/", StringComparison.Ordinal) ? path : ("/" + path)));
        }

        /// <summary>
        /// Determines whether the <paramref name="current"/> Uri instance is a 
        /// base of the specified Uri instance. 
        /// </summary>
        /// <param name="current">Candidate base URI.</param>
        /// <param name="uri">The specified Uri instance to test.</param>
        /// <returns>true if the current Uri instance is a base of uri; otherwise, false.</returns>
        private static bool UriInvariantInsensitiveIsBaseOf(Uri current, Uri uri)
        {
            Debug.Assert(current != null, "current != null");
            Debug.Assert(uri != null, "uri != null");

            Uri upperCurrent = CreateBaseComparableUri(current);
            Uri upperUri = CreateBaseComparableUri(uri);

            return upperCurrent.IsBaseOf(upperUri);
        }

        /// <summary>Creates a URI suitable for host-agnostic comparison purposes.</summary>
        /// <param name="uri">URI to compare.</param>
        /// <returns>URI suitable for comparison.</returns>
        private static Uri CreateBaseComparableUri(Uri uri)
        {
            Debug.Assert(uri != null, "uri != null");

            uri = new Uri(uri.OriginalString.ToUpper(CultureInfo.InvariantCulture), UriKind.RelativeOrAbsolute);

            UriBuilder builder = new UriBuilder(uri);
            builder.Host = "h";
            builder.Port = 80;
            builder.Scheme = "http";
            return builder.Uri;
        }
    }
}