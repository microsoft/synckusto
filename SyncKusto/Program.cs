// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Windows.Forms;
using System.Drawing;
using Microsoft.Extensions.DependencyInjection;
using SyncKusto.Abstractions;
using SyncKusto.Core.Abstractions;
using SyncKusto.Core.Configuration;
using SyncKusto.Core.Services;
using SyncKusto.Services;
using SyncKusto.ErrorHandling;

namespace SyncKusto
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // Configure DI container
            var services = new ServiceCollection();
            
            // Core services
            services.AddSingleton<ISchemaComparisonService, SchemaComparisonService>();
            services.AddSingleton<ISchemaSyncService, SchemaSyncService>();
            services.AddSingleton<ISchemaValidationService, SchemaValidationService>();
            
            // Error resolvers
            services.AddSingleton<IErrorMessageResolver>(provider => ErrorMessageResolverFactory.CreateDefault());
            
            // Settings - Register provider and load settings
            services.AddSingleton<ISettingsProvider, JsonFileSettingsProvider>();
            services.AddSingleton(provider =>
            {
                var settingsProvider = provider.GetRequiredService<ISettingsProvider>();
                return SyncKustoSettingsFactory.CreateFromProvider(settingsProvider);
            });
            
            // Repository factory
            services.AddSingleton<SchemaRepositoryFactory>();
            
            // Presenters
            services.AddSingleton<IMainFormPresenter, MainFormPresenter>();
            
            // Build service provider
            var serviceProvider = services.BuildServiceProvider();

            // Revert some changes that came from the upgrade to .NET 8 regarding high DPI settings
            Application.SetHighDpiMode(HighDpiMode.DpiUnaware);
            Application.SetDefaultFont(new Font("Microsoft Sans Serif", 8.25f));

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            // Create MainForm with all dependencies
            var mainForm = new MainForm(
                serviceProvider.GetRequiredService<IMainFormPresenter>(),
                serviceProvider.GetRequiredService<IErrorMessageResolver>(),
                serviceProvider.GetRequiredService<ISettingsProvider>(),
                serviceProvider.GetRequiredService<SyncKustoSettings>());
            
            Application.Run(mainForm);
        }
    }
}