// ============================================================================
//  Main.xaml.cs  –  Shell window (header với 2 tab Step Component / Finished
//  Goods + body trống). Đây là Window thật duy nhất của app WPF (thay thế vai
//  trò cũ của ShotWeightWindow) — chỉ chứa chrome (drag/minimize/maximize/close),
//  KHÔNG chứa business logic (nằm trong MainViewModel + ShotWeightViewModel /
//  ShotWeightFGViewModel).
//  Namespace : SSSW.UI.WPF
// ============================================================================
using SSSW.UI.WPF.ViewModels;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;

namespace SSSW.UI.WPF
{
    public partial class Main : Window
    {
        // ── Win32 – borderless drag ──────────────────────────────────────────
        [DllImport("user32.dll")] private static extern bool ReleaseCapture();
        [DllImport("user32.dll")] private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        /// <summary>DI constructor – nhận ViewModel từ DI container.</summary>
        public Main(MainViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }

        /// <summary>Parameterless ctor – yêu cầu bởi XAML (x:Class) và XAML designer.</summary>
        public Main()
        {
            InitializeComponent();
            Loaded += Main_Loaded;
        }

        // Đảm bảo MainViewModel.StartupAsync() (load Config/kết nối hardware/mở tab Step mặc
        // định) chỉ được kích hoạt SAU KHI Window này thực sự Show() và Dispatcher message pump
        // (Application.Run) đã chạy — KHÔNG gọi từ constructor của MainViewModel nữa. Trước đây
        // MainViewModel tự bắn "_ = StartupAsync()" ngay trong constructor, tức là TRƯỚC dòng
        // wpfApp.Run(wpfWin) ở Program.cs; vì các query mesocomp/mesoyear/FT608 Config thường trả
        // lời rất nhanh (vài ms trên LAN nội bộ), toàn bộ StartupAsync() — kể cả SelectStep() mở
        // tab mặc định — có thể chạy xong TRƯỚC KHI Application.Run() kịp Show() cửa sổ, khiến
        // ShotWeightWindow (UserControl) được resolve/gắn vào ActiveContent trong khi cây visual
        // chưa có PresentationSource nào cả — race condition tái hiện được qua log: title/Config
        // load thành công nhưng body vẫn trống trơn, không tab nào được check, và
        // ShotWeightWindow.OnLoaded không hề bắn (không hàng "sp_MaterialGetCompanyName" nào theo
        // sau FT608 Config trong log). Gọi từ Loaded của chính Main thay vì trong constructor loại
        // bỏ hẳn khả năng xảy ra race này.
        private bool _startupTriggered;
        private async void Main_Loaded(object sender, RoutedEventArgs e)
        {
            if (_startupTriggered) return; // Loaded có thể bắn lại nếu cửa sổ ẩn/hiện lại
            _startupTriggered = true;

            if (DataContext is MainViewModel vm)
                await vm.StartupAsync();
        }

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
        //  SUB-HEADER (moved up from ShotWeightWindow / ShotWeightFGWindow)
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Bấm vào label số thẻ + tên nhân viên trên sub-header → mở dialog nhập
        /// tay Employee ID (khi không quét được thẻ RFID). Không có interface
        /// chung giữa ShotWeightViewModel/ShotWeightFGViewModel nên dùng
        /// pattern-matching để gọi đúng OpenRfidInputDialog() của ViewModel đang
        /// active (tab Step hay FG).
        /// </summary>
        private void _tbEmployee_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // MouseLeftButtonDown bubbles: nếu không đánh dấu Handled, sự kiện tiếp tục nổi lên
            // Border cha (TitleBar_MouseLeftButtonDown) và kích hoạt kéo-thả cửa sổ ngay sau khi
            // dialog RFID đóng lại — đánh dấu Handled để chặn hẳn, đúng ý đồ "click vào label này
            // chỉ để mở dialog nhập tay RFID".
            e.Handled = true;

            if (DataContext is not MainViewModel vm) return;

            switch ((vm.ActiveContent as FrameworkElement)?.DataContext)
            {
                case ShotWeightViewModel svm:
                    svm.OpenRfidInputDialog();
                    break;
                case ShotWeightFGViewModel fvm:
                    fvm.OpenRfidInputDialog();
                    break;
            }
        }
    }
}
