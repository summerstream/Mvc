using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Html;

namespace Microsoft.AspNet.Mvc
{
    public static class ViewComponentHelperExtensions
    {
        public static Task<IHtmlContent> InvokeAsync(this IViewComponentHelper viewComponentHelper, string name)
            => viewComponentHelper.InvokeAsync(name, arguments: null);

        public static Task<IHtmlContent> InvokeAsync(this IViewComponentHelper viewComponentHelper, Type componentType)
            => viewComponentHelper.InvokeAsync(componentType, arguments: null);

        public static Task RenderInvokeAsync(this IViewComponentHelper viewComponentHelper, string name)
            => viewComponentHelper.RenderInvokeAsync(name, arguments: null);

        public static Task RenderInvokeAsync(this IViewComponentHelper viewComponentHelper, Type componentType)
            => viewComponentHelper.RenderInvokeAsync(componentType, arguments: null);
    }
}
