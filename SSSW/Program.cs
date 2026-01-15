using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ScanAndScale.Helper;
using Serilog;
using Serilog.Events;
using SSSW.modelss;
using System.IO;
using System.Reflection;

namespace SSSW
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();


            // Base path: cùng thư mục với file .exe
            var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

            var exeDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            // Tạo thư mục Logs nếu chưa có
            var logDirectory = Path.Combine(exeDirectory, "Logs");
            Directory.CreateDirectory(logDirectory);

            // Build cấu hình từ appsettings.json
            var config = new ConfigurationBuilder()
                .SetBasePath(exeDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();


            // Decrypt connection once to configure DbContext
            var cfgForDb = config.GetSection("ConnectionStrings")["SSSW"];
            GlobalVariable.ConStringSSSW = EncodeMD5.DecryptString(cfgForDb, "ITFramasBDVN");

            // Định dạng tên file: yyyyMMdd_OMS_KerryAPI_Service_Log_.txt
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                //.MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                //.MinimumLevel.Override("SSSW", LogEventLevel.Error) // namespace/project của bạn
                .WriteTo.Console()
                .WriteTo.File(
                    Path.Combine(logDirectory, "SSSW_Log_.txt"), // Serilog sẽ thay {Date} bằng yyyyMMdd
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                    fileSizeLimitBytes: 10_000_000//giới hạn 10Mb/1file
                )
                .CreateLogger();

            // Dựng Host + DI
            using var host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
                .UseContentRoot(basePath) // đảm bảo đường dẫn đúng khi chạy
                .UseSerilog() // Sử dụng Serilog
                .ConfigureServices(services =>
                {
                    // Đăng ký DbContextFactory: phù hợp WinForms (lifetime ngắn, thread-safe)
                    services.AddDbContextFactory<DbContextDogeWH>(options =>
                        options.UseSqlServer(GlobalVariable.ConStringSSSW));
                    // Đăng ký các Form để DI resolve được
                    services.AddTransient<frmShotWeightScale>();
                    services.AddTransient<frmMainView>();
                    services.AddTransient<frmUpdateMasterData>();
                })
                .Build();

            // Lấy Form chính từ DI và chạy
            using var scope = host.Services.CreateScope();

            // Nếu Form chính là frmShotWeightScale:
            var mainForm = scope.ServiceProvider.GetRequiredService<frmShotWeightScale>();

            // Nếu muốn chạy Form1 làm form chính, thay dòng trên bằng:
            // var mainForm = scope.ServiceProvider.GetRequiredService<Form1>();

            Application.Run(mainForm);

        }
    }
}