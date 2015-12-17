// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNet.Antiforgery;
using Microsoft.AspNet.Mvc.Filters;
using Microsoft.AspNet.Mvc.Internal;

namespace Microsoft.AspNet.Mvc.ViewFeatures
{
    public class AutoValidateAntiforgeryTokenAuthorizationFilter : IAsyncAuthorizationFilter, IAntiforgeryPolicy
    {
        private readonly IAntiforgery _antiforgery;

        public AutoValidateAntiforgeryTokenAuthorizationFilter(IAntiforgery antiforgery)
        {
            if (antiforgery == null)
            {
                throw new ArgumentNullException(nameof(antiforgery));
            }

            _antiforgery = antiforgery;
        }

        public Task OnAuthorizationAsync(AuthorizationContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (IsUnsafeHttpMethod(context) && IsClosestAntiforgeryPolicy(context.Filters))
            {
                return _antiforgery.ValidateRequestAsync(context.HttpContext);
            }

            return TaskCache.CompletedTask;
        }

        private bool IsUnsafeHttpMethod(AuthorizationContext context)
        {
            var method = context.HttpContext.Request.Method;
            if (string.Equals("GET", method, StringComparison.OrdinalIgnoreCase) ||
                string.Equals("HEAD", method, StringComparison.OrdinalIgnoreCase) ||
                string.Equals("TRACE", method, StringComparison.OrdinalIgnoreCase) ||
                string.Equals("OPTIONS", method, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Assume anything we know isn't safe is unsafe.
            return true;
        }

        private bool IsClosestAntiforgeryPolicy(IList<IFilterMetadata> filters)
        {
            // Determine if this instance is the 'effective' antiforgery policy.
            for (var i = filters.Count - 1; i >= 0; i--)
            {
                var filter = filters[i];
                if (filter is IAntiforgeryPolicy)
                {
                    return object.ReferenceEquals(this, filters[i]);
                }
            }

            Debug.Fail("The current instance should be in the list of filters.");
            return false;
        }
    }
}
