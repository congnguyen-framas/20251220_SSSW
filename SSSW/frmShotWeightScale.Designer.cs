using DevExpress.Utils;
using DevExpress.XtraEditors.Controls;
using ScanAndScale.Driver;
using System.Windows.Forms;

namespace SSSW
{
    partial class frmShotWeightScale
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
            EditorButtonImageOptions editorButtonImageOptions1 = new EditorButtonImageOptions();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmShotWeightScale));
            SerializableAppearanceObject serializableAppearanceObject1 = new SerializableAppearanceObject();
            SerializableAppearanceObject serializableAppearanceObject2 = new SerializableAppearanceObject();
            SerializableAppearanceObject serializableAppearanceObject3 = new SerializableAppearanceObject();
            SerializableAppearanceObject serializableAppearanceObject4 = new SerializableAppearanceObject();
            EditorButtonImageOptions editorButtonImageOptions2 = new EditorButtonImageOptions();
            SerializableAppearanceObject serializableAppearanceObject5 = new SerializableAppearanceObject();
            SerializableAppearanceObject serializableAppearanceObject6 = new SerializableAppearanceObject();
            SerializableAppearanceObject serializableAppearanceObject7 = new SerializableAppearanceObject();
            SerializableAppearanceObject serializableAppearanceObject8 = new SerializableAppearanceObject();
            EditorButtonImageOptions editorButtonImageOptions3 = new EditorButtonImageOptions();
            SerializableAppearanceObject serializableAppearanceObject9 = new SerializableAppearanceObject();
            SerializableAppearanceObject serializableAppearanceObject10 = new SerializableAppearanceObject();
            SerializableAppearanceObject serializableAppearanceObject11 = new SerializableAppearanceObject();
            SerializableAppearanceObject serializableAppearanceObject12 = new SerializableAppearanceObject();
            EditorButtonImageOptions editorButtonImageOptions4 = new EditorButtonImageOptions();
            SerializableAppearanceObject serializableAppearanceObject13 = new SerializableAppearanceObject();
            SerializableAppearanceObject serializableAppearanceObject14 = new SerializableAppearanceObject();
            SerializableAppearanceObject serializableAppearanceObject15 = new SerializableAppearanceObject();
            SerializableAppearanceObject serializableAppearanceObject16 = new SerializableAppearanceObject();
            EditorButtonImageOptions editorButtonImageOptions5 = new EditorButtonImageOptions();
            SerializableAppearanceObject serializableAppearanceObject17 = new SerializableAppearanceObject();
            SerializableAppearanceObject serializableAppearanceObject18 = new SerializableAppearanceObject();
            SerializableAppearanceObject serializableAppearanceObject19 = new SerializableAppearanceObject();
            SerializableAppearanceObject serializableAppearanceObject20 = new SerializableAppearanceObject();
            EditorButtonImageOptions editorButtonImageOptions6 = new EditorButtonImageOptions();
            SerializableAppearanceObject serializableAppearanceObject21 = new SerializableAppearanceObject();
            SerializableAppearanceObject serializableAppearanceObject22 = new SerializableAppearanceObject();
            SerializableAppearanceObject serializableAppearanceObject23 = new SerializableAppearanceObject();
            SerializableAppearanceObject serializableAppearanceObject24 = new SerializableAppearanceObject();
            ribbonPageGroup6 = new DevExpress.XtraBars.Ribbon.RibbonPageGroup();
            ribbonPageGroup3 = new DevExpress.XtraBars.Ribbon.RibbonPageGroup();
            ribbonPageGroup4 = new DevExpress.XtraBars.Ribbon.RibbonPageGroup();
            _btnCancel = new DevExpress.XtraEditors.SimpleButton();
            _btnConfirm = new DevExpress.XtraEditors.SimpleButton();
            groupInfo = new DevExpress.XtraEditors.GroupControl();
            labelControl13 = new DevExpress.XtraEditors.LabelControl();
            _txtStepCode = new DevExpress.XtraEditors.TextEdit();
            labelControl12 = new DevExpress.XtraEditors.LabelControl();
            _txtFGName = new DevExpress.XtraEditors.TextEdit();
            _lkStepCode = new DevExpress.XtraEditors.LookUpEdit();
            _scanBarcode = new BarcodeButtonEdit();
            _toggleSwitchRunner = new DevExpress.XtraEditors.ToggleSwitch();
            labelControl11 = new DevExpress.XtraEditors.LabelControl();
            _txtActiclePairShot = new DevExpress.XtraEditors.TextEdit();
            labelControl3 = new DevExpress.XtraEditors.LabelControl();
            labelControl2 = new DevExpress.XtraEditors.LabelControl();
            _txtFgItemCode = new DevExpress.XtraEditors.TextEdit();
            _txtQty = new DevExpress.XtraEditors.TextEdit();
            labelControl1 = new DevExpress.XtraEditors.LabelControl();
            _txtStepIndex = new DevExpress.XtraEditors.TextEdit();
            labelControl7 = new DevExpress.XtraEditors.LabelControl();
            labelControl4 = new DevExpress.XtraEditors.LabelControl();
            labelControl5 = new DevExpress.XtraEditors.LabelControl();
            _txtMoldPairShot = new DevExpress.XtraEditors.TextEdit();
            _txtSize = new DevExpress.XtraEditors.TextEdit();
            labelControl6 = new DevExpress.XtraEditors.LabelControl();
            labelControl10 = new DevExpress.XtraEditors.LabelControl();
            _txtMachine = new DevExpress.XtraEditors.TextEdit();
            labelControl9 = new DevExpress.XtraEditors.LabelControl();
            _txtArticle = new DevExpress.XtraEditors.TextEdit();
            labelControl23 = new DevExpress.XtraEditors.LabelControl();
            _btnSaveWeight = new DevExpress.XtraEditors.SimpleButton();
            groupControl6 = new DevExpress.XtraEditors.GroupControl();
            _txtScaleValue = new ScaleButtonEdit();
            labelControl8 = new DevExpress.XtraEditors.LabelControl();
            groupControl4 = new DevExpress.XtraEditors.GroupControl();
            labelControl14 = new DevExpress.XtraEditors.LabelControl();
            _txtRFIDName = new DevExpress.XtraEditors.TextEdit();
            _txtRFIDCode = new RFIDButtonEdit();
            labelControl15 = new DevExpress.XtraEditors.LabelControl();
            groupControl5 = new DevExpress.XtraEditors.GroupControl();
            _grcTotalStep = new DevExpress.XtraGrid.GridControl();
            _grvTotalStep = new DevExpress.XtraGrid.Views.Grid.GridView();
            _repositoryItemButtonEditScale = new DevExpress.XtraEditors.Repository.RepositoryItemButtonEdit();
            panelControl1 = new DevExpress.XtraEditors.PanelControl();
            ((System.ComponentModel.ISupportInitialize)groupInfo).BeginInit();
            groupInfo.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_txtStepCode.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_txtFGName.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_lkStepCode.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_scanBarcode.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_toggleSwitchRunner.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_txtActiclePairShot.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_txtFgItemCode.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_txtQty.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_txtStepIndex.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_txtMoldPairShot.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_txtSize.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_txtMachine.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_txtArticle.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)groupControl6).BeginInit();
            groupControl6.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_txtScaleValue.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)groupControl4).BeginInit();
            groupControl4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_txtRFIDName.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_txtRFIDCode.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)groupControl5).BeginInit();
            groupControl5.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_grcTotalStep).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_grvTotalStep).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_repositoryItemButtonEditScale).BeginInit();
            ((System.ComponentModel.ISupportInitialize)panelControl1).BeginInit();
            panelControl1.SuspendLayout();
            SuspendLayout();
            // 
            // ribbonPageGroup6
            // 
            ribbonPageGroup6.Name = "ribbonPageGroup6";
            ribbonPageGroup6.Text = "Filter";
            // 
            // ribbonPageGroup3
            // 
            ribbonPageGroup3.Name = "ribbonPageGroup3";
            ribbonPageGroup3.Text = "Filter";
            // 
            // ribbonPageGroup4
            // 
            ribbonPageGroup4.Name = "ribbonPageGroup4";
            ribbonPageGroup4.Text = "Filter";
            // 
            // _btnCancel
            // 
            _btnCancel.Appearance.BackColor = DevExpress.LookAndFeel.DXSkinColors.FillColors.Warning;
            _btnCancel.Appearance.Font = new Font("Segoe UI", 23F, FontStyle.Bold);
            _btnCancel.Appearance.Options.UseBackColor = true;
            _btnCancel.Appearance.Options.UseFont = true;
            _btnCancel.ImageOptions.ImageToTextAlignment = DevExpress.XtraEditors.ImageAlignToText.LeftCenter;
            _btnCancel.Location = new Point(451, 19);
            _btnCancel.Margin = new Padding(3, 1, 3, 1);
            _btnCancel.Name = "_btnCancel";
            _btnCancel.Size = new Size(454, 50);
            _btnCancel.TabIndex = 25;
            _btnCancel.Text = "Cancel";
            // 
            // _btnConfirm
            // 
            _btnConfirm.Appearance.BackColor = DevExpress.LookAndFeel.DXSkinColors.FillColors.Success;
            _btnConfirm.Appearance.Font = new Font("Segoe UI", 23F, FontStyle.Bold);
            _btnConfirm.Appearance.Options.UseBackColor = true;
            _btnConfirm.Appearance.Options.UseFont = true;
            _btnConfirm.ImageOptions.ImageToTextAlignment = DevExpress.XtraEditors.ImageAlignToText.LeftCenter;
            _btnConfirm.Location = new Point(1021, 19);
            _btnConfirm.Margin = new Padding(3, 1, 3, 1);
            _btnConfirm.Name = "_btnConfirm";
            _btnConfirm.Size = new Size(454, 50);
            _btnConfirm.TabIndex = 27;
            _btnConfirm.Text = "Save";
            // 
            // groupInfo
            // 
            groupInfo.AppearanceCaption.BorderColor = Color.FromArgb(43, 45, 66);
            groupInfo.AppearanceCaption.Font = new Font("Segoe UI", 23F, FontStyle.Bold);
            groupInfo.AppearanceCaption.Options.UseBorderColor = true;
            groupInfo.AppearanceCaption.Options.UseFont = true;
            groupInfo.Controls.Add(labelControl13);
            groupInfo.Controls.Add(_txtStepCode);
            groupInfo.Controls.Add(labelControl12);
            groupInfo.Controls.Add(_txtFGName);
            groupInfo.Controls.Add(_lkStepCode);
            groupInfo.Controls.Add(_scanBarcode);
            groupInfo.Controls.Add(_toggleSwitchRunner);
            groupInfo.Controls.Add(labelControl11);
            groupInfo.Controls.Add(_txtActiclePairShot);
            groupInfo.Controls.Add(labelControl3);
            groupInfo.Controls.Add(labelControl2);
            groupInfo.Controls.Add(_txtFgItemCode);
            groupInfo.Controls.Add(_txtQty);
            groupInfo.Controls.Add(labelControl1);
            groupInfo.Controls.Add(_txtStepIndex);
            groupInfo.Controls.Add(labelControl7);
            groupInfo.Controls.Add(labelControl4);
            groupInfo.Controls.Add(labelControl5);
            groupInfo.Controls.Add(_txtMoldPairShot);
            groupInfo.Controls.Add(_txtSize);
            groupInfo.Controls.Add(labelControl6);
            groupInfo.Controls.Add(labelControl10);
            groupInfo.Controls.Add(_txtMachine);
            groupInfo.Controls.Add(labelControl9);
            groupInfo.Controls.Add(_txtArticle);
            groupInfo.Controls.Add(labelControl23);
            groupInfo.GroupStyle = GroupStyle.Card;
            groupInfo.Location = new Point(12, 55);
            groupInfo.Margin = new Padding(3, 1, 3, 1);
            groupInfo.Name = "groupInfo";
            groupInfo.Size = new Size(1425, 478);
            groupInfo.TabIndex = 31;
            groupInfo.Text = "Step information";
            // 
            // labelControl13
            // 
            labelControl13.Appearance.Font = new Font("Tahoma", 15F);
            labelControl13.Appearance.Options.UseFont = true;
            labelControl13.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            labelControl13.Location = new Point(13, 60);
            labelControl13.Margin = new Padding(3, 1, 3, 1);
            labelControl13.Name = "labelControl13";
            labelControl13.Size = new Size(102, 27);
            labelControl13.TabIndex = 92;
            labelControl13.Text = "QR Code";
            // 
            // _txtStepCode
            // 
            _txtStepCode.Location = new Point(204, 159);
            _txtStepCode.Margin = new Padding(3, 1, 3, 1);
            _txtStepCode.Name = "_txtStepCode";
            _txtStepCode.Properties.Appearance.Font = new Font("Tahoma", 20F);
            _txtStepCode.Properties.Appearance.Options.UseFont = true;
            _txtStepCode.Properties.ReadOnly = true;
            _txtStepCode.Size = new Size(601, 40);
            _txtStepCode.TabIndex = 91;
            // 
            // labelControl12
            // 
            labelControl12.Appearance.Font = new Font("Tahoma", 15F);
            labelControl12.Appearance.Options.UseFont = true;
            labelControl12.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            labelControl12.Location = new Point(13, 421);
            labelControl12.Margin = new Padding(3, 1, 3, 1);
            labelControl12.Name = "labelControl12";
            labelControl12.Size = new Size(151, 27);
            labelControl12.TabIndex = 89;
            labelControl12.Text = "FG Item Code";
            // 
            // _txtFGName
            // 
            _txtFGName.EditValue = "";
            _txtFGName.Location = new Point(204, 421);
            _txtFGName.Margin = new Padding(3, 1, 3, 1);
            _txtFGName.Name = "_txtFGName";
            _txtFGName.Properties.Appearance.Font = new Font("Tahoma", 15F);
            _txtFGName.Properties.Appearance.Options.UseFont = true;
            _txtFGName.Properties.Appearance.Options.UseTextOptions = true;
            _txtFGName.Properties.Appearance.TextOptions.WordWrap = WordWrap.Wrap;
            _txtFGName.Properties.AutoHeight = false;
            _txtFGName.Properties.ReadOnly = true;
            _txtFGName.Size = new Size(1210, 40);
            _txtFGName.TabIndex = 90;
            // 
            // _lkStepCode
            // 
            _lkStepCode.Location = new Point(204, 106);
            _lkStepCode.Name = "_lkStepCode";
            _lkStepCode.Properties.ActionButtonIndex = 1;
            _lkStepCode.Properties.Appearance.Font = new Font("Tahoma", 15F);
            _lkStepCode.Properties.Appearance.Options.UseFont = true;
            _lkStepCode.Properties.AutoHeight = false;
            editorButtonImageOptions1.SvgImage = (DevExpress.Utils.Svg.SvgImage)resources.GetObject("editorButtonImageOptions1.SvgImage");
            editorButtonImageOptions1.SvgImageSize = new Size(30, 30);
            editorButtonImageOptions2.SvgImage = (DevExpress.Utils.Svg.SvgImage)resources.GetObject("editorButtonImageOptions2.SvgImage");
            editorButtonImageOptions2.SvgImageSize = new Size(30, 30);
            _lkStepCode.Properties.Buttons.AddRange(new EditorButton[] { new EditorButton(ButtonPredefines.Glyph, "Delete", -1, true, true, false, editorButtonImageOptions1, new KeyShortcut(Keys.None), serializableAppearanceObject1, serializableAppearanceObject2, serializableAppearanceObject3, serializableAppearanceObject4, "", null, null, ToolTipAnchor.Default), new EditorButton(ButtonPredefines.Glyph, "Select", -1, true, true, false, editorButtonImageOptions2, new KeyShortcut(Keys.None), serializableAppearanceObject5, serializableAppearanceObject6, serializableAppearanceObject7, serializableAppearanceObject8, "", null, null, ToolTipAnchor.Default) });
            _lkStepCode.Properties.PopupFilterMode = DevExpress.XtraEditors.PopupFilterMode.Contains;
            _lkStepCode.Properties.SearchMode = SearchMode.AutoSuggest;
            _lkStepCode.Size = new Size(1210, 40);
            _lkStepCode.TabIndex = 88;
            // 
            // _scanBarcode
            // 
            _scanBarcode.Config = null;
            _scanBarcode.EditValue = "";
            _scanBarcode.Location = new Point(204, 53);
            _scanBarcode.Name = "_scanBarcode";
            _scanBarcode.Properties.Appearance.Font = new Font("Tahoma", 15F);
            _scanBarcode.Properties.Appearance.Options.UseFont = true;
            _scanBarcode.Properties.Appearance.Options.UseTextOptions = true;
            _scanBarcode.Properties.Appearance.TextOptions.WordWrap = WordWrap.Wrap;
            _scanBarcode.Properties.AutoHeight = false;
            _scanBarcode.Properties.Buttons.AddRange(new EditorButton[] { new EditorButton() });
            _scanBarcode.Size = new Size(1210, 40);
            _scanBarcode.TabIndex = 87;
            // 
            // _toggleSwitchRunner
            // 
            _toggleSwitchRunner.Location = new Point(1009, 329);
            _toggleSwitchRunner.Margin = new Padding(3, 2, 3, 2);
            _toggleSwitchRunner.Name = "_toggleSwitchRunner";
            _toggleSwitchRunner.Properties.OffText = "Off";
            _toggleSwitchRunner.Properties.OnText = "On";
            _toggleSwitchRunner.Size = new Size(108, 18);
            _toggleSwitchRunner.TabIndex = 83;
            // 
            // labelControl11
            // 
            labelControl11.Appearance.Font = new Font("Tahoma", 15F);
            labelControl11.Appearance.Options.UseFont = true;
            labelControl11.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            labelControl11.Location = new Point(834, 325);
            labelControl11.Margin = new Padding(3, 1, 3, 1);
            labelControl11.Name = "labelControl11";
            labelControl11.Size = new Size(127, 27);
            labelControl11.TabIndex = 82;
            labelControl11.Text = "Have Runner";
            // 
            // _txtActiclePairShot
            // 
            _txtActiclePairShot.EditValue = "0";
            _txtActiclePairShot.Location = new Point(204, 318);
            _txtActiclePairShot.Margin = new Padding(3, 1, 3, 1);
            _txtActiclePairShot.Name = "_txtActiclePairShot";
            _txtActiclePairShot.Properties.Appearance.Font = new Font("Tahoma", 20F);
            _txtActiclePairShot.Properties.Appearance.Options.UseFont = true;
            _txtActiclePairShot.Properties.Appearance.Options.UseTextOptions = true;
            _txtActiclePairShot.Properties.Appearance.TextOptions.HAlignment = HorzAlignment.Far;
            _txtActiclePairShot.Size = new Size(285, 40);
            _txtActiclePairShot.TabIndex = 1;
            // 
            // labelControl3
            // 
            labelControl3.Appearance.Font = new Font("Tahoma", 15F);
            labelControl3.Appearance.Options.UseFont = true;
            labelControl3.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            labelControl3.Location = new Point(13, 325);
            labelControl3.Margin = new Padding(3, 1, 3, 1);
            labelControl3.Name = "labelControl3";
            labelControl3.Size = new Size(169, 27);
            labelControl3.TabIndex = 79;
            labelControl3.Text = "Acticle's Pairs Shot";
            // 
            // labelControl2
            // 
            labelControl2.Appearance.Font = new Font("Tahoma", 15F);
            labelControl2.Appearance.Options.UseFont = true;
            labelControl2.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            labelControl2.Location = new Point(13, 378);
            labelControl2.Margin = new Padding(3, 1, 3, 1);
            labelControl2.Name = "labelControl2";
            labelControl2.Size = new Size(151, 27);
            labelControl2.TabIndex = 76;
            labelControl2.Text = "FG Item Code";
            // 
            // _txtFgItemCode
            // 
            _txtFgItemCode.Location = new Point(204, 371);
            _txtFgItemCode.Margin = new Padding(3, 1, 3, 1);
            _txtFgItemCode.Name = "_txtFgItemCode";
            _txtFgItemCode.Properties.Appearance.Font = new Font("Tahoma", 20F);
            _txtFgItemCode.Properties.Appearance.Options.UseFont = true;
            _txtFgItemCode.Properties.ReadOnly = true;
            _txtFgItemCode.Size = new Size(405, 40);
            _txtFgItemCode.TabIndex = 77;
            // 
            // _txtQty
            // 
            _txtQty.Location = new Point(668, 212);
            _txtQty.Margin = new Padding(3, 1, 3, 1);
            _txtQty.Name = "_txtQty";
            _txtQty.Properties.Appearance.Font = new Font("Tahoma", 20F);
            _txtQty.Properties.Appearance.Options.UseFont = true;
            _txtQty.Properties.ReadOnly = true;
            _txtQty.Size = new Size(137, 40);
            _txtQty.TabIndex = 74;
            // 
            // labelControl1
            // 
            labelControl1.Appearance.Font = new Font("Tahoma", 15F);
            labelControl1.Appearance.Options.UseFont = true;
            labelControl1.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            labelControl1.Location = new Point(549, 219);
            labelControl1.Margin = new Padding(3, 1, 3, 1);
            labelControl1.Name = "labelControl1";
            labelControl1.Size = new Size(78, 27);
            labelControl1.TabIndex = 75;
            labelControl1.Text = "Quantity";
            // 
            // _txtStepIndex
            // 
            _txtStepIndex.Location = new Point(1009, 159);
            _txtStepIndex.Margin = new Padding(3, 1, 3, 1);
            _txtStepIndex.Name = "_txtStepIndex";
            _txtStepIndex.Properties.Appearance.Font = new Font("Tahoma", 20F);
            _txtStepIndex.Properties.Appearance.Options.UseFont = true;
            _txtStepIndex.Properties.ReadOnly = true;
            _txtStepIndex.Size = new Size(405, 40);
            _txtStepIndex.TabIndex = 72;
            // 
            // labelControl7
            // 
            labelControl7.Appearance.Font = new Font("Tahoma", 15F);
            labelControl7.Appearance.Options.UseFont = true;
            labelControl7.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            labelControl7.Location = new Point(13, 113);
            labelControl7.Margin = new Padding(3, 1, 3, 1);
            labelControl7.Name = "labelControl7";
            labelControl7.Size = new Size(102, 27);
            labelControl7.TabIndex = 60;
            labelControl7.Text = "Step Name";
            // 
            // labelControl4
            // 
            labelControl4.Appearance.Font = new Font("Tahoma", 15F);
            labelControl4.Appearance.Options.UseFont = true;
            labelControl4.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            labelControl4.Location = new Point(13, 219);
            labelControl4.Margin = new Padding(3, 1, 3, 1);
            labelControl4.Name = "labelControl4";
            labelControl4.Size = new Size(102, 27);
            labelControl4.TabIndex = 57;
            labelControl4.Text = "Machine";
            // 
            // labelControl5
            // 
            labelControl5.Appearance.Font = new Font("Tahoma", 15F);
            labelControl5.Appearance.Options.UseFont = true;
            labelControl5.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            labelControl5.LineLocation = DevExpress.XtraEditors.LineLocation.Left;
            labelControl5.Location = new Point(834, 166);
            labelControl5.Margin = new Padding(3, 1, 3, 1);
            labelControl5.Name = "labelControl5";
            labelControl5.Size = new Size(151, 27);
            labelControl5.TabIndex = 73;
            labelControl5.Text = "Sequence Index";
            // 
            // _txtMoldPairShot
            // 
            _txtMoldPairShot.Location = new Point(204, 265);
            _txtMoldPairShot.Margin = new Padding(3, 1, 3, 1);
            _txtMoldPairShot.Name = "_txtMoldPairShot";
            _txtMoldPairShot.Properties.Appearance.Font = new Font("Tahoma", 20F);
            _txtMoldPairShot.Properties.Appearance.Options.UseFont = true;
            _txtMoldPairShot.Properties.ReadOnly = true;
            _txtMoldPairShot.Size = new Size(285, 40);
            _txtMoldPairShot.TabIndex = 70;
            // 
            // _txtSize
            // 
            _txtSize.Location = new Point(1009, 212);
            _txtSize.Margin = new Padding(3, 1, 3, 1);
            _txtSize.Name = "_txtSize";
            _txtSize.Properties.Appearance.Font = new Font("Tahoma", 20F);
            _txtSize.Properties.Appearance.Options.UseFont = true;
            _txtSize.Properties.ReadOnly = true;
            _txtSize.Size = new Size(405, 40);
            _txtSize.TabIndex = 64;
            // 
            // labelControl6
            // 
            labelControl6.Appearance.Font = new Font("Tahoma", 15F);
            labelControl6.Appearance.Options.UseFont = true;
            labelControl6.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            labelControl6.LineLocation = DevExpress.XtraEditors.LineLocation.Left;
            labelControl6.Location = new Point(13, 166);
            labelControl6.Margin = new Padding(3, 1, 3, 1);
            labelControl6.Name = "labelControl6";
            labelControl6.Size = new Size(158, 27);
            labelControl6.TabIndex = 67;
            labelControl6.Text = "Step Code";
            // 
            // labelControl10
            // 
            labelControl10.Appearance.Font = new Font("Tahoma", 15F);
            labelControl10.Appearance.Options.UseFont = true;
            labelControl10.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            labelControl10.Location = new Point(834, 272);
            labelControl10.Margin = new Padding(3, 1, 3, 1);
            labelControl10.Name = "labelControl10";
            labelControl10.Size = new Size(88, 27);
            labelControl10.TabIndex = 68;
            labelControl10.Text = "Article";
            // 
            // _txtMachine
            // 
            _txtMachine.Location = new Point(204, 212);
            _txtMachine.Margin = new Padding(3, 1, 3, 1);
            _txtMachine.Name = "_txtMachine";
            _txtMachine.Properties.Appearance.Font = new Font("Tahoma", 20F);
            _txtMachine.Properties.Appearance.Options.UseFont = true;
            _txtMachine.Properties.ReadOnly = true;
            _txtMachine.Size = new Size(285, 40);
            _txtMachine.TabIndex = 61;
            // 
            // labelControl9
            // 
            labelControl9.Appearance.Font = new Font("Tahoma", 15F);
            labelControl9.Appearance.Options.UseFont = true;
            labelControl9.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            labelControl9.Location = new Point(13, 272);
            labelControl9.Margin = new Padding(3, 1, 3, 1);
            labelControl9.Name = "labelControl9";
            labelControl9.Size = new Size(169, 27);
            labelControl9.TabIndex = 71;
            labelControl9.Text = "Mold's Pairs Shot";
            // 
            // _txtArticle
            // 
            _txtArticle.Location = new Point(1009, 265);
            _txtArticle.Margin = new Padding(3, 1, 3, 1);
            _txtArticle.Name = "_txtArticle";
            _txtArticle.Properties.Appearance.Font = new Font("Tahoma", 20F);
            _txtArticle.Properties.Appearance.Options.UseFont = true;
            _txtArticle.Properties.ReadOnly = true;
            _txtArticle.Size = new Size(405, 40);
            _txtArticle.TabIndex = 69;
            // 
            // labelControl23
            // 
            labelControl23.Appearance.Font = new Font("Tahoma", 15F);
            labelControl23.Appearance.Options.UseFont = true;
            labelControl23.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            labelControl23.Location = new Point(834, 219);
            labelControl23.Margin = new Padding(3, 1, 3, 1);
            labelControl23.Name = "labelControl23";
            labelControl23.Size = new Size(53, 27);
            labelControl23.TabIndex = 65;
            labelControl23.Text = "Size";
            // 
            // _btnSaveWeight
            // 
            _btnSaveWeight.Appearance.BackColor = Color.FromArgb(43, 45, 66);
            _btnSaveWeight.Appearance.Font = new Font("Segoe UI", 23F, FontStyle.Bold);
            _btnSaveWeight.Appearance.Options.UseBackColor = true;
            _btnSaveWeight.Appearance.Options.UseFont = true;
            _btnSaveWeight.ImageOptions.ImageToTextAlignment = DevExpress.XtraEditors.ImageAlignToText.LeftCenter;
            _btnSaveWeight.Location = new Point(151, 178);
            _btnSaveWeight.Margin = new Padding(3, 1, 3, 1);
            _btnSaveWeight.Name = "_btnSaveWeight";
            _btnSaveWeight.Size = new Size(290, 40);
            _btnSaveWeight.TabIndex = 37;
            _btnSaveWeight.Text = "Save Value";
            // 
            // groupControl6
            // 
            groupControl6.AppearanceCaption.BorderColor = Color.FromArgb(43, 45, 66);
            groupControl6.AppearanceCaption.Font = new Font("Segoe UI", 23F, FontStyle.Bold);
            groupControl6.AppearanceCaption.Options.UseBorderColor = true;
            groupControl6.AppearanceCaption.Options.UseFont = true;
            groupControl6.Controls.Add(_btnSaveWeight);
            groupControl6.Controls.Add(_txtScaleValue);
            groupControl6.Controls.Add(labelControl8);
            groupControl6.GroupStyle = GroupStyle.Card;
            groupControl6.Location = new Point(1453, 298);
            groupControl6.Margin = new Padding(3, 1, 3, 1);
            groupControl6.Name = "groupControl6";
            groupControl6.Size = new Size(455, 235);
            groupControl6.TabIndex = 34;
            groupControl6.Text = "Scale";
            // 
            // _txtScaleValue
            // 
            _txtScaleValue.AutoDetectUnit = false;
            _txtScaleValue.BagWeight = 0D;
            _txtScaleValue.Config = null;
            _txtScaleValue.DecimalNum = 4;
            _txtScaleValue.EnableReadScale = true;
            _txtScaleValue.Location = new Point(13, 112);
            _txtScaleValue.Margin = new Padding(3, 1, 3, 1);
            _txtScaleValue.Name = "_txtScaleValue";
            _txtScaleValue.Properties.Appearance.Font = new Font("Tahoma", 20F, FontStyle.Bold);
            _txtScaleValue.Properties.Appearance.Options.UseFont = true;
            _txtScaleValue.Properties.Appearance.Options.UseTextOptions = true;
            _txtScaleValue.Properties.Appearance.TextOptions.HAlignment = HorzAlignment.Far;
            _txtScaleValue.Properties.Buttons.AddRange(new EditorButton[] { new EditorButton() });
            _txtScaleValue.Properties.DisplayFormat.FormatString = "n3";
            _txtScaleValue.Properties.DisplayFormat.FormatType = FormatType.Numeric;
            _txtScaleValue.Size = new Size(428, 40);
            _txtScaleValue.Stable = true;
            _txtScaleValue.TabIndex = 18;
            _txtScaleValue.Tare = false;
            _txtScaleValue.UnitType = EmnumUnitType.gr;
            _txtScaleValue.ValueGram = 0D;
            _txtScaleValue.ValueKg = 0D;
            _txtScaleValue.ValueTon = 0D;
            // 
            // labelControl8
            // 
            labelControl8.Appearance.Font = new Font("Tahoma", 15F);
            labelControl8.Appearance.Options.UseFont = true;
            labelControl8.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            labelControl8.Location = new Point(13, 59);
            labelControl8.Margin = new Padding(3, 1, 3, 1);
            labelControl8.Name = "labelControl8";
            labelControl8.Size = new Size(145, 27);
            labelControl8.TabIndex = 12;
            labelControl8.Text = "Scale value (g)";
            // 
            // groupControl4
            // 
            groupControl4.AppearanceCaption.BorderColor = Color.FromArgb(43, 45, 66);
            groupControl4.AppearanceCaption.Font = new Font("Segoe UI", 23F, FontStyle.Bold);
            groupControl4.AppearanceCaption.Options.UseBorderColor = true;
            groupControl4.AppearanceCaption.Options.UseFont = true;
            groupControl4.Controls.Add(labelControl14);
            groupControl4.Controls.Add(_txtRFIDName);
            groupControl4.Controls.Add(_txtRFIDCode);
            groupControl4.Controls.Add(labelControl15);
            groupControl4.GroupStyle = GroupStyle.Card;
            groupControl4.Location = new Point(1453, 55);
            groupControl4.Margin = new Padding(3, 1, 3, 1);
            groupControl4.Name = "groupControl4";
            groupControl4.Size = new Size(455, 235);
            groupControl4.TabIndex = 33;
            groupControl4.Text = "Scan RFID";
            // 
            // labelControl14
            // 
            labelControl14.Appearance.Font = new Font("Tahoma", 15F);
            labelControl14.Appearance.Options.UseFont = true;
            labelControl14.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            labelControl14.Location = new Point(13, 133);
            labelControl14.Margin = new Padding(3, 1, 3, 1);
            labelControl14.Name = "labelControl14";
            labelControl14.Size = new Size(83, 27);
            labelControl14.TabIndex = 15;
            labelControl14.Text = "Name";
            // 
            // _txtRFIDName
            // 
            _txtRFIDName.EditValue = "CONG NGUYEN";
            _txtRFIDName.Location = new Point(13, 169);
            _txtRFIDName.Margin = new Padding(3, 1, 3, 1);
            _txtRFIDName.Name = "_txtRFIDName";
            _txtRFIDName.Properties.Appearance.Font = new Font("Tahoma", 20F);
            _txtRFIDName.Properties.Appearance.Options.UseFont = true;
            _txtRFIDName.Size = new Size(428, 40);
            _txtRFIDName.TabIndex = 14;
            // 
            // _txtRFIDCode
            // 
            _txtRFIDCode.Config = null;
            _txtRFIDCode.Location = new Point(13, 84);
            _txtRFIDCode.Margin = new Padding(3, 1, 3, 1);
            _txtRFIDCode.Name = "_txtRFIDCode";
            _txtRFIDCode.Properties.Appearance.Font = new Font("Tahoma", 20F);
            _txtRFIDCode.Properties.Appearance.Options.UseFont = true;
            _txtRFIDCode.Properties.Buttons.AddRange(new EditorButton[] { new EditorButton(ButtonPredefines.OK, "", -1, true, false, false, editorButtonImageOptions3, new KeyShortcut(Keys.None), serializableAppearanceObject9, serializableAppearanceObject10, serializableAppearanceObject11, serializableAppearanceObject12, "", null, null, ToolTipAnchor.Default), new EditorButton(ButtonPredefines.OK, "", -1, true, false, false, editorButtonImageOptions4, new KeyShortcut(Keys.None), serializableAppearanceObject13, serializableAppearanceObject14, serializableAppearanceObject15, serializableAppearanceObject16, "", null, null, ToolTipAnchor.Default), new EditorButton(ButtonPredefines.OK, "", -1, true, false, false, editorButtonImageOptions5, new KeyShortcut(Keys.None), serializableAppearanceObject17, serializableAppearanceObject18, serializableAppearanceObject19, serializableAppearanceObject20, "", null, null, ToolTipAnchor.Default) });
            _txtRFIDCode.Size = new Size(428, 40);
            _txtRFIDCode.TabIndex = 13;
            _txtRFIDCode.ToolTip = "COM3";
            // 
            // labelControl15
            // 
            labelControl15.Appearance.Font = new Font("Tahoma", 15F);
            labelControl15.Appearance.Options.UseFont = true;
            labelControl15.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            labelControl15.Location = new Point(13, 48);
            labelControl15.Margin = new Padding(3, 1, 3, 1);
            labelControl15.Name = "labelControl15";
            labelControl15.Size = new Size(83, 27);
            labelControl15.TabIndex = 12;
            labelControl15.Text = "ID";
            // 
            // groupControl5
            // 
            groupControl5.AppearanceCaption.BorderColor = Color.FromArgb(43, 45, 66);
            groupControl5.AppearanceCaption.Font = new Font("Segoe UI", 23F, FontStyle.Bold);
            groupControl5.AppearanceCaption.Options.UseBorderColor = true;
            groupControl5.AppearanceCaption.Options.UseFont = true;
            groupControl5.Controls.Add(_grcTotalStep);
            groupControl5.GroupStyle = GroupStyle.Card;
            groupControl5.Location = new Point(10, 549);
            groupControl5.Margin = new Padding(3, 1, 3, 1);
            groupControl5.Name = "groupControl5";
            groupControl5.Size = new Size(1898, 448);
            groupControl5.TabIndex = 34;
            groupControl5.Text = "Total Steps";
            // 
            // _grcTotalStep
            // 
            _grcTotalStep.Dock = DockStyle.Fill;
            _grcTotalStep.EmbeddedNavigator.Buttons.CancelEdit.Visible = false;
            _grcTotalStep.EmbeddedNavigator.Buttons.Edit.Visible = false;
            _grcTotalStep.EmbeddedNavigator.Buttons.EndEdit.Visible = false;
            _grcTotalStep.EmbeddedNavigator.Buttons.Remove.Visible = false;
            _grcTotalStep.EmbeddedNavigator.Margin = new Padding(3, 2, 3, 2);
            _grcTotalStep.Location = new Point(2, 43);
            _grcTotalStep.MainView = _grvTotalStep;
            _grcTotalStep.Margin = new Padding(3, 2, 3, 2);
            _grcTotalStep.Name = "_grcTotalStep";
            _grcTotalStep.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] { _repositoryItemButtonEditScale });
            _grcTotalStep.Size = new Size(1894, 403);
            _grcTotalStep.TabIndex = 36;
            _grcTotalStep.UseEmbeddedNavigator = true;
            _grcTotalStep.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] { _grvTotalStep });
            // 
            // _grvTotalStep
            // 
            _grvTotalStep.DetailHeight = 292;
            _grvTotalStep.GridControl = _grcTotalStep;
            _grvTotalStep.Name = "_grvTotalStep";
            _grvTotalStep.OptionsEditForm.PopupEditFormWidth = 700;
            // 
            // _repositoryItemButtonEditScale
            // 
            _repositoryItemButtonEditScale.AutoHeight = false;
            _repositoryItemButtonEditScale.Buttons.AddRange(new EditorButton[] { new EditorButton(ButtonPredefines.Glyph, "Scale", -1, true, true, false, editorButtonImageOptions6, new KeyShortcut(Keys.None), serializableAppearanceObject21, serializableAppearanceObject22, serializableAppearanceObject23, serializableAppearanceObject24, "", null, null, ToolTipAnchor.Default) });
            _repositoryItemButtonEditScale.Name = "_repositoryItemButtonEditScale";
            // 
            // panelControl1
            // 
            panelControl1.Controls.Add(_btnCancel);
            panelControl1.Controls.Add(_btnConfirm);
            panelControl1.Dock = DockStyle.Bottom;
            panelControl1.Location = new Point(0, 1001);
            panelControl1.Name = "panelControl1";
            panelControl1.Size = new Size(1920, 79);
            panelControl1.TabIndex = 35;
            // 
            // frmShotWeightScale
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1920, 1080);
            Controls.Add(panelControl1);
            Controls.Add(groupControl5);
            Controls.Add(groupControl4);
            Controls.Add(groupInfo);
            Controls.Add(groupControl6);
            FormBorderStyle = FormBorderStyle.None;
            Margin = new Padding(4, 3, 4, 3);
            Name = "frmShotWeightScale";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "SCALE SHOT WEIGHT";
            WindowState = FormWindowState.Maximized;
            ((System.ComponentModel.ISupportInitialize)groupInfo).EndInit();
            groupInfo.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)_txtStepCode.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)_txtFGName.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)_lkStepCode.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)_scanBarcode.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)_toggleSwitchRunner.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)_txtActiclePairShot.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)_txtFgItemCode.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)_txtQty.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)_txtStepIndex.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)_txtMoldPairShot.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)_txtSize.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)_txtMachine.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)_txtArticle.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)groupControl6).EndInit();
            groupControl6.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)_txtScaleValue.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)groupControl4).EndInit();
            groupControl4.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)_txtRFIDName.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)_txtRFIDCode.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)groupControl5).EndInit();
            groupControl5.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)_grcTotalStep).EndInit();
            ((System.ComponentModel.ISupportInitialize)_grvTotalStep).EndInit();
            ((System.ComponentModel.ISupportInitialize)_repositoryItemButtonEditScale).EndInit();
            ((System.ComponentModel.ISupportInitialize)panelControl1).EndInit();
            panelControl1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private DevExpress.XtraBars.Ribbon.RibbonPageGroup ribbonPageGroup6;
        private DevExpress.XtraBars.Ribbon.RibbonPageGroup ribbonPageGroup3;
        private DevExpress.XtraBars.Ribbon.RibbonPageGroup ribbonPageGroup4;
        private DevExpress.XtraEditors.LookUpEdit lupReason;
        private DevExpress.XtraEditors.SimpleButton btnClearAll;
        private DevExpress.XtraEditors.SimpleButton btnSave2;
        private DevExpress.XtraEditors.SimpleButton btnSkip;
        private DevExpress.XtraEditors.SimpleButton _btnConfirm;
        private DevExpress.XtraLayout.EmptySpaceItem emptySpaceItem2;
        private DevExpress.XtraEditors.TextEdit teWeightOrder;
        private DevExpress.XtraEditors.TextEdit teWeightPlastic;
        private DevExpress.XtraEditors.TextEdit teWeightColor;
        private DevExpress.XtraEditors.TextEdit teWeightRecycle;
        private DevExpress.XtraEditors.SimpleButton _btnCancel;
        private DevExpress.XtraEditors.DateEdit teOrderTime;
        private DevExpress.XtraGrid.GridControl gridMain;
        private DevExpress.XtraGrid.Views.Grid.GridView viewMain;
        private DevExpress.XtraEditors.GroupControl groupInfo;
        private DevExpress.XtraEditors.LabelControl labelControl10;
        private DevExpress.XtraEditors.LabelControl labelControl17;
        private DevExpress.XtraEditors.GroupControl groupControl6;
        private ScaleButtonEdit _txtScaleValue;
        private DevExpress.XtraEditors.LabelControl labelControl8;
        private DevExpress.XtraEditors.GroupControl groupControl4;
        private DevExpress.XtraEditors.LabelControl labelControl14;
        private DevExpress.XtraEditors.TextEdit _txtRFIDName;
        private RFIDButtonEdit _txtRFIDCode;
        private DevExpress.XtraEditors.LabelControl labelControl15;
        private DevExpress.XtraEditors.MemoEdit teGuide;
        private DevExpress.XtraEditors.GroupControl groupControl2;
        private DevExpress.XtraGrid.GridControl gridGuid;
        private DevExpress.XtraGrid.Views.Grid.GridView _grvTotalStep;
        private DevExpress.XtraGrid.Columns.GridColumn gridColumn1;
        private DevExpress.XtraGrid.Columns.GridColumn gridColumn2;
        private DevExpress.XtraGrid.Columns.GridColumn gridColumn3;
        private DevExpress.XtraGrid.Columns.GridColumn colIsMixColor;
        private DevExpress.XtraGrid.Columns.GridColumn colMaterialCode;
        private DevExpress.XtraGrid.Columns.GridColumn colMaterialName;
        private DevExpress.XtraGrid.Columns.GridColumn colRecipe;
        private DevExpress.XtraGrid.Columns.GridColumn colRatio;
        private DevExpress.XtraGrid.Columns.GridColumn colGuideWeight;
        private DevExpress.XtraGrid.Columns.GridColumn colTargetWeight;
        private DevExpress.XtraGrid.Columns.GridColumn colActualWeight;
        private DevExpress.XtraGrid.Columns.GridColumn colIsRecipe;
        private DevExpress.XtraGrid.Columns.GridColumn colErrorPercent;
        private DevExpress.XtraGrid.Columns.GridColumn colTypeStr;
        private DevExpress.XtraGrid.Views.Grid.GridView viewDetail;
        private DevExpress.XtraGrid.Columns.GridColumn gridColumn4;
        private DevExpress.XtraGrid.Columns.GridColumn gridColumn5;
        private DevExpress.XtraGrid.Columns.GridColumn colRatioOfWeight;
        private DevExpress.XtraEditors.TextEdit _txtStepIndex;
        private DevExpress.XtraGrid.Columns.GridColumn colRatioOfPercent;
        private DevExpress.XtraEditors.Repository.RepositoryItemToggleSwitch rtogMixColor;
        private DevExpress.XtraGrid.Columns.GridColumn colPercentOfMaterial;
        private DevExpress.XtraEditors.LabelControl labelControl18;
        private DevExpress.XtraEditors.LabelControl labelControl20;
        private DevExpress.XtraEditors.TextEdit teRatioLO;
        private DevExpress.XtraEditors.TextEdit teRatioRE;
        private DevExpress.XtraEditors.LabelControl labelControl21;
        private DevExpress.XtraEditors.LabelControl labelControl22;
        private DevExpress.XtraEditors.TextEdit _txtMoldPairShot;
        private DevExpress.XtraEditors.TextEdit teWeightMixTarget;
        private DevExpress.XtraGrid.Columns.GridColumn colPercentOfRecycle;
        private DevExpress.XtraGrid.Columns.GridColumn colAllow;
        private DevExpress.XtraEditors.ToggleSwitch togHideMaterialNotUse;
        private DevExpress.XtraGrid.Columns.GridColumn gridColumn6;
        private DevExpress.XtraEditors.SimpleButton simpleButton1;
        public DevExpress.XtraEditors.SimpleButton _btnSaveWeight;
        private DevExpress.XtraEditors.TextEdit _txtSize;
        private DevExpress.XtraEditors.LabelControl labelControl23;
        private DevExpress.XtraEditors.SimpleButton btnChooseRecipe;
        private DevExpress.XtraEditors.TextEdit _txtMachine;
        private DevExpress.XtraEditors.LabelControl labelControl7;
        private DevExpress.XtraEditors.LabelControl labelControl4;
        private DevExpress.XtraEditors.LabelControl labelControl6;
        private DevExpress.XtraEditors.LabelControl labelControl5;
        private DevExpress.XtraEditors.LabelControl labelControl9;
        private DevExpress.XtraEditors.TextEdit _txtArticle;
        private DevExpress.XtraEditors.Repository.RepositoryItemMemoEdit repositoryItemMemoEdit1;
        private DevExpress.XtraGrid.Columns.GridColumn gridColumn7;
        private DevExpress.XtraEditors.GroupControl groupControl5;
        private DevExpress.XtraGrid.GridControl _grcTotalStep;
        private DevExpress.XtraEditors.Repository.RepositoryItemButtonEdit _repositoryItemButtonEditScale;
        private DevExpress.XtraEditors.TextEdit _txtQty;
        private DevExpress.XtraEditors.LabelControl labelControl1;
        private DevExpress.XtraEditors.LabelControl labelControl2;
        private DevExpress.XtraEditors.TextEdit _txtFgItemCode;
        private DevExpress.XtraEditors.TextEdit _txtActiclePairShot;
        private DevExpress.XtraEditors.LabelControl labelControl3;
        private DevExpress.XtraEditors.LabelControl labelControl11;
        private DevExpress.XtraEditors.ToggleSwitch _toggleSwitchRunner;
        private BarcodeButtonEdit _scanBarcode;
        private DevExpress.XtraEditors.LookUpEdit _lkStepCode;
        private DevExpress.XtraEditors.LabelControl labelControl12;
        private DevExpress.XtraEditors.TextEdit _labFGName;
        private DevExpress.XtraEditors.LabelControl labelControl13;
        private DevExpress.XtraEditors.TextEdit _txtStepCode;
        private DevExpress.XtraEditors.PanelControl panelControl1;
        private DevExpress.XtraEditors.TextEdit _txtFGName;
    }
}
