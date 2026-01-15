using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
    public partial class frmMainView : Form
    {
        //inject services
        private readonly IDbContextFactory<DbContextDogeWH> _dbFactory;

        private List<FT600> _dataScale = new List<FT600>();

        public frmMainView(IDbContextFactory<DbContextDogeWH> dbFactory) : this()
        {
            _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
        }

        public frmMainView()
        {
            InitializeComponent();

            Load +=  FrmMainView_Load;
        }

        private async void FrmMainView_Load(object? sender, EventArgs e)
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

            await RefreshAsync();
        }

        private async Task RefreshAsync()
        {
            using var dbContext = _dbFactory.CreateDbContext();

            _dataScale = new List<FT600>();

            _dataScale = await dbContext.FT600s
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();

            GlobalVariable.InvokeIfRequired(this, () =>
            {
                _grc.DataSource = null;
                _grc.DataSource = _dataScale;
                _grv.PopulateColumns();
            });
        }
    }
}
