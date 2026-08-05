// ============================================================================
//  ShotWeightFGWindow.xaml.cs  –  Minimal code-behind (UI scaffold)
//
//  Cửa sổ này là bản UI tương tự ShotWeightWindow, dùng cho luồng "Scan FG"
//  (cân mẫu thành phẩm). Hiện tại CHỈ dựng giao diện — phần logic
//  (ViewModel, driver, DB) sẽ được bổ sung ở bước sau.
//
//  Các handler bên dưới là stub trống, chỉ để XAML biên dịch được vì các
//  control (DevExpress LookUpEdit, TextBox KeyDown, DataGrid...) tham chiếu
//  tới chúng qua tên. Khi làm phần code, nối các handler này (hoặc thay
//  bằng Command binding) tới ViewModel tương ứng.
//
//  Namespace : SSSW.UI.WPF
// ============================================================================
using DevExpress.Xpf.Editors;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
// ── Disambiguate WPF vs WinForms types ──────────────────────────────────────
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using TextBox = System.Windows.Controls.TextBox;
using SelectionChangedEventArgs = System.Windows.Controls.SelectionChangedEventArgs;

namespace SSSW.UI.WPF
{
    public partial class ShotWeightFGWindow : Window
    {
        // ── Win32 – borderless drag ──────────────────────────────────────────
        [DllImport("user32.dll")] private static extern bool ReleaseCapture();
        [DllImport("user32.dll")] private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        public ShotWeightFGWindow()
        {
            InitializeComponent();
        }

        // ════════════════════════════════════════════════════════════════════
        //  TITLE BAR – drag window (không có nút Minimize/Maximize/Close)
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
        //  TODO (code sau): nối các handler dưới đây tới ViewModel của
        //  ShotWeightFGWindow khi phần logic được triển khai.
        // ════════════════════════════════════════════════════════════════════

        private void _tbEmployee_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void cbStepName_EditValueChangedAsync(object sender, EditValueChangedEventArgs e)
        {
        }

        private void tbActualPairs_KeyDown(object sender, KeyEventArgs e)
        {
        }

        private void tbUsagePct_KeyDown(object sender, KeyEventArgs e)
        {
        }

        private void tbRFIDName_KeyDown(object sender, KeyEventArgs e)
        {
        }

        private void dgTotalSteps_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void HistoryHeader_Click(object sender, MouseButtonEventArgs e)
        {
        }

        private void _txtScale_KeyDown(object sender, KeyEventArgs e)
        {
        }

        private static readonly Regex _numericRegex =
            new(@"^-?\d*\.?\d*$", RegexOptions.Compiled);

        private void TxtScale_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is not TextBox tb || tb.IsReadOnly) return;

            var current = tb.Text;
            var result = current
                .Remove(tb.SelectionStart, tb.SelectionLength)
                .Insert(tb.SelectionStart, e.Text);

            if (!_numericRegex.IsMatch(result))
                e.Handled = true;
        }

        private void TxtScale_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (sender is not TextBox tb || tb.IsReadOnly) return;

            var pasted = e.DataObject.GetData(typeof(string)) as string ?? "";
            if (!_numericRegex.IsMatch(pasted))
                e.CancelCommand();
        }

        private void _txtBarcode_KeyDown(object sender, KeyEventArgs e)
        {
        }

        private void _txtRFID_KeyDown(object sender, KeyEventArgs e)
        {
        }
    }
}
