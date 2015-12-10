using System.Collections.Generic;
using Xunit;

namespace Microsoft.AspNet.Mvc.Formatters
{
    public class MediaTypeMatcherTests
    {
        [Theory(Skip = "obsolete")]
        [MemberData(nameof(MediaTypesData))]
        public void MediaTypeMatcher_CanParseMediaTypes(string acceptHeader, IList<string> expectedMediaTypes)
        {
            var matcher = new MediaTypeMatcher(acceptHeader, respectBrowserAcceptHeader: true);

            var matches = matcher.GetAllMatches();
            Assert.Equal(matches.Count, expectedMediaTypes.Count);

            foreach (var mt in expectedMediaTypes)
            {
                Assert.Contains(mt, matches);
            }
        }

        [Theory]
        [InlineData("text/html,text/xml;q=0.5,application/json;q=1,text/plain;q=0.3")]
        public void MediaTypeMatcher_ParsesHeaderLazily(string header)
        {
            var matcher = new MediaTypeMatcher(header, respectBrowserAcceptHeader: true);
            while (matcher.Next())
            {
                var current = matcher.Current;
                Assert.NotNull(current);
            }
        }

        public static TheoryData<string, IList<string>> MediaTypesData
        {
            get
            {
                var data = new TheoryData<string, IList<string>>();

                //data.Add("*/*", new List<string> { "*/*;q=1" });
                data.Add("text/*;q=0.3,text/html;q=0.7,text/html;q=1;level=1,text/html;q=0.4;level=2,*/*;q=0.5", new List<string>
                {
                    "text/*;q=0.3",
                    "text/html;q=0.7",
                    "text/html;q=1;level=1",
                    "text/html;q=0.4;level=2",
                    "*/*;q=0.5"
                });

                return data;
            }
        }
    }
}
