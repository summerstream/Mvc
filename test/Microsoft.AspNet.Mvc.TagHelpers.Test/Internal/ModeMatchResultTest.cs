// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.TagHelpers.Logging;
using Microsoft.AspNet.Razor.TagHelpers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.TagHelpers.Internal
{
    public class ModeMatchResultTest
    {
        [Fact]
        public void LogDetails_LogsDebugWhenNoFullMatchesFound()
        {
            // Arrange
            var modeMatchResult = new ModeMatchResult<string>();
            var logger = MakeLogger(LogLevel.Debug);
            var tagHelper = new Mock<ITagHelper>();
            var uniqueId = "id";
            var viewPath = "Views/Home/Index.cshtml";

            // Act
            logger.TagHelperModeMatchResult(modeMatchResult, uniqueId, viewPath, tagHelper.Object);

            // Assert
            Mock.Get(logger).Verify(l => l.Log(
                LogLevel.Debug,
                It.IsAny<int>(),
                It.IsAny<object>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<object, Exception, string>>()), Times.Once);
        }

        [Fact]
        public void LogDetails_DoesNotLogWhenPartialMatchFoundButNoPartiallyMatchedAttributesFound()
        {
            // Arrange
            var modeMatchResult = new ModeMatchResult<string>();
            modeMatchResult.FullMatches.Add(
                ModeMatchAttributes.Create("mode0", new[] { "first-attr" }));
            modeMatchResult.PartialMatches.Add(
                ModeMatchAttributes.Create("mode1", new[] { "first-attr" }, new[] { "second-attr" }));
            var logger = MakeLogger(LogLevel.Debug);
            var tagHelper = new Mock<ITagHelper>();
            var uniqueId = "id";
            var viewPath = "Views/Home/Index.cshtml";

            // Act
            logger.TagHelperModeMatchResult(modeMatchResult, uniqueId, viewPath, tagHelper.Object);

            // Assert
            Mock.Get(logger).Verify(l => l.Log(
                LogLevel.Warning,
                It.IsAny<int>(),
                It.IsAny<object>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<object, Exception, string>>()), Times.Never);
            Mock.Get(logger).Verify(l => l.Log(
                LogLevel.Debug,
                It.IsAny<int>(),
                It.IsAny<object>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<object, Exception, string>>()), Times.Never);
        }

        [Fact]
        public void LogDetails_LogsWhenPartiallyMatchedAttributesFound()
        {
            // Arrange
            var modeMatchResult = new ModeMatchResult<string>();
            modeMatchResult.PartialMatches.Add(
                ModeMatchAttributes.Create("mode0", new[] { "first-attr" }, new[] { "second-attr" }));
            modeMatchResult.PartiallyMatchedAttributes.Add("first-attr");
            var logger = MakeLogger(LogLevel.Debug);
            var tagHelper = new Mock<ITagHelper>();
            var uniqueId = "id";
            var viewPath = "Views/Home/Index.cshtml";

            // Act
            logger.TagHelperModeMatchResult(modeMatchResult, uniqueId, viewPath, tagHelper.Object);

            // Assert
            Mock.Get(logger).Verify(l => l.Log(
                LogLevel.Warning,
                It.IsAny<int>(),
                It.IsAny<object>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<object, Exception, string>>()), Times.Once);
            Mock.Get(logger).Verify(l => l.Log(
                LogLevel.Debug,
                It.IsAny<int>(),
                It.IsAny<object>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<object, Exception, string>>()), Times.Once);
        }

        [Fact]
        public void LogDetails_DoesNotLogWhenLoggingLevelIsSetAboveWarning()
        {
            // Arrange
            var modeMatchResult = new ModeMatchResult<string>();
            modeMatchResult.PartialMatches.Add(
                ModeMatchAttributes.Create("mode0", new[] { "first-attr" }, new[] { "second-attr" }));
            modeMatchResult.PartiallyMatchedAttributes.Add("first-attr");
            var logger = MakeLogger(LogLevel.Critical);
            var tagHelper = new Mock<ITagHelper>();
            var uniqueId = "id";
            var viewPath = "Views/Home/Index.cshtml";

            // Act
            logger.TagHelperModeMatchResult(modeMatchResult, uniqueId, viewPath, tagHelper.Object);

            // Assert
            Mock.Get(logger).Verify(l => l.Log(
                LogLevel.Warning,
                It.IsAny<int>(),
                It.IsAny<object>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<object, Exception, string>>()), Times.Never);
            Mock.Get(logger).Verify(l => l.Log(
                LogLevel.Debug,
                It.IsAny<int>(),
                It.IsAny<object>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<object, Exception, string>>()), Times.Never);
        }

        private static ILogger MakeLogger(LogLevel level)
        {
            var logger = new Mock<ILogger>();
            logger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns<LogLevel>(l => l >= level);

            return logger.Object;
        }
    }
}