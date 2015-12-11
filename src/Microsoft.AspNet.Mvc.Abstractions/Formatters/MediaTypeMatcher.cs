using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.Formatters
{
    public class MediaTypeMatcher
    {
        private static readonly List<MediaTypeHeaderValue> EmptyFilterList = new List<MediaTypeHeaderValue>();
        private bool _doneParsing = false;
        private int _currentPosition = 0;
        private bool _hasValidItems = false;
        private bool _respectBrowserAcceptHeader;
        private double _threshold = 1.0;
        private double _nextThreshold = 0;

        private MediaTypeCandidate _current;
        private IList<MediaTypeHeaderValue> _filteredContentTypes;

        public MediaTypeMatcher(string acceptHeader, bool respectBrowserAcceptHeader)
        : this(acceptHeader, respectBrowserAcceptHeader, filteredContentTypes: null)
        { }

        public MediaTypeMatcher(string acceptHeader, bool respectBrowserAcceptHeader, IList<MediaTypeHeaderValue> filteredContentTypes)
        {
            // We are going to be nice and treat null as string.Empty
            if (string.IsNullOrEmpty(acceptHeader) || (!respectBrowserAcceptHeader && acceptHeader.Contains("*/*")))
            {
                _doneParsing = true;
                AcceptHeader = string.Empty;
            }
            else
            {
                AcceptHeader = acceptHeader;
            }
            _respectBrowserAcceptHeader = respectBrowserAcceptHeader;
            _filteredContentTypes = filteredContentTypes ?? EmptyFilterList;
            // Initialize to the first header we want to match.
            _hasValidItems = Next();
        }

        public string AcceptHeader { get; }

        /// <summary>
        /// Represents a parsed media type within an accept header string.
        /// </summary>
        [DebuggerDisplay("{ToString()}")]
        private struct MediaTypeCandidate
        {
            public StringSegment mediaType;
            public StringSegment subtype;
            public IList<StringSegment> parameters;
            public double quality;

            public bool MediaTypeMatches(string mediaTypeValue)
            {
                var hasValues = mediaType.HasValue && subtype.HasValue;

                if (!hasValues || mediaTypeValue.Length != mediaType.Length + subtype.Length + 1)
                {
                    return false;
                }

                var mediaTypeMatches = mediaType.StartsWith("*", StringComparison.OrdinalIgnoreCase);
                if (!mediaTypeMatches)
                {
                    var matches = true;
                    for (int i = 0; i < mediaType.Length; i++)
                    {
                        if (char.ToLower(mediaType.Buffer[mediaType.Offset + i]) != char.ToLower(mediaTypeValue[i]))
                        {
                            matches = false;
                            break;
                        }
                    }

                    mediaTypeMatches = mediaTypeMatches || matches;
                }

                var subtypeMatches = subtype.StartsWith("*", StringComparison.OrdinalIgnoreCase);
                if (!subtypeMatches)
                {
                    var matches = true;
                    for (int i = 0; i < subtype.Length; i++)
                    {
                        if (char.ToLower(subtype.Buffer[subtype.Offset + i]) != char.ToLower(mediaTypeValue[i + mediaType.Length + 1]))
                        {
                            matches = false;
                            break;
                        }
                    }

                    subtypeMatches = subtypeMatches || matches;
                }

                return hasValues && mediaTypeMatches && subtypeMatches;
            }

            public override string ToString()
            {
                var mediaTypeWithQuality = $"{mediaType}/{subtype};q={quality}";
                if (parameters?.Count > 0)
                {
                    var formattedParameter = string.Join(";", parameters?.Select(mtp => mtp));
                    return $"{mediaTypeWithQuality};{formattedParameter}";
                }
                else
                {
                    return mediaTypeWithQuality;
                }
            }
        }

        public bool HasValidValues => _hasValidItems;

        private bool IsCompatible(MediaTypeCandidate candidate)
        {
            foreach (var contentType in _filteredContentTypes)
            {
                if (!IsSuperSetOf(candidate, contentType))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsSuperSetOf(MediaTypeCandidate candidate, MediaTypeHeaderValue mediaType)
        {
            return candidate.MediaTypeMatches(mediaType.MediaType) &&
                ParametersAreASupersetOf(candidate.parameters, mediaType.Parameters);
        }

        private bool ParametersAreASupersetOf(IList<StringSegment> parameters, IList<NameValueHeaderValue> mediatypeParameters)
        {
            foreach (var mtp in mediatypeParameters)
            {
                foreach (var parameter in parameters)
                {
                    if (parameter.StartsWith(mtp.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        return !parameter.EndsWith(mtp.Value, StringComparison.OrdinalIgnoreCase);
                    }
                }
            }

            return true;
        }

        private MediaTypeCandidate ParseNext()
        {
            var foundCommaOrEnd = false;
            StringSegment mediaType = new StringSegment();
            StringSegment subtype = new StringSegment();
            IList<StringSegment> parameters = null;
            double quality = 1.0;

            var currentPosition = _currentPosition;

            var previousPosition = currentPosition;

            while (!foundCommaOrEnd && currentPosition <= AcceptHeader.Length)
            {
                if (currentPosition == AcceptHeader.Length)
                {
                    var end = currentPosition;
                    // If we found 'EOS' we correct the current position in order to parse the last bit of information (subtype, quality or parameter)
                    if (!subtype.HasValue)
                    {
                        subtype = new StringSegment(AcceptHeader, previousPosition, end - previousPosition);
                        foundCommaOrEnd = true;
                    }
                    else
                    {
                        if (AcceptHeader.IndexOf("q=", previousPosition, StringComparison.OrdinalIgnoreCase) == previousPosition)
                        {
                            var afterEquals = AcceptHeader.IndexOf('=', previousPosition, end - previousPosition) + 1;
                            quality = double.Parse(AcceptHeader.Substring(afterEquals, end - afterEquals));
                        }
                        else
                        {
                            if (parameters == null)
                            {
                                parameters = new List<StringSegment> { new StringSegment(AcceptHeader, previousPosition, end - previousPosition) };
                            }
                            else
                            {
                                parameters.Add(new StringSegment(AcceptHeader, previousPosition, end - previousPosition));
                            }
                        }
                    }

                    break;
                }

                switch (AcceptHeader[currentPosition])
                {
                    case '/':
                        mediaType = new StringSegment(AcceptHeader, previousPosition, currentPosition - previousPosition);
                        previousPosition = currentPosition + 1;
                        break;
                    case ',':
                        if (!subtype.HasValue)
                        {
                            subtype = new StringSegment(AcceptHeader, previousPosition, currentPosition - previousPosition);
                            previousPosition = currentPosition + 1;
                            foundCommaOrEnd = true;
                        }
                        else
                        {
                            if (AcceptHeader.IndexOf("q=", previousPosition, StringComparison.OrdinalIgnoreCase) == previousPosition)
                            {
                                var afterEquals = AcceptHeader.IndexOf('=', previousPosition, currentPosition - previousPosition) + 1;
                                quality = double.Parse(AcceptHeader.Substring(afterEquals, currentPosition - afterEquals));
                            }
                            else
                            {
                                if (parameters == null)
                                {
                                    parameters = new List<StringSegment> { new StringSegment(AcceptHeader, previousPosition, currentPosition - previousPosition) };
                                }
                                else
                                {
                                    parameters.Add(new StringSegment(AcceptHeader, previousPosition, currentPosition - previousPosition));
                                }
                            }
                        }
                        foundCommaOrEnd = true;
                        break;
                    case ';':
                        if (!subtype.HasValue)
                        {
                            subtype = new StringSegment(AcceptHeader, previousPosition, currentPosition - previousPosition);
                        }
                        else
                        {
                            if (AcceptHeader.IndexOf("q=", previousPosition, StringComparison.OrdinalIgnoreCase) == previousPosition)
                            {
                                var afterEquals = AcceptHeader.IndexOf('=', previousPosition, currentPosition - previousPosition) + 1;
                                quality = double.Parse(AcceptHeader.Substring(afterEquals, currentPosition - afterEquals));
                            }
                            else
                            {
                                if (parameters == null)
                                {
                                    parameters = new List<StringSegment> { new StringSegment(AcceptHeader, previousPosition, currentPosition - previousPosition) };
                                }
                                else
                                {
                                    parameters.Add(new StringSegment(AcceptHeader, previousPosition, currentPosition - previousPosition));
                                }
                            }
                        }

                        previousPosition = currentPosition + 1;
                        break;
                }

                currentPosition++;
            }

            _doneParsing = currentPosition >= AcceptHeader.Length;
            _currentPosition = currentPosition;

            MediaTypeCandidate result = new MediaTypeCandidate();
            if (mediaType.HasValue && subtype.HasValue)

            {
                result = new MediaTypeCandidate
                {
                    mediaType = mediaType,
                    subtype = subtype,
                    parameters = parameters,
                    quality = quality
                };
            }

            return result;
        }

        public IList<string> GetAllMatches()
        {
            var result = new List<MediaTypeCandidate>();
            while (!_doneParsing)
            {
                result.Add(ParseNext());
            }

            var list = new List<string>();
            foreach (var mt in result)
            {
                list.Add(mt.ToString());
            }

            return list;
        }

        public string Current => _current.ToString();

        // Advances the matcher to the next match.  We do the parsing as lazily as we can, that means
        // the moment we see a media type with quality 1.0 we stop and we try that. This optimizes the case
        // where the Accept header contains something like Accept: text/html, application/xhtml+xml, image/jxr, */*
        // (sample taken from the Edge accept header) in which we might be able to decide with the first media type.
        // After we parsed all the header, if we didn't find a suitable media type with quality 1.0, we iterate
        // through the list of parsed media types in quality order.
        public bool Next()
        {
            if (_doneParsing)
            {
                _threshold = _nextThreshold;
                if (_threshold == 0)
                {
                    return false;
                }

                _nextThreshold = 0;
                _currentPosition = 0;
                _doneParsing = false;
                return Next();
            }
            else
            {
                _current = ParseNext();
                while (_current.quality != _threshold && !_doneParsing)
                {
                    if (IsCompatible(_current))
                    {
                        if (_nextThreshold < _current.quality && _current.quality < _threshold)
                        {
                            _nextThreshold = _current.quality;
                        }
                    }

                    _current = ParseNext();
                }

                if (_doneParsing && _current.quality != _threshold)
                {
                    if (IsCompatible(_current))
                    {
                        if (_nextThreshold < _current.quality && _current.quality < _threshold)
                        {
                            _nextThreshold = _current.quality;
                        }
                    }

                    return Next();
                }
                else
                {
                    // _current.quality = 1.0, so we stop parsing and we evaluate this candidate.
                    return true;
                }
            }
        }

        /// <summary>
        /// Determines if the current media type from the header is a superset of
        /// the media type given as an argument. For example, if the current parsed media
        /// type is text/* and the given <paramref name="value"/> is "text/plain", IsSuperSetOf
        /// will return true.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool IsSuperSetOf(MediaTypeHeaderValue value)
        {
            return IsSuperSetOf(_current, value);
        }
    }
}
