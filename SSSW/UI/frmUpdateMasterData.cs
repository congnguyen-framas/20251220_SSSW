using DevExpress.Utils.Menu;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using Microsoft.EntityFrameworkCore;
using SSSW.models;
using SSSW.modelss;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SSSW
{
    public partial class frmUpdateMasterData : Form
    {
        //inject services
        private readonly IDbContextFactory<DbContextDogeWH> _dbFactory;

        private List<FT601> _data = new List<FT601>();

        public frmUpdateMasterData(IDbContextFactory<DbContextDogeWH> dbFactory) : this()
        {
            _dbFactory = dbFactory;
        }

        public frmUpdateMasterData()
        {
            InitializeComponent();

            Load += FrmUpdateMasterData_Load;
        }

        private async void FrmUpdateMasterData_Load(object? sender, EventArgs e)
        {
            #region Grid initialize
            _grv.OptionsView.ShowAutoFilterRow = true;
            _grv.OptionsCustomization.AllowFilter = true;
            _grv.OptionsView.ShowFilterPanelMode = DevExpress.XtraGrid.Views.Base.ShowFilterPanelMode.ShowAlways;
            _grv.OptionsView.ColumnAutoWidth = false;
            _grv.OptionsCustomization.AllowSort = true;
            _grv.OptionsBehavior.ReadOnly = true;
            _grv.OptionsView.ShowFooter = true;
            _grv.OptionsView.ShowGroupPanel = true;
            _grv.OptionsFind.AlwaysVisible = true;
            #endregion

            _grv.PopupMenuShowing += _grv_PopupMenuShowing;

            await RefreshAsync();
        }

        private void _grv_PopupMenuShowing(object sender, DevExpress.XtraGrid.Views.Grid.PopupMenuShowingEventArgs e)
        {
            try
            {
                // Check if the right-click was on a data row or the grid's empty area.
                // e.MenuType.Row is a good choice for data-related options.
                // e.MenuType.GroupRow is for group rows.
                if (e.MenuType == GridMenuType.Row)// || e.MenuType == GridMenuType.DataArea)
                {
                    // Ensure e.Menu is not null before adding an item.
                    if (e.Menu != null)
                    {
                        e.Menu.Items.Add(new DXMenuItem("Update unusage this size", new EventHandler(UnsuageSize)));
                    }
                }
                else if (e.MenuType == GridMenuType.Column)
                {
                    // You can also add items for the column header menu if needed.
                    // e.Menu.Items.Add(new DXMenuItem("..."));
                }
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Popup master view eror: " + ex.Message, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UnsuageSize(object? sender, EventArgs e)
        {
            try
            {
                int rowHandle = _grv.FocusedRowHandle;
                var rowSelected = _grv.GetRow(rowHandle) as FT601;

                if (rowSelected == null) return;

                using var dbContext = _dbFactory.CreateDbContext();
                var entity = dbContext.FT601s.FirstOrDefault(x => x.Id == rowSelected.Id);
                if (entity != null)
                {
                    entity.C021 = false; // Mark as unused
                    dbContext.SaveChanges();
                    XtraMessageBox.Show("Size marked as unused successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    // Refresh the grid to reflect changes
                    //_ = RefreshAsync();
                }
                else
                {
                    XtraMessageBox.Show("Selected size not found in the database.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("MixingLisr error: " + ex.Message, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task RefreshAsync()
        {
            using var dbContext = _dbFactory.CreateDbContext();

            _data = new List<FT601>();

            _data = await dbContext.FT601s
                .Where(x => x.C021 == true)
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();

            GlobalVariable.InvokeIfRequired(this, () =>
            {
                _grc.DataSource = null;
                _grc.DataSource = _data;
                _grv.PopulateColumns();
            });
        }
    }
}
