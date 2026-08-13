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
using SSSW.UI.WPF.Services;
using SSSW.UI.WPF.ViewModels;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
// ── Disambiguate WPF vs WinForms types ──────────────────────────────────────
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using TextBox = System.Windows.Controls.TextBox;
using SelectionChangedEventArgs = System.Windows.Controls.SelectionChangedEventArgs;
using UserControl = System.Windows.Controls.UserControl;
using Serilog;

namespace SSSW.UI.WPF
{
    public partial class ShotWeightWindow : UserControl
    {
        // ── ViewModel ────────────────────────────────────────────────────────
        private ShotWeightViewModel _vm = null!;

        // ── Chặn EditValueChanged bắn chồng lên nhau (VD người dùng bấm nhanh 2 dòng
        // liên tiếp trong popup trước khi lần chọn trước load DB xong) ──────────────
        private bool _isHandlingStepSelection;

        // ── Shared hardware connections — Main đã kết nối (RFID/Barcode/Scale) và gán
        // GlobalVariable.Devices trước khi tab này được mở, nên đọc thẳng biến toàn cục
        // thay vì nhận qua constructor DI (xem MainViewModel.StartupAsync()). ─────────
        private DeviceConnectionService _deviceService = null!;

        // ── Chỉ load master data (InitializeAsync) MỘT LẦN cho instance này.
        // Control này được Main giữ lại và tái sử dụng mỗi lần chuyển tab (Step ↔ FG):
        // Unloaded/Loaded bắn lại mỗi lần ContentControl.Content đổi, nhưng không được
        // load lại toàn bộ dữ liệu mỗi lần — sẽ mất dữ liệu cân dở của operator. ──────
        private bool _initialized;

        // ════════════════════════════════════════════════════════════════════
        //  CONSTRUCTORS
        // ════════════════════════════════════════════════════════════════════

        /// <summary>DI constructor – nhận ViewModel từ DI container.</summary>
        public ShotWeightWindow(ShotWeightViewModel viewModel) : this()
        {
            _vm = viewModel;
            DataContext = viewModel;
        }

        /// <summary>Parameterless ctor – yêu cầu bởi XAML (x:Class) và XAML designer.</summary>
        public ShotWeightWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        // ════════════════════════════════════════════════════════════════════
        //  LOADED
        // ════════════════════════════════════════════════════════════════════
        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_vm == null) return; // guard cho design-time preview

            // 0. Lấy service dùng chung từ biến toàn cục — Main đã gán GlobalVariable.Devices
            //    và gọi EnsureInitialized() (kết nối RFID/Barcode/Scale bằng Config thật) trước
            //    khi tab này được mở (xem MainViewModel.StartupAsync()).
            _deviceService = GlobalVariable.Devices!;

            // 1. Đăng ký các event re-broadcast từ DeviceConnectionService TRƯỚC khi backfill
            //    status để không bỏ sót thay đổi xảy ra đúng lúc đăng ký.
            _deviceService.BarcodeChanged += BarcodeDriver_DataValueChanged;
            _deviceService.RfidChanged += RfidDriver_DataValueChanged;
            _deviceService.ScaleChanged += ScaleDriver_DataValueChanged;
            _deviceService.EnsureInitialized(); // no-op nếu Main đã kết nối sẵn (luôn đúng)

            // Backfill trạng thái hiện tại — driver đã kết nối từ trước (do Main init) nên sẽ
            // không có event mới nào bắn ra để cập nhật UI ở đây.
            _vm.BarcodeStatus = _deviceService.BarcodeStatus;
            _vm.RfidStatus = _deviceService.RfidStatus;
            _vm.ScaleStatus = _deviceService.ScaleStatus;

            // 2. Cấu hình View callbacks để ViewModel gọi lại View khi cần
            //    Ghi chú: ClearBarcodeAction / ClearRfidAction đã được VM khởi tạo
            //    trong constructor → tự xóa BarcodeScannedValue / RfidCardCode.
            //    Ở đây chỉ gán những callback liên quan đến WPF controls cụ thể.
            _vm.FocusRfidNameAction = () => tbRFIDName.Focus();
            _vm.ClearStepComboAction = () => { cbStepName.EditValue = null; };
            _vm.SetStepComboAction = item => { cbStepName.EditValue = item?.StepItemCode; };
            _vm.FocusGridRowAction = code => FocusStepInGrid(code);

            // 3. Load master data CHỈ MỘT LẦN cho instance này (xem ghi chú ở _initialized).
            //    Config/hardware không còn được load/kết nối ở đây nữa — Main lo hết.
            if (!_initialized)
            {
                _initialized = true;
                await _vm.InitializeAsync();
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  UNLOADED (control bị gỡ khỏi cây visual — Main chuyển sang tab khác)
        // ════════════════════════════════════════════════════════════════════
        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            // Hủy đăng ký events → tránh 1 lần scan/cân vật lý bị xử lý trùng bởi
            // tab đang không hiển thị. KHÔNG dispose driver ở đây: chúng được chia sẻ
            // với ShotWeightFGWindow và chỉ được giải phóng một lần bởi
            // DeviceConnectionService.Shutdown() khi ứng dụng thoát.
            _deviceService.BarcodeChanged -= BarcodeDriver_DataValueChanged;
            _deviceService.RfidChanged -= RfidDriver_DataValueChanged;
            _deviceService.ScaleChanged -= ScaleDriver_DataValueChanged;
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
                        value: Convert.ToDouble(data.Value ?? 0.0),
                        stable: sd.IsStable,
                        tare: sd.IsTare,
                        unit: sd.Unit
                    );
                }
                else if (data.DriverStatus == DriverStatus.Disconnected)
                {
                    _vm.OnScaleValueChanged(0, false, false, "G");
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
            if (_vm == null || _isHandlingStepSelection) return;

            var editVal = cbStepName.EditValue?.ToString();
            var selected = string.IsNullOrEmpty(editVal)
                ? null
                : _vm.StepCodeMaster.FirstOrDefault(x => x.StepItemCode == editVal);

            _isHandlingStepSelection = true;
            try
            {
                await _vm.OnStepSelectedAsync(selected);
            }
            finally
            {
                _isHandlingStepSelection = false;
            }
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

        private void _txtScale_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var tb = sender as TextBox;

                //force cập nhật binding
                tb.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();

                var oldValue = new DataValue(DriverStatus.Reconnecting, _vm.ScaleValue);

                //_vm.ScaleValue = Convert.ToDouble(_vm.ScaleDisplay);
                var newValue = new DataValue(DriverStatus.Reconnecting, _vm.ScaleValue);
                ScaleDriver_DataValueChanged(null, new DataValueChangedEventArgs(newValue, oldValue));
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  SCALE TEXTBOX – NUMERIC VALIDATION
        // ════════════════════════════════════════════════════════════════════

        /// Regex: số nguyên hoặc số thực, có thể âm, chưa hoàn chỉnh (ví dụ "36." hợp lệ khi đang gõ)
        private static readonly Regex _numericRegex =
            new(@"^-?\d*\.?\d*$", RegexOptions.Compiled);

        private System.Windows.Controls.ToolTip? _scaleWarningTip;
        private System.Windows.Threading.DispatcherTimer? _tipTimer;

        /// <summary>Chặn ký tự không phải số; hiện tooltip cảnh báo nếu sai.</summary>
        private void TxtScale_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is not TextBox tb || tb.IsReadOnly) return;

            // Tính chuỗi kết quả nếu ký tự được chèn vào
            var current = tb.Text;
            var result  = current
                .Remove(tb.SelectionStart, tb.SelectionLength)
                .Insert(tb.SelectionStart, e.Text);

            if (!_numericRegex.IsMatch(result))
            {
                e.Handled = true;                    // chặn ký tự
                ShowScaleWarning(tb, e.Text);
            }
        }

        /// <summary>Chặn paste nếu nội dung không phải số.</summary>
        private void TxtScale_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (sender is not TextBox tb || tb.IsReadOnly) return;

            var pasted = e.DataObject.GetData(typeof(string)) as string ?? "";
            if (!_numericRegex.IsMatch(pasted))
            {
                e.CancelCommand();
                ShowScaleWarning(tb, pasted);
            }
        }

        /// <summary>Hiện tooltip cảnh báo bên dưới ô cân, tự đóng sau 2 giây.</summary>
        private void ShowScaleWarning(TextBox tb, string badInput)
        {
            // Khởi tạo tooltip một lần
            if (_scaleWarningTip is null)
            {
                _scaleWarningTip = new System.Windows.Controls.ToolTip
                {
                    Placement        = PlacementMode.Bottom,
                    PlacementTarget  = tb,
                    StaysOpen        = false,
                    Background       = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 243, 205)),
                    BorderBrush      = new SolidColorBrush(System.Windows.Media.Color.FromRgb(251, 140,   0)),
                    Foreground       = new SolidColorBrush(System.Windows.Media.Color.FromRgb(100,  60,   0)),
                    FontSize         = 12,
                    FontWeight       = FontWeights.SemiBold,
                    Padding          = new Thickness(10, 6, 10, 6),
                };
                tb.ToolTip = _scaleWarningTip;
            }

            _scaleWarningTip.Content = $"⚠  \"{badInput}\" không hợp lệ — chỉ nhập số (vd: 36.5)";
            _scaleWarningTip.IsOpen  = true;

            // Reset bộ đếm tự đóng
            _tipTimer?.Stop();
            _tipTimer = new System.Windows.Threading.DispatcherTimer
                { Interval = TimeSpan.FromSeconds(2) };
            _tipTimer.Tick += (_, _) =>
            {
                _scaleWarningTip.IsOpen = false;
                _tipTimer?.Stop();
            };
            _tipTimer.Start();
        }

        private void _txtBarcode_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.Enter)
                {
                    var tb = sender as TextBox;

                    //force cập nhật binding
                    tb.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();

                    BarcodeDriver_DataValueChanged(null,
                        new DataValueChangedEventArgs(
                            new DataValue(DriverStatus.Connected, _vm.BarcodeScannedValue),
                            new DataValue(DriverStatus.Connected, _vm.BarcodeScannedValue)
                        ));
                }

            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "EROR", MessageBoxButton.OK, (MessageBoxImage)MessageBoxIcon.Error);
                Log.Error(ex.Message);
            }
        }

        private void _txtRFID_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.Enter)
                {
                    var tb = sender as TextBox;

                    //force cập nhật binding
                    tb.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();

                    RfidDriver_DataValueChanged(null,
                        new DataValueChangedEventArgs(
                            new DataValue(DriverStatus.Connected, _vm.RfidCardCode),
                            new DataValue(DriverStatus.Connected, _vm.RfidCardCode)
                        ));
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "EROR", MessageBoxButton.OK, (MessageBoxImage)MessageBoxIcon.Error);
                Log.Error(ex.Message);
            }
        }

    }
}
