using Microsoft.Extensions.Configuration;
using ScanAndScale.Helper;
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

            var exeDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            // Tạo thư mục Logs nếu chưa có
            var logDirectory = Path.Combine(exeDirectory, "Logs");
            Directory.CreateDirectory(logDirectory);


            // Load cấu hình từ appsettings.json
            var config = new ConfigurationBuilder()
                .SetBasePath(exeDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Decrypt connection once to configure DbContext
            var cfgForDb = config.GetSection("ConnectionStrings")["DogeWH"];
            GlobalVariable.ConStringDogeWH = EncodeMD5.DecryptString(cfgForDb, "ITFramasBDVN");

            Application.Run(new Form1());
        }
    }
}