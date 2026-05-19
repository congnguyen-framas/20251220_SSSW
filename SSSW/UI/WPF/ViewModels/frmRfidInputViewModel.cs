using DevExpress.Data.Controls.ExpressionEditor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SSSW.modelss;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace SSSW.UI.WPF.ViewModels
{
    public class frmRfidInputViewModel : BaseViewModel
    {
        // ── DI ──────────────────────────────────────────────────────────────
        private readonly IDbContextFactory<DbContextDogeWH> _dbFactory;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ShotWeightViewModel> _logger;

        private string _rfidCardCode = string.Empty;
        /// <summary>Mã thẻ RFID vừa quét (hiển thị trên UI, khác với RfidName = tên nhân viên).</summary>
        public string RfidCardCode
        {
            get => _rfidCardCode;
            set { SetProperty(ref _rfidCardCode, value); }
        }

        // RFID name textbox
        private string _rfidName = "";
        public string RfidName
        {
            get => _rfidName;
            set
            {
                _rfidCardCode = value;
                SetProperty(ref _rfidName, value);
            }
        }

        private string _userName = string.Empty;
        public string UserName
        {
            get => _userName;
            set { _userName = value; SetProperty(ref _userName, value); }
        }

        public frmRfidInputViewModel(IDbContextFactory<DbContextDogeWH> dbFactory, IServiceProvider serviceProvider, ILogger<ShotWeightViewModel> logger)
        {
            _dbFactory = dbFactory;
            _serviceProvider = serviceProvider;
            _logger = logger;

            // Default: xóa các giá trị hiển thị trong VM thay vì xóa WinForms TextBox
            ClearRfidAction = () => RfidCardCode = string.Empty;
        }

        public Action? ClearRfidAction { get; set; }
        /// <summary>Code-behind gán: focus vào tbRFIDName.</summary>

        public void OnRfidValueChanged()
        {
            try
            { 
                if (string.IsNullOrEmpty(_rfidCardCode))
                    throw new Exception("ID cannot be null.");

                using var db = _dbFactory.CreateDbContext();
                var operatorInfo = db.fT029_Operator_RFIDs
                      .FirstOrDefault(x => x.C000.Contains(RfidCardCode));

                if (operatorInfo == null || operatorInfo.Id == Guid.Empty)
                {
                    ClearRfidAction?.Invoke();

                    throw new Exception(
                        $"Employee ID {RfidCardCode} not found. " +
                        "Please enter the name and press Enter to register.");
                }

                operatorInfo.DepartmentInfor = db.FT031s.FirstOrDefault(x =>
                    x.Id == operatorInfo.C002 && (x.C000 == "IT" || x.C000 == "QC"));

                if (operatorInfo.DepartmentInfor == null)
                {
                    RfidCardCode = string.Empty;
                    RfidName = string.Empty;
                    ClearRfidAction?.Invoke();
                    throw new Exception("Employee does not have permission for this function.");
                }

                RfidName = operatorInfo.C001;
                UserName = $"{operatorInfo.C000} · {operatorInfo.C001}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RFID error");
                System.Windows.Forms.MessageBox.Show(ex.Message, "ERROR",
                    (MessageBoxButtons)(MessageBoxButtons)MessageBoxButton.OK, (MessageBoxIcon)MessageBoxImage.Error);
            }
        }
    }
}
