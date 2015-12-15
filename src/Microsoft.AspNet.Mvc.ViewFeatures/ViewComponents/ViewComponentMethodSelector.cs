// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ViewFeatures;

namespace Microsoft.AspNet.Mvc.ViewComponents
{
    public static class ViewComponentMethodSelector
    {
        public static readonly string AsyncMethodName = "InvokeAsync";
        public static readonly string SyncMethodName = "Invoke";

        public static MethodInfo FindAsyncMethod(Type componentType)
        {
            if (componentType == null)
            {
                throw new ArgumentNullException(nameof(componentType));
            }

            var method = componentType.GetMethod(AsyncMethodName, BindingFlags.Public | BindingFlags.Instance);
            if (method == null)
            {
                return null;
            }

            if (!method.ReturnType.GetTypeInfo().IsGenericType ||
                method.ReturnType.GetGenericTypeDefinition() != typeof(Task<>))
            {
                throw new InvalidOperationException(
                    Resources.FormatViewComponent_AsyncMethod_ShouldReturnTask(AsyncMethodName));
            }

            return method;
        }

        public static MethodInfo FindSyncMethod(Type componentType)
        {
            if (componentType == null)
            {
                throw new ArgumentNullException(nameof(componentType));
            }

            var method = componentType.GetMethod(SyncMethodName, BindingFlags.Public | BindingFlags.Instance);
            if (method == null)
            {
                return null;
            }

            if (method.ReturnType == typeof(void))
            {
                throw new InvalidOperationException(
                    Resources.FormatViewComponent_SyncMethod_ShouldReturnValue(SyncMethodName));
            }
            else if (method.ReturnType.IsAssignableFrom(typeof(Task)))
            {
                throw new InvalidOperationException(
                    Resources.FormatViewComponent_SyncMethod_CannotReturnTask(SyncMethodName, nameof(Task)));
            }

            return method;
        }
    }
}
