using AutoUpdaterDotNET;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Mvvm.POCO;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraPrinting.Native;
using DevExpress.XtraSplashScreen;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ScanAndScale.Helper;
using SSSW.models;
using SSSW.modelss;
using System.DirectoryServices.ActiveDirectory;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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
        private Label titleText;
        private Button btnClose;
        private Button btnMaximize;
        private Button btnMinimize;
        private Button btnUpdateVersion;
        private Button btnReload;
        private Button btnViewHistoryScale;
        private Button btnGetDataHydra;
        private Button btnDisableSize;

        /// <summary>
        /// List model chứa tất cả các item lấy từ Hydra về, được truyền từ master vào.
        /// </summary>
        public List<FT601> _dataHydra { get; set; } = new();

        /// <summary>
        /// List chứa thông tin các size chạy chung 1 khuôn.
        /// Để chứa các size liên quan để lưu chung 1 lần với khối lượng cân.
        /// vì với trường hợp 1 khuôn nhiều size thì nó sản xuất hàng clear, rồi màu sau khi ép xong sẽ mang đi sơn, nên khi tạo đơn trên Hydra sẽ có thông số màu.
        /// </summary>
        private List<FT601> _dataHydraMultiSizeOfMold { get; set; } = new();

        private bool _newScale = true;
        /// <summary>
        /// Bước được chọn để vào cân, được truyền từ master vào, và ta dựa vào thông tin này để tạo ra bộ data cân tương ứng.
        /// </summary>
        public FT601 _stepItemCodeScale { get; set; } = new FT601();

        private bool isUpdateClicked = false;

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
        /// chứa các bước cân của bước hiện tại, và các bước trước đó đã cân, đây chính là model dùng để cân, và lưu DB.
        /// </summary>
        private List<FT600> _scaleDataFinal = new();

        /// <summary>
        /// model để chứa step nào được chọn để cân.
        /// </summary>
        private FT600 _rowSelected = new();

        private double _scaleValue = 0;
        private string _mesocomp = string.Empty;
        private int _mesoYear = 0;

        RepositoryItemButtonEdit _buttonEdit = new RepositoryItemButtonEdit();

        private bool _isRunner { get; set; } = true;

        /// <summary>
        /// Số đôi thực tế trên khuôn, dùng để xác định để tính trọng lượng Runner.
        /// </summary>
        private double? _articlePaisShotFinaly = 0;

        /// <summary>
        /// mặc định sẽ bằng 100%, nhưng với hàng vải là Non-wovwn và mesh thì tùy trường hợp, thì sẽ dùng hết hay dùng 1 phần.
        /// </summary>
        private double _percentOfUsage = 0;

        private string? _remarkFinal = string.Empty;

        private List<StepSelectModel> _allStepCodeMaster = new List<StepSelectModel>();
        //private StepSelectModel _stepCodeMasterSelect = new StepSelectModel();

        /// <summary>
        /// Bước được chọn ban đầu khi scan QR code hoặc chọn tay ở lookupedit step code.
        /// </summary>
        public FT601 _stepSelected = new FT601();

        /// <summary>
        /// chứa thông tin của QR code khi quét.
        /// </summary>
        private string _qrCodeScan = string.Empty;

        /// <summary>
        /// chứa thông tin của label query từ thong tin đọc từ QR
        /// </summary>
        private FT606_Label _labelInfo = new FT606_Label();

        private string _employeeCode = string.Empty, _employeeName = string.Empty;
        private FT029_Operator_RFID _operatorInfo = new FT029_Operator_RFID();//chua thông tin của operator, để xem có đúng phòng ban đc phép dùng tính năng này hay ko.

        private List<HydraItemDetailModel> _hydraItemDetails = new List<HydraItemDetailModel>();
        #endregion

        //inject services
        private readonly IServiceProvider _serviceProvider;
        private readonly IDbContextFactory<DbContextDogeWH> _dbFactory;
        private readonly ILogger<frmShotWeightScale> _logger;

        // Nguồn hủy chung cho mỗi lần tải
        private CancellationTokenSource _loadCts;

        // Field trong lớp form (ở trên cùng của class)
        private DevExpress.XtraEditors.SimpleButton btnCancel;

        // Gọi hàm này trong constructor, sau InitializeComponent()
        private void InitCancelButton()
        {
            btnCancel = new DevExpress.XtraEditors.SimpleButton
            {
                Name = "btnCancel",
                Text = "Hủy tải",
                Enabled = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnCancel.Size = new Size(80, 28);
            btnCancel.Location = new Point(this.ClientSize.Width - btnCancel.Width - 10, 10);
            btnCancel.Click += btnCancel_Click;
            this.Controls.Add(btnCancel);
        }

        // Ví dụ: nút Cancel (nếu muốn cho phép người dùng hủy)
        private void btnCancel_Click(object sender, EventArgs e)
        {
            _loadCts?.Cancel();
        }

        // Hàm bạn cần: tải dữ liệu + overlay
        private async Task LoadDataAsync(
            Control overlayTarget = null,
            TimeSpan? timeout = null,
            DevExpress.XtraEditors.SimpleButton cancelButton = null) // <= truyền nút nếu có
        {
            overlayTarget ??= this;
            timeout ??= TimeSpan.FromSeconds(30);

            _loadCts?.Cancel();
            _loadCts?.Dispose();
            _loadCts = new CancellationTokenSource();

            using var timeoutCts = new CancellationTokenSource(timeout.Value);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_loadCts.Token, timeoutCts.Token);
            var token = linkedCts.Token;

            IOverlaySplashScreenHandle overlay = null;

            try
            {
                overlay = DevExpress.XtraSplashScreen.SplashScreenManager.ShowOverlayForm(overlayTarget);

                if (cancelButton != null)
                    cancelButton.Enabled = true;

                // *** TẢI DỮ LIỆU BẤT ĐỒNG BỘ ***
                var data = await Task.Run(async () =>
                {
                    // Mô phỏng công việc nặng: DB/API
                    // Hãy kiểm tra token để dừng sớm khi bị hủy:
                    token.ThrowIfCancellationRequested();

                    // TODO: thay bằng repo thực tế
                    using var dbContext = _dbFactory.CreateDbContext();
                    var result = await dbContext.FT601s.Where(x => x.C021 == true).ToListAsync(); // truyền token vào repo nếu có
                                                                                                  //get all data master
                    return result;

                }, token);

                //// Cập nhật UI sau khi tải xong
                _dataHydra = data;
                _allStepCodeMaster = _dataHydra.Select(x => new StepSelectModel()
                {
                    StepItemCode = x.C004,
                    StepItemName = x.C005,
                    Size = x.C002,
                    ArticlePairsShot = x.C013,
                    MoldPairsShot = x.C014,
                    Machine = x.C015,
                    HydraOrderNo = x.C018
                }).Distinct().ToList();

                GlobalVariable.InvokeIfRequired(this, () =>
                {
                    _lkStepCode.Properties.DataSource = null;
                    _lkStepCode.Properties.DataSource = _allStepCodeMaster;
                    _lkStepCode.Properties.DisplayMember = "StepItemCode";
                    _lkStepCode.Properties.ValueMember = "StepItemCode";
                    _lkStepCode.Properties.PopulateColumns();


                });
            }
            catch (OperationCanceledException)
            {
                // bị hủy (user/timeout) -> có thể im lặng
            }
            catch (Exception ex)
            {
                DevExpress.XtraEditors.XtraMessageBox.Show(this,
                    $"Load data failure:\n{ex.Message}", "Load data",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (overlay != null)
                    DevExpress.XtraSplashScreen.SplashScreenManager.CloseOverlayForm(overlay);

                if (cancelButton != null)
                    cancelButton.Enabled = false;
            }
        }

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
            btnClose.MouseEnter += (s, e) => btnClose.BackColor = Color.Green;
            btnClose.MouseLeave += (s, e) => btnClose.BackColor = Color.Black;
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
            btnMaximize.MouseEnter += (s, e) => btnMaximize.BackColor = Color.Green;
            btnMaximize.MouseLeave += (s, e) => btnMaximize.BackColor = Color.Black;
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
            btnMinimize.MouseEnter += (s, e) => btnMinimize.BackColor = Color.Green;
            btnMinimize.MouseLeave += (s, e) => btnMinimize.BackColor = Color.Black;
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
            tip.AutoPopDelay = 3000;     // hiển thị tối đa 5 giây
            tip.InitialDelay = 300;      // trễ 300ms
            tip.ReshowDelay = 100;       // xuất hiện lại nhanh
            tip.ShowAlways = true;       // luôn hiển thị tooltip
            tip.SetToolTip(btnUpdateVersion, "Click to update version");  // nội dung tooltip

            // Tùy chọn: hiệu ứng hover (đổi nền cho dễ nhìn)
            //btnUpdateVersion.MouseEnter += (s, e) => btnUpdateVersion.BackColor = Color.FromArgb(30, 30, 30);
            btnUpdateVersion.MouseEnter += (s, e) => btnUpdateVersion.BackColor = Color.Green;
            btnUpdateVersion.MouseLeave += (s, e) => btnUpdateVersion.BackColor = Color.Black;

            // Sự kiện Click (giữ nguyên như bạn đã có)
            btnUpdateVersion.Click += BtnUpdateVersion_Click; ; // hoặc sự kiện update version thực tế của bạn
            titleBar.Controls.Add(btnUpdateVersion);

            // Nút update reload massterdata
            btnReload = new Button();
            btnReload.Text = "";                      // Không cần chữ, chỉ hiển thị icon
            btnReload.ForeColor = Color.White;
            btnReload.BackColor = Color.Black;
            btnReload.FlatStyle = FlatStyle.Flat;
            btnReload.FlatAppearance.BorderSize = 0;
            btnReload.Size = new Size(40, 40);
            btnReload.Location = new Point(this.Width - 200, 0);
            btnReload.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnReload.Cursor = Cursors.Hand;

            // 1) Gán icon từ Resources (đặt tên hình là "updateVersion" như trong Resource)
            btnReload.Image = Properties.Resources.reload_white_30;  // PNG từ Resources
            btnReload.ImageAlign = ContentAlignment.MiddleCenter;  // căn giữa
            btnReload.Padding = new Padding(0);                    // tránh lệch
            btnReload.TextImageRelation = TextImageRelation.Overlay; // chỉ icon

            // Tùy chọn: scale icon nếu quá lớn/nhỏ (WinForms Button không có ImageLayout)
            // => bạn có thể dùng phiên bản icon 24x24 hoặc 32x32 trong file PNG để vừa với nút 40x40.

            // 2) Tooltip khi hover
            tip = new ToolTip();
            tip.AutoPopDelay = 3000;     // hiển thị tối đa 5 giây
            tip.InitialDelay = 300;      // trễ 300ms
            tip.ReshowDelay = 100;       // xuất hiện lại nhanh
            tip.ShowAlways = true;       // luôn hiển thị tooltip
            tip.SetToolTip(btnReload, "Click to reload master data");  // nội dung tooltip

            // Tùy chọn: hiệu ứng hover (đổi nền cho dễ nhìn)
            //btnUpdateVersion.MouseEnter += (s, e) => btnUpdateVersion.BackColor = Color.FromArgb(30, 30, 30);
            btnReload.MouseEnter += (s, e) => btnReload.BackColor = Color.Green;
            btnReload.MouseLeave += (s, e) => btnReload.BackColor = Color.Black;

            // Sự kiện Click (giữ nguyên như bạn đã có)
            btnReload.Click += async (s, args) => await btnReload_Click(s, args); // hoặc sự kiện update version thực tế của bạn
            titleBar.Controls.Add(btnReload);

            // Nút update reload hoistory scale
            btnViewHistoryScale = new Button();
            btnViewHistoryScale.Text = "";                      // Không cần chữ, chỉ hiển thị icon
            btnViewHistoryScale.ForeColor = Color.White;
            btnViewHistoryScale.BackColor = Color.Black;
            btnViewHistoryScale.FlatStyle = FlatStyle.Flat;
            btnViewHistoryScale.FlatAppearance.BorderSize = 0;
            btnViewHistoryScale.Size = new Size(40, 40);
            btnViewHistoryScale.Location = new Point(this.Width - 240, 0);
            btnViewHistoryScale.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnViewHistoryScale.Cursor = Cursors.Hand;

            // 1) Gán icon từ Resources (đặt tên hình là "updateVersion" như trong Resource)
            btnViewHistoryScale.Image = Properties.Resources.activity_history_30_White;  // PNG từ Resources
            btnViewHistoryScale.ImageAlign = ContentAlignment.MiddleCenter;  // căn giữa
            btnViewHistoryScale.Padding = new Padding(0);                    // tránh lệch
            btnViewHistoryScale.TextImageRelation = TextImageRelation.Overlay; // chỉ icon

            // Tùy chọn: scale icon nếu quá lớn/nhỏ (WinForms Button không có ImageLayout)
            // => bạn có thể dùng phiên bản icon 24x24 hoặc 32x32 trong file PNG để vừa với nút 40x40.

            // 2) Tooltip khi hover
            tip = new ToolTip();
            tip.AutoPopDelay = 3000;     // hiển thị tối đa 5 giây
            tip.InitialDelay = 300;      // trễ 300ms
            tip.ReshowDelay = 100;       // xuất hiện lại nhanh
            tip.ShowAlways = true;       // luôn hiển thị tooltip
            tip.SetToolTip(btnViewHistoryScale, "Click to reload the history scale data");  // nội dung tooltip

            // Tùy chọn: hiệu ứng hover (đổi nền cho dễ nhìn)
            //btnUpdateVersion.MouseEnter += (s, e) => btnUpdateVersion.BackColor = Color.FromArgb(30, 30, 30);
            btnViewHistoryScale.MouseEnter += (s, e) => btnViewHistoryScale.BackColor = Color.Green;
            btnViewHistoryScale.MouseLeave += (s, e) => btnViewHistoryScale.BackColor = Color.Black;

            // Sự kiện Click (giữ nguyên như bạn đã có)
            btnViewHistoryScale.Click += async (s, args) => await btnViewHistoryScale_Click(s, args); // hoặc sự kiện update version thực tế của bạn
            titleBar.Controls.Add(btnViewHistoryScale);

            // Nút update get data from hydra
            btnGetDataHydra = new Button();
            btnGetDataHydra.Text = "";                      // Không cần chữ, chỉ hiển thị icon
            btnGetDataHydra.ForeColor = Color.White;
            btnGetDataHydra.BackColor = Color.Black;
            btnGetDataHydra.FlatStyle = FlatStyle.Flat;
            btnGetDataHydra.FlatAppearance.BorderSize = 0;
            btnGetDataHydra.Size = new Size(40, 40);
            btnGetDataHydra.Location = new Point(this.Width - 280, 0);
            btnGetDataHydra.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnGetDataHydra.Cursor = Cursors.Hand;

            // 1) Gán icon từ Resources (đặt tên hình là "updateVersion" như trong Resource)
            btnGetDataHydra.Image = Properties.Resources.icons8_big_data_30_white;  // PNG từ Resources
            btnGetDataHydra.ImageAlign = ContentAlignment.MiddleCenter;  // căn giữa
            btnGetDataHydra.Padding = new Padding(0);                    // tránh lệch
            btnGetDataHydra.TextImageRelation = TextImageRelation.Overlay; // chỉ icon

            // Tùy chọn: scale icon nếu quá lớn/nhỏ (WinForms Button không có ImageLayout)
            // => bạn có thể dùng phiên bản icon 24x24 hoặc 32x32 trong file PNG để vừa với nút 40x40.

            // 2) Tooltip khi hover
            tip = new ToolTip();
            tip.AutoPopDelay = 3000;     // hiển thị tối đa 5 giây
            tip.InitialDelay = 300;      // trễ 300ms
            tip.ReshowDelay = 100;       // xuất hiện lại nhanh
            tip.ShowAlways = true;       // luôn hiển thị tooltip
            tip.SetToolTip(btnGetDataHydra, "Click to get master data from Hydra.");  // nội dung tooltip

            // Tùy chọn: hiệu ứng hover (đổi nền cho dễ nhìn)
            //btnUpdateVersion.MouseEnter += (s, e) => btnUpdateVersion.BackColor = Color.FromArgb(30, 30, 30);
            btnGetDataHydra.MouseEnter += (s, e) => btnGetDataHydra.BackColor = Color.Green;
            btnGetDataHydra.MouseLeave += (s, e) => btnGetDataHydra.BackColor = Color.Black;

            // Sự kiện Click (giữ nguyên như bạn đã có)
            btnGetDataHydra.Click += async (s, args) => await btnGetDataHydra_Click(s, args); // hoặc sự kiện update version thực tế của bạn
            titleBar.Controls.Add(btnGetDataHydra);

            // Nút disable size
            btnDisableSize = new Button();
            btnDisableSize.Text = "";                      // Không cần chữ, chỉ hiển thị icon
            btnDisableSize.ForeColor = Color.White;
            btnDisableSize.BackColor = Color.Black;
            btnDisableSize.FlatStyle = FlatStyle.Flat;
            btnDisableSize.FlatAppearance.BorderSize = 0;
            btnDisableSize.Size = new Size(40, 40);
            btnDisableSize.Location = new Point(this.Width - 320, 0);
            btnDisableSize.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnDisableSize.Cursor = Cursors.Hand;

            // 1) Gán icon từ Resources (đặt tên hình là "updateVersion" như trong Resource)
            btnDisableSize.Image = Properties.Resources.icons8_installing_updates_30_white;  // PNG từ Resources
            btnDisableSize.ImageAlign = ContentAlignment.MiddleCenter;  // căn giữa
            btnDisableSize.Padding = new Padding(0);                    // tránh lệch
            btnDisableSize.TextImageRelation = TextImageRelation.Overlay; // chỉ icon

            // Tùy chọn: scale icon nếu quá lớn/nhỏ (WinForms Button không có ImageLayout)
            // => bạn có thể dùng phiên bản icon 24x24 hoặc 32x32 trong file PNG để vừa với nút 40x40.

            // 2) Tooltip khi hover
            tip = new ToolTip();
            tip.AutoPopDelay = 3000;     // hiển thị tối đa 5 giây
            tip.InitialDelay = 300;      // trễ 300ms
            tip.ReshowDelay = 100;       // xuất hiện lại nhanh
            tip.ShowAlways = true;       // luôn hiển thị tooltip
            tip.SetToolTip(btnDisableSize, "Click to get disable size on master data.");  // nội dung tooltip

            // Tùy chọn: hiệu ứng hover (đổi nền cho dễ nhìn)
            //btnUpdateVersion.MouseEnter += (s, e) => btnUpdateVersion.BackColor = Color.FromArgb(30, 30, 30);
            btnDisableSize.MouseEnter += (s, e) => btnDisableSize.BackColor = Color.Green;
            btnDisableSize.MouseLeave += (s, e) => btnDisableSize.BackColor = Color.Black;

            // Sự kiện Click (giữ nguyên như bạn đã có)
            btnDisableSize.Click += async (s, args) => await btnDisableSize_Click(s, args); // hoặc sự kiện update version thực tế của bạn
            titleBar.Controls.Add(btnDisableSize);

            ///////////////////////////////////////////////////////////////////////////////////
            // Đảm bảo tất cả có cùng Height = 30 và Y = 5
            btnClose.Size = btnMaximize.Size = btnMinimize.Size = btnUpdateVersion.Size = btnReload.Size = btnViewHistoryScale.Size = btnGetDataHydra.Size = btnDisableSize.Size = new Size(30, 30);
            // Anchor cho cả 3 nút
            btnClose.Anchor = btnMaximize.Anchor = btnMinimize.Anchor = btnUpdateVersion.Anchor = btnReload.Anchor = btnViewHistoryScale.Anchor = btnGetDataHydra.Anchor = btnDisableSize.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            // Logo
            PictureBox logo = new PictureBox();
            logo.Image = Properties.Resources.framas_white; // logo từ Resources
            logo.SizeMode = PictureBoxSizeMode.Zoom;
            logo.Size = new Size(100, 30); // kích thước logo
            logo.Location = new Point(10, 5); // vị trí bên trái
            titleBar.Controls.Add(logo);

            // Text
            titleText = new Label();
            titleText.Text = $"Unknown - SSSW Station ver:{Application.ProductVersion}";
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
        private async void AutoUpdater_ApplicationExitEvent()
        {
            Text = @"Closing application...";
            await System.Threading.Tasks.Task.Delay(3000);
            Application.Exit();
        }
        private async void AutoUpdater_CheckForUpdateEvent(UpdateInfoEventArgs args)
        {
            if (args.IsUpdateAvailable)
            {
                DialogResult dialogResult;
                dialogResult =
                        MessageBox.Show(
                            $@"SSSW has new version: {args.CurrentVersion}. Would you like upgrade it?", @"Information",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Information);

                if (dialogResult.Equals(DialogResult.Yes) || dialogResult.Equals(DialogResult.OK))
                {
                    SplashScreenManager.ShowForm(typeof(WaitForm1));
                    await System.Threading.Tasks.Task.Delay(3000);
                    //AutoZipFolder();

                    try
                    {
                        if (AutoUpdater.DownloadUpdate(args))
                        {
                            SplashScreenManager.CloseForm(false);
                            Application.Exit();
                        }
                        else
                        {
                            SplashScreenManager.ShowForm(typeof(WaitForm1));
                        }
                    }
                    catch (Exception exception)
                    {
                        SplashScreenManager.CloseForm(false);
                        MessageBox.Show(exception.Message, exception.GetType().ToString(), MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                if (isUpdateClicked)
                {
                    MessageBox.Show(@"SSSW up to date version.", @"Information",
                   MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private async void FrmShotWeightScale_Load(object? sender, EventArgs e)
        {
            AutoUpdater.RunUpdateAsAdmin = false;
            AutoUpdater.DownloadPath = Environment.CurrentDirectory;

            AutoUpdater.ApplicationExitEvent += AutoUpdater_ApplicationExitEvent;
            AutoUpdater.CheckForUpdateEvent += AutoUpdater_CheckForUpdateEvent;

            _txtActiclePairShot.Focus();

            using var dbContext = _dbFactory.CreateDbContext();
            _mesocomp = dbContext.Database.SqlQueryRaw<string>($"sp_MaterialGetCompanyName").AsEnumerable().FirstOrDefault();
            _mesoYear = dbContext.Database.SqlQueryRaw<int>($"sp_MaterialGetMesoyear").AsEnumerable().FirstOrDefault();

            var location = _mesocomp == "VNT1" ? "fVN" :
                          _mesocomp == "FKV" ? "fKV" :
                          _mesocomp == "FTT1" ? "fFT" :
                          _mesocomp == "05FI" ? "fIN" :
                          _mesocomp == "fGE" ? "fGE" : "Unknown";

            if (Enum.TryParse<EnumLocation>(location, ignoreCase: true, out var loc))
            {
                titleText.Text = $"{loc} - SSSW Station";
            }
            _labVer.Text = Application.ProductVersion.Split('+')[0];
            //else
            //{
            //    // dữ liệu không hợp lệ → fallback
            //    titleText.Text = $"Unknown - SSSW Station";
            //}

            //get config
            var configDaTa = await dbContext.FT608s.FirstOrDefaultAsync(x => x.c000 == Environment.MachineName);
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

            //Call master data overlay
            //// Có nút:
            //await LoadDataAsync(this, TimeSpan.FromSeconds(30), btnCancel);
            // Không có nút (truyền null hoặc bỏ hẳn tham số):
            await LoadDataAsync(this, TimeSpan.FromSeconds(30));

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

            _scanBarcode.Config = GlobalVariable.ConfigSystem.Scanner;
            _txtRFIDCode.Config = GlobalVariable.ConfigSystem.RFID;
            scaleButtonEdit1.Config = GlobalVariable.ConfigSystem.Scale;
            scaleButtonEdit1.EnableReadScale = (bool)GlobalVariable.ConfigSystem.EnableReadScale;

            _scanBarcode.DataValueChanged += _scanBarcode_DataValueChanged;

            _txtRFIDCode.DataValueChanged += _txtRFIDCode_DataValueChanged;
            _txtRFIDName.KeyDown += async (s, args) => await _txtRFIDName_KeyDownAsync(s, args);

            scaleButtonEdit1.DataValueChanged += scaleButtonEdit1_DataValueChanged;
            _btnSaveWeight.Click += _btnSaveWeight_Click;
            _btnConfirm.Click += async (s, agrs) => await _btnComfim_Click(s, agrs);
            _btnCancel.Click += _btnCancel_Click;

            _txtActiclePairShot.KeyDown += (s, ev) =>
            {
                if (ev.KeyCode == Keys.Enter)
                {
                    _articlePaisShotFinaly = double.TryParse(_txtActiclePairShot.EditValue.ToString(), out double value) ? value : 0;

                    _scaleDataFinal.ForEach(x => x.C028 = _articlePaisShotFinaly);
                }
            };
            _txtPercentOFusageNonwoven.EditValue = "0";

            _txtRemark.EditValueChanged += (s, ev) =>
            {
                _remarkFinal = _txtRemark.EditValue?.ToString() ?? string.Empty;
                _scaleDataFinal?.ForEach(x => x.C038 = _remarkFinal);
            };
            //nhập thông tin phần trăm sử dụng cho các item FG có sử dụng vải Nonwoven và Mesh.
            _txtPercentOFusageNonwoven.KeyDown += (s, ev) =>
            {
                if (ev.KeyCode == Keys.Enter)
                {
                    //kiểm tra nếu item hiện tại không phải REX hoặc không phải hàng vải Nonwoven và Mesh thì không cần xử lý.
                    var catCheck = GlobalVariable.ConfigSystem.CategoryOfNonInjectionUsagePartial
                                        .FirstOrDefault(x => x.CategoryCode == _rowSelected.C033);
                    if (!(_rowSelected?.C002?.StartsWith("REX") ?? false) ||
                        ((_rowSelected?.C002?.StartsWith("REX") ?? false) && catCheck == null)
                    ) return;

                    _percentOfUsage = double.TryParse(_txtPercentOFusageNonwoven.EditValue?.ToString(), out double value) ? value : 0;

                    //cập nhật lại cho tất cả các item trong danh sách cân.
                    _scaleDataFinal?.Where(x => x.C002.StartsWith("REX")).ForEach(x =>
                    {
                        var usage = x.C035 == 100 ?
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
                }
            };
            _txtPercentOFusageNonwoven.EditValue = GlobalVariable.ConfigSystem.PercentOfUserNonWoven;
            _percentOfUsage = GlobalVariable.ConfigSystem.PercentOfUserNonWoven;

            // Đặt text khi bật/tắt
            _comboBoxEditIsRunner.Properties.Items.AddRange(new[] { "YES", "NO" });
            _comboBoxEditIsRunner.SelectedIndex = 0; // chọn mặc định


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
            _rowSelected = _scaleDataFinal.FirstOrDefault(x => x.C002 == _stepItemCodeScale.C004 && x.C015 == _stepItemCodeScale.C010);
            //_rowSelected = _scaleDataFinal.FirstOrDefault(x => x.C002 == _stepItemCodeScale.StepItemCode && x.C015 == _stepItemCodeScale.StepIndex);

            //// render Add button                   
            //RenderAddButtonForGrid();

            //Check If scanned then disable buttons.
            // CheckEnableScale();

            _lkStepCode.Properties.DataSource = _allStepCodeMaster;
            _lkStepCode.Properties.DisplayMember = "StepItemName";
            _lkStepCode.Properties.ValueMember = "StepItemCode";
            _lkStepCode.Properties.PopulateColumns();

            _lkStepCode.ButtonClick += Properties_ButtonClick;

            _lkStepCode.EditValueChanged += async (s, ev) => await _lkStepCode_EditValueChangedAsync(s, ev);

            _txtPercentOFusageNonwoven.Enabled = false;
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
            scaleButtonEdit1.DataValueChanged -= scaleButtonEdit1_DataValueChanged;
            _btnSaveWeight.Click -= _btnSaveWeight_Click;
            _btnConfirm.Click -= async (s, args) => await _btnComfim_Click(s, args);
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
            try
            {
                isUpdateClicked = true;
                string UUrl = GlobalVariable.ConfigSystem.UpdatePath;
                SplashScreenManager.ShowForm(typeof(WaitForm1));
                System.Threading.Thread.Sleep(3000);
                AutoUpdater.Start(UUrl);
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"{ex.Message}", "Error");
            }
            finally
            {
                SplashScreenManager.CloseForm(false);
            }
        }

        private async Task btnReload_Click(object sender, EventArgs e)
        {
            await LoadDataAsync(this, TimeSpan.FromSeconds(30));
        }

        private async Task btnViewHistoryScale_Click(object sender, EventArgs e)
        {
            var nf = _serviceProvider.GetRequiredService<frmMainView>();
            nf.StartPosition = FormStartPosition.CenterParent;
            nf.MaximizeBox = true;
            nf.WindowState = FormWindowState.Maximized;
            nf.ShowDialog(this);
        }

        private async Task btnGetDataHydra_Click(object sender, EventArgs e)
        {
            await GetDataHydra();
        }

        private async Task btnDisableSize_Click(object sender, EventArgs e)
        {
            var nf = _serviceProvider.GetRequiredService<frmUpdateMasterData>();
            nf.StartPosition = FormStartPosition.CenterParent;
            nf.MaximizeBox = true;
            nf.WindowState = FormWindowState.Maximized;
            nf.ShowDialog(this);

            //reload the master data.
            await LoadDataAsync(this, TimeSpan.FromSeconds(30));
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
            try
            {
                //get cac thong tin lien quan den nguye lieu
                _qrCodeScan = e.NewValue.Value.ToString();

                using var dbContext = _dbFactory.CreateDbContext();


                _labelInfo = await dbContext.FT606s.FirstOrDefaultAsync(x => x.c001 == _qrCodeScan);

                if (_labelInfo == null)
                {
                    throw new Exception("The label information was not found.");
                }

                _stepSelected = _dataHydra.FirstOrDefault(x => x.Id == _labelInfo.c000);

                if (_stepSelected == null)
                {
                    throw new Exception("The step information was not found.");
                }

                //if (_stepSelected.C005)
                //{

                //}
                _lkStepCode.EditValue = _stepSelected.C004;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in _scanBarcode_DataValueChanged");
                MessageBox.Show(ex.Message, "WARNING", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                this.Focus();
            }
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
                var editor = sender as DevExpress.XtraEditors.LookUpEdit;
                if (editor == null) return;

                // 1) Giá trị (ValueMember)
                var selectedValue = editor.EditValue; // kiểu object, thường là string/int tùy nguồn dữ liệu

                // 2) Text hiển thị (DisplayMember)
                var selectedText = editor.Text;

                // 3) (Tuỳ chọn) Lấy toàn bộ row dữ liệu đang chọn
                // Với LookUpEdit:
                var _stepCodeMasterSelect = (StepSelectModel)editor.GetSelectedDataRow(); // DataRowView hoặc object (tuỳ DataSource)
                                                                                          // Với GridLookUpEdit:
                                                                                          // var gridEditor = sender as GridLookUpEdit;
                                                                                          // var row = gridEditor.Properties.View.GetFocusedRow();
                if (_stepCodeMasterSelect == null)
                {
                    _labelInfo = new FT606_Label();
                    ResetNewLoop();
                    return;
                }                                                                      //truy vấn master data để lấy thông tin FG code

                //kiểm tra nếu qr code null thì mới vào xử lý để lấy ra thông tin bước  trong master để vào lấy các bước, còn nếu có qrcode nghĩa là đã scan từ tem thì đã có thông tin rồi nên không cần làm bước này nữa.
                if (string.IsNullOrEmpty(_qrCodeScan))
                {
                    _stepSelected = _dataHydra.FirstOrDefault(x => x.C004 == _stepCodeMasterSelect.StepItemCode &&
                        x.C015 == _stepCodeMasterSelect.Machine &&
                        x.C018 == _stepCodeMasterSelect.HydraOrderNo
                        );

                    if (_stepSelected == null)
                    {
                        throw new Exception("The step was not found, please recheck master data.");
                    }
                }

                // Ví dụ: cập nhật các control khác
                await GetDataAsync(_stepSelected);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in _lkStepCode_EditValueChangedAsync");
                MessageBox.Show(ex.Message, "WARNING", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ResetNewLoop()
        {
            _rowSelected = new FT600();
            _allStepsFG = new List<BomWinlineModel>();
            _stepItemCodeScale = new FT601();
            _scaleData = new List<FT600>();
            _scaleDataFinal = new List<FT600>();
            //_stepCodeMasterSelect = new StepSelectModel();
            _newScale = true;
            _qrCodeScan = string.Empty;

            _percentOfUsage = GlobalVariable.ConfigSystem.PercentOfUserNonWoven;
            _articlePaisShotFinaly = 0;
            _qrCodeScan = string.Empty;
            _remarkFinal = string.Empty;

            GlobalVariable.InvokeIfRequired(this, () =>
            {
                _comboBoxEditIsRunner.SelectedIndex = 0; // chọn mặc định
            });

            UpdateUI(false);
        }

        private void ResetAll()
        {
            //_stepCodeMasterSelect = new StepSelectModel();

        }

        private async Task GetDataAsync(FT601 stepCode)
        {
            try
            {
                using var dbContextDogeWH = _dbFactory.CreateDbContext();

                if (_scaleDataFinal.Count() == 0)
                {
                    ResetNewLoop();

                    #region Lấy data của các bước cân
                    //Lấy ra tất cả các bước chạy theo FG item code để kiểm tra thứ tự cân có đúng theo thứ tự của các bước chạy hay không.
                    _stepItemCodeScale = await dbContextDogeWH.FT601s.Where(x => x.C007 == stepCode.C007).FirstOrDefaultAsync();

                    if (_stepItemCodeScale == null)
                    {
                        throw new Exception($"Step item code {stepCode.C007} not found in the SSSW systems, please check again.");
                    }

                    //get thông tin BOM từ Winline
                    _allStepsFG = await dbContextDogeWH.Database.SqlQueryRaw<BomWinlineModel>("sp_getBomWinlineOfItemFG @itemFG = {0}", stepCode.C007)
                        .AsNoTracking().ToListAsync();

                    foreach (var item in _allStepsFG)
                    {
                        var line = new FT600();
                        bool allowScale = true;

                        //kiểm tra các bước cân của itemFG đưuọc plan trên Hydra, chỉ cho phép cân các steps thuộc kế hoạch này.
                        FT601 ckHydra = new();
                        ckHydra = _dataHydra.FirstOrDefault(x => x.C004 == item.ItemStepCode && x.C007 == item.ItemFgCode);

                        if (ckHydra == null)
                        {
                            if (item.ItemStepCode != "Z-VHXXXXXX" && item.ItemStepCode.Substring(0, 3) != "REX")
                            {

                                var mc = item.ItemFgCode.Split('-')[0];
                                var smc = item.ItemStepCode.Split('-')[1];
                                ckHydra = _dataHydra.FirstOrDefault(x => x.C007.Contains($"{mc}-") || (x.C004.Contains($"-{smc}-")));
                                //ckHydra = _dataHydra.FirstOrDefault(x => x.FGItemCode.Contains($"{mc}-") || (x.StepItemCode.Contains($"-{smc}-")));

                                allowScale = false;
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
                            }
                        }

                        line.id = Guid.NewGuid();
                        line.C000 = ckHydra?.C000;
                        line.C001 = ckHydra?.C000 != "21" && _stepItemCodeScale.C000 != "22" ? EnumSampleLocation.Production : EnumSampleLocation.Sample;
                        line.C004 = ckHydra?.C015;
                        line.C005 = ckHydra?.C006;
                        line.C006 = ckHydra?.C011;
                        line.C007 = ckHydra?.C012;
                        line.C009 = 1;
                        line.C012 = ckHydra.Id == _labelInfo.c000 ? _labelInfo?.c001 : null;
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
                        line.C025 = item.Quantity;
                        line.C026 = ckHydra?.C020;
                        line.C027 = ckHydra?.C003;
                        line.C028 = ckHydra?.C013 != null ? (int)ckHydra.C013 : 0;
                        line.C029 = ckHydra.Id == _labelInfo.c000 ? _labelInfo.Id : null;
                        line.C032 = ckHydra?.Id;
                        line.C033 = item.CategoryCode;
                        line.C034 = item.CategoryName;
                        line.C035 = _percentOfUsage;
                        line.C036 = 0;
                        line.C037 = item.Unit;
                        line.AllowScale = allowScale;

                        _scaleData.Add(line);
                    }

                    #region Kiểm tra trường hợp nhiều size trên 1 khuôn thì xử lý theo logic đó
                    var dataSize = new List<FT600>();
                    foreach (var item in _scaleData)
                    {
                        // Tiền tố (2 cụm đầu) của item.C004
                        var itemPrefix2 = GlobalVariable.PrefixUpToSecondHyphen(item.C002);

                        //lọc trong master data ra theo MoldId,machine và khác step với size hiện tại.
                        var sameMolds = _dataHydra.Where(x =>
                                x.C019 == item.C020 && //MoldId
                                x.C015 == item.C004 &&//Machine
                                x.C004 != item.C002 &&//Step khác với size hiện tại
                                x.C002 != item.C008 &&//size
                                GlobalVariable.PrefixUpToSecondHyphen(x.C004) == itemPrefix2 &&
                                x.C010 == item.C015 //Step index WL
                            ).DistinctBy(x => x.C002).ToList();

                        if (sameMolds == null || (sameMolds != null && sameMolds.Count == 0))
                            continue;

                        //get category từ bom winline
                        var itemList = string.Join(",", sameMolds.Select(x => x.C004));
                        var category = await dbContextDogeWH.Database.SqlQueryRaw<CategoryOfItemModel>("sp_GetCategorryOfItem @ItemCode = {0}",
                            itemList)
                            .AsNoTracking().ToListAsync();

                        foreach (var itemNultiSize in sameMolds)
                        {
                            //get category gán vào
                            var catItem = category.FirstOrDefault(x => x.ItemCode == itemNultiSize.C004);

                            dataSize.Add(new FT600()
                            {
                                id = Guid.NewGuid(),
                                C000 = itemNultiSize?.C000,
                                C001 = itemNultiSize?.C000 != "21" && itemNultiSize?.C000 != "22" ? EnumSampleLocation.Production : EnumSampleLocation.Sample,
                                C004 = itemNultiSize?.C015,
                                C005 = itemNultiSize?.C006,
                                C006 = itemNultiSize?.C011,
                                C007 = itemNultiSize?.C012,
                                C009 = 1,
                                C012 = _labelInfo != null ? _labelInfo.c001 : null,
                                C013 = itemNultiSize?.C007,
                                C014 = itemNultiSize?.C008,
                                C016 = null,
                                C017 = itemNultiSize?.C013,
                                C018 = itemNultiSize?.C014,
                                C019 = itemNultiSize?.C016,
                                C020 = itemNultiSize?.C019,

                                C002 = itemNultiSize?.C004,
                                C003 = itemNultiSize?.C005,
                                C008 = itemNultiSize?.C002,
                                C015 = itemNultiSize?.C010,
                                //                       
                                C021 = 0, // Khối lượng sẽ được cập nhật sau khi cân
                                C022 = 0,
                                C023 = 0,
                                C024 = 0,
                                C025 = item.C025,
                                C026 = itemNultiSize?.C020,
                                C027 = itemNultiSize?.C003,
                                C028 = itemNultiSize?.C013 != null ? (int)itemNultiSize.C013 : 0,
                                C029 = _labelInfo != null ? _labelInfo.Id : null,
                                C032 = itemNultiSize?.Id,
                                C033 = catItem.CategoryCode,
                                C034 = catItem.CategoryName,
                                C035 = _percentOfUsage,
                                C037 = catItem.Unit,
                                AllowScale = true,
                            });
                        }
                    }

                    var itemsToAdd = dataSize.Where(ds => !_scaleData.Any(sd => sd.C002 == ds.C002 && sd.C015 == ds.C015)
                                                  ).ToList();

                    _scaleData.AddRange(itemsToAdd);
                    #endregion

                    _scaleDataFinal = _scaleData.OrderBy(x => x.C015).ThenBy(x => x.C027).ToList();

                    #region doc thông tin các bước đã cân
                    //var stepHasScale = await dbContextDogeWH.FT600s
                    //    .Where(x => x.C013 == _stepItemCodeScale.C007)//.Where(x => x.C013 == _stepItemCodeScale.FGItemCode)
                    //    .ToListAsync();

                    foreach (var item in _scaleDataFinal)
                    {
                        var catCheck = GlobalVariable.ConfigSystem.CategoryOfNonInjectionUsagePartial.FirstOrDefault(x => x.CategoryCode == item.C033);

                        if (item.C002.Substring(0, 3) == "REX")
                        {
                            var firstNotNull = _scaleDataFinal.Where(x => x.C015 == item.C015 && !string.IsNullOrEmpty(x.C026));
                            var mainItemCode = firstNotNull?.FirstOrDefault().C026;
                            var mainItemName = firstNotNull?.FirstOrDefault().C027;

                            var paisShotFinal = catCheck == null ?
                                firstNotNull.FirstOrDefault().C028 :
                                firstNotNull.Sum(x => x.C028);

                            item.C026 = mainItemCode;
                            item.C027 = mainItemName;
                            item.C028 = paisShotFinal;
                            item.C017 = firstNotNull?.FirstOrDefault().C017 ?? 0;
                            item.C018 = firstNotNull?.FirstOrDefault().C018 ?? 0;
                        }

                        //nếu ko có khối lượng cân, thì cần lấy thông tin cân theo group size
                        string mainCode = string.Empty;
                        FT600 stepPrevious = new();

                        if (item.C002 != "Z-VHXXXXXX" && item.C002.Substring(0, 3) != "REX")
                        {
                            mainCode = item.C002.Split('-')[1];

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
                               .Where(x => x.C002 == item.C002 //&& x.C015 == item.C015
                                   )
                               .OrderByDescending(x => x.CreatedDate).FirstOrDefaultAsync();

                            //không cho cân lại đối với non-injection đã có khối lượng
                            //if (stepPrevious == null)
                            //    item.AllowScale = true;
                        }

                        if (!item.C003.StartsWith("Stud") && !item.C003.StartsWith("Logo") && !item.C003.StartsWith("Cleat_Ring") && !item.C002.StartsWith("REX"))
                            continue;

                        if (stepPrevious != null)
                        {
                            _percentOfUsage = (double)stepPrevious.C035;

                            if (item.C002.StartsWith("REX"))
                            {
                                var total = catCheck == null ? stepPrevious?.C036 * item.C025 : stepPrevious?.C036;
                                var usage = (double)Math.Round((decimal)(total * _percentOfUsage / 100), 3);
                                var unusage = total - usage;

                                item.C021 = catCheck == null ? usage : usage / item.C028; // Part Weight (g) of step.
                                item.C022 = catCheck == null ? unusage : unusage / item.C028; // Runner weight (g) of step.
                                item.C023 = catCheck == null ? usage : usage / item.C028; // Total scale value of part weight (include these previous step), scale value.
                                item.C024 = total; // Total weight of step injection (include runner + part), Scale value.
                                item.C035 = stepPrevious.C035;
                                item.C036 = stepPrevious?.C036;
                            }
                            else
                            {
                                item.C021 = stepPrevious.C021; // Part Weight (g) of step.
                                item.C022 = stepPrevious.C022; // Runner weight (g) of step.
                                item.C023 = stepPrevious.C023; // Total scale value of part weight (include these previous step), scale value.
                                item.C024 = stepPrevious.C024; // Total weight of step injection (include runner + part), Scale value.
                                item.C028 = stepPrevious.C028;//Actual pái shot
                                item.C035 = stepPrevious.C035;//Percent of usage
                                item.C036 = stepPrevious?.C036;//
                            }
                        }
                    }
                    #endregion

                    _scaleDataFinal = _scaleDataFinal.OrderBy(x => x.C015).ToList();

                    //chọn bước để cân
                    var stepSelected = _scaleDataFinal.FirstOrDefault(x => x.C002 == _stepSelected.C004);

                    //Kiểm tra xem các bước trước nó đã cân chưa
                    var previousSteps = _scaleDataFinal.Where(x => x.C015 < stepSelected?.C015).ToList();

                    if (previousSteps?.FirstOrDefault(x => x.C021 == 0) != null)
                    {
                        MessageBox.Show("The previous step has not been weighed.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    else
                        _rowSelected = _scaleDataFinal.FirstOrDefault(x => x.C002 == _stepSelected.C004);
                    #endregion
                }
                else
                {
                    var rowSelect = _scaleDataFinal.FirstOrDefault(x => x.C002 == _stepSelected.C004 && x.C013 == _stepSelected.C007 && x.C004 == _stepSelected.C015);

                    if (rowSelect == null)
                    {
                        MessageBox.Show($"The label does not match the item being weighed.{Environment.NewLine}{_stepSelected.C004}|{_stepSelected.C005}", "WARNING", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

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
                    rowSelect.C012 = _labelInfo != null ? _labelInfo.c001 : null;
                    rowSelect.C029 = _labelInfo != null ? _labelInfo.Id : null;
                    _rowSelected = rowSelect;
                }

                _articlePaisShotFinaly = _articlePaisShotFinaly == 0 ? (int)_scaleDataFinal.FirstOrDefault().C028 : _articlePaisShotFinaly;

                UpdateUI(false);

                if (!string.IsNullOrEmpty(_rowSelected.C002))
                    FocusRowByStepCode(_grvTotalStep, "C002", _rowSelected.C002);
                // Tuỳ ý: clear text sau khi xử lý
                // txtQRCode.EditValue = null;
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

            if (e.Button.Index == 0) // Nút "Cân"
            {
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

                _rowSelected = rowSelect;
                _articlePaisShotFinaly = _articlePaisShotFinaly == 0 ? _rowSelected.C017 : _articlePaisShotFinaly;
            }
            else if (e.Button.Index == 1) // Nút "Reset"
            {
                //MessageBox.Show("Đã nhấn nút Reset cho dòng: " + rowHandle);
                // Gọi hàm reset ở đây
                var rowReset = _scaleDataFinal.FirstOrDefault(x => x.AllowScale == true && x.C002 == _rowSelected.C002);

                if (rowReset == null)
                {
                    MessageBox.Show("Can not reset this line.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                rowReset.C021 = 0;
                rowReset.C022 = 0;
                rowReset.C023 = 0;
                rowReset.C024 = 0;

                _rowSelected = rowSelect;
                _articlePaisShotFinaly = _articlePaisShotFinaly == 0 ? _rowSelected.C017 : _articlePaisShotFinaly;
            }
            else
            {
                _scaleDataFinal.Remove(rowSelect);
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
            _labelInfo = new FT606_Label();
            _scanBarcode = null;
            _qrCodeScan = string.Empty;
            _remarkFinal = string.Empty;

            GlobalVariable.InvokeIfRequired(this, () =>
            {
                _lkStepCode.EditValue = null;
                _scanBarcode.Text = string.Empty;
            });

            ResetNewLoop();
        }

        private async Task _btnComfim_Click(object sender, EventArgs e)
        {
            using var dbContext = _dbFactory.CreateDbContext();
            using var transaction = await dbContext.Database.BeginTransactionAsync();

            try
            {
                if (_operatorInfo == null || _operatorInfo.Id == Guid.Empty)
                {
                    MessageBox.Show($"RFID card not yet scanned.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                #region get nhieu size treen khuon
                // Tiền tố (2 cụm đầu) của item.C004
                var itemPrefix2 = GlobalVariable.PrefixUpToSecondHyphen(_rowSelected.C002);
                //lọc trong danh sách các item đang cân ra theo MoldId,machine và khác step với size hiện tại.
                var sameMolds = _scaleDataFinal.Where(x =>
                        x.C020 == _rowSelected.C020 && //MoldId
                        x.C004 == _rowSelected.C004 &&//Machine
                                                      x.C002 != _rowSelected.C002 &&//Step khác với size hiện tại
                                                      x.C008 != _rowSelected.C008 &&//size
                        GlobalVariable.PrefixUpToSecondHyphen(x.C002) == itemPrefix2 &&
                        x.C015 == _rowSelected.C015
                    ).ToList();
                #endregion

                if (sameMolds.Count == 0)
                {
                    foreach (var item in _scaleDataFinal)
                    {
                        if (item.AllowScale && item.C023 == 0 && (item.C024 == 0 && item.C002.StartsWith("REX")))
                        {
                            MessageBox.Show($"You do not complete scale for the step: {item.C002}.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                }
                else
                {

                }

                var createdAt = DateTime.Now;
                var createdMachine = Environment.MachineName;

                var dataInsert = _scaleDataFinal.Where(x => x.AllowScale == true && x.C021 > 0).ToList();
                dataInsert.ForEach(x =>
                {
                    //x.id = Guid.NewGuid();
                    x.C010 = _operatorInfo.C000;
                    x.C011 = _operatorInfo.C001;
                    x.CreatedDate = createdAt;
                    x.CreatedMachine = createdMachine;
                    x.Mesocomp = _mesocomp;
                    x.Mesoyear = _mesoYear;
                });

                await dbContext.FT600s.AddRangeAsync(dataInsert);

                await dbContext.FT601s
                    .Where(b => b.C004 == _stepItemCodeScale.C004 && b.C007 == _stepItemCodeScale.C007 && b.C017 == false)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(b => b.C017, b => true)
                        .SetProperty(b => b.ModifiedDate, b => createdAt)
                        .SetProperty(b => b.ModifiedMachine, b => createdMachine)
                    );

                await dbContext.SaveChangesAsync();

                // Commit the transaction
                await transaction.CommitAsync();

                MessageBox.Show("Scale shot weight success.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);

                //reset để bắt đầu 1 chu trình mới.
                _labelInfo = new FT606_Label();
                GlobalVariable.InvokeIfRequired(this, () =>
                {
                    _lkStepCode.EditValue = null;
                });

                ResetNewLoop();
            }
            catch (Exception ex)
            {
                // Rollback the transaction on error
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error in _btnComfim_Click - Transaction rolled back");
                MessageBox.Show($"Transaction rolled error: {ex.Message} - {ex.InnerException}", "LỖI", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void scaleButtonEdit1_DataValueChanged(object sender, DataValueChangedEventArgs e)
        {
            var _data = e.NewValue.Value.ToString();

            _scaleValue = Math.Round(Convert.ToDouble(_data), 2);


        }

        private void _btnSaveWeight_Click(object sender, EventArgs e)
        {
            if (_rowSelected == null) return;

            if (!_rowSelected.AllowScale)
            {
                MessageBox.Show("Can not scale this step.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!_rowSelected.C002.StartsWith("REX"))
            {
                if (_rowSelected.C024 == 0)//cân lần đầu tiên, ghi nhận khối lượng của cả runner và part
                {
                    _rowSelected.C024 = _scaleValue;
                }
                else
                {
                    _rowSelected.C023 = _scaleValue;

                    //tính khối lượng runner
                    var prsShot = _articlePaisShotFinaly;

                    _rowSelected.C022 = _comboBoxEditIsRunner.Text == "YES" ? Math.Round(
                        ((double)(_rowSelected.C024 - (_rowSelected.C023 * (double)prsShot)) / (double)prsShot), 3) : 0;

                    //tính part weight
                    var previuosStep = _scaleDataFinal.Where(x => x.C015 == _rowSelected.C015 - 1).ToList();
                    var nonInjection = _scaleDataFinal.Where(x => x.C015 == _rowSelected.C015
                        && (x.C002 == "Z-VHXXXXXX" || x.C002.StartsWith( "REX"))).ToList();

                    _rowSelected.C021 = _rowSelected.C023 - previuosStep?.Sum(x => x.C023) - nonInjection?.Sum(x => x.C023);

                    //tính trọng lượng trên mỗi pieces
                    _rowSelected.C036 = _rowSelected.C003.StartsWith("Studs") || _rowSelected.C003.StartsWith("Logo") || _rowSelected.C003.StartsWith("Cleat_Ring") ?
                        Math.Round(_scaleValue / (double)prsShot, 2) :
                        0;
                }
            }
            else//Nếu là hàng non injection thì cân trọng lượng chính là partWeight.
            {
                var catCheck = GlobalVariable.ConfigSystem.CategoryOfNonInjectionUsagePartial.FirstOrDefault(x => x.CategoryCode == _rowSelected.C033);

                if (catCheck == null)//Receptacle
                {
                    _rowSelected.C024 = _scaleValue;
                    _rowSelected.C023 = _scaleValue;
                    _rowSelected.C021 = _scaleValue;

                    //tính trọng lượng trên mỗi đôi
                    _rowSelected.C036 = Math.Round(_scaleValue / (double)_rowSelected.C025, 2);
                }
                else//nonwoven/mesh
                {
                    var usage = _rowSelected.C035 == 100 ?
                        (double)Math.Round((decimal)(_scaleValue * _percentOfUsage / 100) / (decimal)_rowSelected.C028, 3) :
                        (double)Math.Round((decimal)((decimal)(_scaleValue * _percentOfUsage / 100) / (decimal)_rowSelected.C028), 3);

                    var unusage = (_scaleValue - usage * _rowSelected.C028) / _rowSelected.C028;

                    _rowSelected.C024 = _scaleValue;
                    _rowSelected.C023 = usage;
                    _rowSelected.C021 = usage;
                    _rowSelected.C022 = unusage;

                    //tính trọng lượng trên mỗi đôi
                    _rowSelected.C036 = Math.Round(usage / 2, 2);
                }
            }

            #region get nhieu size treen khuon
            // Tiền tố (2 cụm đầu) của item.C004
            var itemPrefix2 = GlobalVariable.PrefixUpToSecondHyphen(_rowSelected.C002);
            //lọc trong danh sách các item đang cân ra theo MoldId,machine và khác step với size hiện tại.
            var sameMolds = _scaleDataFinal.Where(x =>
                    x.C020 == _rowSelected.C020 && //MoldId
                    x.C004 == _rowSelected.C004 &&//Machine
                                                  //x.C002 != _rowSelected.C002 &&//Step khác với size hiện tại
                                                  //x.C008 != _rowSelected.C008 &&//size
                    GlobalVariable.PrefixUpToSecondHyphen(x.C002) == itemPrefix2 &&
                    x.C015 == _rowSelected.C015
                ).ToList();

            if (sameMolds.Count > 1)
            {
                var sumPartWeight = sameMolds.Sum(x => x.C023 * x.C028);
                var pairShot = sameMolds.Sum(x => x.C028);

                foreach (var item in sameMolds)
                {
                    item.C024 = _rowSelected.C024;
                    item.C022 = item.C023 > 0 ?
                       (_rowSelected.C024 - sumPartWeight) / pairShot :
                        0;
                }
            }
            #endregion

            //cập nhật lại giá trị cân cho các bước sau bước hiện tại khi bước hiện tại bị thay đổi khối lượng.
            var stepNeedToUpdate = _scaleDataFinal.Where(x => x.C015 >= _rowSelected.C015 && x.C024 > 0 &&
                    !x.C003.StartsWith("Stud") &&
                    !x.C003.StartsWith("Inlay") &&
                    !x.C003.StartsWith("Ring") &&
                    !x.C002.StartsWith("REX") &&
                    x.C002 != _rowSelected.C002 &&
                    GlobalVariable.PrefixUpToSecondHyphen(x.C002) != itemPrefix2
                )
                .OrderBy(x => x.C015)
                .ToList();

            foreach (var item in stepNeedToUpdate)
            {
                if (item.C015 == _rowSelected.C015)
                {
                    item.C021 = item.C023 - _rowSelected.C023;
                    item.C022 = item.C024 - item.C021;
                }
                else
                {
                    //lấy các bước trước nó đã cân
                    var previousSteps = stepNeedToUpdate.FirstOrDefault(x => x.C015 < item.C015 - 1);

                    item.C021 = item.C023 - previousSteps?.C023;
                    item.C022 = item.C024 - item.C021;
                }
            }

            UpdateUI(false);

            FocusRowByStepCode(_grvTotalStep, "C002", _rowSelected.C002);
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

                _rowSelected = rowSelect;

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
                _txtStepCode.Text = _rowSelected?.C002;
                //_lkStepCode.Text = _rowSelected?.C002;
                _txtMachine.Text = _rowSelected?.C004;
                _txtSize.Text = _rowSelected?.C008;
                _txtStepIndex.Text = _rowSelected?.C015.ToString();
                _txtMoldPairShot.Text = _rowSelected?.C018.ToString();
                _txtActiclePairShot.Text = _articlePaisShotFinaly.ToString();
                _txtArticle.Text = _rowSelected?.C005;
                _txtQty.Text = _rowSelected?.C025.ToString();
                _txtFgItemCode.Text = _rowSelected?.C013;
                _txtFGName.Text = _rowSelected?.C014;
                _txtPercentOFusageNonwoven.EditValue = _rowSelected?.C035 != 0 ? _rowSelected?.C035 : _percentOfUsage;
                _txtRemark.Text = _remarkFinal;

                //kiểm tra nếu bước cân non injection là non-woven và mesh thì enanle text nhập phần trăm sử dụng lên để tính toán
                var catChceck = GlobalVariable.ConfigSystem.CategoryOfNonInjectionUsagePartial.Where(x => x.CategoryCode == _rowSelected.C033).FirstOrDefault();
                if (_rowSelected.C002?.Substring(0, 3) == "REX" && catChceck != null)
                {
                    _txtPercentOFusageNonwoven.Enabled = true;
                }
                else
                {
                    _txtPercentOFusageNonwoven.Enabled = false;
                }

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

                    _grvTotalStep.Columns[nameof(FT600.C010)].Visible = false;
                    _grvTotalStep.Columns[nameof(FT600.C011)].Visible = false;
                    _grvTotalStep.Columns[nameof(FT600.C029)].Visible = false;
                    _grvTotalStep.Columns[nameof(FT600.C032)].Visible = false;
                    _grvTotalStep.Columns[nameof(FT600.C012)].Visible = false;
                    _grvTotalStep.Columns[nameof(FT600.C030)].Visible = false;
                    _grvTotalStep.Columns[nameof(FT600.C031)].Visible = false;
                }

                if (_newScale)
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

            EditorButton btnDelete = new EditorButton(ButtonPredefines.Glyph)
            {
                ImageOptions = { Image = Properties.Resources.delete_30_black },
                ToolTip = "Delete"
            };
            _buttonEdit.Buttons.Add(btnDelete);

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

        async Task GetDataHydra()
        {
            //CancellationTokenSource cts = new CancellationTokenSource();
            //var currentForm = (this is Form) ? this : this.FindForm();
            //var handle = currentForm.ShowWaitingOverlay(cts);
            try
            {
                //await Task.Delay(5000);
                using var dbContextDogeWH = _dbFactory.CreateDbContext();

                //get thông tin tồn kho từ Winline
                _hydraItemDetails = await dbContextDogeWH.Database.SqlQueryRaw<HydraItemDetailModel>("sp_GetFullStepItemHydraIsRun").AsNoTracking().ToListAsync();

                _hydraItemDetails = _hydraItemDetails
                    .OrderBy(x => x.FGItemCode).ThenBy(x => x.StepIndex)
                    .ToList();

                //var reader = await connection.ExecuteReaderAsync("sp_GetFullStepItemHydraIsRun", commandType: CommandType.StoredProcedure);

                //DataTable dt = new DataTable();
                //dt.Load(reader);

                //_hydraItemDetails = ConvertDataTableToList.Instance.ConvertDataTable<HydraItemDetailModel>(dt);

                if (_hydraItemDetails.Count > 0)
                {
                    #region Insert data to FT601
                    var ft601s = new List<FT601>();

                    var dataInsert1 = _hydraItemDetails.Where(d =>
                                                                !dbContextDogeWH.FT601s.Any(ft601 =>
                                                                    ft601.C004 == d.StepItemCode &&
                                                                    ft601.C015 == d.Machine &&
                                                                    ft601.C018 == d.OrderHydraNum &&
                                                                    ft601.Actived == true
                                                                )
                                                            ).ToList();

                    //insert the new data hydra to FT601
                    var CreateAt = DateTime.Now;
                    var createMachine = Environment.MachineName;

                    foreach (var item in dataInsert1)
                    {
                        var ft601 = new FT601()
                        {
                            Id = Guid.NewGuid(),
                            C000 = item.HydraOrderType,
                            C001 = item.Location == "Sample" ? EnumSampleLocation.Sample : EnumSampleLocation.Production,
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
                            CreatedMachine = createMachine,
                            CreatedDate = CreateAt,
                            Mesoyear = item.MesoYear,
                            Mesocomp = item.MesoComp
                        };
                        ft601s.Add(ft601);
                    }

                    if (ft601s.Count > 0)
                    {
                        await dbContextDogeWH.FT601s.AddRangeAsync(ft601s);
                        await dbContextDogeWH.SaveChangesAsync();
                    }
                    #endregion
                }
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
            {
                // In ra thông tin lỗi chi tiết nhất từ inner exception
                var innerException = ex.InnerException;
                if (innerException != null)
                {
                    _logger.LogError(innerException, "Database update error in GetDataHydra");
                }
                throw; // Re-throw để chương trình không chạy tiếp
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetDataHydra");
            }
            finally
            {
                //SplashScreenManager.CloseOverlayForm(handle);
            }
        }

        private void FocusRowByStepCode(GridView view, string stepCodeField, string stepCode)
        {
            if (view == null) return;

            view.BeginUpdate();
            try
            {
                // Xoá chọn cũ
                view.ClearSelection();

                // Cách 1: LocateByValue – tìm theo FieldName khớp hoàn toàn value
                int rowHandle = view.LocateByValue(stepCodeField, stepCode);

                // Nếu không tìm thấy, có thể thử "contains" (không phân biệt hoa thường)
                if (rowHandle < 0)
                {
                    rowHandle = FindRowContains(view, stepCodeField, stepCode);
                }

                if (rowHandle >= 0)
                {
                    view.FocusedRowHandle = rowHandle;
                    view.SelectRow(rowHandle);
                    view.MakeRowVisible(rowHandle, true);
                }
                else
                {
                    // Không thấy: có thể highlight bằng ApplyFindFilter
                    view.ApplyFindFilter(stepCode);
                    XtraMessageBox.Show($"The step '{stepCode}' was not found.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            finally
            {
                view.EndUpdate();
            }
        }

        private int FindRowContains(GridView view, string fieldName, string keyword)
        {
            keyword = (keyword ?? "").Trim();
            if (string.IsNullOrEmpty(keyword)) return -1;

            for (int i = 0; i < view.DataRowCount; i++)
            {
                int handle = view.GetVisibleRowHandle(i);
                if (handle < 0) continue;

                var val = view.GetRowCellValue(handle, fieldName)?.ToString();
                if (!string.IsNullOrEmpty(val) && val.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return handle;
                }
            }
            return -1;
        }
        #endregion
    }
}
