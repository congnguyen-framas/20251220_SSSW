// ============================================================================
//  ShotWeightViewModel.cs
//  MVVM ViewModel cho ShotWeightWindow – toàn bộ business logic
//  Logic cân (C021/C022/C023/C024) giống 100% frmShotWeightScale.cs
//  Namespace : SSSW.UI.WPF.ViewModels
// ============================================================================
using AutoUpdaterDotNET;
using DevExpress.Mvvm.Native;
using DevExpress.XtraRichEdit.Import.Html;
using DevExpress.XtraSpreadsheet.Import.Xls;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ScanAndScale.Core.Models;
using SSSW.models;
using SSSW.modelss;
using SSSW.UI.WPF.Models;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Reflection.PortableExecutable;
using System.Windows;
using System.Windows.Media;
// ── Disambiguate WPF vs WinForms/Drawing types (project uses UseWPF + UseWindowsForms) ──
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;

namespace SSSW.UI.WPF.ViewModels
{
    public class ShotWeightViewModel : BaseViewModel
    {
        // ── DI ──────────────────────────────────────────────────────────────
        private readonly IDbContextFactory<DbContextDogeWH> _dbFactory;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ShotWeightViewModel> _logger;

        // ── Cancellation ────────────────────────────────────────────────────
        private CancellationTokenSource? _loadCts;

        // ─────────────────────────────────────────────────────────────────────
        //  DOMAIN STATE (giống frmShotWeightScale field-for-field)
        // ─────────────────────────────────────────────────────────────────────
        public List<FT601> _dataHydra = new();
        private List<FT601> _dataHydraMultiSizeOfMold = new();
        private bool _newScale = true;
        public FT601 _stepItemCodeScale = new();
        private bool _isUpdateClicked = false;

        private List<BomWinlineModel> _allStepsFG = new();
        private List<FT600> _scaledDataPreviousStep = new();
        private List<FT600> _scaleData = new();
        private List<FT600> _scaleDataFinal = new();
        private FT600 _rowSelected = new();

        private double _scaleValue = 0;
        private string _mesocomp = string.Empty;
        private int _mesoYear = 0;

        private bool _isRunner = true;
        private double? _articlePaisShotFinaly = 0;
        private double _percentOfUsage = 0;
        private string? _remarkFinal = string.Empty;
        private List<StepSelectModel> _allStepCodeMaster = new();
        private StepSelectModel? _selectedStepItem;

        public FT601 _stepSelected = new();
        private string _qrCodeScan = string.Empty;
        private FT606_Label _labelInfo = new();

        private string _employeeCode = string.Empty;
        private FT029_Operator_RFID _operatorInfo = new();
        private List<HydraItemDetailModel> _hydraItemDetails = new();

        private bool _allowPartitionAdjustment = false;
        private bool _suppress = false;
        private bool _historyExpanded = false;

        private List<ReferenceRow> _referenceRows = new();
        private FT600 _stdRow = new();
        private List<FT600> _refHistory = new();



        // ─────────────────────────────────────────────────────────────────────
        //  OBSERVABLE COLLECTIONS (ItemsSource cho DataGrid)
        // ─────────────────────────────────────────────────────────────────────
        public ObservableCollection<FT600> StepsCollection { get; } = new();
        public ObservableCollection<FT600> HistoryCollection { get; } = new();
        public ObservableCollection<ReferenceRow> RefRowsCollection { get; } = new();

        private List<StepSelectModel> _stepCodeMaster = new();
        public List<StepSelectModel> StepCodeMaster
        {
            get => _stepCodeMaster;
            private set { _stepCodeMaster = value; OnPropertyChanged(); }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  BINDABLE DISPLAY PROPERTIES
        // ─────────────────────────────────────────────────────────────────────



        #region Title / Header
        private string _windowTitle = "IT – Shotweight Station";
        public string WindowTitle
        {
            get => _windowTitle;
            set { _windowTitle = value; OnPropertyChanged(); }
        }

        private string _userName = "";
        public string UserName
        {
            get => _userName;
            set { _userName = value; OnPropertyChanged(); }
        }
        #endregion

        #region Step Information Panel
        private string? _stepName;
        public string? StepName
        {
            get => _stepName;
            set { _stepName = value; OnPropertyChanged(); }
        }

        private string? _stepCode;
        public string? StepCode
        {
            get => _stepCode;
            set { _stepCode = value; OnPropertyChanged(); }
        }

        private string? _machine;
        public string? Machine
        {
            get => _machine;
            set { _machine = value; OnPropertyChanged(); }
        }

        private string? _qty;
        public string? Qty
        {
            get => _qty;
            set { _qty = value; OnPropertyChanged(); }
        }

        private string? _actualPairs;
        public string? ActualPairs
        {
            get => _actualPairs;
            set { _actualPairs = value; OnPropertyChanged(); }
        }

        private string? _size;
        public string? Size
        {
            get => _size;
            set { _size = value; OnPropertyChanged(); }
        }

        private string? _stepIndex;
        public string? StepIndex
        {
            get => _stepIndex;
            set { _stepIndex = value; OnPropertyChanged(); }
        }

        private string? _fgName;
        public string? FgName
        {
            get => _fgName;
            set { _fgName = value; OnPropertyChanged(); }
        }

        private string? _fgItemCode;
        public string? FgItemCode
        {
            get => _fgItemCode;
            set { _fgItemCode = value; OnPropertyChanged(); }
        }

        private string? _remark;
        public string? Remark
        {
            get => _remark;
            set
            {
                _remark = value;
                OnPropertyChanged();
                // sync remarkFinal và lan sang tất cả rows
                _remarkFinal = value;
                _scaleDataFinal?.ForEach(x => x.C038 = _remarkFinal);
            }
        }

        private string? _usagePct;
        public string? UsagePct
        {
            get => _usagePct;
            set { _usagePct = value; OnPropertyChanged(); }
        }

        private bool _isUsagePctReadOnly = true;
        public bool IsUsagePctReadOnly
        {
            get => _isUsagePctReadOnly;
            set { _isUsagePctReadOnly = value; OnPropertyChanged(); }
        }

        private bool _isActualPairsReadOnly = true;
        public bool IsActualPairsReadOnly
        {
            get => _isActualPairsReadOnly;
            set { _isActualPairsReadOnly = value; OnPropertyChanged(); }
        }

        // CheckBox Partition Adj
        private bool _allowPartitionAdj = false;
        public bool AllowPartitionAdj
        {
            get => _allowPartitionAdj;
            set
            {
                _allowPartitionAdj = value;
                _allowPartitionAdjustment = value;
                IsActualPairsReadOnly = !value;
                OnPropertyChanged();
            }
        }

        // ComboBox Runner (YES/NO)
        //public List<string> RunnerOptions { get; set; } = new() { "YES", "NO" };
        private string _runnerText = "YES";
        public string RunnerText
        {
            get => _runnerText;
            set { _runnerText = value; OnPropertyChanged(); }
        }

        // RFID name textbox
        private string _rfidName = "";
        public string RfidName
        {
            get => _rfidName;
            set { _rfidName = value; OnPropertyChanged(); }
        }
        #endregion

        #region Scale Card
        private string _scaleDisplay = "0.00";
        public string ScaleDisplay
        {
            get => _scaleDisplay;
            set { _scaleDisplay = value; OnPropertyChanged(); }
        }

        private Brush _scaleCardBorderBrush = new SolidColorBrush(Color.FromRgb(207, 216, 220));
        public Brush ScaleCardBorderBrush
        {
            get => _scaleCardBorderBrush;
            set { _scaleCardBorderBrush = value; OnPropertyChanged(); }
        }

        private Brush _scaleCardBackground = new SolidColorBrush(Color.FromRgb(245, 245, 245));
        public Brush ScaleCardBackground
        {
            get => _scaleCardBackground;
            set { _scaleCardBackground = value; OnPropertyChanged(); }
        }

        public double ScaleValue
        {
            get => _scaleValue;
            set
            {
                _scaleValue = value;
                ScaleDisplay = _scaleValue.ToString("F2");
                OnPropertyChanged();
            }
        }
        #endregion

        #region Device Connection Status  (ScanAndScale.Core)
        // ── Barcode ──────────────────────────────────────────────────────────
        private DriverStatus _barcodeStatus = DriverStatus.Unknown;
        public DriverStatus BarcodeStatus
        {
            get => _barcodeStatus;
            set { SetProperty(ref _barcodeStatus, value); }
        }

        private string _barcodeScannedValue = string.Empty;
        public string BarcodeScannedValue
        {
            get => _barcodeScannedValue;
            set => SetProperty(ref _barcodeScannedValue, value);
        }

        private bool _readOnlyScanner = false;
        public bool ReadOnlyScanner
        {
            get => _readOnlyScanner;
            set { _readOnlyScanner = value; OnPropertyChanged(); }
        }

        // ── RFID ─────────────────────────────────────────────────────────────
        private DriverStatus _rfidStatus = DriverStatus.Unknown;
        public DriverStatus RfidStatus
        {
            get => _rfidStatus;
            set { SetProperty(ref _rfidStatus, value); }
        }

        private string _rfidCardCode = string.Empty;
        /// <summary>Mã thẻ RFID vừa quét (hiển thị trên UI, khác với RfidName = tên nhân viên).</summary>
        public string RfidCardCode
        {
            get => _rfidCardCode;
            set { SetProperty(ref _rfidCardCode, value); }
        }

        private bool _readOnlyRfid = false;
        public bool ReadOnlyRfid
        {
            get => _readOnlyRfid;
            set { _readOnlyRfid = value; OnPropertyChanged(); }
        }

        // ── Scale ─────────────────────────────────────────────────────────────
        private DriverStatus _scaleStatus = DriverStatus.Unknown;
        public DriverStatus ScaleStatus
        {
            get => _scaleStatus;
            set { SetProperty(ref _scaleStatus, value); }
        }

        private bool _scaleStable = false;
        public bool ScaleStable
        {
            get => _scaleStable;
            set { SetProperty(ref _scaleStable, value); }
        }

        private bool _scaleTare = false;
        public bool ScaleTare
        {
            get => _scaleTare;
            set { SetProperty(ref _scaleTare, value); }
        }

        private string _scaleUnit = "KG";
        public string ScaleUnit
        {
            get => _scaleUnit;
            set { SetProperty(ref _scaleUnit, value); }
        }

        private string _deltaInformation = string.Empty;
        public string DeltaInformation
        {
            get => _deltaInformation;
            set { SetProperty(ref _deltaInformation, value); }
        }

        private bool _readOnlyScale = false;
        public bool ReadOnlyScale
        {
            get => _readOnlyScale;
            set { _readOnlyScale = value; OnPropertyChanged(); }
        }

        private bool _enableBtnSaveScale = false;
        public bool EnableBtnSaveScale
        {
            get => _enableBtnSaveScale;
            set { _enableBtnSaveScale = value; OnPropertyChanged(); }
        }
        #endregion

        #region History
        private bool _isHistoryExpanded = false;
        public bool IsHistoryExpanded
        {
            get => _isHistoryExpanded;
            set { _isHistoryExpanded = value; OnPropertyChanged(); }
        }

        private string _historyToggleText = "▶  REFERENCE / HISTORY";
        public string HistoryToggleText
        {
            get => _historyToggleText;
            set { _historyToggleText = value; OnPropertyChanged(); }
        }

        private string _historyToggleTextSecond = string.Empty;
        public string HistoryToggleTextSecond
        {
            get => _historyToggleTextSecond;
            set
            {
                _historyToggleTextSecond = value;
                OnPropertyChanged();
            }
        }

        private string _historyToggleTextDetail = string.Empty;
        public string HistoryToggleTextDetail
        {
            get => _historyToggleTextDetail;
            set
            {
                _historyToggleTextDetail = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region Overlay
        private bool _isOverlayVisible = false;
        public bool IsOverlayVisible
        {
            get => _isOverlayVisible;
            set { _isOverlayVisible = value; OnPropertyChanged(); }
        }

        private bool _isCancelLoadEnabled = false;
        public bool IsCancelLoadEnabled
        {
            get => _isCancelLoadEnabled;
            set { _isCancelLoadEnabled = value; OnPropertyChanged(); }
        }
        #endregion

        // ─────────────────────────────────────────────────────────────────────
        //  VIEW CALLBACKS (View-only operations không thể bind)
        // ─────────────────────────────────────────────────────────────────────
        /// <summary>Code-behind gán: xóa text trên BarcodeButtonEdit.</summary>
        public Action? ClearBarcodeAction { get; set; }
        /// <summary>Code-behind gán: xóa text trên RFIDButtonEdit.</summary>
        public Action? ClearRfidAction { get; set; }
        /// <summary>Code-behind gán: focus vào tbRFIDName.</summary>
        public Action? FocusRfidNameAction { get; set; }
        /// <summary>Code-behind gán: reset DevExpress LookUpEdit selection.</summary>
        public Action? ClearStepComboAction { get; set; }
        /// <summary>Code-behind gán: set DevExpress LookUpEdit selection.</summary>
        public Action<StepSelectModel?>? SetStepComboAction { get; set; }
        /// <summary>Code-behind gán: focus+scroll đến row trong dgTotalSteps.</summary>
        public Action<string?>? FocusGridRowAction { get; set; }
        /// <summary>Code-behind gán: apply config cho hardware controls.</summary>
        public Action? ApplyHardwareConfigAction { get; set; }

        // ─────────────────────────────────────────────────────────────────────
        //  COMMANDS
        // ─────────────────────────────────────────────────────────────────────
        public AsyncRelayCommand ReloadCommand { get; }
        public RelayCommand HistoryViewCommand { get; }
        public AsyncRelayCommand HydraCommand { get; }
        public RelayCommand SettingsCommand { get; }
        public RelayCommand UpdateCommand { get; }
        public RelayCommand MinimizeCommand { get; }
        public RelayCommand MaximizeCommand { get; }
        public RelayCommand CloseCommand { get; }
        public RelayCommand CancelLoadCommand { get; }
        public RelayCommand SaveCommand { get; }
        public AsyncRelayCommand ConfirmCommand { get; }
        public RelayCommand CancelCommand { get; }
        public RelayCommand HistoryToggleCommand { get; }
        public RelayCommand GridScaleCommand { get; }
        public RelayCommand GridResetCommand { get; }
        public RelayCommand GridDeleteCommand { get; }

        // ─────────────────────────────────────────────────────────────────────
        //  CONSTRUCTOR
        // ─────────────────────────────────────────────────────────────────────
        public ShotWeightViewModel(
            IDbContextFactory<DbContextDogeWH> dbFactory,
            IServiceProvider serviceProvider,
            ILogger<ShotWeightViewModel> logger)
        {
            _dbFactory = dbFactory;
            _serviceProvider = serviceProvider;
            _logger = logger;

            // Wire commands
            ReloadCommand = new AsyncRelayCommand(() => LoadDataAsync(TimeSpan.FromSeconds(30)));
            HistoryViewCommand = new RelayCommand(OpenHistoryView);
            HydraCommand = new AsyncRelayCommand(GetDataHydraAsync);
            SettingsCommand = new RelayCommand(OpenSettings);
            UpdateCommand = new RelayCommand(CheckUpdate);
            MinimizeCommand = new RelayCommand(() => System.Windows.Application.Current.MainWindow.WindowState = System.Windows.WindowState.Minimized);
            MaximizeCommand = new RelayCommand(ToggleMaximize);
            CloseCommand = new RelayCommand(() => System.Windows.Application.Current.MainWindow.Close());
            CancelLoadCommand = new RelayCommand(() => _loadCts?.Cancel());
            SaveCommand = new RelayCommand(ExecuteSave);
            ConfirmCommand = new AsyncRelayCommand(ExecuteConfirmAsync);
            CancelCommand = new RelayCommand(ExecuteCancel);
            HistoryToggleCommand = new RelayCommand(ToggleHistory);
            GridScaleCommand = new RelayCommand(p => OnGridScale(p as FT600));
            GridResetCommand = new RelayCommand(p => OnGridReset(p as FT600));
            GridDeleteCommand = new RelayCommand(p => OnGridDelete(p as FT600));

            // Default: xóa các giá trị hiển thị trong VM thay vì xóa WinForms TextBox
            ClearBarcodeAction = () => BarcodeScannedValue = string.Empty;
            ClearRfidAction = () => RfidCardCode = string.Empty;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  INITIALIZATION
        // ─────────────────────────────────────────────────────────────────────
        public async Task InitializeAsync()
        {
            // Auto-updater
            AutoUpdater.RunUpdateAsAdmin = false;
            AutoUpdater.DownloadPath = Environment.CurrentDirectory;
            AutoUpdater.ApplicationExitEvent += AutoUpdater_ApplicationExitEvent;
            AutoUpdater.CheckForUpdateEvent += AutoUpdater_CheckForUpdateEvent;

            // Lấy mesocomp + mesoyear
            using var db = _dbFactory.CreateDbContext();
            _mesocomp = db.Database.SqlQueryRaw<string>("sp_MaterialGetCompanyName")
                          .AsEnumerable().FirstOrDefault() ?? string.Empty;
            _mesoYear = db.Database.SqlQueryRaw<int>("sp_MaterialGetMesoyear")
                          .AsEnumerable().FirstOrDefault();

            var location = _mesocomp switch
            {
                "VNT1" => "fVN",
                "FKV" => "fKV",
                "FTT1" => "fFT",
                "05FI" => "fIN",
                "fGE" => "fGE",
                _ => "Unknown"
            };
            if (Enum.TryParse<EnumLocation>(location, true, out var loc))
                WindowTitle = $"{loc} – Shotweight Station";

            // Config
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
                    Mesoyear = _mesoYear,
                    Mesocomp = _mesocomp,
                    CreatedMachine = Environment.MachineName,
                    CreatedDate = DateTime.Now
                });
                await db.SaveChangesAsync();
            }

            // Apply hardware config qua callback
            ApplyHardwareConfigAction?.Invoke();

            // Default values
            UsagePct = GlobalVariable.ConfigSystem.PercentOfUserNonWoven.ToString();
            _percentOfUsage = GlobalVariable.ConfigSystem.PercentOfUserNonWoven;

            DeltaInformation = $"STD: Target Weight ↔ ACTUAL: Live Weight · auto colored vs. tolerance: -{GlobalVariable.ConfigSystem.DeltaLevel1}g<=Delta<={GlobalVariable.ConfigSystem.DeltaLevel1}g -->green;  -{GlobalVariable.ConfigSystem.DeltaLevel2}g<=Delta<=-{GlobalVariable.ConfigSystem.DeltaLevel1}g or {GlobalVariable.ConfigSystem.DeltaLevel1}g<=Delta<={GlobalVariable.ConfigSystem.DeltaLevel2}g -->Orange; Delta<-{GlobalVariable.ConfigSystem.DeltaLevel2}g or Delta>{GlobalVariable.ConfigSystem.DeltaLevel2}g -->Red.";
            ReadOnlyScale = GlobalVariable.ConfigSystem.Scale.ReadOnly ?? true;
            ReadOnlyRfid = GlobalVariable.ConfigSystem.RFID.ReadOnly;
            ReadOnlyScanner = GlobalVariable.ConfigSystem.Scanner.ReadOnly;

            await LoadDataAsync();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  LOAD MASTER DATA (FT601s)
        // ─────────────────────────────────────────────────────────────────────
        public async Task LoadDataAsync(TimeSpan? timeout = null)
        {
            timeout ??= TimeSpan.FromSeconds(30);
            _loadCts?.Cancel();
            _loadCts?.Dispose();
            _loadCts = new CancellationTokenSource();

            using var timeoutCts = new CancellationTokenSource(timeout.Value);
            using var linkedCts = CancellationTokenSource
                .CreateLinkedTokenSource(_loadCts.Token, timeoutCts.Token);
            var token = linkedCts.Token;

            IsOverlayVisible = true;
            IsCancelLoadEnabled = true;
            try
            {
                var data = await Task.Run(async () =>
                {
                    token.ThrowIfCancellationRequested();
                    using var ctx = _dbFactory.CreateDbContext();
                    return await ctx.FT601s
                        //.Where(x => x.C021 == true && x.Mesoyear == _mesoYear)
                        .Where(x => x.C021 == true)
                        .ToListAsync(token);
                }, token);

                _dataHydra = data;
                _allStepCodeMaster = _dataHydra.Select(x => new StepSelectModel
                {
                    StepItemCode = x.C004,
                    StepItemName = x.C005,
                    Size = x.C002,
                    ArticlePairsShot = x.C013,
                    MoldPairsShot = x.C014,
                    Machine = x.C015,
                    HydraOrderNo = x.C018,
                    FGCode = x.C007,
                    FGName = x.C008,
                    FT601Id = x.Id
                }).Distinct().ToList();

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    StepCodeMaster = _allStepCodeMaster);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Load data failure:\n{ex.Message}", "Load data",
                    (MessageBoxButtons)MessageBoxButton.OK, (MessageBoxIcon)MessageBoxImage.Error);
            }
            finally
            {
                IsOverlayVisible = false;
                IsCancelLoadEnabled = false;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  HARDWARE CALLBACKS (gọi từ code-behind, luôn trên UI thread)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Scale hardware trả về giá trị cân mới (từ ScanAndScale.Core driver).</summary>
        /// KG - G
        public void OnScaleValueChanged(double value, bool stable = false, bool tare = false, string unit = "G")
        {
            _scaleValue = Math.Round(value * 1000, 2);
            ScaleValue = _scaleValue;
            ScaleDisplay = _scaleValue.ToString("F2");
            ScaleStable = stable;
            ScaleTare = tare;
            ScaleUnit = unit;

            if (ScaleStable == false && ReadOnlyScale == true)
            {
                EnableBtnSaveScale = false;
            }
            else //if (ScaleStable == true)
            {
                EnableBtnSaveScale = true;
            }
        }

        /// <summary>RFID đọc được employee code.</summary>
        public void OnRfidValueChanged(string rfidCode)
        {
            try
            {
                _employeeCode = rfidCode;
                if (string.IsNullOrEmpty(_employeeCode))
                    throw new Exception("ID cannot be null.");

                using var db = _dbFactory.CreateDbContext();
                _operatorInfo = db.fT029_Operator_RFIDs
                    .FirstOrDefault(x => x.C000.Contains(_employeeCode));

                if (_operatorInfo == null || _operatorInfo.Id == Guid.Empty)
                {
                    ClearRfidAction?.Invoke();
                    FocusRfidNameAction?.Invoke();
                    throw new Exception(
                        $"Employee ID {_employeeCode} not found. " +
                        "Please enter the name and press Enter to register.");
                }

                _operatorInfo.DepartmentInfor = db.FT031s.FirstOrDefault(x =>
                    x.Id == _operatorInfo.C002 && (x.C000 == "IT" || x.C000 == "QC"));

                if (_operatorInfo.DepartmentInfor == null)
                {
                    _employeeCode = string.Empty;
                    RfidName = string.Empty;
                    ClearRfidAction?.Invoke();
                    throw new Exception("Employee does not have permission for this function.");
                }

                RfidName = _operatorInfo.C001;
                UserName = $"{_operatorInfo.C000} - {_operatorInfo.C001}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RFID error");
                System.Windows.Forms.MessageBox.Show(ex.Message, "ERROR",
                    (MessageBoxButtons)(MessageBoxButtons)MessageBoxButton.OK, (MessageBoxIcon)MessageBoxImage.Error);
            }
        }

        /// <summary>Người dùng nhấn Enter trong ô RFID Name để đăng ký operator mới.</summary>
        public async Task OnRfidNameEnterAsync(string name)
        {
            try
            {
                if (string.IsNullOrEmpty(_employeeCode) || string.IsNullOrEmpty(name))
                    throw new Exception("ID or name cannot be null.");

                var res = System.Windows.Forms.MessageBox.Show(
                    $"Register operator {name} with ID {_employeeCode}?",
                    "Confirm", (MessageBoxButtons)MessageBoxButton.YesNo, (MessageBoxIcon)MessageBoxImage.Question);
                if (res != DialogResult.Yes) return;

                using var db = _dbFactory.CreateDbContext();
                var dept = db.FT031s.FirstOrDefault(x => x.C000 == "QC")
                           ?? throw new Exception("Department 'QC' not found.");

                if (await db.fT029_Operator_RFIDs.AnyAsync(x => x.C000 == _employeeCode))
                    throw new Exception("ID already exists.");

                await db.fT029_Operator_RFIDs.AddAsync(new FT029_Operator_RFID
                {
                    Id = Guid.NewGuid(),
                    C000 = _employeeCode,
                    C001 = name,
                    C002 = dept.Id,
                    CreatedDate = DateTime.Now,
                    CreatedBy = string.Empty,
                    CreatedMachine = Environment.MachineName,
                    Actived = true
                });
                await db.SaveChangesAsync();
                System.Windows.Forms.MessageBox.Show(
                    $"Operator '{_employeeCode}-{name}' registered successfully.", "OK",
                    (MessageBoxButtons)MessageBoxButton.OK, (MessageBoxIcon)(MessageBoxIcon)MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RFID name enter error");
                System.Windows.Forms.MessageBox.Show(ex.Message, "WARNING",
                    (MessageBoxButtons)MessageBoxButton.OK, (MessageBoxIcon)(MessageBoxIcon)MessageBoxImage.Warning);
            }
        }

        /// <summary>Barcode scanner đọc được QR code.</summary>
        public async Task OnBarcodeScannedAsync(string barcode)
        {
            try
            {
                _qrCodeScan = barcode;
                using var db = _dbFactory.CreateDbContext();

                _labelInfo = new FT606_Label();
                _labelInfo = await db.FT606s.FirstOrDefaultAsync(x => x.c001 == _qrCodeScan)
                             ?? throw new Exception("Label information not found.");

                _stepSelected = _dataHydra.FirstOrDefault(x => x.Id == _labelInfo.c000)
                                ?? throw new Exception("Step information not found.");

                FilterStepCombo(_stepSelected.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Barcode scan error");
                System.Windows.Forms.MessageBox.Show(ex.Message, "WARNING",
                    (MessageBoxButtons)MessageBoxButton.OK, (MessageBoxIcon)(MessageBoxIcon)MessageBoxImage.Warning);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  STEP SELECTION
        // ─────────────────────────────────────────────────────────────────────
        private void FilterStepCombo(Guid id)
        {
            var item = _allStepCodeMaster.FirstOrDefault(x => x.FT601Id == id);
            if (item != null)
            {
                _suppress = true;
                SetStepComboAction?.Invoke(item);
                _suppress = false;
                _ = TriggerStepSelectionAsync(item);
            }
            else
                System.Windows.Forms.MessageBox.Show("No matching data found!", "WARNING",
                    (MessageBoxButtons)MessageBoxButton.OK, (MessageBoxIcon)(MessageBoxIcon)MessageBoxImage.Warning);
        }

        /// <summary>Gọi từ code-behind khi DevExpress LookUpEdit thay đổi selection.</summary>
        public async Task OnStepSelectedAsync(StepSelectModel? selected)
        {
            if (_suppress) return;
            try
            {
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
                _logger.LogError(ex, "OnStepSelectedAsync");
                System.Windows.Forms.MessageBox.Show(ex.Message, "WARNING",
                    (MessageBoxButtons)MessageBoxButton.OK, (MessageBoxIcon)(MessageBoxIcon)MessageBoxImage.Warning);
            }
        }

        private async Task TriggerStepSelectionAsync(StepSelectModel _)
        {
            AllowPartitionAdj = false;
            await GetDataAsync(_stepSelected);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  GET DATA – build _scaleDataFinal từ BOM (giống frmShotWeightScale)
        // ─────────────────────────────────────────────────────────────────────
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
                        ?? throw new Exception($"Step item1 code {stepCode.C007} not found.");

                    _allStepsFG = await db.Database
                        .SqlQueryRaw<BomWinlineModel>(
                            "sp_getBomWinlineOfItemFG @itemFG = {0}", stepCode.C007)
                        .AsNoTracking().ToListAsync();

                    foreach (var item in _allStepsFG)
                    {
                        bool allowScale = true;
                        FT601 ckHydra = new();

                        //kiểm tra xem bước hiện tại có đúng với step đang chọn hay không
                        ckHydra = item.ItemStepCode == _stepSelected.C004
                            ? _stepSelected
                            : _dataHydra.FirstOrDefault(x =>
                                //x.C004 == item1.ItemStepCode && x.C007 == item1.ItemFgCode);
                                x.C004 == item.ItemStepCode && x.C020 == item.ItemFgCode.Split('-')[0]);

                        if (ckHydra == null)
                        {
                            if (item.ItemStepCode != "Z-VHXXXXXX" &&
                                item.ItemStepCode.Substring(0, 3) != "REX")
                            {
                                var mc = item.ItemFgCode.Split('-')[0];
                                var smc = item.ItemStepCode.Split('-')[1];
                                ckHydra = _dataHydra.FirstOrDefault(x =>
                                    x.C007.Contains($"{mc}-") || x.C004.Contains($"-{smc}-"));
                                allowScale = false;
                            }
                            else
                            {
                                var parallelInfo = _allStepsFG.Where(x => x.ParallelSequence == item.ParallelSequence).ToList();

                                ckHydra = new FT601
                                {
                                    C007 = item.ItemFgCode,
                                    C008 = item.ItemFgName,
                                    C004 = item.ItemStepCode,
                                    C005 = item.ItemStepName,
                                    C000 = _stepItemCodeScale.C000,
                                    C010 = item.ParallelSequence,
                                };
                            }
                        }

                        var line = new FT600
                        {
                            id = Guid.NewGuid(),
                            C000 = ckHydra?.C000,
                            C001 = ckHydra?.C000 != "21" && _stepItemCodeScale.C000 != "22"
                                        ? EnumSampleLocation.Production : EnumSampleLocation.Sample,
                            C004 = ckHydra?.C015,
                            C005 = ckHydra?.C006,
                            C006 = ckHydra?.C011,
                            C007 = ckHydra?.C012,
                            C009 = 1,
                            C012 = ckHydra?.Id == _labelInfo.c000 ? _labelInfo?.c001 : null,
                            C013 = ckHydra?.C007,
                            C014 = ckHydra?.C008,
                            C016 = null,
                            C017 = ckHydra?.C013,
                            C018 = ckHydra?.C014,
                            C019 = ckHydra?.C016,
                            C020 = ckHydra?.C019,
                            C002 = item.ItemStepCode,
                            C003 = item.ItemStepName,
                            C008 = item.Size,
                            C015 = item.ParallelSequence,
                            C021 = 0,
                            C022 = 0,
                            C023 = 0,
                            C024 = 0,
                            C025 = item.Quantity,
                            C026 = ckHydra?.C020,
                            C027 = ckHydra?.C003,
                            C028 = ckHydra?.C013 != null ? (int)ckHydra.C013 : 0,
                            C029 = ckHydra?.Id == _labelInfo.c000 ? _labelInfo?.Id : null,
                            C032 = ckHydra?.Id,
                            C033 = item.CategoryCode,
                            C034 = item.CategoryName,
                            C035 = _percentOfUsage,
                            C036 = 0,
                            C037 = item.Unit,
                            AllowScale = allowScale
                        };
                        _scaleData.Add(line);
                    }

                    // Trường hợp nhiều size trên 1 khuôn
                    var dataSize = new List<FT600>();
                    foreach (var item in _scaleData)
                    {
                        //var pfx2 = GlobalVariable.PrefixUpToSecondHyphen(item1.C002);
                        //var sameMolds = _dataHydra.Where(x =>
                        //    x.C019 == item1.C020 && x.C015 == item1.C004 &&
                        //    x.C004 != item1.C002 && x.C002 != item1.C008 &&
                        //    GlobalVariable.PrefixUpToSecondHyphen(x.C004) == pfx2 &&
                        //    x.C010 == item1.C015).DistinctBy(x => x.C002).ToList();

                        var pfx2 = GlobalVariable.PrefixUpToThirdHyphen(item.C002);
                        var sameMolds = _dataHydra.Where(x =>
                            x.C019 == item.C020 && x.C015 == item.C004 &&
                            x.C004 != item.C002 && x.C002 != item.C008 &&
                            GlobalVariable.PrefixUpToThirdHyphen(x.C004) == pfx2 &&
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
                                id = Guid.NewGuid(),
                                C000 = ms?.C000,
                                C001 = ms?.C000 != "21" && ms?.C000 != "22"
                                            ? EnumSampleLocation.Production : EnumSampleLocation.Sample,
                                C004 = ms?.C015,
                                C005 = ms?.C006,
                                C006 = ms?.C011,
                                C007 = ms?.C012,
                                C009 = 1,
                                C012 = _labelInfo?.c001,
                                C013 = ms?.C007,
                                C014 = ms?.C008,
                                C017 = ms?.C013,
                                C018 = ms?.C014,
                                C019 = ms?.C016,
                                C020 = ms?.C019,
                                C002 = ms?.C004,
                                C003 = ms?.C005,
                                C008 = ms?.C002,
                                C015 = ms?.C010,
                                C021 = 0,
                                C022 = 0,
                                C023 = 0,
                                C024 = 0,
                                C025 = item.C025,
                                C026 = ms?.C020,
                                C027 = ms?.C003,
                                C028 = ms?.C013 != null ? (int)ms.C013 : 0,
                                C029 = _labelInfo?.Id,
                                C032 = ms?.Id,
                                C033 = cat?.CategoryCode,
                                C034 = cat?.CategoryName,
                                C035 = _percentOfUsage,
                                C037 = cat?.Unit,
                                AllowScale = true
                            });
                        }
                    }

                    _scaleData.AddRange(dataSize.Where(ds =>
                        !_scaleData.Any(sd => sd.C002 == ds.C002 && sd.C015 == ds.C015)));

                    _scaleDataFinal = _scaleData.OrderBy(x => x.C015).ThenBy(x => x.C027).ToList();

                    #region Lọc lại để cho phép cân hay là không các bước không được lấy ra theo step đọc từ QR/selected Lookup
                    /*
                     * Nếu là Logo/Cleat_Ring/Studs(có stepNo ko đứng sau bước Base)/Inlay(chung machine và MoldId) thì chỉ cho phép cân các bước Logo/Cleat_Ring/Studs/Inlay tương ứng, các bước khác sẽ bị xóa khỏi danh sách cân.
                     * Khi quét tem chọn can các bước khác Logo/Cleat_Ring/Studs/Inlay thì sẽ set bit AllowScale = False cho các bước Logo/Cleat_Ring/Studs/Inlay theo các điều kiện như trên ra để không cho phép cân. còn ngaoif ra thì cho phép cân bình thường.
                     * Nếu chọn cân bước cuối mà có các bước trước đó chưa có giá trị cân thì cần phải cân các bước đó trước rồi mới cân bước cuối. Nếu không thì sẽ báo lỗi và không cho phép cân bước cuối.
                     */

                    //var stepAll = _allStepsFG.Select(x => x.ItemStepCode).ToList();
                    var stepAll = _allStepsFG
                                .Select(x => x.ItemStepCode)
                                .ToHashSet();
                    var checkMultiSize = _scaleDataFinal
                                .GroupBy(x => (x.C004, x.C020))
                                .Where(g => g.Count() > 1)
                                .ToDictionary(g => g.Key, g => g.ToList());

                    var startWith = stepCode.C005.StartsWith("Logo") ? "Logo"
                           : stepCode.C005.StartsWith("Studs") ? "Studs"
                           : "Cleat_Ring";

                    //nếu là cân cho bước logo/studs/ring thì xóa các bước khác logo/studs/ring
                    if (stepCode.C005.StartsWith("Logo ")
                        || stepCode.C005.StartsWith("Cleat_Ring")
                      //|| stepCode.C005.StartsWith("Studs ")
                      )
                    {
                        //var previousStep = _allStepsFG.FirstOrDefault(x => x.ParallelSequence == stepCode.C010 - 1);

                        //if (startWith != "Studs"
                        //    || (startWith == "Studs"
                        //        && (previousStep == null
                        //            || (previousStep != null && !previousStep.ItemStepName.StartsWith("Base"))
                        //            )
                        //        )
                        //    )
                        {
                            _scaleDataFinal.RemoveAll(x =>
                            {
                                bool isStartWith = x.C003.StartsWith(startWith);

                                return (!isStartWith);
                                //|| duplicatedSize;
                            });
                        }
                    }
                    //nếu là cân cho bước khác logo/studs/ring thì xóa các bước logo/studs/ring mà không có trong BOM
                    else
                    {
                        //lọc lại nếu bước được chọn cân mà ko phải là Stud hoặc Inlay (nhiều size/1 mold) thì khóa ko cho cân Stud và Inlay
                        //bắt buộc phải cân Stud và Inlay riêng

                        var logosStep = _scaleDataFinal.FirstOrDefault(x => x.C003.StartsWith("Logo"));

                        if (logosStep != null && !stepCode.C005.StartsWith("Logo"))
                        {
                            logosStep.AllowScale = false;
                        }

                        var cleat_RingStep = _scaleDataFinal.FirstOrDefault(x => x.C003.StartsWith("Cleat_Ring"));

                        if (cleat_RingStep != null && !stepCode.C005.StartsWith("Cleat_Ring"))
                        {
                            cleat_RingStep.AllowScale = false;
                        }

                        var studsStep = _scaleDataFinal.FirstOrDefault(x => x.C003.StartsWith("Studs"));

                        //if (studsStep != null && !stepCode.C005.StartsWith("Studs"))
                        //{
                        //    studsStep.AllowScale = false;
                        //}

                        var inlaySteps = _scaleDataFinal.Where(x => x.C003.StartsWith("Inlay")).ToList();

                        var previousStep = _allStepsFG.FirstOrDefault(x => x.ParallelSequence == stepCode.C010 - 1);

                        if ((inlaySteps.Count() > 1 && !stepCode.C005.StartsWith("Inlay"))
                            && (!stepCode.C005.StartsWith("Studs")
                                || (stepCode.C005.StartsWith("Studs") && previousStep.ItemStepName.StartsWith("Base"))
                                )
                            )
                        {
                            inlaySteps.ForEach(x => x.AllowScale = false);

                            //check allow scle for studs
                            if (studsStep != null)
                            {
                                var previousStepBse = _allStepsFG.FirstOrDefault(x => x.ParallelSequence == studsStep.C015 - 1);

                                studsStep.AllowScale = !(previousStep == null
                                                            || (previousStep != null && !previousStep.ItemStepName.StartsWith("Base")
                                                                )
                                                         );
                            }

                            //xóa những step ko có trong BOM ra
                            _scaleDataFinal.RemoveAll(x =>
                            {
                                bool isLogo = x.C003.StartsWith("Logo");
                                bool isStuds = x.C003.StartsWith("Studs");
                                bool isRing = x.C003.StartsWith("Cleat_Ring");
                                bool isInlay = x.C003.StartsWith("Inlay");

                                bool hasC002 = stepAll.Contains(x.C002);

                                bool duplicatedSize = checkMultiSize.ContainsKey((x.C004, x.C020));

                                return (isLogo || isStuds || isRing || isInlay) && !hasC002;
                                //|| duplicatedSize;
                            });
                        }
                        else if (stepCode.C005.StartsWith("Inlay")
                                || (stepCode.C005.StartsWith("Studs")
                                    && (previousStep == null
                                        || (previousStep != null && !previousStep.ItemStepName.StartsWith("Base"))
                                        )
                                   )
                                )
                        {
                            var startWith1 = stepCode.C005.StartsWith("Studs") ? "Studs" : "Inlay";

                            _scaleDataFinal.RemoveAll(x =>
                            {
                                bool isStartWith = x.C003.StartsWith(startWith1);

                                return (!isStartWith);
                                //|| duplicatedSize;
                            });
                        }
                    }
                    #endregion

                    // Đọc dữ liệu các bước đã cân trước đó
                    foreach (var item1 in _scaleDataFinal)
                    {
                        var catCheck = GlobalVariable.ConfigSystem.CategoryOfNonInjectionUsagePartial?
                            .FirstOrDefault(x => x.CategoryCode == item1.C033);

                        if (item1.C002?.Substring(0, 3) == "REX")
                        {
                            var parallelStep = _scaleDataFinal
                                //.Where(x => x.C015 == item1.C015 && !string.IsNullOrEmpty(x.C026));
                                .Where(x => x.C015 == item1.C015 && x.C002 != item1.C002)
                                .ToList();

                            var updateMainInfo = parallelStep.FirstOrDefault(x => !string.IsNullOrEmpty(x.C026));
                            parallelStep.ForEach(x =>
                            {
                                x.C026 = updateMainInfo?.C026;
                                x.C027 = updateMainInfo?.C027;
                            });

                            var paisShotFinal = catCheck == null
                                ? parallelStep.FirstOrDefault()?.C028 : parallelStep.Sum(x => x.C028);

                            var paisShotPPIC = catCheck == null
                              ? parallelStep.FirstOrDefault()?.C017 : parallelStep.Sum(x => x.C017);

                            item1.C026 = parallelStep.FirstOrDefault()?.C026;
                            item1.C027 = parallelStep.FirstOrDefault()?.C027;
                            item1.C028 = paisShotFinal;
                            item1.C017 = paisShotPPIC;
                            item1.C018 = parallelStep.FirstOrDefault()?.C018 ?? 0;
                        }

                        if (!item1.C003!.StartsWith("Stud") && !item1.C003.StartsWith("Logo") &&
                            !item1.C003.StartsWith("Cleat_Ring") && !item1.C002!.StartsWith("REX") && !item1.C003!.StartsWith("Inlay"))
                            continue;

                        FT600? stepPrevious;
                        if (item1.C002 != "Z-VHXXXXXX" && item1.C002.Substring(0, 3) != "REX")
                        {
                            var mainCode = item1.C002.Split('-')[1];
                            stepPrevious = await db.FT600s
                                .Where(x => x.C015 == item1.C015 &&
                                    (x.C002 == item1.C002 ||
                                     (x.C002!.Contains(mainCode) && x.C008 == item1.C008)))
                                .OrderByDescending(x => x.CreatedDate).FirstOrDefaultAsync();
                        }
                        else
                        {
                            stepPrevious = await db.FT600s
                                .Where(x => x.C002 == item1.C002)
                                .OrderByDescending(x => x.CreatedDate).FirstOrDefaultAsync();
                        }

                        if (stepPrevious != null)
                        {
                            _percentOfUsage = (double)stepPrevious.C035!;
                            if (item1.C002.StartsWith("REX"))
                            {
                                var total = catCheck == null
                                    ? stepPrevious?.C036 * item1.C025
                                    : stepPrevious?.C036;
                                var usage = (double)Math.Round(
                                    (decimal)(total * _percentOfUsage / 100), 3);
                                var unusage = total - usage;

                                item1.C021 = catCheck == null ? usage : usage / item1.C028;
                                item1.C022 = catCheck == null ? unusage : unusage / item1.C028;
                                item1.C023 = catCheck == null ? usage : usage / item1.C028;
                                item1.C024 = total;
                                item1.C035 = stepPrevious!.C035;
                                item1.C036 = stepPrevious?.C036;
                            }
                            else
                            {
                                item1.C021 = stepPrevious.C021;
                                item1.C022 = stepPrevious.C022;
                                item1.C023 = stepPrevious.C023;
                                item1.C024 = stepPrevious.C024;
                                item1.C028 = stepPrevious.C028;
                                item1.C035 = stepPrevious.C035;
                                item1.C036 = stepPrevious?.C036;
                            }
                        }
                    }

                    _scaleDataFinal = _scaleDataFinal.OrderBy(x => x.C015).ThenBy(x => x.C004).ToList();

                    var stepSel = _scaleDataFinal.FirstOrDefault(x => x.C002 == _stepSelected.C004);
                    var prevSteps = _scaleDataFinal.Where(x => x.AllowScale == true && x.C015 < stepSel?.C015).ToList();
                    if (prevSteps.Any(x => x.C021 == 0))
                    {
                        _rowSelected = prevSteps.Where(x => x.C021 == 0).OrderBy(od => od.C015).FirstOrDefault();
                        System.Windows.Forms.MessageBox.Show("The previous step has not been weighed.", "Warning",
                            (MessageBoxButtons)MessageBoxButton.OK, (MessageBoxIcon)(MessageBoxIcon)MessageBoxImage.Warning);
                    }
                    else
                        _rowSelected = _scaleDataFinal
                            .FirstOrDefault(x => x.C002 == _stepSelected.C004) ?? new FT600();
                }
                else
                {
                    var rowSelect = _scaleDataFinal.FirstOrDefault(x =>
                        x.C002 == _stepSelected.C004 &&
                        x.C013 == _stepSelected.C007 &&
                        x.C004 == _stepSelected.C015);

                    if (rowSelect == null)
                    {
                        System.Windows.Forms.MessageBox.Show(
                            $"Label does not match the item1 being weighed.\n{_stepSelected.C004}|{_stepSelected.C005}",
                            "WARNING", (MessageBoxButtons)MessageBoxButton.OK, (MessageBoxIcon)MessageBoxImage.Warning);
                        return;
                    }
                    if (!rowSelect.AllowScale)
                    {
                        System.Windows.Forms.MessageBox.Show("Do not allow to scale this step.", "Warning",
                            (MessageBoxButtons)MessageBoxButton.OK, (MessageBoxIcon)(MessageBoxIcon)MessageBoxImage.Information);
                        return;
                    }
                    foreach (var step in _scaleDataFinal.Where(x => x.C015 < rowSelect.C015))
                    {
                        if (step.C021 == 0)
                        {
                            System.Windows.Forms.MessageBox.Show("The previous step has not been weighed.", "Warning",
                                (MessageBoxButtons)MessageBoxButton.OK, (MessageBoxIcon)MessageBoxImage.Warning);
                            return;
                        }
                    }
                    rowSelect.C012 = _labelInfo?.c001;
                    rowSelect.C029 = _labelInfo?.Id;
                    _rowSelected = rowSelect;
                }

                _articlePaisShotFinaly = _rowSelected.C028;
                RefreshUI(false);
                FocusGridRowAction?.Invoke(_rowSelected.C002);

                IsHistoryExpanded = true;
                _historyExpanded = true;

                await LoadRefHistoryAsync(_rowSelected);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetDataAsync error");
                System.Windows.Forms.MessageBox.Show(ex.Message, "WARNING",
                    (MessageBoxButtons)MessageBoxButton.OK, (MessageBoxIcon)MessageBoxImage.Warning);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  SAVE WEIGHT (logic C021/C022/C023/C024 từ frmShotWeightScale)
        // ─────────────────────────────────────────────────────────────────────
        private void ExecuteSave(object? _)
        {
            if (_rowSelected == null) return;

            if (!_rowSelected.AllowScale)
            {
                System.Windows.Forms.MessageBox.Show("Cannot scale this step.", "Warning",
                    (MessageBoxButtons)MessageBoxButton.OK, (MessageBoxIcon)MessageBoxImage.Warning);
                return;
            }

            var unweighedPrevSteps = _scaleDataFinal
                .Where(x => x.C015 < _rowSelected.C015 && (x.C021 ?? 0) == 0)
                .OrderBy(x => x.C015)
                .ToList();

            if (unweighedPrevSteps.Count > 0)
            {
                var detail = string.Join(Environment.NewLine, unweighedPrevSteps.Select((x, i) =>
                    $"{i + 1}. Step {x.C015} [{x.C002}]: {x.C003} (Size:{x.C008})"));

                System.Windows.Forms.MessageBox.Show(
                    $"{unweighedPrevSteps.Count} previous step(s) are missing Part Weight and must be weighed first:" +
                    $"{Environment.NewLine}{Environment.NewLine}{detail}",
                    "Warning", (MessageBoxButtons)MessageBoxButton.OK, (MessageBoxIcon)MessageBoxImage.Warning);
                return;
            }

            if (!(_rowSelected.C002?.StartsWith("REX") ?? false))
            {
                // ── Injection items ──────────────────────────────────────────
                if (_rowSelected.C024 == 0)
                {
                    // Lần cân đầu tiên: ghi tổng khối lượng runner + part
                    _rowSelected.C024 = _scaleValue;
                }
                else
                {
                    // Lần cân thứ hai: ghi khối lượng part weight (g/prs)
                    _rowSelected.C023 = _scaleValue;

                    var prsShot = _articlePaisShotFinaly;

                    // C022 = Runner weight (g/prs)
                    // = (TotalWeight – PartWeight×pairs) / pairs
                    _rowSelected.C022 = RunnerText == "YES"
                        ? Math.Round(
                            ((_rowSelected.C024 - (_rowSelected.C023 * (double?)prsShot)) /
                             (double?)prsShot) ?? 0, 3)
                        : 0;

                    if (!_rowSelected.C003.StartsWith("Logo") &&
                        !_rowSelected.C003.StartsWith("Cleat_Ring"))
                    {
                        // C021 = Part weight thực tế (g/prs)
                        // = C023(bước này) − ∑C023(bước trước) − ∑C023(non-injection cùng bước)
                        var previuosStep = _scaleDataFinal
                            .Where(x => x.C015 == _rowSelected.C015 - 1).ToList();

                        var nonInjection = _scaleDataFinal
                            .Where(x => x.C015 == _rowSelected.C015 &&
                                        (x.C002 == "Z-VHXXXXXX" ||
                                         (x.C002?.StartsWith("REX") ?? false))).ToList();

                        if (!_rowSelected.C003.StartsWith("Studs"))
                        {
                            _rowSelected.C021 = _rowSelected.C023
                                                - previuosStep?.Sum(x => x.C023)
                                                - nonInjection?.Sum(x => x.C023);
                        }
                        else if (_rowSelected.C003.StartsWith("Studs"))
                        {
                            if (previuosStep == null
                                || !previuosStep.Any()
                                || (previuosStep != null
                                    && previuosStep.Any()
                                    && (previuosStep.FirstOrDefault().C003.StartsWith("Logo")
                                        || previuosStep.FirstOrDefault().C003.StartsWith("Cleat_Ring")
                                        || previuosStep.FirstOrDefault().C003.StartsWith("Inlay")
                                        )
                                    )
                                )
                            {
                                _rowSelected.C021 = _rowSelected.C023;
                            }
                            else
                            {
                                _rowSelected.C021 = _rowSelected.C023
                                                    - previuosStep?.Sum(x => x.C023)
                                                    - nonInjection?.Sum(x => x.C023);
                            }
                        }
                    }
                    else
                    {
                        _rowSelected.C021 = _rowSelected.C023;
                        // C036 = trọng lượng/piece (Studs, Logo, Cleat_Ring)
                        _rowSelected.C036 = Math.Round(_scaleValue / _rowSelected.C025 ?? 1.0, 2);
                    }
                    //// C036 = trọng lượng/piece (Studs, Logo, Cleat_Ring)
                    //_rowSelected.C036 =
                    //    (_rowSelected.C003?.StartsWith("Studs") ?? false) ||
                    //    (_rowSelected.C003?.StartsWith("Logo") ?? false) ||
                    //    (_rowSelected.C003?.StartsWith("Cleat_Ring") ?? false)
                    //        ? Math.Round(_scaleValue / (double?)prsShot ?? 1.0, 2)
                    //        : 0;
                }
            }
            else
            {
                // ── Non-injection items (REX…) ───────────────────────────────
                var catCheck = GlobalVariable.ConfigSystem.CategoryOfNonInjectionUsagePartial?
                    .FirstOrDefault(x => x.CategoryCode == _rowSelected.C033);

                if (catCheck == null)
                {
                    // Receptacle: toàn bộ scale value = part weight
                    _rowSelected.C024 = _scaleValue;
                    _rowSelected.C023 = _scaleValue;
                    _rowSelected.C021 = _scaleValue;
                    _rowSelected.C036 = Math.Round(_scaleValue / (_rowSelected.C025 ?? 1), 2);
                }
                else
                {
                    // Nonwoven / Mesh: chỉ dùng một phần theo %
                    var usage = (double)Math.Round(
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

            // ── Multi-size mold: phân bổ C024 và tính C022 theo tỉ lệ ────────
            var pfx2 = GlobalVariable.PrefixUpToSecondHyphen(_rowSelected.C002);
            var sameMolds = _scaleDataFinal.Where(x =>
                x.C020 == _rowSelected.C020 &&
                x.C004 == _rowSelected.C004 &&
                GlobalVariable.PrefixUpToSecondHyphen(x.C002) == pfx2 &&
                x.C015 == _rowSelected.C015).ToList();

            if (sameMolds.Count > 1)
            {
                var sumPW = sameMolds.Sum(x => x.C023 * x.C028);
                var pairShot = sameMolds.Sum(x => x.C028);
                foreach (var s in sameMolds)
                {
                    s.C024 = _rowSelected.C024;
                    s.C022 = s.C023 > 0 ? (_rowSelected.C024 - sumPW) / pairShot : 0;
                }
            }

            // ── Cascade: cập nhật C021/C022 cho các bước sau ────────────────
            var toUpdate = _scaleDataFinal.Where(x =>
                    x.C015 >= _rowSelected.C015 && x.C024 > 0 &&
                    !x.C003!.StartsWith("Stud") && !x.C003.StartsWith("Inlay") &&
                    !x.C003.StartsWith("Ring") &&
                    !(x.C002?.StartsWith("REX") ?? false) &&
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
                    var prev = toUpdate.FirstOrDefault(x => x.C015 < item.C015 - 1);
                    item.C021 = item.C023 - prev?.C023;
                    item.C022 = item.C024 - item.C021;
                }
            }

            RefreshUI(false);
            FocusGridRowAction?.Invoke(_rowSelected.C002);
            UpdateReferencePanel();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  CONFIRM (lưu DB)
        // ─────────────────────────────────────────────────────────────────────
        private async Task ExecuteConfirmAsync(object? _)
        {
            using var db = _dbFactory.CreateDbContext();
            using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                if (_operatorInfo == null || _operatorInfo.Id == Guid.Empty)
                {
                    System.Windows.Forms.MessageBox.Show("RFID card not yet scanned.", "Warning",
                        (MessageBoxButtons)MessageBoxButton.OK, (MessageBoxIcon)MessageBoxImage.Warning);
                    return;
                }

                var pfx2 = GlobalVariable.PrefixUpToSecondHyphen(_rowSelected.C002);
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
                            System.Windows.Forms.MessageBox.Show(
                                $"Scale not completed for step: {item.C002}.", "Warning",
                                (MessageBoxButtons)MessageBoxButton.OK, (MessageBoxIcon)MessageBoxImage.Warning);
                            return;
                        }
                    }
                }

                var now = DateTime.Now;
                var machine = Environment.MachineName;
                var insert = _scaleDataFinal
                    .Where(x => x.AllowScale == true && x.C021 > 0).ToList();

                insert.ForEach(x =>
                {
                    x.C010 = _operatorInfo.C000;
                    x.C011 = _operatorInfo.C001;
                    x.CreatedDate = now;
                    x.CreatedMachine = machine;
                    x.Mesocomp = _mesocomp;
                    x.Mesoyear = _mesoYear;
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

                System.Windows.Forms.MessageBox.Show("Scale shot weight saved successfully.", "Information",
                    (MessageBoxButtons)MessageBoxButton.OK, (MessageBoxIcon)MessageBoxImage.Information);

                #region check update to WL
                //await UpdateShotweightWlAsync(insert);
                #endregion

                _labelInfo = new FT606_Label();
                ClearStepComboAction?.Invoke();

                _qrCodeScan = string.Empty;
                ClearBarcodeAction?.Invoke();

                ResetNewLoop();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Confirm error");
                System.Windows.Forms.MessageBox.Show(
                    $"Transaction error: {ex.Message}\n{ex.InnerException?.Message}",
                    "ERROR", (MessageBoxButtons)MessageBoxButton.OK, (MessageBoxIcon)MessageBoxImage.Error);
            }
        }

        //
        private async Task UpdateShotweightWlAsync(List<FT600> insert)
        {
            using var db = _dbFactory.CreateDbContext();
            var now = DateTime.Now;
            var machine = Environment.MachineName;

            var dt = new DataTable();

            dt.Columns.Add(nameof(WeightUpdateModel.ItemCode), typeof(string));
            dt.Columns.Add(nameof(WeightUpdateModel.PartWeight_C201), typeof(double));
            dt.Columns.Add(nameof(WeightUpdateModel.RunnerWeight_C022), typeof(double));
            dt.Columns.Add(nameof(WeightUpdateModel.MachineGroup_C019), typeof(string));
            dt.Columns.Add(nameof(WeightUpdateModel.MoldId_C020), typeof(string));

            var itemJoin = string.Join(",", insert
                                .Select(x => x.C002));
            //.Distinct());

            var curentShotweight = await db.Database.SqlQueryRaw<GetShotWeightWL_DTO>("sp_Shotweight_Gets @Items = {0}", itemJoin).ToListAsync();

            foreach (var item in curentShotweight)
            {
                var itemUpdate = insert.FirstOrDefault(x => x.C002 == item.ItemCode_C016);

                if (itemUpdate == null) continue;

                //só sánh nếu nằm trong ngưỡng cho phép thì update WL
                var delta = ((itemUpdate.C021 + itemUpdate.C022) - item.ShotWeight) / item.ShotWeight * 100;

                if (delta >= -GlobalVariable.ConfigSystem.DeltaUpWL && delta <= GlobalVariable.ConfigSystem.DeltaUpWL)
                {
                    dt.Rows.Add(
                        itemUpdate.C002,
                        itemUpdate.C021,
                        itemUpdate.C022,
                        itemUpdate.C019,
                        itemUpdate.C020);
                }
            }

            var tvpParam = new SqlParameter("@Items", dt)
            {
                SqlDbType = SqlDbType.Structured,
                TypeName = "dbo.WeightUpdateType"
            };

            var result = await db.Set<ResultModel>()
                .FromSqlRaw(
                    "EXEC sp_Showweight_UpdateToWinline @Items",
                    tvpParam)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (result?.Result == true)
            {
                var createdAt = DateTime.Now;
                var createdBy = _operatorInfo.C001;
                var createdMachine = Environment.MachineName;

                var insertFT609 = new List<FT609_ShotWeightUpToWL>();
                var itemSuccess = result?.Message.Split(";");
                foreach (var item in insert)
                {
                    var ck = itemSuccess.Contains(item.C002);

                    if (!ck) continue;

                    var curentSwWl = curentShotweight.FirstOrDefault(x => x.ItemCode_C016 == item.C002);
                    var delta = Math.Round((double)(((item.C021 + item.C022) - curentSwWl.ShotWeight) / curentSwWl.ShotWeight * 100), 2);

                    insertFT609.Add(new FT609_ShotWeightUpToWL()
                    {
                        Id = Guid.NewGuid(),
                        CreatedDate = createdAt,
                        CreatedBy = createdBy,
                        CreatedMachine = createdMachine,
                        Actived = true,
                        C000 = item.C002,
                        C001 = item.C003,
                        C002 = item.C026,
                        C003 = item.C027,
                        C004 = item.C021,
                        C005 = item.C022,
                        C006 = Math.Round((double)(item.C021 + item.C022), 3),
                        C007 = true,
                        C008 = item.C019,
                        C009 = item.C020,
                        C010 = item.id.ToString(),
                        C011 = curentSwWl.PartWeight_c063,
                        C012 = curentSwWl.RunnerWeight_c064,
                        C013 = curentSwWl.ShotWeight,
                        C014 = delta,
                    });
                }

                //deactive giá trị cũ
                var updateInactive = insertFT609.Select(x => x.C000).ToList();
                await db.FT609_ShotWeightUpToWLs
                .Where(b => updateInactive.Contains(b.C000) && b.C007 == true)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(b => b.C007, false)
                    .SetProperty(b => b.ModifiedDate, now)
                    .SetProperty(b => b.ModifiedMachine, machine));

                //insert thông tin shotweight mới vào.
                await db.FT609_ShotWeightUpToWLs.AddRangeAsync(insertFT609);

                await db.SaveChangesAsync();

                System.Windows.Forms.MessageBox.Show("Update data to Winline saved successfully.", "Information",
             (MessageBoxButtons)MessageBoxButton.OK, (MessageBoxIcon)MessageBoxImage.Information);
            }
            else
            {
                // Error
                var msg = result?.Message;
                System.Windows.Forms.MessageBox.Show(msg, "ERROR",
              (MessageBoxButtons)MessageBoxButton.OK, (MessageBoxIcon)MessageBoxImage.Error);
                _logger.LogError(msg, "Update WL error.");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  CANCEL
        // ─────────────────────────────────────────────────────────────────────
        private void ExecuteCancel(object? _)
        {
            _labelInfo = new FT606_Label();
            _qrCodeScan = string.Empty;
            _remarkFinal = string.Empty;

            ClearStepComboAction?.Invoke();
            ClearBarcodeAction?.Invoke();
            ResetNewLoop();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  GRID ROW ACTIONS
        // ─────────────────────────────────────────────────────────────────────
        public void OnGridScale(FT600? rowSelect)
        {
            if (rowSelect == null) return;
            AllowPartitionAdj = false;

            if (!rowSelect.AllowScale)
            {
                System.Windows.Forms.MessageBox.Show("Do not allow to scale this step.", "Warning",
                    (MessageBoxButtons)MessageBoxButton.OK, (MessageBoxIcon)MessageBoxImage.Warning);
                return;
            }
            foreach (var step in _scaleDataFinal.Where(x => x.C015 < rowSelect.C015))
            {
                if (step.C021 == 0)
                {
                    System.Windows.Forms.MessageBox.Show("The previous step has not been weighed.", "Warning",
                        (MessageBoxButtons)MessageBoxButton.OK, (MessageBoxIcon)MessageBoxImage.Warning);
                    return;
                }
            }
            _rowSelected = rowSelect;
            _articlePaisShotFinaly = _rowSelected.C017 != 0 && _rowSelected.C017 == _rowSelected.C028
                ? _rowSelected.C017 : _rowSelected.C028;

            IsHistoryExpanded = true;
            _historyExpanded = true;

            RefreshUI(true);
            UpdateReferencePanel();
            _ = LoadRefHistoryAsync(_rowSelected);
        }

        public void OnGridReset(FT600? rowSelect)
        {
            if (rowSelect == null) return;

            var rowReset = _scaleDataFinal.FirstOrDefault(x =>
                x.AllowScale && x.C002 == rowSelect.C002);
            if (rowReset == null)
            {
                System.Windows.Forms.MessageBox.Show("Cannot reset this line.", "Warning",
                    (MessageBoxButtons)MessageBoxButton.OK, (MessageBoxIcon)MessageBoxImage.Information);
                return;
            }
            rowReset.C021 = rowReset.C022 = rowReset.C023 = rowReset.C024 = 0;
            _rowSelected = rowSelect;
            _articlePaisShotFinaly = _rowSelected.C017 != 0 && _rowSelected.C017 == _rowSelected.C028
                ? _rowSelected.C017 : _rowSelected.C028;
            RefreshUI(true);
            UpdateReferencePanel();
        }

        public void OnGridDelete(FT600? rowSelect)
        {
            if (rowSelect == null) return;

            var rowReset = _scaleDataFinal.FirstOrDefault(x =>
                x.AllowScale && x.C002 == rowSelect.C002);
            if (rowReset == null)
            {
                System.Windows.Forms.MessageBox.Show("Cannot reset this line.", "Warning",
                    (MessageBoxButtons)MessageBoxButton.OK, (MessageBoxIcon)MessageBoxImage.Information);
                return;
            }

            _scaleDataFinal.Remove(rowReset);

            RefreshUI(true);
            UpdateReferencePanel();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  FIELD ENTER HANDLERS (gọi từ code-behind khi nhấn Enter)
        // ─────────────────────────────────────────────────────────────────────
        public void OnActualPairsEnter(string text)
        {
            _articlePaisShotFinaly = double.TryParse(text, out var v) ? v : 0;
            var row = _scaleDataFinal.FirstOrDefault(x => x.C032 == _rowSelected.C032);
            if (row != null) row.C028 = _articlePaisShotFinaly;
            RefreshUI(true);
        }

        public void OnUsagePctEnter(string text)
        {
            var catCheck = GlobalVariable.ConfigSystem.CategoryOfNonInjectionUsagePartial?
                .FirstOrDefault(x => x.CategoryCode == _rowSelected.C033);
            if (!(_rowSelected?.C002?.StartsWith("REX") ?? false) ||
                (_rowSelected?.C002?.StartsWith("REX") == true && catCheck == null)) return;

            _percentOfUsage = double.TryParse(text, out var v) ? v : 0;

            _scaleDataFinal?.Where(x => x.C002?.StartsWith("REX") ?? false).ToList().ForEach(x =>
            {
                var usage = x.C035 == 100
                    ? (double)Math.Round(
                        (decimal)(_rowSelected!.C024 * _percentOfUsage / 100), 3)
                    : (double)Math.Round(
                        (decimal)((decimal)(_rowSelected!.C024 * _percentOfUsage / 100) /
                                   (decimal)x.C028!), 3);

                var unusage = _rowSelected!.C035 == 100
                    ? _rowSelected.C024 - usage
                    : (_rowSelected.C024 - usage * x.C028) / x.C028;

                x.C035 = _percentOfUsage;
                x.C023 = usage;
                x.C021 = usage;
                x.C022 = unusage;
            });

            RefreshUI(true);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  HISTORY TOGGLE
        // ─────────────────────────────────────────────────────────────────────
        public void ToggleHistory(object? _ = null)
        {
            _historyExpanded = !_historyExpanded;
            IsHistoryExpanded = _historyExpanded;

            var current = HistoryToggleText.TrimStart('▼', '▶', ' ');
            HistoryToggleText = (_historyExpanded ? "▼" : "▶") + "  " + current;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  HYDRA SYNC
        // ─────────────────────────────────────────────────────────────────────
        private async Task GetDataHydraAsync(object? _)
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

                var ft601s = new List<FT601>();
                var now = DateTime.Now;
                var machine = Environment.MachineName;
                var toInsert = _hydraItemDetails.Where(d =>
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

                await LoadDataAsync();
            }
            catch (Exception ex) { _logger.LogError(ex, "GetDataHydra error"); }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  HISTORY REFERENCE
        // ─────────────────────────────────────────────────────────────────────
        private async Task LoadRefHistoryAsync(FT600 StepInfo)
        {
            try
            {
                if (StepInfo == null) return;

                using var db = _dbFactory.CreateDbContext();
                _refHistory = await db.FT600s
                    .Where(x => x.C002 == StepInfo.C002)
                    .OrderByDescending(x => x.CreatedDate)
                    .Take(15).ToListAsync();

                _stdRow = new FT600
                {
                    C024 = AverageOrNull(_refHistory.Select(x => x.C024)),
                    C023 = AverageOrNull(_refHistory.Select(x => x.C023)),
                    C021 = AverageOrNull(_refHistory.Select(x => x.C021)),
                    C022 = AverageOrNull(_refHistory.Select(x => x.C022)),
                };
                UpdateReferencePanel();

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    HistoryCollection.Clear();
                    foreach (var h in _refHistory)
                        HistoryCollection.Add(h);

                    var toggle = (_historyExpanded ? "▼" : "▶");
                    HistoryToggleTextDetail = $"Last {_refHistory.Count} weighings  ·  {StepInfo.C002}  ·  {StepInfo.C008}  ·  {StepInfo.C007}";

                    //HistoryToggleText = $"{toggle}  REFERENCE / HISTORY";
                    HistoryToggleText = $"REFERENCE / HISTORY";

                    HistoryToggleTextSecond = $"{StepInfo.C003}";

                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LoadRefHistoryAsync error");
            }
        }

        private static double? AverageOrNull(IEnumerable<double?> values)
        {
            var positive = values.Where(v => v > 0).Select(v => v!.Value).ToList();
            return positive.Count > 0 ? Math.Round(positive.Average(), 3) : null;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  REFERENCE PANEL UPDATE
        // ─────────────────────────────────────────────────────────────────────
        private void UpdateReferencePanel()
        {
            _referenceRows = new List<ReferenceRow>
            {
                new() { No=1, FieldName="Total Weight",           Unit="g",
                        Std    = _stdRow?.C024 > 0 ? _stdRow.C024 : null,
                        Actual = _rowSelected?.C024 > 0 ? _rowSelected.C024 : null },
                new() { No=2, FieldName="Total Part Weight",             Unit="g/prs",
                        Std    = _stdRow?.C023 > 0 ? _stdRow.C023 : null,
                        Actual = _rowSelected?.C023 > 0 ? _rowSelected.C023 : null },
                new() { No=3, FieldName="Part Weight",                 Unit="g/prs",
                        Std    = _stdRow?.C021 > 0 ? _stdRow.C021 : null,
                        Actual = _rowSelected?.C021 > 0 ? _rowSelected.C021 : null },
                new() { No=4, FieldName="Runner / Excess Mat. Weight", Unit="g/prs",
                        Std    = _stdRow?.C022 > 0 ? _stdRow.C022 : null,
                        Actual = _rowSelected?.C022 > 0 ? _rowSelected.C022 : null }
            };

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                RefRowsCollection.Clear();
                foreach (var row in _referenceRows)
                    RefRowsCollection.Add(row);

                // Tô màu viền Scale Card theo tolerance
                var worst = _referenceRows
                    .OrderByDescending(r => (int)r.Tolerance)
                    .FirstOrDefault()?.Tolerance ?? ToleranceCategory.Idle;

                ScaleCardBorderBrush = worst switch
                {
                    ToleranceCategory.Ok => new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                    ToleranceCategory.Warn => new SolidColorBrush(Color.FromRgb(255, 152, 0)),
                    ToleranceCategory.Err => new SolidColorBrush(Color.FromRgb(244, 67, 54)),
                    _ => new SolidColorBrush(Color.FromRgb(207, 216, 220))
                };

                ScaleCardBackground = worst switch
                {
                    ToleranceCategory.Ok => new SolidColorBrush(Color.FromRgb(212, 247, 220)), // xanh nhạt
                    ToleranceCategory.Warn => new SolidColorBrush(Color.FromRgb(255, 243, 205)), // vàng nhạt
                    ToleranceCategory.Err => new SolidColorBrush(Color.FromRgb(253, 232, 232)), // đỏ nhạt
                    _ => new SolidColorBrush(Color.FromRgb(245, 245, 245))  // xám nhạt
                };
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        //  UI REFRESH
        // ─────────────────────────────────────────────────────────────────────
        private void RefreshUI(bool refreshInPlace = true)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                // Step info panel
                StepCode = _rowSelected?.C002;
                Machine = _rowSelected?.C004;
                Size = _rowSelected?.C008;
                StepIndex = _rowSelected?.C015?.ToString();
                ActualPairs = _rowSelected?.C028?.ToString();
                Qty = _rowSelected?.C025?.ToString();
                FgItemCode = _rowSelected?.C013;
                FgName = _rowSelected?.C014;
                UsagePct = (_rowSelected?.C035 != 0
                                  ? _rowSelected?.C035 : _percentOfUsage)?.ToString();
                Remark = _remarkFinal;
                StepName = _rowSelected?.C003;


                // Enable/disable Usage %
                var catCheck = GlobalVariable.ConfigSystem.CategoryOfNonInjectionUsagePartial?
                    .FirstOrDefault(x => x.CategoryCode == _rowSelected?.C033);
                IsUsagePctReadOnly =
                    !(_rowSelected?.C002?.StartsWith("REX") == true && catCheck != null);

                // Status dots + bars
                UpdateStepStatuses();

                // Rebuild grid collection
                StepsCollection.Clear();
                if (_scaleDataFinal != null)
                    foreach (var item in _scaleDataFinal)
                        StepsCollection.Add(item);

                UpdateReferencePanel();
            });
        }

        private void UpdateStepStatuses()
        {
            foreach (var item in _scaleDataFinal ?? new())
            {
                bool isDone = item.C021 > 0 ||
                    (!(item.C002?.StartsWith("REX") ?? false) ? item.C024 > 0 : item.C023 > 0);
                bool isActive = item.C002 == _rowSelected?.C002 &&
                                item.C032 == _rowSelected?.C032;

                item.StatusText = isDone ? "Done" : isActive ? "Active" : "Pending";
                item.StatusDotColor = isDone ? "#4CAF50" : isActive ? "#1976D2" : "#9E9E9E";
                item.StatusBarColor = isActive ? "#1565C0" : isDone ? "#A5D6A7" : "Transparent";
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  RESET LOOP
        // ─────────────────────────────────────────────────────────────────────
        private void ResetNewLoop()
        {
            _rowSelected = new FT600();
            _allStepsFG = new List<BomWinlineModel>();
            _stepItemCodeScale = new FT601();
            _scaleData = new List<FT600>();
            _scaleDataFinal = new List<FT600>();
            _newScale = true;
            _qrCodeScan = string.Empty;
            _percentOfUsage = GlobalVariable.ConfigSystem.PercentOfUserNonWoven;
            _articlePaisShotFinaly = 0;
            _remarkFinal = string.Empty;
            _stdRow = new FT600();
            _refHistory = new List<FT600>();

            HistoryToggleTextDetail = HistoryToggleTextSecond = string.Empty;
            HistoryCollection.Clear();

            RunnerText = "YES";

            RefreshUI(false);
            UpdateReferencePanel();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  NAVIGATION COMMANDS
        // ─────────────────────────────────────────────────────────────────────
        private void OpenHistoryView()
        {
            var nf = _serviceProvider.GetRequiredService<frmMainView>();
            nf.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            nf.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            nf.ShowDialog();
        }

        private void OpenSettings()
        {
            var nf = _serviceProvider.GetRequiredService<frmUpdateMasterData>();
            nf.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            nf.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            nf.ShowDialog();
            _ = LoadDataAsync(TimeSpan.FromSeconds(30));
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
                System.Windows.Forms.MessageBox.Show(ex.Message, "Error", (MessageBoxButtons)MessageBoxButton.OK, (MessageBoxIcon)MessageBoxImage.Error);
            }
        }

        private void ToggleMaximize()
        {
            var win = System.Windows.Application.Current.MainWindow;
            win.WindowState = win.WindowState == System.Windows.WindowState.Normal
                ? System.Windows.WindowState.Maximized
                : System.Windows.WindowState.Normal;
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
                    "Update", (MessageBoxButtons)MessageBoxButton.YesNo, (MessageBoxIcon)MessageBoxImage.Information);
                if (res == DialogResult.Yes)
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
                            (MessageBoxButtons)MessageBoxButton.OK, (MessageBoxIcon)MessageBoxImage.Error);
                    }
                }
            }
            else if (_isUpdateClicked)
            {
                System.Windows.Forms.MessageBox.Show("Already up to date.", "Information",
                    (MessageBoxButtons)MessageBoxButton.OK, (MessageBoxIcon)MessageBoxImage.Information);
            }
        }

    }
}
