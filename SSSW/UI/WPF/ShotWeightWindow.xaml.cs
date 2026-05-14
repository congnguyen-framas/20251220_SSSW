// ============================================================================
//  ShotWeightWindow.xaml.cs  –  Minimal code-behind (MVVM refactor)
//
//  Chỉ chứa những gì KHÔNG thể làm qua binding:
//    • Win32 drag (titlebar)
//    • WindowsFormsHost attachment cho BarcodeButtonEdit / RFIDButtonEdit / ScaleButtonEdit
//    • Event bridges từ hardware controls → ViewModel
//    • DevExpress LookUpEdit EditValueChanged → ViewModel
//    • KeyDown handlers (tbActualPairs, tbUsagePct, tbRFIDName) → ViewModel
//    • Title-bar button Click handlers
//    • DataGrid helper: scroll + select row theo step code
//
//  Mọi business logic đã chuyển sang ShotWeightViewModel.cs
//  Namespace : SSSW.UI.WPF
// ============================================================================
using DevExpress.Xpf.Editors;
using ScanAndScale.Driver;
using ScanAndScale.Helper;
using SSSW.models;
using SSSW.modelss;
using SSSW.UI.WPF.ViewModels;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.Integration;
using System.Windows.Input;
// ── Disambiguate WPF vs WinForms types (UseWPF + UseWindowsForms + ImplicitUsings) ──
// Aliases take precedence over namespace lookups → no more CS0104
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

        // ── Hardware controls (WinForms) ──────────────────────────────────────
        private BarcodeButtonEdit? _scanBarcode;
        private RFIDButtonEdit?    _txtRFIDCode;
        private ScaleButtonEdit?   _scaleCtrl;

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

            // 1. Gắn WinForms hardware controls vào WindowsFormsHost
            AttachHardwareControls();

            // 2. Cấu hình View callbacks để ViewModel gọi lại View khi cần
            _vm.ClearBarcodeAction      = () => { if (_scanBarcode != null) _scanBarcode.Text = string.Empty; };
            _vm.ClearRfidAction         = () => { if (_txtRFIDCode != null) _txtRFIDCode.Text = string.Empty; };
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
            // Detach hardware events để tránh memory leak
            if (_scanBarcode != null) _scanBarcode.DataValueChanged -= ScanBarcode_DataValueChanged;
            if (_txtRFIDCode != null) _txtRFIDCode.DataValueChanged -= RFIDCode_DataValueChanged;
            if (_scaleCtrl   != null) _scaleCtrl.DataValueChanged   -= Scale_DataValueChanged;
        }

        // ════════════════════════════════════════════════════════════════════
        //  HARDWARE CONTROLS – ATTACH
        // ════════════════════════════════════════════════════════════════════
        private void AttachHardwareControls()
        {
            // ── Barcode scanner ──────────────────────────────────────────────
            _scanBarcode = new BarcodeButtonEdit();
            _scanBarcode.DataValueChanged += ScanBarcode_DataValueChanged;
            barcodeHost.Child = new WindowsFormsHost { Child = _scanBarcode };

            // ── RFID reader ──────────────────────────────────────────────────
            _txtRFIDCode = new RFIDButtonEdit();
            _txtRFIDCode.DataValueChanged += RFIDCode_DataValueChanged;
            rfidHost.Child = new WindowsFormsHost { Child = _txtRFIDCode };

            // ── Weight scale ─────────────────────────────────────────────────
            _scaleCtrl = new ScaleButtonEdit();
            _scaleCtrl.DataValueChanged += Scale_DataValueChanged;
            scaleHost.Child = new WindowsFormsHost { Child = _scaleCtrl };
        }

        // ── Apply hardware config (called back from ViewModel after config load) ──
        private void ApplyHardwareConfig()
        {
            if (_scanBarcode != null)
                _scanBarcode.Config = GlobalVariable.ConfigSystem.Scanner;

            if (_txtRFIDCode != null)
                _txtRFIDCode.Config = GlobalVariable.ConfigSystem.RFID;

            if (_scaleCtrl != null)
            {
                _scaleCtrl.Config          = GlobalVariable.ConfigSystem.Scale;
                _scaleCtrl.EnableReadScale = GlobalVariable.ConfigSystem.EnableReadScale == true;
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  HARDWARE EVENT BRIDGES  (marshal về UI thread rồi gọi ViewModel)
        // ════════════════════════════════════════════════════════════════════

        private async void ScanBarcode_DataValueChanged(object? sender, DataValueChangedEventArgs e)
        {
            var barcode = e.NewValue?.Value?.ToString() ?? string.Empty;
            // Barcode event có thể fire từ background thread → InvokeAsync
            await Dispatcher.InvokeAsync(() => _vm.OnBarcodeScannedAsync(barcode));
        }

        private void RFIDCode_DataValueChanged(object? sender, DataValueChangedEventArgs e)
        {
            var code = e.NewValue?.Value?.ToString() ?? string.Empty;
            Dispatcher.Invoke(() => _vm.OnRfidValueChanged(code));
        }

        private void Scale_DataValueChanged(object? sender, DataValueChangedEventArgs e)
        {
            double val = 0;
            if (e.NewValue?.Value is double d)
                val = d;
            else
                double.TryParse(e.NewValue?.Value?.ToString(), out val);

            Dispatcher.Invoke(() => _vm.OnScaleValueChanged(val));
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

            // Lấy item tương ứng với EditValue (StepItemCode) từ danh sách ViewModel
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
