// ============================================================================
//  ShotWeightFGWindow.xaml.cs  –  Code-behind (MVVM)
//
//  Cửa sổ "Scan FG" (cân mẫu thành phẩm) dùng RIÊNG một instance
//  ShotWeightFGViewModel (transient qua DI). FG weighing không có khái niệm
//  step/BOM — mỗi lần scan/chọn FG thêm 1 dòng mới vào SAMPLING LIST.
//
//  Hardware (Barcode/RFID/Scale) được sở hữu bởi DeviceConnectionService
//  (singleton, dùng chung với ShotWeightWindow) — cửa sổ này chỉ đăng ký
//  events re-broadcast từ service, không tự Initialize/Dispose driver.
//
//  Namespace : SSSW.UI.WPF
// ============================================================================
using DevExpress.Xpf.Editors;
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
    public partial class ShotWeightFGWindow : UserControl
    {
        // ── ViewModel (instance RIÊNG của cửa sổ này) ───────────────────────
        private ShotWeightFGViewModel _vm = null!;

        // ── Chặn EditValueChanged bắn chồng lên nhau (VD người dùng bấm nhanh 2 dòng
        // liên tiếp trong popup trước khi lần chọn trước load DB xong) ──────────────
        private bool _isHandlingFgSelection;

        // ── Shared hardware connections — đọc qua biến toàn cục GlobalVariable.Devices
        // (Main đã gán + kết nối trước khi tab này được mở), xem ghi chú tương ứng trong
        // ShotWeightWindow.xaml.cs. ─────────────────────────────────────────────────
        private DeviceConnectionService _deviceService = null!;

        // ── Chỉ load master data (InitializeFgAsync) MỘT LẦN cho instance này —
        // xem ghi chú tương ứng trong ShotWeightWindow.xaml.cs. ─────────────────
        private bool _initialized;

        // ════════════════════════════════════════════════════════════════════
        //  CONSTRUCTORS
        // ════════════════════════════════════════════════════════════════════

        /// <summary>DI constructor – nhận ViewModel riêng của cửa sổ này.</summary>
        public ShotWeightFGWindow(ShotWeightFGViewModel viewModel) : this()
        {
            _vm = viewModel;
            DataContext = viewModel;
        }

        /// <summary>Parameterless ctor – yêu cầu bởi XAML (x:Class) và XAML designer.</summary>
        public ShotWeightFGWindow()
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
            //    và kết nối RFID/Barcode/Scale trước khi tab nào (Step/FG) được mở.
            _deviceService = GlobalVariable.Devices!;

            // 1. Đăng ký events TRƯỚC khi Initialize để không bỏ sót status thay đổi.
            //    EnsureInitialized() ở đây luôn là no-op vì Main đã kết nối sẵn với config đúng.
            _deviceService.BarcodeChanged += BarcodeDriver_DataValueChanged;
            _deviceService.RfidChanged += RfidDriver_DataValueChanged;
            _deviceService.ScaleChanged += ScaleDriver_DataValueChanged;
            _deviceService.EnsureInitialized();

            // Backfill trạng thái hiện tại — driver có thể đã kết nối từ trước (do cửa sổ
            // chính init), nên sẽ không có event mới nào bắn ra để cập nhật UI ở đây.
            _vm.BarcodeStatus = _deviceService.BarcodeStatus;
            _vm.RfidStatus = _deviceService.RfidStatus;
            _vm.ScaleStatus = _deviceService.ScaleStatus;

            // 2. Cấu hình View callbacks để ViewModel gọi lại View khi cần
            _vm.FocusRfidNameAction = () => tbRFIDName.Focus();
            _vm.ClearFgComboAction = () => { cbFgCode.EditValue = null; };

            // 3. Khởi tạo ViewModel (bản rút gọn dành cho FG) — CHỈ MỘT LẦN cho
            //    instance này (xem ghi chú ở _initialized).
            if (!_initialized)
            {
                _initialized = true;
                await InitializeFgAsync();
            }
        }

        /// <summary>
        /// Bản rút gọn dành cho cửa sổ FG: chỉ áp dụng cờ readonly hiện có (config đã do cửa
        /// sổ chính load khi app khởi động) và gọi LoadDataAsync() — mesocomp/mesoyear/
        /// WindowTitle được ViewModel tự fetch bên trong LoadDataAsync().
        /// </summary>
        private async Task InitializeFgAsync()
        {
            _vm.ReadOnlyScale = GlobalVariable.ConfigSystem.Scale.ReadOnly ?? true;
            _vm.ReadOnlyRfid = GlobalVariable.ConfigSystem.RFID.ReadOnly;
            _vm.ReadOnlyScanner = GlobalVariable.ConfigSystem.Scanner.ReadOnly;

            await _vm.LoadDataAsync();
        }

        // ════════════════════════════════════════════════════════════════════
        //  UNLOADED (control bị gỡ khỏi cây visual — Main chuyển sang tab khác)
        // ════════════════════════════════════════════════════════════════════
        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            // Hủy đăng ký events → tránh double-handling khi tab này không hiển thị.
            // KHÔNG Dispose driver nào — DeviceConnectionService.Shutdown() lo việc đó,
            // gọi đúng 1 lần từ Program.cs khi thoát app.
            _deviceService.BarcodeChanged -= BarcodeDriver_DataValueChanged;
            _deviceService.RfidChanged -= RfidDriver_DataValueChanged;
            _deviceService.ScaleChanged -= ScaleDriver_DataValueChanged;
        }

        // ════════════════════════════════════════════════════════════════════
        //  CORE DRIVER EVENT BRIDGES
        //  ⚠️ Tất cả events có thể fire từ background thread.
        //     Phải Dispatcher.Invoke/InvokeAsync để cập nhật UI.
        // ════════════════════════════════════════════════════════════════════

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

        private void ScaleDriver_DataValueChanged(object? sender, DataValueChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                var data = e.NewValue;
                _vm.ScaleStatus = data.DriverStatus;

                if (data.DriverStatus == DriverStatus.Connected && sender is ScanAndScale.Core.Drivers.ScaleDriver sd)
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
        //  DEVEXPRESS LOOKUPERDIT – FG SELECTOR
        // ════════════════════════════════════════════════════════════════════
        private async void cbFgCode_EditValueChangedAsync(object sender, EditValueChangedEventArgs e)
        {
            if (_vm == null || _isHandlingFgSelection) return;

            var editVal = cbFgCode.EditValue?.ToString();
            var selected = string.IsNullOrEmpty(editVal)
                ? null
                : _vm.FgCodeMaster.FirstOrDefault(x => x.FGCode == editVal);

            _isHandlingFgSelection = true;
            try
            {
                await _vm.OnFgSelectedAsync(selected);
            }
            finally
            {
                _isHandlingFgSelection = false;
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  TEXTBOX KEYDOWN BRIDGES
        // ════════════════════════════════════════════════════════════════════
        private async void tbRFIDName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && sender is TextBox tb && _vm != null)
                await _vm.OnRfidNameEnterAsync(tb.Text);
        }

        // ════════════════════════════════════════════════════════════════════
        //  DATAGRID EVENTS
        // ════════════════════════════════════════════════════════════════════
        private void dgSamplingList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // No-op: hành động qua GridScaleCommand / GridResetCommand / GridDeleteCommand (XAML)
        }

        // ════════════════════════════════════════════════════════════════════
        //  HISTORY HEADER CLICK (toggle expand/collapse)
        // ════════════════════════════════════════════════════════════════════
        private void HistoryHeader_Click(object sender, MouseButtonEventArgs e)
            => _vm?.ToggleHistory();

        private void _txtScale_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var tb = sender as TextBox;

                //force cập nhật binding
                tb.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();

                var oldValue = new DataValue(DriverStatus.Reconnecting, _vm.ScaleValue);
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

            var current = tb.Text;
            var result = current
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
            if (_scaleWarningTip is null)
            {
                _scaleWarningTip = new System.Windows.Controls.ToolTip
                {
                    Placement = PlacementMode.Bottom,
                    PlacementTarget = tb,
                    StaysOpen = false,
                    Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 243, 205)),
                    BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(251, 140, 0)),
                    Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 60, 0)),
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Padding = new Thickness(10, 6, 10, 6),
                };
                tb.ToolTip = _scaleWarningTip;
            }

            _scaleWarningTip.Content = $"⚠  \"{badInput}\" không hợp lệ — chỉ nhập số (vd: 36.5)";
            _scaleWarningTip.IsOpen = true;

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
