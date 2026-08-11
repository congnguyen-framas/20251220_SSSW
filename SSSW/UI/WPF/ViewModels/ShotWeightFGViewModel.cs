// ============================================================================
//  ShotWeightFGViewModel.cs
//  MVVM ViewModel cho ShotWeightFGWindow – cân shot weight theo FG code
//  (không có khái niệm Step/BOM/Mold như ShotWeightViewModel). Mỗi lần
//  scan/chọn FG code sẽ thêm 1 dòng mới vào SAMPLING LIST (không build lại
//  toàn bộ danh sách step như màn hình chính).
//  Formula cân: 2 lần cân (giống nhánh Injection Items của ShotWeightViewModel,
//  bỏ phần trừ previous-step/non-injection vì FG không có step chain):
//    Lần 1 (C024==0): C024 = ScaleValue
//    Lần 2 (C024>0) : C023 = ScaleValue; C021 = C023;
//                      C022 = (C024 − C023×prsShot) / prsShot   (prsShot = C028)
//  Namespace : SSSW.UI.WPF.ViewModels
// ============================================================================
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ScanAndScale.Core.Models;
using SSSW.models;
using SSSW.modelss;
using SSSW.UI.WPF.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
// ── Disambiguate WPF vs WinForms/Drawing types (project uses UseWPF + UseWindowsForms) ──
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;

namespace SSSW.UI.WPF.ViewModels
{
    public class ShotWeightFGViewModel : BaseViewModel
    {
        // ── DI ──────────────────────────────────────────────────────────────
        private readonly IDbContextFactory<DbContextDogeWH> _dbFactory;
        // Lệch nhẹ so với chữ ký constructor nêu trong plan (không có IServiceProvider):
        // OpenRfidInputDialog() cần resolve frmRfidInput qua DI, và plan yêu cầu copy
        // method này "unchanged" từ ShotWeightViewModel — nên vẫn cần IServiceProvider.
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ShotWeightFGViewModel> _logger;

        // ── Cancellation ────────────────────────────────────────────────────
        private CancellationTokenSource? _loadCts;

        // ─────────────────────────────────────────────────────────────────────
        //  DOMAIN STATE
        // ─────────────────────────────────────────────────────────────────────
        private List<FT601> _dataHydra = new();
        private List<FT600> _samples = new();          // backing list cho SamplesCollection
        private FT600? _rowSelected;
        private FT029_Operator_RFID _operatorInfo = new();
        private string _employeeCode = string.Empty;
        private FT606_Label _labelInfo = new();

        // Đếm sample theo từng FG code, reset khi Confirm/Cancel thành công (theo session).
        private readonly Dictionary<string, int> _sampleCounters = new();

        private string _mesocomp = string.Empty;
        private int _mesoYear = 0;
        private double _scaleValue = 0;
        private bool _historyExpanded = false;

        private FT600 _stdRow = new();
        private List<FT600> _refHistory = new();
        private List<ReferenceRow> _referenceRows = new();

        private List<FgSelectModel> _fgCodeMaster = new();
        public List<FgSelectModel> FgCodeMaster
        {
            get => _fgCodeMaster;
            private set { _fgCodeMaster = value; OnPropertyChanged(); }
        }

        // ── Collections bound to grids ──────────────────────────────────────
        public ObservableCollection<FT600> SamplesCollection { get; } = new();
        public ObservableCollection<FT600> HistoryCollection { get; } = new();
        public ObservableCollection<ReferenceRow> RefRowsCollection { get; } = new();

        // ─────────────────────────────────────────────────────────────────────
        //  BINDABLE DISPLAY PROPERTIES
        // ─────────────────────────────────────────────────────────────────────

        #region Title / Header
        private string _windowTitle = "FG – Shotweight Station";
        public string WindowTitle
        {
            get => _windowTitle;
            set { _windowTitle = value; OnPropertyChanged(); }
        }

        private string _userName = string.Empty;
        public string UserName
        {
            get => _userName;
            set { _userName = value; OnPropertyChanged(); }
        }
        #endregion

        #region FG Information Panel
        private string? _fgName;
        public string? FgName
        {
            get => _fgName;
            set { _fgName = value; OnPropertyChanged(); }
        }

        private string? _fgCode;
        public string? FgCode
        {
            get => _fgCode;
            set { _fgCode = value; OnPropertyChanged(); }
        }

        private string? _machine;
        public string? Machine
        {
            get => _machine;
            set { _machine = value; OnPropertyChanged(); }
        }

        private string? _size;
        public string? Size
        {
            get => _size;
            set { _size = value; OnPropertyChanged(); }
        }

        private string? _sampleNum;
        public string? SampleNum
        {
            get => _sampleNum;
            set { _sampleNum = value; OnPropertyChanged(); }
        }

        private string? _remark;
        public string? Remark
        {
            get => _remark;
            set
            {
                _remark = value;
                OnPropertyChanged();

                // Remark chỉ áp dụng cho đúng sample (Sample Num) đang chọn — KHÔNG lan sang các
                // sample khác đã có trong SAMPLING LIST (trước đây ForEach lên toàn bộ _samples,
                // khiến mọi dòng bị đè cùng 1 remark). Nếu chưa chọn sample nào thì bỏ qua.
                if (_rowSelected != null)
                {
                    _rowSelected.C038 = value;

                    // FT600 không implement INotifyPropertyChanged nên gán C038 ở trên không tự
                    // đẩy lên DataGrid — phải ép rebind lại SamplesCollection để cột REMARKS hiện
                    // giá trị mới ngay khi gõ. Không gọi RefreshUI() ở đây vì RefreshUI() có set lại
                    // Remark = _rowSelected.C038, sẽ gọi ngược lại setter này → đệ quy vô hạn.
                    RefreshSamplesGrid();
                }
            }
        }

        // RFID name textbox
        private string _rfidName = string.Empty;
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
            set { _historyToggleTextSecond = value; OnPropertyChanged(); }
        }

        private string _historyToggleTextDetail = string.Empty;
        public string HistoryToggleTextDetail
        {
            get => _historyToggleTextDetail;
            set { _historyToggleTextDetail = value; OnPropertyChanged(); }
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
        /// <summary>Code-behind gán: reset DevExpress LookUpEdit (cbFgCode) selection.</summary>
        public Action? ClearFgComboAction { get; set; }

        // ─────────────────────────────────────────────────────────────────────
        //  COMMANDS
        // ─────────────────────────────────────────────────────────────────────
        public RelayCommand ReloadCommand { get; }
        public RelayCommand HistoryViewCommand { get; }
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
        public ShotWeightFGViewModel(
            IDbContextFactory<DbContextDogeWH> dbFactory,
            IServiceProvider serviceProvider,
            ILogger<ShotWeightFGViewModel> logger)
        {
            _dbFactory = dbFactory;
            _serviceProvider = serviceProvider;
            _logger = logger;

            ReloadCommand = new RelayCommand(() => _ = LoadDataAsync(TimeSpan.FromSeconds(30)));
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

            ClearBarcodeAction = () => BarcodeScannedValue = string.Empty;
            ClearRfidAction = () => RfidCardCode = string.Empty;
        }

        private void ToggleMaximize()
        {
            var win = System.Windows.Application.Current.Windows
                .OfType<System.Windows.Window>()
                .FirstOrDefault(w => w.IsActive) ?? System.Windows.Application.Current.MainWindow;
            win.WindowState = win.WindowState == System.Windows.WindowState.Maximized
                ? System.Windows.WindowState.Normal
                : System.Windows.WindowState.Maximized;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  LOAD MASTER DATA (FT601s, grouped by FG code) + mesocomp/mesoyear/title
        // ─────────────────────────────────────────────────────────────────────
        public async Task LoadDataAsync(TimeSpan? timeout = null)
        {
            timeout ??= TimeSpan.FromSeconds(60);
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
                var (data, mesocomp, mesoYear) = await Task.Run(async () =>
                {
                    token.ThrowIfCancellationRequested();
                    using var ctx = _dbFactory.CreateDbContext();

                    var comp = ctx.Database.SqlQueryRaw<string>("sp_MaterialGetCompanyName")
                                  .AsEnumerable().FirstOrDefault() ?? string.Empty;
                    var year = ctx.Database.SqlQueryRaw<int>("sp_MaterialGetMesoyear")
                                  .AsEnumerable().FirstOrDefault();

                    var list = await ctx.FT601s.Where(x => x.C021 == true).ToListAsync(token);
                    return (list, comp, year);
                }, token);

                _dataHydra = data;
                _mesocomp = mesocomp;
                _mesoYear = mesoYear;

                var master = _dataHydra
                    .GroupBy(x => x.C007)
                    .Select(g => g.OrderByDescending(x => x.C010).First())
                    .Select(x => new FgSelectModel
                    {
                        FGCode = x.C007,
                        FGName = x.C008,
                        Machine = x.C015,
                        Size = x.C002,
                        HydraOrderNo = x.C018,
                        FT601Id = x.Id,
                        ArticlePairsShot = x.C013,
                        MainCode = x.C020,
                        MainName = x.C003,
                        Article = x.C006,
                        MachineGroup = x.C016,
                        MoldId = x.C019,
                        MoldPairsShot = x.C014
                    })
                    .OrderBy(x => x.FGName)
                    .ToList();

                // Category/Unit KHÔNG còn được batch-load ở đây nữa.
                // Lý do: master là TOÀN BỘ FT601 active (không lọc theo machine/company) — với hệ
                // thống thực tế lên tới hàng nghìn FG item (~7600+), kể cả khi chia batch 200
                // item/lần (fix trước) thì tổng số round-trip vẫn quá lớn và vẫn vượt timeout →
                // TaskCanceledException lặp lại. sp_GetCategorryOfItem vốn chỉ được thiết kế để
                // gọi với 1 vài item (xem cách gọi bên frmShotWeightScale/ShotWeightViewModel —
                // luôn truyền "sameMolds", không bao giờ truyền cả danh mục).
                // Vì Category/Unit chỉ thực sự được dùng tại thời điểm 1 FG cụ thể được CHỌN/QUÉT
                // (AddSample/OnFgSelectedAsync/OnBarcodeScannedAsync), nó được tra cứu LAZY — đúng
                // 1 item — ngay tại thời điểm đó (xem ResolveCategoryAsync), không cần thiết phải
                // preload cho toàn bộ master list.
                var location = _mesocomp switch
                {
                    "VNT1" => "fVN",
                    "FKV" => "fKV",
                    "FTT1" => "fFT",
                    "05FI" => "fIN",
                    "fGE" => "fGE",
                    _ => "Unknown"
                };
                var appVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "N/A";

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    FgCodeMaster = master;
                    WindowTitle = $"{location} – Shotweight Station For FG – Ver-{appVersion}";
                    DeltaInformation = $"STD: Target Weight ↔ ACTUAL: Live Weight · auto colored vs. tolerance: -{GlobalVariable.ConfigSystem.DeltaLevel1}g<=Delta<={GlobalVariable.ConfigSystem.DeltaLevel1}g -->green;  -{GlobalVariable.ConfigSystem.DeltaLevel2}g<=Delta<=-{GlobalVariable.ConfigSystem.DeltaLevel1}g or {GlobalVariable.ConfigSystem.DeltaLevel1}g<=Delta<={GlobalVariable.ConfigSystem.DeltaLevel2}g -->Orange; Delta<-{GlobalVariable.ConfigSystem.DeltaLevel2}g or Delta>{GlobalVariable.ConfigSystem.DeltaLevel2}g -->Red.";
                });
            }
            catch (OperationCanceledException)
            {
                // Phân biệt user bấm "Cancel Load" (không cần log) với timeout tự động (đáng log —
                // trước đây bị nuốt lặng lẽ hoàn toàn, không có dấu vết gì để chẩn đoán khi
                // sp_GetCategorryOfItem/FT601 query chạy chậm).
                if (timeoutCts.IsCancellationRequested)
                    _logger.LogWarning(
                        "LoadDataAsync timed out after {TimeoutSeconds}s — check sp_GetCategorryOfItem/FT601 query performance",
                        timeout.Value.TotalSeconds);
            }
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

        /// <summary>Scale hardware trả về giá trị cân mới (từ ScanAndScale.Core driver). Copy nguyên
        /// vẹn từ ShotWeightViewModel — logic quy đổi đơn vị/gating theo stable là logic chung,
        /// không phải logic step.</summary>
        public void OnScaleValueChanged(double value, bool stable = false, bool tare = false, string unit = "G")
        {
            _scaleValue = Math.Round(value * 1000, 2);
            ScaleValue = _scaleValue;
            ScaleDisplay = _scaleValue.ToString("F2");
            ScaleStable = stable;
            ScaleTare = tare;
            ScaleUnit = unit;

            if (ScaleStable == false && ReadOnlyScale == true)
                EnableBtnSaveScale = false;
            else
                EnableBtnSaveScale = true;
        }

        /// <summary>RFID đọc được employee code. Copy nguyên vẹn từ ShotWeightViewModel — logic
        /// operator/permission là logic chung, không phải logic step.</summary>
        public void OnRfidValueChanged(string rfidCode)
        {
            try
            {
                _employeeCode = rfidCode;
                if (string.IsNullOrEmpty(_employeeCode))
                    throw new Exception("ID cannot be null.");

                using var db = _dbFactory.CreateDbContext();

                var operatorRows = db.fT029_Operator_RFIDs
                    .Where(x => x.C000.Contains(_employeeCode))
                    .ToList();

                if (operatorRows.Count == 0)
                {
                    ClearRfidAction?.Invoke();
                    FocusRfidNameAction?.Invoke();
                    throw new Exception(
                        $"Employee ID {_employeeCode} not found. " +
                        "Please enter the name and press Enter to register.");
                }

                var allowedDepartments = db.FT031s
                    .Where(d => d.C000 == "IT" || d.C000 == "QC")
                    .ToList();

                _operatorInfo = operatorRows.FirstOrDefault(op => allowedDepartments.Any(d => d.Id == op.C002))
                                 ?? new FT029_Operator_RFID();

                if (_operatorInfo.Id == Guid.Empty)
                {
                    _employeeCode = string.Empty;
                    RfidName = string.Empty;
                    ClearRfidAction?.Invoke();
                    throw new Exception("Employee does not have permission for this function.");
                }

                _operatorInfo.DepartmentInfor = allowedDepartments.First(d => d.Id == _operatorInfo.C002);

                RfidName = _operatorInfo.C001;
                UserName = $"{_operatorInfo.C000} - {_operatorInfo.C001}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RFID error");
                System.Windows.Forms.MessageBox.Show(ex.Message, "ERROR",
                    (MessageBoxButtons)MessageBoxButton.OK, (MessageBoxIcon)MessageBoxImage.Error);
            }
        }

        /// <summary>Người dùng nhấn Enter trong ô RFID Name để đăng ký operator mới. Copy nguyên
        /// vẹn từ ShotWeightViewModel.</summary>
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
                    (MessageBoxButtons)MessageBoxButton.OK, (MessageBoxIcon)MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RFID name enter error");
                System.Windows.Forms.MessageBox.Show(ex.Message, "WARNING",
                    (MessageBoxButtons)MessageBoxButton.OK, (MessageBoxIcon)MessageBoxImage.Warning);
            }
        }

        /// <summary>Mở màn hình xem lại lịch sử cân (frmMainView) — giống hệt nút History bên
        /// ShotWeightViewModel (dùng chung 1 màn hình cho cả 2 form, không tách riêng theo FG/step).</summary>
        private void OpenHistoryView()
        {
            var nf = _serviceProvider.GetRequiredService<frmMainView>();
            nf.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            nf.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            nf.ShowDialog();
        }

        /// <summary>Mở dialog nhập tay Employee ID (khi không quét được RFID). Copy nguyên vẹn từ
        /// ShotWeightViewModel.</summary>
        public void OpenRfidInputDialog()
        {
            var dlg = _serviceProvider.GetRequiredService<frmRfidInput>();
            dlg.Owner = System.Windows.Application.Current.MainWindow;
            dlg.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;

            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                RfidCardCode = dlg.ResultRfidCode;
                OnRfidValueChanged(dlg.ResultRfidCode);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  FG SELECTION / BARCODE SCAN → thêm 1 sample mới vào SAMPLING LIST
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Gọi từ code-behind khi DevExpress LookUpEdit (cbFgCode) thay đổi selection.</summary>
        public async Task OnFgSelectedAsync(FgSelectModel? selected)
        {
            if (selected == null) return;
            await ResolveCategoryAsync(selected);
            AddSample(selected);
        }

        /// <summary>Barcode scanner đọc được QR code của label FG.</summary>
        public async Task OnBarcodeScannedAsync(string barcode)
        {
            try
            {
                using var db = _dbFactory.CreateDbContext();

                _labelInfo = await db.FT606s.FirstOrDefaultAsync(x => x.C001 == barcode)
                             ?? throw new Exception("Label information not found.");

                var hydraRow = _dataHydra.FirstOrDefault(x => x.Id == _labelInfo.C000)
                               ?? throw new Exception("FG information not found.");

                var newFg = new FgSelectModel
                {
                    FGCode = hydraRow.C007,
                    FGName = hydraRow.C008,
                    Machine = hydraRow.C015,
                    Size = hydraRow.C002,
                    HydraOrderNo = hydraRow.C018,
                    FT601Id = hydraRow.Id,
                    ArticlePairsShot = hydraRow.C013,
                    MainCode = hydraRow.C020,
                    MainName = hydraRow.C003,
                    Article = hydraRow.C006,
                    MachineGroup = hydraRow.C016,
                    MoldId = hydraRow.C019,
                    MoldPairsShot = hydraRow.C014,
                    QrCode = _labelInfo.C001
                };
                await ResolveCategoryAsync(newFg);
                AddSample(newFg);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Barcode scan error");
                System.Windows.Forms.MessageBox.Show(ex.Message, "WARNING",
                    (MessageBoxButtons)MessageBoxButton.OK, (MessageBoxIcon)MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Tra Category/Unit cho đúng 1 FG code, gọi LAZY tại thời điểm FG được CHỌN (combo) hoặc
        /// QUÉT (barcode) — thay vì batch-load sẵn cho toàn bộ master list trong LoadDataAsync (đã
        /// gỡ bỏ, xem ghi chú trong LoadDataAsync). sp_GetCategorryOfItem chỉ nhận đúng 1 item ở
        /// đây nên luôn nhanh, không phụ thuộc vào tổng số FG item của toàn hệ thống.
        /// </summary>
        private async Task ResolveCategoryAsync(FgSelectModel fg)
        {
            if (string.IsNullOrEmpty(fg.FGCode) || fg.CategoryCode.HasValue) return;
            try
            {
                using var ctx = _dbFactory.CreateDbContext();
                var cat = (await ctx.Database
                        .SqlQueryRaw<CategoryOfItemModel>("sp_GetCategorryOfItem @ItemCode = {0}", fg.FGCode)
                        .AsNoTracking().ToListAsync())
                    .FirstOrDefault();
                fg.CategoryCode = cat?.CategoryCode;
                fg.CategoryName = cat?.CategoryName;
                fg.Unit = cat?.Unit;
            }
            catch (Exception ex)
            {
                // Không chặn luồng thêm sample nếu tra Category lỗi — chỉ log, Category/Unit sẽ để
                // trống trên dòng FT600 (không phải lỗi nghiêm trọng, có thể sửa tay sau).
                _logger.LogError(ex, "ResolveCategoryAsync error for FGCode {FGCode}", fg.FGCode);
            }
        }

        /// <summary>
        /// Thêm 1 dòng FT600 mới vào SAMPLING LIST cho FG vừa scan/chọn. Sample num được đếm
        /// riêng theo từng FG code, bắt đầu lại từ 1 khi FG code đó lần đầu xuất hiện trong session.
        /// Các cột step-only (C002/C003/C015/.../C037...) không bao giờ được set — tự động thỏa
        /// mãn yêu cầu "loại bỏ thông tin step" khi Confirm, không cần lọc thêm.
        /// </summary>
        private void AddSample(FgSelectModel fg)
        {
            if (string.IsNullOrEmpty(fg.FGCode)) return;

            // Đang cân dở cho 1 FG item (session hiện tại đã có ít nhất 1 sample) → không cho
            // phép quét/chọn FG khác vào. Phải Confirm hoặc Cancel để kết thúc session trước.
            var activeFgCode = _samples.Count > 0 ? _samples[0].C013 : null;
            if (!string.IsNullOrEmpty(activeFgCode) &&
                !string.Equals(activeFgCode, fg.FGCode, StringComparison.Ordinal))
            {
                ClearFgComboAction?.Invoke();
                ClearBarcodeAction?.Invoke();
                System.Windows.Forms.MessageBox.Show(
                    $"Currently weighing FG item '{activeFgCode}'. Please Confirm or Cancel before switching to a different FG item.",
                    "Warning", (MessageBoxButtons)MessageBoxButton.OK, (MessageBoxIcon)MessageBoxImage.Warning);
                return;
            }

            if (!_sampleCounters.ContainsKey(fg.FGCode)) _sampleCounters[fg.FGCode] = 0;
            var sampleNum = ++_sampleCounters[fg.FGCode];

            var row = new FT600
            {
                id = Guid.NewGuid(),
                C013 = fg.FGCode,
                C014 = fg.FGName,
                C004 = fg.Machine,
                C008 = fg.Size,
                C000 = fg.HydraOrderNo,
                C032 = fg.FT601Id,
                C009 = sampleNum,
                // Remark của mỗi sample độc lập với nhau — sample mới luôn bắt đầu trống, không
                // kế thừa remark của sample khác (theo yêu cầu: remark ăn theo đúng Sample Num).
                C038 = string.Empty,
                C028 = fg.ArticlePairsShot,
                C017 = fg.ArticlePairsShot,
                C018 = fg.MoldPairsShot,
                C026 = fg.MainCode,
                C027 = fg.MainName,
                C005 = fg.Article,
                C019 = fg.MachineGroup,
                C020 = fg.MoldId,
                C033 = fg.CategoryCode,
                C034 = fg.CategoryName,
                C037 = fg.Unit,
                C012 = fg.QrCode,
                AllowScale = true,
                StatusText = "Pending",
                StatusDotColor = "#9E9E9E",
                StatusBarColor = "Transparent",
            };

            _samples.Add(row);
            _rowSelected = row;

            IsHistoryExpanded = true;
            _historyExpanded = true;

            RefreshUI();
            _ = LoadRefHistoryAsync(row);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  SAVE (weigh the currently selected sample) — 2-lần cân, giống công thức
        //  Injection Items của ShotWeightViewModel (mục 4.1/4.2 trong doc) nhưng bỏ
        //  phần trừ previous-step/non-injection vì FG không có khái niệm step chain
        //  và không có toggle Runner (YES/NO) — C022 luôn được tính.
        // ─────────────────────────────────────────────────────────────────────
        private void ExecuteSave(object? _)
        {
            if (_rowSelected == null)
            {
                System.Windows.Forms.MessageBox.Show("Please select or scan an FG item first.", "Warning",
                    (MessageBoxButtons)MessageBoxButton.OK, (MessageBoxIcon)MessageBoxImage.Warning);
                return;
            }

            if (!_rowSelected.AllowScale)
            {
                System.Windows.Forms.MessageBox.Show("Cannot scale this sample.", "Warning",
                    (MessageBoxButtons)MessageBoxButton.OK, (MessageBoxIcon)MessageBoxImage.Warning);
                return;
            }

            // Mẫu đang chọn đã cân đủ 2 lần (Done) mà vẫn tiếp tục nhấn Save (giữ nguyên FG,
            // cân tiếp) → tự động thêm 1 mẫu mới cùng FG code vào SAMPLING LIST để bắt đầu
            // chu kỳ cân mới, thay vì ghi đè lên kết quả đã hoàn thành của mẫu cũ. Giá trị cân
            // hiện tại (ScaleValue) sẽ trở thành lần cân đầu tiên (C024) của mẫu mới này.
            if ((_rowSelected.C021 ?? 0) > 0)
            {
                AddSample(new FgSelectModel
                {
                    FGCode = _rowSelected.C013,
                    FGName = _rowSelected.C014,
                    Machine = _rowSelected.C004,
                    Size = _rowSelected.C008,
                    HydraOrderNo = _rowSelected.C000,
                    FT601Id = _rowSelected.C032 ?? Guid.Empty,
                    ArticlePairsShot = _rowSelected.C028.HasValue ? (int)_rowSelected.C028.Value : null,
                    MainCode = _rowSelected.C026,
                    MainName = _rowSelected.C027,
                    Article = _rowSelected.C005,
                    MachineGroup = _rowSelected.C019,
                    MoldId = _rowSelected.C020,
                    MoldPairsShot = _rowSelected.C018,
                    CategoryCode = _rowSelected.C033,
                    CategoryName = _rowSelected.C034,
                    Unit = _rowSelected.C037
                    // QrCode không mang sang mẫu mới — QR chỉ gắn với 1 lần quét barcode cụ thể.
                });
                // AddSample() đã set _rowSelected = mẫu mới; tiếp tục xử lý bên dưới như bình thường.
            }

            if ((_rowSelected.C024 ?? 0) == 0)
            {
                // Lần cân đầu tiên: tổng khối lượng runner + part (Total W_injection)
                _rowSelected.C024 = ScaleValue;
            }
            else
            {
                // Lần cân thứ hai: khối lượng part đã tách runner (Total PW)
                _rowSelected.C023 = ScaleValue;

                var prsShot = _rowSelected.C028 ?? 1.0;

                _rowSelected.C021 = _rowSelected.C023;
                // C022 = Runner/Excess weight (g/prs) = (TotalWeight − PartWeight×pairs) / pairs
                _rowSelected.C022 = Math.Round(
                    ((_rowSelected.C024 - (_rowSelected.C023 * prsShot)) / prsShot) ?? 0, 3);
            }

            RefreshUI();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  GRID ROW ACTIONS
        // ─────────────────────────────────────────────────────────────────────
        public void OnGridScale(FT600? rowSelect)
        {
            if (rowSelect == null) return;

            if (!rowSelect.AllowScale)
            {
                System.Windows.Forms.MessageBox.Show("Do not allow to scale this sample.", "Warning",
                    (MessageBoxButtons)MessageBoxButton.OK, (MessageBoxIcon)MessageBoxImage.Warning);
                return;
            }

            _rowSelected = rowSelect;

            IsHistoryExpanded = true;
            _historyExpanded = true;

            RefreshUI();
            _ = LoadRefHistoryAsync(_rowSelected);
        }

        public void OnGridReset(FT600? rowSelect)
        {
            if (rowSelect == null) return;

            rowSelect.C021 = rowSelect.C022 = rowSelect.C023 = rowSelect.C024 = 0;
            _rowSelected = rowSelect;

            RefreshUI();
        }

        public void OnGridDelete(FT600? rowSelect)
        {
            if (rowSelect == null) return;

            _samples.Remove(rowSelect);
            if (_rowSelected == rowSelect) _rowSelected = null;

            RenumberSamples();

            RefreshUI();
        }

        /// <summary>
        /// Đánh lại SampleNum (C009) liên tiếp từ 1 cho các sample còn lại trong SAMPLING LIST,
        /// theo từng FG code (giữ nguyên thứ tự hiện có trong _samples). Đồng bộ lại
        /// _sampleCounters theo số lượng mới để lần AddSample kế tiếp tiếp tục đếm đúng, không
        /// bị nhảy số so với các dòng vừa bị xóa.
        /// </summary>
        private void RenumberSamples()
        {
            var countByFg = new Dictionary<string, int>();
            foreach (var sample in _samples)
            {
                var fgCode = sample.C013 ?? string.Empty;
                if (!countByFg.ContainsKey(fgCode)) countByFg[fgCode] = 0;
                sample.C009 = ++countByFg[fgCode];
            }

            _sampleCounters.Clear();
            foreach (var kv in countByFg) _sampleCounters[kv.Key] = kv.Value;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  CONFIRM — insert SAMPLING LIST vào FT600 (không update FT601.C017)
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

                var insert = _samples.Where(x => x.AllowScale && x.C021 > 0).ToList();
                if (insert.Count == 0)
                {
                    System.Windows.Forms.MessageBox.Show("No weighed sample to confirm.", "Warning",
                        (MessageBoxButtons)MessageBoxButton.OK, (MessageBoxIcon)MessageBoxImage.Warning);
                    return;
                }

                var now = DateTime.Now;
                var machine = Environment.MachineName;
                insert.ForEach(x =>
                {
                    x.C010 = _operatorInfo.C000;
                    x.C011 = _operatorInfo.C001;
                    x.CreatedDate = now;
                    x.CreatedMachine = machine;
                    x.Mesocomp = _mesocomp;
                    x.Mesoyear = _mesoYear;
                });

                // Ghi chú: khác với ShotWeightViewModel.ExecuteConfirmAsync, KHÔNG update FT601.C017
                // ("step already produced") — FG scan không tương ứng với việc "step đã hoàn thành".
                await db.FT600s.AddRangeAsync(insert);
                await db.SaveChangesAsync();
                await transaction.CommitAsync();

                System.Windows.Forms.MessageBox.Show("Scale shot weight saved successfully.", "Information",
                    (MessageBoxButtons)MessageBoxButton.OK, (MessageBoxIcon)MessageBoxImage.Information);

                _labelInfo = new FT606_Label();
                ClearFgComboAction?.Invoke();
                ClearBarcodeAction?.Invoke();

                ResetSession();
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

        // ─────────────────────────────────────────────────────────────────────
        //  CANCEL — discard toàn bộ SAMPLING LIST chưa confirm của session hiện tại
        // ─────────────────────────────────────────────────────────────────────
        private void ExecuteCancel(object? _)
        {
            _labelInfo = new FT606_Label();

            ClearFgComboAction?.Invoke();
            ClearBarcodeAction?.Invoke();
            ResetSession();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  RESET SESSION
        //  Xóa toàn bộ sample chưa confirm + reset counter theo FG code (theo quyết định:
        //  counter reset khi Confirm thành công; Cancel cũng discard toàn bộ batch nên reset
        //  tương tự để tránh SampleNum tiếp tục đếm cho các dòng đã bị xóa).
        // ─────────────────────────────────────────────────────────────────────
        private void ResetSession()
        {
            _samples = new List<FT600>();
            _sampleCounters.Clear();
            _rowSelected = null;
            _stdRow = new FT600();
            _refHistory = new List<FT600>();

            FgName = FgCode = Machine = Size = SampleNum = null;
            Remark = string.Empty;

            HistoryToggleTextDetail = HistoryToggleTextSecond = string.Empty;
            HistoryCollection.Clear();

            RefreshUI();
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
        //  REFERENCE / HISTORY (theo FG code, khác ShotWeightViewModel theo step code)
        // ─────────────────────────────────────────────────────────────────────
        private async Task LoadRefHistoryAsync(FT600 row)
        {
            try
            {
                if (row == null) return;

                using var db = _dbFactory.CreateDbContext();
                _refHistory = await db.FT600s
                    .Where(x => x.C013 == row.C013)
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

                    HistoryToggleTextDetail = $"Last {_refHistory.Count} weighings  ·  {row.C013}  ·  {row.C014}";
                    HistoryToggleText = "REFERENCE / HISTORY";
                    HistoryToggleTextSecond = $"{row.C014}";
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
        //  REFERENCE PANEL UPDATE — vẫn giữ layout 4 dòng như màn hình chính (theo
        //  quyết định: giữ 3 cột weight riêng biệt để khớp layout main window).
        //  Từ khi đổi sang công thức 2-lần-cân, Total Weight/Total Part/Part/Runner
        //  không còn luôn bằng nhau/luôn = 0 nữa — xem ExecuteSave().
        // ─────────────────────────────────────────────────────────────────────
        private void UpdateReferencePanel()
        {
            _referenceRows = new List<ReferenceRow>
            {
                new() { No=1, FieldName="Total Weight",                Unit="g",
                        Std    = _stdRow?.C024 > 0 ? _stdRow.C024 : null,
                        Actual = _rowSelected?.C024 > 0 ? _rowSelected.C024 : null },
                new() { No=2, FieldName="Total Part Weight",           Unit="g",
                        Std    = _stdRow?.C023 > 0 ? _stdRow.C023 : null,
                        Actual = _rowSelected?.C023 > 0 ? _rowSelected.C023 : null },
                new() { No=3, FieldName="Part Weight",                 Unit="g",
                        Std    = _stdRow?.C021 > 0 ? _stdRow.C021 : null,
                        Actual = _rowSelected?.C021 > 0 ? _rowSelected.C021 : null },
                new() { No=4, FieldName="Runner / Excess Mat. Weight", Unit="g",
                        Std    = _stdRow?.C022 > 0 ? _stdRow.C022 : null,
                        Actual = _rowSelected?.C022 > 0 ? _rowSelected.C022 : null }
            };

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                RefRowsCollection.Clear();
                foreach (var row in _referenceRows)
                    RefRowsCollection.Add(row);

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
                    ToleranceCategory.Ok => new SolidColorBrush(Color.FromRgb(212, 247, 220)),
                    ToleranceCategory.Warn => new SolidColorBrush(Color.FromRgb(255, 243, 205)),
                    ToleranceCategory.Err => new SolidColorBrush(Color.FromRgb(253, 232, 232)),
                    _ => new SolidColorBrush(Color.FromRgb(245, 245, 245))
                };
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        //  UI REFRESH
        // ─────────────────────────────────────────────────────────────────────
        private void RefreshUI()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                // FG info panel
                FgName = _rowSelected?.C014;
                FgCode = _rowSelected?.C013;
                Machine = _rowSelected?.C004;
                Size = _rowSelected?.C008;
                SampleNum = _rowSelected?.C009?.ToString();
                // Hiển thị remark của đúng sample đang chọn — không phải 1 giá trị dùng chung.
                Remark = _rowSelected?.C038;

                UpdateSampleStatuses();
                RefreshSamplesGridCore();

                UpdateReferencePanel();
            });
        }

        /// <summary>
        /// Ép DataGrid SAMPLING LIST rebind lại toàn bộ row từ _samples. FT600 không implement
        /// INotifyPropertyChanged nên việc sửa trực tiếp 1 field (VD C038 khi gõ Remark) không tự
        /// đẩy lên UI — phải Clear + Add lại ObservableCollection để WPF render lại cell mới nhất.
        /// Luôn chạy trên UI thread (Dispatcher.Invoke), an toàn để gọi từ bất kỳ thread nào.
        /// </summary>
        private void RefreshSamplesGrid()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(RefreshSamplesGridCore);
        }

        /// <summary>Phần lõi (không tự Dispatcher.Invoke) dùng chung bởi RefreshUI() (đã ở trong
        /// Dispatcher.Invoke) và RefreshSamplesGrid() (public entry, tự lo Dispatcher).</summary>
        private void RefreshSamplesGridCore()
        {
            SamplesCollection.Clear();
            foreach (var item in _samples)
                SamplesCollection.Add(item);
        }

        private void UpdateSampleStatuses()
        {
            foreach (var item in _samples)
            {
                bool isDone = item.C021 > 0;
                bool isActive = item == _rowSelected;

                item.StatusText = isDone ? "Done" : isActive ? "Active" : "Pending";
                item.StatusDotColor = isDone ? "#4CAF50" : isActive ? "#1976D2" : "#9E9E9E";
                item.StatusBarColor = isActive ? "#1565C0" : isDone ? "#A5D6A7" : "Transparent";
            }
        }
    }
}
