// ============================================================================
//  ShotWeightViewModel.cs
//  MVVM ViewModel cho ShotWeightWindow – toàn bộ business logic
//  Logic cân (C021/C022/C023/C024) giống 100% frmShotWeightScale.cs
//  Namespace : SSSW.UI.WPF.ViewModels
// ============================================================================
using DevExpress.Mvvm.Native;
using DevExpress.XtraRichEdit.Import.Html;
using DevExpress.XtraSpreadsheet.Import.Xls;
using DevExpress.XtraSpreadsheet.Model.CopyOperation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ScanAndScale.Core.Models;
using SSSW.models;
using SSSW.modelss;
using SSSW.UI.WPF.Models;
using SSSW.UI.WPF.Services;
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
        private readonly IDialogService _dialog;
        private readonly IBomService _bom;
        private readonly IPartWeightCalculator _partWeightCalculator;
        private readonly IScaleStepRepository _scaleRepo;

        // BOM Level 2 của từng step đang cân (key = ItemStepCode đã Trim) + FT600 đã lưu của các chi tiết con
        private Dictionary<string, IReadOnlyList<BomWinlineModel>> _level2ByStep = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, FT600> _weighedChildrenFromDb = new(StringComparer.OrdinalIgnoreCase);

        // Số chiếc / 1 đôi gộp theo BOM Level 2 của các step thuộc FG đang cân (xem ComponentQuantityAggregator)
        private Dictionary<string, double> _pieceQtyByCode = new(StringComparer.OrdinalIgnoreCase);

        // ── Cancellation ────────────────────────────────────────────────────
        private CancellationTokenSource? _loadCts;

        // ─────────────────────────────────────────────────────────────────────
        //  DOMAIN STATE (giống frmShotWeightScale field-for-field)
        // ─────────────────────────────────────────────────────────────────────
        public List<FT601> _dataHydra = new();
        private List<FT601> _dataHydraMultiSizeOfMold = new();
        private bool _newScale = true;
        public FT601 _stepItemCodeScale = new();
        private readonly HashSet<Guid> _sameMoldRowIds = new();
        /// <summary>Dòng cùng khuôn / group size thêm vào → step chọn cân ban đầu (nguồn copy FG, Main, số chiếc khi thiếu).</summary>
        private readonly Dictionary<Guid, FT600> _sameMoldAnchor = new();
        /// <summary>Id các FT601 dựng tạm (step không có trong FT601) — không tồn tại trong DB.</summary>
        private readonly HashSet<Guid> _syntheticStepIds = new();
        /// <summary>Dòng step đang chuyển sang là PU (đọc trước khi danh sách bị dựng lại).</summary>
        private bool _switchRowIsPu;
        private int? _switchRowSeq;
        /// <summary>FG (code / name) thật của step Logo đang cân, lấy từ sp_sssw_getproductInfo; null = dùng FG của FT601 như thường.</summary>
        private string? _productFgCode;
        private string? _productFgName;

        private List<BomWinlineModel> _allStepsFG = new();
        private List<FT600> _scaledDataPreviousStep = new();
        private List<FT600> _scaleData = new();
        private List<FT600> _scaleDataFinal = new();
        private FT600 _rowSelected = new();
        // Row vừa cân xong gần nhất — giữ lại riêng để ExecuteConfirmAsync (kiểm tra multi-size
        // mold REX) vẫn hoạt động đúng sau khi _rowSelected đã tự động chuyển/xóa (xem ExecuteSave).
        private FT600? _lastWeighedRow;

        private double _scaleValue = 0;
        private string _mesocomp = string.Empty;
        private int _mesoYear = 0;

        private bool _isRunner = true;
        private double? _articlePaisShotFinaly = 0;
        private double _percentOfUsage = 0;
        private List<StepSelectModel> _allStepCodeMaster = new();
        private StepSelectModel? _selectedStepItem;

        public FT601 _stepSelected = new();
        private string _qrCodeScan = string.Empty;
        private FT606_Label _labelInfo = new();

        // RFID/operator identity không còn là field riêng của tab này nữa — đọc thẳng từ
        // _operatorSession (OperatorSessionService, dùng chung với tab FG) để chuyển tab không
        // làm "mất" operator đã quét (chỉ Cancel mới xóa, xem ExecuteCancel()).
        private readonly OperatorSessionService _operatorSession;

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
        // WindowTitle không còn ở đây nữa — MainViewModel.WindowTitle là chủ sở hữu duy nhất
        // của dòng tiêu đề header (xem MainViewModel.LoadWindowTitleAsync()).
        // UserName cũng không còn ở đây — MainViewModel.OperatorSession.UserName là chủ sở hữu
        // duy nhất, dùng chung cho cả Step/FG (xem OperatorSessionService.cs).
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

        private string _countUnit = "prs";
        /// <summary>"prs" (hàng thường) hoặc "pcs" (hàng PU) — nhãn đơn vị cho Partitioning / Adjust no.</summary>
        public string CountUnit
        {
            get => _countUnit;
            set { _countUnit = value; OnPropertyChanged(); OnPropertyChanged(nameof(PartitioningLabel)); OnPropertyChanged(nameof(AdjustLabel)); }
        }
        public string PartitioningLabel => $"Partitioning ({CountUnit})";
        public string AdjustLabel => $"Adjust no. of {CountUnit}";

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

                // Remark chỉ áp dụng cho đúng step đang chọn — KHÔNG lan sang các step khác đã có
                // trong TOTAL STEPS (trước đây ForEach lên toàn bộ _scaleDataFinal, khiến mọi dòng
                // bị đè cùng 1 remark). Nếu chưa chọn step nào thì bỏ qua.
                if (_rowSelected != null)
                {
                    _rowSelected.C038 = value;

                    // FT600 không implement INotifyPropertyChanged nên gán C038 ở trên không tự
                    // đẩy lên DataGrid — phải ép rebind lại StepsCollection để cột REMARKS hiện
                    // giá trị mới ngay khi gõ. Không gọi RefreshUI() ở đây vì RefreshUI() có set lại
                    // Remark = _rowSelected.C038, sẽ gọi ngược lại setter này → đệ quy vô hạn.
                    RefreshStepsGrid();
                }
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

        // RfidName không còn ở đây — dùng OperatorSession.RfidName (dùng chung Step/FG).
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

        // RfidStatus/RfidCardCode/ReadOnlyRfid không còn ở đây — RFID giờ dùng chung cho cả
        // Step/FG qua OperatorSessionService (đọc qua MainViewModel.OperatorSession, bind thẳng
        // ở Main.xaml header). Việc kết nối/lắng nghe driver RFID cũng chuyển hẳn lên
        // OperatorSessionService.Attach() — không còn RfidDriver_DataValueChanged riêng ở
        // ShotWeightWindow.xaml.cs nữa.

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
            set
            {
                _readOnlyScale = value;
                OnPropertyChanged();

                // Chế độ nhập tay (readOnly = false): mở nút Save ngay, không chờ event
                // cân bắn ra (đối xứng với nhánh else trong OnScaleValueChanged) — nếu không
                // nút Save bị kẹt ở trạng thái mặc định (disabled) cho tới khi có 1 lần cân
                // thực tế xảy ra, kể cả khi config đã cho phép lưu tay.
                if (!value)
                    EnableBtnSaveScale = true;
            }
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

        private string _overlayMessage = "Loading…";
        /// <summary>Nội dung hiển thị trên lớp phủ chờ (Loading… / Saving data…).</summary>
        public string OverlayMessage
        {
            get => _overlayMessage;
            set { _overlayMessage = value; OnPropertyChanged(); }
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
        // ClearRfidAction/FocusRfidNameAction không còn — RFID giờ do OperatorSessionService tự
        // quản lý property của chính nó, không cần callback ngược vào View nữa.
        /// <summary>Code-behind gán: reset DevExpress LookUpEdit selection.</summary>
        public Action? ClearStepComboAction { get; set; }
        /// <summary>Code-behind gán: set DevExpress LookUpEdit selection.</summary>
        public Action<StepSelectModel?>? SetStepComboAction { get; set; }
        /// <summary>Code-behind gán: focus+scroll đến row trong dgTotalSteps.</summary>
        public Action<string?>? FocusGridRowAction { get; set; }

        // ─────────────────────────────────────────────────────────────────────
        //  COMMANDS
        // ─────────────────────────────────────────────────────────────────────
        public AsyncRelayCommand ReloadCommand { get; }
        public RelayCommand HistoryViewCommand { get; }
        // HydraCommand/SettingsCommand/UpdateCommand chuyển lên MainViewModel — đây là hành động
        // cấp APP dùng chung cho cả Step/FG, không riêng gì tab này (xem Main.xaml + MainViewModel).
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
            OperatorSessionService operatorSession,
            IDialogService dialog,
            IBomService bom,
            IPartWeightCalculator partWeightCalculator,
            IScaleStepRepository scaleRepo,
            ILogger<ShotWeightViewModel> logger)
        {
            _dialog = dialog;
            _bom = bom;
            _partWeightCalculator = partWeightCalculator;
            _scaleRepo = scaleRepo;
            _dbFactory = dbFactory;
            _serviceProvider = serviceProvider;
            _operatorSession = operatorSession;
            _logger = logger;

            // Wire commands
            ReloadCommand = new AsyncRelayCommand(() => LoadDataAsync(TimeSpan.FromSeconds(30)));
            HistoryViewCommand = new RelayCommand(OpenHistoryView);
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
        }

        // ─────────────────────────────────────────────────────────────────────
        //  INITIALIZATION
        // ─────────────────────────────────────────────────────────────────────
        public async Task InitializeAsync()
        {
            // Auto-updater (RunUpdateAsAdmin/DownloadPath + subscribe ApplicationExitEvent/
            // CheckForUpdateEvent) đã chuyển lên MainViewModel — hạ tầng app-wide, chỉ cần hook
            // 1 lần duy nhất, không gắn theo tab Step nữa.

            // Lấy mesocomp + mesoyear
            using var db = _dbFactory.CreateDbContext();
            _mesocomp = db.Database.SqlQueryRaw<string>("sp_MaterialGetCompanyName")
                          .AsEnumerable().FirstOrDefault() ?? string.Empty;
            _mesoYear = db.Database.SqlQueryRaw<int>("sp_MaterialGetMesoyear")
                          .AsEnumerable().FirstOrDefault();

            // WindowTitle (location + version) đã chuyển hẳn lên MainViewModel.LoadWindowTitleAsync()
            // — Main giờ là chủ sở hữu duy nhất của dòng tiêu đề header, tính 1 lần không phụ thuộc
            // tab active. Không tính lại ở đây nữa để tránh trùng lặp logic.

            // Config (GlobalVariable.ConfigSystem) + kết nối hardware (Barcode/RFID/Scale) đã
            // chuyển hẳn lên MainViewModel.StartupAsync() — chạy MỘT LẦN ở cấp Main trước khi
            // tab Step này được mở, nên tới đây Config/hardware luôn đã sẵn sàng. Không load/kết
            // nối lại ở đây nữa để tránh trùng lặp logic và tránh 2 tab đua nhau connect.

            // Default values
            UsagePct = GlobalVariable.ConfigSystem.PercentOfUserNonWoven.ToString();
            _percentOfUsage = GlobalVariable.ConfigSystem.PercentOfUserNonWoven;

            DeltaInformation = $"STD: Target Weight ↔ ACTUAL: Live Weight · auto colored vs. tolerance: -{GlobalVariable.ConfigSystem.DeltaLevel1}g<=Delta<={GlobalVariable.ConfigSystem.DeltaLevel1}g -->green;  -{GlobalVariable.ConfigSystem.DeltaLevel2}g<=Delta<=-{GlobalVariable.ConfigSystem.DeltaLevel1}g or {GlobalVariable.ConfigSystem.DeltaLevel1}g<=Delta<={GlobalVariable.ConfigSystem.DeltaLevel2}g -->Orange; Delta<-{GlobalVariable.ConfigSystem.DeltaLevel2}g or Delta>{GlobalVariable.ConfigSystem.DeltaLevel2}g -->Red.";
            ReadOnlyScale = GlobalVariable.ConfigSystem.Scale.ReadOnly ?? true;
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
                _dialog.Error($"Load data failure:\n{ex.Message}", "Load data");
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
            var gain = unit == "KG" ? 1000 : 1;
            _scaleValue = Math.Round(value * gain, 2);
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

        // OnRfidValueChanged/OnRfidNameEnterAsync/OpenRfidInputDialog đã chuyển hẳn lên
        // OperatorSessionService/MainViewModel — dùng chung cho cả Step/FG, không còn ở đây nữa.

        /// <summary>Barcode scanner đọc được QR code.</summary>
        public async Task OnBarcodeScannedAsync(string barcode)
        {
            try
            {
                // Máy scan đôi khi trả về nội dung QR kèm ký tự Enter/xuống dòng (CR/LF) hoặc khoảng
                // trắng thừa ở đầu/cuối chuỗi — nếu không loại bỏ, so khớp C001 trong FT606 sẽ không
                // tìm thấy dữ liệu dù QR code thực chất đúng. Trim trước khi dùng trong câu truy vấn.
                barcode = barcode?.Trim() ?? string.Empty;

                _qrCodeScan = barcode;
                using var db = _dbFactory.CreateDbContext();

                _labelInfo = new FT606_Label();
                _labelInfo = await db.FT606s.FirstOrDefaultAsync(x => x.C001 == _qrCodeScan)
                             ?? throw new Exception("Label information not found.");

                _stepSelected = _dataHydra.FirstOrDefault(x => x.Id == _labelInfo.C000)
                                ?? throw new Exception("Step information not found.");

                FilterStepCombo(_stepSelected.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Barcode scan error");
                _dialog.Warn(ex.Message, "WARNING");
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
                _dialog.Warn("No matching data found!", "WARNING");
        }

        /// <summary>Gọi từ code-behind khi DevExpress LookUpEdit thay đổi selection.</summary>
        public async Task OnStepSelectedAsync(StepSelectModel? selected)
        {
            if (_suppress) return;
            try
            {
                if (selected == null) { _labelInfo = new FT606_Label(); ResetNewLoop(); return; }

                if (string.IsNullOrEmpty(_qrCodeScan) || selected.StepItemCode?.Trim() != _stepSelected?.C004?.Trim())
                {
                    _stepSelected = _dataHydra.FirstOrDefault(x =>
                        x.C004?.Trim() == selected.StepItemCode?.Trim() &&
                        x.C015?.Trim() == selected.Machine?.Trim() &&
                        x.C018 == selected.HydraOrderNo)
                        ?? _dataHydra.FirstOrDefault(x => x.C004?.Trim() == selected.StepItemCode?.Trim())
                        // Step không có trong FT601 của lệnh (vd. size L/R lấy từ group size): tìm master FT601 theo đúng mã step,
                        // không có thì dựng từ thông tin step + sp_sssw_getproductInfo
                        ?? await CreateStepFromSelectionAsync(selected);

                    if (!string.IsNullOrEmpty(_qrCodeScan) && _stepSelected.Id != _labelInfo?.C000)
                    {
                        _qrCodeScan = string.Empty;
                        _labelInfo = new FT606_Label();
                        ClearBarcodeAction?.Invoke();
                    }
                }

                await TriggerStepSelectionAsync(selected);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OnStepSelectedAsync");
                _dialog.Warn(ex.Message, "WARNING");
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
                        .Where(x => x.C007 == stepCode.C007 && x.C004 == stepCode.C004)
                        .FirstOrDefaultAsync()
                        ?? (_syntheticStepIds.Contains(stepCode.Id) ? stepCode
                            : throw new Exception($"Step item1 code {stepCode.C007} not found."));

                    _allStepsFG = await _bom.GetLevel1Async(stepCode.C007);

                    // FG hàng PU: có step của FG (hoặc step đang chọn) với FT601.C016 = 'PU'
                    bool fgIsPu = string.Equals(stepCode.C016?.Trim(), FT600.PuMachineGroup, StringComparison.OrdinalIgnoreCase) ||
                        _allStepsFG.Any(it => _dataHydra.Any(h =>
                            string.Equals(h.C004?.Trim(), it.ItemStepCode?.Trim(), StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(h.C016?.Trim(), FT600.PuMachineGroup, StringComparison.OrdinalIgnoreCase)));

                    foreach (var item in _allStepsFG)
                    {
                        bool allowScale = true;
                        FT601 ckHydra = new();

                        var stepCodeTrim = item.ItemStepCode?.Trim() ?? string.Empty;
                        var fgMainCodeTrim = item.ItemFgCode?.Split('-')[0]?.Trim() ?? string.Empty;

                        //kiểm tra xem bước hiện tại có đúng với step đang chọn hay không
                        ckHydra = stepCodeTrim.Equals(_stepSelected.C004?.Trim(), StringComparison.OrdinalIgnoreCase)
                            ? _stepSelected
                            : _dataHydra.FirstOrDefault(x =>
                                x.C004?.Trim().Equals(stepCodeTrim, StringComparison.OrdinalIgnoreCase) == true &&
                                (x.C020?.Trim().Equals(fgMainCodeTrim, StringComparison.OrdinalIgnoreCase) == true ||
                                 (!string.IsNullOrEmpty(fgMainCodeTrim) && x.C007?.Contains(fgMainCodeTrim) == true)));

                        if (ckHydra == null)
                        {
                            if (stepCodeTrim != "Z-VHXXXXXX" &&
                                !stepCodeTrim.StartsWith("REX", StringComparison.OrdinalIgnoreCase))
                            {
                                var mc = fgMainCodeTrim;
                                var smc = stepCodeTrim.Contains('-') ? stepCodeTrim.Split('-')[1] : stepCodeTrim;
                                // Chỉ hàng PU: FT601 khớp mờ phải cùng L/R với step, tránh trúng size chung / phía kia
                                // (vd. step ...-F21L không được khớp ...-F189 hay ...-F21R). Hàng khác giữ cách khớp cũ.
                                var stepSide = fgIsPu ? FT600.PuSideOf(stepCodeTrim) : null;
                                ckHydra = _dataHydra.FirstOrDefault(x =>
                                    ((!string.IsNullOrEmpty(mc) && x.C007 != null && x.C007.Contains($"{mc}-")) ||
                                     (!string.IsNullOrEmpty(smc) && x.C004 != null && x.C004.Contains($"-{smc}-"))) &&
                                    (stepSide == null || FT600.PuSideOf(x.C004) == stepSide));
                                // Không còn khóa theo lệnh đang chạy: mọi step đều cho phép cân (xem docs/tech/ALLOWSCALE_LOGIC_REVIEW.md)
                            }
                            else
                            {
                                var parallelInfo = _allStepsFG.FirstOrDefault(x => x.ParallelSequence == item.ParallelSequence && !x.ItemStepCode.StartsWith("REX", StringComparison.OrdinalIgnoreCase));

                                ckHydra = new FT601
                                {
                                    C007 = item.ItemFgCode,
                                    C008 = item.ItemFgName,
                                    C004 = item.ItemStepCode,
                                    C005 = item.ItemStepName,
                                    C000 = _stepItemCodeScale.C000,
                                    C010 = item.ParallelSequence,
                                    C002 = parallelInfo?.Size,

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
                            C012 = ckHydra?.Id == _labelInfo.C000 ? _labelInfo?.C001 : null,
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
                            C029 = ckHydra?.Id == _labelInfo.C000 ? _labelInfo?.Id : null,
                            C032 = ckHydra?.Id,
                            C033 = item.CategoryCode,
                            C034 = item.CategoryName,
                            C035 = _percentOfUsage,
                            C036 = 0,
                            C037 = item.Unit,
                            AllowScale = allowScale
                        };

                        // Step không có dòng Hydra riêng (chỉ khớp "mờ" theo model/size) thì article của lệnh khác không đúng cho step này
                        // → tự lấy từ mã step bỏ phần màu. (Step chọn từ master FT601 / khớp đúng mã step giữ nguyên.)
                        bool exactHydra = ckHydra != null &&
                            string.Equals(ckHydra.C004?.Trim(), stepCodeTrim, StringComparison.OrdinalIgnoreCase);
                        if (!exactHydra && !stepCodeTrim.StartsWith("REX", StringComparison.OrdinalIgnoreCase))
                        {
                            line.C005 = RemoveColorFromStepCode(stepCodeTrim);
                            // Khớp mờ có thể trúng FT601 của step/size khác (cùng "-FC992601-" hoặc cùng mã FG chính) →
                            // FG code / FG name của dòng đó sai. FG đang cân là FG của step trong BOM (item).
                            line.C013 = item.ItemFgCode;
                            line.C014 = item.ItemFgName;
                            // Step không có dữ liệu trong FT601: bỏ machine group / mold / partitioning của dòng khớp sai
                            // (Actual Partitioning = 0 → coi là 1, người cân chỉnh bằng "Adjust no.").
                            line.C017 = null;
                            line.C018 = null;
                            line.C019 = null;
                            line.C020 = null;
                            line.C028 = 0;
                        }
                        _scaleData.Add(line);
                    }

                    // FG hàng PU: các step không có dòng FT601 (đã bỏ machine group ở trên) cũng là PU.
                    if (_scaleData.Any(x => x.IsPu))
                    {
                        foreach (var r in _scaleData.Where(x =>
                                     string.IsNullOrEmpty(x.C019) &&
                                     !string.IsNullOrWhiteSpace(x.C002) && x.C002 != "Z-VHXXXXXX" &&
                                     !x.C002.StartsWith("REX", StringComparison.OrdinalIgnoreCase)))
                            r.C019 = FT600.PuMachineGroup;
                    }

                    // Trường hợp nhiều size trên 1 khuôn
                    var dataSize = new List<FT600>();
                    _sameMoldRowIds.Clear();
                    _sameMoldAnchor.Clear();
                    _productFgCode = _productFgName = null;
                    foreach (var item in _scaleData)
                    {
                        //var pfx2 = GlobalVariable.PrefixUpToSecondHyphen(item1.C002);
                        //var sameMolds = _dataHydra.Where(x =>
                        //    x.C019 == item1.C020 && x.C015 == item1.C004 &&
                        //    x.C004 != item1.C002 && x.C002 != item1.C008 &&
                        //    GlobalVariable.PrefixUpToSecondHyphen(x.C004) == pfx2 &&
                        //    x.C010 == item1.C015).DistinctBy(x => x.C002).ToList();

                        if (string.IsNullOrEmpty(item.C020)) continue;   // không có mold (step không có dòng FT601) → không có "cùng khuôn"
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

                        var firstNewIdx = dataSize.Count;
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
                                C012 = _labelInfo?.C001,
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

                        // FG của các dòng cùng khuôn = FG của step chọn cân ban đầu (FT601 của size khác có thể ghi FG khác);
                        // Main code/name giữ theo FT601, thiếu thì copy từ step gốc. Số chiếc (C039) copy từ step gốc khi chưa có.
                        foreach (var added in dataSize.Skip(firstNewIdx))
                        {
                            if (!string.IsNullOrWhiteSpace(item.C013)) added.C013 = item.C013;
                            if (!string.IsNullOrWhiteSpace(item.C014)) added.C014 = item.C014;
                            CopyMissingFgInfo(added, item);
                            _sameMoldAnchor[added.id] = item;
                        }
                    }

                    var newSameMold = dataSize.Where(ds =>
                        !_scaleData.Any(sd => sd.C002 == ds.C002 && sd.C015 == ds.C015)).ToList();
                    foreach (var ds in newSameMold) _sameMoldRowIds.Add(ds.id);
                    _scaleData.AddRange(newSameMold);

                    FillMissingArticleInfo(_scaleData);

                    // Không có thông tin partitioning trong FT601 (null / 0) → mặc định 1 (Actual / PPIC / Mold pairs-or-pcs shot)
                    foreach (var r in _scaleData.Where(x => !(x.C002?.StartsWith("REX", StringComparison.OrdinalIgnoreCase) ?? false)))
                    {
                        if ((r.C028 ?? 0) <= 0) r.C028 = 1;
                        if ((r.C017 ?? 0) <= 0) r.C017 = 1;
                        if ((r.C018 ?? 0) <= 0) r.C018 = 1;
                    }
                    // C003 (tên step) có thể null: ItemStepName từ SP rỗng, hoặc FT601.C005 null (dòng cùng khuôn nhiều size).
                    // Phía dưới gọi C003.StartsWith(...) trực tiếp → chuẩn hóa về chuỗi rỗng để không NullReferenceException.
                    foreach (var r in _scaleData) r.C003 ??= string.Empty;
                    _scaleDataFinal = _scaleData.OrderBy(x => x.C015).ThenBy(x => x.C027).ToList();

                    // Khôi phục logic khóa / lọc nút cân như bản gốc (2026-11-05): Logo, Cleat_Ring, Studs, Inlay (1 khuôn nhiều size, group size)
                    // phải cân riêng — các step không thuộc nhóm đang cân bị khóa (AllowScale = false) hoặc bị loại khỏi danh sách.
                    // Phần trừ chi tiết con của từng step vẫn lấy theo BOM Level 2. Xem docs/tech/ALLOWSCALE_LOGIC_REVIEW.md.
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
                                .Where(x => !string.IsNullOrEmpty(x.C020))
                                .GroupBy(x => (x.C004, x.C020))
                                .Where(g => g.Count() > 1)
                                .ToDictionary(g => g.Key, g => g.ToList());

                    var startWith = stepCode.C005.StartsWith("Logo") ? "Logo"
                           : stepCode.C005.StartsWith("Studs") || stepCode.C005.StartsWith("Outer_Studs") || stepCode.C005.StartsWith("Inner_Studs") ? "Studs"
                           : stepCode.C005.StartsWith("Cleat_Ring") ? "Cleat_Ring"
                        : string.Empty;

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

                            // Logo: lấy thêm các size cùng nhóm (sp_sssw_getGroupSize) và đưa vào lưới như 1 khuôn nhiều size
                            if (startWith == "Logo")
                                await AddLogoGroupSizesAsync(db, stepCode);
                        }
                    }
                    //nếu là cân cho bước khác logo/studs/ring thì xóa các bước logo/studs/ring mà không có trong BOM
                    else
                    {
                        //lọc lại nếu bước được chọn cân mà ko phải là Stud hoặc Inlay (nhiều size/1 mold) thì khóa ko cho cân Stud và Inlay
                        //bắt buộc phải cân Stud và Inlay riêng

                        var logosStep = _scaleDataFinal.Where(x => x.C003.StartsWith("Logo")).ToList();

                        if (logosStep.Any() && !stepCode.C005.StartsWith("Logo"))
                        {
                            logosStep.ForEach(x => x.AllowScale = false);
                        }

                        var cleat_RingStep = _scaleDataFinal.FirstOrDefault(x => x.C003.StartsWith("Cleat_Ring"));

                        if (cleat_RingStep != null && !stepCode.C005.StartsWith("Cleat_Ring"))
                        {
                            cleat_RingStep.AllowScale = false;
                        }

                        var studsSteps = _scaleDataFinal
                            .Where(x => x.C003.StartsWith("Studs")
                                || x.C003.StartsWith("Outer_Studs")
                                || x.C003.StartsWith("Inner_Studs")
                            )
                            .ToList();

                        var inlaySteps = _scaleDataFinal.Where(x => x.C003.StartsWith("Inlay")).ToList();

                        var previousStep = _allStepsFG.FirstOrDefault(x => x.ParallelSequence == stepCode.C010 - 1);

                        if (!stepCode.C005.StartsWith("Inlay")
                            && (startWith != "Studs"
                                || (startWith == "Studs" && previousStep != null && (previousStep.ItemStepName.StartsWith("Base") || previousStep.ItemStepName.StartsWith("Outer_Studs")))
                                )
                            )
                        {
                            if (inlaySteps.Count() > 1)
                                inlaySteps.ForEach(x => x.AllowScale = false);

                            //check allow scle for studs
                            if (studsSteps != null)
                            {
                                //lấy ra bước trước của nước studs để check xem có phải là base hay không, nếu là base thì cho phép cân studs, nếu ko phải base thì ko cho phép cân studs
                                //nếu ở fVN thì nếu bước trước là Studs thì cho phép cân bước studs này luôn, và sẽ trừ số lượng cân của bước Studs trước đó ra khỏi số lượng cân của bước Studs hiện tại. Ex: 6112042502-C134-2801 - item code:3-VB042502-C136-5065

                                //var previousStepBse = _allStepsFG.FirstOrDefault(x => x.ParallelSequence == studsSteps.FirstOrDefault(x => x.C002 == stepCode.C004)?.C015 - 1);

                                //var allowStudsScale = !(previousStepBse == null
                                //                            || (previousStepBse != null
                                //                                    && !previousStepBse.ItemStepName.StartsWith("Base")
                                //                                    && !previousStepBse.ItemStepName.StartsWith("Studs")
                                //                                    && !previousStepBse.ItemStepName.StartsWith("Outer_Studs")
                                //                                    && !previousStepBse.ItemStepName.StartsWith("Inner_Studs")
                                //                                )
                                //                         );
                                //studsSteps.ForEach(x => x.AllowScale = allowStudsScale);

                                foreach (var itemStud in studsSteps)
                                {
                                    var previousStepS = _allStepsFG.FirstOrDefault(x => x.ParallelSequence == itemStud.C015 - 1);

                                    itemStud.AllowScale = !(previousStepS == null
                                                                || (previousStepS != null
                                                                        && !previousStepS.ItemStepName.StartsWith("Base")
                                                                        && !previousStepS.ItemStepName.StartsWith("Studs")
                                                                        && !previousStepS.ItemStepName.StartsWith("Outer_Studs")
                                                                        && !previousStepS.ItemStepName.StartsWith("Inner_Studs")
                                                                    ));

                                }
                            }

                            //xóa những step ko có trong BOM ra
                            _scaleDataFinal.RemoveAll(x =>
                            {
                                bool isLogo = x.C003.StartsWith("Logo");
                                bool isStuds = x.C003.StartsWith("Studs") || x.C003.StartsWith("Outer_Studs") || x.C003.StartsWith("Inner_Studs");
                                bool isRing = x.C003.StartsWith("Cleat_Ring");
                                bool isInlay = x.C003.StartsWith("Inlay");

                                bool hasC002 = stepAll.Contains(x.C002);

                                bool duplicatedSize = checkMultiSize.ContainsKey((x.C004, x.C020));

                                return (isLogo || isStuds || isRing || isInlay) && !hasC002;
                                //|| duplicatedSize;
                            });
                        }
                        else if (stepCode.C005.StartsWith("Inlay")
                                || (startWith == "Studs"
                                    && (previousStep == null
                                        || (previousStep != null && !previousStep.ItemStepName.StartsWith("Base"))
                                        )
                                   )
                                )
                        {
                            var startWith1 = stepCode.C005.StartsWith("Studs") || stepCode.C005.StartsWith("Outer_Studs") || stepCode.C005.StartsWith("Inner_Studs") ? "Studs"
                                : stepCode.C005.StartsWith("Outer_Studs") ? "Outer_Studs"
                                    : stepCode.C005.StartsWith("Inner_Studs") ? "Inner_Studs"
                                        : "Inlay";

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

                        if (!item1.C003!.StartsWith("Stud") && !item1.C003!.StartsWith("Outer_Stud") && !item1.C003!.StartsWith("Inner_Stud") && !item1.C003.StartsWith("Logo") &&
                            !item1.C003.StartsWith("Cleat_Ring") && !item1.C002!.StartsWith("REX") && !item1.C003!.StartsWith("Inlay"))
                            continue;

                        FT600? stepPrevious;
                        if (item1.C002 != "Z-VHXXXXXX" && item1.C002.Substring(0, 3) != "REX")
                        {
                            // Chỉ khớp ĐÚNG mã step (trước đây khớp "mờ" theo model + size + C015 nên mượn nhầm bản ghi khác).
                            // Mã step không chứa màu FG nên trọng lượng vẫn dùng chung giữa các FG/màu.
                            stepPrevious = await db.FT600s
                                .Where(x => x.C002 == item1.C002)
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
                            item1.IsReusedFromHistory = true;
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
                                // C036 cũ (chưa có C039) theo nghĩa cũ → bỏ, tính lại từ C021 / C039 ở ApplyPieceQuantities()
                                item1.C036 = (stepPrevious.C039 ?? 0) > 0 ? stepPrevious.C036 : 0;
                            }
                        }
                    }

                    _scaleDataFinal = _scaleDataFinal.OrderBy(x => x.C015).ThenBy(x => x.C004).ToList();

                    ApplyPuUnits(_scaleDataFinal);
                    await PreloadLevel2Async();
                    var level2RexRows = AddLevel2OnlyRexRows();
                    ApplyPieceQuantities();
                    PrefillRexFromHistory(level2RexRows);

                    var stepSel = _scaleDataFinal.FirstOrDefault(x => x.C002?.Trim() == _stepSelected.C004?.Trim());
                    var prevSteps = stepSel == null ? new List<FT600>() : PreviousStepsToCheck(stepSel).ToList();
                    if (prevSteps.Any(x => x.C021 == 0))
                    {
                        _rowSelected = prevSteps.Where(x => x.C021 == 0).OrderBy(od => od.C015).FirstOrDefault();
                        _dialog.Warn("The previous step has not been weighed.", "Warning");
                    }
                    else
                        _rowSelected = _scaleDataFinal
                            .FirstOrDefault(x => x.C002?.Trim() == _stepSelected.C004?.Trim()) ?? new FT600();
                }
                else
                {
                    var stepCodeTrim = _stepSelected.C004?.Trim();
                    var rowSelect = _scaleDataFinal.FirstOrDefault(x =>
                        x.C002?.Trim() == stepCodeTrim &&
                        (x.C013?.Trim() == _stepSelected.C007?.Trim() || string.IsNullOrEmpty(_stepSelected.C007)) &&
                        (x.C004?.Trim() == _stepSelected.C015?.Trim() || string.IsNullOrEmpty(_stepSelected.C015)))
                        ?? _scaleDataFinal.FirstOrDefault(x => x.C002?.Trim() == stepCodeTrim);

                    if (rowSelect == null)
                    {
                        _dialog.Warn($"Label does not match the item1 being weighed.\n{_stepSelected.C004}|{_stepSelected.C005}", "WARNING");
                        return;
                    }
                    if (!rowSelect.AllowScale)
                    {
                        _dialog.Info("Do not allow to scale this step.", "Warning");
                        return;
                    }
                    foreach (var step in PreviousStepsToCheck(rowSelect))
                    {
                        if (step.C021 == 0)
                        {
                            _dialog.Warn("The previous step has not been weighed.", "Warning");
                            return;
                        }
                    }
                    rowSelect.C012 = _labelInfo?.C001;
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
                _dialog.Warn(ex.Message, "WARNING");
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
                _dialog.Warn("Cannot scale this step.", "Warning");
                return;
            }

            var unweighedPrevSteps = PreviousStepsToCheck(_rowSelected)
                .Where(x => (x.C021 ?? 0) == 0)
                .OrderBy(x => x.C015)
                .ToList();

            if (unweighedPrevSteps.Count > 0)
            {
                var detail = string.Join(Environment.NewLine, unweighedPrevSteps.Select((x, i) =>
                    $"{i + 1}. Step {x.C015} [{x.C002}]: {x.C003} (Size:{x.C008})"));

                _dialog.Warn($"{unweighedPrevSteps.Count} previous step(s) are missing Part Weight and must be weighed first:" +
                    $"{Environment.NewLine}{Environment.NewLine}{detail}", "Warning");
                return;
            }

            _rowSelected.IsReusedFromHistory = false; // cân lại trong phiên này

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
                    // Lần cân thứ hai: số cân = tổng part của cả N đôi trong 1 shot (N = Actual Partitioning)
                    var prsShot = (_articlePaisShotFinaly ?? 0) > 0 ? _articlePaisShotFinaly!.Value : 1;

                    // C023 = Total Part Weight = S (không chia N); Part Weight C021 = C023 / N − Σ level 2 (xem PartPerPair)
                    _rowSelected.C023 = _scaleValue;

                    // C022 = Runner weight (g/prs) = (Total Weight − S) / N
                    _rowSelected.C022 = RunnerText == "YES"
                        ? Math.Round((_rowSelected.C024 - _scaleValue) / prsShot ?? 0, 3)
                        : 0;

                    if (!_rowSelected.C003.StartsWith("Logo") &&
                        !_rowSelected.C003.StartsWith("Cleat_Ring"))
                    {
                        // BOM Level 2: C021 = C023 − Σ(Qty × trọng lượng chi tiết con). Không có Level 2 → công thức cũ.
                        var l2 = UsesLegacyPartWeight(_rowSelected) ? PartWeightApply.Unavailable : ApplyLevel2PartWeight();
                        if (l2 == PartWeightApply.Blocked) return;
                        if (l2 == PartWeightApply.Unavailable)
                        {
                            // 1 khuôn nhiều size / nonwoven / mesh: công thức riêng (cũ). Còn lại không có BOM Level 2 → Part Weight = chính nó (C023 / N)
                            if (UsesLegacyPartWeight(_rowSelected)) ApplyLegacyPartWeight();
                            else _rowSelected.C021 = PartPerPair(_rowSelected);
                        }
                    }
                    else
                    {
                        _rowSelected.C021 = PartPerPair(_rowSelected);
                    }

                    // C036 = trọng lượng/chiếc = C021 / C039 (số chiếc trong 1 đôi gộp theo BOM Level 2)
                    UpdateWeightPerPiece(_rowSelected);
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
                    _rowSelected.C036 = PiecesStrict(_rowSelected) > 0
                        ? Math.Round(_scaleValue / PiecesStrict(_rowSelected), 2) : 0;
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
                var sumPW = sameMolds.Sum(x => x.C023);   // C023 = tổng part của N đôi của từng size
                var pairShot = sameMolds.Sum(x => x.C028);
                foreach (var s in sameMolds)
                {
                    s.C024 = _rowSelected.C024;
                    s.C022 = s.C023 > 0 ? (_rowSelected.C024 - sumPW) / pairShot : 0;
                }
            }

            // ── Cascade: cập nhật C021/C022 cho các bước sau ────────────────
            // Step thường: tính lại theo BOM Level 2 (không có Level 2 thì Part Weight = chính nó).
            if (_level2ByStep.Count > 0)
                CascadeLevel2();

            // Step 1 khuôn nhiều size / nonwoven: cascade theo cách cũ (giữ nguyên như trước)
            {
                var toUpdate = _scaleDataFinal.Where(x =>
                        UsesLegacyPartWeight(x) &&
                        x.C015 >= _rowSelected.C015 && x.C024 > 0 &&
                        !x.C003!.StartsWith("Stud") && !x.C003.StartsWith("Inlay") &&
                        !x.C003.StartsWith("Cleat_Ring") &&
                        !(x.C002?.StartsWith("REX") ?? false) &&
                        x.C002 != _rowSelected.C002 &&
                        GlobalVariable.PrefixUpToSecondHyphen(x.C002) != pfx2)
                    .OrderBy(x => x.C015).ToList();

                foreach (var item in toUpdate)
                {
                    if (item.C015 == _rowSelected.C015)
                    {
                        item.C021 = PartPerPair(item) - PartPerPair(_rowSelected);
                        item.C022 = item.C024 - item.C021;
                    }
                    else
                    {
                        var prev = toUpdate.FirstOrDefault(x => x.C015 < item.C015 - 1);
                        item.C021 = PartPerPair(item) - (prev == null ? null : PartPerPair(prev));
                        item.C022 = item.C024 - item.C021;
                    }
                }
            }

            // ── Step vừa cân đã hoàn tất (đủ Part Weight – 2 lần cân với item injection,
            // 1 lần với REX/non-injection): tự động chuyển _rowSelected sang bước kế tiếp
            // chưa cân. Nếu không còn bước nào, xóa _rowSelected để tránh trường hợp user
            // lỡ bấm Save lần nữa sẽ ghi đè lại Part Weight của bước vừa cân xong.
            bool advancedToNextStep = false;
            if ((_rowSelected.C021 ?? 0) > 0)
            {
                _lastWeighedRow = _rowSelected;

                var nextStep = _scaleDataFinal
                    .Where(x => x.AllowScale && (x.C021 ?? 0) == 0)
                    .OrderBy(x => x.C015).ThenBy(x => x.C004)
                    .FirstOrDefault();

                _rowSelected = nextStep ?? new FT600();
                _articlePaisShotFinaly = _rowSelected.C028;
                advancedToNextStep = nextStep != null;

                if (advancedToNextStep)
                {
                    // Giống OnGridScale: step mới được chọn thì mở panel Reference / History
                    AllowPartitionAdj = false;
                    IsHistoryExpanded = true;
                    _historyExpanded = true;
                }
            }

            _scaleDataFinal.ForEach(RoundDoubles);   // hiển thị và lưu cùng giá trị đã làm tròn 3 số lẻ

            RefreshUI(false);
            FocusGridRowAction?.Invoke(_rowSelected.C002);
            UpdateReferencePanel();

            // Reference / History theo step vừa được tự động chuyển sang (trước đây chỉ cập nhật khi bấm nút Scale trong lưới)
            if (advancedToNextStep) _ = LoadRefHistoryAsync(_rowSelected);
        }


        // ─────────────────────────────────────────────────────────────────────
        //  PART WEIGHT THEO BOM LEVEL 2
        // ─────────────────────────────────────────────────────────────────────
        /// <summary>Nạp BOM Level 2 của các step ép trong danh sách + FT600 đã lưu của chi tiết con (để Save tính đồng bộ).</summary>
        private async Task PreloadLevel2Async()
        {
            try
            {
                foreach (var row in _scaleDataFinal)
                {
                    var code = row.C002?.Trim();
                    if (string.IsNullOrEmpty(code) || code == "Z-VHXXXXXX" ||
                        code.StartsWith("REX", StringComparison.OrdinalIgnoreCase)) continue;

                    _level2ByStep[code] = await _bom.GetLevel2Async(code);
                }

                var childCodes = _level2ByStep
                    .SelectMany(kv => kv.Value.Where(c => !PartWeightCalculator.IsSelfRow(c, kv.Key)))
                    .Select(c => c.ItemStepCode!.Trim())
                    .Distinct().ToList();

                _weighedChildrenFromDb = new Dictionary<string, FT600>(
                    await _scaleRepo.GetLatestByStepCodesAsync(childCodes), StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                // Không có Level 2 → ExecuteSave tự rơi về công thức cũ
                _logger.LogError(ex, "PreloadLevel2Async error");
                _level2ByStep.Clear();
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  SỐ CHIẾC / 1 ĐÔI (C039) VÀ TRỌNG LƯỢNG / CHIẾC (C036)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// C039 = số chiếc của chi tiết trong 1 đôi, gộp theo BOM Level 2 (REX / studs / logo / ring...).
        /// Không có trong Level 2 của step nào (hoặc là step "chain" chỉ dồn vào step kế tiếp) → 0.
        /// </summary>
        private double PiecesFor(FT600 row)
        {
            var code = row.C002?.Trim();
            return code != null && _pieceQtyByCode.TryGetValue(code, out var q) ? q : 0;
        }

        /// <summary>
        /// Hàng PU (FT601.C016 = 'PU'): cân theo chiếc — Unit (C037) = PCS-L / PCS-R theo hậu tố mã step.
        /// Mold / PPIC / Actual Partitioning (C018 / C017 / C028) là số chiếc; mọi phép chia N (Part, Runner, Total PW) ra g/chiếc,
        /// không còn quy đổi 1 PRS = 2 chiếc (đơn vị không phải "PRS").
        /// </summary>
        private static void ApplyPuUnits(IEnumerable<FT600> rows)
        {
            foreach (var r in rows.Where(x => x.IsPu))
                r.C037 = FT600.PuUnitOf(r.C002);
        }

        /// <summary>Đơn vị số lượng ở panel/lưới: "pcs" nếu dòng PU, ngược lại "prs".</summary>
        private static string CountUnitOf(FT600? row) => row?.IsPu == true ? "pcs" : "prs";

        /// <summary>Số chiếc dùng để chia trọng lượng: C039 nếu đã có, ngược lại C025 (Quantity), tối thiểu 1.</summary>
        private static double PiecesOf(FT600 row)
            => (row.C039 ?? 0) > 0 ? row.C039!.Value : ((row.C025 ?? 0) > 0 ? row.C025!.Value : 1);

        /// <summary>Số chiếc theo C039 (không fallback): 0 nếu chưa có trong Level 2.</summary>
        private static double PiecesStrict(FT600 row) => (row.C039 ?? 0) > 0 ? row.C039!.Value : 0;

        /// <summary>C036 (g/chiếc) = C021 / C039; C039 = 0 (không có trong Level 2) → C036 = 0. REX giữ công thức riêng khi cân (scale / C039).</summary>
        private static void UpdateWeightPerPiece(FT600 row)
        {
            if (row.C002?.StartsWith("REX") ?? false) return;
            if ((row.C021 ?? 0) <= 0) return;
            if (row.IsPu) { row.C036 = Math.Round(row.C021!.Value, 3); return; }   // PU: C021 đã là g/chiếc
            var pcs = PiecesStrict(row);
            row.C036 = pcs > 0 ? Math.Round(row.C021!.Value / pcs, 3) : 0;
        }

        /// <summary>
        /// Gộp số chiếc theo BOM Level 2 (chỉ tính các step thuộc FG đang cân) rồi gán C039 cho từng dòng;
        /// tính C036 cho các dòng đã có C021 mà chưa có g/chiếc; chuẩn hóa bản ghi cũ (chưa có C039) nạp từ DB.
        /// </summary>
        private void ApplyPieceQuantities()
        {
            try
            {
                var fgSteps = _allStepsFG
                    .Select(x => x.ItemStepCode?.Trim())
                    .Where(x => !string.IsNullOrEmpty(x))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var scoped = _level2ByStep
                    .Where(kv => fgSteps.Contains(kv.Key))
                    .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);

                _pieceQtyByCode = ComponentQuantityAggregator.Aggregate(scoped);

                // Step "chain" (chỉ dồn vào step kế tiếp) coi như không có trong Level 2 → C039 = 0
                var fgRows = FgStepRows();
                foreach (var st in fgRows.Where(x => IsChainOnly(x, fgRows)))
                    _pieceQtyByCode.Remove(st.C002!.Trim());

                foreach (var row in _scaleDataFinal)
                {
                    row.C039 = PiecesFor(row);
                    // Dòng cùng khuôn / group size không có trong BOM Level 2 của FG → số chiếc & Quantity copy từ step chọn cân
                    if (row.C039 == 0 && _sameMoldAnchor.TryGetValue(row.id, out var anchor))
                    {
                        row.C039 = PiecesFor(anchor);
                        if ((row.C025 ?? 0) <= 0) row.C025 = anchor.C025;
                    }
                    if ((row.C036 ?? 0) <= 0) UpdateWeightPerPiece(row);
                }

                foreach (var saved in _weighedChildrenFromDb.Values)
                {
                    if ((saved.C039 ?? 0) > 0) continue;

                    // Bản ghi cũ chưa có C039: dùng số chiếc của FG hiện tại; C036 cũ theo nghĩa cũ (trừ REX) → tính lại
                    saved.C039 = PiecesFor(saved);
                    if (!(saved.C002?.StartsWith("REX") ?? false)) saved.C036 = 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ApplyPieceQuantities error");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  REX CHỈ XUẤT HIỆN Ở BOM LEVEL 2 → ĐƯA RA LƯỚI ĐỂ CÂN
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Các mã REX có trong BOM Level 2 của một step nhưng không có dòng riêng trong lưới (Level 1) →
        /// tạo dòng mới để cân: mỗi mã REX một dòng, số thứ tự step (C015) = 0 (vì có thể thuộc nhiều step),
        /// đặt đầu danh sách; C025 = 1; số chiếc cân = tổng số chiếc của 1 đôi (C039 gộp theo Level 2).
        /// </summary>
        private List<FT600> AddLevel2OnlyRexRows()
        {
            var created = new List<FT600>();
            try
            {
                var fgSteps = _allStepsFG
                    .Select(x => x.ItemStepCode?.Trim())
                    .Where(x => !string.IsNullOrEmpty(x))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var seen = _scaleDataFinal
                    .Where(x => x.C002?.StartsWith("REX", StringComparison.OrdinalIgnoreCase) ?? false)
                    .Select(x => x.C002!.Trim())
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var parents = _scaleDataFinal
                    .Where(x => !(x.C002?.StartsWith("REX", StringComparison.OrdinalIgnoreCase) ?? false)
                                && x.C002 != null && fgSteps.Contains(x.C002.Trim()) && GetLevel2(x) != null)
                    .OrderBy(x => x.C015).ToList();

                foreach (var parent in parents)
                {
                    foreach (var child in GetLevel2(parent)!)
                    {
                        var code = child.ItemStepCode?.Trim();
                        if (string.IsNullOrEmpty(code) || !code.StartsWith("REX", StringComparison.OrdinalIgnoreCase)) continue;
                        if ((child.Quantity ?? 0) <= 0 || !seen.Add(code)) continue;

                        var row = CreateLevel2RexRow(parent, child, code);
                        _scaleDataFinal.Insert(created.Count, row);   // step thứ tự 0 → đầu danh sách
                        created.Add(row);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AddLevel2OnlyRexRows error");
            }

            return created;
        }

        /// <summary>Dòng REX mới: thông tin lệnh / máy / FG lấy từ step chứa nó; mã, tên, category, đơn vị, số lượng lấy từ BOM Level 2.</summary>
        private FT600 CreateLevel2RexRow(FT600 parent, BomWinlineModel child, string code) => new()
        {
            id = Guid.NewGuid(),
            C000 = parent.C000,
            C001 = parent.C001,
            C002 = code,
            C003 = child.ItemStepName?.Trim() ?? string.Empty,
            C004 = parent.C004,
            C005 = parent.C005,
            C006 = parent.C006,
            C007 = parent.C007,
            C008 = parent.C008,
            C009 = parent.C009,
            C012 = parent.C012,
            C013 = parent.C013,
            C014 = parent.C014,
            C015 = 0,                           // REX có thể nằm ở nhiều step → không theo step nào, thứ tự 0
            C017 = parent.C017,
            C018 = parent.C018,
            C019 = parent.C019,
            C020 = parent.C020,
            C021 = 0,
            C022 = 0,
            C023 = 0,
            C024 = 0,
            C025 = 1,                           // Quantity giữ 1; tổng số chiếc/đôi nằm ở C039 (gộp Level 2)
            C026 = parent.C026,
            C027 = parent.C027,
            C028 = parent.C028,
            C029 = parent.C029,
            C032 = parent.C032,
            C033 = child.CategoryCode,
            C034 = child.CategoryName,
            C035 = _percentOfUsage,
            C036 = 0,
            C037 = child.Unit,
            C038 = parent.C038,
            AllowScale = true
        };

        /// <summary>
        /// Giống cách nạp REX từ lần cân trước ở Level 1: nếu mã REX đã có trong FT600 (bản ghi mới nhất) thì
        /// điền sẵn trọng lượng = g/chiếc × số chiếc của 1 đôi (C039); nếu chưa có thì để trống chờ cân.
        /// </summary>
        private void PrefillRexFromHistory(IEnumerable<FT600> rows)
        {
            foreach (var row in rows)
            {
                if (!_weighedChildrenFromDb.TryGetValue(row.C002!.Trim(), out var saved) || (saved.C036 ?? 0) <= 0) continue;

                var catCheck = GlobalVariable.ConfigSystem.CategoryOfNonInjectionUsagePartial?
                    .FirstOrDefault(x => x.CategoryCode == row.C033);
                var percent = (saved.C035 ?? 0) > 0 ? saved.C035!.Value : _percentOfUsage;
                var pairs = (row.C028 ?? 0) > 0 ? row.C028!.Value : 1;

                double total = catCheck == null ? saved.C036!.Value * PiecesOf(row) : saved.C036!.Value;
                var usage = (double)Math.Round((decimal)(total * percent / 100), 3);
                var unusage = total - usage;

                row.C021 = catCheck == null ? usage : usage / pairs;
                row.C022 = catCheck == null ? unusage : unusage / pairs;
                row.C023 = catCheck == null ? usage : usage / pairs;
                row.C024 = total;
                row.C035 = percent;
                row.C036 = saved.C036;
                row.IsReusedFromHistory = true;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  ARTICLE / MAIN ITEM NAME KHI STEP KHÔNG CÓ THÔNG TIN
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>"4-VB042313-A123-2801" → "4-VB042313-2801" (bỏ đoạn màu ở vị trí thứ 3).</summary>
        internal static string? RemoveColorFromStepCode(string? stepCode)
        {
            var code = stepCode?.Trim();
            if (string.IsNullOrEmpty(code)) return code;
            var parts = code.Split('-');
            if (parts.Length < 4) return code;
            return string.Join("-", parts.Where((_, i) => i != 2));
        }

        /// <summary>"Base_HF Outsole Future MxSG #FB1167 Color:Any color Size:8" → "Outsole Future MxSG #FB1167" (bỏ loại step + màu/size).</summary>
        internal static string? MainNameFromStepName(string? stepName)
        {
            var name = stepName?.Trim();
            if (string.IsNullOrEmpty(name)) return name;
            var colorAt = name.IndexOf(" Color:", StringComparison.OrdinalIgnoreCase);
            if (colorAt > 0) name = name[..colorAt].Trim();
            var firstSpace = name.IndexOf(' ');
            return firstSpace > 0 ? name[(firstSpace + 1)..].Trim() : name;
        }

        /// <summary>Điền article / main item name còn trống: step thường lấy từ mã/tên step bỏ màu, REX lấy theo step cùng thứ tự.</summary>
        private static void FillMissingArticleInfo(List<FT600> rows)
        {
            foreach (var r in rows)
            {
                bool isRex = r.C002?.StartsWith("REX", StringComparison.OrdinalIgnoreCase) == true;
                if (isRex)
                {
                    var sib = rows.FirstOrDefault(x => x != r && x.C015 == r.C015 &&
                        x.C002?.StartsWith("REX", StringComparison.OrdinalIgnoreCase) != true);
                    if (string.IsNullOrWhiteSpace(r.C005)) r.C005 = sib?.C005;
                    if (string.IsNullOrWhiteSpace(r.C027)) r.C027 = sib?.C027;
                    continue;
                }
                if (string.IsNullOrWhiteSpace(r.C005)) r.C005 = RemoveColorFromStepCode(r.C002);
                if (string.IsNullOrWhiteSpace(r.C027)) r.C027 = MainNameFromStepName(r.C003);
            }
        }

        private enum PartWeightApply { Applied, Unavailable, Blocked }

        /// <summary>
        /// Step thuộc nhóm 1 khuôn nhiều size: cùng máy, khuôn, thứ tự step và tiền tố mã step (cùng điều kiện phân bổ C024/C022 khi Save).
        /// </summary>
        private bool IsMultiSizeMold(FT600 row)
        {
            if (row.C002?.StartsWith("REX", StringComparison.OrdinalIgnoreCase) ?? false) return false;
            var pfx2 = GlobalVariable.PrefixUpToSecondHyphen(row.C002);
            return _scaleDataFinal.Count(x =>
                x.C020 == row.C020 && x.C004 == row.C004 && x.C015 == row.C015 &&
                GlobalVariable.PrefixUpToSecondHyphen(x.C002) == pfx2) > 1;
        }

        private static bool IsNonwovenCategory(int? categoryCode)
            => categoryCode != null &&
               (GlobalVariable.ConfigSystem.CategoryOfNonInjectionUsagePartial?.Any(c => c.CategoryCode == categoryCode) ?? false);

        /// <summary>Step có chi tiết REX loại nonwoven / mesh (chỉ dùng một phần theo %) trong cùng step hoặc trong BOM Level 2.</summary>
        private bool HasNonwovenRex(FT600 row)
        {
            if (_scaleDataFinal.Any(x => x != row && x.C015 == row.C015 && (x.C002?.StartsWith("REX", StringComparison.OrdinalIgnoreCase) ?? false)
                                         && IsNonwovenCategory(x.C033))) return true;
            var code = row.C002?.Trim();
            return code != null && _level2ByStep.TryGetValue(code, out var l2) &&
                   l2.Any(c => (c.ItemStepCode?.StartsWith("REX", StringComparison.OrdinalIgnoreCase) ?? false) && IsNonwovenCategory(c.CategoryCode));
        }

        /// <summary>
        /// Chỉ riêng cách TÍNH Part Weight (C021) dùng công thức cũ (C023 − Σ C021 các bước trước − Σ C021 non-injection cùng step) cho:
        /// 1 khuôn nhiều size, và step có vải nonwoven / mesh. Việc chặn cân / hiển thị theo BOM Level 2 giữ nguyên.
        /// </summary>
        /// <summary>Total Part Weight của 1 đôi = C023 / N (N = Actual Partitioning C028, rỗng/0 coi là 1). C023 giữ nguyên là số cân của cả N đôi.</summary>
        private static double PartPerPair(FT600 row)
            => Math.Round((row.C023 ?? 0) / ((row.C028 ?? 0) > 0 ? row.C028!.Value : 1), 3);

        private bool UsesLegacyPartWeight(FT600 row) => IsMultiSizeMold(row) || HasNonwovenRex(row);

        /// <summary>Level 2 (các chi tiết cấu thành) của step, null nếu SP không trả dữ liệu.</summary>
        private IReadOnlyList<BomWinlineModel>? GetLevel2(FT600 row)
        {
            var code = row.C002?.Trim();
            return code != null && _level2ByStep.TryGetValue(code, out var l2) && l2.Count > 0 ? l2 : null;
        }

        /// <summary>
        /// Step phải cân xong trước khi cân <paramref name="row"/>:
        /// - 1 khuôn nhiều size / nonwoven (công thức cũ): mọi step trước;
        /// - còn lại: chỉ step ngay trước nếu nó là "chain" (xem ChainPredecessors); chi tiết con Level 2 được kiểm khi Save.
        /// REX / non-injection không phụ thuộc step trước.
        /// </summary>
        private IEnumerable<FT600> PreviousStepsToCheck(FT600 row)
        {
            if (row.C002?.StartsWith("REX", StringComparison.OrdinalIgnoreCase) ?? false) return Enumerable.Empty<FT600>();
            return UsesLegacyPartWeight(row)
                ? _scaleDataFinal.Where(x => x.C015 > 0 && x.C015 < row.C015)   // bỏ dòng REX Level 2 (C015 = 0)
                : ChainPredecessors(row);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  CHUỖI STEP: step không được dùng làm chi tiết con ở nơi nào khác thì "dồn" vào step kế tiếp
        // ─────────────────────────────────────────────────────────────────────

        private List<FT600> FgStepRows() => _scaleDataFinal.Where(x =>
            !string.IsNullOrWhiteSpace(x.C002) && x.C002 != "Z-VHXXXXXX" &&
            !x.C002.StartsWith("REX", StringComparison.OrdinalIgnoreCase)).ToList();

        /// <summary>
        /// Step P là "chain": trong BOM Level 2 của các step FG, P chỉ được tham chiếu bởi step kế liền sau nó
        /// (hoặc không được tham chiếu ở đâu cả) → trọng lượng của P đã nằm trọn trong step kế tiếp, nên khi cân step kế tiếp
        /// phải trừ Total Part của P. P được dùng ở step khác (vd. Studs dùng ở nhiều step, Logo dùng ở step xa) → trừ theo Level 2.
        /// </summary>
        private bool IsChainOnly(FT600 p, List<FT600> steps)
        {
            int? nextSeq = steps.Where(x => x.C015 > p.C015).Select(x => x.C015).OrderBy(x => x).FirstOrDefault();
            var code = p.C002!.Trim();
            foreach (var q in steps)
            {
                if (!_level2ByStep.TryGetValue(q.C002!.Trim(), out var l2)) continue;
                bool refs = l2.Any(c => !PartWeightCalculator.IsSelfRow(c, q.C002!) && (c.Quantity ?? 0) > 0 &&
                    string.Equals(c.ItemStepCode?.Trim(), code, StringComparison.OrdinalIgnoreCase));
                if (refs && (nextSeq == null || q.C015 != nextSeq)) return false;
            }
            return true;
        }

        /// <summary>Các step "chain" ở thứ tự ngay trước <paramref name="row"/>.</summary>
        private List<FT600> ChainCandidatesBefore(FT600 row)
        {
            var steps = FgStepRows();
            var before = steps.Where(x => x.C015 < row.C015).ToList();
            if (before.Count == 0) return new List<FT600>();
            var prevSeq = before.Max(x => x.C015);
            return before.Where(x => x.C015 == prevSeq && IsChainOnly(x, steps)).ToList();
        }

        /// <summary>Step chain ngay trước cần trừ (cùng thứ tự có nhiều size thì ưu tiên cùng size).</summary>
        private List<FT600> ChainPredecessors(FT600 row)
        {
            var cands = ChainCandidatesBefore(row);
            if (cands.Count == 0) return cands;
            var pick = cands.FirstOrDefault(x => x.C008 == row.C008) ?? cands.First();
            return new List<FT600> { pick };
        }

        private HashSet<string> ChainPredecessorCodes(FT600 row)
            => ChainCandidatesBefore(row).Select(x => x.C002!.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);

        /// <summary>Tìm FT600 đã cân của chi tiết con: ưu tiên dòng trong phiên hiện tại, sau đó bản ghi đã lưu.</summary>
        private FT600? FindWeighedChild(string code)
        {
            var inSession = _scaleDataFinal.FirstOrDefault(x =>
                x.C002?.Trim().Equals(code, StringComparison.OrdinalIgnoreCase) == true &&
                PartWeightCalculator.UnitWeightPerPiece(x) != null);
            if (inSession != null) return inSession;
            return _weighedChildrenFromDb.TryGetValue(code, out var saved) ? saved : null;
        }

        /// <summary>Dựng dữ liệu hiển thị Row Details (BOM Level 2) cho 1 step: chi tiết con, trạng thái cân, phần trừ, công thức.</summary>
        private void RefreshLevel2Details(FT600 row)
        {
            var l2 = UsesLegacyPartWeight(row) ? null : GetLevel2(row);
            var preds = UsesLegacyPartWeight(row) ? new List<FT600>() : ChainPredecessors(row);
            var predCodes = preds.Count == 0 ? new HashSet<string>() : ChainPredecessorCodes(row);
            if (l2 == null && preds.Count == 0)
            {
                row.Level2Items = Array.Empty<Level2DetailItem>();
                row.Level2Formula = null;
                row.Level2HasMissing = false;
                return;
            }

            var items = new List<Level2DetailItem>();
            var terms = new List<string>();
            var missing = new List<string>();
            double total = 0;

            foreach (var child in l2 ?? new List<BomWinlineModel>())
            {
                if (PartWeightCalculator.IsSelfRow(child, row.C002!)) continue;

                var qty = child.Quantity ?? 0;
                if (qty <= 0) continue;

                var code = child.ItemStepCode!.Trim();
                if (predCodes.Contains(code)) continue;   // step chain: trừ Total Part của step trước (dòng riêng bên dưới)
                var weighed = FindWeighedChild(code);
                var perPiece = weighed == null ? null : PartWeightCalculator.UnitWeightPerPiece(weighed);
                double? deduct = perPiece == null ? null : qty * perPiece;

                items.Add(new Level2DetailItem
                {
                    Component = child.ItemStepName?.Trim() ?? code,
                    ComponentCode = code,
                    Step = code.StartsWith("REX", StringComparison.OrdinalIgnoreCase)
                        ? "REX" : (weighed?.C015?.ToString() ?? "—"),
                    Qty = qty,
                    // Qty ở Level 2 là số chiếc → hàng thường hiển thị PCS (chỉ dòng "previous step" mới là PRS); hàng PU giữ đơn vị BOM
                    Unit = row.IsPu ? child.Unit?.Trim() : "PCS",
                    Weighed = perPiece == null ? null : weighed!.C021,
                    PerPiece = perPiece,
                    Deduct = deduct,
                    IsWeighed = perPiece != null
                });

                if (deduct != null)
                {
                    total += deduct.Value;
                    terms.Add(deduct.Value.ToString("F2"));
                }
                else
                {
                    missing.Add(code);
                    terms.Add($"{code} ?");
                }
            }

            foreach (var pr in preds)
            {
                var prev = PartPerPair(pr);
                bool ok = prev > 0;
                items.Add(new Level2DetailItem
                {
                    Component = (pr.C003?.Trim() ?? pr.C002!.Trim()) + " (previous step)",
                    ComponentCode = pr.C002!.Trim(),
                    Step = pr.C015?.ToString() ?? "—",
                    Qty = 1,
                    Unit = pr.IsPu ? (pr.C037 ?? "PCS") : "PRS",
                    Weighed = ok ? prev : null,
                    PerPiece = ok ? prev : null,
                    Deduct = ok ? prev : null,
                    IsWeighed = ok
                });
                if (ok) { total += prev; terms.Add(prev.ToString("F2")); }
                else { missing.Add(pr.C002!.Trim()); terms.Add($"{pr.C002!.Trim()} ?"); }
            }

            row.Level2Items = items;
            row.Level2HasMissing = missing.Count > 0;
            row.Level2Formula = items.Count == 0 ? null
                : $"Part Weight = Total Part Weight − ({string.Join(" + ", terms)})" +
                  (missing.Count > 0
                      ? $"  →  Weigh {string.Join(", ", missing)} before saving this step"
                      : $" = Total Part Weight − {total:F2}");
        }

        /// <summary>
        /// Part Weight của step (g/prs) = Total Part / N − Σ chi tiết con Level 2 (không tính step "chain") − Total Part của step chain ngay trước.
        /// available=false: không có Level 2 và không có step chain → Part Weight = chính nó.
        /// </summary>
        private double? ComputePartWeight(FT600 row, out List<string> missing, out bool available)
        {
            missing = new List<string>();
            var l2 = GetLevel2(row);
            var preds = ChainPredecessors(row);
            available = l2 != null || preds.Count > 0;
            if (!available) return null;

            var predCodes = ChainPredecessorCodes(row);
            var children = (l2 ?? new List<BomWinlineModel>())
                .Where(c => !predCodes.Contains(c.ItemStepCode?.Trim() ?? string.Empty)).ToList();

            var result = _partWeightCalculator.Calculate(PartPerPair(row), row.C002!, children, FindWeighedChild);
            missing.AddRange(result.MissingChildren);

            double prevTotal = 0;
            foreach (var pr in preds)
            {
                if (PartPerPair(pr) <= 0) missing.Add(pr.C002!.Trim());
                else prevTotal += PartPerPair(pr);
            }

            return missing.Count == 0 && result.PartWeight.HasValue ? result.PartWeight.Value - prevTotal : null;
        }

        /// <summary>Làm tròn 3 số lẻ các cột kiểu double của FT600 (trước khi hiển thị / lưu DB).</summary>
        private static void RoundDoubles(FT600 r)
        {
            static double? R(double? v) => v.HasValue ? Math.Round(v.Value, 3) : null;
            r.C021 = R(r.C021); r.C022 = R(r.C022); r.C023 = R(r.C023); r.C024 = R(r.C024);
            r.C025 = R(r.C025); r.C028 = R(r.C028); r.C035 = R(r.C035); r.C036 = R(r.C036); r.C039 = R(r.C039);
        }

        private PartWeightApply ApplyLevel2PartWeight()
        {
            var part = ComputePartWeight(_rowSelected, out var missing, out var available);
            if (!available) return PartWeightApply.Unavailable;

            if (part == null)
            {
                _dialog.Warn(
                    "Component / previous step(s) of this step (BOM level 2) have no weight yet and must be weighed first:" +
                    $"{Environment.NewLine}{Environment.NewLine}{string.Join(Environment.NewLine, missing)}");
                _rowSelected.C023 = 0;
                _rowSelected.C022 = 0;
                return PartWeightApply.Blocked;
            }

            _rowSelected.C021 = part;
            _logger.LogInformation("PartWeight L2 {Step}: C023={C023} N={N} => C021={C021}",
                _rowSelected.C002, _rowSelected.C023, _rowSelected.C028, _rowSelected.C021);
            return PartWeightApply.Applied;
        }

        /// <summary>Tính lại C021/C022 của các step đã cân có dùng step vừa cân (chi tiết con Level 2 hoặc step chain ngay trước).</summary>
        private void CascadeLevel2()
        {
            var changed = _rowSelected.C002?.Trim();
            if (string.IsNullOrEmpty(changed)) return;

            foreach (var parent in _scaleDataFinal.Where(x =>
                         x != _rowSelected && (x.C023 ?? 0) > 0 && (x.C021 ?? 0) > 0 &&
                         !(x.C002?.StartsWith("REX") ?? false) && !UsesLegacyPartWeight(x)).ToList())
            {
                var l2 = GetLevel2(parent);
                bool usesChanged =
                    (l2?.Any(c => !PartWeightCalculator.IsSelfRow(c, parent.C002!) &&
                        string.Equals(c.ItemStepCode?.Trim(), changed, StringComparison.OrdinalIgnoreCase)) ?? false)
                    || ChainPredecessors(parent).Any(pr => pr == _rowSelected);
                if (!usesChanged) continue;

                var part = ComputePartWeight(parent, out _, out var available);
                if (!available || part == null) continue;

                parent.C021 = part;
                UpdateWeightPerPiece(parent);
                parent.C022 = parent.C024 - parent.C021;
            }
        }

        /// <summary>Công thức cũ (trừ Part Weight các bước trước) — chỉ dùng khi không có BOM Level 2.</summary>
        private void ApplyLegacyPartWeight()
        {
                    {
                        // C021 = Part weight thực tế (g/prs)
                        // = C023(bước này) − ∑C023(bước trước) − ∑C023(non-injection cùng bước)
                        var previuosStep = _scaleDataFinal
                            // C015 = 0: dòng REX chỉ có ở Level 2 (không thuộc step nào) — không tính vào các bước trước
                            .Where(x => x.C015 > 0 && x.C015 <= _rowSelected.C015 - 1).ToList();

                        // REX nonwoven chỉ có ở Level 2 (dòng C015 = 0) vẫn được trừ như REX cùng step
                        var l2Codes = _level2ByStep.TryGetValue(_rowSelected.C002?.Trim() ?? "", out var l2c)
                            ? l2c.Select(c => c.ItemStepCode?.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase)
                            : new HashSet<string?>();
                        var nonInjection = _scaleDataFinal
                            .Where(x => (x.C015 == _rowSelected.C015 &&
                                        (x.C002 == "Z-VHXXXXXX" ||
                                         (x.C002?.StartsWith("REX") ?? false)))
                                        || ((x.C015 ?? 0) == 0 && IsNonwovenCategory(x.C033) && l2Codes.Contains(x.C002?.Trim()))).ToList();

                         //tạm thời thêm vào để chạy đưuọc bước item có cleat nhưng ko dùng ở bước Inlay ở fVN đã, sau này khi sửa công thức BOM theo level 2 và update code xong thì bỏ phần này đi
                        if (_rowSelected.C003.StartsWith("Inlay"))
                        {
                            if (previuosStep == null
                                || !previuosStep.Any()
                                || (previuosStep != null
                                    && previuosStep.Any()
                                    && previuosStep.FirstOrDefault().C003.StartsWith("Cleat")
                                    )
                                )
                            {
                                _rowSelected.C021 = PartPerPair(_rowSelected);
                            }
                            else
                            {
                                _rowSelected.C021 = PartPerPair(_rowSelected)
                                                    - previuosStep?.Sum(x => x.C021)
                                                    - nonInjection?.Sum(x => x.C021);
                            }
                        }
                        else if (!_rowSelected.C003.StartsWith("Studs"))
                        {
                            _rowSelected.C021 = PartPerPair(_rowSelected)
                                                - previuosStep?.Sum(x => x.C021)
                                                - nonInjection?.Sum(x => x.C021);
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
                                _rowSelected.C021 = PartPerPair(_rowSelected);
                            }
                            else
                            {
                                _rowSelected.C021 = PartPerPair(_rowSelected)
                                                    - previuosStep?.Sum(x => x.C021)
                                                    - nonInjection?.Sum(x => x.C021);
                            }
                        }
                       
                    }
        }
        // ─────────────────────────────────────────────────────────────────────
        //  CONFIRM (lưu DB)
        // ─────────────────────────────────────────────────────────────────────
        /// <summary>Hiện lớp phủ chờ (thanh chạy) với nội dung <paramref name="message"/>; không cho hủy.</summary>
        private void ShowBusy(string message)
        {
            OverlayMessage = message;
            IsCancelLoadEnabled = false;
            IsOverlayVisible = true;
        }

        private void HideBusy()
        {
            IsOverlayVisible = false;
            OverlayMessage = "Loading…";
        }

        /// <summary>Confirm: hiện thanh chờ "Saving data…" trong lúc ghi DB, xong thì ẩn.</summary>
        private async Task ExecuteConfirmAsync(object? _)
        {
            if (IsOverlayVisible) return;   // đang bận
            ShowBusy("Saving data…");
            await Task.Delay(50);           // để UI kịp vẽ lớp phủ trước khi xử lý
            try { await ConfirmCoreAsync(); }
            finally { HideBusy(); }
        }

        private async Task ConfirmCoreAsync()
        {
            using var db = _dbFactory.CreateDbContext();
            using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                if (!_operatorSession.IsOperatorSet)
                {
                    HideBusy(); _dialog.Warn("RFID card not yet scanned.", "Warning");
                    return;
                }

                // _rowSelected có thể đã tự động chuyển sang bước kế tiếp (hoặc bị xóa) ngay sau khi
                // cân xong bước cuối (xem ExecuteSave) — dùng _lastWeighedRow (bước vừa cân xong gần
                // nhất) làm mốc cho việc phát hiện multi-size mold REX, để hành vi không đổi so với
                // trước khi có auto-advance.
                var confirmRow = _lastWeighedRow ?? _rowSelected;

                var pfx2 = GlobalVariable.PrefixUpToSecondHyphen(confirmRow.C002);
                var sameMolds = _scaleDataFinal.Where(x =>
                    x.C020 == confirmRow.C020 && x.C004 == confirmRow.C004 &&
                    x.C002 != confirmRow.C002 && x.C008 != confirmRow.C008 &&
                    GlobalVariable.PrefixUpToSecondHyphen(x.C002) == pfx2 &&
                    x.C015 == confirmRow.C015).ToList();

                if (sameMolds.Count == 0)
                {
                    foreach (var item in _scaleDataFinal)
                    {
                        if (item.AllowScale && item.C023 == 0 &&
                            (item.C024 == 0 && (item.C002?.StartsWith("REX") ?? false)))
                        {
                            HideBusy(); _dialog.Warn($"Scale not completed for step: {item.C002}.", "Warning");
                            return;
                        }
                    }
                }
                else
                {
                    var nonInjectionCheck = _scaleDataFinal.FirstOrDefault(x => x.C015 == confirmRow.C015 && x.C002.StartsWith("REX"));

                    var category = await db.Database
                          .SqlQueryRaw<CategoryOfItemModel>(
                              "sp_GetCategorryOfItem @ItemCode = {0}", nonInjectionCheck?.C002)
                          .AsNoTracking().ToListAsync();

                    var catCheck = GlobalVariable.ConfigSystem.CategoryOfNonInjectionUsagePartial?
                          .FirstOrDefault(x => x.CategoryCode == category?.FirstOrDefault()?.CategoryCode);

                    if (catCheck != null && nonInjectionCheck != null)
                    {
                        var sizes = _scaleDataFinal.Where(x => x.C015 == confirmRow.C015 && !x.C002.StartsWith("REX")).ToList();
                        //nonInjectionCheck.C008 = 


                        foreach (var item in sizes)
                        {
                            var upSize = _scaleDataFinal.FirstOrDefault(x => x.C002.StartsWith("REX") && x.C015 == item.C015 && x.C008 != item.C008 && string.IsNullOrEmpty(x.C008));

                            if (upSize != null)
                            {
                                upSize.C008 = item.C008;
                            }
                            else
                            {
                                var newItem = new FT600
                                {
                                    id = Guid.NewGuid(),

                                    C000 = _stepItemCodeScale.C000, // override
                                    C001 = item.C001,
                                    C002 = nonInjectionCheck.C002,
                                    C003 = nonInjectionCheck.C003,
                                    C004 = nonInjectionCheck.C004,
                                    C005 = nonInjectionCheck.C005,
                                    C006 = nonInjectionCheck.C006,
                                    C007 = nonInjectionCheck.C007,
                                    C008 = item.C008,
                                    C009 = item.C009,
                                    C010 = item.C010,
                                    C011 = item.C011,
                                    C012 = nonInjectionCheck.C012,
                                    C013 = item.C013,
                                    C014 = item.C014,
                                    C015 = item.C015,
                                    C016 = item.C016,
                                    C017 = nonInjectionCheck.C017,
                                    C018 = item.C018,
                                    C019 = nonInjectionCheck.C019,
                                    C020 = nonInjectionCheck.C020,
                                    C021 = nonInjectionCheck.C021,
                                    C022 = nonInjectionCheck.C022,
                                    C023 = nonInjectionCheck.C023,
                                    C024 = nonInjectionCheck.C024,
                                    C025 = nonInjectionCheck.C025,
                                    C026 = item.C026,
                                    C027 = item.C027,
                                    C028 = nonInjectionCheck.C028,
                                    C029 = nonInjectionCheck.C029,
                                    C030 = item.C030,
                                    C031 = item.C031,
                                    C032 = nonInjectionCheck.C032,
                                    C033 = nonInjectionCheck.C033,
                                    C034 = nonInjectionCheck.C034,
                                    C035 = nonInjectionCheck.C035,
                                    C036 = nonInjectionCheck.C036,
                                    C037 = nonInjectionCheck.C037,
                                    C038 = item.C038,

                                    // BaseEntity
                                    CreatedDate = item.CreatedDate,
                                    ModifiedDate = item.ModifiedDate,
                                    CreatedBy = item.CreatedBy,
                                    ModifiedBy = item.ModifiedBy,
                                    AllowScale = item.AllowScale,
                                    StatusText = nonInjectionCheck.StatusText,
                                    StatusDotColor = nonInjectionCheck.StatusDotColor,
                                    StatusBarColor = nonInjectionCheck.StatusBarColor,
                                };

                                _scaleDataFinal.Add(newItem);
                            }
                        }
                    }
                }

                var now = DateTime.Now;
                var machine = Environment.MachineName;
                var insert = _scaleDataFinal
                    .Where(x => x.AllowScale == true && x.C021 > 0).ToList();

                insert.ForEach(x =>
                {
                    RoundDoubles(x);   // các cột double lưu DB làm tròn 3 số lẻ
                    x.C010 = _operatorSession.OperatorInfo.C000;
                    x.C011 = _operatorSession.OperatorInfo.C001;
                    x.CreatedDate = now;
                    x.CreatedMachine = machine;
                    x.Mesocomp = _mesocomp;
                    x.Mesoyear = _mesoYear;

                    // FG code / FG name lưu theo FG của step đang scan/chọn cân (trọng lượng step dùng chung giữa các FG/màu).
                    // Không áp cho dòng cùng khuôn khác size (mỗi size có FG riêng).
                    // Step Logo: FG thật lấy từ sp_sssw_getproductInfo (FT601 của Logo ghi FG là chính step Logo).
                    var fgCode = _productFgCode ?? _stepItemCodeScale.C007;
                    var fgName = _productFgName ?? _stepItemCodeScale.C008;
                    if (!string.IsNullOrEmpty(fgCode) && !_sameMoldRowIds.Contains(x.id))
                    {
                        x.C013 = fgCode;
                        x.C014 = fgName;
                    }
                });

                // Hàng PU: lưu C019 = PU và C037 = PCS-L / PCS-R (cả step không có dòng FT601).
                if (insert.Any(r => r.IsPu))
                {
                    foreach (var r in insert.Where(r => string.IsNullOrEmpty(r.C019) && !string.IsNullOrWhiteSpace(r.C002) &&
                                 !r.C002.StartsWith("REX", StringComparison.OrdinalIgnoreCase)))
                        r.C019 = FT600.PuMachineGroup;
                    ApplyPuUnits(insert);
                }

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

                // ── Tự động gọi sp_ShotweightReport_UpsertFT609 nếu bước đang cân là bước cuối ──
                await ProcessUpsertFT609ForFinalStepsAsync(insert);

                HideBusy(); _dialog.Info("Scale shot weight saved successfully.", "Information");

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
                HideBusy(); _dialog.Error($"Transaction error: {ex.Message}\n{ex.InnerException?.Message}", "ERROR");
            }
        }

        /// <summary>
        /// Kiểm tra xem với con item FG đó bước đang cân/chọn cân có phải là bước cuối hay chưa:
        /// - Nếu chưa phải là bước cuối: bỏ qua (skip).
        /// - Khi bước đang chọn cân là bước cuối: gọi store sp_ShotweightReport_UpsertFT609 @FGItemCode = {0}, @CreatedBy = {1}
        ///   Store sẽ tự động chạy quy trình và xử lý trong database (thay CreatedBy bằng employName).
        /// </summary>
        private async Task ProcessUpsertFT609ForFinalStepsAsync(List<FT600> insert)
        {
            if (insert == null || !insert.Any()) return;

            try
            {
                var employName = !string.IsNullOrWhiteSpace(_operatorSession?.OperatorInfo?.C001)
                    ? _operatorSession.OperatorInfo.C001
                    : (!string.IsNullOrWhiteSpace(_operatorSession?.OperatorInfo?.C000)
                        ? _operatorSession.OperatorInfo.C000
                        : Environment.UserName);

                // Nhóm các bước theo mã FG
                var fgGroups = _scaleDataFinal
                    .Where(x => !string.IsNullOrEmpty(x.C013) || !string.IsNullOrEmpty(_stepItemCodeScale.C007))
                    .GroupBy(x => !string.IsNullOrEmpty(x.C013) ? x.C013! : _stepItemCodeScale.C007!)
                    .ToList();

                using var db = _dbFactory.CreateDbContext();

                foreach (var fgGroup in fgGroups)
                {
                    var fgCode = fgGroup.Key;
                    if (string.IsNullOrEmpty(fgCode)) continue;

                    // Tìm bước cuối cùng (có ParallelSequence / C015 lớn nhất) của con FG này
                    var maxSequence = fgGroup.Max(x => x.C015);
                    if (maxSequence == null) continue;

                    var finalStep = fgGroup.FirstOrDefault(x => x.C015 == maxSequence);
                    if (finalStep == null) continue;

                    // Kiểm tra bước đang cân/chọn cân có phải là bước cuối hay không:
                    // 1) Bước đang chọn cân trong UI combo/grid (_stepSelected) khớp với finalStep
                    var isSelectedStepFinal = (_stepSelected != null && !string.IsNullOrEmpty(_stepSelected.C004) && _stepSelected.C004 == finalStep.C002)
                        || (_stepSelected?.C010 != null && _stepSelected.C010 == maxSequence);

                    // 2) Bước cuối có nằm trong danh sách vừa cân được bấm Confirm (insert) hay không
                    var hasFinalStepInInsert = insert.Any(x =>
                        (!string.IsNullOrEmpty(x.C013) ? x.C013 : _stepItemCodeScale.C007) == fgCode &&
                        x.C015 == maxSequence &&
                        (x.C021 ?? 0) > 0);

                    // Nếu chưa phải là bước cuối (hoặc bước cuối chưa được cân xong) thì bỏ qua
                    if (!isSelectedStepFinal && !hasFinalStepInInsert)
                    {
                        _logger.LogInformation("FG {FGCode}: buoc dang can khong phai buoc cuoi (Seq: {Seq}, Max: {Max}). Bo qua.",
                            fgCode, _stepSelected?.C010, maxSequence);
                        continue;
                    }

                    if (!hasFinalStepInInsert)
                    {
                        continue;
                    }

                    _logger.LogInformation("FG {FGCode}: buoc can la buoc cuoi (Seq: {Max}). Thuc thi sp_ShotweightReport_UpsertFT609 (CreatedBy: {User})...",
                        fgCode, maxSequence, employName);

                    // Gọi store tự động xử lý quy trình và lưu vào bảng FT609
                    await db.Database.ExecuteSqlRawAsync(
                        "EXEC sp_ShotweightReport_UpsertFT609 @FGItemCode = {0}, @CreatedBy = {1}",
                        fgCode, employName);

                    _logger.LogInformation("Thuc thi sp_ShotweightReport_UpsertFT609 thanh cong cho FG {FGCode}.", fgCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Loi khi thuc thi sp_ShotweightReport_UpsertFT609 cho FG");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  CANCEL
        // ─────────────────────────────────────────────────────────────────────
        private void ExecuteCancel(object? _)
        {
            _labelInfo = new FT606_Label();
            _qrCodeScan = string.Empty;

            // Cancel cũng xóa luôn thẻ RFID đã quét (không chỉ dữ liệu step) — buộc operator phải
            // quét lại thẻ RFID cho lượt cân tiếp theo, tránh dùng nhầm operator của lượt đã hủy.
            // OperatorSession dùng chung Step/FG nên Clear() ở đây ảnh hưởng CẢ 2 tab — đúng ý đồ
            // "chuyển tab không xóa, chỉ Cancel mới xóa" (bấm Cancel ở tab nào cũng xóa chung).
            _operatorSession.Clear();

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
                // Step bị khóa ở lượt kiểm tra này → tự chuyển sang cân step đó (như quét QR / chọn từ dropdown Step)
                _ = SwitchToLockedStepAsync(rowSelect);
                return;
            }
            foreach (var step in PreviousStepsToCheck(rowSelect))
            {
                if (step.C021 == 0)
                {
                    _dialog.Warn("The previous step has not been weighed.", "Warning");
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

        /// <summary>
        /// Bấm vào step đang bị khóa (không được cân trong lượt này): kiểm tra rồi tải lại màn hình cân cho step đó,
        /// lấy đầy đủ thông tin như khi quét QR / chọn step từ dropdown (danh sách được dựng lại theo step này).
        /// </summary>
        private async Task SwitchToLockedStepAsync(FT600 row)
        {
            try
            {
                // C032 phải trỏ đúng step (cùng mã) — tránh C032 của FT601 khớp sai (vd. step F21L trỏ nhầm FT601 của F189)
                var item = _allStepCodeMaster.FirstOrDefault(x => row.C032 != null && x.FT601Id == row.C032 &&
                                string.Equals(x.StepItemCode?.Trim(), row.C002?.Trim(), StringComparison.OrdinalIgnoreCase))
                           ??_allStepCodeMaster.FirstOrDefault(x =>
                                string.Equals(x.StepItemCode?.Trim(), row.C002?.Trim(), StringComparison.OrdinalIgnoreCase) &&
                                (string.IsNullOrEmpty(row.C004) || x.Machine?.Trim() == row.C004.Trim()))
                           ?? _allStepCodeMaster.FirstOrDefault(x =>
                                string.Equals(x.StepItemCode?.Trim(), row.C002?.Trim(), StringComparison.OrdinalIgnoreCase));

                // Không có trong danh sách master (FT601 của lệnh) → vẫn chuyển step đang chọn sang xử lý:
                // OnStepSelectedAsync tìm master FT601 theo mã step, không có thì dựng từ sp_sssw_getproductInfo.
                bool inMaster = item != null;
                _switchRowIsPu = row.IsPu;
                _switchRowSeq = row.C015;
                item ??= new StepSelectModel
                {
                    StepItemCode = row.C002?.Trim() ?? string.Empty,
                    StepItemName = row.C003 ?? string.Empty,
                    Size = row.C008,
                    ArticlePairsShot = row.C017,
                    MoldPairsShot = row.C018,
                    Machine = row.C004,
                    HydraOrderNo = _stepItemCodeScale.C018,
                    FGCode = _stepItemCodeScale.C007,   // FG hiện tại: dự phòng khi SP không trả FG
                    FGName = _stepItemCodeScale.C008,
                    FT601Id = Guid.Empty
                };

                // Luôn hỏi xác nhận trước khi chuyển; đổi step sẽ dựng lại danh sách → mất giá trị đã cân trong phiên mà chưa Confirm
                var unsaved = _scaleDataFinal.Count(x => (x.C021 ?? 0) > 0 && !x.IsReusedFromHistory);
                var msg = $"Step {row.C002} ({row.C003}) is locked in the current check.{Environment.NewLine}" +
                          "Switch to weigh this step? The step list will be reloaded for it.";
                if (unsaved > 0)
                    msg += $"{Environment.NewLine}{Environment.NewLine}{unsaved} step(s) have been weighed but not confirmed yet and will be discarded.";
                if (!_dialog.Confirm(msg, "Switch step"))
                    return;

                _scaleDataFinal = new List<FT600>();
                _labelInfo = new FT606_Label();
                _qrCodeScan = string.Empty;
                ClearBarcodeAction?.Invoke();

                _suppress = true;
                if (inMaster) SetStepComboAction?.Invoke(item);   // step dựng tạm không có trong combo
                _suppress = false;

                await OnStepSelectedAsync(item);
            }
            catch (Exception ex)
            {
                _suppress = false;
                _logger.LogError(ex, "SwitchToLockedStepAsync");
                _dialog.Warn(ex.Message, "WARNING");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  LOGO: CÁC SIZE CÙNG NHÓM (sp_sssw_getGroupSize)
        // ─────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Với step Logo, lấy các size liên quan cùng nhóm (cùng tiền tố mã step) rồi thêm vào lưới cùng thứ tự step / máy / khuôn
        /// → được xử lý như 1 khuôn nhiều size (phân bổ runner, công thức cũ). Dòng size đang chọn giữ nguyên.
        /// </summary>
        private async Task AddLogoGroupSizesAsync(DbContextDogeWH db, FT601 stepCode)
        {
            try
            {
                var logoRow = _scaleDataFinal.FirstOrDefault(x =>
                                  string.Equals(x.C002?.Trim(), stepCode.C004?.Trim(), StringComparison.OrdinalIgnoreCase))
                              ?? _scaleDataFinal.FirstOrDefault(x => x.C003?.StartsWith("Logo") == true);
                if (logoRow == null) return;

                var prefix = GlobalVariable.PrefixUpToThirdHyphen(logoRow.C002);
                if (string.IsNullOrEmpty(prefix)) return;

                var sizes = await GetGroupSizesAsync(db, prefix);

                // FG / Main thật của step Logo đang cân (FT601 của Logo ghi FG là chính step) → theo sp_sssw_getproductInfo,
                // áp cho dòng Logo chính; các size thêm vào bên dưới copy từ dòng này.
                var product = await GetProductInfoAsync(db, logoRow.C002);
                if (product != null)
                {
                    if (!string.IsNullOrWhiteSpace(product.FgCode)) { logoRow.C013 = product.FgCode; _productFgCode = product.FgCode; }
                    if (!string.IsNullOrWhiteSpace(product.FgName)) { logoRow.C014 = product.FgName; _productFgName = product.FgName; }
                    if (!string.IsNullOrWhiteSpace(product.MainCode)) logoRow.C026 = product.MainCode;
                    if (!string.IsNullOrWhiteSpace(product.MainName)) logoRow.C027 = product.MainName;
                }

                foreach (var gs in sizes)
                {
                    var code = gs.ItemCode?.Trim();
                    if (string.IsNullOrEmpty(code) || code.EndsWith("-0000", StringComparison.Ordinal) ||
                        string.Equals(gs.SizeName?.Trim(), "no size", StringComparison.OrdinalIgnoreCase)) continue;
                    // Hàng PU: chỉ lấy size có hậu tố cuối giống step gốc (gốc ...-F21L → chỉ L; ...-F21R → chỉ R)
                    if (logoRow.IsPu && FT600.PuSideOf(code) != FT600.PuSideOf(logoRow.C002)) continue;
                    if (_scaleDataFinal.Any(x => string.Equals(x.C002?.Trim(), code, StringComparison.OrdinalIgnoreCase))) continue;

                    // Mỗi size có partitioning riêng: lấy từ FT601 của chính size đó (lệnh đang chạy, nếu không có thì master)
                    var sizeStep = _dataHydra.FirstOrDefault(x =>
                                       string.Equals(x.C004?.Trim(), code, StringComparison.OrdinalIgnoreCase) &&
                                       x.C015 == logoRow.C004 && x.C019 == logoRow.C020)
                                   ?? _dataHydra.FirstOrDefault(x =>
                                       string.Equals(x.C004?.Trim(), code, StringComparison.OrdinalIgnoreCase))
                                   ?? await db.FT601s.AsNoTracking()
                                       .Where(x => x.C004 == code)
                                       .OrderByDescending(x => x.CreatedDate)
                                       .FirstOrDefaultAsync();
                    // Có dữ liệu riêng của size thì dùng; không có (hoặc = 0) thì copy từ dòng Logo ban đầu
                    int ownPartition = sizeStep?.C013 != null ? (int)sizeStep.C013 : 0;
                    int sizePartition = ownPartition > 0 ? ownPartition : (int)(logoRow.C028 ?? 0);
                    int sizePpic = ownPartition > 0 ? ownPartition : (logoRow.C017 ?? sizePartition);

                    _scaleDataFinal.Add(new FT600
                    {
                        id = Guid.NewGuid(),
                        C000 = logoRow.C000,
                        C001 = logoRow.C001,
                        C002 = code,
                        C003 = gs.ItemName?.Trim() ?? string.Empty,
                        C004 = logoRow.C004,
                        C005 = logoRow.C005,
                        C006 = logoRow.C006,
                        C007 = logoRow.C007,
                        C008 = gs.SizeName?.Trim(),
                        C009 = logoRow.C009,
                        C012 = logoRow.C012,
                        C013 = logoRow.C013,
                        C014 = logoRow.C014,
                        C015 = logoRow.C015,
                        C017 = sizePpic,
                        C018 = sizeStep?.C014 ?? logoRow.C018,
                        C019 = logoRow.C019,
                        C020 = logoRow.C020,
                        C021 = 0,
                        C022 = 0,
                        C023 = 0,
                        C024 = 0,
                        C025 = logoRow.C025,
                        C026 = logoRow.C026,
                        C027 = logoRow.C027,
                        C028 = sizePartition,
                        C029 = logoRow.C029,
                        C032 = sizeStep?.Id ?? logoRow.C032,   // FT601 của chính size này
                        C033 = logoRow.C033,
                        C034 = logoRow.C034,
                        C035 = _percentOfUsage,
                        C036 = 0,
                        C037 = logoRow.C037,
                        C038 = logoRow.C038,
                        AllowScale = true
                    });
                    CopyMissingFgInfo(_scaleDataFinal[^1], logoRow);
                    _sameMoldAnchor[_scaleDataFinal[^1].id] = logoRow;
                    _sameMoldRowIds.Add(_scaleDataFinal[^1].id);
                }

                _scaleDataFinal = _scaleDataFinal.OrderBy(x => x.C015).ThenBy(x => x.C008).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AddLogoGroupSizesAsync error");
            }
        }

        /// <summary>
        /// Step được chọn nhưng không có trong FT601 của lệnh: ưu tiên master FT601 mới nhất đúng mã step;
        /// không có thì dựng FT601 tạm (không lưu DB) với FG / Main từ sp_sssw_getproductInfo, máy &amp; size &amp; partitioning từ step đang chọn.
        /// </summary>
        private async Task<FT601> CreateStepFromSelectionAsync(StepSelectModel s)
        {
            using var db = _dbFactory.CreateDbContext();
            var code = s.StepItemCode?.Trim();

            var master = await db.FT601s.AsNoTracking()
                .Where(x => x.C004 == code)
                .OrderByDescending(x => x.CreatedDate)
                .FirstOrDefaultAsync();
            if (master != null) return master;

            var product = await GetProductInfoAsync(db, code);
            var step = new FT601
            {
                Id = Guid.NewGuid(),
                C001 = EnumSampleLocation.Production,
                C002 = s.Size,
                C003 = product?.MainName,
                C004 = code,
                C005 = s.StepItemName,
                C007 = !string.IsNullOrWhiteSpace(product?.FgCode) ? product!.FgCode : s.FGCode,
                C008 = !string.IsNullOrWhiteSpace(product?.FgName) ? product!.FgName : s.FGName,
                C010 = _switchRowSeq,
                C013 = s.ArticlePairsShot,
                C014 = s.MoldPairsShot,
                C015 = s.Machine,
                C016 = _switchRowIsPu ? FT600.PuMachineGroup : null,
                C017 = false,
                C018 = s.HydraOrderNo,
                C020 = product?.MainCode
            };
            _syntheticStepIds.Add(step.Id);
            return step;
        }

        private sealed record ProductInfo(string? FgCode, string? FgName, string? MainCode, string? MainName);

        /// <summary>
        /// sp_sssw_getproductInfo @itemCode = mã step → FG chứa step (ProductItemCode / ProductName) và Main (MainCode / MainName).
        /// Lỗi hoặc không có dòng → null (giữ dữ liệu FT601).
        /// </summary>
        private async Task<ProductInfo?> GetProductInfoAsync(DbContextDogeWH db, string? stepCode)
        {
            if (string.IsNullOrWhiteSpace(stepCode)) return null;
            var conn = db.Database.GetDbConnection();
            bool openedHere = conn.State != ConnectionState.Open;
            try
            {
                if (openedHere) await conn.OpenAsync();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "sp_sssw_getproductInfo";
                cmd.CommandType = CommandType.StoredProcedure;
                var prm = cmd.CreateParameter();
                prm.ParameterName = "@itemCode";
                prm.Value = stepCode.Trim();
                cmd.Parameters.Add(prm);

                using var rd = await cmd.ExecuteReaderAsync();
                if (!await rd.ReadAsync()) return null;

                string? Str(string name)
                {
                    for (int i = 0; i < rd.FieldCount; i++)
                        if (string.Equals(rd.GetName(i), name, StringComparison.OrdinalIgnoreCase))
                            return rd.IsDBNull(i) ? null : Convert.ToString(rd.GetValue(i), System.Globalization.CultureInfo.InvariantCulture)?.Trim();
                    return null;
                }
                return new ProductInfo(Str("ProductItemCode"), Str("ProductName"), Str("MainCode"), Str("MainName"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetProductInfoAsync error ({Code})", stepCode);
                return null;
            }
            finally
            {
                if (openedHere) await conn.CloseAsync();
            }
        }

        /// <summary>Dòng thêm vào chưa có FG code/name, main code/name thì copy từ dòng gốc.</summary>
        private static void CopyMissingFgInfo(FT600 row, FT600 source)
        {
            if (string.IsNullOrWhiteSpace(row.C013)) row.C013 = source.C013;
            if (string.IsNullOrWhiteSpace(row.C014)) row.C014 = source.C014;
            if (string.IsNullOrWhiteSpace(row.C026)) row.C026 = source.C026;
            if (string.IsNullOrWhiteSpace(row.C027)) row.C027 = source.C027;
        }

        /// <summary>
        /// Gọi sp_sssw_getGroupSize và đọc kết quả bằng DbDataReader, đổi kiểu mềm (SizeCode có thể là int, C063/C064 có thể là decimal/float)
        /// để không phụ thuộc kiểu cột của SP (map trực tiếp bằng SqlQueryRaw từng lỗi "Unable to cast Int32 to String").
        /// </summary>
        private static async Task<List<GroupSizeModel>> GetGroupSizesAsync(DbContextDogeWH db, string prefix)
        {
            var result = new List<GroupSizeModel>();
            var conn = db.Database.GetDbConnection();
            bool openedHere = conn.State != ConnectionState.Open;
            if (openedHere) await conn.OpenAsync();
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "sp_sssw_getGroupSize";
                cmd.CommandType = CommandType.StoredProcedure;
                var prm = cmd.CreateParameter();
                prm.ParameterName = "@ItemCode";
                prm.Value = prefix;
                cmd.Parameters.Add(prm);

                using var rd = await cmd.ExecuteReaderAsync();
                static string? Str(System.Data.Common.DbDataReader r, string name)
                {
                    var i = r.GetOrdinal(name);
                    return r.IsDBNull(i) ? null : Convert.ToString(r.GetValue(i), System.Globalization.CultureInfo.InvariantCulture);
                }
                static double? Dbl(System.Data.Common.DbDataReader r, string name)
                {
                    var i = r.GetOrdinal(name);
                    return r.IsDBNull(i) ? null : Convert.ToDouble(r.GetValue(i), System.Globalization.CultureInfo.InvariantCulture);
                }
                while (await rd.ReadAsync())
                {
                    result.Add(new GroupSizeModel
                    {
                        ItemCode = Str(rd, "ItemCode"),
                        ItemName = Str(rd, "ItemName"),
                        SizeCode = Str(rd, "SizeCode"),
                        SizeName = Str(rd, "SizeName"),
                        C063 = Dbl(rd, "C063"),
                        C064 = Dbl(rd, "C064"),
                        ShotWeight = Dbl(rd, "ShotWeight")
                    });
                }
            }
            finally
            {
                if (openedHere) await conn.CloseAsync();
            }
            return result;
        }

        public void OnGridReset(FT600? rowSelect)
        {
            if (rowSelect == null) return;

            var rowReset = _scaleDataFinal.FirstOrDefault(x =>
                x.AllowScale && x.C002 == rowSelect.C002);
            if (rowReset == null)
            {
                _dialog.Info("Cannot reset this line.", "Warning");
                return;
            }

            // Trường hợp 1 khuôn nhiều size: reset luôn các dòng cùng khuôn (Machine + MoldID + prefix step code).
            var pfx3 = GlobalVariable.PrefixUpToThirdHyphen(rowReset.C002);
            var sameMolds = _scaleDataFinal.Where(x =>
                x.AllowScale &&
                x.C004 == rowReset.C004 &&
                x.C020 == rowReset.C020 &&
                GlobalVariable.PrefixUpToThirdHyphen(x.C002) == pfx3).ToList();

            foreach (var row in sameMolds)
                row.C021 = row.C022 = row.C023 = row.C024 = 0;

            _rowSelected = rowSelect;
            _articlePaisShotFinaly = _rowSelected.C017 != 0 && _rowSelected.C017 == _rowSelected.C028
                ? _rowSelected.C017 : _rowSelected.C028;

            IsHistoryExpanded = true;
            _historyExpanded = true;

            RefreshUI(true);
            UpdateReferencePanel();
            _ = LoadRefHistoryAsync(_rowSelected);
        }

        public void OnGridDelete(FT600? rowSelect)
        {
            if (rowSelect == null) return;

            var rowReset = _scaleDataFinal.FirstOrDefault(x =>
                x.AllowScale && x.C002 == rowSelect.C002);
            if (rowReset == null)
            {
                _dialog.Info("Cannot reset this line.", "Warning");
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
            // Khớp đúng dòng đang chọn (id duy nhất). Trước đây khớp theo C032 (FT601.Id) nên các size cùng nhóm dùng chung C032
            // chỉ sửa được dòng đầu tiên.
            var row = _scaleDataFinal.FirstOrDefault(x => x.id == _rowSelected.id)
                      ?? _scaleDataFinal.FirstOrDefault(x => x.C032 == _rowSelected.C032 && x.C002 == _rowSelected.C002);
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
        //  HYDRA SYNC — chuyển hẳn lên MainViewModel.GetDataHydraAsync() (hành động cấp app,
        //  dùng chung Step/FG — xem comment ở MainViewModel).
        // ─────────────────────────────────────────────────────────────────────

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
            var perUnit = "g/" + CountUnitOf(_rowSelected);
            _referenceRows = new List<ReferenceRow>
            {
                new() { No=1, FieldName="Total Weight",           Unit="g",
                        Std    = _stdRow?.C024 > 0 ? _stdRow.C024 : null,
                        Actual = _rowSelected?.C024 > 0 ? _rowSelected.C024 : null },
                new() { No=2, FieldName="Total Part Weight",             Unit=perUnit,
                        Std    = _stdRow?.C023 > 0 ? _stdRow.C023 : null,
                        Actual = _rowSelected?.C023 > 0 ? _rowSelected.C023 : null },
                new() { No=3, FieldName="Part Weight",                 Unit=perUnit,
                        Std    = _stdRow?.C021 > 0 ? _stdRow.C021 : null,
                        Actual = _rowSelected?.C021 > 0 ? _rowSelected.C021 : null },
                new() { No=4, FieldName="Runner / Excess Mat. Weight", Unit=perUnit,
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
                CountUnit = CountUnitOf(_rowSelected);
                Qty = _rowSelected?.C025?.ToString();
                FgItemCode = _rowSelected?.C013;
                FgName = _rowSelected?.C014;
                UsagePct = (_rowSelected?.C035 != 0
                                  ? _rowSelected?.C035 : _percentOfUsage)?.ToString();
                // Hiển thị remark của đúng step đang chọn — không phải 1 giá trị dùng chung.
                Remark = _rowSelected?.C038;
                StepName = _rowSelected?.C003;


                // Enable/disable Usage %
                var catCheck = GlobalVariable.ConfigSystem.CategoryOfNonInjectionUsagePartial?
                    .FirstOrDefault(x => x.CategoryCode == _rowSelected?.C033);
                IsUsagePctReadOnly =
                    !(_rowSelected?.C002?.StartsWith("REX") == true && catCheck != null);

                // Status dots + bars
                UpdateStepStatuses();

                // Rebuild grid collection
                RefreshStepsGridCore();

                UpdateReferencePanel();
            });
        }

        /// <summary>
        /// Ép DataGrid TOTAL STEPS rebind lại toàn bộ row từ _scaleDataFinal. FT600 không implement
        /// INotifyPropertyChanged nên việc sửa trực tiếp 1 field (VD C038 khi gõ Remark) không tự
        /// đẩy lên UI — phải Clear + Add lại ObservableCollection để WPF render lại cell mới nhất.
        /// Luôn chạy trên UI thread (Dispatcher.Invoke), an toàn để gọi từ bất kỳ thread nào.
        /// </summary>
        private void RefreshStepsGrid()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(RefreshStepsGridCore);
        }

        /// <summary>Phần lõi (không tự Dispatcher.Invoke) dùng chung bởi RefreshUI() (đã ở trong
        /// Dispatcher.Invoke) và RefreshStepsGrid() (public entry, tự lo Dispatcher).</summary>
        private void RefreshStepsGridCore()
        {
            StepsCollection.Clear();
            if (_scaleDataFinal != null)
                foreach (var item in _scaleDataFinal)
                {
                    RefreshLevel2Details(item);
                    StepsCollection.Add(item);
                }
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
            _lastWeighedRow = null;
            _allStepsFG = new List<BomWinlineModel>();
            _level2ByStep = new(StringComparer.OrdinalIgnoreCase);
            _weighedChildrenFromDb = new(StringComparer.OrdinalIgnoreCase);
            _pieceQtyByCode = new(StringComparer.OrdinalIgnoreCase);
            _bom.ClearCache();
            _stepItemCodeScale = new FT601();
            _scaleData = new List<FT600>();
            _scaleDataFinal = new List<FT600>();
            _newScale = true;
            _qrCodeScan = string.Empty;
            _percentOfUsage = GlobalVariable.ConfigSystem.PercentOfUserNonWoven;
            _articlePaisShotFinaly = 0;
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

        // OpenRfidInputDialog() chuyển hẳn lên MainViewModel (dùng chung Step/FG qua
        // OperatorSessionService) — không còn ở đây nữa.

        // OpenSettings()/CheckUpdate()/AutoUpdater_* chuyển hẳn lên MainViewModel (hành động cấp
        // app dùng chung Step/FG — xem comment ở MainViewModel).

        private void ToggleMaximize()
        {
            var win = System.Windows.Application.Current.MainWindow;
            win.WindowState = win.WindowState == System.Windows.WindowState.Normal
                ? System.Windows.WindowState.Maximized
                : System.Windows.WindowState.Normal;
        }
    }
}
