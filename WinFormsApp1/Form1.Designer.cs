namespace WinFormsApp1
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            scaleButtonEdit1 = new ScanAndScale.Driver.ScaleButtonEdit();
            barcodeButtonEdit1 = new ScanAndScale.Driver.BarcodeButtonEdit();
            rfidButtonEdit1 = new ScanAndScale.Driver.RFIDButtonEdit();
            checkBox1 = new CheckBox();
            ((System.ComponentModel.ISupportInitialize)scaleButtonEdit1.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)barcodeButtonEdit1.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)rfidButtonEdit1.Properties).BeginInit();
            SuspendLayout();
            // 
            // scaleButtonEdit1
            // 
            scaleButtonEdit1.AutoDetectUnit = false;
            scaleButtonEdit1.BagWeight = 0D;
            scaleButtonEdit1.Config = null;
            scaleButtonEdit1.DecimalNum = 4;
            scaleButtonEdit1.EnableReadScale = true;
            scaleButtonEdit1.Location = new Point(152, 173);
            scaleButtonEdit1.Name = "scaleButtonEdit1";
            scaleButtonEdit1.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton() });
            scaleButtonEdit1.Size = new Size(104, 20);
            scaleButtonEdit1.Stable = true;
            scaleButtonEdit1.TabIndex = 0;
            scaleButtonEdit1.Tare = false;
            scaleButtonEdit1.UnitType = ScanAndScale.Driver.EmnumUnitType.Kg;
            scaleButtonEdit1.ValueGram = 0D;
            scaleButtonEdit1.ValueKg = 0D;
            scaleButtonEdit1.ValueTon = 0D;
            // 
            // barcodeButtonEdit1
            // 
            barcodeButtonEdit1.Config = null;
            barcodeButtonEdit1.Location = new Point(156, 63);
            barcodeButtonEdit1.Name = "barcodeButtonEdit1";
            barcodeButtonEdit1.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton() });
            barcodeButtonEdit1.Size = new Size(100, 20);
            barcodeButtonEdit1.TabIndex = 1;
            // 
            // rfidButtonEdit1
            // 
            rfidButtonEdit1.Config = null;
            rfidButtonEdit1.Location = new Point(156, 265);
            rfidButtonEdit1.Name = "rfidButtonEdit1";
            rfidButtonEdit1.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Search), new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Delete) });
            rfidButtonEdit1.Properties.NullValuePrompt = "Vui lòng quét thẻ nhân viên";
            rfidButtonEdit1.Size = new Size(100, 20);
            rfidButtonEdit1.TabIndex = 2;
            // 
            // checkBox1
            // 
            checkBox1.AutoSize = true;
            checkBox1.Location = new Point(471, 174);
            checkBox1.Name = "checkBox1";
            checkBox1.Size = new Size(82, 19);
            checkBox1.TabIndex = 3;
            checkBox1.Text = "checkBox1";
            checkBox1.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(checkBox1);
            Controls.Add(rfidButtonEdit1);
            Controls.Add(barcodeButtonEdit1);
            Controls.Add(scaleButtonEdit1);
            Name = "Form1";
            Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)scaleButtonEdit1.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)barcodeButtonEdit1.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)rfidButtonEdit1.Properties).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ScanAndScale.Driver.ScaleButtonEdit scaleButtonEdit1;
        private ScanAndScale.Driver.BarcodeButtonEdit barcodeButtonEdit1;
        private ScanAndScale.Driver.RFIDButtonEdit rfidButtonEdit1;
        private CheckBox checkBox1;
    }
}
