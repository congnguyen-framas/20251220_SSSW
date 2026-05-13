// ============================================================================
//  ShotWeightWindow.xaml.cs
//  WPF port of frmShotWeightScaleV2 – Shot Weight Scale Station (Option A)
//  Namespace : SSSW.UI.WPF
// ============================================================================
using AutoUpdaterDotNET;
using DevExpress.DataProcessing.InMemoryDataProcessor;
using DevExpress.Xpf.Grid.LookUp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ScanAndScale.Driver;
using ScanAndScale.Helper;
using SSSW.models;
using SSSW.modelss;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using System.Windows.Media;
using Brushes = System.Windows.Media.Brushes;
using Button = System.Windows.Controls.Button;
using Color = System.Windows.Media.Color;

namespace SSSW.UI.WPF
{
  
    // ─── Value converters ────────────────────────────────────────────────────────

    /// <summary>Maps ToleranceCategory to a background Brush.</summary>
    public class ToleranceToBrushConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ToleranceCategory t)
                return t switch
                {
                    ToleranceCategory.Ok   => new SolidColorBrush(Color.FromRgb(212, 247, 220)),
                    ToleranceCategory.Warn => new SolidColorBrush(Color.FromRgb(255, 243, 205)),
                    ToleranceCategory.Err  => new SolidColorBrush(Color.FromRgb(253, 232, 232)),
                    _                      => new SolidColorBrush(Color.FromRgb(245, 245, 245))
                };
            return Brushes.White;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => System.Windows.Data.Binding.DoNothing;
    }

    /// <summary>Maps ToleranceCategory to a foreground Brush.</summary>
    public class ToleranceToForeConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ToleranceCategory t)
                return t switch
                {
                    ToleranceCategory.Ok   => new SolidColorBrush(Color.FromRgb(0, 100, 0)),
                    ToleranceCategory.Warn => new SolidColorBrush(Color.FromRgb(130, 80, 0)),
                    ToleranceCategory.Err  => new SolidColorBrush(Color.FromRgb(160, 0, 0)),
                    _                      => new SolidColorBrush(Color.FromRgb(80, 80, 80))
                };
            return Brushes.Black;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => System.Windows.Data.Binding.DoNothing;
    }

    // ─── Tolerance enum / ReferenceRow model ─────────────────────────────────────

    public enum ToleranceCategory { Idle, Ok, Warn, Err }

    public class ReferenceRow
    {
        public int    No        { get; set; }
        public string FieldName { get; set; } = "";
        public string Unit      { get; set; } = "";
        public double? Std      { get; set; }
        public double? Actual   { get; set; }

        public string StdDisplay    => Std.HasValue    ? Std.Value.ToString("F2")    : "—";
        public string ActualDisplay => (Actual.HasValue && Actual > 0) ? Actual.Value.ToString("F2") : "—";
        public string DeltaDisplay  => Delta.HasValue  ? ((Delta >= 0 ? "+" : "") + Delta.Value.ToString("F2")) : "—";

        public double? Delta => (Actual.HasValue && Std.HasValue && Std.Value != 0)
            ? Math.Round(Actual.Value - Std.Value, 3) : (double?)null;

        public double? DeltaPct => (Actual.HasValue && Std.HasValue && Std.Value != 0)
            ? (Actual.Value - Std.Value) / Std.Value * 100.0 : (double?)null;

        public ToleranceCategory Tolerance => DeltaPct == null ? ToleranceCategory.Idle :
            Math.Abs(DeltaPct.Value) <= 1.0 ? ToleranceCategory.Ok  :
            Math.Abs(DeltaPct.Value) <= 3.0 ? ToleranceCategory.Warn : ToleranceCategory.Err;
    }

    // ─── Window ───────────────────────────────────────────────────────────────────

    public partial class ShotWeightWindow : System.Windows.Window
    {
        // ── Win32 drag ──────────────────────────────────────────────────────────
        [DllImport("user32.dll")] private static extern bool ReleaseCapture();
        [DllImport("user32.dll")] private static extern int  SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        // ── DI ──────────────────────────────────────────────────────────────────
        private readonly IServiceProvider                   _serviceProvider;
        private readonly IDbContextFactory<DbContextDogeWH> _dbFactory;
        private readonly ILogger<ShotWeightWindow>          _logger;
        private CancellationTokenSource?                    _loadCts;

        // ── Hardware controls (WinForms) ─────────────────────────────────────────
        private BarcodeButtonEdit? _scanBarcode;
        private RFIDButtonEdit?    _txtRFIDCode;
        private ScaleButtonEdit?   _scaleCtrl;

        // ── Domain state (mirror of V2) ──────────────────────────────────────────
        public  List<FT601>            _dataHydra                = new();
        private List<FT601>            _dataHydraMultiSizeOfMold = new();
        private bool                   _newScale                 = true;
        public  FT601                  _stepItemCodeScale        = new();
        private bool                   isUpdateClicked           = false;

        private List<BomWinlineModel>  _allStepsFG               = new();
        private List<FT600>            _scaledDataPreviousStep   = new();
        private List<FT600>            _scaleData                = new();
        private List<FT600>            _scaleDataFinal           = new();
        private FT600                  _rowSelected              = new();

        private double                 _scaleValue               = 0;
        private string                 _mesocomp                 = string.Empty;
        private int                    _mesoYear                 = 0;

        private bool                   _isRunner                 = true;
        private double?                _articlePaisShotFinaly    = 0;
        private double                 _percentOfUsage           = 0;
        private string?                _remarkFinal              = string.Empty;
        private List<StepSelectModel>  _allStepCodeMaster        = new();
        private StepSelectModel _selectedStepItem;

        public  FT601                  _stepSelected             = new();
        private string                 _qrCodeScan               = string.Empty;
        private FT606_Label            _labelInfo                = new();

        private string                 _employeeCode             = string.Empty;
        private FT029_Operator_RFID    _operatorInfo             = new();
        private List<HydraItemDetailModel> _hydraItemDetails     = new();

        private bool                   _allowPartitionAdjustment = false;
        private bool                   _suppress                 = false;
        private bool                   _historyExpanded          = false;

        // ── Option-A state ──────────────────────────────────────────────────────
        private List<ReferenceRow> _referenceRows = new();
        private FT600              _stdRow        = new();
        private List<FT600>        _refHistory    = new();

        // ── ObservableCollections ────────────────────────────────────────────────
        private ObservableCollection<FT600>        _stepsCollection   = new();
        private ObservableCollection<FT600>        _historyCollection = new();
        private ObservableCollection<ReferenceRow> _refRowsCollection = new();

        // =====================================================================
        //  CONSTRUCTORS
        // =====================================================================

        public ShotWeightWindow(
            IDbContextFactory<DbContextDogeWH> dbFactory,
            IServiceProvider serviceProvider,
            ILogger<ShotWeightWindow> logger) : this()
        {
            _dbFactory       = dbFactory;
            _serviceProvider = serviceProvider;
            _logger          = logger;
        }

        public ShotWeightWindow()
        {
            InitializeComponent();
            Loaded  += OnLoaded;
            Closing += OnClosing;
        }

        // =====================================================================
        //  LOADED
        // =====================================================================
        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Auto-updater wiring
            AutoUpdater.RunUpdateAsAdmin = false;
            AutoUpdater.DownloadPath     = Environment.CurrentDirectory;
            AutoUpdater.ApplicationExitEvent  += AutoUpdater_ApplicationExitEvent;
            AutoUpdater.CheckForUpdateEvent   += AutoUpdater_CheckForUpdateEvent;

            // Bind grids
            dgTotalSteps.ItemsSource = _stepsCollection;
            dgHistory.ItemsSource    = _historyCollection;
            dgRefValues.ItemsSource  = _refRowsCollection;

            // Attach WinForms hardware controls via WindowsFormsHost
            AttachHardwareControls();

            // Load config + master data
            using var dbContext = _dbFactory.CreateDbContext();
            _mesocomp = dbContext.Database.SqlQueryRaw<string>("sp_MaterialGetCompanyName")
                        .AsEnumerable().FirstOrDefault() ?? string.Empty;
            _mesoYear = dbContext.Database.SqlQueryRaw<int>("sp_MaterialGetMesoyear")
                        .AsEnumerable().FirstOrDefault();

            var location = _mesocomp switch
            {
                "VNT1" => "fVN", "FKV" => "fKV", "FTT1" => "fFT",
                "05FI" => "fIN", "fGE" => "fGE", _       => "Unknown"
            };
            if (Enum.TryParse<EnumLocation>(location, true, out var loc))
                tbWindowTitle.Text = $"{loc} – IT Shotweight Station";

            // Config
            var configData = await dbContext.FT608s
                .FirstOrDefaultAsync(x => x.c000 == Environment.MachineName);
            if (configData != null)
            {
                GlobalVariable.ConfigSystem =
                    JsonConvert.DeserializeObject<ConfigModel>(configData.c001) ?? new ConfigModel();
            }
            else
            {
                await dbContext.FT608s.AddAsync(new FT608_Config
                {
                    Id             = Guid.NewGuid(),
                    c000           = Environment.MachineName,
                    c001           = JsonConvert.SerializeObject(new ConfigModel()),
                    Mesoyear       = _mesoYear,
                    Mesocomp       = _mesocomp,
                    CreatedMachine = Environment.MachineName,
                    CreatedDate    = DateTime.Now
                });
                await dbContext.SaveChangesAsync();
            }

            // Apply hardware config
            if (_scanBarcode != null)
                _scanBarcode.Config = GlobalVariable.ConfigSystem.Scanner;
            if (_txtRFIDCode != null)
                _txtRFIDCode.Config = GlobalVariable.ConfigSystem.RFID;
            if (_scaleCtrl != null)
            {
                _scaleCtrl.Config          = GlobalVariable.ConfigSystem.Scale;
                _scaleCtrl.EnableReadScale = GlobalVariable.ConfigSystem.EnableReadScale == true;
            }

            // Initialise defaults
            tbUsagePct.Text  = GlobalVariable.ConfigSystem.PercentOfUserNonWoven.ToString();
            _percentOfUsage  = GlobalVariable.ConfigSystem.PercentOfUserNonWoven;

            // Bind step selector
            cbStepName.ItemsSource    = _allStepCodeMaster;
            //cbStepName.DisplayMemberPath = nameof(StepSelectModel.StepItemName);

            await LoadDataAsync();
        }

        // ── Attach WinForms hardware controls ────────────────────────────────
        private void AttachHardwareControls()
        {
            // Barcode
            _scanBarcode = new BarcodeButtonEdit();
            _scanBarcode.DataValueChanged += ScanBarcode_DataValueChanged;
            var barcodeWfh = new WindowsFormsHost { Child = _scanBarcode };
            barcodeHost.Child = barcodeWfh;

            // RFID
            _txtRFIDCode = new RFIDButtonEdit();
            _txtRFIDCode.DataValueChanged += RFIDCode_DataValueChanged;
            var rfidWfh = new WindowsFormsHost { Child = _txtRFIDCode };
            rfidHost.Child = rfidWfh;

            // Scale
            _scaleCtrl = new ScaleButtonEdit();
            _scaleCtrl.DataValueChanged += Scale_DataValueChanged;
            var scaleWfh = new WindowsFormsHost { Child = _scaleCtrl };
            scaleHost.Child = scaleWfh;
        }

        // =====================================================================
        //  CLOSING
        // =====================================================================
        private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _loadCts?.Cancel();
            if (_scanBarcode != null) _scanBarcode.DataValueChanged -= ScanBarcode_DataValueChanged;
            if (_txtRFIDCode != null) _txtRFIDCode.DataValueChanged -= RFIDCode_DataValueChanged;
            if (_scaleCtrl   != null) _scaleCtrl.DataValueChanged   -= Scale_DataValueChanged;
        }

        // =====================================================================
        //  LOAD MASTER DATA
        // =====================================================================
        private async Task LoadDataAsync(TimeSpan? timeout = null)
        {
            timeout ??= TimeSpan.FromSeconds(30);
            _loadCts?.Cancel();
            _loadCts?.Dispose();
            _loadCts = new CancellationTokenSource();

            using var timeoutCts = new CancellationTokenSource(timeout.Value);
            using var linkedCts  = CancellationTokenSource
                .CreateLinkedTokenSource(_loadCts.Token, timeoutCts.Token);
            var token = linkedCts.Token;

            overlayPanel.Visibility = Visibility.Visible;
            btnCancelLoad.IsEnabled  = true;
            try
            {
                var data = await Task.Run(async () =>
                {
                    token.ThrowIfCancellationRequested();
                    using var db = _dbFactory.CreateDbContext();
                    return await db.FT601s
                        .Where(x => x.C021 == true && x.Mesoyear == _mesoYear)
                        .ToListAsync(token);
                }, token);

                _dataHydra = data;
                _allStepCodeMaster = _dataHydra.Select(x => new StepSelectModel
                {
                    StepItemCode     = x.C004,
                    StepItemName     = x.C005,
                    Size             = x.C002,
                    ArticlePairsShot = x.C013,
                    MoldPairsShot    = x.C014,
                    Machine          = x.C015,
                    HydraOrderNo     = x.C018,
                    FT601Id          = x.Id
                }).Distinct().ToList();

                Dispatcher.Invoke(() =>
                {
                    cbStepName.ItemsSource = null;
                    cbStepName.ItemsSource = _allStepCodeMaster;
                });
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Load data failure:\n{ex.Message}", "Load data",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                overlayPanel.Visibility = Visibility.Collapsed;
                btnCancelLoad.IsEnabled  = false;
            }
        }

        // =====================================================================
        //  AUTO-UPDATER
        // =====================================================================
        private async void AutoUpdater_ApplicationExitEvent()
        {
            Title = "Closing…";
            await Task.Delay(3000);
            System.Windows.Application.Current.Shutdown();
        }

        private async void AutoUpdater_CheckForUpdateEvent(UpdateInfoEventArgs args)
        {
            if (args.IsUpdateAvailable)
            {
                var res = System.Windows.MessageBox.Show(
                    $"New version available: {args.CurrentVersion}. Update now?",
                    "Update", MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (res == MessageBoxResult.Yes)
                {
                    await Task.Delay(3000);
                    try
                    {
                        if (AutoUpdater.DownloadUpdate(args))
                            System.Windows.Application.Current.Shutdown();
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show(ex.Message, ex.GetType().ToString(),
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else if (isUpdateClicked)
            {
                System.Windows.MessageBox.Show("Already up to date.", "Information",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // =====================================================================
        //  TITLE BAR
        // =====================================================================
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                ReleaseCapture();
                SendMessage(new System.Windows.Interop.WindowInteropHelper(this).Handle, 0x112, 0xf012, 0);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)    => Close();
        private void btnMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void btnMaximize_Click(object sender, RoutedEventArgs e) =>
            WindowState = WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal;

        private async void btnReload_Click(object sender, RoutedEventArgs e) =>
            await LoadDataAsync(TimeSpan.FromSeconds(30));

        private void btnHistory_Click(object sender, RoutedEventArgs e)
        {
            var nf = _serviceProvider.GetRequiredService<frmMainView>();
            nf.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            nf.WindowState   = System.Windows.Forms.FormWindowState.Maximized;
            nf.ShowDialog();
        }

        private async void btnHydra_Click(object sender, RoutedEventArgs e) =>
            await GetDataHydra();

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            var nf = _serviceProvider.GetRequiredService<frmUpdateMasterData>();
            nf.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            nf.WindowState   = System.Windows.Forms.FormWindowState.Maximized;
            nf.ShowDialog();
            _ = LoadDataAsync(TimeSpan.FromSeconds(30));
        }

        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                isUpdateClicked = true;
                AutoUpdater.Start(GlobalVariable.ConfigSystem.UpdatePath);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCancelLoad_Click(object sender, RoutedEventArgs e) =>
            _loadCts?.Cancel();

        // =====================================================================
        //  BARCODE SCAN
        // =====================================================================
        private async void ScanBarcode_DataValueChanged(object? sender, DataValueChangedEventArgs e)
        {
            try
            {
                _qrCodeScan = e.NewValue.Value.ToString() ?? string.Empty;
                using var db = _dbFactory.CreateDbContext();
                _labelInfo   = new FT606_Label();
                _labelInfo   = await db.FT606s.FirstOrDefaultAsync(x => x.c001 == _qrCodeScan)
                               ?? throw new Exception("Label information not found.");

                _stepSelected = _dataHydra.FirstOrDefault(x => x.Id == _labelInfo.c000)
                                ?? throw new Exception("Step information not found.");

                FilterStepCombo(_stepSelected.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Barcode scan error");
                System.Windows.MessageBox.Show(ex.Message, "WARNING",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void FilterStepCombo(Guid id)
        {
            var item = _allStepCodeMaster.FirstOrDefault(x => x.FT601Id == id);
            if (item != null)
            {
                _suppress = true;
                cbStepName.SelectedItem = item;
                _suppress = false;
                _ = TriggerStepSelectionAsync(item);
            }
            else
                System.Windows.MessageBox.Show("No matching data found!", "WARNING",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        // =====================================================================
        //  STEP SELECTOR
        // =====================================================================
        private async void cbStepName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppress) return;
            try
            {
                var selected = cbStepName.SelectedItem as StepSelectModel;
                if (selected == null) { _labelInfo = new FT606_Label(); ResetNewLoop(); return; }

                if (string.IsNullOrEmpty(_qrCodeScan))
                {
                    _stepSelected = _dataHydra.FirstOrDefault(x =>
                        x.C004 == selected.StepItemCode &&
                        x.C015 == selected.Machine &&
                        x.C018 == selected.HydraOrderNo)
                        ?? throw new Exception("Step not found in master data.");
                }

                await TriggerStepSelectionAsync(selected);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "cbStepName_SelectionChanged");
                System.Windows.MessageBox.Show(ex.Message, "WARNING",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async Task TriggerStepSelectionAsync(StepSelectModel selected)
        {
            Dispatcher.Invoke(() =>
                tgPartitionAdj.IsChecked = false);
            await GetDataAsync(_stepSelected);
        }

        // =====================================================================
        //  GET DATA (build _scaleDataFinal from BOM)
        // =====================================================================
        private async Task GetDataAsync(FT601 stepCode)
        {
            try
            {
                using var db = _dbFactory.CreateDbContext();

                if (_scaleDataFinal.Count == 0)
                {
                    ResetNewLoop();

                    _stepItemCodeScale = await db.FT601s
                        .Where(x => x.C007 == stepCode.C007)
                        .FirstOrDefaultAsync()
                        ?? throw new Exception($"Step item code {stepCode.C007} not found.");

                    _allStepsFG = await db.Database
                        .SqlQueryRaw<BomWinlineModel>(
                            "sp_getBomWinlineOfItemFG @itemFG = {0}", stepCode.C007)
                        .AsNoTracking().ToListAsync();

                    foreach (var item in _allStepsFG)
                    {
                        bool allowScale = true;
                        FT601 ckHydra  = new();

                        ckHydra = item.ItemStepCode == _stepSelected.C004
                            ? _stepSelected
                            : _dataHydra.FirstOrDefault(x =>
                                x.C004 == item.ItemStepCode && x.C007 == item.ItemFgCode);

                        if (ckHydra == null)
                        {
                            if (item.ItemStepCode != "Z-VHXXXXXX" &&
                                item.ItemStepCode.Substring(0, 3) != "REX")
                            {
                                var mc  = item.ItemFgCode.Split('-')[0];
                                var smc = item.ItemStepCode.Split('-')[1];
                                ckHydra    = _dataHydra.FirstOrDefault(x =>
                                    x.C007.Contains($"{mc}-") || x.C004.Contains($"-{smc}-"));
                                allowScale = false;
                            }
                            else
                            {
                                ckHydra = new FT601
                                {
                                    C007 = item.ItemFgCode, C008 = item.ItemFgName,
                                    C004 = item.ItemStepCode, C005 = item.ItemStepName,
                                    C000 = _stepItemCodeScale.C000, C010 = item.ParallelSequence
                                };
                            }
                        }

                        var line = new FT600
                        {
                            id    = Guid.NewGuid(),
                            C000  = ckHydra?.C000,
                            C001  = ckHydra?.C000 != "21" && _stepItemCodeScale.C000 != "22"
                                        ? EnumSampleLocation.Production : EnumSampleLocation.Sample,
                            C004  = ckHydra?.C015,
                            C005  = ckHydra?.C006,
                            C006  = ckHydra?.C011,
                            C007  = ckHydra?.C012,
                            C009  = 1,
                            C012  = ckHydra?.Id == _labelInfo.c000 ? _labelInfo?.c001 : null,
                            C013  = ckHydra?.C007,
                            C014  = ckHydra?.C008,
                            C016  = null,
                            C017  = ckHydra?.C013,
                            C018  = ckHydra?.C014,
                            C019  = ckHydra?.C016,
                            C020  = ckHydra?.C019,
                            C002  = item.ItemStepCode,
                            C003  = item.ItemStepName,
                            C008  = item.Size,
                            C015  = item.ParallelSequence,
                            C021  = 0, C022 = 0, C023 = 0, C024 = 0,
                            C025  = item.Quantity,
                            C026  = ckHydra?.C020,
                            C027  = ckHydra?.C003,
                            C028  = ckHydra?.C013 != null ? (int)ckHydra.C013 : 0,
                            C029  = ckHydra?.Id == _labelInfo.c000 ? _labelInfo?.Id : null,
                            C032  = ckHydra?.Id,
                            C033  = item.CategoryCode,
                            C034  = item.CategoryName,
                            C035  = _percentOfUsage,
                            C036  = 0,
                            C037  = item.Unit,
                            AllowScale = allowScale
                        };
                        _scaleData.Add(line);
                    }

                    // Multi-size molds
                    var dataSize = new List<FT600>();
                    foreach (var item in _scaleData)
                    {
                        var pfx2      = GlobalVariable.PrefixUpToSecondHyphen(item.C002);
                        var sameMolds = _dataHydra.Where(x =>
                            x.C019 == item.C020 && x.C015 == item.C004 &&
                            x.C004 != item.C002 && x.C002 != item.C008 &&
                            GlobalVariable.PrefixUpToSecondHyphen(x.C004) == pfx2 &&
                            x.C010 == item.C015).DistinctBy(x => x.C002).ToList();

                        if (!sameMolds.Any()) continue;

                        var itemList = string.Join(",", sameMolds.Select(x => x.C004));
                        var category = await db.Database
                            .SqlQueryRaw<CategoryOfItemModel>(
                                "sp_GetCategorryOfItem @ItemCode = {0}", itemList)
                            .AsNoTracking().ToListAsync();

                        foreach (var ms in sameMolds)
                        {
                            var cat = category.FirstOrDefault(x => x.ItemCode == ms.C004);
                            dataSize.Add(new FT600
                            {
                                id    = Guid.NewGuid(),
                                C000  = ms?.C000,
                                C001  = ms?.C000 != "21" && ms?.C000 != "22"
                                            ? EnumSampleLocation.Production : EnumSampleLocation.Sample,
                                C004  = ms?.C015, C005 = ms?.C006, C006 = ms?.C011, C007 = ms?.C012,
                                C009  = 1,
                                C012  = _labelInfo?.c001,
                                C013  = ms?.C007, C014 = ms?.C008,
                                C017  = ms?.C013, C018 = ms?.C014, C019 = ms?.C016, C020 = ms?.C019,
                                C002  = ms?.C004, C003 = ms?.C005, C008 = ms?.C002, C015 = ms?.C010,
                                C021  = 0, C022 = 0, C023 = 0, C024 = 0,
                                C025  = item.C025,
                                C026  = ms?.C020, C027 = ms?.C003,
                                C028  = ms?.C013 != null ? (int)ms.C013 : 0,
                                C029  = _labelInfo?.Id,
                                C032  = ms?.Id,
                                C033  = cat?.CategoryCode, C034 = cat?.CategoryName,
                                C035  = _percentOfUsage,
                                C037  = cat?.Unit,
                                AllowScale = true
                            });
                        }
                    }

                    _scaleData.AddRange(dataSize.Where(ds =>
                        !_scaleData.Any(sd => sd.C002 == ds.C002 && sd.C015 == ds.C015)));
                    _scaleDataFinal = _scaleData.OrderBy(x => x.C015).ThenBy(x => x.C027).ToList();

                    // Previous scale values
                    foreach (var item in _scaleDataFinal)
                    {
                        var catCheck = GlobalVariable.ConfigSystem.CategoryOfNonInjectionUsagePartial?
                            .FirstOrDefault(x => x.CategoryCode == item.C033);

                        if (item.C002?.Substring(0, 3) == "REX")
                        {
                            var first = _scaleDataFinal
                                .Where(x => x.C015 == item.C015 && !string.IsNullOrEmpty(x.C026));
                            var paisShotFinal = catCheck == null
                                ? first.FirstOrDefault()?.C028 : first.Sum(x => x.C028);
                            item.C026 = first.FirstOrDefault()?.C026;
                            item.C027 = first.FirstOrDefault()?.C027;
                            item.C028 = paisShotFinal;
                            item.C017 = first.FirstOrDefault()?.C017 ?? 0;
                            item.C018 = first.FirstOrDefault()?.C018 ?? 0;
                        }

                        if (!item.C003!.StartsWith("Stud") && !item.C003.StartsWith("Logo") &&
                            !item.C003.StartsWith("Cleat_Ring") && !item.C002!.StartsWith("REX"))
                            continue;

                        FT600? stepPrevious;
                        if (item.C002 != "Z-VHXXXXXX" && item.C002.Substring(0, 3) != "REX")
                        {
                            var mainCode = item.C002.Split('-')[1];
                            stepPrevious = await db.FT600s
                                .Where(x => x.C015 == item.C015 &&
                                    (x.C002 == item.C002 ||
                                     (x.C002!.Contains(mainCode) && x.C008 == item.C008)))
                                .OrderByDescending(x => x.CreatedDate).FirstOrDefaultAsync();
                        }
                        else
                        {
                            stepPrevious = await db.FT600s
                                .Where(x => x.C002 == item.C002)
                                .OrderByDescending(x => x.CreatedDate).FirstOrDefaultAsync();
                        }

                        if (stepPrevious != null)
                        {
                            _percentOfUsage = (double)stepPrevious.C035!;
                            if (item.C002.StartsWith("REX"))
                            {
                                var total   = catCheck == null
                                    ? stepPrevious?.C036 * item.C025
                                    : stepPrevious?.C036;
                                var usage   = (double)Math.Round(
                                    (decimal)(total * _percentOfUsage / 100), 3);
                                var unusage = total - usage;
                                item.C021 = catCheck == null ? usage : usage / item.C028;
                                item.C022 = catCheck == null ? unusage : unusage / item.C028;
                                item.C023 = catCheck == null ? usage : usage / item.C028;
                                item.C024 = total;
                                item.C035 = stepPrevious!.C035;
                                item.C036 = stepPrevious?.C036;
                            }
                            else
                            {
                                item.C021 = stepPrevious.C021;
                                item.C022 = stepPrevious.C022;
                                item.C023 = stepPrevious.C023;
                                item.C024 = stepPrevious.C024;
                                item.C028 = stepPrevious.C028;
                                item.C035 = stepPrevious.C035;
                                item.C036 = stepPrevious?.C036;
                            }
                        }
                    }

                    _scaleDataFinal = _scaleDataFinal.OrderBy(x => x.C015).ToList();

                    var stepSel   = _scaleDataFinal.FirstOrDefault(x => x.C002 == _stepSelected.C004);
                    var prevSteps = _scaleDataFinal.Where(x => x.C015 < stepSel?.C015).ToList();
                    if (prevSteps.Any(x => x.C021 == 0))
                        System.Windows.MessageBox.Show(
                            "The previous step has not been weighed.", "Warning",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    else
                        _rowSelected = _scaleDataFinal
                            .FirstOrDefault(x => x.C002 == _stepSelected.C004) ?? new FT600();
                }
                else
                {
                    var rowSelect = _scaleDataFinal.FirstOrDefault(x =>
                        x.C002 == _stepSelected.C004 && x.C013 == _stepSelected.C007 &&
                        x.C004 == _stepSelected.C015);

                    if (rowSelect == null)
                    {
                        System.Windows.MessageBox.Show(
                            $"Label does not match the item being weighed.\n{_stepSelected.C004}|{_stepSelected.C005}",
                            "WARNING", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    if (!rowSelect.AllowScale)
                    {
                        System.Windows.MessageBox.Show(
                            "Do not allow to scale this step.", "Warning",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    foreach (var step in _scaleDataFinal.Where(x => x.C015 < rowSelect.C015))
                    {
                        if (step.C021 == 0)
                        {
                            System.Windows.MessageBox.Show(
                                "The previous step has not been weighed.", "Warning",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }
                    rowSelect.C012 = _labelInfo?.c001;
                    rowSelect.C029 = _labelInfo?.Id;
                    _rowSelected   = rowSelect;
                }

                _articlePaisShotFinaly = _rowSelected.C028;
                UpdateUI(false);
                FocusStepInGrid(_rowSelected.C002);
                await LoadRefHistoryAsync(_rowSelected.C002);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetDataAsync error");
                System.Windows.MessageBox.Show(ex.Message, "WARNING",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // =====================================================================
        //  SAVE WEIGHT
        // =====================================================================
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (_rowSelected == null) return;
            if (!_rowSelected.AllowScale)
            {
                System.Windows.MessageBox.Show("Cannot scale this step.", "Warning",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!(_rowSelected.C002?.StartsWith("REX") ?? false))
            {
                if (_rowSelected.C024 == 0)
                    _rowSelected.C024 = _scaleValue;
                else
                {
                    _rowSelected.C023 = _scaleValue;
                    var prs = _articlePaisShotFinaly;
                    _rowSelected.C022 = (cbRunner.Text == "YES")
                        ? Math.Round(
                            ((_rowSelected.C024 - (_rowSelected.C023 * (double?)prs)) / (double?)prs) ?? 0, 3)
                        : 0;

                    var prev    = _scaleDataFinal.Where(x => x.C015 == _rowSelected.C015 - 1).ToList();
                    var nonInj  = _scaleDataFinal.Where(x =>
                        x.C015 == _rowSelected.C015 &&
                        (x.C002 == "Z-VHXXXXXX" || (x.C002?.StartsWith("REX") ?? false))).ToList();
                    _rowSelected.C021 = _rowSelected.C023 - prev.Sum(x => x.C023) - nonInj.Sum(x => x.C023);
                    _rowSelected.C036 = (_rowSelected.C003?.StartsWith("Studs") ?? false) ||
                                        (_rowSelected.C003?.StartsWith("Logo")  ?? false) ||
                                        (_rowSelected.C003?.StartsWith("Cleat_Ring") ?? false)
                        ? Math.Round(_scaleValue / (double?)prs ?? 1.0, 2) : 0;
                }
            }
            else
            {
                var catCheck = GlobalVariable.ConfigSystem.CategoryOfNonInjectionUsagePartial?
                    .FirstOrDefault(x => x.CategoryCode == _rowSelected.C033);
                if (catCheck == null)
                {
                    _rowSelected.C024 = _scaleValue;
                    _rowSelected.C023 = _scaleValue;
                    _rowSelected.C021 = _scaleValue;
                    _rowSelected.C036 = Math.Round(_scaleValue / (_rowSelected.C025 ?? 1), 2);
                }
                else
                {
                    var usage   = (double)Math.Round(
                        (decimal)(_scaleValue * _percentOfUsage / 100) /
                        (decimal)(_rowSelected.C028 ?? 1), 3);
                    var unusage = (_scaleValue - usage * (_rowSelected.C028 ?? 1)) /
                                  (_rowSelected.C028 ?? 1);
                    _rowSelected.C024 = _scaleValue;
                    _rowSelected.C023 = usage;
                    _rowSelected.C021 = usage;
                    _rowSelected.C022 = unusage;
                    _rowSelected.C036 = Math.Round(usage / 2, 2);
                }
            }

            // Multi-size mold distribution
            var pfx2      = GlobalVariable.PrefixUpToSecondHyphen(_rowSelected.C002);
            var sameMolds = _scaleDataFinal.Where(x =>
                x.C020 == _rowSelected.C020 && x.C004 == _rowSelected.C004 &&
                GlobalVariable.PrefixUpToSecondHyphen(x.C002) == pfx2 &&
                x.C015 == _rowSelected.C015).ToList();

            if (sameMolds.Count > 1)
            {
                var sumPW    = sameMolds.Sum(x => x.C023 * x.C028);
                var pairShot = sameMolds.Sum(x => x.C028);
                foreach (var s in sameMolds)
                {
                    s.C024 = _rowSelected.C024;
                    s.C022 = s.C023 > 0 ? (_rowSelected.C024 - sumPW) / pairShot : 0;
                }
            }

            // Cascade subsequent steps
            var toUpdate = _scaleDataFinal.Where(x =>
                x.C015 >= _rowSelected.C015 && x.C024 > 0 &&
                !x.C003!.StartsWith("Stud") && !x.C003.StartsWith("Inlay") &&
                !x.C003.StartsWith("Ring") && !(x.C002?.StartsWith("REX") ?? false) &&
                x.C002 != _rowSelected.C002 &&
                GlobalVariable.PrefixUpToSecondHyphen(x.C002) != pfx2)
                .OrderBy(x => x.C015).ToList();

            foreach (var item in toUpdate)
            {
                if (item.C015 == _rowSelected.C015)
                {
                    item.C021 = item.C023 - _rowSelected.C023;
                    item.C022 = item.C024 - item.C021;
                }
                else
                {
                    var prev  = toUpdate.FirstOrDefault(x => x.C015 < item.C015 - 1);
                    item.C021 = item.C023 - prev?.C023;
                    item.C022 = item.C024 - item.C021;
                }
            }

            UpdateUI(false);
            FocusStepInGrid(_rowSelected.C002);
            UpdateReferencePanel();
        }

        // =====================================================================
        //  CONFIRM
        // =====================================================================
        private async void btnConfirm_Click(object sender, RoutedEventArgs e)
        {
            await ConfirmAsync();
        }

        private async Task ConfirmAsync()
        {
            using var db          = _dbFactory.CreateDbContext();
            using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                if (_operatorInfo == null || _operatorInfo.Id == Guid.Empty)
                {
                    System.Windows.MessageBox.Show("RFID card not yet scanned.", "Warning",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var pfx2      = GlobalVariable.PrefixUpToSecondHyphen(_rowSelected.C002);
                var sameMolds = _scaleDataFinal.Where(x =>
                    x.C020 == _rowSelected.C020 && x.C004 == _rowSelected.C004 &&
                    x.C002 != _rowSelected.C002 && x.C008 != _rowSelected.C008 &&
                    GlobalVariable.PrefixUpToSecondHyphen(x.C002) == pfx2 &&
                    x.C015 == _rowSelected.C015).ToList();

                if (sameMolds.Count == 0)
                {
                    foreach (var item in _scaleDataFinal)
                    {
                        if (item.AllowScale && item.C023 == 0 &&
                            (item.C024 == 0 && (item.C002?.StartsWith("REX") ?? false)))
                        {
                            System.Windows.MessageBox.Show(
                                $"Scale not completed for step: {item.C002}.", "Warning",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }
                }

                var now     = DateTime.Now;
                var machine = Environment.MachineName;
                var insert  = _scaleDataFinal
                    .Where(x => x.AllowScale == true && x.C021 > 0).ToList();

                insert.ForEach(x =>
                {
                    x.C010           = _operatorInfo.C000;
                    x.C011           = _operatorInfo.C001;
                    x.CreatedDate    = now;
                    x.CreatedMachine = machine;
                    x.Mesocomp       = _mesocomp;
                    x.Mesoyear       = _mesoYear;
                });

                await db.FT600s.AddRangeAsync(insert);
                await db.FT601s
                    .Where(b => b.C004 == _stepItemCodeScale.C004 &&
                                b.C007 == _stepItemCodeScale.C007 && b.C017 == false)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(b => b.C017, true)
                        .SetProperty(b => b.ModifiedDate, now)
                        .SetProperty(b => b.ModifiedMachine, machine));

                await db.SaveChangesAsync();
                await transaction.CommitAsync();

                System.Windows.MessageBox.Show("Scale shot weight saved successfully.", "Information",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                _labelInfo = new FT606_Label();
                Dispatcher.Invoke(() => cbStepName.SelectedItem = null);
                ResetNewLoop();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Confirm error – transaction rolled back");
                System.Windows.MessageBox.Show(
                    $"Transaction error: {ex.Message}\n{ex.InnerException?.Message}",
                    "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // =====================================================================
        //  CANCEL
        // =====================================================================
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            _labelInfo  = new FT606_Label();
            _qrCodeScan = _remarkFinal = string.Empty;
            Dispatcher.Invoke(() =>
            {
                cbStepName.SelectedItem = null;
                if (_scanBarcode != null) _scanBarcode.Text = string.Empty;
            });
            ResetNewLoop();
        }

        // =====================================================================
        //  SCALE VALUE
        // =====================================================================
        private void Scale_DataValueChanged(object? sender, DataValueChangedEventArgs e)
        {
            _scaleValue = Math.Round(Convert.ToDouble(e.NewValue.Value.ToString()), 2);
            Dispatcher.Invoke(() =>
                tbScaleDisplay.Text = _scaleValue.ToString("F2"));
        }

        // =====================================================================
        //  RFID
        // =====================================================================
        private void RFIDCode_DataValueChanged(object? sender, DataValueChangedEventArgs e)
        {
            try
            {
                _employeeCode = e.NewValue.Value?.ToString() ?? string.Empty;
                if (string.IsNullOrEmpty(_employeeCode))
                    throw new Exception("ID cannot be null.");

                using var db  = _dbFactory.CreateDbContext();
                _operatorInfo = db.fT029_Operator_RFIDs
                    .FirstOrDefault(x => x.C000.Contains(_employeeCode));

                if (_operatorInfo == null || _operatorInfo.Id == Guid.Empty)
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (_txtRFIDCode != null) _txtRFIDCode.Text = string.Empty;
                        tbRFIDName.Focus();
                    });
                    throw new Exception(
                        $"Employee ID {_employeeCode} not found. Please enter the name and press Enter to register.");
                }

                _operatorInfo.DepartmentInfor = db.FT031s.FirstOrDefault(x =>
                    x.Id == _operatorInfo.C002 && (x.C000 == "IT" || x.C000 == "QC"));

                if (_operatorInfo.DepartmentInfor == null)
                {
                    _employeeCode = string.Empty;
                    Dispatcher.Invoke(() =>
                    {
                        tbRFIDName.Text = string.Empty;
                        if (_txtRFIDCode != null) _txtRFIDCode.Text = string.Empty;
                    });
                    throw new Exception(
                        $"Employee does not have permission for this function.");
                }

                Dispatcher.Invoke(() =>
                {
                    tbRFIDName.Text  = _operatorInfo.C001;
                    tbUserName.Text  = $"{_operatorInfo.C000} · {_operatorInfo.C001}";
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RFID error");
                System.Windows.MessageBox.Show(ex.Message, "ERROR",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void tbRFIDName_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            try
            {
                if (e.Key != Key.Enter) return;
                var name = tbRFIDName.Text;
                if (string.IsNullOrEmpty(_employeeCode) || string.IsNullOrEmpty(name))
                    throw new Exception("ID or name cannot be null.");

                var res = System.Windows.MessageBox.Show(
                    $"Register operator {name} with ID {_employeeCode}?",
                    "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res != MessageBoxResult.Yes) return;

                using var db = _dbFactory.CreateDbContext();
                var dept     = db.FT031s.FirstOrDefault(x => x.C000 == "QC")
                               ?? throw new Exception("Department 'QC' not found.");

                if (await db.fT029_Operator_RFIDs.AnyAsync(x => x.C000 == _employeeCode))
                    throw new Exception("ID already exists.");

                await db.fT029_Operator_RFIDs.AddAsync(new FT029_Operator_RFID
                {
                    Id             = Guid.NewGuid(),
                    C000           = _employeeCode,
                    C001           = name,
                    C002           = dept.Id,
                    CreatedDate    = DateTime.Now,
                    CreatedBy      = string.Empty,
                    CreatedMachine = Environment.MachineName,
                    Actived        = true
                });
                await db.SaveChangesAsync();
                System.Windows.MessageBox.Show(
                    $"Operator '{_employeeCode}-{name}' registered successfully.", "OK",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RFID name keydown error");
                System.Windows.MessageBox.Show(ex.Message, "WARNING",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // =====================================================================
        //  HYDRA SYNC
        // =====================================================================
        private async Task GetDataHydra()
        {
            try
            {
                using var db = _dbFactory.CreateDbContext();
                _hydraItemDetails = await db.Database
                    .SqlQueryRaw<HydraItemDetailModel>("sp_GetFullStepItemHydraIsRun")
                    .AsNoTracking().ToListAsync();
                _hydraItemDetails = _hydraItemDetails
                    .OrderBy(x => x.FGItemCode).ThenBy(x => x.StepIndex).ToList();

                if (!_hydraItemDetails.Any()) return;

                var ft601s   = new List<FT601>();
                var now      = DateTime.Now;
                var machine  = Environment.MachineName;
                var toInsert = _hydraItemDetails.Where(d =>
                    !db.FT601s.Any(ft => ft.C004 == d.StepItemCode &&
                                         ft.C015 == d.Machine &&
                                         ft.C018 == d.OrderHydraNum &&
                                         ft.Actived == true)).ToList();

                foreach (var item in toInsert)
                    ft601s.Add(new FT601
                    {
                        Id    = Guid.NewGuid(), C000 = item.HydraOrderType,
                        C001  = item.Location == "Sample"
                                    ? EnumSampleLocation.Sample : EnumSampleLocation.Production,
                        C002  = item.Size,         C003 = item.MainName,
                        C004  = item.StepItemCode, C005 = item.StepItemName,
                        C006  = item.Artikel,      C007 = item.FGItemCode,
                        C008  = item.FGItemName,   C009 = item.StepIndexHydra,
                        C010  = item.StepIndex,    C011 = item.ColorCode,
                        C012  = item.ColorName,    C013 = item.ArticlePairShot,
                        C014  = item.MoldPairShot, C015 = item.Machine,
                        C016  = item.MachineGroup, C017 = false,
                        C018  = item.OrderHydraNum, C019 = item.MoldId,
                        C020  = item.MainCode,     Actived = true,
                        CreatedMachine = machine,  CreatedDate = now,
                        Mesoyear = item.MesoYear,  Mesocomp = item.MesoComp
                    });

                if (ft601s.Any())
                {
                    await db.FT601s.AddRangeAsync(ft601s);
                    await db.SaveChangesAsync();
                }

                await LoadDataAsync();
            }
            catch (Exception ex) { _logger.LogError(ex, "GetDataHydra error"); }
        }

        // =====================================================================
        //  RESET
        // =====================================================================
        private void ResetNewLoop()
        {
            _rowSelected           = new FT600();
            _allStepsFG            = new List<BomWinlineModel>();
            _stepItemCodeScale     = new FT601();
            _scaleData             = new List<FT600>();
            _scaleDataFinal        = new List<FT600>();
            _newScale              = true;
            _qrCodeScan            = string.Empty;
            _percentOfUsage        = GlobalVariable.ConfigSystem.PercentOfUserNonWoven;
            _articlePaisShotFinaly = 0;
            _remarkFinal           = string.Empty;
            _stdRow                = new FT600();
            _refHistory            = new List<FT600>();

            Dispatcher.Invoke(() => cbRunner.SelectedIndex = 0);

            UpdateUI(false);
            UpdateReferencePanel();
        }

        // =====================================================================
        //  UPDATE UI
        // =====================================================================
        private void UpdateUI(bool refresh = true)
        {
            Dispatcher.Invoke(() =>
            {
                tbStepCode.Text  = _rowSelected?.C002;
                tbMachine.Text   = _rowSelected?.C004;
                tbSize.Text      = _rowSelected?.C008;
                tbStepIndex.Text = _rowSelected?.C015?.ToString();
                tbActualPairs.Text = _rowSelected?.C028?.ToString();
                tbQty.Text       = _rowSelected?.C025?.ToString();
                tbFgItemCode.Text = _rowSelected?.C013;
                tbFgName.Text    = _rowSelected?.C014;
                tbUsagePct.Text  = (_rowSelected?.C035 != 0
                    ? _rowSelected?.C035 : _percentOfUsage)?.ToString();
                tbRemark.Text    = _remarkFinal;
                tbStepName.Text  = _rowSelected?.C003;

                // Enable/disable Usage%
                var catCheck = GlobalVariable.ConfigSystem.CategoryOfNonInjectionUsagePartial?
                    .FirstOrDefault(x => x.CategoryCode == _rowSelected?.C033);
                tbUsagePct.IsReadOnly =
                    !(_rowSelected?.C002?.StartsWith("REX") == true && catCheck != null);

                UpdateStepStatuses();

                _stepsCollection.Clear();
                if (_scaleDataFinal != null)
                    foreach (var item in _scaleDataFinal)
                        _stepsCollection.Add(item);

                UpdateReferencePanel();
            });
        }

        private void UpdateStepStatuses()
        {
            foreach (var item in _scaleDataFinal ?? new())
            {
                bool isDone   = (item.C021 > 0 ||
                    (!(item.C002?.StartsWith("REX") ?? false) ? item.C024 > 0 : item.C023 > 0));
                bool isActive = item.C002 == _rowSelected?.C002 &&
                                item.C032 == _rowSelected?.C032;

                item.StatusText     = isDone ? "Done"   : isActive ? "Active"  : "Pending";
                item.StatusDotColor = isDone ? "#4CAF50": isActive ? "#1976D2" : "#9E9E9E";
                item.StatusBarColor = isActive ? "#1565C0" : isDone ? "#A5D6A7" : "Transparent";
            }
        }

        // =====================================================================
        //  REFERENCE PANEL
        // =====================================================================
        private void UpdateReferencePanel()
        {
            _referenceRows = new List<ReferenceRow>
            {
                new ReferenceRow
                {
                    No        = 1,
                    FieldName = "Total W_Injection",
                    Unit      = "g",
                    Std       = _stdRow?.C024 > 0 ? _stdRow.C024 : null,
                    Actual    = _rowSelected?.C024 > 0 ? _rowSelected.C024 : null
                },
                new ReferenceRow
                {
                    No        = 2,
                    FieldName = "Total PW Weight",
                    Unit      = "g/prs",
                    Std       = _stdRow?.C023 > 0 ? _stdRow.C023 : null,
                    Actual    = _rowSelected?.C023 > 0 ? _rowSelected.C023 : null
                },
                new ReferenceRow
                {
                    No        = 3,
                    FieldName = "Part Weight",
                    Unit      = "g/prs",
                    Std       = _stdRow?.C021 > 0 ? _stdRow.C021 : null,
                    Actual    = _rowSelected?.C021 > 0 ? _rowSelected.C021 : null
                },
                new ReferenceRow
                {
                    No        = 4,
                    FieldName = "Runner / Excess Mat. Weight",
                    Unit      = "g/prs",
                    Std       = _stdRow?.C022 > 0 ? _stdRow.C022 : null,
                    Actual    = _rowSelected?.C022 > 0 ? _rowSelected.C022 : null
                }
            };

            Dispatcher.Invoke(() =>
            {
                _refRowsCollection.Clear();
                foreach (var row in _referenceRows)
                    _refRowsCollection.Add(row);

            
                // Tint scale card border
                var worst = _referenceRows
                    .OrderByDescending(r => (int)r.Tolerance)
                    .FirstOrDefault()?.Tolerance ?? ToleranceCategory.Idle;

                pnlScaleCard.BorderBrush = worst switch
                {
                    ToleranceCategory.Ok   => new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                    ToleranceCategory.Warn => new SolidColorBrush(Color.FromRgb(255, 152, 0)),
                    ToleranceCategory.Err  => new SolidColorBrush(Color.FromRgb(244, 67, 54)),
                    _                      => new SolidColorBrush(Color.FromRgb(207, 216, 220))
                };
            });
        }

        private async Task LoadRefHistoryAsync(string? stepCode)
        {
            try
            {
                if (string.IsNullOrEmpty(stepCode)) return;

                using var db = _dbFactory.CreateDbContext();
                _refHistory  = await db.FT600s
                    .Where(x => x.C002 == stepCode)
                    .OrderByDescending(x => x.CreatedDate)
                    .Take(5).ToListAsync();

                _stdRow = _refHistory.FirstOrDefault() ?? new FT600();
                UpdateReferencePanel();

                Dispatcher.Invoke(() =>
                {
                    _historyCollection.Clear();
                    foreach (var h in _refHistory)
                        _historyCollection.Add(h);

                    dgHistory.ItemsSource = _historyCollection;

                    tbHistoryToggle.Text =
                        (_historyExpanded ? "▼" : "▶") +
                        $"  REFERENCE / HISTORY  ·  last {_refHistory.Count} weighings  ·  {stepCode}";
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LoadRefHistoryAsync error");
            }
        }

        // =====================================================================
        //  GRID EVENTS
        // =====================================================================
        private void dgTotalSteps_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

        private void btnGridScale_Click(object sender, RoutedEventArgs e)
        {
            var rowSelect = (sender as Button)?.Tag as FT600;
            if (rowSelect == null) return;

            Dispatcher.Invoke(() => tgPartitionAdj.IsChecked = false);

            if (!rowSelect.AllowScale)
            {
                System.Windows.MessageBox.Show("Do not allow to scale this step.", "Warning",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            foreach (var step in _scaleDataFinal.Where(x => x.C015 < rowSelect.C015))
            {
                if (step.C021 == 0)
                {
                    System.Windows.MessageBox.Show("The previous step has not been weighed.", "Warning",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            _rowSelected           = rowSelect;
            _articlePaisShotFinaly = _rowSelected.C017 != 0 && _rowSelected.C017 == _rowSelected.C028
                ? _rowSelected.C017 : _rowSelected.C028;
            UpdateUI(true);
            UpdateReferencePanel();
            _ = LoadRefHistoryAsync(_rowSelected.C002);
        }

        private void btnGridReset_Click(object sender, RoutedEventArgs e)
        {
            var rowSelect = (sender as Button)?.Tag as FT600;
            if (rowSelect == null) return;

            var rowReset = _scaleDataFinal.FirstOrDefault(x =>
                x.AllowScale && x.C002 == rowSelect.C002);
            if (rowReset == null)
            {
                System.Windows.MessageBox.Show("Cannot reset this line.", "Warning",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            rowReset.C021 = rowReset.C022 = rowReset.C023 = rowReset.C024 = 0;
            _rowSelected  = rowSelect;
            _articlePaisShotFinaly = _rowSelected.C017 != 0 && _rowSelected.C017 == _rowSelected.C028
                ? _rowSelected.C017 : _rowSelected.C028;
            UpdateUI(true);
            UpdateReferencePanel();
        }

        private void HistoryHeader_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _historyExpanded  = !_historyExpanded;
            pnlHistory.Visibility = _historyExpanded ? Visibility.Visible : Visibility.Collapsed;
            var current = tbHistoryToggle.Text.TrimStart('▼', '▶', ' ');
            tbHistoryToggle.Text = (_historyExpanded ? "▼" : "▶") + "  " + current;
        }

        // =====================================================================
        //  FIELD EVENTS
        // =====================================================================
        private void tbActualPairs_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            _articlePaisShotFinaly = double.TryParse(tbActualPairs.Text, out var v) ? v : 0;
            var row = _scaleDataFinal.FirstOrDefault(x => x.C032 == _rowSelected.C032);
            if (row != null) row.C028 = _articlePaisShotFinaly;
            UpdateUI(true);
        }

        private void tbUsagePct_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            var catCheck = GlobalVariable.ConfigSystem.CategoryOfNonInjectionUsagePartial?
                .FirstOrDefault(x => x.CategoryCode == _rowSelected.C033);
            if (!(_rowSelected?.C002?.StartsWith("REX") ?? false) ||
                (_rowSelected?.C002?.StartsWith("REX") == true && catCheck == null)) return;

            _percentOfUsage = double.TryParse(tbUsagePct.Text, out var v) ? v : 0;
            _scaleDataFinal?.Where(x => x.C002?.StartsWith("REX") ?? false).ToList().ForEach(x =>
            {
                var usage   = x.C035 == 100
                    ? (double)Math.Round((decimal)(_rowSelected!.C024 * _percentOfUsage / 100), 3)
                    : (double)Math.Round((decimal)((decimal)(_rowSelected!.C024 * _percentOfUsage / 100) /
                                                    (decimal)x.C028!), 3);
                var unusage = _rowSelected!.C035 == 100
                    ? _rowSelected.C024 - usage
                    : (_rowSelected.C024 - usage * x.C028) / x.C028;
                x.C035 = _percentOfUsage;
                x.C023 = usage;
                x.C021 = usage;
                x.C022 = unusage;
            });
            UpdateUI(true);
        }

        private void tbRemark_TextChanged(object sender, TextChangedEventArgs e)
        {
            _remarkFinal = tbRemark.Text;
            _scaleDataFinal?.ForEach(x => x.C038 = _remarkFinal);
        }

        private void tgPartitionAdj_Changed(object sender, RoutedEventArgs e)
        {
            _allowPartitionAdjustment = tgPartitionAdj.IsChecked == true;
            tbActualPairs.IsReadOnly  = !_allowPartitionAdjustment;
            tbActualPairs.Background  = _allowPartitionAdjustment
                ? System.Windows.Media.Brushes.White
                : new SolidColorBrush(Color.FromRgb(240, 240, 240));
        }

        // =====================================================================
        //  FOCUS HELPER
        // =====================================================================
        private void FocusStepInGrid(string? stepCode)
        {
            if (string.IsNullOrEmpty(stepCode)) return;
            var item = _stepsCollection.FirstOrDefault(x => x.C002 == stepCode);
            if (item != null)
            {
                dgTotalSteps.SelectedItem = item;
                dgTotalSteps.ScrollIntoView(item);
            }
        }

      
        private async void cbStepName_EditValueChangedAsync(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            var editor = sender as LookUpEdit;
            // Value hiện tại (theo ValueMember)
            var selectedId = editor.EditValue;
           
            // Lấy object đầy đủ
            var selectedItem = editor.SelectedItem;
            if (selectedItem != null)
            {
                // Nếu Data là class Product
                var step = selectedItem as StepSelectModel;
                if (step != null)
                {
                    try
                    {
                        var selected = cbStepName.SelectedItem as StepSelectModel;
                        if (selected == null) { _labelInfo = new FT606_Label(); ResetNewLoop(); return; }

                        if (string.IsNullOrEmpty(_qrCodeScan))
                        {
                            _stepSelected = _dataHydra.FirstOrDefault(x =>
                                x.C004 == selected.StepItemCode &&
                                x.C015 == selected.Machine &&
                                x.C018 == selected.HydraOrderNo)
                                ?? throw new Exception("Step not found in master data.");
                        }

                        await TriggerStepSelectionAsync(selected);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "cbStepName_SelectionChanged");
                        System.Windows.MessageBox.Show(ex.Message, "WARNING",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
        }

      
    }
}
