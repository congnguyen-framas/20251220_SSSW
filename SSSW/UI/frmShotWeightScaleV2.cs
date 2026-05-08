// ============================================================================
//  frmShotWeightScaleV2.cs
//  Shotweight Station – Option A "Compact split grid" redesign
//  Exact same backend logic as frmShotWeightScale; only the right-panel UI
//  is new (Reference Values 5-col grid + Scale Value card + History panel).
// ============================================================================
using AutoUpdaterDotNET;
using DevExpress.Utils;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraSplashScreen;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ScanAndScale.Helper;
using SSSW.models;
using SSSW.modelss;
using System.Runtime.InteropServices;
using ColumnFilterPopupMode = DevExpress.XtraGrid.Columns.ColumnFilterPopupMode;

namespace SSSW
{
    public partial class frmShotWeightScaleV2 : Form
    {
        // ─── Win32 for dragging borderless form ───────────────────────────────────
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        // ─── Title-bar buttons (built in constructor, same pattern as original) ──
        private Panel  titleBar;
        private Label  titleText;
        private Button btnClose;
        private Button btnMaximize;
        private Button btnMinimize;
        private Button btnUpdateVersion;
        private Button btnReload;
        private Button btnViewHistoryScale;
        private Button btnGetDataHydra;
        private Button btnDisableSize;

        // ─── Option-A specific: ReferenceRow model ───────────────────────────────
        /// <summary>Tolerance category used for color-coding the reference grid.</summary>
        private enum ToleranceCategory { Idle, Ok, Warn, Err }

        /// <summary>One row in the 5-col Reference Values grid.</summary>
        private class ReferenceRow
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

        // ─── Option-A specific fields ─────────────────────────────────────────────
        private List<ReferenceRow> _referenceRows = new();
        private FT600              _stdRow         = new();       // last recorded weighing (= Std)
        private List<FT600>        _refHistory     = new();       // last 5 weighings for history grid
        private bool               _historyExpanded = false;

        // ─── Domain data (identical to original) ─────────────────────────────────
        public  List<FT601>            _dataHydra                  { get; set; } = new();
        private List<FT601>            _dataHydraMultiSizeOfMold   { get; set; } = new();
        private bool                   _newScale                   = true;
        public  FT601                  _stepItemCodeScale          { get; set; } = new FT601();
        private bool                   isUpdateClicked             = false;

        private List<BomWinlineModel>  _allStepsFG                 = new();
        private List<FT600>            _scaledDataPreviousStep     = new();
        private List<FT600>            _scaleData                  = new();
        private List<FT600>            _scaleDataFinal             = new();
        private FT600                  _rowSelected                = new();

        private double                 _scaleValue                 = 0;
        private string                 _mesocomp                   = string.Empty;
        private int                    _mesoYear                   = 0;

        private RepositoryItemButtonEdit _buttonEdit               = new RepositoryItemButtonEdit();

        private bool                   _isRunner                   { get; set; } = true;
        private double?                _articlePaisShotFinaly      = 0;
        private double                 _percentOfUsage             = 0;
        private string?                _remarkFinal                = string.Empty;
        private List<StepSelectModel>  _allStepCodeMaster          = new();

        public  FT601                  _stepSelected               = new FT601();
        private string                 _qrCodeScan                 = string.Empty;
        private FT606_Label            _labelInfo                  = new FT606_Label();

        private string                 _employeeCode               = string.Empty;
        private string                 _employeeName               = string.Empty;
        private FT029_Operator_RFID    _operatorInfo               = new FT029_Operator_RFID();
        private List<HydraItemDetailModel> _hydraItemDetails       = new();

        // ─── DI services ─────────────────────────────────────────────────────────
        private readonly IServiceProvider             _serviceProvider;
        private readonly IDbContextFactory<DbContextDogeWH> _dbFactory;
        private readonly ILogger<frmShotWeightScaleV2> _logger;

        private CancellationTokenSource _loadCts;

        // Small "cancel load" button (kept from original)
        private DevExpress.XtraEditors.SimpleButton btnCancelLoad;
        private bool _allowPartitionAdjustment = false;
        private bool _suppress                 = false;

        // =========================================================================
        //  CONSTRUCTORS
        // =========================================================================
        public frmShotWeightScaleV2(
            IDbContextFactory<DbContextDogeWH> dbFactory,
            IServiceProvider serviceProvider,
            ILogger<frmShotWeightScaleV2> logger) : this()
        {
            _dbFactory       = dbFactory;
            _serviceProvider = serviceProvider;
            _logger          = logger;
        }

        public frmShotWeightScaleV2()
        {
            InitializeComponent();
            BuildTitleBar();
            Load         += FrmShotWeightScaleV2_Load;
            FormClosing  += FrmShotWeightScaleV2_FormClosing;
        }

        // =========================================================================
        //  TITLE BAR  (identical pattern to original)
        // =========================================================================
        private void BuildTitleBar()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition   = FormStartPosition.CenterScreen;
            this.Size            = new Size(1920, 1080);

            titleBar           = new Panel();
            titleBar.Dock      = DockStyle.Top;
            titleBar.Height    = 40;
            titleBar.BackColor = Color.Black;
            titleBar.MouseDown += TitleBar_MouseDown;
            this.Controls.Add(titleBar);

            // helper to create icon buttons
            Button MakeBtn(Image img, int xFromRight, EventHandler click, string tooltip)
            {
                var b = new Button
                {
                    Text              = "",
                    ForeColor         = Color.White,
                    BackColor         = Color.Black,
                    FlatStyle         = FlatStyle.Flat,
                    Size              = new Size(30, 30),
                    Anchor            = AnchorStyles.Top | AnchorStyles.Right,
                    Cursor            = Cursors.Hand,
                    Image             = img,
                    ImageAlign        = ContentAlignment.MiddleCenter,
                    TextImageRelation = TextImageRelation.Overlay,
                    Padding           = new Padding(0)
                };
                b.FlatAppearance.BorderSize = 0;
                b.Location    = new Point(this.Width - xFromRight, 5);
                b.Click      += click;
                b.MouseEnter += (s, e) => b.BackColor = Color.FromArgb(60, 90, 200);
                b.MouseLeave += (s, e) => b.BackColor = Color.Black;
                var tip = new ToolTip { AutoPopDelay = 3000, InitialDelay = 300, ShowAlways = true };
                tip.SetToolTip(b, tooltip);
                return b;
            }

            btnClose            = MakeBtn(Properties.Resources.close_white_30,            40,  BtnClose_Click,            "Close");
            btnMaximize         = MakeBtn(Properties.Resources.maximize_white_30,          80,  BtnMaximize_Click,         "Maximize / Restore");
            btnMinimize         = MakeBtn(Properties.Resources.minimize_white_30,          120, BtnMinimize_Click,         "Minimize");
            btnUpdateVersion    = MakeBtn(Properties.Resources.arrow_upward_white_30,      160, BtnUpdateVersion_Click,    "Check for updates");
            btnReload           = MakeBtn(Properties.Resources.reload_white_30,            200, async (s, e) => await btnReload_ClickAsync(s, e),           "Reload master data");
            btnViewHistoryScale = MakeBtn(Properties.Resources.activity_history_30_White,  240, async (s, e) => await btnViewHistoryScale_ClickAsync(s, e), "View scale history");
            btnGetDataHydra     = MakeBtn(Properties.Resources.icons8_big_data_30_white,   280, async (s, e) => await btnGetDataHydra_ClickAsync(s, e),     "Get data from Hydra");
            btnDisableSize      = MakeBtn(Properties.Resources.icons8_installing_updates_30_white, 320, async (s, e) => await btnDisableSize_ClickAsync(s, e), "Manage master data");

            foreach (var b in new[] { btnClose, btnMaximize, btnMinimize, btnUpdateVersion,
                                      btnReload, btnViewHistoryScale, btnGetDataHydra, btnDisableSize })
                titleBar.Controls.Add(b);

            // Logo
            var logo = new PictureBox
            {
                Image    = Properties.Resources.framas_white,
                SizeMode = PictureBoxSizeMode.Zoom,
                Size     = new Size(100, 30),
                Location = new Point(10, 5)
            };
            titleBar.Controls.Add(logo);

            // Title text
            titleText = new Label
            {
                Text      = $"fT – SSSW Station  ver:{Application.ProductVersion}  |  Option A",
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 11, FontStyle.Bold),
                AutoSize  = true,
                Location  = new Point(120, 10)
            };
            titleBar.Controls.Add(titleText);
        }

        // =========================================================================
        //  FORM LOAD
        // =========================================================================
        private async void FrmShotWeightScaleV2_Load(object? sender, EventArgs e)
        {
            AutoUpdater.RunUpdateAsAdmin = false;
            AutoUpdater.DownloadPath     = Environment.CurrentDirectory;
            AutoUpdater.ApplicationExitEvent  += AutoUpdater_ApplicationExitEvent;
            AutoUpdater.CheckForUpdateEvent   += AutoUpdater_CheckForUpdateEvent;

            _txtActiclePairShot.Focus();

            using var dbContext = _dbFactory.CreateDbContext();
            _mesocomp = dbContext.Database.SqlQueryRaw<string>("sp_MaterialGetCompanyName").AsEnumerable().FirstOrDefault();
            _mesoYear = dbContext.Database.SqlQueryRaw<int>("sp_MaterialGetMesoyear").AsEnumerable().FirstOrDefault();

            var location = _mesocomp switch
            {
                "VNT1" => "fVN", "FKV"  => "fKV", "FTT1" => "fFT",
                "05FI" => "fIN", "fGE"  => "fGE", _      => "Unknown"
            };

            if (Enum.TryParse<EnumLocation>(location, true, out var loc))
                titleText.Text = $"{loc} – SSSW Station  |  Option A";

            _labVer.Text = Application.ProductVersion.Split('+')[0];

            // Config
            var configData = await dbContext.FT608s.FirstOrDefaultAsync(x => x.c000 == Environment.MachineName);
            if (configData != null)
            {
                GlobalVariable.ConfigSystem = JsonConvert.DeserializeObject<ConfigModel>(configData.c001);
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

            await LoadDataAsync(this, TimeSpan.FromSeconds(30));

            // ─── Total Steps grid options ─────────────────────────────────────────
            _grvTotalStep.OptionsView.ShowAutoFilterRow    = true;
            _grvTotalStep.OptionsCustomization.AllowFilter = true;
            _grvTotalStep.OptionsView.ShowFilterPanelMode  = DevExpress.XtraGrid.Views.Base.ShowFilterPanelMode.ShowAlways;
            _grvTotalStep.OptionsView.ColumnAutoWidth      = false;
            _grvTotalStep.OptionsCustomization.AllowSort   = true;
            _grvTotalStep.OptionsBehavior.ReadOnly         = true;
            _grvTotalStep.OptionsView.ShowFooter           = true;
            _grvTotalStep.OptionsView.ShowGroupPanel       = true;
            _grvTotalStep.OptionsFind.AlwaysVisible        = true;

            // ─── Hardware config ──────────────────────────────────────────────────
            _scanBarcode.Config              = GlobalVariable.ConfigSystem.Scanner;
            _txtRFIDCode.Config              = GlobalVariable.ConfigSystem.RFID;
            scaleButtonEdit1.Config          = GlobalVariable.ConfigSystem.Scale;
            scaleButtonEdit1.EnableReadScale = (bool)GlobalVariable.ConfigSystem.EnableReadScale;

            // ─── Wire events ──────────────────────────────────────────────────────
            _scanBarcode.DataValueChanged    += _scanBarcode_DataValueChanged;
            _txtRFIDCode.DataValueChanged    += _txtRFIDCode_DataValueChanged;
            _txtRFIDName.KeyDown             += async (s, a) => await _txtRFIDName_KeyDownAsync(s, a);
            scaleButtonEdit1.DataValueChanged += scaleButtonEdit1_DataValueChanged;
            _btnSaveWeight.Click             += _btnSaveWeight_Click;
            _btnConfirm.Click                += async (s, a) => await _btnConfirm_Click(s, a);
            _btnCancel.Click                 += _btnCancel_Click;

            _txtActiclePairShot.KeyDown += (s, ev) =>
            {
                if (ev.KeyCode != Keys.Enter) return;
                _articlePaisShotFinaly = double.TryParse(_txtActiclePairShot.EditValue?.ToString(), out var v) ? v : 0;
                var row = _scaleDataFinal.FirstOrDefault(x => x.C032 == _rowSelected.C032);
                if (row != null) row.C028 = _articlePaisShotFinaly;
                GlobalVariable.InvokeIfRequired(this, () => _grvTotalStep.RefreshData());
            };

            _txtPercentOFusageNonwoven.KeyDown += (s, ev) =>
            {
                if (ev.KeyCode != Keys.Enter) return;
                var catCheck = GlobalVariable.ConfigSystem.CategoryOfNonInjectionUsagePartial
                    .FirstOrDefault(x => x.CategoryCode == _rowSelected.C033);
                if (!(_rowSelected?.C002?.StartsWith("REX") ?? false) ||
                    ((_rowSelected?.C002?.StartsWith("REX") ?? false) && catCheck == null)) return;

                _percentOfUsage = double.TryParse(_txtPercentOFusageNonwoven.EditValue?.ToString(), out var v) ? v : 0;
                _scaleDataFinal?.Where(x => x.C002.StartsWith("REX")).ToList().ForEach(x =>
                {
                    var usage   = x.C035 == 100 ?
                        (double)Math.Round((decimal)(_rowSelected.C024 * _percentOfUsage / 100), 3) :
                        (double)Math.Round((decimal)((decimal)(_rowSelected.C024 * _percentOfUsage / 100) / (decimal)x.C028), 3);
                    var unusage = _rowSelected.C035 == 100 ?
                        _rowSelected.C024 - usage :
                        (_rowSelected.C024 - usage * x.C028) / x.C028;
                    x.C035 = _percentOfUsage;
                    x.C023 = usage;
                    x.C021 = usage;
                    x.C022 = unusage;
                });
                UpdateUI(refresh: true);
            };

            _txtPercentOFusageNonwoven.EditValue = GlobalVariable.ConfigSystem.PercentOfUserNonWoven;
            _percentOfUsage = GlobalVariable.ConfigSystem.PercentOfUserNonWoven;

            _txtRemark.EditValueChanged += (s, ev) =>
            {
                _remarkFinal = _txtRemark.EditValue?.ToString() ?? string.Empty;
                _scaleDataFinal?.ForEach(x => x.C038 = _remarkFinal);
            };

            _comboBoxEditIsRunner.Properties.Items.AddRange(new[] { "YES", "NO" });
            _comboBoxEditIsRunner.SelectedIndex = 0;

            _buttonEdit.ButtonClick += _buttonEdit_ButtonClick;
            _grvTotalStep.RowStyle  += _grvTotalStep_RowStyle;

            _rowSelected = _scaleDataFinal.FirstOrDefault(x =>
                x.C002 == _stepItemCodeScale.C004 && x.C015 == _stepItemCodeScale.C010);

            _lkStepCode.Properties.DataSource = _allStepCodeMaster;
            InitGridLookUpEdit();
            _lkStepCode.ButtonClick += Properties_ButtonClick;

            _txtPercentOFusageNonwoven.Enabled = false;

            _toggleSwitchEnablePartition.EditValueChanged += (s, ev) =>
            {
                _allowPartitionAdjustment   = (bool)_toggleSwitchEnablePartition.EditValue;
                _txtActiclePairShot.Enabled = _allowPartitionAdjustment;
                _txtActiclePairShot.BackColor = _allowPartitionAdjustment
                    ? Color.White : Color.FromArgb(240, 240, 240);
            };

            // ─── Initialize NEW right-panel grids ─────────────────────────────────
            InitRefGrid();
            InitHistoryGrid();
        }

        // =========================================================================
        //  OPTION A – Reference Values grid setup
        // =========================================================================

        /// <summary>Configure columns for the 5-col Reference Values grid.</summary>
        private void InitRefGrid()
        {
            _grvRefValues.Columns.Clear();

            var colNo = _grvRefValues.Columns.AddField(nameof(ReferenceRow.No));
            colNo.Caption = "#";
            colNo.Visible = true;
            colNo.Width   = 40;
            colNo.OptionsColumn.AllowEdit = false;

            var colName = _grvRefValues.Columns.AddField(nameof(ReferenceRow.FieldName));
            colName.Caption = "Field (Unit)";
            colName.Visible = true;
            colName.Width   = 195;
            colName.OptionsColumn.AllowEdit = false;

            var colStd = _grvRefValues.Columns.AddField(nameof(ReferenceRow.StdDisplay));
            colStd.Caption = "STD";
            colStd.Visible = true;
            colStd.Width   = 110;
            colStd.AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            colStd.AppearanceCell.TextOptions.HAlignment   = DevExpress.Utils.HorzAlignment.Center;
            colStd.OptionsColumn.AllowEdit = false;

            var colDelta = _grvRefValues.Columns.AddField(nameof(ReferenceRow.DeltaDisplay));
            colDelta.Caption = "Δ";
            colDelta.Visible = true;
            colDelta.Width   = 90;
            colDelta.AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            colDelta.AppearanceCell.TextOptions.HAlignment   = DevExpress.Utils.HorzAlignment.Center;
            colDelta.OptionsColumn.AllowEdit = false;

            var colActual = _grvRefValues.Columns.AddField(nameof(ReferenceRow.ActualDisplay));
            colActual.Caption = "ACTUAL";
            colActual.Visible = true;
            colActual.Width   = 110;
            colActual.AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            colActual.AppearanceCell.TextOptions.HAlignment   = DevExpress.Utils.HorzAlignment.Center;
            colActual.OptionsColumn.AllowEdit = false;

            // Font sizes for the ref grid
            _grvRefValues.Appearance.Row.Font         = new Font("Segoe UI", 13F);
            _grvRefValues.Appearance.HeaderPanel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            _grvRefValues.RowHeight = 68;
            _grvRefValues.OptionsView.ShowAutoFilterRow = false;

            _grvRefValues.RowCellStyle += GrvRefValues_RowCellStyle;

            // Initial empty data
            UpdateReferencePanel();
        }

        /// <summary>Configure columns for the History grid.</summary>
        private void InitHistoryGrid()
        {
            _grvHistory.Columns.Clear();

            void AddCol(string field, string caption, int width, HorzAlignment align = HorzAlignment.Near)
            {
                var c = _grvHistory.Columns.AddField(field);
                c.Caption = caption;
                c.Visible = true;
                c.Width   = width;
                c.AppearanceHeader.TextOptions.HAlignment = align;
                c.AppearanceCell.TextOptions.HAlignment   = align;
                c.OptionsColumn.AllowEdit = false;
            }

            AddCol("CreateDate",   "Date / Time",       140);
            AddCol(nameof(FT600.C011), "Operator",       90);
            AddCol(nameof(FT600.C024), "Total W (g)",   100, HorzAlignment.Far);
            AddCol(nameof(FT600.C023), "Total PW (g/p)", 110, HorzAlignment.Far);
            AddCol(nameof(FT600.C021), "Part W (g/p)",  100, HorzAlignment.Far);
            AddCol(nameof(FT600.C022), "Runner (g/p)",  100, HorzAlignment.Far);

            _grvHistory.Appearance.Row.Font         = new Font("Segoe UI", 10F);
            _grvHistory.Appearance.HeaderPanel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        }

        // ─── Color logic  ≤±1% green · ±1-3% amber · >±3% red ───────────────────
        private static (Color back, Color fore) GetToleranceColors(ToleranceCategory cat) => cat switch
        {
            ToleranceCategory.Ok   => (Color.FromArgb(212, 247, 220), Color.FromArgb(0, 100, 0)),
            ToleranceCategory.Warn => (Color.FromArgb(255, 243, 205), Color.FromArgb(130, 80,  0)),
            ToleranceCategory.Err  => (Color.FromArgb(253, 232, 232), Color.FromArgb(160, 0,   0)),
            _                      => (Color.FromArgb(245, 245, 245), Color.FromArgb(80, 80, 80))
        };

        private void GrvRefValues_RowCellStyle(object sender, RowCellStyleEventArgs e)
        {
            var row = _grvRefValues.GetRow(e.RowHandle) as ReferenceRow;
            if (row == null) return;

            var (back, fore) = GetToleranceColors(row.Tolerance);

            var tintedFields = new[]
            {
                nameof(ReferenceRow.StdDisplay),
                nameof(ReferenceRow.DeltaDisplay),
                nameof(ReferenceRow.ActualDisplay)
            };

            if (tintedFields.Contains(e.Column.FieldName))
            {
                e.Appearance.BackColor = back;
                e.Appearance.ForeColor = fore;
                e.Appearance.Font      = new Font("Segoe UI", 14F, FontStyle.Bold);
            }
            else
            {
                e.Appearance.Font = new Font("Segoe UI", 11F);
            }
        }

        // ─── Update the 5-col reference grid ─────────────────────────────────────
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

            GlobalVariable.InvokeIfRequired(this, () =>
            {
                _grcRefValues.DataSource = null;
                _grcRefValues.DataSource = _referenceRows;

                // Tint the scale card border to match the worst tolerance
                var worst = _referenceRows.OrderByDescending(r => (int)r.Tolerance).FirstOrDefault()?.Tolerance
                            ?? ToleranceCategory.Idle;
                var (borderColor, _) = GetToleranceColors(worst);
                pnlScaleCard.BackColor = worst == ToleranceCategory.Idle
                    ? Color.White
                    : Color.FromArgb(230, borderColor);
            });
        }

        /// <summary>Load last 5 weighings for the selected step → used as Std + History.</summary>
        private async Task LoadRefHistoryAsync(string stepCode)
        {
            try
            {
                if (string.IsNullOrEmpty(stepCode)) return;

                using var db = _dbFactory.CreateDbContext();
                _refHistory = await db.FT600s
                    .Where(x => x.C002 == stepCode)
                    .OrderByDescending(x => x.CreatedDate)
                    .Take(5)
                    .ToListAsync();

                _stdRow = _refHistory.FirstOrDefault() ?? new FT600();

                UpdateReferencePanel();

                GlobalVariable.InvokeIfRequired(this, () =>
                {
                    _grcHistory.DataSource = null;
                    _grcHistory.DataSource = _refHistory;
                    _grvHistory.BestFitColumns();

                    // show history title with step info
                    lblHistoryToggle.Text =
                        (_historyExpanded ? "▼" : "▶") +
                        $"  REFERENCE / HISTORY  ·  last {_refHistory.Count} weighings  ·  {stepCode}";
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LoadRefHistoryAsync error");
            }
        }

        // ─── History panel toggle ─────────────────────────────────────────────────
        private void PnlHistoryHeader_Click(object? sender, EventArgs e)
        {
            _historyExpanded      = !_historyExpanded;
            _grcHistory.Visible   = _historyExpanded;
            lblHistoryToggle.Text = (_historyExpanded ? "▼" : "▶") +
                lblHistoryToggle.Text.TrimStart('▼', '▶', ' ');
        }

        // =========================================================================
        //  LOAD MASTER DATA  (identical to original)
        // =========================================================================
        private void InitCancelButton()
        {
            btnCancelLoad = new DevExpress.XtraEditors.SimpleButton
            {
                Name    = "btnCancelLoad",
                Text    = "Cancel load",
                Enabled = false,
                Anchor  = AnchorStyles.Top | AnchorStyles.Right,
                Size    = new Size(90, 28),
            };
            btnCancelLoad.Location = new Point(ClientSize.Width - btnCancelLoad.Width - 10, 10);
            btnCancelLoad.Click   += (s, e) => _loadCts?.Cancel();
            Controls.Add(btnCancelLoad);
        }

        private async Task LoadDataAsync(
            Control         overlayTarget = null,
            TimeSpan?       timeout       = null,
            DevExpress.XtraEditors.SimpleButton cancelButton = null)
        {
            overlayTarget ??= this;
            timeout       ??= TimeSpan.FromSeconds(30);

            _loadCts?.Cancel();
            _loadCts?.Dispose();
            _loadCts = new CancellationTokenSource();

            using var timeoutCts = new CancellationTokenSource(timeout.Value);
            using var linkedCts  = CancellationTokenSource.CreateLinkedTokenSource(_loadCts.Token, timeoutCts.Token);
            var token = linkedCts.Token;

            IOverlaySplashScreenHandle overlay = null;
            try
            {
                overlay = SplashScreenManager.ShowOverlayForm(overlayTarget);
                if (cancelButton != null) cancelButton.Enabled = true;

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
                    StepItemCode    = x.C004,
                    StepItemName    = x.C005,
                    Size            = x.C002,
                    ArticlePairsShot = x.C013,
                    MoldPairsShot   = x.C014,
                    Machine         = x.C015,
                    HydraOrderNo    = x.C018,
                    FT601Id         = x.Id
                }).Distinct().ToList();

                GlobalVariable.InvokeIfRequired(this, () =>
                {
                    _lkStepCode.Properties.DataSource = null;
                    _lkStepCode.Properties.DataSource = _allStepCodeMaster;
                    InitGridLookUpEdit();
                });
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                XtraMessageBox.Show(this, $"Load data failure:\n{ex.Message}", "Load data",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (overlay != null) SplashScreenManager.CloseOverlayForm(overlay);
                if (cancelButton != null) cancelButton.Enabled = false;
            }
        }

        // =========================================================================
        //  EVENT HANDLERS  (identical to original)
        // =========================================================================
        private async void AutoUpdater_ApplicationExitEvent()
        {
            Text = "Closing...";
            await Task.Delay(3000);
            Application.Exit();
        }

        private async void AutoUpdater_CheckForUpdateEvent(UpdateInfoEventArgs args)
        {
            if (args.IsUpdateAvailable)
            {
                var res = MessageBox.Show(
                    $"New version available: {args.CurrentVersion}. Update now?",
                    "Update", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (res == DialogResult.Yes || res == DialogResult.OK)
                {
                    SplashScreenManager.ShowForm(typeof(WaitForm1));
                    await Task.Delay(3000);
                    try
                    {
                        if (AutoUpdater.DownloadUpdate(args))
                        { SplashScreenManager.CloseForm(false); Application.Exit(); }
                        else
                            SplashScreenManager.ShowForm(typeof(WaitForm1));
                    }
                    catch (Exception ex)
                    {
                        SplashScreenManager.CloseForm(false);
                        MessageBox.Show(ex.Message, ex.GetType().ToString(),
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else if (isUpdateClicked)
            {
                MessageBox.Show("Already up to date.", "Information",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void FrmShotWeightScaleV2_FormClosing(object sender, FormClosingEventArgs e)
        {
            _txtRFIDCode.DataValueChanged     -= _txtRFIDCode_DataValueChanged;
            scaleButtonEdit1.DataValueChanged -= scaleButtonEdit1_DataValueChanged;
            _btnSaveWeight.Click              -= _btnSaveWeight_Click;
            _btnConfirm.Click                 -= async (s, a) => await _btnConfirm_Click(s, a);
            _btnCancel.Click                  -= _btnCancel_Click;
        }

        private void TitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            ReleaseCapture();
            SendMessage(Handle, 0x112, 0xf012, 0);
        }

        private void BtnClose_Click(object sender, EventArgs e)   => Close();
        private void BtnMaximize_Click(object sender, EventArgs e) =>
            WindowState = WindowState == FormWindowState.Normal
                ? FormWindowState.Maximized : FormWindowState.Normal;
        private void BtnMinimize_Click(object sender, EventArgs e) =>
            WindowState = FormWindowState.Minimized;

        private void BtnUpdateVersion_Click(object sender, EventArgs e)
        {
            try
            {
                isUpdateClicked = true;
                SplashScreenManager.ShowForm(typeof(WaitForm1));
                System.Threading.Thread.Sleep(3000);
                AutoUpdater.Start(GlobalVariable.ConfigSystem.UpdatePath);
            }
            catch (Exception ex) { XtraMessageBox.Show($"{ex.Message}", "Error"); }
            finally { SplashScreenManager.CloseForm(false); }
        }

        private async Task btnReload_ClickAsync(object sender, EventArgs e) =>
            await LoadDataAsync(this, TimeSpan.FromSeconds(30));

        private async Task btnViewHistoryScale_ClickAsync(object sender, EventArgs e)
        {
            var nf = _serviceProvider.GetRequiredService<frmMainView>();
            nf.StartPosition = FormStartPosition.CenterParent;
            nf.WindowState   = FormWindowState.Maximized;
            nf.ShowDialog(this);
        }

        private async Task btnGetDataHydra_ClickAsync(object sender, EventArgs e) =>
            await GetDataHydra();

        private async Task btnDisableSize_ClickAsync(object sender, EventArgs e)
        {
            var nf = _serviceProvider.GetRequiredService<frmUpdateMasterData>();
            nf.StartPosition = FormStartPosition.CenterParent;
            nf.WindowState   = FormWindowState.Maximized;
            nf.ShowDialog(this);
            await LoadDataAsync(this, TimeSpan.FromSeconds(30));
        }

        // ─── Barcode scan ─────────────────────────────────────────────────────────
        private async void _scanBarcode_DataValueChanged(object? sender, DataValueChangedEventArgs e)
        {
            try
            {
                _qrCodeScan = e.NewValue.Value.ToString();
                using var db = _dbFactory.CreateDbContext();
                _labelInfo   = new FT606_Label();
                _labelInfo   = await db.FT606s.FirstOrDefaultAsync(x => x.c001 == _qrCodeScan);

                if (_labelInfo == null)
                    throw new Exception("Label information not found.");

                _stepSelected = _dataHydra.FirstOrDefault(x => x.Id == _labelInfo.c000);
                if (_stepSelected == null)
                    throw new Exception("Step information not found.");

                FilterLookup(_stepSelected.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Barcode scan error");
                MessageBox.Show(ex.Message, "WARNING", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally { Focus(); }
        }

        private void FilterLookup(Guid id)
        {
            var data = (List<StepSelectModel>)_lkStepCode.Properties.DataSource;
            var item = data.FirstOrDefault(x => x.FT601Id == id);
            if (item != null)
            {
                var code = item.StepItemCode?.Trim();
                if (Equals(_lkStepCode.EditValue, code)) _lkStepCode.EditValue = null;
                _lkStepCode.EditValue = code;
            }
            else
                MessageBox.Show("No matching data found!");
        }

        // ─── GridLookUpEdit init ──────────────────────────────────────────────────
        private void InitGridLookUpEdit()
        {
            _lkStepCode.Properties.ValueMember    = nameof(StepSelectModel.StepItemCode);
            _lkStepCode.Properties.DisplayMember  = nameof(StepSelectModel.StepItemName);
            _lkStepCode.Properties.ImmediatePopup = true;
            _lkStepCode.Properties.TextEditStyle  = DevExpress.XtraEditors.Controls.TextEditStyles.Standard;
            _lkStepCode.Properties.PopupFilterMode = PopupFilterMode.Contains;

            var view = _lkStepCode.Properties.PopupView as GridView;
            if (view == null) return;

            _lkStepCode.Popup  -= Lk_Popup;
            _lkStepCode.Popup  += Lk_Popup;
            _lkStepCode.CloseUp += (s, e) => _suppress = false;
            _lkStepCode.EditValueChanged -= Lk_EditValueChangedAsyncSafe;
            _lkStepCode.EditValueChanged += Lk_EditValueChangedAsyncSafe;

            view.OptionsView.ShowAutoFilterRow      = true;
            view.OptionsCustomization.AllowFilter   = true;
            view.OptionsView.ShowFilterPanelMode    = DevExpress.XtraGrid.Views.Base.ShowFilterPanelMode.ShowAlways;
            view.OptionsView.ColumnAutoWidth        = false;
            view.OptionsCustomization.AllowSort     = true;
            view.OptionsBehavior.ReadOnly           = true;
            view.OptionsView.ShowGroupPanel         = false;
            view.OptionsFind.AlwaysVisible          = true;
            view.OptionsFilter.ColumnFilterPopupMode = ColumnFilterPopupMode.Excel;
            view.OptionsView.BestFitMode            = GridBestFitMode.Default;
            view.OptionsFind.FindNullPrompt         = "Search…";
            view.OptionsFilter.ShowAllTableValuesInFilterPopup = true;
            view.OptionsFilter.DefaultFilterEditorView = DevExpress.XtraEditors.FilterEditorViewMode.Visual;
            view.OptionsCustomization.AllowColumnMoving = true;
            view.OptionsCustomization.AllowGroup    = false;

            view.Columns.Clear();
            view.Columns.AddVisible(nameof(StepSelectModel.StepItemCode), "Step Item Code");
            view.Columns.AddVisible(nameof(StepSelectModel.StepItemName), "Step Item Name");
            view.Columns.AddVisible(nameof(StepSelectModel.Size),         "Size");
            view.Columns.AddVisible(nameof(StepSelectModel.Machine),      "Machine");
            view.Columns.AddVisible(nameof(StepSelectModel.HydraOrderNo), "Hydra Order No");
            view.Columns.AddVisible(nameof(StepSelectModel.MoldPairsShot), "Mold Pairs Shot");
            view.Columns.AddVisible(nameof(StepSelectModel.ArticlePairsShot), "Article Pairs Shot");
            view.Columns.AddVisible(nameof(StepSelectModel.FT601Id),      "FT601 Id");

            foreach (GridColumn col in view.Columns)
                if (col.ColumnType == typeof(string))
                    col.OptionsFilter.AutoFilterCondition = AutoFilterCondition.Contains;
        }

        private void Lk_Popup(object sender, EventArgs e)
        {
            _suppress = true;
            try
            {
                var edit = (DevExpress.XtraEditors.GridLookUpEdit)sender;
                var view = edit.Properties.PopupView as GridView;
                if (view == null) return;
                view.OptionsView.ColumnAutoWidth = false;
                view.BestFitColumns();
            }
            finally { _suppress = false; }
        }

        private void Properties_ButtonClick(object sender, ButtonPressedEventArgs e)
        {
            var edit = (DevExpress.XtraEditors.GridLookUpEdit)sender;
            if (e.Button.Kind == ButtonPredefines.Delete ||
                string.Equals(e.Button.Caption, "Delete", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    edit.EditValueChanged -= Lk_EditValueChangedAsyncSafe;
                    edit.ClosePopup();
                    edit.Properties.AllowNullInput = DevExpress.Utils.DefaultBoolean.True;
                    edit.Properties.NullText       = string.Empty;
                    if (edit.Properties.PopupView is GridView v)
                    {
                        v.ClearSelection();
                        v.FocusedRowHandle = DevExpress.XtraGrid.GridControl.InvalidRowHandle;
                    }
                    edit.EditValue = null;
                    Lk_EditValueChangedAsyncSafe(edit, EventArgs.Empty);
                }
                finally { edit.EditValueChanged += Lk_EditValueChangedAsyncSafe; }
            }
        }

        private async void Lk_EditValueChangedAsyncSafe(object sender, EventArgs e)
        {
            try
            {
                if (_suppress) return;
                await _lkStepCode_EditValueChangedAsync(sender, e);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                MessageBox.Show(ex.Message);
            }
        }

        private async Task _lkStepCode_EditValueChangedAsync(object sender, EventArgs e)
        {
            try
            {
                GlobalVariable.InvokeIfRequired(this, () =>
                    _toggleSwitchEnablePartition.EditValue = false);

                var editor = sender as DevExpress.XtraEditors.GridLookUpEdit;
                if (editor == null) return;

                var selected = (StepSelectModel)editor.GetSelectedDataRow();
                if (selected == null)
                {
                    _labelInfo = new FT606_Label();
                    ResetNewLoop();
                    return;
                }

                if (string.IsNullOrEmpty(_qrCodeScan))
                {
                    _stepSelected = _dataHydra.FirstOrDefault(x =>
                        x.C004 == selected.StepItemCode &&
                        x.C015 == selected.Machine &&
                        x.C018 == selected.HydraOrderNo);

                    if (_stepSelected == null)
                        throw new Exception("Step not found in master data.");
                }

                await GetDataAsync(_stepSelected);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "_lkStepCode_EditValueChangedAsync");
                MessageBox.Show(ex.Message, "WARNING", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // ─── Reset ────────────────────────────────────────────────────────────────
        private void ResetNewLoop()
        {
            _rowSelected       = new FT600();
            _allStepsFG        = new List<BomWinlineModel>();
            _stepItemCodeScale = new FT601();
            _scaleData         = new List<FT600>();
            _scaleDataFinal    = new List<FT600>();
            _newScale          = true;
            _qrCodeScan        = string.Empty;
            _percentOfUsage    = GlobalVariable.ConfigSystem.PercentOfUserNonWoven;
            _articlePaisShotFinaly = 0;
            _remarkFinal       = string.Empty;
            _stdRow            = new FT600();
            _refHistory        = new List<FT600>();

            GlobalVariable.InvokeIfRequired(this, () =>
                _comboBoxEditIsRunner.SelectedIndex = 0);

            UpdateUI(false);
            UpdateReferencePanel();
        }

        // ─── GetDataAsync (identical to original) ────────────────────────────────
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
                        .SqlQueryRaw<BomWinlineModel>("sp_getBomWinlineOfItemFG @itemFG = {0}", stepCode.C007)
                        .AsNoTracking().ToListAsync();

                    foreach (var item in _allStepsFG)
                    {
                        bool allowScale = true;
                        FT601 ckHydra  = new();

                        ckHydra = item.ItemStepCode == _stepSelected.C004
                            ? _stepSelected
                            : _dataHydra.FirstOrDefault(x => x.C004 == item.ItemStepCode && x.C007 == item.ItemFgCode);

                        if (ckHydra == null)
                        {
                            if (item.ItemStepCode != "Z-VHXXXXXX" && item.ItemStepCode.Substring(0, 3) != "REX")
                            {
                                var mc  = item.ItemFgCode.Split('-')[0];
                                var smc = item.ItemStepCode.Split('-')[1];
                                ckHydra     = _dataHydra.FirstOrDefault(x =>
                                    x.C007.Contains($"{mc}-") || x.C004.Contains($"-{smc}-"));
                                allowScale  = false;
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
                            C012  = ckHydra.Id == _labelInfo.c000 ? _labelInfo?.c001 : null,
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
                            C029  = ckHydra.Id == _labelInfo.c000 ? _labelInfo.Id : null,
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

                    // Multi-size on same mold
                    var dataSize = new List<FT600>();
                    foreach (var item in _scaleData)
                    {
                        var pfx2     = GlobalVariable.PrefixUpToSecondHyphen(item.C002);
                        var sameMolds = _dataHydra.Where(x =>
                            x.C019 == item.C020 && x.C015 == item.C004 &&
                            x.C004 != item.C002 && x.C002 != item.C008 &&
                            GlobalVariable.PrefixUpToSecondHyphen(x.C004) == pfx2 &&
                            x.C010 == item.C015).DistinctBy(x => x.C002).ToList();

                        if (!sameMolds.Any()) continue;

                        var itemList = string.Join(",", sameMolds.Select(x => x.C004));
                        var category = await db.Database
                            .SqlQueryRaw<CategoryOfItemModel>("sp_GetCategorryOfItem @ItemCode = {0}", itemList)
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

                    // Load previous scale data
                    foreach (var item in _scaleDataFinal)
                    {
                        var catCheck = GlobalVariable.ConfigSystem.CategoryOfNonInjectionUsagePartial
                            .FirstOrDefault(x => x.CategoryCode == item.C033);

                        if (item.C002.Substring(0, 3) == "REX")
                        {
                            var first        = _scaleDataFinal.Where(x => x.C015 == item.C015 && !string.IsNullOrEmpty(x.C026));
                            var paisShotFinal = catCheck == null
                                ? first.FirstOrDefault()?.C028 : first.Sum(x => x.C028);
                            item.C026  = first.FirstOrDefault()?.C026;
                            item.C027  = first.FirstOrDefault()?.C027;
                            item.C028  = paisShotFinal;
                            item.C017  = first.FirstOrDefault()?.C017 ?? 0;
                            item.C018  = first.FirstOrDefault()?.C018 ?? 0;
                        }

                        if (!item.C003.StartsWith("Stud") && !item.C003.StartsWith("Logo") &&
                            !item.C003.StartsWith("Cleat_Ring") && !item.C002.StartsWith("REX"))
                            continue;

                        FT600 stepPrevious;
                        if (item.C002 != "Z-VHXXXXXX" && item.C002.Substring(0, 3) != "REX")
                        {
                            var mainCode  = item.C002.Split('-')[1];
                            stepPrevious  = await db.FT600s
                                .Where(x => x.C015 == item.C015 &&
                                    (x.C002 == item.C002 || (x.C002.Contains(mainCode) && x.C008 == item.C008)))
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
                            _percentOfUsage = (double)stepPrevious.C035;
                            if (item.C002.StartsWith("REX"))
                            {
                                var total  = catCheck == null ? stepPrevious?.C036 * item.C025 : stepPrevious?.C036;
                                var usage  = (double)Math.Round((decimal)(total * _percentOfUsage / 100), 3);
                                var unusage = total - usage;
                                item.C021  = catCheck == null ? usage : usage / item.C028;
                                item.C022  = catCheck == null ? unusage : unusage / item.C028;
                                item.C023  = catCheck == null ? usage : usage / item.C028;
                                item.C024  = total;
                                item.C035  = stepPrevious.C035;
                                item.C036  = stepPrevious?.C036;
                            }
                            else
                            {
                                item.C021  = stepPrevious.C021;
                                item.C022  = stepPrevious.C022;
                                item.C023  = stepPrevious.C023;
                                item.C024  = stepPrevious.C024;
                                item.C028  = stepPrevious.C028;
                                item.C035  = stepPrevious.C035;
                                item.C036  = stepPrevious?.C036;
                            }
                        }
                    }

                    _scaleDataFinal = _scaleDataFinal.OrderBy(x => x.C015).ToList();

                    var stepSel = _scaleDataFinal.FirstOrDefault(x => x.C002 == _stepSelected.C004);
                    var prevSteps = _scaleDataFinal.Where(x => x.C015 < stepSel?.C015).ToList();
                    if (prevSteps.Any(x => x.C021 == 0))
                        MessageBox.Show("The previous step has not been weighed.", "Warning",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    else
                        _rowSelected = _scaleDataFinal.FirstOrDefault(x => x.C002 == _stepSelected.C004);
                }
                else
                {
                    var rowSelect = _scaleDataFinal.FirstOrDefault(x =>
                        x.C002 == _stepSelected.C004 && x.C013 == _stepSelected.C007 &&
                        x.C004 == _stepSelected.C015);

                    if (rowSelect == null)
                    {
                        MessageBox.Show($"Label does not match the item being weighed.\n{_stepSelected.C004}|{_stepSelected.C005}",
                            "WARNING", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    if (!rowSelect.AllowScale)
                    {
                        MessageBox.Show("Do not allow to scale this step.", "Warning",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    foreach (var step in _scaleDataFinal.Where(x => x.C015 < rowSelect.C015))
                    {
                        if (step.C021 == 0)
                        {
                            MessageBox.Show("The previous step has not been weighed.", "Warning",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                    rowSelect.C012 = _labelInfo?.c001;
                    rowSelect.C029 = _labelInfo?.Id;
                    _rowSelected   = rowSelect;
                }

                _articlePaisShotFinaly = _rowSelected.C028;
                UpdateUI(false);

                if (!string.IsNullOrEmpty(_rowSelected.C002))
                    FocusRowByStepCode(_grvTotalStep, "C002", _rowSelected.C002);

                // ── NEW: load history/std for the selected step ────────────────────
                await LoadRefHistoryAsync(_rowSelected.C002);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetDataAsync error");
                MessageBox.Show(ex.Message, "WARNING", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // ─── Grid action buttons ──────────────────────────────────────────────────
        private void _buttonEdit_ButtonClick(object sender, ButtonPressedEventArgs e)
        {
            var rowSelect = _grvTotalStep.GetRow(_grvTotalStep.FocusedRowHandle) as FT600;
            GlobalVariable.InvokeIfRequired(this, () =>
                _toggleSwitchEnablePartition.EditValue = false);

            if (e.Button.Index == 0) // Scale
            {
                if (rowSelect != null && !rowSelect.AllowScale)
                {
                    MessageBox.Show("Do not allow to scale this step.", "Warning",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                foreach (var step in _scaleDataFinal.Where(x => x.C015 < rowSelect.C015))
                {
                    if (step.C021 == 0)
                    {
                        MessageBox.Show("The previous step has not been weighed.", "Warning",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
                _rowSelected = rowSelect;
            }
            else if (e.Button.Index == 1) // Reset
            {
                var rowReset = _scaleDataFinal.FirstOrDefault(x => x.AllowScale && x.C002 == _rowSelected.C002);
                if (rowReset == null)
                {
                    MessageBox.Show("Cannot reset this line.", "Warning",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                rowReset.C021 = rowReset.C022 = rowReset.C023 = rowReset.C024 = 0;
                _rowSelected  = rowSelect;
            }
            else // Delete
            {
                _scaleDataFinal.Remove(rowSelect);
            }

            _articlePaisShotFinaly = _rowSelected.C017 != 0 && _rowSelected.C017 == _rowSelected.C028
                ? _rowSelected.C017 : _rowSelected.C028;
            UpdateUI(true);
            UpdateReferencePanel(); // ← NEW: refresh right panel
        }

        private void _grvTotalStep_RowStyle(object sender, RowStyleEventArgs e)
        {
            try
            {
                var view = sender as GridView;
                var data = view?.GetRow(e.RowHandle) as FT600;
                if (data != null && !data.AllowScale)
                    e.Appearance.BackColor = Color.FromArgb(173, 181, 189);

                if (view.IsRowSelected(e.RowHandle))
                {
                    e.Appearance.ForeColor = Color.Black;
                    e.Appearance.BackColor = Color.FromArgb(129, 236, 236);
                    e.HighPriority = true;
                }
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("RowStyle error: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ─── Cancel / Confirm ─────────────────────────────────────────────────────
        private void _btnCancel_Click(object sender, EventArgs e)
        {
            _labelInfo  = new FT606_Label();
            _qrCodeScan = _remarkFinal = string.Empty;

            GlobalVariable.InvokeIfRequired(this, () =>
            {
                _lkStepCode.EditValue = null;
                if (_scanBarcode != null) _scanBarcode.Text = string.Empty;
            });

            ResetNewLoop();
        }

        private async Task _btnConfirm_Click(object sender, EventArgs e)
        {
            using var db          = _dbFactory.CreateDbContext();
            using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                if (_operatorInfo == null || _operatorInfo.Id == Guid.Empty)
                {
                    MessageBox.Show("RFID card not yet scanned.", "Warning",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                        if (item.AllowScale && item.C023 == 0 && (item.C024 == 0 && item.C002.StartsWith("REX")))
                        {
                            MessageBox.Show($"Scale not completed for step: {item.C002}.", "Warning",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                }

                var now     = DateTime.Now;
                var machine = Environment.MachineName;
                var insert  = _scaleDataFinal.Where(x => x.AllowScale == true && x.C021 > 0).ToList();
                insert.ForEach(x =>
                {
                    x.C010 = _operatorInfo.C000;
                    x.C011 = _operatorInfo.C001;
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

                MessageBox.Show("Scale shot weight saved successfully.", "Information",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                _labelInfo = new FT606_Label();
                GlobalVariable.InvokeIfRequired(this, () => _lkStepCode.EditValue = null);
                ResetNewLoop();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Confirm error – transaction rolled back");
                MessageBox.Show($"Transaction error: {ex.Message}\n{ex.InnerException?.Message}",
                    "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ─── Scale value changed ──────────────────────────────────────────────────
        private void scaleButtonEdit1_DataValueChanged(object sender, DataValueChangedEventArgs e)
        {
            _scaleValue = Math.Round(Convert.ToDouble(e.NewValue.Value.ToString()), 2);
        }

        // ─── Save weight (core weighing logic – identical to original) ────────────
        private void _btnSaveWeight_Click(object sender, EventArgs e)
        {
            if (_rowSelected == null) return;
            if (!_rowSelected.AllowScale)
            {
                MessageBox.Show("Cannot scale this step.", "Warning",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!_rowSelected.C002.StartsWith("REX"))
            {
                if (_rowSelected.C024 == 0)
                    _rowSelected.C024 = _scaleValue;
                else
                {
                    _rowSelected.C023 = _scaleValue;
                    var prs = _articlePaisShotFinaly;
                    _rowSelected.C022 = _comboBoxEditIsRunner.Text == "YES"
                        ? Math.Round(((double)(_rowSelected.C024 - (_rowSelected.C023 * (double)prs)) / (double)prs), 3)
                        : 0;

                    var prev         = _scaleDataFinal.Where(x => x.C015 == _rowSelected.C015 - 1).ToList();
                    var nonInj       = _scaleDataFinal.Where(x => x.C015 == _rowSelected.C015 &&
                        (x.C002 == "Z-VHXXXXXX" || x.C002.StartsWith("REX"))).ToList();
                    _rowSelected.C021 = _rowSelected.C023 - prev?.Sum(x => x.C023) - nonInj?.Sum(x => x.C023);
                    _rowSelected.C036 = _rowSelected.C003.StartsWith("Studs") ||
                                        _rowSelected.C003.StartsWith("Logo") ||
                                        _rowSelected.C003.StartsWith("Cleat_Ring")
                        ? Math.Round(_scaleValue / (double)prs, 2) : 0;
                }
            }
            else
            {
                var catCheck = GlobalVariable.ConfigSystem.CategoryOfNonInjectionUsagePartial
                    .FirstOrDefault(x => x.CategoryCode == _rowSelected.C033);
                if (catCheck == null)
                {
                    _rowSelected.C024  = _scaleValue;
                    _rowSelected.C023  = _scaleValue;
                    _rowSelected.C021  = _scaleValue;
                    _rowSelected.C036  = Math.Round(_scaleValue / (double)_rowSelected.C025, 2);
                }
                else
                {
                    var usage   = _rowSelected.C035 == 100
                        ? (double)Math.Round((decimal)(_scaleValue * _percentOfUsage / 100) / (decimal)_rowSelected.C028, 3)
                        : (double)Math.Round((decimal)(_scaleValue * _percentOfUsage / 100) / (decimal)_rowSelected.C028, 3);
                    var unusage = (_scaleValue - usage * _rowSelected.C028) / _rowSelected.C028;
                    _rowSelected.C024  = _scaleValue;
                    _rowSelected.C023  = usage;
                    _rowSelected.C021  = usage;
                    _rowSelected.C022  = unusage;
                    _rowSelected.C036  = Math.Round(usage / 2, 2);
                }
            }

            // Multi-size mold weight distribution
            var pfx2      = GlobalVariable.PrefixUpToSecondHyphen(_rowSelected.C002);
            var sameMolds = _scaleDataFinal.Where(x =>
                x.C020 == _rowSelected.C020 && x.C004 == _rowSelected.C004 &&
                GlobalVariable.PrefixUpToSecondHyphen(x.C002) == pfx2 &&
                x.C015 == _rowSelected.C015).ToList();

            if (sameMolds.Count > 1)
            {
                var sumPW     = sameMolds.Sum(x => x.C023 * x.C028);
                var pairShot  = sameMolds.Sum(x => x.C028);
                foreach (var s in sameMolds)
                {
                    s.C024 = _rowSelected.C024;
                    s.C022 = s.C023 > 0 ? (_rowSelected.C024 - sumPW) / pairShot : 0;
                }
            }

            // Cascade update subsequent steps
            var toUpdate = _scaleDataFinal.Where(x =>
                x.C015 >= _rowSelected.C015 && x.C024 > 0 &&
                !x.C003.StartsWith("Stud") && !x.C003.StartsWith("Inlay") &&
                !x.C003.StartsWith("Ring") && !x.C002.StartsWith("REX") &&
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
            FocusRowByStepCode(_grvTotalStep, "C002", _rowSelected.C002);

            // ── NEW: refresh right panel after save ────────────────────────────────
            UpdateReferencePanel();
        }

        // ─── RFID ────────────────────────────────────────────────────────────────
        private void _txtRFIDCode_DataValueChanged(object sender, DataValueChangedEventArgs e)
        {
            try
            {
                _employeeCode = e.NewValue.Value.ToString();
                if (string.IsNullOrEmpty(_employeeCode))
                    throw new Exception("ID cannot be null.");

                using var db = _dbFactory.CreateDbContext();
                _operatorInfo = db.fT029_Operator_RFIDs.FirstOrDefault(x => x.C000.Contains(_employeeCode));

                if (_operatorInfo == null || _operatorInfo.Id == Guid.Empty)
                {
                    GlobalVariable.InvokeIfRequired(this, () =>
                    { _txtRFIDCode.Text = string.Empty; _txtRFIDName.Focus(); });
                    throw new Exception($"Employee ID {_employeeCode} not found. Please enter the name and press Enter to register.");
                }

                _operatorInfo.DepartmentInfor = db.FT031s.FirstOrDefault(x =>
                    x.Id == _operatorInfo.C002 && (x.C000 == "IT" || x.C000 == "QC"));

                if (_operatorInfo.DepartmentInfor == null)
                {
                    _employeeCode = null;
                    GlobalVariable.InvokeIfRequired(this, () =>
                    { _txtRFIDName.Text = string.Empty; _txtRFIDCode.Text = string.Empty; });
                    throw new Exception($"Employee {_employeeCode} does not have permission for this function.");
                }

                GlobalVariable.InvokeIfRequired(this, () => _txtRFIDName.Text = _operatorInfo.C001);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RFID error");
                MessageBox.Show(ex.Message, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task _txtRFIDName_KeyDownAsync(object? sender, KeyEventArgs e)
        {
            try
            {
                if (e.KeyCode != Keys.Enter) return;
                var t = (TextEdit)sender;
                if (string.IsNullOrEmpty(_employeeCode) || string.IsNullOrEmpty(t.Text))
                    throw new Exception("ID or name cannot be null.");

                if (MessageBox.Show($"Register operator {t.Text} with ID {_employeeCode}?",
                    "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

                using var db = _dbFactory.CreateDbContext();
                var dept = db.FT031s.FirstOrDefault(x => x.C000 == "QC")
                    ?? throw new Exception("Department 'QC' not found.");

                if (await db.fT029_Operator_RFIDs.AnyAsync(x => x.C000 == _employeeCode))
                    throw new Exception("ID already exists.");

                await db.fT029_Operator_RFIDs.AddAsync(new FT029_Operator_RFID
                {
                    Id = Guid.NewGuid(), C000 = _employeeCode, C001 = t.Text,
                    C002 = dept.Id, CreatedDate = DateTime.Now,
                    CreatedBy = string.Empty, CreatedMachine = Environment.MachineName, Actived = true
                });
                await db.SaveChangesAsync();
                MessageBox.Show($"Operator '{_employeeCode}-{t.Text}' registered successfully.", "OK",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RFID name keydown error");
                MessageBox.Show(ex.Message, "WARNING", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // ─── Hydra data sync ─────────────────────────────────────────────────────
        private async Task GetDataHydra()
        {
            try
            {
                using var db = _dbFactory.CreateDbContext();
                _hydraItemDetails = await db.Database
                    .SqlQueryRaw<HydraItemDetailModel>("sp_GetFullStepItemHydraIsRun")
                    .AsNoTracking().ToListAsync();
                _hydraItemDetails = _hydraItemDetails.OrderBy(x => x.FGItemCode).ThenBy(x => x.StepIndex).ToList();

                if (!_hydraItemDetails.Any()) return;

                var ft601s = new List<FT601>();
                var toInsert = _hydraItemDetails.Where(d =>
                    !db.FT601s.Any(ft => ft.C004 == d.StepItemCode && ft.C015 == d.Machine &&
                                         ft.C018 == d.OrderHydraNum && ft.Actived == true)).ToList();

                var now = DateTime.Now; var machine = Environment.MachineName;
                foreach (var item in toInsert)
                    ft601s.Add(new FT601
                    {
                        Id = Guid.NewGuid(), C000 = item.HydraOrderType,
                        C001  = item.Location == "Sample" ? EnumSampleLocation.Sample : EnumSampleLocation.Production,
                        C002  = item.Size, C003 = item.MainName, C004 = item.StepItemCode,
                        C005  = item.StepItemName, C006 = item.Artikel, C007 = item.FGItemCode,
                        C008  = item.FGItemName, C009 = item.StepIndexHydra, C010 = item.StepIndex,
                        C011  = item.ColorCode, C012 = item.ColorName,
                        C013  = item.ArticlePairShot, C014 = item.MoldPairShot, C015 = item.Machine,
                        C016  = item.MachineGroup, C017 = false, C018 = item.OrderHydraNum,
                        C019  = item.MoldId, C020 = item.MainCode, Actived = true,
                        CreatedMachine = machine, CreatedDate = now,
                        Mesoyear = item.MesoYear, Mesocomp = item.MesoComp
                    });

                if (ft601s.Any())
                {
                    await db.FT601s.AddRangeAsync(ft601s);
                    await db.SaveChangesAsync();
                }
            }
            catch (Exception ex) { _logger.LogError(ex, "GetDataHydra error"); }
        }

        // =========================================================================
        //  UPDATE UI  (same as original + calls UpdateReferencePanel)
        // =========================================================================
        private void UpdateUI(bool refresh = true)
        {
            GlobalVariable.InvokeIfRequired(this, () =>
            {
                _txtStepCode.Text                     = _rowSelected?.C002;
                _txtMachine.Text                      = _rowSelected?.C004;
                _txtSize.Text                         = _rowSelected?.C008;
                _txtStepIndex.Text                    = _rowSelected?.C015.ToString();
                _txtActiclePairShot.Text              = _rowSelected?.C028.ToString();
                _txtQty.Text                          = _rowSelected?.C025.ToString();
                _txtFgItemCode.Text                   = _rowSelected?.C013;
                _txtFGName.Text                       = _rowSelected?.C014;
                _txtPercentOFusageNonwoven.EditValue  = _rowSelected?.C035 != 0 ? _rowSelected?.C035 : _percentOfUsage;
                _txtRemark.Text                       = _remarkFinal;

                var catCheck = GlobalVariable.ConfigSystem.CategoryOfNonInjectionUsagePartial
                    .FirstOrDefault(x => x.CategoryCode == _rowSelected?.C033);
                _txtPercentOFusageNonwoven.Enabled =
                    (_rowSelected?.C002?.Substring(0, 3) == "REX" && catCheck != null);

                if (refresh)
                    _grvTotalStep.RefreshData();
                else
                {
                    _grcTotalStep.DataSource = null;
                    _grcTotalStep.DataSource = _scaleDataFinal;
                    _grvTotalStep.PopulateColumns();
                    _grvTotalStep.BestFitColumns();
                    _grvTotalStep.Columns[nameof(FT600.C010)].Visible = false;
                    _grvTotalStep.Columns[nameof(FT600.C011)].Visible = false;
                    _grvTotalStep.Columns[nameof(FT600.C012)].Visible = false;
                    _grvTotalStep.Columns[nameof(FT600.C030)].Visible = false;
                    _grvTotalStep.Columns[nameof(FT600.C031)].Visible = false;
                }

                if (_newScale) RenderGridButton();

                // ── NEW: refresh reference panel whenever UI updates ───────────
                UpdateReferencePanel();
            });
        }

        // ─── Grid action column ───────────────────────────────────────────────────
        private void RenderGridButton()
        {
            if (_buttonEdit == null) _buttonEdit = new RepositoryItemButtonEdit();

            if (_grvTotalStep.Columns["ActionColumn"] == null)
            {
                var col      = _grvTotalStep.Columns.AddField("ActionColumn");
                col.UnboundType = DevExpress.Data.UnboundColumnType.Object;
                col.Visible  = true;
                col.Width    = 100;
                col.Caption  = "Actions";
            }

            _buttonEdit.Buttons.Clear();
            _buttonEdit.Buttons.Add(new EditorButton(ButtonPredefines.Glyph)
            {
                ImageOptions = { Image = Properties.Resources.scale_30 }, ToolTip = "Scale"
            });
            _buttonEdit.Buttons.Add(new EditorButton(ButtonPredefines.Glyph)
            {
                ImageOptions = { Image = Properties.Resources.eraser_30 }, ToolTip = "Reset"
            });
            _buttonEdit.Buttons.Add(new EditorButton(ButtonPredefines.Glyph)
            {
                ImageOptions = { Image = Properties.Resources.delete_30_black }, ToolTip = "Delete"
            });
            _buttonEdit.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.HideTextEditor;
            _grvTotalStep.Columns["ActionColumn"].ColumnEdit    = _buttonEdit;
            _grvTotalStep.Columns["ActionColumn"].VisibleIndex  = 0;

            if (!_grcTotalStep.RepositoryItems.Contains(_buttonEdit))
                _grcTotalStep.RepositoryItems.Add(_buttonEdit);
        }

        // ─── Grid focus helper ────────────────────────────────────────────────────
        private void FocusRowByStepCode(GridView view, string field, string code)
        {
            if (view == null) return;
            view.BeginUpdate();
            try
            {
                view.ClearSelection();
                int rh = view.LocateByValue(field, code);
                if (rh < 0) rh = FindRowContains(view, field, code);
                if (rh >= 0) { view.FocusedRowHandle = rh; view.SelectRow(rh); view.MakeRowVisible(rh, true); }
                else { view.ApplyFindFilter(code); }
            }
            finally { view.EndUpdate(); }
        }

        private int FindRowContains(GridView view, string field, string keyword)
        {
            if (string.IsNullOrEmpty(keyword)) return -1;
            for (int i = 0; i < view.DataRowCount; i++)
            {
                int h = view.GetVisibleRowHandle(i);
                if (h < 0) continue;
                var val = view.GetRowCellValue(h, field)?.ToString();
                if (!string.IsNullOrEmpty(val) &&
                    val.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                    return h;
            }
            return -1;
        }
    }
}
