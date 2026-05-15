// ============================================================================
//  ShotWeightWindow.xaml.cs  –  Minimal code-behind (MVVM refactor)
//
//  Chỉ chứa những gì KHÔNG thể làm qua binding:
//    • Win32 drag (titlebar)
//    • Khởi tạo ScanAndScale.Core drivers (BarcodeDriver / RfidDriver / ScaleDriver)
//    • Event bridges từ Core drivers → ViewModel (marshal về UI thread)
//    • DevExpress LookUpEdit EditValueChanged → ViewModel
//    • KeyDown handlers (tbActualPairs, tbUsagePct, tbRFIDName) → ViewModel
//    • Title-bar button Click handlers
//    • DataGrid helper: scroll + select row theo step code
//
//  Mọi business logic đã chuyển sang ShotWeightViewModel.cs
//  Namespace : SSSW.UI.WPF
// ============================================================================
using DevExpress.Xpf.Editors;
using ScanAndScale.Core.Drivers;
using ScanAndScale.Core.Models;
using SSSW.models;
using SSSW.modelss;
using SSSW.UI.WPF.ViewModels;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
// ── Disambiguate WPF vs WinForms types ──────────────────────────────────────
using KeyEventArgs              = System.Windows.Input.KeyEventArgs;
using TextBox                   = System.Windows.Controls.TextBox;
using SelectionChangedEventArgs = System.Windows.Controls.SelectionChangedEventArgs;

namespace SSSW.UI.WPF
{
    public partial class ShotWeightWindow : Window
    {
        // ── Win32 – borderless drag ──────────────────────────────────────────
        [DllImport("user32.dll")] private static extern bool ReleaseCapture();
        [DllImport("user32.dll")] private static extern int  SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        // ── ViewModel ────────────────────────────────────────────────────────
        private ShotWeightViewModel _vm = null!;

        // ── ScanAndScale.Core drivers (thay thế BarcodeButtonEdit / RFIDButtonEdit / ScaleButtonEdit) ──
        private readonly BarcodeDriver _barcodeDriver = BarcodeDriver.Instance;
        private readonly RfidDriver    _rfidDriver    = RfidDriver.Instance;
        private ScaleDriver?           _scaleDriver;

        // ════════════════════════════════════════════════════════════════════
        //  CONSTRUCTORS
        // ════════════════════════════════════════════════════════════════════

        /// <summary>DI constructor – nhận ViewModel từ DI container.</summary>
        public ShotWeightWindow(ShotWeightViewModel viewModel) : this()
        {
            _vm         = viewModel;
            DataContext = viewModel;
        }

        /// <summary>Parameterless ctor – yêu cầu bởi XAML (x:Class) và XAML designer.</summary>
        public ShotWeightWindow()
        {
            InitializeComponent();
            Loaded  += OnLoaded;
            Closing += OnClosing;
        }

        // ════════════════════════════════════════════════════════════════════
        //  LOADED
        // ════════════════════════════════════════════════════════════════════
        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_vm == null) return; // guard cho design-time preview

            // 1. Đăng ký events TRƯỚC khi Initialize để không bỏ sót status thay đổi
            _barcodeDriver.DataValueChanged += BarcodeDriver_DataValueChanged;
            _rfidDriver.DataValueChanged    += RfidDriver_DataValueChanged;

            // 2. Cấu hình View callbacks để ViewModel gọi lại View khi cần
            //    Ghi chú: ClearBarcodeAction / ClearRfidAction đã được VM khởi tạo
            //    trong constructor → tự xóa BarcodeScannedValue / RfidCardCode.
            //    Ở đây chỉ gán những callback liên quan đến WPF controls cụ thể.
            _vm.FocusRfidNameAction     = () => tbRFIDName.Focus();
            _vm.ClearStepComboAction    = () => { cbStepName.EditValue = null; };
            _vm.SetStepComboAction      = item => { cbStepName.EditValue = item?.StepItemCode; };
            _vm.FocusGridRowAction      = code => FocusStepInGrid(code);
            _vm.ApplyHardwareConfigAction = ApplyHardwareConfig;

            // 3. Khởi tạo ViewModel (load config DB + master data)
            await _vm.InitializeAsync();
        }

        // ════════════════════════════════════════════════════════════════════
        //  CLOSING
        // ════════════════════════════════════════════════════════════════════
        private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // Hủy đăng ký events → tránh memory leak
            _barcodeDriver.DataValueChanged -= BarcodeDriver_DataValueChanged;
            _rfidDriver.DataValueChanged    -= RfidDriver_DataValueChanged;
            if (_scaleDriver != null)
                _scaleDriver.DataValueChanged -= ScaleDriver_DataValueChanged;

            // Dispose drivers để giải phóng serial port, TCP socket
            _barcodeDriver.Dispose();
            _rfidDriver.Dispose();
            _scaleDriver?.Dispose();
            _scaleDriver = null;
        }

        // ════════════════════════════════════════════════════════════════════
        //  HARDWARE CONFIG
        //  Gọi từ ViewModel (ApplyHardwareConfigAction) sau khi load config từ DB.
        //  Map ScanAndScale.Driver config → ScanAndScale.Core config → Initialize.
        // ════════════════════════════════════════════════════════════════════
        private void ApplyHardwareConfig()
        {
            var cfg = GlobalVariable.ConfigSystem;

            // ── Barcode (Zebra CoreScanner SDK) ──────────────────────────────
            var barcodeCfg = new BarcodeConfig
            {
                Enable   = cfg.Scanner.Enable,
                ReadOnly = cfg.Scanner.ReadOnly
            };

            if (barcodeCfg.Enable)
            {
                bool ok = _barcodeDriver.Initialize(barcodeCfg);
                _vm.BarcodeStatus = ok ? DriverStatus.Connected : DriverStatus.Disconnected;
            }
            else
                _vm.BarcodeStatus = DriverStatus.Disconnected;

            // ── RFID (SerialPort / COM) ───────────────────────────────────────
            //  Map: Rfid_Com → ComPort, Rfid_AutoFindCom → AutoFindCom,
            //       Rfid_Caption → DeviceCaption, Rfid_Manufact → DeviceManufacturer
            var rfidCfg = new RfidConfig
            {
                Enable             = cfg.RFID.Enable,
                AutoFindCom        = cfg.RFID.Rfid_AutoFindCom,
                ComPort            = cfg.RFID.Rfid_Com,
                DeviceCaption      = cfg.RFID.Rfid_Caption,
                DeviceManufacturer = cfg.RFID.Rfid_Manufact
            };

            if (rfidCfg.Enable)
            {
                bool ok = _rfidDriver.Initialize(rfidCfg);
                _vm.RfidStatus = ok ? DriverStatus.Connected : DriverStatus.Disconnected;
            }
            else
                _vm.RfidStatus = DriverStatus.Disconnected;

            // ── Scale (TCP/IP) ────────────────────────────────────────────────
            //  Map: TimeScan → TimeScanMs (tên property khác nhau giữa hai thư viện)
            var scaleCfg = new ScaleConfig
            {
                Enable      = cfg.Scale.Enable == true,
                ReadOnly    = cfg.Scale.ReadOnly == true,
                IP          = cfg.Scale.IP,
                Port        = cfg.Scale.Port,
                TimeScanMs  = cfg.Scale.TimeScan,
                CalibZero   = cfg.Scale.CalibZero,
                CalibGain   = cfg.Scale.CalibGain,
                DecimalNum  = cfg.Scale.DecimalNum,
                ModelName   = cfg.Scale.ModelName,
                CheckStable = cfg.Scale.CheckStable == true,
                CheckTare   = cfg.Scale.CheckTare == true
            };

            bool enableScale = scaleCfg.Enable && (cfg.EnableReadScale ?? false);
            if (enableScale)
            {
                _scaleDriver = new ScaleDriver();
                _scaleDriver.DataValueChanged += ScaleDriver_DataValueChanged;
                _scaleDriver.Initialize(scaleCfg);
                // Connected/Disconnected sẽ được cập nhật khi event đầu tiên fire
            }
            else
                _vm.ScaleStatus = DriverStatus.Disconnected;
        }

        // ════════════════════════════════════════════════════════════════════
        //  CORE DRIVER EVENT BRIDGES
        //  ⚠️ Tất cả events có thể fire từ background thread.
        //     Phải Dispatcher.Invoke/InvokeAsync để cập nhật UI.
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// BarcodeDriver (Zebra SDK) → chạy trên thread của Zebra SDK.
        /// </summary>
        private async void BarcodeDriver_DataValueChanged(object? sender, DataValueChangedEventArgs e)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                var data = e.NewValue;
                _vm.BarcodeStatus = data.DriverStatus;

                if (data.IsValid)
                {
                    var barcode = data.Value?.ToString() ?? string.Empty;
                    _vm.BarcodeScannedValue = barcode;
                    _ = _vm.OnBarcodeScannedAsync(barcode);
                }
            });
        }

        /// <summary>
        /// RfidDriver (SerialPort.DataReceived) → chạy trên ThreadPool thread.
        /// </summary>
        private void RfidDriver_DataValueChanged(object? sender, DataValueChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                var data = e.NewValue;
                _vm.RfidStatus = data.DriverStatus;

                if (data.IsValid)
                {
                    var code = data.Value?.ToString() ?? string.Empty;
                    _vm.RfidCardCode = code;
                    _vm.OnRfidValueChanged(code);
                }
                else if (data.DriverStatus == DriverStatus.Disconnected)
                {
                    _vm.RfidCardCode = string.Empty;
                }
            });
        }

        /// <summary>
        /// ScaleDriver (Timer.Elapsed / TCP) → chạy trên ThreadPool thread.
        /// </summary>
        private void ScaleDriver_DataValueChanged(object? sender, DataValueChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                var data = e.NewValue;
                _vm.ScaleStatus = data.DriverStatus;

                if (data.DriverStatus == DriverStatus.Connected && sender is ScaleDriver sd)
                {
                    _vm.OnScaleValueChanged(
                        value:  Convert.ToDouble(data.Value ?? 0.0),
                        stable: sd.IsStable,
                        tare:   sd.IsTare,
                        unit:   sd.Unit
                    );
                }
                else if (data.DriverStatus == DriverStatus.Disconnected)
                {
                    _vm.OnScaleValueChanged(0, false, false, "KG");
                }
            });
        }

        // ════════════════════════════════════════════════════════════════════
        //  DEVEXPRESS LOOKUPERDIT – STEP SELECTOR
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Bridge: DevExpress LookUpEdit không support Command binding,
        /// nên dùng event handler để chuyển lên ViewModel.
        /// </summary>
        private async void cbStepName_EditValueChangedAsync(object sender, EditValueChangedEventArgs e)
        {
            if (_vm == null) return;

            var editVal  = cbStepName.EditValue?.ToString();
            var selected = string.IsNullOrEmpty(editVal)
                ? null
                : _vm.StepCodeMaster.FirstOrDefault(x => x.StepItemCode == editVal);

            await _vm.OnStepSelectedAsync(selected);
        }

        // ════════════════════════════════════════════════════════════════════
        //  TEXTBOX KEYDOWN BRIDGES
        // ════════════════════════════════════════════════════════════════════

        private void tbActualPairs_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && sender is TextBox tb)
                _vm?.OnActualPairsEnter(tb.Text);
        }

        private void tbUsagePct_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && sender is TextBox tb)
                _vm?.OnUsagePctEnter(tb.Text);
        }

        private async void tbRFIDName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && sender is TextBox tb && _vm != null)
                await _vm.OnRfidNameEnterAsync(tb.Text);
        }

        // ════════════════════════════════════════════════════════════════════
        //  DATAGRID EVENTS
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// SelectionChanged trên dgTotalSteps – không cần xử lý vì các hành động
        /// (Scale / Reset) đã qua Command binding trong XAML.
        /// </summary>
        private void dgTotalSteps_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // No-op: hành động qua GridScaleCommand / GridResetCommand (XAML)
        }

        // ════════════════════════════════════════════════════════════════════
        //  HISTORY HEADER CLICK (toggle expand/collapse)
        // ════════════════════════════════════════════════════════════════════

        private void HistoryHeader_Click(object sender, MouseButtonEventArgs e)
            => _vm?.ToggleHistory();

        // ════════════════════════════════════════════════════════════════════
        //  TITLE BAR
        // ════════════════════════════════════════════════════════════════════

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                ReleaseCapture();
                SendMessage(
                    new System.Windows.Interop.WindowInteropHelper(this).Handle,
                    0x0112,   // WM_SYSCOMMAND
                    0xF012,   // SC_DRAGMOVE
                    0);
            }
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
            => WindowState = WindowState.Minimized;

        private void btnMaximize_Click(object sender, RoutedEventArgs e)
            => WindowState = WindowState == WindowState.Normal
               ? WindowState.Maximized
               : WindowState.Normal;

        private void btnClose_Click(object sender, RoutedEventArgs e)
            => Close();

        // ════════════════════════════════════════════════════════════════════
        //  HELPER – Focus + scroll dgTotalSteps tới row có step code tương ứng
        // ════════════════════════════════════════════════════════════════════
        private void FocusStepInGrid(string? stepCode)
        {
            if (string.IsNullOrEmpty(stepCode)) return;

            var item = dgTotalSteps.Items
                .OfType<FT600>()
                .FirstOrDefault(x => x.C002 == stepCode);

            if (item == null) return;

            dgTotalSteps.ScrollIntoView(item);
            dgTotalSteps.SelectedItem = item;
        }
    }
}
