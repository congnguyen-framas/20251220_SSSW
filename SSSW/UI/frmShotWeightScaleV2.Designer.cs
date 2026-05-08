using DevExpress.Utils;
using DevExpress.XtraEditors.Controls;
using ScanAndScale.Driver;
using System.Windows.Forms;

namespace SSSW
{
    partial class frmShotWeightScaleV2
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            // ── Ribbon groups (kept for compatibility / shared base class fields) ──
            ribbonPageGroup6 = new DevExpress.XtraBars.Ribbon.RibbonPageGroup();
            ribbonPageGroup3 = new DevExpress.XtraBars.Ribbon.RibbonPageGroup();
            ribbonPageGroup4 = new DevExpress.XtraBars.Ribbon.RibbonPageGroup();

            // ── LEFT PANEL ──────────────────────────────────────────────────────────
            groupInfo                       = new DevExpress.XtraEditors.GroupControl();
            labelControl13                  = new DevExpress.XtraEditors.LabelControl();  // QR Code
            _scanBarcode                    = new BarcodeButtonEdit();
            labelControl7                   = new DevExpress.XtraEditors.LabelControl();  // Step Name
            _lkStepCode                     = new DevExpress.XtraEditors.GridLookUpEdit();
            gridLookUpEdit1View             = new DevExpress.XtraGrid.Views.Grid.GridView();
            labelControl6                   = new DevExpress.XtraEditors.LabelControl();  // Step Code
            _txtStepCode                    = new DevExpress.XtraEditors.TextEdit();
            labelControl4                   = new DevExpress.XtraEditors.LabelControl();  // Machine
            labelControl1                   = new DevExpress.XtraEditors.LabelControl();  // Quantity
            _txtMachine                     = new DevExpress.XtraEditors.TextEdit();
            _txtQty                         = new DevExpress.XtraEditors.TextEdit();
            labelControl23                  = new DevExpress.XtraEditors.LabelControl();  // Size
            labelControl5                   = new DevExpress.XtraEditors.LabelControl();  // Seq Index
            _txtSize                        = new DevExpress.XtraEditors.TextEdit();
            _txtStepIndex                   = new DevExpress.XtraEditors.TextEdit();
            labelControl3                   = new DevExpress.XtraEditors.LabelControl();  // Actual Prs
            _txtActiclePairShot             = new DevExpress.XtraEditors.TextEdit();
            _toggleSwitchEnablePartition    = new DevExpress.XtraEditors.ToggleSwitch();
            labelControl11                  = new DevExpress.XtraEditors.LabelControl();  // Runner
            labelControl16                  = new DevExpress.XtraEditors.LabelControl();  // % Usage
            _comboBoxEditIsRunner           = new DevExpress.XtraEditors.ComboBoxEdit();
            _txtPercentOFusageNonwoven      = new DevExpress.XtraEditors.TextEdit();
            labelControl2                   = new DevExpress.XtraEditors.LabelControl();  // FG Item Code
            _txtFgItemCode                  = new DevExpress.XtraEditors.TextEdit();
            labelControl12                  = new DevExpress.XtraEditors.LabelControl();  // FG Name
            _txtFGName                      = new DevExpress.XtraEditors.TextEdit();
            labelControl19                  = new DevExpress.XtraEditors.LabelControl();  // Remark
            _txtRemark                      = new DevExpress.XtraEditors.TextEdit();

            // ── CENTER PANEL ────────────────────────────────────────────────────────
            groupControl5                   = new DevExpress.XtraEditors.GroupControl();
            _grcTotalStep                   = new DevExpress.XtraGrid.GridControl();
            _grvTotalStep                   = new DevExpress.XtraGrid.Views.Grid.GridView();
            _repositoryItemButtonEditScale  = new DevExpress.XtraEditors.Repository.RepositoryItemButtonEdit();

            // ── RIGHT PANEL ─────────────────────────────────────────────────────────
            pnlRight            = new Panel();

            // RFID
            groupControl4       = new DevExpress.XtraEditors.GroupControl();
            labelControl15      = new DevExpress.XtraEditors.LabelControl();
            _txtRFIDCode        = new RFIDButtonEdit();
            labelControl14      = new DevExpress.XtraEditors.LabelControl();
            _txtRFIDName        = new DevExpress.XtraEditors.TextEdit();

            // Scale display
            pnlScaleArea        = new Panel();
            pnlScaleCard        = new Panel();
            lblScaleTitle       = new Label();
            scaleButtonEdit1    = new ScaleButtonEdit();
            _btnSaveWeight      = new DevExpress.XtraEditors.SimpleButton();

            // Reference Values (Option A – compact 5-col grid)
            groupRefValues      = new DevExpress.XtraEditors.GroupControl();
            _grcRefValues       = new DevExpress.XtraGrid.GridControl();
            _grvRefValues       = new DevExpress.XtraGrid.Views.Grid.GridView();

            // History
            pnlHistory          = new Panel();
            pnlHistoryHeader    = new Panel();
            lblHistoryToggle    = new Label();
            _grcHistory         = new DevExpress.XtraGrid.GridControl();
            _grvHistory         = new DevExpress.XtraGrid.Views.Grid.GridView();

            // ── BOTTOM PANEL ────────────────────────────────────────────────────────
            panelControl1       = new DevExpress.XtraEditors.PanelControl();
            _btnCancel          = new DevExpress.XtraEditors.SimpleButton();
            _btnConfirm         = new DevExpress.XtraEditors.SimpleButton();
            _labVer             = new Label();

            // ════════════════ Begin Init ════════════════════════════════════════════
            ((System.ComponentModel.ISupportInitialize)groupInfo).BeginInit();
            groupInfo.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_toggleSwitchEnablePartition.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_comboBoxEditIsRunner.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_txtRemark.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_txtPercentOFusageNonwoven.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_txtStepCode.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_txtFGName.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_scanBarcode.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_txtActiclePairShot.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_txtFgItemCode.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_txtQty.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_txtStepIndex.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_txtSize.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_txtMachine.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_lkStepCode.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)gridLookUpEdit1View).BeginInit();
            ((System.ComponentModel.ISupportInitialize)scaleButtonEdit1.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)groupControl4).BeginInit();
            groupControl4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_txtRFIDName.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_txtRFIDCode.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)groupControl5).BeginInit();
            groupControl5.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_grcTotalStep).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_grvTotalStep).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_repositoryItemButtonEditScale).BeginInit();
            ((System.ComponentModel.ISupportInitialize)groupRefValues).BeginInit();
            groupRefValues.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_grcRefValues).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_grvRefValues).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_grcHistory).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_grvHistory).BeginInit();
            ((System.ComponentModel.ISupportInitialize)panelControl1).BeginInit();
            panelControl1.SuspendLayout();
            SuspendLayout();

            // ═══════════════════════════════════════════════════════════════════════
            //  LEFT PANEL  –  groupInfo  (STEP INFORMATION)
            //  x=5, y=45, w=285, h=950
            // ═══════════════════════════════════════════════════════════════════════
            groupInfo.AppearanceCaption.BorderColor = Color.FromArgb(43, 45, 66);
            groupInfo.AppearanceCaption.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            groupInfo.AppearanceCaption.Options.UseBorderColor = true;
            groupInfo.AppearanceCaption.Options.UseFont = true;
            groupInfo.Controls.Add(labelControl13);
            groupInfo.Controls.Add(_scanBarcode);
            groupInfo.Controls.Add(labelControl7);
            groupInfo.Controls.Add(_lkStepCode);
            groupInfo.Controls.Add(labelControl6);
            groupInfo.Controls.Add(_txtStepCode);
            groupInfo.Controls.Add(labelControl4);
            groupInfo.Controls.Add(labelControl1);
            groupInfo.Controls.Add(_txtMachine);
            groupInfo.Controls.Add(_txtQty);
            groupInfo.Controls.Add(labelControl23);
            groupInfo.Controls.Add(labelControl5);
            groupInfo.Controls.Add(_txtSize);
            groupInfo.Controls.Add(_txtStepIndex);
            groupInfo.Controls.Add(labelControl3);
            groupInfo.Controls.Add(_txtActiclePairShot);
            groupInfo.Controls.Add(_toggleSwitchEnablePartition);
            groupInfo.Controls.Add(labelControl11);
            groupInfo.Controls.Add(labelControl16);
            groupInfo.Controls.Add(_comboBoxEditIsRunner);
            groupInfo.Controls.Add(_txtPercentOFusageNonwoven);
            groupInfo.Controls.Add(labelControl2);
            groupInfo.Controls.Add(_txtFgItemCode);
            groupInfo.Controls.Add(labelControl12);
            groupInfo.Controls.Add(_txtFGName);
            groupInfo.Controls.Add(labelControl19);
            groupInfo.Controls.Add(_txtRemark);
            groupInfo.GroupStyle = GroupStyle.Card;
            groupInfo.Location = new Point(5, 45);
            groupInfo.Name = "groupInfo";
            groupInfo.Size = new Size(285, 950);
            groupInfo.TabIndex = 10;
            groupInfo.Text = "STEP INFORMATION";

            // shared fonts for compact left panel
            var lblFont   = new Font("Tahoma", 10F);
            var inputFont = new Font("Tahoma", 11F);

            // labelControl13 – QR Code
            labelControl13.Appearance.Font = lblFont;
            labelControl13.Appearance.Options.UseFont = true;
            labelControl13.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            labelControl13.Location = new Point(8, 28);
            labelControl13.Name = "labelControl13";
            labelControl13.Size = new Size(265, 18);
            labelControl13.TabIndex = 1;
            labelControl13.Text = "QR Code";

            // _scanBarcode
            _scanBarcode.Config = null;
            _scanBarcode.EditValue = "";
            _scanBarcode.Location = new Point(8, 46);
            _scanBarcode.Name = "_scanBarcode";
            _scanBarcode.Properties.Appearance.Font = inputFont;
            _scanBarcode.Properties.Appearance.Options.UseFont = true;
            _scanBarcode.Properties.AutoHeight = false;
            _scanBarcode.Properties.Buttons.AddRange(new EditorButton[] { new EditorButton() });
            _scanBarcode.Size = new Size(265, 32);
            _scanBarcode.TabIndex = 2;

            // labelControl7 – Step Name
            labelControl7.Appearance.Font = lblFont;
            labelControl7.Appearance.Options.UseFont = true;
            labelControl7.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            labelControl7.Location = new Point(8, 86);
            labelControl7.Name = "labelControl7";
            labelControl7.Size = new Size(265, 18);
            labelControl7.TabIndex = 3;
            labelControl7.Text = "Step Name";

            // _lkStepCode
            _lkStepCode.Location = new Point(8, 104);
            _lkStepCode.Name = "_lkStepCode";
            _lkStepCode.Properties.ActionButtonIndex = 1;
            _lkStepCode.Properties.Appearance.Font = inputFont;
            _lkStepCode.Properties.Appearance.Options.UseFont = true;
            _lkStepCode.Properties.AutoHeight = false;
            _lkStepCode.Properties.Buttons.AddRange(new EditorButton[]
            {
                new EditorButton(ButtonPredefines.Delete, "Delete", -1, true, true, false,
                    new EditorButtonImageOptions(), new KeyShortcut(Keys.None),
                    new SerializableAppearanceObject(), new SerializableAppearanceObject(),
                    new SerializableAppearanceObject(), new SerializableAppearanceObject(),
                    "", null, null, ToolTipAnchor.Default),
                new EditorButton(ButtonPredefines.Glyph, "Select", -1, true, true, false,
                    new EditorButtonImageOptions(), new KeyShortcut(Keys.None),
                    new SerializableAppearanceObject(), new SerializableAppearanceObject(),
                    new SerializableAppearanceObject(), new SerializableAppearanceObject(),
                    "", null, null, ToolTipAnchor.Default)
            });
            _lkStepCode.Properties.PopupView = gridLookUpEdit1View;
            _lkStepCode.Size = new Size(265, 32);
            _lkStepCode.TabIndex = 4;

            // gridLookUpEdit1View
            gridLookUpEdit1View.FocusRectStyle = DevExpress.XtraGrid.Views.Grid.DrawFocusRectStyle.RowFocus;
            gridLookUpEdit1View.Name = "gridLookUpEdit1View";
            gridLookUpEdit1View.OptionsSelection.EnableAppearanceFocusedCell = false;
            gridLookUpEdit1View.OptionsView.ShowGroupPanel = false;

            // labelControl6 – Step Code
            labelControl6.Appearance.Font = lblFont;
            labelControl6.Appearance.Options.UseFont = true;
            labelControl6.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            labelControl6.Location = new Point(8, 144);
            labelControl6.Name = "labelControl6";
            labelControl6.Size = new Size(265, 18);
            labelControl6.TabIndex = 5;
            labelControl6.Text = "Step Code";

            // _txtStepCode
            _txtStepCode.Location = new Point(8, 162);
            _txtStepCode.Name = "_txtStepCode";
            _txtStepCode.Properties.Appearance.Font = inputFont;
            _txtStepCode.Properties.Appearance.Options.UseFont = true;
            _txtStepCode.Properties.ReadOnly = true;
            _txtStepCode.Size = new Size(265, 32);
            _txtStepCode.TabIndex = 6;

            // labelControl4 – Machine  (left col)
            labelControl4.Appearance.Font = lblFont;
            labelControl4.Appearance.Options.UseFont = true;
            labelControl4.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            labelControl4.Location = new Point(8, 202);
            labelControl4.Name = "labelControl4";
            labelControl4.Size = new Size(125, 18);
            labelControl4.TabIndex = 7;
            labelControl4.Text = "Machine";

            // labelControl1 – Quantity (right col)
            labelControl1.Appearance.Font = lblFont;
            labelControl1.Appearance.Options.UseFont = true;
            labelControl1.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            labelControl1.Location = new Point(140, 202);
            labelControl1.Name = "labelControl1";
            labelControl1.Size = new Size(133, 18);
            labelControl1.TabIndex = 8;
            labelControl1.Text = "Quantity";

            // _txtMachine
            _txtMachine.Location = new Point(8, 220);
            _txtMachine.Name = "_txtMachine";
            _txtMachine.Properties.Appearance.Font = inputFont;
            _txtMachine.Properties.Appearance.Options.UseFont = true;
            _txtMachine.Properties.ReadOnly = true;
            _txtMachine.Size = new Size(125, 32);
            _txtMachine.TabIndex = 9;

            // _txtQty
            _txtQty.Location = new Point(140, 220);
            _txtQty.Name = "_txtQty";
            _txtQty.Properties.Appearance.Font = inputFont;
            _txtQty.Properties.Appearance.Options.UseFont = true;
            _txtQty.Properties.ReadOnly = true;
            _txtQty.Size = new Size(133, 32);
            _txtQty.TabIndex = 10;

            // labelControl23 – Size
            labelControl23.Appearance.Font = lblFont;
            labelControl23.Appearance.Options.UseFont = true;
            labelControl23.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            labelControl23.Location = new Point(8, 260);
            labelControl23.Name = "labelControl23";
            labelControl23.Size = new Size(125, 18);
            labelControl23.TabIndex = 11;
            labelControl23.Text = "Size";

            // labelControl5 – Sequence Index
            labelControl5.Appearance.Font = lblFont;
            labelControl5.Appearance.Options.UseFont = true;
            labelControl5.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            labelControl5.Location = new Point(140, 260);
            labelControl5.Name = "labelControl5";
            labelControl5.Size = new Size(133, 18);
            labelControl5.TabIndex = 12;
            labelControl5.Text = "Sequence Index";

            // _txtSize
            _txtSize.Location = new Point(8, 278);
            _txtSize.Name = "_txtSize";
            _txtSize.Properties.Appearance.Font = inputFont;
            _txtSize.Properties.Appearance.Options.UseFont = true;
            _txtSize.Properties.ReadOnly = true;
            _txtSize.Size = new Size(125, 32);
            _txtSize.TabIndex = 13;

            // _txtStepIndex
            _txtStepIndex.Location = new Point(140, 278);
            _txtStepIndex.Name = "_txtStepIndex";
            _txtStepIndex.Properties.Appearance.Font = inputFont;
            _txtStepIndex.Properties.Appearance.Options.UseFont = true;
            _txtStepIndex.Properties.ReadOnly = true;
            _txtStepIndex.Size = new Size(133, 32);
            _txtStepIndex.TabIndex = 14;

            // labelControl3 – Actual Partitioning
            labelControl3.Appearance.Font = lblFont;
            labelControl3.Appearance.Options.UseFont = true;
            labelControl3.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            labelControl3.Location = new Point(8, 318);
            labelControl3.Name = "labelControl3";
            labelControl3.Size = new Size(265, 18);
            labelControl3.TabIndex = 15;
            labelControl3.Text = "Actual Partitioning (Prs)";

            // _txtActiclePairShot
            _txtActiclePairShot.EditValue = "0";
            _txtActiclePairShot.Enabled = false;
            _txtActiclePairShot.Location = new Point(8, 336);
            _txtActiclePairShot.Name = "_txtActiclePairShot";
            _txtActiclePairShot.Properties.Appearance.BackColor = SystemColors.ButtonFace;
            _txtActiclePairShot.Properties.Appearance.Font = inputFont;
            _txtActiclePairShot.Properties.Appearance.Options.UseBackColor = true;
            _txtActiclePairShot.Properties.Appearance.Options.UseFont = true;
            _txtActiclePairShot.Properties.Appearance.Options.UseTextOptions = true;
            _txtActiclePairShot.Properties.Appearance.TextOptions.HAlignment = HorzAlignment.Far;
            _txtActiclePairShot.Size = new Size(265, 32);
            _txtActiclePairShot.TabIndex = 16;

            // _toggleSwitchEnablePartition
            _toggleSwitchEnablePartition.Location = new Point(8, 376);
            _toggleSwitchEnablePartition.Name = "_toggleSwitchEnablePartition";
            _toggleSwitchEnablePartition.Properties.Appearance.Font = new Font("Tahoma", 9F);
            _toggleSwitchEnablePartition.Properties.Appearance.Options.UseFont = true;
            _toggleSwitchEnablePartition.Properties.AutoHeight = false;
            _toggleSwitchEnablePartition.Properties.OffText = "Partitioning Adjustment: OFF";
            _toggleSwitchEnablePartition.Properties.OnText  = "Partitioning Adjustment: ON";
            _toggleSwitchEnablePartition.Size = new Size(265, 28);
            _toggleSwitchEnablePartition.TabIndex = 17;

            // labelControl11 – Have Runner
            labelControl11.Appearance.Font = lblFont;
            labelControl11.Appearance.Options.UseFont = true;
            labelControl11.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            labelControl11.Location = new Point(8, 412);
            labelControl11.Name = "labelControl11";
            labelControl11.Size = new Size(125, 18);
            labelControl11.TabIndex = 18;
            labelControl11.Text = "Have Runner";

            // labelControl16 – % of usage
            labelControl16.Appearance.Font = lblFont;
            labelControl16.Appearance.Options.UseFont = true;
            labelControl16.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            labelControl16.Location = new Point(140, 412);
            labelControl16.Name = "labelControl16";
            labelControl16.Size = new Size(133, 18);
            labelControl16.TabIndex = 19;
            labelControl16.Text = "% of usage";

            // _comboBoxEditIsRunner
            _comboBoxEditIsRunner.EditValue = "YES";
            _comboBoxEditIsRunner.Location = new Point(8, 430);
            _comboBoxEditIsRunner.Name = "_comboBoxEditIsRunner";
            _comboBoxEditIsRunner.Properties.Appearance.Font = inputFont;
            _comboBoxEditIsRunner.Properties.Appearance.Options.UseFont = true;
            _comboBoxEditIsRunner.Properties.AutoHeight = false;
            _comboBoxEditIsRunner.Size = new Size(125, 32);
            _comboBoxEditIsRunner.TabIndex = 20;

            // _txtPercentOFusageNonwoven
            _txtPercentOFusageNonwoven.EditValue = "0";
            _txtPercentOFusageNonwoven.Location = new Point(140, 430);
            _txtPercentOFusageNonwoven.Name = "_txtPercentOFusageNonwoven";
            _txtPercentOFusageNonwoven.Properties.Appearance.Font = inputFont;
            _txtPercentOFusageNonwoven.Properties.Appearance.Options.UseFont = true;
            _txtPercentOFusageNonwoven.Properties.Appearance.Options.UseTextOptions = true;
            _txtPercentOFusageNonwoven.Properties.Appearance.TextOptions.HAlignment = HorzAlignment.Far;
            _txtPercentOFusageNonwoven.Size = new Size(133, 32);
            _txtPercentOFusageNonwoven.TabIndex = 21;

            // labelControl2 – FG Item Code
            labelControl2.Appearance.Font = lblFont;
            labelControl2.Appearance.Options.UseFont = true;
            labelControl2.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            labelControl2.Location = new Point(8, 470);
            labelControl2.Name = "labelControl2";
            labelControl2.Size = new Size(265, 18);
            labelControl2.TabIndex = 22;
            labelControl2.Text = "FG Item Code";

            // _txtFgItemCode
            _txtFgItemCode.Location = new Point(8, 488);
            _txtFgItemCode.Name = "_txtFgItemCode";
            _txtFgItemCode.Properties.Appearance.Font = inputFont;
            _txtFgItemCode.Properties.Appearance.Options.UseFont = true;
            _txtFgItemCode.Properties.ReadOnly = true;
            _txtFgItemCode.Size = new Size(265, 32);
            _txtFgItemCode.TabIndex = 23;

            // labelControl12 – FG Description
            labelControl12.Appearance.Font = lblFont;
            labelControl12.Appearance.Options.UseFont = true;
            labelControl12.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            labelControl12.Location = new Point(8, 528);
            labelControl12.Name = "labelControl12";
            labelControl12.Size = new Size(265, 18);
            labelControl12.TabIndex = 24;
            labelControl12.Text = "FG Description";

            // _txtFGName
            _txtFGName.EditValue = "";
            _txtFGName.Location = new Point(8, 546);
            _txtFGName.Name = "_txtFGName";
            _txtFGName.Properties.Appearance.Font = new Font("Tahoma", 10F);
            _txtFGName.Properties.Appearance.Options.UseFont = true;
            _txtFGName.Properties.Appearance.Options.UseTextOptions = true;
            _txtFGName.Properties.Appearance.TextOptions.WordWrap = WordWrap.Wrap;
            _txtFGName.Properties.AutoHeight = false;
            _txtFGName.Properties.ReadOnly = true;
            _txtFGName.Size = new Size(265, 50);
            _txtFGName.TabIndex = 25;

            // labelControl19 – Remark
            labelControl19.Appearance.Font = lblFont;
            labelControl19.Appearance.Options.UseFont = true;
            labelControl19.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            labelControl19.Location = new Point(8, 604);
            labelControl19.Name = "labelControl19";
            labelControl19.Size = new Size(265, 18);
            labelControl19.TabIndex = 26;
            labelControl19.Text = "Remark";

            // _txtRemark
            _txtRemark.EditValue = "";
            _txtRemark.Location = new Point(8, 622);
            _txtRemark.Name = "_txtRemark";
            _txtRemark.Properties.Appearance.Font = inputFont;
            _txtRemark.Properties.Appearance.Options.UseFont = true;
            _txtRemark.Size = new Size(265, 32);
            _txtRemark.TabIndex = 27;

            // ═══════════════════════════════════════════════════════════════════════
            //  CENTER PANEL  –  groupControl5  (TOTAL STEPS)
            //  x=295, y=45, w=920, h=950
            // ═══════════════════════════════════════════════════════════════════════
            groupControl5.AppearanceCaption.BorderColor = Color.FromArgb(43, 45, 66);
            groupControl5.AppearanceCaption.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            groupControl5.AppearanceCaption.Options.UseBorderColor = true;
            groupControl5.AppearanceCaption.Options.UseFont = true;
            groupControl5.Controls.Add(_grcTotalStep);
            groupControl5.GroupStyle = GroupStyle.Card;
            groupControl5.Location = new Point(295, 45);
            groupControl5.Name = "groupControl5";
            groupControl5.Size = new Size(920, 950);
            groupControl5.TabIndex = 20;
            groupControl5.Text = "TOTAL STEPS";

            // _grcTotalStep
            _grcTotalStep.Dock = DockStyle.Fill;
            _grcTotalStep.EmbeddedNavigator.Buttons.CancelEdit.Visible = false;
            _grcTotalStep.EmbeddedNavigator.Buttons.Edit.Visible = false;
            _grcTotalStep.EmbeddedNavigator.Buttons.EndEdit.Visible = false;
            _grcTotalStep.EmbeddedNavigator.Buttons.Remove.Visible = false;
            _grcTotalStep.Location = new Point(2, 43);
            _grcTotalStep.MainView = _grvTotalStep;
            _grcTotalStep.Name = "_grcTotalStep";
            _grcTotalStep.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[]
            {
                _repositoryItemButtonEditScale
            });
            _grcTotalStep.Size = new Size(916, 905);
            _grcTotalStep.TabIndex = 0;
            _grcTotalStep.UseEmbeddedNavigator = true;
            _grcTotalStep.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[]
            {
                _grvTotalStep
            });

            // _grvTotalStep
            _grvTotalStep.DetailHeight = 292;
            _grvTotalStep.GridControl = _grcTotalStep;
            _grvTotalStep.Name = "_grvTotalStep";
            _grvTotalStep.OptionsEditForm.PopupEditFormWidth = 700;

            // _repositoryItemButtonEditScale
            _repositoryItemButtonEditScale.AutoHeight = false;
            _repositoryItemButtonEditScale.Buttons.AddRange(new EditorButton[]
            {
                new EditorButton(ButtonPredefines.Glyph, "Scale")
            });
            _repositoryItemButtonEditScale.Name = "_repositoryItemButtonEditScale";

            // ═══════════════════════════════════════════════════════════════════════
            //  RIGHT PANEL  –  pnlRight
            //  x=1220, y=45, w=695, h=950
            // ═══════════════════════════════════════════════════════════════════════
            pnlRight.Location = new Point(1220, 45);
            pnlRight.Name = "pnlRight";
            pnlRight.Size = new Size(695, 950);
            pnlRight.Controls.Add(groupControl4);
            pnlRight.Controls.Add(pnlScaleArea);
            pnlRight.Controls.Add(groupRefValues);
            pnlRight.Controls.Add(pnlHistory);

            // ── RFID group  (inside pnlRight)  y=0, h=128 ─────────────────────────
            groupControl4.AppearanceCaption.BorderColor = Color.FromArgb(43, 45, 66);
            groupControl4.AppearanceCaption.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            groupControl4.AppearanceCaption.Options.UseBorderColor = true;
            groupControl4.AppearanceCaption.Options.UseFont = true;
            groupControl4.Controls.Add(labelControl15);
            groupControl4.Controls.Add(_txtRFIDCode);
            groupControl4.Controls.Add(labelControl14);
            groupControl4.Controls.Add(_txtRFIDName);
            groupControl4.GroupStyle = GroupStyle.Card;
            groupControl4.Location = new Point(0, 0);
            groupControl4.Name = "groupControl4";
            groupControl4.Size = new Size(695, 128);
            groupControl4.TabIndex = 30;
            groupControl4.Text = "Scan RFID";

            // labelControl15 – ID
            labelControl15.Appearance.Font = new Font("Tahoma", 10F);
            labelControl15.Appearance.Options.UseFont = true;
            labelControl15.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            labelControl15.Location = new Point(8, 40);
            labelControl15.Name = "labelControl15";
            labelControl15.Size = new Size(305, 18);
            labelControl15.TabIndex = 1;
            labelControl15.Text = "ID";

            // _txtRFIDCode
            _txtRFIDCode.Config = null;
            _txtRFIDCode.Location = new Point(8, 58);
            _txtRFIDCode.Name = "_txtRFIDCode";
            _txtRFIDCode.Properties.Appearance.Font = new Font("Tahoma", 13F);
            _txtRFIDCode.Properties.Appearance.Options.UseFont = true;
            _txtRFIDCode.Properties.Buttons.AddRange(new EditorButton[]
            {
                new EditorButton(ButtonPredefines.OK),
                new EditorButton(ButtonPredefines.OK),
                new EditorButton(ButtonPredefines.OK)
            });
            _txtRFIDCode.Size = new Size(305, 40);
            _txtRFIDCode.TabIndex = 2;
            _txtRFIDCode.ToolTip = "COM3";

            // labelControl14 – Name
            labelControl14.Appearance.Font = new Font("Tahoma", 10F);
            labelControl14.Appearance.Options.UseFont = true;
            labelControl14.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            labelControl14.Location = new Point(325, 40);
            labelControl14.Name = "labelControl14";
            labelControl14.Size = new Size(358, 18);
            labelControl14.TabIndex = 3;
            labelControl14.Text = "Name";

            // _txtRFIDName
            _txtRFIDName.EditValue = "";
            _txtRFIDName.Location = new Point(325, 58);
            _txtRFIDName.Name = "_txtRFIDName";
            _txtRFIDName.Properties.Appearance.Font = new Font("Tahoma", 13F);
            _txtRFIDName.Properties.Appearance.Options.UseFont = true;
            _txtRFIDName.Size = new Size(358, 40);
            _txtRFIDName.TabIndex = 4;

            // ── Scale area  (inside pnlRight)  y=133, h=155 ──────────────────────
            pnlScaleArea.BackColor = Color.Transparent;
            pnlScaleArea.Location = new Point(0, 133);
            pnlScaleArea.Name = "pnlScaleArea";
            pnlScaleArea.Size = new Size(695, 155);
            pnlScaleArea.Controls.Add(lblScaleTitle);
            pnlScaleArea.Controls.Add(pnlScaleCard);

            // lblScaleTitle
            lblScaleTitle.AutoSize = true;
            lblScaleTitle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblScaleTitle.ForeColor = Color.FromArgb(33, 150, 243);
            lblScaleTitle.Location = new Point(5, 5);
            lblScaleTitle.Name = "lblScaleTitle";
            lblScaleTitle.Text = "●  SCALE VALUE (G)";

            // pnlScaleCard  – bordered card  (contains scale edit + save button)
            pnlScaleCard.BackColor = Color.White;
            pnlScaleCard.BorderStyle = BorderStyle.FixedSingle;
            pnlScaleCard.Location = new Point(5, 28);
            pnlScaleCard.Name = "pnlScaleCard";
            pnlScaleCard.Size = new Size(685, 120);
            pnlScaleCard.Controls.Add(scaleButtonEdit1);
            pnlScaleCard.Controls.Add(_btnSaveWeight);

            // scaleButtonEdit1  (large readout)
            scaleButtonEdit1.AutoDetectUnit = false;
            scaleButtonEdit1.BagWeight = 0D;
            scaleButtonEdit1.Config = null;
            scaleButtonEdit1.DecimalNum = 4;
            scaleButtonEdit1.EditValue = "0";
            scaleButtonEdit1.EnableReadScale = true;
            scaleButtonEdit1.Location = new Point(5, 10);
            scaleButtonEdit1.Name = "scaleButtonEdit1";
            scaleButtonEdit1.Properties.Appearance.Font = new Font("Consolas", 34F, FontStyle.Bold);
            scaleButtonEdit1.Properties.Appearance.Options.UseFont = true;
            scaleButtonEdit1.Properties.Appearance.Options.UseTextOptions = true;
            scaleButtonEdit1.Properties.Appearance.TextOptions.HAlignment = HorzAlignment.Far;
            scaleButtonEdit1.Properties.AutoHeight = false;
            scaleButtonEdit1.Properties.Buttons.AddRange(new EditorButton[] { new EditorButton() });
            scaleButtonEdit1.Size = new Size(445, 98);
            scaleButtonEdit1.Stable = true;
            scaleButtonEdit1.TabIndex = 50;
            scaleButtonEdit1.Tare = false;
            scaleButtonEdit1.UnitType = EmnumUnitType.gr;
            scaleButtonEdit1.ValueGram = 0D;
            scaleButtonEdit1.ValueKg = 0D;
            scaleButtonEdit1.ValueTon = 0D;

            // _btnSaveWeight
            _btnSaveWeight.Appearance.BackColor = Color.FromArgb(43, 45, 66);
            _btnSaveWeight.Appearance.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            _btnSaveWeight.Appearance.ForeColor = Color.White;
            _btnSaveWeight.Appearance.Options.UseBackColor = true;
            _btnSaveWeight.Appearance.Options.UseFont = true;
            _btnSaveWeight.Appearance.Options.UseForeColor = true;
            _btnSaveWeight.Location = new Point(460, 10);
            _btnSaveWeight.Name = "_btnSaveWeight";
            _btnSaveWeight.Size = new Size(218, 98);
            _btnSaveWeight.TabIndex = 51;
            _btnSaveWeight.Text = "↓  Save Value";

            // ── Reference Values group  (inside pnlRight)  y=293, h=380 ──────────
            // Option A: Compact split grid  –  # | Field (Unit) | STD | Δ | ACTUAL
            groupRefValues.AppearanceCaption.BorderColor = Color.FromArgb(43, 45, 66);
            groupRefValues.AppearanceCaption.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            groupRefValues.AppearanceCaption.Options.UseBorderColor = true;
            groupRefValues.AppearanceCaption.Options.UseFont = true;
            groupRefValues.Controls.Add(_grcRefValues);
            groupRefValues.GroupStyle = GroupStyle.Card;
            groupRefValues.Location = new Point(0, 293);
            groupRefValues.Name = "groupRefValues";
            groupRefValues.Size = new Size(695, 380);
            groupRefValues.TabIndex = 40;
            groupRefValues.Text = "REFERENCE VALUES  ·  Std (target) ↔ Actual (live)  ·  ±1% green  ·  ±3% amber  ·  >±3% red";

            // _grcRefValues
            _grcRefValues.Dock = DockStyle.Fill;
            _grcRefValues.Location = new Point(2, 43);
            _grcRefValues.MainView = _grvRefValues;
            _grcRefValues.Name = "_grcRefValues";
            _grcRefValues.Size = new Size(691, 335);
            _grcRefValues.TabIndex = 0;
            _grcRefValues.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[]
            {
                _grvRefValues
            });

            // _grvRefValues
            _grvRefValues.GridControl = _grcRefValues;
            _grvRefValues.Name = "_grvRefValues";
            _grvRefValues.OptionsBehavior.ReadOnly = true;
            _grvRefValues.OptionsView.ShowGroupPanel = false;
            _grvRefValues.OptionsView.ShowAutoFilterRow = false;
            _grvRefValues.OptionsView.RowAutoHeight = true;
            _grvRefValues.OptionsView.ShowFooter = false;
            _grvRefValues.OptionsView.ColumnAutoWidth = false;
            _grvRefValues.OptionsCustomization.AllowSort = false;
            _grvRefValues.OptionsCustomization.AllowFilter = false;

            // ── History panel  (inside pnlRight)  y=678, h=272 ───────────────────
            pnlHistory.BackColor = Color.Transparent;
            pnlHistory.Location = new Point(0, 678);
            pnlHistory.Name = "pnlHistory";
            pnlHistory.Size = new Size(695, 272);
            pnlHistory.Controls.Add(pnlHistoryHeader);
            pnlHistory.Controls.Add(_grcHistory);

            pnlHistoryHeader.BackColor = Color.FromArgb(230, 232, 240);
            pnlHistoryHeader.Cursor = Cursors.Hand;
            pnlHistoryHeader.Location = new Point(0, 0);
            pnlHistoryHeader.Name = "pnlHistoryHeader";
            pnlHistoryHeader.Size = new Size(695, 36);
            pnlHistoryHeader.Controls.Add(lblHistoryToggle);
            pnlHistoryHeader.Click += PnlHistoryHeader_Click;

            lblHistoryToggle.AutoSize = true;
            lblHistoryToggle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblHistoryToggle.ForeColor = Color.FromArgb(43, 45, 66);
            lblHistoryToggle.Location = new Point(8, 8);
            lblHistoryToggle.Name = "lblHistoryToggle";
            lblHistoryToggle.Text = "▶  REFERENCE / HISTORY  ·  last 5 weighings";
            lblHistoryToggle.Click += PnlHistoryHeader_Click;

            _grcHistory.Location = new Point(0, 36);
            _grcHistory.MainView = _grvHistory;
            _grcHistory.Name = "_grcHistory";
            _grcHistory.Size = new Size(695, 236);
            _grcHistory.TabIndex = 1;
            _grcHistory.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[]
            {
                _grvHistory
            });
            _grcHistory.Visible = false; // collapsed by default

            _grvHistory.GridControl = _grcHistory;
            _grvHistory.Name = "_grvHistory";
            _grvHistory.OptionsBehavior.ReadOnly = true;
            _grvHistory.OptionsView.ShowGroupPanel = false;
            _grvHistory.OptionsView.ColumnAutoWidth = false;

            // ═══════════════════════════════════════════════════════════════════════
            //  BOTTOM PANEL  –  panelControl1
            // ═══════════════════════════════════════════════════════════════════════
            panelControl1.Controls.Add(_labVer);
            panelControl1.Controls.Add(_btnCancel);
            panelControl1.Controls.Add(_btnConfirm);
            panelControl1.Dock = DockStyle.Bottom;
            panelControl1.Location = new Point(0, 1001);
            panelControl1.Name = "panelControl1";
            panelControl1.Size = new Size(1920, 79);
            panelControl1.TabIndex = 90;

            // _btnCancel
            _btnCancel.Appearance.BackColor = DevExpress.LookAndFeel.DXSkinColors.FillColors.Warning;
            _btnCancel.Appearance.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
            _btnCancel.Appearance.Options.UseBackColor = true;
            _btnCancel.Appearance.Options.UseFont = true;
            _btnCancel.Location = new Point(451, 14);
            _btnCancel.Name = "_btnCancel";
            _btnCancel.Size = new Size(454, 50);
            _btnCancel.TabIndex = 92;
            _btnCancel.Text = "Cancel";

            // _btnConfirm
            _btnConfirm.Appearance.BackColor = DevExpress.LookAndFeel.DXSkinColors.FillColors.Success;
            _btnConfirm.Appearance.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
            _btnConfirm.Appearance.Options.UseBackColor = true;
            _btnConfirm.Appearance.Options.UseFont = true;
            _btnConfirm.Location = new Point(1021, 14);
            _btnConfirm.Name = "_btnConfirm";
            _btnConfirm.Size = new Size(454, 50);
            _btnConfirm.TabIndex = 93;
            _btnConfirm.Text = "Confirm";

            // _labVer
            _labVer.Location = new Point(1706, 55);
            _labVer.Name = "_labVer";
            _labVer.Size = new Size(214, 20);
            _labVer.TabIndex = 94;
            _labVer.TextAlign = ContentAlignment.MiddleRight;

            // ═══════════════════════════════════════════════════════════════════════
            //  FORM  –  frmShotWeightScaleV2
            // ═══════════════════════════════════════════════════════════════════════
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1920, 1080);
            Controls.Add(panelControl1);
            Controls.Add(groupInfo);
            Controls.Add(groupControl5);
            Controls.Add(pnlRight);
            FormBorderStyle = FormBorderStyle.None;
            Margin = new Padding(4, 3, 4, 3);
            Name = "frmShotWeightScaleV2";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "SCALE SHOT WEIGHT – Option A";
            WindowState = FormWindowState.Maximized;

            // ════════════════ End Init ══════════════════════════════════════════════
            ((System.ComponentModel.ISupportInitialize)groupInfo).EndInit();
            groupInfo.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)_toggleSwitchEnablePartition.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)_comboBoxEditIsRunner.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)_txtRemark.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)_txtPercentOFusageNonwoven.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)_txtStepCode.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)_txtFGName.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)_scanBarcode.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)_txtActiclePairShot.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)_txtFgItemCode.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)_txtQty.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)_txtStepIndex.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)_txtSize.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)_txtMachine.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)_lkStepCode.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)gridLookUpEdit1View).EndInit();
            ((System.ComponentModel.ISupportInitialize)scaleButtonEdit1.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)groupControl4).EndInit();
            groupControl4.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)_txtRFIDName.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)_txtRFIDCode.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)groupControl5).EndInit();
            groupControl5.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)_grcTotalStep).EndInit();
            ((System.ComponentModel.ISupportInitialize)_grvTotalStep).EndInit();
            ((System.ComponentModel.ISupportInitialize)_repositoryItemButtonEditScale).EndInit();
            ((System.ComponentModel.ISupportInitialize)groupRefValues).EndInit();
            groupRefValues.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)_grcRefValues).EndInit();
            ((System.ComponentModel.ISupportInitialize)_grvRefValues).EndInit();
            ((System.ComponentModel.ISupportInitialize)_grcHistory).EndInit();
            ((System.ComponentModel.ISupportInitialize)_grvHistory).EndInit();
            ((System.ComponentModel.ISupportInitialize)panelControl1).EndInit();
            panelControl1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        // ═══ Ribbon groups (field declarations – needed by partial class) ═════════
        private DevExpress.XtraBars.Ribbon.RibbonPageGroup ribbonPageGroup6;
        private DevExpress.XtraBars.Ribbon.RibbonPageGroup ribbonPageGroup3;
        private DevExpress.XtraBars.Ribbon.RibbonPageGroup ribbonPageGroup4;

        // ═══ LEFT PANEL ═════════════════════════════════════════════════════════════
        private DevExpress.XtraEditors.GroupControl groupInfo;
        private DevExpress.XtraEditors.LabelControl  labelControl13;
        private BarcodeButtonEdit                    _scanBarcode;
        private DevExpress.XtraEditors.LabelControl  labelControl7;
        private DevExpress.XtraEditors.GridLookUpEdit _lkStepCode;
        private DevExpress.XtraGrid.Views.Grid.GridView gridLookUpEdit1View;
        private DevExpress.XtraEditors.LabelControl  labelControl6;
        private DevExpress.XtraEditors.TextEdit      _txtStepCode;
        private DevExpress.XtraEditors.LabelControl  labelControl4;
        private DevExpress.XtraEditors.LabelControl  labelControl1;
        private DevExpress.XtraEditors.TextEdit      _txtMachine;
        private DevExpress.XtraEditors.TextEdit      _txtQty;
        private DevExpress.XtraEditors.LabelControl  labelControl23;
        private DevExpress.XtraEditors.LabelControl  labelControl5;
        private DevExpress.XtraEditors.TextEdit      _txtSize;
        private DevExpress.XtraEditors.TextEdit      _txtStepIndex;
        private DevExpress.XtraEditors.LabelControl  labelControl3;
        private DevExpress.XtraEditors.TextEdit      _txtActiclePairShot;
        private DevExpress.XtraEditors.ToggleSwitch  _toggleSwitchEnablePartition;
        private DevExpress.XtraEditors.LabelControl  labelControl11;
        private DevExpress.XtraEditors.LabelControl  labelControl16;
        private DevExpress.XtraEditors.ComboBoxEdit  _comboBoxEditIsRunner;
        private DevExpress.XtraEditors.TextEdit      _txtPercentOFusageNonwoven;
        private DevExpress.XtraEditors.LabelControl  labelControl2;
        private DevExpress.XtraEditors.TextEdit      _txtFgItemCode;
        private DevExpress.XtraEditors.LabelControl  labelControl12;
        private DevExpress.XtraEditors.TextEdit      _txtFGName;
        private DevExpress.XtraEditors.LabelControl  labelControl19;
        private DevExpress.XtraEditors.TextEdit      _txtRemark;

        // ═══ CENTER PANEL ════════════════════════════════════════════════════════════
        private DevExpress.XtraEditors.GroupControl  groupControl5;
        private DevExpress.XtraGrid.GridControl      _grcTotalStep;
        private DevExpress.XtraGrid.Views.Grid.GridView _grvTotalStep;
        private DevExpress.XtraEditors.Repository.RepositoryItemButtonEdit _repositoryItemButtonEditScale;

        // ═══ RIGHT PANEL ═════════════════════════════════════════════════════════════
        private Panel                                pnlRight;

        // RFID
        private DevExpress.XtraEditors.GroupControl  groupControl4;
        private DevExpress.XtraEditors.LabelControl  labelControl15;
        private RFIDButtonEdit                       _txtRFIDCode;
        private DevExpress.XtraEditors.LabelControl  labelControl14;
        private DevExpress.XtraEditors.TextEdit      _txtRFIDName;

        // Scale display
        private Panel                                pnlScaleArea;
        private Panel                                pnlScaleCard;
        private Label                                lblScaleTitle;
        private ScaleButtonEdit                      scaleButtonEdit1;
        public  DevExpress.XtraEditors.SimpleButton  _btnSaveWeight;

        // Reference Values (Option A)
        private DevExpress.XtraEditors.GroupControl  groupRefValues;
        private DevExpress.XtraGrid.GridControl      _grcRefValues;
        private DevExpress.XtraGrid.Views.Grid.GridView _grvRefValues;

        // History
        private Panel                                pnlHistory;
        private Panel                                pnlHistoryHeader;
        private Label                                lblHistoryToggle;
        private DevExpress.XtraGrid.GridControl      _grcHistory;
        private DevExpress.XtraGrid.Views.Grid.GridView _grvHistory;

        // ═══ BOTTOM PANEL ════════════════════════════════════════════════════════════
        private DevExpress.XtraEditors.PanelControl  panelControl1;
        private DevExpress.XtraEditors.SimpleButton  _btnCancel;
        private DevExpress.XtraEditors.SimpleButton  _btnConfirm;
        private Label                                _labVer;
    }
}
