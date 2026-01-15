namespace SSSW
{
    partial class frmMainView
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMainView));
            ribbonControl1 = new DevExpress.XtraBars.Ribbon.RibbonControl();
            _barButtonItemLoad = new DevExpress.XtraBars.BarButtonItem();
            ribbonPage1 = new DevExpress.XtraBars.Ribbon.RibbonPage();
            ribbonPageGroup1 = new DevExpress.XtraBars.Ribbon.RibbonPageGroup();
            _grc = new DevExpress.XtraGrid.GridControl();
            _grv = new DevExpress.XtraGrid.Views.Grid.GridView();
            ((System.ComponentModel.ISupportInitialize)ribbonControl1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_grc).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_grv).BeginInit();
            SuspendLayout();
            // 
            // ribbonControl1
            // 
            ribbonControl1.ExpandCollapseItem.Id = 0;
            ribbonControl1.Items.AddRange(new DevExpress.XtraBars.BarItem[] { ribbonControl1.ExpandCollapseItem, _barButtonItemLoad });
            ribbonControl1.Location = new Point(0, 0);
            ribbonControl1.MaxItemId = 2;
            ribbonControl1.Name = "ribbonControl1";
            ribbonControl1.Pages.AddRange(new DevExpress.XtraBars.Ribbon.RibbonPage[] { ribbonPage1 });
            ribbonControl1.Size = new Size(1904, 150);
            // 
            // _barButtonItemLoad
            // 
            _barButtonItemLoad.Caption = "Load";
            _barButtonItemLoad.Id = 1;
            _barButtonItemLoad.ImageOptions.SvgImage = (DevExpress.Utils.Svg.SvgImage)resources.GetObject("_barButtonItemLoad.ImageOptions.SvgImage");
            _barButtonItemLoad.Name = "_barButtonItemLoad";
            // 
            // ribbonPage1
            // 
            ribbonPage1.Groups.AddRange(new DevExpress.XtraBars.Ribbon.RibbonPageGroup[] { ribbonPageGroup1 });
            ribbonPage1.Name = "ribbonPage1";
            ribbonPage1.Text = "Home";
            // 
            // ribbonPageGroup1
            // 
            ribbonPageGroup1.ItemLinks.Add(_barButtonItemLoad);
            ribbonPageGroup1.Name = "ribbonPageGroup1";
            ribbonPageGroup1.Text = "Actions";
            // 
            // _grc
            // 
            _grc.Dock = DockStyle.Fill;
            _grc.EmbeddedNavigator.Buttons.Append.Visible = false;
            _grc.EmbeddedNavigator.Buttons.CancelEdit.Visible = false;
            _grc.EmbeddedNavigator.Buttons.Edit.Visible = false;
            _grc.EmbeddedNavigator.Buttons.EndEdit.Visible = false;
            _grc.EmbeddedNavigator.Buttons.Remove.Visible = false;
            _grc.Location = new Point(0, 150);
            _grc.MainView = _grv;
            _grc.MenuManager = ribbonControl1;
            _grc.Name = "_grc";
            _grc.Size = new Size(1904, 811);
            _grc.TabIndex = 1;
            _grc.UseEmbeddedNavigator = true;
            _grc.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] { _grv });
            // 
            // _grv
            // 
            _grv.GridControl = _grc;
            _grv.Name = "_grv";
            // 
            // frmMainView
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1904, 961);
            Controls.Add(_grc);
            Controls.Add(ribbonControl1);
            Name = "frmMainView";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "SSSW - History Scale";
            ((System.ComponentModel.ISupportInitialize)ribbonControl1).EndInit();
            ((System.ComponentModel.ISupportInitialize)_grc).EndInit();
            ((System.ComponentModel.ISupportInitialize)_grv).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private DevExpress.XtraBars.Ribbon.RibbonControl ribbonControl1;
        private DevExpress.XtraBars.BarButtonItem _barButtonItemLoad;
        private DevExpress.XtraBars.Ribbon.RibbonPage ribbonPage1;
        private DevExpress.XtraBars.Ribbon.RibbonPageGroup ribbonPageGroup1;
        private DevExpress.XtraGrid.GridControl _grc;
        private DevExpress.XtraGrid.Views.Grid.GridView _grv;
    }
}