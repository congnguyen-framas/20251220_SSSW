using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Serilog;
using ShotweightEnterprise1000Users.Infrastructure.Data;
using ShotweightEnterprise1000Users.Infrastructure.Repositories;
using ShotweightEnterprise1000Users.Application.Interfaces;
using ShotweightEnterprise1000Users.Presentation.ViewModels;
using ShotweightEnterprise1000Users.Presentation.Views;
using System.IO;
using System.Windows;

namespace ShotweightEnterprise1000Users
{
    public partial class App : System.Windows.Application
    {
        private ServiceProvider _provider;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            Log.Logger = new LoggerConfiguration()
                .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            var services = new ServiceCollection();

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(config.GetConnectionString("DefaultConnection")));

            services.AddScoped<IStepRepository, StepRepository>();
            services.AddScoped<MainViewModel>();
            services.AddScoped<MainWindow>();

            _provider = services.BuildServiceProvider();

            var mainWindow = _provider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
    }
}