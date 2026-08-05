using SSSW.UI.WPF.ViewModels;
using System;
using System.Windows;
using System.Windows.Input;
// ── Disambiguate WPF vs WinForms types (UseWindowsForms=true kéo theo global using System.Windows.Forms) ──
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace SSSW
{
    /// <summary>
    /// Interaction logic for frmRfidInput.xaml
    ///
    /// Dialog nhập tay Employee ID (khi không quét được RFID). Người dùng gõ ID,
    /// nhấn Enter để tra cứu tên; nếu chưa tồn tại thì gõ tên rồi Enter ở ô Name
    /// để đăng ký operator mới. Bấm OK để trả kết quả (ResultRfidCode/ResultRfidName)
    /// về cho form gọi (ShotWeightWindow) qua ShowDialog() == true.
    /// </summary>
    public partial class frmRfidInput : Window
    {
        // ── ViewModel ────────────────────────────────────────────────────────
        private readonly frmRfidInputViewModel _vm = null!;

        // ════════════════════════════════════════════════════════════════════
        //  CONSTRUCTORS
        // ════════════════════════════════════════════════════════════════════

        /// <summary>Parameterless ctor – yêu cầu bởi XAML (x:Class) và XAML designer.</summary>
        public frmRfidInput()
        {
            InitializeComponent();
        }

        /// <summary>DI constructor – nhận ViewModel từ DI container.</summary>
        public frmRfidInput(frmRfidInputViewModel viewModel) : this()
        {
            _vm = viewModel;
            DataContext = viewModel;
        }

        // ════════════════════════════════════════════════════════════════════
        //  RESULT – form gọi đọc sau khi ShowDialog() trả về true
        // ════════════════════════════════════════════════════════════════════
        public string ResultRfidCode => _vm?.RfidCardCode ?? string.Empty;
        public string ResultRfidName => _vm?.RfidName ?? string.Empty;

        // ════════════════════════════════════════════════════════════════════
        //  TEXTBOX KEYDOWN BRIDGES
        // ════════════════════════════════════════════════════════════════════

        private void _txtRfid_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.Enter)
                {
                    var tb = sender as System.Windows.Controls.TextBox;

                    //force cập nhật binding
                    tb?.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty)?.UpdateSource();

                    _vm?.OnRfidValueChanged();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "EROR", MessageBoxButton.OK, (MessageBoxImage)MessageBoxIcon.Error);
            }
        }

        private async void tbRFIDName_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.Enter && sender is System.Windows.Controls.TextBox tb && _vm != null)
                    await _vm.OnRfidNameEnterAsync(tb.Text);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "EROR", MessageBoxButton.OK, (MessageBoxImage)MessageBoxIcon.Error);
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  OK / CANCEL
        // ════════════════════════════════════════════════════════════════════

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_vm?.RfidCardCode) || string.IsNullOrWhiteSpace(_vm?.RfidName))
            {
                System.Windows.MessageBox.Show(
                    "Please enter an Employee ID and press Enter to resolve the name before continuing.",
                    "WARNING", MessageBoxButton.OK, (MessageBoxImage)MessageBoxIcon.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
