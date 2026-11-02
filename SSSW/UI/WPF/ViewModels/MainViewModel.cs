// ============================================================================
//  MainViewModel.cs
//  ViewModel cho Main.xaml – shell chứa 2 tab: Step Component / Finished Goods.
//  Ngoài việc điều phối tab nào đang hiển thị trong body (ActiveContent), Main
//  giờ còn là nơi DUY NHẤT load Config (GlobalVariable.ConfigSystem) và kết nối
//  hardware (Barcode/RFID/Scale qua DeviceConnectionService) khi app khởi động —
//  trước đây việc này nằm trong ShotWeightViewModel.InitializeAsync(), gắn liền
//  với tab Step. Business logic cân (đọc giá trị RFID/Barcode/Scale khi có sự
//  kiện quét) vẫn nằm trong ShotWeightViewModel / ShotWeightFGViewModel.
//  Namespace : SSSW.UI.WPF.ViewModels
// ============================================================================
using AutoUpdaterDotNET;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SSSW.models;
using SSSW.modelss;
using SSSW.UI.WPF.Services;
using System.Windows;

namespace SSSW.UI.WPF.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IDbContextFactory<DbContextDogeWH> _dbFactory;
        private readonly DeviceConnectionService _deviceService;
        private readonly ILogger<MainViewModel> _logger;

        // RFID/operator identity dùng chung cho cả 2 tab Step/FG — xem OperatorSessionService
        // để biết lý do gộp về đây (chuyển tab không còn làm "mất" operator đã quét).
        public OperatorSessionService OperatorSession { get; }

        // Cờ đánh dấu người dùng chủ động bấm nút Update (phân biệt với auto-check ngầm) —
        // dùng để quyết định có hiện popup "Already up to date." hay im lặng. Copy nguyên vẹn
        // hành vi cũ từ ShotWeightViewModel.
        private bool _isUpdateClicked = false;

        // ── Cache: mỗi view (UserControl) chỉ được DI resolve MỘT LẦN rồi giữ lại,
        // không tạo mới mỗi lần chuyển tab — tránh mất dữ liệu cân dở của operator
        // (step đang chọn, giá trị đã scan nhưng chưa Save). ────────────────────
        private UIElement? _stepView;
        private UIElement? _fgView;

        // ── ACTIVE CONTENT (body của Main) ──────────────────────────────────
        private object? _activeContent;
        public object? ActiveContent
        {
            get => _activeContent;
            private set => SetProperty(ref _activeContent, value);
        }

        private bool _isStepActive;
        public bool IsStepActive
        {
            get => _isStepActive;
            private set => SetProperty(ref _isStepActive, value);
        }

        private bool _isFgActive;
        public bool IsFgActive
        {
            get => _isFgActive;
            private set => SetProperty(ref _isFgActive, value);
        }

        // ── COMMANDS ─────────────────────────────────────────────────────────
        public RelayCommand SelectStepCommand { get; }
        public RelayCommand SelectFgCommand { get; }

        // Hành động cấp APP (không riêng Step/FG) — chuyển từ ShotWeightViewModel lên đây để
        // cả 2 tab đều bấm được (xem comment ở Main.xaml, group ACTION ICON BUTTONS).
        public AsyncRelayCommand HydraCommand { get; }
        public RelayCommand SettingsCommand { get; }
        public RelayCommand UpdateCommand { get; }

        // ── WINDOW TITLE ─────────────────────────────────────────────────────
        // Tính DUY NHẤT 1 lần ở cấp Main, không phụ thuộc tab đang active — để
        // header tuyệt đối không đổi khi chuyển qua lại Step/FG. Trước đây bind
        // qua ActiveContent.DataContext.WindowTitle nên mỗi ViewModel tự tính
        // lại (async, tốc độ khác nhau tuỳ tab), khiến 2 tab hiện 2 giá trị khác
        // nhau tại cùng 1 thời điểm (tab chưa load xong DB vẫn còn hiện title
        // mặc định "SSSW – Scan and Scale Shot Weight").
        private string _windowTitle = "SSSW – Scan and Scale Shot Weight";
        public string WindowTitle
        {
            get => _windowTitle;
            private set => SetProperty(ref _windowTitle, value);
        }

        public MainViewModel(
            IServiceProvider serviceProvider,
            IDbContextFactory<DbContextDogeWH> dbFactory,
            DeviceConnectionService deviceService,
            OperatorSessionService operatorSession,
            ILogger<MainViewModel> logger)
        {
            _serviceProvider = serviceProvider;
            _dbFactory = dbFactory;
            _deviceService = deviceService;
            OperatorSession = operatorSession;
            _logger = logger;

            SelectStepCommand = new RelayCommand(SelectStep);
            SelectFgCommand = new RelayCommand(SelectFg);
            HydraCommand = new AsyncRelayCommand(GetDataHydraAsync);
            SettingsCommand = new RelayCommand(OpenSettings);
            UpdateCommand = new RelayCommand(CheckUpdate);

            // Gán ngay (đồng bộ) để bất kỳ ai đọc GlobalVariable.Devices trước khi
            // StartupAsync() chạy xong vẫn có tham chiếu hợp lệ (chỉ là driver bên trong
            // chưa kịp connect) — không bao giờ null sau dòng này.
            GlobalVariable.Devices = _deviceService;

            // Auto-updater — trước đây gắn trong ShotWeightViewModel.InitializeAsync(), nghĩa là
            // chỉ hook 1 lần cùng lúc Step tab load lần đầu (đúng vì Step luôn được mở đầu tiên) —
            // nay chuyển hẳn lên đây, đúng vị trí hơn vì đây là hạ tầng app-wide (event tĩnh của
            // AutoUpdaterDotNET), không riêng gì tab Step.
            AutoUpdater.RunUpdateAsAdmin = false;
            AutoUpdater.DownloadPath = Environment.CurrentDirectory;
            AutoUpdater.ApplicationExitEvent += AutoUpdater_ApplicationExitEvent;
            AutoUpdater.CheckForUpdateEvent += AutoUpdater_CheckForUpdateEvent;

            // KHÔNG tự bắn StartupAsync() ngay tại đây nữa (xem lý do trong XML doc của
            // StartupAsync() bên dưới) — Main.xaml.cs gọi vm.StartupAsync() trong sự kiện
            // Loaded của chính Window Main, đảm bảo message pump (Application.Run) đã chạy.
        }

        /// <summary>
        /// Chạy MỘT LẦN khi Main khởi tạo: load mesocomp/version → WindowTitle, load
        /// GlobalVariable.ConfigSystem từ FT608_Config (chuyển từ
        /// ShotWeightViewModel.InitializeAsync() lên đây), rồi kết nối hardware
        /// (Barcode/RFID/Scale qua DeviceConnectionService) bằng đúng config vừa load.
        /// Chỉ sau khi xong hết mới SelectStep() để mở tab mặc định — đảm bảo Step/FG
        /// không bao giờ thấy Config mặc định hay hardware chưa kết nối.
        ///
        /// QUAN TRỌNG — public, gọi từ Main.xaml.cs's Window.Loaded, KHÔNG tự gọi trong
        /// constructor của ViewModel này: constructor chạy trong lúc
        /// scope.ServiceProvider.GetRequiredService&lt;Main&gt;() ở Program.cs, tức là TRƯỚC
        /// khi wpfApp.Run(wpfWin) khởi động Dispatcher message pump. Vì các câu lệnh SQL bên
        /// dưới thường trả lời rất nhanh (vài ms trên mạng LAN nội bộ), toàn bộ StartupAsync()
        /// — kể cả SelectStep() trong finally — có thể chạy xong TRƯỚC KHI Application.Run()
        /// kịp gọi Show()/bơm message đầu tiên. Đây là race condition thật (đã tái hiện qua
        /// log: FT608 Config load thành công nhưng ShotWeightWindow.OnLoaded không bao giờ
        /// bắn — ActiveContent coi như được set nhưng cây visual chưa gắn vào PresentationSource
        /// nào, DevExpress/WPF init dở dang) khiến header hiện đúng title nhưng cả 2 tab đều
        /// không được check và body trống trơn — đúng triệu chứng người dùng báo cáo, xảy ra
        /// không ổn định tuỳ tốc độ DB. Gọi từ Window.Loaded đảm bảo Show()/message pump đã
        /// chạy trước khi bất kỳ UserControl con nào được resolve/gắn vào ActiveContent.
        /// </summary>
        public async Task StartupAsync()
        {
            try
            {
                using var db = _dbFactory.CreateDbContext();

                // sp_MaterialGetCompanyName/sp_MaterialGetMesoyear là stored procedure (EXEC),
                // không phải SELECT thuần nên không composable — .FirstOrDefaultAsync() sẽ khiến
                // EF Core cố compose thêm TOP(1) và ném InvalidOperationException lúc chạy. Phải
                // dùng .AsEnumerable() (client-eval, không compose), chạy trong Task.Run để giữ async.
                var (mesocomp, mesoYear) = await Task.Run(() =>
                {
                    var comp = db.Database.SqlQueryRaw<string>("sp_MaterialGetCompanyName")
                        .AsEnumerable().FirstOrDefault() ?? string.Empty;
                    var year = db.Database.SqlQueryRaw<int>("sp_MaterialGetMesoyear")
                        .AsEnumerable().FirstOrDefault();
                    return (comp, year);
                });

                var location = mesocomp switch
                {
                    "VNT1" => "fVN",
                    "FKV" => "fKV",
                    "FTT1" => "fFT",
                    "05FI" => "fIN",
                    "fGE" => "fGE",
                    _ => "Unknown"
                };
                var appVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "N/A";

                if (Enum.TryParse<EnumLocation>(location, true, out var loc))
                    WindowTitle = $"{loc} – Scan and Scale Shot Weight (SSSW) – Ver-{appVersion}";

                // ── Config (moved verbatim from ShotWeightViewModel.InitializeAsync()) ──
                var configData = await db.FT608s
                    .FirstOrDefaultAsync(x => x.c000 == Environment.MachineName);
                if (configData != null)
                {
                    GlobalVariable.ConfigSystem =
                        JsonConvert.DeserializeObject<ConfigModel>(configData.c001) ?? new ConfigModel();
                }
                else
                {
                    await db.FT608s.AddAsync(new FT608_Config
                    {
                        Id = Guid.NewGuid(),
                        c000 = Environment.MachineName,
                        c001 = JsonConvert.SerializeObject(new ConfigModel()),
                        Mesoyear = mesoYear,
                        Mesocomp = mesocomp,
                        CreatedMachine = Environment.MachineName,
                        CreatedDate = DateTime.Now
                    });
                    await db.SaveChangesAsync();
                }

                // ── Hardware — PHẢI chạy sau khi Config đã load ở trên, nếu không sẽ
                // connect bằng config mặc định (IP/COM sai). ─────────────────────────
                _deviceService.EnsureInitialized();

                // OperatorSession phải Attach() SAU khi hardware đã EnsureInitialized() để backfill
                // đúng RfidStatus hiện tại (không phải Unknown mặc định), rồi subscribe sự kiện quét
                // thẻ vật lý — chạy 1 lần duy nhất ở cấp Main, dùng chung cho cả Step lẫn FG.
                OperatorSession.Attach(_deviceService);
            }
            catch (Exception ex)
            {
                // DB/hardware chưa sẵn sàng lúc khởi động (VD: mất kết nối) — giữ nguyên
                // Config/title mặc định thay vì crash app; operator vẫn thấy được UI và
                // có thể Reload sau khi hạ tầng sẵn sàng trở lại. TRƯỚC ĐÂY catch rỗng, nuốt
                // hẳn exception không log gì — không thể chẩn đoán khi có sự cố (đã log để
                // Logs/SSSW_Log_*.txt luôn có dấu vết).
                _logger.LogError(ex, "MainViewModel.StartupAsync: lỗi load Config/kết nối hardware lúc khởi động");
            }
            finally
            {
                // Luôn mở tab Step mặc định dù Config/hardware load thành công hay không —
                // không được để app kẹt màn hình trống nếu DB tạm thời lỗi lúc khởi động.
                // Bọc riêng try/catch: SelectStep() ở đây chạy bên trong 1 Task fire-and-forget
                // (StartupAsync() được gọi không await từ Main.xaml.cs) — nếu bản thân SelectStep()
                // ném lỗi (VD DI resolve ShotWeightWindow thất bại) mà không bắt ở đây, exception
                // sẽ biến thành Unobserved Task Exception và bị nuốt hoàn toàn im lặng (không crash,
                // không log) — để lại đúng triệu chứng "màn hình trống, không tab nào được chọn,
                // không có gì trong log" mà không cách nào chẩn đoán được.
                try
                {
                    SelectStep();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "MainViewModel.StartupAsync: SelectStep() mặc định thất bại — màn hình sẽ trống");
                }
            }
        }

        private void SelectStep()
        {
            _stepView ??= _serviceProvider.GetRequiredService<ShotWeightWindow>();
            ActiveContent = _stepView;
            IsStepActive = true;
            IsFgActive = false;
        }

        private void SelectFg()
        {
            _fgView ??= _serviceProvider.GetRequiredService<ShotWeightFGWindow>();
            ActiveContent = _fgView;
            IsStepActive = false;
            IsFgActive = true;
        }

        /// <summary>Gọi ReloadCommand của ViewModel đang active (Step hoặc FG) sau một thao tác
        /// cấp Main làm thay đổi dữ liệu chung (Hydra sync, Settings) — tab nào đang mở tự cập
        /// nhật lại grid của nó mà code ở đây không cần biết đang là Step hay FG.</summary>
        private void RefreshActiveContent()
        {
            switch ((ActiveContent as FrameworkElement)?.DataContext)
            {
                case ShotWeightViewModel svm:
                    svm.ReloadCommand.Execute(null);
                    break;
                case ShotWeightFGViewModel fvm:
                    fvm.ReloadCommand.Execute(null);
                    break;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  HYDRA SYNC — copy nguyên vẹn từ ShotWeightViewModel.GetDataHydraAsync(), chỉ đổi
        //  bước reload cuối (LoadDataAsync() của riêng Step) thành RefreshActiveContent()
        //  (áp dụng cho tab đang mở, Step hoặc FG) vì FT601 là dữ liệu dùng chung cho cả 2.
        // ─────────────────────────────────────────────────────────────────────
        private async Task GetDataHydraAsync(object? _)
        {
            try
            {
                using var db = _dbFactory.CreateDbContext();
                var hydraItemDetails = await db.Database
                    .SqlQueryRaw<HydraItemDetailModel>("sp_GetFullStepItemHydraIsRun")
                    .AsNoTracking().ToListAsync();
                hydraItemDetails = hydraItemDetails
                    .OrderBy(x => x.FGItemCode).ThenBy(x => x.StepIndex).ToList();

                if (!hydraItemDetails.Any()) return;

                var ft601s = new List<FT601>();
                var now = DateTime.Now;
                var machine = Environment.MachineName;
                var toInsert = hydraItemDetails.Where(d =>
                    !db.FT601s.Any(ft => ft.C004 == d.StepItemCode &&
                                         ft.C015 == d.Machine &&
                                         ft.C018 == d.OrderHydraNum &&
                                         ft.Actived == true)).ToList();

                foreach (var item in toInsert)
                    ft601s.Add(new FT601
                    {
                        Id = Guid.NewGuid(),
                        C000 = item.HydraOrderType,
                        C001 = item.Location == "Sample"
                                     ? EnumSampleLocation.Sample : EnumSampleLocation.Production,
                        C002 = item.Size,
                        C003 = item.MainName,
                        C004 = item.StepItemCode,
                        C005 = item.StepItemName,
                        C006 = item.Artikel,
                        C007 = item.FGItemCode,
                        C008 = item.FGItemName,
                        C009 = item.StepIndexHydra,
                        C010 = item.StepIndex,
                        C011 = item.ColorCode,
                        C012 = item.ColorName,
                        C013 = item.ArticlePairShot,
                        C014 = item.MoldPairShot,
                        C015 = item.Machine,
                        C016 = item.MachineGroup,
                        C017 = false,
                        C018 = item.OrderHydraNum,
                        C019 = item.MoldId,
                        C020 = item.MainCode,
                        Actived = true,
                        CreatedMachine = machine,
                        CreatedDate = now,
                        Mesoyear = item.MesoYear,
                        Mesocomp = item.MesoComp
                    });

                if (ft601s.Any())
                {
                    await db.FT601s.AddRangeAsync(ft601s);
                    await db.SaveChangesAsync();
                }

                RefreshActiveContent();
            }
            catch (Exception ex) { _logger.LogError(ex, "GetDataHydra error"); }
        }

        /// <summary>Mở màn hình Settings (frmUpdateMasterData) — dùng chung cho cả Step/FG
        /// (master data không tách riêng theo tab). Sau khi đóng, reload tab đang active vì
        /// dữ liệu master vừa sửa có thể ảnh hưởng danh sách đang hiển thị.</summary>
        private void OpenSettings()
        {
            var nf = _serviceProvider.GetRequiredService<frmUpdateMasterData>();
            nf.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            nf.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            nf.ShowDialog();
            RefreshActiveContent();
        }

        /// <summary>Mở dialog nhập tay Employee ID (frmRfidInput) khi không quét được thẻ RFID —
        /// bấm vào label số thẻ/tên nhân viên trên header (xem Main.xaml.cs's
        /// _tbEmployee_MouseLeftButtonDown). Dùng chung cho cả 2 tab vì OperatorSession dùng
        /// chung; trước đây mỗi ViewModel Step/FG tự có 1 bản OpenRfidInputDialog() giống hệt
        /// nhau, nay chỉ còn 1 bản duy nhất ở đây.</summary>
        public void OpenRfidInputDialog()
        {
            var dlg = _serviceProvider.GetRequiredService<frmRfidInput>();
            dlg.Owner = System.Windows.Application.Current.MainWindow;
            dlg.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                OperatorSession.OnRfidValueChanged(dlg.ResultRfidCode);
            }
        }

        private void CheckUpdate()
        {
            try
            {
                _isUpdateClicked = true;
                AutoUpdater.Start(GlobalVariable.ConfigSystem.UpdatePath);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message, "Error",
                    (System.Windows.Forms.MessageBoxButtons)MessageBoxButton.OK,
                    (System.Windows.Forms.MessageBoxIcon)MessageBoxImage.Error);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  AUTO-UPDATER
        // ─────────────────────────────────────────────────────────────────────
        private async void AutoUpdater_ApplicationExitEvent()
        {
            System.Windows.Application.Current.MainWindow.Title = "Closing…";
            await Task.Delay(3000);
            System.Windows.Application.Current.Shutdown();
        }

        private async void AutoUpdater_CheckForUpdateEvent(UpdateInfoEventArgs args)
        {
            if (args.IsUpdateAvailable)
            {
                var res = System.Windows.Forms.MessageBox.Show(
                    $"New version available: {args.CurrentVersion}. Update now?",
                    "Update",
                    (System.Windows.Forms.MessageBoxButtons)MessageBoxButton.YesNo,
                    (System.Windows.Forms.MessageBoxIcon)MessageBoxImage.Information);
                if (res == System.Windows.Forms.DialogResult.Yes)
                {
                    await Task.Delay(3000);
                    try
                    {
                        if (AutoUpdater.DownloadUpdate(args))
                            System.Windows.Application.Current.Shutdown();
                    }
                    catch (Exception ex)
                    {
                        System.Windows.Forms.MessageBox.Show(ex.Message, ex.GetType().ToString(),
                            (System.Windows.Forms.MessageBoxButtons)MessageBoxButton.OK,
                            (System.Windows.Forms.MessageBoxIcon)MessageBoxImage.Error);
                    }
                }
            }
            else if (_isUpdateClicked)
            {
                System.Windows.Forms.MessageBox.Show("Already up to date.", "Information",
                    (System.Windows.Forms.MessageBoxButtons)MessageBoxButton.OK,
                    (System.Windows.Forms.MessageBoxIcon)MessageBoxImage.Information);
            }
        }
    }
}
