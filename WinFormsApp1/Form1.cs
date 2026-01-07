using DevExpress.XtraEditors;
using ScanAndScale.Driver;
using ScanAndScale.Helper;
using SSSW.models;
using System.Diagnostics;
using System.Reflection.Metadata;

namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            Load += Form1_Load;
        }

        private void Form1_Load(object? sender, EventArgs e)
        {
            barcodeButtonEdit1.Config = GolobalTag.barcodeConfig;
            rfidButtonEdit1.Config = GolobalTag.rfidConfig;
            scaleButtonEdit1.Config = GolobalTag.scaleConfig;

            scaleButtonEdit1.EnableReadScale = checkBox1.Checked;
            checkBox1.CheckedChanged += (s, o) =>
            {
                scaleButtonEdit1.EnableReadScale = checkBox1.Checked;
            };

            barcodeButtonEdit1.DataValueChanged += BarcodeButtonEdit1_DataValueChanged;
            rfidButtonEdit1.DataValueChanged += RfidButtonEdit1_DataValueChanged;
            scaleButtonEdit1.DataValueChanged += ScaleButtonEdit1_DataValueChanged;
        }

        private void ScaleButtonEdit1_DataValueChanged(object? sender, DataValueChangedEventArgs e)
        {
            Debug.WriteLine($"Đã nhận được tín hiệu thay đổi Cân: '{e.NewValue.Value}'|{Convert.ToDouble(e.NewValue.Value?.ToString()) - scaleButtonEdit1.BagWeight}");
        }

        private void RfidButtonEdit1_DataValueChanged(object? sender, DataValueChangedEventArgs e)
        {
            MessageBox.Show($"Đã nhận được tín hiệu thay đổi RFID: '{e.NewValue.Value}'");
        }

        private void BarcodeButtonEdit1_DataValueChanged(object? sender, DataValueChangedEventArgs e)
        {
            MessageBox.Show($"Đã nhận được tín hiệu thay đổi barcode: '{e.NewValue.Value}'");
        }
    }
}
