// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNet.Antiforgery;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Mvc.DataAnnotations;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;
using Microsoft.AspNet.Mvc.Routing;
using Microsoft.AspNet.Mvc.ViewEngines;
using Microsoft.AspNet.Mvc.ViewFeatures;
using Microsoft.AspNet.Mvc.ViewFeatures.Buffer;
using Microsoft.AspNet.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.OptionsModel;
using Microsoft.Extensions.WebEncoders.Testing;
using Moq;

namespace Microsoft.AspNet.Mvc.Rendering
{
    public class DefaultTemplatesUtilities
    {
        public class ObjectTemplateModel
        {
            public ObjectTemplateModel()
            {
                ComplexInnerModel = new object();
            }

            public string Property1 { get; set; }
            [Display(Name = "Prop2")]
            public string Property2 { get; set; }
            public object ComplexInnerModel { get; set; }
        }

        public class ObjectWithScaffoldColumn
        {
            public string Property1 { get; set; }

            [ScaffoldColumn(false)]
            public string Property2 { get; set; }

            [ScaffoldColumn(true)]
            public string Property3 { get; set; }
        }

        public static HtmlHelper<ObjectTemplateModel> GetHtmlHelper()
        {
            return GetHtmlHelper<ObjectTemplateModel>(model: null);
        }

        public static HtmlHelper<IEnumerable<ObjectTemplateModel>> GetHtmlHelperForEnumerable()
        {
            return GetHtmlHelper<IEnumerable<ObjectTemplateModel>>(model: null);
        }

        public static HtmlHelper<ObjectTemplateModel> GetHtmlHelper(IUrlHelper urlHelper)
        {
            return GetHtmlHelper<ObjectTemplateModel>(
                model: null,
                urlHelper: urlHelper,
                viewEngine: CreateViewEngine(),
                provider: TestModelMetadataProvider.CreateDefaultProvider());
        }

        public static HtmlHelper<ObjectTemplateModel> GetHtmlHelper(IHtmlGenerator htmlGenerator)
        {
            var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            return GetHtmlHelper<ObjectTemplateModel>(
                new ViewDataDictionary<ObjectTemplateModel>(metadataProvider),
                CreateUrlHelper(),
                CreateViewEngine(),
                metadataProvider,
                innerHelperWrapper: null,
                htmlGenerator: htmlGenerator,
                idAttributeDotReplacement: null);
        }

        public static HtmlHelper<TModel> GetHtmlHelper<TModel>(ViewDataDictionary<TModel> viewData)
        {
            return GetHtmlHelper(
                viewData,
                CreateUrlHelper(),
                CreateViewEngine(),
                TestModelMetadataProvider.CreateDefaultProvider(),
                innerHelperWrapper: null,
                htmlGenerator: null,
                idAttributeDotReplacement: null);
        }

        public static HtmlHelper<TModel> GetHtmlHelper<TModel>(
            ViewDataDictionary<TModel> viewData,
            string idAttributeDotReplacement)
        {
            return GetHtmlHelper(
                viewData,
                CreateUrlHelper(),
                CreateViewEngine(),
                TestModelMetadataProvider.CreateDefaultProvider(),
                innerHelperWrapper: null,
                htmlGenerator: null,
                idAttributeDotReplacement: idAttributeDotReplacement);
        }

        public static HtmlHelper<TModel> GetHtmlHelper<TModel>(TModel model)
        {
            return GetHtmlHelper(model, CreateViewEngine());
        }

        public static HtmlHelper<TModel> GetHtmlHelper<TModel>(TModel model, string idAttributeDotReplacement)
        {
            var provider = TestModelMetadataProvider.CreateDefaultProvider();
            var viewData = new ViewDataDictionary<TModel>(provider);
            viewData.Model = model;

            return GetHtmlHelper(
                viewData,
                CreateUrlHelper(),
                CreateViewEngine(),
                provider,
                innerHelperWrapper: null,
                htmlGenerator: null,
                idAttributeDotReplacement: idAttributeDotReplacement);
        }

        public static HtmlHelper<IEnumerable<TModel>> GetHtmlHelperForEnumerable<TModel>(TModel model)
        {
            return GetHtmlHelper<IEnumerable<TModel>>(new TModel[] { model });
        }

        public static HtmlHelper<TModel> GetHtmlHelper<TModel>(IModelMetadataProvider provider)
        {
            return GetHtmlHelper<TModel>(model: default(TModel), provider: provider);
        }

        public static HtmlHelper<ObjectTemplateModel> GetHtmlHelper(IModelMetadataProvider provider)
        {
            return GetHtmlHelper<ObjectTemplateModel>(model: null, provider: provider);
        }

        public static HtmlHelper<IEnumerable<ObjectTemplateModel>> GetHtmlHelperForEnumerable(
            IModelMetadataProvider provider)
        {
            return GetHtmlHelper<IEnumerable<ObjectTemplateModel>>(model: null, provider: provider);
        }

        public static HtmlHelper<TModel> GetHtmlHelper<TModel>(TModel model, IModelMetadataProvider provider)
        {
            return GetHtmlHelper(model, CreateUrlHelper(), CreateViewEngine(), provider);
        }

        public static HtmlHelper<TModel> GetHtmlHelper<TModel>(
            TModel model,
            ICompositeViewEngine viewEngine)
        {
            return GetHtmlHelper(model, CreateUrlHelper(), viewEngine, TestModelMetadataProvider.CreateDefaultProvider());
        }

        public static HtmlHelper<TModel> GetHtmlHelper<TModel>(
            TModel model,
            ICompositeViewEngine viewEngine,
            Func<IHtmlHelper, IHtmlHelper> innerHelperWrapper)
        {
            return GetHtmlHelper(
                model,
                CreateUrlHelper(),
                viewEngine,
                TestModelMetadataProvider.CreateDefaultProvider(),
                innerHelperWrapper);
        }

        public static HtmlHelper<TModel> GetHtmlHelper<TModel>(
            TModel model,
            IUrlHelper urlHelper,
            ICompositeViewEngine viewEngine,
            IModelMetadataProvider provider)
        {
            return GetHtmlHelper(model, urlHelper, viewEngine, provider, innerHelperWrapper: null);
        }

        public static HtmlHelper<TModel> GetHtmlHelper<TModel>(
            TModel model,
            IUrlHelper urlHelper,
            ICompositeViewEngine viewEngine,
            IModelMetadataProvider provider,
            Func<IHtmlHelper, IHtmlHelper> innerHelperWrapper)
        {
            var viewData = new ViewDataDictionary<TModel>(provider);
            viewData.Model = model;

            return GetHtmlHelper(
                viewData,
                urlHelper,
                viewEngine,
                provider,
                innerHelperWrapper,
                htmlGenerator: null,
                idAttributeDotReplacement: null);
        }

        private static HtmlHelper<TModel> GetHtmlHelper<TModel>(
            ViewDataDictionary<TModel> viewData,
            IUrlHelper urlHelper,
            ICompositeViewEngine viewEngine,
            IModelMetadataProvider provider,
            Func<IHtmlHelper, IHtmlHelper> innerHelperWrapper,
            IHtmlGenerator htmlGenerator,
            string idAttributeDotReplacement)
        {
            var httpContext = new DefaultHttpContext();
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            var options = new MvcViewOptions();
            if (!string.IsNullOrEmpty(idAttributeDotReplacement))
            {
                options.HtmlHelperOptions.IdAttributeDotReplacement = idAttributeDotReplacement;
            }
            var localizationOptionsAccesor = new Mock<IOptions<MvcDataAnnotationsLocalizationOptions>>();

            localizationOptionsAccesor.SetupGet(o => o.Value).Returns(new MvcDataAnnotationsLocalizationOptions());

            options.ClientModelValidatorProviders.Add(new DataAnnotationsClientModelValidatorProvider(
                new ValidationAttributeAdapterProvider(),
                localizationOptionsAccesor.Object,
                stringLocalizerFactory: null));
            var optionsAccessor = new Mock<IOptions<MvcViewOptions>>();
            optionsAccessor
                .SetupGet(o => o.Value)
                .Returns(options);

            var urlHelperFactory = new Mock<IUrlHelperFactory>();
            urlHelperFactory
                .Setup(f => f.GetUrlHelper(It.IsAny<ActionContext>()))
                .Returns(urlHelper);

            if (htmlGenerator == null)
            {
                htmlGenerator = new DefaultHtmlGenerator(
                    Mock.Of<IAntiforgery>(),
                    optionsAccessor.Object,
                    provider,
                    urlHelperFactory.Object,
                    new HtmlTestEncoder());
            }

            // TemplateRenderer will Contextualize this transient service.
            var innerHelper = (IHtmlHelper)new HtmlHelper(
                htmlGenerator,
                viewEngine,
                provider,
                new TestViewBufferScope(),
                new HtmlTestEncoder(),
                UrlEncoder.Default,
                JavaScriptEncoder.Default);

            if (innerHelperWrapper != null)
            {
                innerHelper = innerHelperWrapper(innerHelper);
            }

            var serviceProvider = new ServiceCollection()
               .AddSingleton(viewEngine)
               .AddSingleton(urlHelperFactory.Object)
               .AddSingleton(Mock.Of<IViewComponentHelper>())
               .AddSingleton(innerHelper)
               .AddSingleton<IViewBufferScope, TestViewBufferScope>()
               .BuildServiceProvider();

            httpContext.RequestServices = serviceProvider;

            var htmlHelper = new HtmlHelper<TModel>(
                htmlGenerator,
                viewEngine,
                provider,
                new TestViewBufferScope(),
                new HtmlTestEncoder(),
                UrlEncoder.Default,
                JavaScriptEncoder.Default);

            var viewContext = new ViewContext(
                actionContext,
                Mock.Of<IView>(),
                viewData,
                new TempDataDictionary(
                    httpContext,
                    Mock.Of<ITempDataProvider>()),
                new StringWriter(),
                options.HtmlHelperOptions);

            htmlHelper.Contextualize(viewContext);

            return htmlHelper;
        }

        public static string FormatOutput(IHtmlHelper helper, object model)
        {
            var modelExplorer = helper.MetadataProvider.GetModelExplorerForType(model.GetType(), model);
            return FormatOutput(modelExplorer);
        }

        private static ICompositeViewEngine CreateViewEngine()
        {
            var view = new Mock<IView>();
            view
                .Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
                .Callback(async (ViewContext v) =>
                {
                    view.ToString();
                    await v.Writer.WriteAsync(FormatOutput(v.ViewData.ModelExplorer));
                })
                .Returns(Task.FromResult(0));

            var viewEngine = new Mock<ICompositeViewEngine>(MockBehavior.Strict);
            viewEngine
                .Setup(v => v.GetView(/*executingFilePath*/ null, It.IsAny<string>(), /*isMainPage*/ false))
                .Returns(ViewEngineResult.NotFound("MyView", Enumerable.Empty<string>()))
                .Verifiable();
            viewEngine
                .Setup(v => v.FindView(It.IsAny<ActionContext>(), It.IsAny<string>(), /*isMainPage*/ false))
                .Returns(ViewEngineResult.Found("MyView", view.Object))
                .Verifiable();

            return viewEngine.Object;
        }

        private static string FormatOutput(ModelExplorer modelExplorer)
        {
            var metadata = modelExplorer.Metadata;
            return string.Format(
                CultureInfo.InvariantCulture,
                "Model = {0}, ModelType = {1}, PropertyName = {2}, SimpleDisplayText = {3}",
                modelExplorer.Model ?? "(null)",
                metadata.ModelType == null ? "(null)" : metadata.ModelType.FullName,
                metadata.PropertyName ?? "(null)",
                modelExplorer.GetSimpleDisplayText() ?? "(null)");
        }

        private static IUrlHelper CreateUrlHelper()
        {
            return Mock.Of<IUrlHelper>();
        }
    }
}
