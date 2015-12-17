// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.Filters;

namespace Microsoft.AspNet.Mvc.ViewFeatures
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class IgnoreAntiforgeryTokenAttribute : Attribute, IAntiforgeryPolicy, IOrderedFilter
    {
        public int Order { get; set; }
    }
}
