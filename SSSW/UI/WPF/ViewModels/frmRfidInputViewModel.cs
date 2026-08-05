using DevExpress.Data.Controls.ExpressionEditor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SSSW.models;
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
            set { SetProperty(ref _rfidName, value); }
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

        /// <summary>Người dùng nhấn Enter trong ô Employee Name để đăng ký operator mới (khi ID chưa tồn tại).</summary>
        public async Task OnRfidNameEnterAsync(string name)
        {
            try
            {
                if (string.IsNullOrEmpty(RfidCardCode) || string.IsNullOrEmpty(name))
                    throw new Exception("ID or name cannot be null.");

                var res = System.Windows.Forms.MessageBox.Show(
                    $"Register operator {name} with ID {RfidCardCode}?",
                    "Confirm", (MessageBoxButtons)MessageBoxButton.YesNo, (MessageBoxIcon)MessageBoxImage.Question);
                if (res != DialogResult.Yes) return;

                using var db = _dbFactory.CreateDbContext();
                var dept = db.FT031s.FirstOrDefault(x => x.C000 == "QC")
                           ?? throw new Exception("Department 'QC' not found.");

                if (await db.fT029_Operator_RFIDs.AnyAsync(x => x.C000 == RfidCardCode))
                    throw new Exception("ID already exists.");

                await db.fT029_Operator_RFIDs.AddAsync(new FT029_Operator_RFID
                {
                    Id = Guid.NewGuid(),
                    C000 = RfidCardCode,
                    C001 = name,
                    C002 = dept.Id,
                    CreatedDate = DateTime.Now,
                    CreatedBy = string.Empty,
                    CreatedMachine = Environment.MachineName,
                    Actived = true
                });
                await db.SaveChangesAsync();

                // Đăng ký xong → tra cứu lại ngay để RfidName/UserName phản ánh operator vừa tạo,
                // để dialog có thể trả kết quả về form chính mà không cần người dùng nhấn Enter lại ở ô ID.
                OnRfidValueChanged();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RFID name enter error");
                System.Windows.Forms.MessageBox.Show(ex.Message, "WARNING",
                    (MessageBoxButtons)MessageBoxButton.OK, (MessageBoxIcon)(MessageBoxIcon)MessageBoxImage.Warning);
            }
        }
    }
}
