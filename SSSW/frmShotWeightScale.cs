using DevExpress.CodeParser.Diagnostics;
using DevExpress.Data.Controls.ExpressionEditor;
using DevExpress.Mvvm;
using DevExpress.Mvvm.POCO;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraPrinting.Native;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ScanAndScale.Driver;
using ScanAndScale.Helper;
using SSSW.models;
using SSSW.modelss;
using System;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace SSSW
{
    public partial class frmShotWeightScale : Form
    {
        // Import để cho phép kéo form
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        private Panel titleBar;
        private Button btnClose;
        private Button btnMaximize;
        private Button btnMinimize;
        private Button btnUpdateVersion;

        #region Public properties
        /// <summary>
        /// List model chứa tất cả các item lấy từ Hydra về, được truyền từ master vào.
        /// </summary>
        public List<FT601> _dataHydra { get; set; } = new();
        //public List<HydraItemDetailModel> _dataHydra { get; set; } = new();


        /// <summary>
        /// Bước được chọn để vào cân, được truyền từ master vào, và ta dựa vào thông tin này để tạo ra bộ data cân tương ứng.
        /// </summary>
        public FT601 _stepItemCodeScale { get; set; } = new FT601();
        //public HydraItemDetailModel _stepItemCodeScale { get; set; } = new HydraItemDetailModel();

        public bool NewScale { get; set; } = true;

        private bool isUpdateClicked = false;
        #endregion

        #region Private properties
        /// <summary>
        /// Get tất cả các bước từ WL của item FG.
        /// </summary>
        private List<BomWinlineModel> _allStepsFG = new();

        /// <summary>
        /// Lưu trữ dữ liệu đã cân trước đó để so sánh và kiểm tra thứ tự cân.
        /// </summary>
        private List<FT600> _scaledDataPreviousStep = new();

        /// <summary>
        /// Chứa các bước cân generate ra từ WL.
        /// </summary>
        private List<FT600> _scaleData = new();

        /// <summary>
        /// chứa các bước cân của bước hiện tại, và các bước trước đó đã cân, đây chính là model dùng để cân, và lưu DB
        /// </summary>
        private List<FT600> _scaleDataFinal = new();

        /// <summary>
        /// model để chứa step nào được chọn để cân.
        /// </summary>
        private FT600 _rowSelect = new();

        private double _scaleValue = 0;
        private string _mesocomp = string.Empty;
        private int _mesoYear = 0;

        RepositoryItemButtonEdit _buttonEdit = new RepositoryItemButtonEdit();

        private bool _isRunner { get; set; } = true;

        private int _articlePaisShotFinaly = 0;

        private List<StepSelectModel> _allStepCodeMaster = new List<StepSelectModel>();
        private StepSelectModel _stepCodeMasterSelect = new StepSelectModel();
        public string _fgCode = string.Empty;
        private string _fgName = string.Empty;
        private string _qrCodeScan = string.Empty;
        private FT606_Label _labelInfo = new FT606_Label();
        private string _employeeCode = string.Empty, _employeeName = string.Empty;
        private FT029_Operator_RFID _operatorInfo = new FT029_Operator_RFID();//chua thông tin của operator, để xem có đúng phòng ban đc phép dùng tính năng này hay ko.
        #endregion

        //inject services
        private readonly IServiceProvider _serviceProvider;
        private readonly IDbContextFactory<DbContextDogeWH> _dbFactory;
        private readonly ILogger<frmShotWeightScale> _logger;


        public frmShotWeightScale(IDbContextFactory<DbContextDogeWH> dbFactory, IServiceProvider serviceProvider, ILogger<frmShotWeightScale> logger) : this()
        {
            _dbFactory = dbFactory;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public frmShotWeightScale()
        {
            InitializeComponent();

            #region add header
            // Cấu hình form
            this.Text = "Custom Title Bar";
            this.FormBorderStyle = FormBorderStyle.None; // Bỏ header mặc định
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new System.Drawing.Size(1920, 1080);

            // Tạo panel làm thanh tiêu đề
            titleBar = new Panel();
            titleBar.Dock = DockStyle.Top;
            titleBar.Height = 40;
            titleBar.BackColor = Color.Black;
            titleBar.MouseDown += TitleBar_MouseDown;
            this.Controls.Add(titleBar);

            // Nút Close
            btnClose = new Button();
            btnClose.Text = "";
            btnClose.ForeColor = Color.White;
            btnClose.BackColor = Color.Black;
            btnClose.FlatStyle = FlatStyle.Flat;
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Size = new Size(40, 40);
            btnClose.Location = new Point(this.Width - 40, 0);
            btnClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 1) Gán icon từ Resources (đặt tên hình là "updateVersion" như trong Resource)
            btnClose.Image = Properties.Resources.close_white_30;  // PNG từ Resources
            btnClose.ImageAlign = ContentAlignment.MiddleCenter;  // căn giữa
            btnClose.Padding = new Padding(0);                    // tránh lệch
            btnClose.TextImageRelation = TextImageRelation.Overlay; // chỉ icon
            btnClose.Click += BtnClose_Click;
            titleBar.Controls.Add(btnClose);

            // Nút Maximize
            btnMaximize = new Button();
            btnMaximize.Text = "";
            btnMaximize.ForeColor = Color.White;
            btnMaximize.BackColor = Color.Black;
            btnMaximize.FlatStyle = FlatStyle.Flat;
            btnMaximize.FlatAppearance.BorderSize = 0;
            btnMaximize.Size = new Size(40, 40);
            btnMaximize.Location = new Point(this.Width - 80, 0);
            btnMaximize.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 1) Gán icon từ Resources (đặt tên hình là "updateVersion" như trong Resource)
            btnMaximize.Image = Properties.Resources.maximize_white_30;  // PNG từ Resources
            btnMaximize.ImageAlign = ContentAlignment.MiddleCenter;  // căn giữa
            btnMaximize.Padding = new Padding(0);                    // tránh lệch
            btnMaximize.TextImageRelation = TextImageRelation.Overlay; // chỉ icon
            btnMaximize.Click += BtnMaximize_Click;
            titleBar.Controls.Add(btnMaximize);

            // Nút Minimize
            btnMinimize = new Button();
            btnMinimize.Text = "";
            btnMinimize.ForeColor = Color.White;
            btnMinimize.BackColor = Color.Black;
            btnMinimize.FlatStyle = FlatStyle.Flat;
            btnMinimize.FlatAppearance.BorderSize = 0;
            btnMinimize.Size = new Size(40, 40);
            btnMinimize.Location = new Point(this.Width - 120, 0);
            btnMinimize.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 1) Gán icon từ Resources (đặt tên hình là "updateVersion" như trong Resource)
            btnMinimize.Image = Properties.Resources.minimize_white_30;  // PNG từ Resources
            btnMinimize.ImageAlign = ContentAlignment.MiddleCenter;  // căn giữa
            btnMinimize.Padding = new Padding(0);                    // tránh lệch
            btnMinimize.TextImageRelation = TextImageRelation.Overlay; // chỉ icon
            btnMinimize.Click += BtnMinimize_Click;
            titleBar.Controls.Add(btnMinimize);


            // Nút update version
            btnUpdateVersion = new Button();
            btnUpdateVersion.Text = "";                      // Không cần chữ, chỉ hiển thị icon
            btnUpdateVersion.ForeColor = Color.White;
            btnUpdateVersion.BackColor = Color.Black;
            btnUpdateVersion.FlatStyle = FlatStyle.Flat;
            btnUpdateVersion.FlatAppearance.BorderSize = 0;
            btnUpdateVersion.Size = new Size(40, 40);
            btnUpdateVersion.Location = new Point(this.Width - 160, 0);
            btnUpdateVersion.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnUpdateVersion.Cursor = Cursors.Hand;

            // 1) Gán icon từ Resources (đặt tên hình là "updateVersion" như trong Resource)
            btnUpdateVersion.Image = Properties.Resources.arrow_upward_white_30;  // PNG từ Resources
            btnUpdateVersion.ImageAlign = ContentAlignment.MiddleCenter;  // căn giữa
            btnUpdateVersion.Padding = new Padding(0);                    // tránh lệch
            btnUpdateVersion.TextImageRelation = TextImageRelation.Overlay; // chỉ icon

            // Tùy chọn: scale icon nếu quá lớn/nhỏ (WinForms Button không có ImageLayout)
            // => bạn có thể dùng phiên bản icon 24x24 hoặc 32x32 trong file PNG để vừa với nút 40x40.

            // 2) Tooltip khi hover
            var tip = new ToolTip();
            tip.AutoPopDelay = 5000;     // hiển thị tối đa 5 giây
            tip.InitialDelay = 300;      // trễ 300ms
            tip.ReshowDelay = 100;       // xuất hiện lại nhanh
            tip.ShowAlways = true;       // luôn hiển thị tooltip
            tip.SetToolTip(btnUpdateVersion, "Click to update version");  // nội dung tooltip

            // Tùy chọn: hiệu ứng hover (đổi nền cho dễ nhìn)
            btnUpdateVersion.MouseEnter += (s, e) => btnUpdateVersion.BackColor = Color.FromArgb(30, 30, 30);
            btnUpdateVersion.MouseLeave += (s, e) => btnUpdateVersion.BackColor = Color.Black;

            // Sự kiện Click (giữ nguyên như bạn đã có)
            btnUpdateVersion.Click += BtnUpdateVersion_Click; ; // hoặc sự kiện update version thực tế của bạn
            titleBar.Controls.Add(btnUpdateVersion);


            // Đảm bảo tất cả có cùng Height = 30 và Y = 5
            btnClose.Size = btnMaximize.Size = btnMinimize.Size = btnUpdateVersion.Size = new Size(30, 30);


            // Anchor cho cả 3 nút
            btnClose.Anchor = btnMaximize.Anchor = btnMinimize.Anchor = btnUpdateVersion.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            // Logo
            PictureBox logo = new PictureBox();
            logo.Image = Properties.Resources.framas_white; // logo từ Resources
            logo.SizeMode = PictureBoxSizeMode.Zoom;
            logo.Size = new Size(100, 30); // kích thước logo
            logo.Location = new Point(10, 5); // vị trí bên trái
            titleBar.Controls.Add(logo);

            // Text
            Label titleText = new Label();
            titleText.Text = $"fFT - SSSW Station";
            titleText.ForeColor = Color.White;
            titleText.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            titleText.AutoSize = true;
            titleText.Location = new Point(120, 10); // ngay sau logo
            titleBar.Controls.Add(titleText);
            #endregion

            Load += FrmShotWeightScale_Load;
            FormClosing += FrmShotWeightScale_FormClosing;
        }

        #region Events
        private async void FrmShotWeightScale_Load(object? sender, EventArgs e)
        {
            using var dbContext = _dbFactory.CreateDbContext();
            _mesocomp = dbContext.Database.SqlQueryRaw<string>($"sp_MaterialGetCompanyName").AsEnumerable().FirstOrDefault();
            _mesoYear = dbContext.Database.SqlQueryRaw<int>($"sp_MaterialGetMesoyear").AsEnumerable().FirstOrDefault();

            //get config
            var configDaTa = await dbContext.FT608s.FirstOrDefaultAsync(x => x.c001 == Environment.MachineName);
            if (configDaTa != null)
            {
                GlobalVariable.ConfigSystem = JsonConvert.DeserializeObject<ConfigModel>(configDaTa.c001);
            }
            else
            {
                await dbContext.FT608s.AddAsync(new FT608_Config()
                {
                    Id = Guid.NewGuid(),
                    c000 = Environment.MachineName,
                    c001 = JsonConvert.SerializeObject(new ConfigModel()),
                    Mesoyear = _mesoYear,
                    Mesocomp = _mesocomp,
                    CreatedMachine = Environment.MachineName,
                    CreatedDate = DateTime.Now
                });

                await dbContext.SaveChangesAsync();
            }


            //get all data master
            _dataHydra = await dbContext.FT601s.ToListAsync();
            _allStepCodeMaster = _dataHydra.Select(x => new StepSelectModel()
            {
                StepItemCode = x.C004,
                StepItemName = x.C005,
                Machine = x.C015,
                HydraOrderNo = x.C018
            }).Distinct().ToList();

            #region Grid initialize
            _grvTotalStep.OptionsView.ShowAutoFilterRow = true;
            _grvTotalStep.OptionsCustomization.AllowFilter = true;
            _grvTotalStep.OptionsView.ShowFilterPanelMode = DevExpress.XtraGrid.Views.Base.ShowFilterPanelMode.ShowAlways;
            _grvTotalStep.OptionsView.ColumnAutoWidth = false;
            _grvTotalStep.OptionsCustomization.AllowSort = true;
            _grvTotalStep.OptionsBehavior.ReadOnly = true;
            _grvTotalStep.OptionsView.ShowFooter = true;
            _grvTotalStep.OptionsView.ShowGroupPanel = true;
            _grvTotalStep.OptionsFind.AlwaysVisible = true;

            //_grvTotalStep.OptionsSelection.MultiSelect = true;
            //_grvTotalStep.OptionsSelection.MultiSelectMode = GridMultiSelectMode.CheckBoxRowSelect;
            //_grvTotalStep.OptionsSelection.ShowCheckBoxSelectorInColumnHeader = DevExpress.Utils.DefaultBoolean.True;
            #endregion

            //  DriverControlHelper.ConfigWeight(teGrossWeight, lupTareWeight, teNetWeight, "n1");
            //  ControlHelper.LoadShitfs(luWorkShitfs);

            _scanBarcode.Config = GlobalVariable.ConfigSystem.Scanner;
            _txtRFIDCode.Config = GlobalVariable.ConfigSystem.RFID;
            _txtScaleValue.Config = GlobalVariable.ConfigSystem.Scale;
            _txtScaleValue.EnableReadScale = true;

            _scanBarcode.DataValueChanged += _scanBarcode_DataValueChanged;

            _txtRFIDCode.DataValueChanged += _txtRFIDCode_DataValueChanged;
            _txtRFIDName.KeyDown += async (s, args) => await _txtRFIDName_KeyDownAsync(s, args);

            _txtScaleValue.DataValueChanged += _txtScaleValue_DataValueChanged;
            _btnSaveWeight.Click += _btnSaveWeight_Click;
            _btnConfirm.Click += _btnComfim_Click;
            _btnCancel.Click += _btnCancel_Click;

            _txtActiclePairShot.EditValueChanged += (s, ev) =>
            {
                _articlePaisShotFinaly = int.TryParse(_txtActiclePairShot.EditValue.ToString(), out int value) ? value : 0;

                _scaleDataFinal.ForEach(x => x.C028 = _articlePaisShotFinaly);
            };

            // Đặt text khi bật/tắt
            _toggleSwitchRunner.Properties.OnText = "Yes";
            _toggleSwitchRunner.Properties.OffText = "No";
            // Đặt trạng thái mặc định
            _toggleSwitchRunner.IsOn = true;

            //_toggleSwitchRunner.EditValueChanged += (s, ev) =>
            //{
            //    _isRunner = (bool)_toggleSwitchRunner.EditValue;
            //    //_toggleSwitchRunner.Properties.OffText = _isRunner ? "Runner" : "Non-Runner";
            //    //_toggleSwitchRunner.Properties.OnText = _isRunner ? "Runner" : "Non-Runner";
            //};

            //InitRepository();
            //_repositoryItemButtonEditScale.Click += _repositoryItemButtonEditScale_Click;
            _buttonEdit.ButtonClick += _buttonEdit_ButtonClick;

            _grvTotalStep.RowStyle += _grvTotalStep_RowStyle;

            //chọn sẵn dòng đầu tiên để cân
            _rowSelect = _scaleDataFinal.FirstOrDefault(x => x.C002 == _stepItemCodeScale.C004 && x.C015 == _stepItemCodeScale.C010);
            //_rowSelect = _scaleDataFinal.FirstOrDefault(x => x.C002 == _stepItemCodeScale.StepItemCode && x.C015 == _stepItemCodeScale.StepIndex);

            //// render Add button                   
            //RenderAddButtonForGrid();

            //Check If scanned then disable buttons.
            // CheckEnableScale();

            _lkStepCode.Properties.DataSource = _allStepCodeMaster;
            _lkStepCode.Properties.DisplayMember = "StepItemCode";
            _lkStepCode.Properties.ValueMember = "StepItemCode";
            _lkStepCode.Properties.PopulateColumns();

            _lkStepCode.ButtonClick += Properties_ButtonClick;

            _lkStepCode.EditValueChanged += async (s, ev) => await _lkStepCode_EditValueChangedAsync(s, ev);

            //_lkStepCode.EditValue = !string.IsNullOrEmpty(_fgCode) ? _fgCode : null;
        }

        private async Task _txtRFIDName_KeyDownAsync(object? sender, KeyEventArgs e)
        {
            try
            {
                TextEdit t = (TextEdit)sender;

                if (e.KeyCode == Keys.Enter)
                {
                    if (string.IsNullOrEmpty(_employeeCode) || string.IsNullOrEmpty(t.Text))
                    {
                        throw new Exception("The ID or name can not be null.");
                    }

                    if (MessageBox.Show($"Do you want to adding the operator {t.Text} and ID {_employeeCode}", "QUESTIONs", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        using var dbContext = _dbFactory.CreateDbContext();

                        var department = dbContext.FT031s.FirstOrDefault(x => x.C000 == "QC");

                        if (department == null)
                        {
                            throw new Exception("Department C&M not found.");
                        }

                        var checkExist = await dbContext.fT029_Operator_RFIDs.FirstOrDefaultAsync(x => x.C000 == _employeeCode);

                        if (checkExist != null)
                        {
                            throw new Exception("The ID already exist.");
                        }

                        var newOperator = new FT029_Operator_RFID()
                        {
                            Id = Guid.NewGuid(),
                            C000 = _employeeCode,
                            C001 = t.Text,
                            C002 = department.Id,
                            CreatedDate = DateTime.Now,
                            CreatedBy = string.Empty,
                            CreatedMachine = Environment.MachineName,
                            Actived = true
                        };

                        await dbContext.fT029_Operator_RFIDs.AddAsync(newOperator);
                        await dbContext.SaveChangesAsync();

                        MessageBox.Show($"Register for operator '{_employeeCode}-{t.Text}' successful.", "INFORMATION", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in _txtRFIDName_KeyDown");
                MessageBox.Show(ex.Message, "WARNING", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void FrmShotWeightScale_FormClosing(object sender, FormClosingEventArgs e)
        {
            _txtRFIDCode.DataValueChanged -= _txtRFIDCode_DataValueChanged;
            _txtScaleValue.DataValueChanged -= _txtScaleValue_DataValueChanged;
            _btnSaveWeight.Click -= _btnSaveWeight_Click;
            _btnConfirm.Click -= _btnComfim_Click;
            _btnCancel.Click -= _btnCancel_Click;
        }

        // Cho phép kéo form bằng panel
        private void TitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            ReleaseCapture();
            SendMessage(this.Handle, 0x112, 0xf012, 0);
        }

        private void BtnUpdateVersion_Click(object sender, EventArgs e)
        {
            //try
            //{
            //    isUpdateClicked = true;
            //    string UUrl = GlobalVariables.ConfigJson.UpdatePath;
            //    SplashScreenManager.ShowForm(typeof(WaitForm1));
            //    System.Threading.Thread.Sleep(3000);
            //    AutoUpdater.Start(UUrl);
            //}
            //catch (Exception ex)
            //{
            //    XtraMessageBox.Show($"{ex.Message}", "Error");
            //}
            //finally
            //{
            //    SplashScreenManager.CloseForm(false);
            //}
        }

        private void BtnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void BtnMaximize_Click(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Normal)
                this.WindowState = FormWindowState.Maximized;
            else
                this.WindowState = FormWindowState.Normal;
        }

        private void BtnMinimize_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private async void _scanBarcode_DataValueChanged(object? sender, DataValueChangedEventArgs e)
        {
            var sen = sender as ButtonEdit;
            //get cac thong tin lien quan den nguye lieu

            //get cac thong tin lien quan den nguye lieu
            _qrCodeScan = e.NewValue.Value.ToString();

            //using (var dbContext = new Entities2())
            //{
            //    _labelInfo = await dbContext.FT606s
            //        .Where(x => x.Actived == true && x.Mesoyear == _mesoYear)
            //        .OrderByDescending(x => x.CreatedDate)
            //        .FirstOrDefaultAsync();
            //}

            //GlobalVariable.InvokeIfRequired(this, () => _lkStepCode.EditValue = "");
        }

        private void Properties_ButtonClick(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            LookUpEdit edit = sender as LookUpEdit;
            EditorButton Button = e.Button;
            if (edit.Properties.Buttons.IndexOf(e.Button) == 0)
            {
                edit.EditValue = null;
            }
        }

        private async Task _lkStepCode_EditValueChangedAsync(object sender, EventArgs e)
        {
            try
            {
                ResetNewLoop();

                var editor = sender as DevExpress.XtraEditors.LookUpEdit;
                if (editor == null) return;

                // 1) Giá trị (ValueMember)
                var selectedValue = editor.EditValue; // kiểu object, thường là string/int tùy nguồn dữ liệu

                // 2) Text hiển thị (DisplayMember)
                var selectedText = editor.Text;

                // 3) (Tuỳ chọn) Lấy toàn bộ row dữ liệu đang chọn
                // Với LookUpEdit:
                _stepCodeMasterSelect = (StepSelectModel)editor.GetSelectedDataRow(); // DataRowView hoặc object (tuỳ DataSource)
                                                                                      // Với GridLookUpEdit:
                                                                                      // var gridEditor = sender as GridLookUpEdit;
                                                                                      // var row = gridEditor.Properties.View.GetFocusedRow();
                                                                                      //truy vấn master data để lấy thông tin FG code
                _fgCode = _dataHydra.FirstOrDefault(x => x.C004 == _stepCodeMasterSelect.StepItemCode &&
                    x.C015 == _stepCodeMasterSelect.Machine &&
                    x.C018 == _stepCodeMasterSelect.HydraOrderNo
                ).C007 ?? string.Empty;

                if (string.IsNullOrEmpty(_fgCode))
                {
                    throw new Exception("The step was not found, please recheck master data.");
                }

                // Ví dụ: cập nhật các control khác
                await GetDataAsync(_fgCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in _lkStepCode_EditValueChangedAsync");
                MessageBox.Show(ex.Message, "WARNING", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ResetNewLoop()
        {
            _fgCode = _fgName = string.Empty;
            _rowSelect = new FT600();
            _allStepsFG = new List<BomWinlineModel>();
            _stepItemCodeScale = new FT601();
            _scaleData = new List<FT600>();
            _scaleDataFinal = new List<FT600>();
            _stepCodeMasterSelect = new StepSelectModel();
            NewScale = true;

            UpdateUI();
        }

        private void ResetAll()
        {
            _stepCodeMasterSelect = new StepSelectModel();

        }


        private async Task GetDataAsync(string fgCode)
        {
            try
            {
                using var dbContextDogeWH = _dbFactory.CreateDbContext();

                #region Lấy data của các bước cân
                //Lấy ra tất cả các bước chạy theo FG item code để kiểm tra thứ tự cân có đúng theo thứ tự của các bước chạy hay không.
                _stepItemCodeScale = await dbContextDogeWH.FT601s.Where(x => x.C007 == fgCode).FirstOrDefaultAsync();

                if (_stepItemCodeScale == null)
                {
                    throw new Exception($"Step item code {fgCode} not found in the SSSW systems, please check again.");
                }

                //get thông tin BOM từ Winline
                //_allStepsFG = await dbContextDogeWH.Database.SqlQueryRaw<BomWinlineModel>(
                //    $"sp_getBomWinlineOfItemFG @itemFG = {0}", fgCode)
                //    .OrderBy(x => x.ParallelSequence).ThenBy(x => x.ItemFgCode)
                //    .ToListAsync();


                //var pItemFg = new SqlParameter("@itemFG", fgCode);

                //_allStepsFG = await dbContextDogeWH.BomWinlineModels
                //    .FromSqlRaw("EXEC sp_getBomWinlineOfItemFG @itemFG", pItemFg)
                //    .AsNoTracking()
                //    .ToListAsync();

                _allStepsFG = await dbContextDogeWH.Database.SqlQueryRaw<BomWinlineModel>("sp_getBomWinlineOfItemFG @itemFG = {0}", fgCode).AsNoTracking().ToListAsync();


                foreach (var item in _allStepsFG)
                {
                    var line = new FT600();
                    bool allowScale = true;

                    //kiểm tra các bước cân của itemFG đưuọc plan trên Hydra, chỉ cho phép cân các steps thuộc kế hoạch này.
                    FT601 ckHydra = new();
                    ckHydra = _dataHydra.FirstOrDefault(x => x.C004 == item.ItemStepCode && x.C007 == item.ItemFgCode);
                    //HydraItemDetailModel ckHydra = new();
                    //ckHydra = _dataHydra.FirstOrDefault(x => x.StepItemCode == item.ItemStepCode && x.FGItemCode == item.ItemFgCode);

                    if (ckHydra == null)
                    {
                        if (item.ItemStepCode != "Z-VHXXXXXX" && item.ItemStepCode.Substring(0, 3) != "REX")
                        {

                            var mc = item.ItemFgCode.Split('-')[0];
                            var smc = item.ItemStepCode.Split('-')[1];
                            ckHydra = _dataHydra.FirstOrDefault(x => x.C007.Contains($"{mc}-") || (x.C004.Contains($"-{smc}-")));
                            //ckHydra = _dataHydra.FirstOrDefault(x => x.FGItemCode.Contains($"{mc}-") || (x.StepItemCode.Contains($"-{smc}-")));
                        }
                        else
                        {
                            ckHydra = new FT601()
                            {
                                C007 = item.ItemFgCode,
                                C008 = item.ItemFgName,
                                C004 = item.ItemStepCode,
                                C005 = item.ItemStepName,
                                C000 = _stepItemCodeScale.C000,
                                //HydraOrderType = _stepItemCodeScale.HydraOrderType,
                                C010 = item.ParallelSequence
                            };

                            //ckHydra = new HydraItemDetailModel()
                            //{
                            //    FGItemCode = item.ItemFgCode,
                            //    FGItemName = item.ItemFgName,
                            //    StepItemCode = item.ItemStepCode,
                            //    StepItemName = item.ItemStepName,
                            //    HydraOrderType = _stepItemCodeScale.HydraOrderType,
                            //    //HydraOrderType = _stepItemCodeScale.HydraOrderType,
                            //    StepIndex = item.ParallelSequence
                            //};
                        }

                        allowScale = false;
                    }

                    line.C000 = ckHydra?.C000;
                    line.C001 = ckHydra?.C000 != "21" && _stepItemCodeScale.C000 != "22" ? EnumSampleLocation.Production : EnumSampleLocation.Sample;
                    line.C004 = ckHydra?.C015;
                    line.C005 = ckHydra?.C006;
                    line.C006 = ckHydra?.C011;
                    line.C007 = ckHydra?.C012;
                    line.C009 = 1;
                    line.C012 = string.Empty;// $"{_stepItemCodeScale.StepItemCode}|{_stepItemCodeScale.Machine}|{Gui}";
                    line.C013 = ckHydra?.C007;
                    line.C014 = ckHydra?.C008;
                    line.C016 = null;
                    line.C017 = ckHydra?.C013;
                    line.C018 = ckHydra?.C014;
                    line.C019 = ckHydra?.C016;
                    line.C020 = ckHydra?.C019;

                    line.C002 = item.ItemStepCode;
                    line.C003 = item.ItemStepName;
                    line.C008 = item.Size;
                    line.C015 = item.ParallelSequence;
                    //                       
                    line.C021 = 0; // Khối lượng sẽ được cập nhật sau khi cân
                    line.C022 = 0;
                    line.C023 = 0;
                    line.C024 = 0;
                    line.C025 = (int)item.Quantity;
                    line.C026 = ckHydra.C020;
                    line.C027 = ckHydra.C003;
                    line.C028 = ckHydra.C013 != null ? (int)ckHydra.C013 : 0;
                    line.AllowScale = allowScale;

                    //line.C000 = ckHydra?.OrderHydraNum;
                    //line.C001 = ckHydra?.HydraOrderType != "21" && _stepItemCodeScale.HydraOrderType != "22" ? EnumSampleLocation.Production : EnumSampleLocation.Sample;
                    //line.C004 = ckHydra?.Machine;
                    //line.C005 = ckHydra?.Artikel;
                    //line.C006 = ckHydra?.ColorCode;
                    //line.C007 = ckHydra?.ColorName;
                    //line.C009 = 1;
                    //line.C012 = string.Empty;// $"{_stepItemCodeScale.StepItemCode}|{_stepItemCodeScale.Machine}|{Gui}";
                    //line.C013 = ckHydra?.FGItemCode;
                    //line.C014 = ckHydra?.FGItemName;
                    //line.C016 = null;
                    //line.C017 = ckHydra?.ArticlePairShot;
                    //line.C018 = ckHydra?.MoldPairShot;
                    //line.C019 = ckHydra?.MachineGroup;
                    //line.C020 = ckHydra?.MoldId;

                    //line.C002 = item.ItemStepCode;
                    //line.C003 = item.ItemStepName;
                    //line.C008 = item.Size;
                    //line.C015 = item.ParallelSequence;
                    ////                       
                    //line.C021 = 0; // Khối lượng sẽ được cập nhật sau khi cân
                    //line.C022 = 0;
                    //line.C023 = 0;
                    //line.C024 = 0;
                    //line.C025 = item.Quantity;
                    //line.C026 = ckHydra.MainCode;
                    //line.C027 = ckHydra.MainName;

                    //line.AllowScale = allowScale;

                    _scaleData.Add(line);
                }

                _scaleDataFinal = _scaleData.ToList();

                //doc thông tin các bước đã cân
                var stepHasScale = await dbContextDogeWH.FT600s
                    .Where(x => x.C013 == _stepItemCodeScale.C007)//.Where(x => x.C013 == _stepItemCodeScale.FGItemCode)
                    .ToListAsync();

                foreach (var item in _scaleDataFinal)
                {
                    if (item.AllowScale)
                    {
                        //nếu bước này đã được cân nhiều lần rồi, thì lấy lần cân gần đây nhất.
                        var line = stepHasScale.Where(x => x.C002 == item.C002).OrderByDescending(x => x.CreatedDate).FirstOrDefault();

                        if (line != null)
                        {
                            item.C021 = line.C021; // Part Weight (g) of step.
                            item.C022 = line.C022; // Runner weight (g) of step.
                            item.C023 = line.C023; // Total scale value of part weight (include these previous step), scale value.
                            item.C024 = line.C024; // Total weight of step injection (include runner + part), Scale value.
                            item.C025 = line.C025; // Số lượng. Dùng cho cân Recetacle/outsoleboard/Stud/Logo để quy đinh số lượng sử dụng trong bước.
                        }
                    }
                    else//nếu ko có khối lượng cân, thì cần lấy thông tin cân theo group size
                    {
                        string mainCode = string.Empty;
                        FT600 stepPrevious = new();

                        if (item.C002 != "Z-VHXXXXXX" && item.C002.Substring(0, 3) != "REX")
                        {
                            mainCode = item.C002.Split('-')[1];
                            //var script = _dbContextDogeWH.Database.GenerateCreateScript();

                            stepPrevious = await dbContextDogeWH.FT600s
                               .Where(x => x.C015 == item.C015
                                   && (x.C002 == item.C002 || (x.C002.Contains(mainCode) && x.C008 == item.C008))
                                   )
                               .OrderByDescending(x => x.CreatedDate).FirstOrDefaultAsync();
                            //&& (x.C002 == item.C002));
                        }
                        else
                        {
                            stepPrevious = await dbContextDogeWH.FT600s
                               .Where(x => x.C015 == item.C015
                                   && x.C002 == item.C002
                                   )
                               .OrderByDescending(x => x.CreatedDate).FirstOrDefaultAsync();

                            if (stepPrevious == null)
                                item.AllowScale = true;
                        }

                        if (stepPrevious != null)
                        {
                            item.C000 = stepPrevious?.C000;
                            item.C001 = stepPrevious?.C001;
                            item.C004 = stepPrevious?.C004;
                            item.C005 = stepPrevious?.C005;
                            item.C006 = stepPrevious?.C006;
                            item.C007 = stepPrevious?.C007;
                            item.C009 = 1;
                            item.C012 = stepPrevious?.C012;// $"{_stepItemCodeScale.StepItemCode}|{_stepItemCodeScale.Machine}|{Gui}";
                            item.C013 = stepPrevious?.C013;
                            item.C014 = stepPrevious?.C014;
                            item.C016 = stepPrevious?.C016;
                            item.C017 = stepPrevious?.C017;
                            item.C018 = stepPrevious?.C018;
                            item.C019 = stepPrevious?.C019;
                            item.C020 = stepPrevious?.C020;

                            item.C002 = stepPrevious?.C002;
                            item.C003 = stepPrevious?.C003;
                            item.C008 = stepPrevious?.C008;
                            item.C015 = stepPrevious?.C015;

                            item.C021 = stepPrevious?.C021 ?? 0; // Part Weight (g) of step.
                            item.C022 = stepPrevious?.C022 ?? 0; // Runner weight (g) of step.
                            item.C023 = stepPrevious?.C023 ?? 0; // Total scale value of part weight (include these previous step), scale value.
                            item.C024 = stepPrevious?.C024 ?? 0; // Total weight of step injection (include runner + part), Scale value.
                            item.C025 = stepPrevious?.C025 ?? 0; // Số lượng. Dùng cho cân Recetacle/outsoleboard/Stud/Logo để quy đinh số lượng sử dụng trong bước.
                            item.C026 = stepPrevious.C026;
                            item.C027 = stepPrevious.C027;
                            item.C028 = stepPrevious.C028;
                        }
                    }
                }

                _scaleDataFinal = _scaleDataFinal.OrderBy(x => x.C015).ToList();

                _rowSelect = _scaleDataFinal.FirstOrDefault();

                //Kiểm tra nếu là đợt cân mới thì reset hết các giá trị cân đọc từ DB lên để vào cân lại và lưu mẻ mới
                if (NewScale)
                {
                    var dataReset = _scaleDataFinal.Where(x => x.AllowScale).ToList();
                    dataReset.ForEach(x =>
                    {
                        x.C021 = 0;
                        x.C022 = 0;
                        x.C023 = 0;
                        x.C024 = 0;
                    });
                }
                else
                {
                    _btnSaveWeight.Enabled = false;
                    _btnConfirm.Enabled = false;
                }
                #endregion

                UpdateUI(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetDataAsync");
                MessageBox.Show(ex.Message, "WARNING", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void _buttonEdit_ButtonClick(object sender, ButtonPressedEventArgs e)
        {
            int rowHandle = _grvTotalStep.FocusedRowHandle;

            var rowSelect = _grvTotalStep.GetRow(rowHandle) as FT600;

            if (rowSelect != null && !rowSelect.AllowScale)
            {
                MessageBox.Show("Do not allow to scale this step.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            //Kiểm tra xem các bước trước nó đã cân chưa
            var previousSteps = _scaleDataFinal.Where(x => x.C015 < rowSelect.C015).ToList();
            foreach (var step in previousSteps)
            {
                if (step.C021 == 0)
                {
                    MessageBox.Show("The previous step has not been weighed.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            _rowSelect = rowSelect;

            //if (e.Button.Index == 0) // Nút "Cân"
            //{
            //    MessageBox.Show("Đã nhấn nút Cân cho dòng: " + rowHandle);
            //    // Gọi hàm cân ở đây
            //}
            if (e.Button.Index == 1) // Nút "Reset"
            {
                //MessageBox.Show("Đã nhấn nút Reset cho dòng: " + rowHandle);
                // Gọi hàm reset ở đây
                var rowReset = _scaleDataFinal.FirstOrDefault(x => x.AllowScale == true && x.C002 == _rowSelect.C002);

                if (rowReset == null)
                {
                    MessageBox.Show("Can not reset this line.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                rowReset.C021 = 0;
                rowReset.C022 = 0;
                rowReset.C023 = 0;
                rowReset.C024 = 0;
            }

            UpdateUI(true);
        }

        private void _grvTotalStep_RowStyle(object sender, DevExpress.XtraGrid.Views.Grid.RowStyleEventArgs e)
        {
            try
            {
                var view = sender as DevExpress.XtraGrid.Views.Grid.GridView;
                FT600 data = view.GetRow(e.RowHandle) as FT600;

                if (data != null)
                {
                    if (!data.AllowScale)
                    {
                        e.Appearance.BackColor = Color.FromArgb(173, 181, 189);
                    }
                }

                if (view.IsRowSelected(e.RowHandle))
                {
                    e.Appearance.ForeColor = Color.Black;
                    e.Appearance.BackColor = Color.FromArgb(129, 236, 236);
                    // This property controls whether settings provided by the RowStyle event have a higher priority 
                    e.HighPriority = true;
                }
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Lỗi RowStyle: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void _btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private async void _btnComfim_Click(object sender, EventArgs e)
        {
            //using (var transaction = _dbContextDogeWH.Database.BeginTransaction())
            //{
            //    try
            //    {
            //        foreach (var item in _scaleDataFinal)
            //        {
            //            if (item.AllowScale && item.C023 == 0 && (item.C024 == 0 && item.C002.Substring(3) != "REX"))
            //            {
            //                MessageBox.Show($"You do not complete scale for the step: {item.C002}.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            //                return;
            //            }
            //        }

            //        var createdAt = DateTime.Now;
            //        //var createdBy
            //        var createdMachine = System.Environment.Machine;

            //        var dataInsert = _scaleDataFinal.Where(x => x.AllowScale == true).ToList();
            //        dataInsert.ForEach(x =>
            //        {
            //            x.id = Guid.NewGuid();
            //            x.C010 = _employeeCode;
            //            x.C011 = _employeeName;
            //            x.CreatedDate = createdAt;
            //            x.CreatedMachine = createdMachine;
            //            x.Mesocomp = _mesocomp;
            //            x.Mesoyear = _mesoYear;
            //        });

            //        await _dbContextDogeWH.FT600s.AddRangeAsync(dataInsert);

            //        await _dbContextDogeWH.FT601s
            //            .Where(b => b.C004 == _stepItemCodeScale.C004 && b.C007 == _stepItemCodeScale.C007 && b.C017 == false)
            //            .ExecuteUpdateAsync(s => s
            //                .SetProperty(b => b.C017, b => true)
            //                .SetProperty(b => b.ModifiedDate, b => createdAt)
            //                .SetProperty(b => b.ModifiedMachine, b => createdMachine)
            //            );

            //        await _dbContextDogeWH.SaveChangesAsync();

            //        // Commit the transaction
            //        await transaction.CommitAsync();

            //        MessageBox.Show("Lưu mẫu thành công.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);

            //        DialogResult = DialogResult.OK;
            //        this.Close();
            //    }
            //    catch (Exception ex)
            //    {
            //        // Rollback the transaction on error
            //        await transaction.RollbackAsync();
            //        MessageBox.Show($"Transaction rolled error: {ex.Message} - {ex.InnerException}", "LỖI", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //    }
            //}
        }

        private void _txtScaleValue_DataValueChanged(object sender, DataValueChangedEventArgs e)
        {
            var _data = e.NewValue.Value.ToString();

            _scaleValue = Math.Round(Convert.ToDouble(_data), 3);
        }

        private void _btnSaveWeight_Click(object sender, EventArgs e)
        {
            if (!_rowSelect.AllowScale)
            {
                MessageBox.Show("Can not scale this step.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_rowSelect.C002.Substring(0, 3) != "REX")
            {
                if (_rowSelect.C024 == 0)//cân lần đầu tiên, ghi nhận khối lượng của cả runner và part
                {
                    _rowSelect.C024 = _scaleValue;
                }
                else
                {
                    _rowSelect.C023 = _scaleValue;

                    //tính khối lượng runner
                    //_rowSelect.C022 = _toggleSwitchRunner.IsOn == true ? Math.Round(((double)(_rowSelect.C024 - (_rowSelect.C023 * (double)_rowSelect.C017)) / (double)_rowSelect.C017), 3) : 0;
                    _rowSelect.C022 = _toggleSwitchRunner.IsOn == true ? Math.Round(((double)(_rowSelect.C024 - (_rowSelect.C023 * (double)_articlePaisShotFinaly)) / (double)_articlePaisShotFinaly), 3) : 0;

                    //tính part weight
                    var previuosStep = _scaleDataFinal.Where(x => x.C015 == _rowSelect.C015 - 1).ToList();
                    var nonInjection = _scaleDataFinal.Where(x => x.C015 == _rowSelect.C015
                        && (x.C002 == "Z-VHXXXXXX" || x.C002.Substring(0, 3) == "REX")).ToList();

                    _rowSelect.C021 = _rowSelect.C023 - previuosStep?.Sum(x => x.C023) - nonInjection?.Sum(x => x.C023);
                }
            }
            else//Nếu là hàng non injection thì cân trọng lượng chính là partWeight.
            {
                _rowSelect.C023 = _scaleValue;
                _rowSelect.C021 = _scaleValue;
            }

            UpdateUI(false);

            //// render Add button                   
            //RenderAddButtonForGrid();
        }

        private void _txtRFIDCode_DataValueChanged(object sender, DataValueChangedEventArgs e)
        {
            try
            {
                _employeeCode = e.NewValue.Value.ToString();

                if (string.IsNullOrEmpty(_employeeCode))
                {
                    throw new Exception("The ID can not be null.");
                }

                using var dbContext = _dbFactory.CreateDbContext();
                _operatorInfo = dbContext.fT029_Operator_RFIDs.FirstOrDefault(x => x.C000.Contains(_employeeCode));

                if (_operatorInfo == null || _operatorInfo.Id == Guid.Empty)
                {
                    GlobalVariable.InvokeIfRequired(this, () => { _txtRFIDCode.Text = string.Empty; _txtRFIDName.Focus(); });
                    throw new Exception($"Employee ID {_employeeCode} was not found. You need to input the name and press the Enter key to register.");
                }

                _operatorInfo.DepartmentInfor = dbContext.FT031s.FirstOrDefault(x => x.Id == _operatorInfo.C002 &&
                        (x.C000 == "IT" || x.C000 == "QC")
                    );

                if (_operatorInfo.DepartmentInfor == null)
                {
                    _employeeCode = null;
                    GlobalVariable.InvokeIfRequired(this, () => { _txtRFIDName.Text = string.Empty; _txtRFIDCode.Text = string.Empty; });
                    throw new Exception($"Employee ID {_employeeCode} does not have permission to perform this function.");
                }

                GlobalVariable.InvokeIfRequired(this, () => { _txtRFIDName.Text = _operatorInfo.C001; });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in _txtRFIDCode_DataValueChanged");
                MessageBox.Show(ex.Message, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void _repositoryItemButtonEditScale_Click(object sender, EventArgs e)
        {
            try
            {
                ButtonEdit btn = sender as ButtonEdit;

                int handle = _grvTotalStep.FocusedRowHandle;
                var rowSelect = _grvTotalStep.GetRow(handle) as FT600;

                if (rowSelect != null && !rowSelect.AllowScale)
                {
                    MessageBox.Show("Do not allow to scale this step.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                //Kiểm tra xem các bước trước nó đã cân chưa
                var previousSteps = _scaleDataFinal.Where(x => x.C015 < rowSelect.C015).ToList();
                foreach (var step in previousSteps)
                {
                    if (step.C021 == 0)
                    {

                        MessageBox.Show("The previous step has not been weighed.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                _rowSelect = rowSelect;

                UpdateUI(true);
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            finally
            {

            }
        }

        #endregion

        #region Methods
        private void UpdateUI(bool refresh = true)
        {
            GlobalVariable.InvokeIfRequired(this, () =>
            {
                _lkStepCode.Text = _rowSelect?.C002;
                _scanBarcode.Text = _rowSelect?.C003;
                _txtMachine.Text = _rowSelect?.C004;
                _txtSize.Text = _rowSelect?.C008;
                _txtStepIndex.Text = _rowSelect?.C015.ToString();
                _txtMoldPairShot.Text = _rowSelect?.C018.ToString();
                _txtActiclePairShot.Text = _rowSelect.C028 == 0 ? _rowSelect?.C017.ToString() : _rowSelect.C028.ToString();
                _txtArticle.Text = _rowSelect?.C005;
                _txtQty.Text = _rowSelect?.C025.ToString();
                _txtFgItemCode.Text = _rowSelect?.C013;

                if (refresh)
                {
                    _grvTotalStep.RefreshData();
                }
                else
                {
                    _grcTotalStep.DataSource = null;
                    _grcTotalStep.DataSource = _scaleDataFinal;
                    _grvTotalStep.PopulateColumns();
                    _grvTotalStep.BestFitColumns();
                }

                if (NewScale)
                    RenderGridButton();
            });
        }

        private void RenderAddButtonForGrid()
        {
            //Kiem tra xem user nay co quyen dat nhua tu form Hydra ko
            // create GridColumn for btnAdd            
            GridColumn commandsColumn = _grvTotalStep.Columns.AddField("Scale");
            commandsColumn.UnboundType = DevExpress.Data.UnboundColumnType.Object;
            commandsColumn.Visible = true;
            commandsColumn.Width = 70;
            commandsColumn.ColumnEdit = _repositoryItemButtonEditScale;

            _grvTotalStep.Columns["Scale"].VisibleIndex = 0;

            // always show EditButton
            _grvTotalStep.OptionsView.ShowButtonMode = DevExpress.XtraGrid.Views.Base.ShowButtonModeEnum.ShowAlways;
            //SetEditableGridViewColumn("Operation");

            // btn fit in cell
            _repositoryItemButtonEditScale.TextEditStyle = TextEditStyles.HideTextEditor;
            _repositoryItemButtonEditScale.ButtonsStyle = BorderStyles.Flat;
            _repositoryItemButtonEditScale.AllowFocused = false;
        }

        private void RenderGridButton()
        {
            // Ensure _buttonEdit is initialized
            if (_buttonEdit == null)
            {
                _buttonEdit = new RepositoryItemButtonEdit();
            }

            // Ensure "ActionColumn" exists
            if (_grvTotalStep.Columns["ActionColumn"] == null)
            {
                GridColumn actionColumn = _grvTotalStep.Columns.AddField("ActionColumn");
                actionColumn.UnboundType = DevExpress.Data.UnboundColumnType.Object;
                actionColumn.Visible = true;
                actionColumn.Width = 100;
                actionColumn.Caption = "Actions";
            }

            // Configure buttons
            _buttonEdit.Buttons.Clear();

            EditorButton btnScale = new EditorButton(ButtonPredefines.Glyph)
            {
                ImageOptions = { Image = Properties.Resources.scale_30 },
                ToolTip = "Scale"
            };
            _buttonEdit.Buttons.Add(btnScale);

            EditorButton btnReset = new EditorButton(ButtonPredefines.Glyph)
            {
                ImageOptions = { Image = Properties.Resources.eraser_30 },
                ToolTip = "Reset"
            };
            _buttonEdit.Buttons.Add(btnReset);

            _buttonEdit.TextEditStyle = TextEditStyles.HideTextEditor;

            // Assign the button editor to the column
            _grvTotalStep.Columns["ActionColumn"].ColumnEdit = _buttonEdit;
            _grvTotalStep.Columns["ActionColumn"].VisibleIndex = 0;

            // Add the button editor to the grid's repository items
            if (!_grcTotalStep.RepositoryItems.Contains(_buttonEdit))
            {
                _grcTotalStep.RepositoryItems.Add(_buttonEdit);
            }
        }
        #endregion       
    }
}
