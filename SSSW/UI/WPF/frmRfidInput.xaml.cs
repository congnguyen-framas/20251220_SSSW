using SSSW.UI.WPF.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SSSW
{
    /// <summary>
    /// Interaction logic for frmRfidInput.xaml
    /// </summary>
    public partial class frmRfidInput : Window
    {
        // ── ViewModel ────────────────────────────────────────────────────────
        private readonly frmRfidInput _vm = null!;

        public frmRfidInput()
        {
            InitializeComponent();
        }

        // ════════════════════════════════════════════════════════════════════
        //  CONSTRUCTORS
        // ════════════════════════════════════════════════════════════════════

        /// <summary>DI constructor – nhận ViewModel từ DI container.</summary>
        public frmRfidInput(frmRfidInput viewModel) : this()
        {
            _vm = viewModel;
            DataContext = viewModel;
        }

        private void _txtRfid_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.Enter)
                {
                    var tb = sender as System.Windows.Controls.TextBox;

                    //force cập nhật binding
                    tb.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty)?.UpdateSource();
                    //_vm.OnRfidValueChanged();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "EROR", MessageBoxButton.OK, (MessageBoxImage)MessageBoxIcon.Error);
                //Log.Error(ex.Message);
            }
        }
    }
}
