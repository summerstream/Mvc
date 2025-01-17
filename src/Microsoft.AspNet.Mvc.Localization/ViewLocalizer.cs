// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Mvc.ViewFeatures.Internal;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.PlatformAbstractions;

namespace Microsoft.AspNet.Mvc.Localization
{
    /// <summary>
    /// A <see cref="IHtmlLocalizer"/> implementation that provides localized strings for views.
    /// </summary>
    public class ViewLocalizer : IViewLocalizer, ICanHasViewContext
    {
        private readonly IHtmlLocalizerFactory _localizerFactory;
        private readonly string _applicationName;
        private IHtmlLocalizer _localizer;

        /// <summary>
        /// Creates a new <see cref="ViewLocalizer"/>.
        /// </summary>
        /// <param name="localizerFactory">The <see cref="IHtmlLocalizerFactory"/>.</param>
        /// <param name="applicationEnvironment">The <see cref="IApplicationEnvironment"/>.</param>
        public ViewLocalizer(IHtmlLocalizerFactory localizerFactory, IApplicationEnvironment applicationEnvironment)
        {
            if (localizerFactory == null)
            {
                throw new ArgumentNullException(nameof(localizerFactory));
            }

            if (applicationEnvironment == null)
            {
                throw new ArgumentNullException(nameof(applicationEnvironment));
            }

            _applicationName = applicationEnvironment.ApplicationName;
            _localizerFactory = localizerFactory;
        }

        /// <inheritdoc />
        public virtual LocalizedHtmlString this[string key]
        {
            get
            {
                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                return _localizer[key];
            }
        }

        /// <inheritdoc />
        public virtual LocalizedHtmlString this[string key, params object[] arguments]
        {
            get
            {
                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                return _localizer[key, arguments];
            }
        }

        /// <inheritdoc />
        public LocalizedString GetString(string name) => _localizer.GetString(name);

        /// <inheritdoc />
        public LocalizedString GetString(string name, params object[] values) => _localizer.GetString(name, values);

        /// <inheritdoc />
        public IHtmlLocalizer WithCulture(CultureInfo culture) => _localizer.WithCulture(culture);

        /// <inheritdoc />
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            _localizer.GetAllStrings(includeParentCultures);

        /// <summary>
        /// Apply the specified <see cref="ViewContext"/>.
        /// </summary>
        /// <param name="viewContext">The <see cref="ViewContext"/>.</param>
        public void Contextualize(ViewContext viewContext)
        {
            if (viewContext == null)
            {
                throw new ArgumentNullException(nameof(viewContext));
            }

            var baseName = viewContext.View.Path.Replace('/', '.').Replace('\\', '.');
            if (baseName.StartsWith(".", StringComparison.OrdinalIgnoreCase))
            {
                baseName = baseName.Substring(1);
            }

            _localizer = _localizerFactory.Create(baseName, _applicationName);
        }
    }
}