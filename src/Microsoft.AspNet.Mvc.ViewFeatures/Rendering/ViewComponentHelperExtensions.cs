// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Html;

namespace Microsoft.AspNet.Mvc.Rendering
{
    public static class ViewComponentHelperExtensions
    {
        public static Task<IHtmlContent> InvokeAsync<TComponent>(
            this IViewComponentHelper helper,
            object arguments)
        {
            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            return helper.InvokeAsync(typeof(TComponent), arguments);
        }

        public static Task RenderInvokeAsync<TComponent>(this IViewComponentHelper helper, object arguments)
        {
            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            return helper.RenderInvokeAsync(typeof(TComponent), arguments);
        }
    }
}
