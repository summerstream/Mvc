// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Mvc.Infrastructure;
using Microsoft.AspNet.TestHost;
using Microsoft.AspNet.Testing;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.AspNet.Hosting.Internal;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class MvcTestFixture : IDisposable
    {
        private readonly TestServer _server;

        public MvcTestFixture(object startupInstance)
        {
            var startupTypeInfo = startupInstance.GetType().GetTypeInfo();
            var configureApplication = (Action<IApplicationBuilder>)startupTypeInfo
                .DeclaredMethods
                .FirstOrDefault(m => m.Name == "Configure" && m.GetParameters().Length == 1)
                ?.CreateDelegate(typeof(Action<IApplicationBuilder>), startupInstance);
            if (configureApplication == null)
            {
                var configureWithLogger = (Action<IApplicationBuilder, ILoggerFactory>)startupTypeInfo
                    .DeclaredMethods
                    .FirstOrDefault(m => m.Name == "Configure" && m.GetParameters().Length == 2)
                    ?.CreateDelegate(typeof(Action<IApplicationBuilder, ILoggerFactory>), startupInstance);
                Debug.Assert(configureWithLogger != null);

                configureApplication = application => configureWithLogger(application, NullLoggerFactory.Instance);
            }

            var configureServices = (Action<IServiceCollection>)startupTypeInfo
                .DeclaredMethods
                .First(m => m.Name == "ConfigureServices")
                .CreateDelegate(typeof(Action<IServiceCollection>), startupInstance);

            // RequestLocalizationOptions saves the current culture when constructed, potentially changing response
            // localization i.e. RequestLocalizationMiddleware behavior. Ensure the saved culture
            // (DefaultRequestCulture) is consistent regardless of system configuration or personal preferences.
            using (new CultureReplacer())
            {
                var builder = new WebApplicationBuilder()
                    .Configure(configureApplication)
                    .ConfigureServices(
                        services => InitializeServices(startupTypeInfo.Assembly, services, configureServices));

                _server = new TestServer(builder);
            }

            Client = _server.CreateClient();
            Client.BaseAddress = new Uri("http://localhost");
        }

        public HttpClient Client { get; }

        public void Dispose()
        {
            Client.Dispose();
            _server.Dispose();
        }

        protected virtual void AddAdditionalServices(IServiceCollection services)
        {
        }

        private void InitializeServices(
            Assembly startupAssembly,
            IServiceCollection services,
            Action<IServiceCollection> configureServices)
        {
            var libraryManager = PlatformServices.Default.LibraryManager;

            // When an application executes in a regular context, the application base path points to the root
            // directory where the application is located, for example .../samples/MvcSample.Web. However, when
            // executing an application as part of a test, the ApplicationBasePath of the IApplicationEnvironment
            // points to the root folder of the test project.
            // To compensate, we need to calculate the correct project path and override the application
            // environment value so that components like the view engine work properly in the context of the test.
            var applicationName = startupAssembly.GetName().Name;
            var library = libraryManager.GetLibrary(applicationName);
            var applicationRoot = Path.GetDirectoryName(library.Path);

            var applicationEnvironment = PlatformServices.Default.Application;

            services.AddSingleton<IApplicationEnvironment>(
                new TestApplicationEnvironment(applicationEnvironment, applicationName, applicationRoot));

            var hostingEnvironment = new HostingEnvironment();
            hostingEnvironment.Initialize(applicationRoot, new WebApplicationOptions(), configuration: null);
            services.AddSingleton<IHostingEnvironment>(hostingEnvironment);

            // Inject a custom assembly provider. Overrides AddMvc() because that uses TryAdd().
            var assemblyProvider = new StaticAssemblyProvider();
            assemblyProvider.CandidateAssemblies.Add(startupAssembly);
            services.AddSingleton<IAssemblyProvider>(assemblyProvider);

            AddAdditionalServices(services);

            if (configureServices != null)
            {
                configureServices(services);
            }
        }
    }
}
