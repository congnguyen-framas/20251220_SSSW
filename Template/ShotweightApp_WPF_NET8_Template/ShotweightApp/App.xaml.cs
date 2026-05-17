
using Microsoft.Extensions.DependencyInjection;
using ShotweightApp.Core.Services;
using ShotweightApp.ViewModels;
using ShotweightApp.Views;
using System;
using System.Windows;

namespace ShotweightApp
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            var services = new ServiceCollection();

            services.AddSingleton<WeightService>();
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<MainWindow>();

            Services = services.BuildServiceProvider();

            var mainWindow = Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
    }
}
