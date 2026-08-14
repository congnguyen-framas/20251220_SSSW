// ============================================================================
//  OperatorSessionService.cs
//  Singleton owning the RFID/operator identity state (đã quét thẻ chưa, tên
//  nhân viên nào) DÙNG CHUNG cho cả 2 tab Step Component và Finished Goods.
//
//  Trước đây mỗi tab (ShotWeightViewModel/ShotWeightFGViewModel) tự giữ một bản
//  sao riêng của state này — quét thẻ ở tab Step không cập nhật bản sao của tab
//  FG (và ngược lại), nên khi chuyển qua lại giữa 2 tab, operator info trông như
//  bị xóa dù không ai bấm Cancel. Gộp về đây MỘT lần duy nhất giải quyết cả 2 vấn
//  đề: (1) info không còn "biến mất" khi đổi tab vì cả 2 tab đọc chung 1 instance,
//  (2) logic tra cứu/đăng ký operator (trùng lặp y hệt ở ShotWeightViewModel,
//  ShotWeightFGViewModel, và frmRfidInputViewModel) giờ chỉ tồn tại ở một chỗ.
//
//  Registered as a DI singleton in Program.cs. MainViewModel.StartupAsync() gọi
//  Attach(DeviceConnectionService) đúng 1 lần (mirror DeviceConnectionService's
//  EnsureInitialized() idempotent pattern) để subscribe sự kiện quét thẻ vật lý.
//  Step/FG ViewModel không còn giữ field/property RFID riêng nữa — đọc thẳng từ
//  đây; Cancel trên mỗi tab gọi Clear() (xem ShotWeightViewModel.ExecuteCancel()).
//  Namespace : SSSW.UI.WPF.Services
// ============================================================================
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScanAndScale.Core.Models;
using SSSW.models;
using SSSW.modelss;
using SSSW.UI.WPF.ViewModels;

namespace SSSW.UI.WPF.Services
{
    public class OperatorSessionService : BaseViewModel
    {
        private readonly IDbContextFactory<DbContextDogeWH> _dbFactory;
        private readonly ILogger<OperatorSessionService> _logger;

        public OperatorSessionService(
            IDbContextFactory<DbContextDogeWH> dbFactory,
            ILogger<OperatorSessionService> logger)
        {
            _dbFactory = dbFactory;
            _logger = logger;
        }

        // ── STATE (ported từ ShotWeightViewModel._employeeCode/_operatorInfo) ──
        private string _employeeCode = string.Empty;

        private FT029_Operator_RFID _operatorInfo = new();
        /// <summary>Operator hiện tại (rỗng/Id=Guid.Empty nếu chưa quét thẻ hợp lệ) — dùng để
        /// gate Confirm/Save và stamp C010/C011 lên FT600 (xem ShotWeightViewModel.ExecuteConfirmAsync()).</summary>
        public FT029_Operator_RFID OperatorInfo => _operatorInfo;

        /// <summary>true khi đã có operator hợp lệ (đã quét thẻ hoặc nhập tay thành công).</summary>
        public bool IsOperatorSet => _operatorInfo != null && _operatorInfo.Id != Guid.Empty;

        private DriverStatus _rfidStatus = DriverStatus.Unknown;
        public DriverStatus RfidStatus
        {
            get => _rfidStatus;
            private set => SetProperty(ref _rfidStatus, value);
        }

        private string _rfidCardCode = string.Empty;
        /// <summary>Mã thẻ RFID vừa quét (hiển thị trên header của Main).</summary>
        public string RfidCardCode
        {
            get => _rfidCardCode;
            private set => SetProperty(ref _rfidCardCode, value);
        }

        private string _rfidName = string.Empty;
        public string RfidName
        {
            get => _rfidName;
            private set => SetProperty(ref _rfidName, value);
        }

        private string _userName = string.Empty;
        public string UserName
        {
            get => _userName;
            private set => SetProperty(ref _userName, value);
        }

        // ── HARDWARE ATTACH ──────────────────────────────────────────────────
        private bool _attached;

        /// <summary>
        /// Idempotent, gọi 1 lần từ MainViewModel.StartupAsync() sau khi
        /// DeviceConnectionService.EnsureInitialized() đã chạy xong. Backfill trạng thái
        /// hiện tại rồi subscribe RfidChanged — thay thế RfidDriver_DataValueChanged trước
        /// đây từng nằm riêng ở ShotWeightWindow.xaml.cs VÀ ShotWeightFGWindow.xaml.cs.
        /// </summary>
        public void Attach(DeviceConnectionService device)
        {
            if (_attached) return;
            _attached = true;

            RfidStatus = device.RfidStatus;
            device.RfidChanged += Device_RfidChanged;
        }

        /// <summary>
        /// RfidDriver (SerialPort.DataReceived) → chạy trên ThreadPool thread, y hệt bridge cũ
        /// ở ShotWeightWindow.xaml.cs — vẫn phải Dispatcher.Invoke vì set property bindable.
        /// </summary>
        private void Device_RfidChanged(object? sender, DataValueChangedEventArgs e)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var data = e.NewValue;
                RfidStatus = data.DriverStatus;

                if (data.IsValid)
                {
                    var code = data.Value?.ToString() ?? string.Empty;
                    RfidCardCode = code;
                    OnRfidValueChanged(code);
                }
                else if (data.DriverStatus == DriverStatus.Disconnected)
                {
                    RfidCardCode = string.Empty;
                }
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        //  LOOKUP / REGISTER (ported nguyên vẹn từ ShotWeightViewModel.OnRfidValueChanged /
        //  OnRfidNameEnterAsync — logic này trước đây bị lặp lại y hệt ở
        //  ShotWeightViewModel, ShotWeightFGViewModel, và gần giống ở frmRfidInputViewModel).
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>RFID đọc được employee code (từ đầu đọc thẻ vật lý hoặc từ dialog nhập tay).
        /// Trả về true nếu LẦN GỌI NÀY tìm thấy operator hợp lệ — dùng bởi frmRfidInputViewModel
        /// để biết có nên coi là thất bại hay không, KHÔNG dựa vào IsOperatorSet (có thể vẫn true
        /// từ 1 lần quét thành công trước đó, không phản ánh đúng kết quả của lần gọi này).</summary>
        public bool OnRfidValueChanged(string rfidCode)
        {
            try
            {
                _employeeCode = rfidCode;
                if (string.IsNullOrEmpty(_employeeCode))
                    throw new Exception("ID cannot be null.");

                using var db = _dbFactory.CreateDbContext();

                // 1 nhân viên (C000) có thể có NHIỀU dòng FT029 — mỗi dòng ứng với 1 phòng ban/quyền (C002 → FT031).
                // Phải duyệt tất cả các dòng của nhân viên để tìm dòng có quyền IT/QC, thay vì chỉ xét dòng đầu tiên
                // (dòng đầu tiên trả về từ DB có thể là 1 phòng ban không có quyền, dù nhân viên có dòng khác hợp lệ).
                var operatorRows = db.fT029_Operator_RFIDs
                    .Where(x => x.C000.Contains(_employeeCode))
                    .ToList();

                if (operatorRows.Count == 0)
                {
                    RfidCardCode = string.Empty;
                    throw new Exception(
                        $"Employee ID {_employeeCode} not found. " +
                        "Please enter the name and press Enter to register.");
                }

                var allowedDepartments = db.FT031s
                    .Where(d => d.C000 == "IT" || d.C000 == "QC")
                    .ToList();

                _operatorInfo = operatorRows.FirstOrDefault(op => allowedDepartments.Any(d => d.Id == op.C002))
                                 ?? new FT029_Operator_RFID();

                if (_operatorInfo.Id == Guid.Empty)
                {
                    _employeeCode = string.Empty;
                    RfidName = string.Empty;
                    RfidCardCode = string.Empty;
                    throw new Exception("Employee does not have permission for this function.");
                }

                _operatorInfo.DepartmentInfor = allowedDepartments.First(d => d.Id == _operatorInfo.C002);

                RfidName = _operatorInfo.C001;
                UserName = $"{_operatorInfo.C000} - {_operatorInfo.C001}";
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RFID error");
                System.Windows.Forms.MessageBox.Show(ex.Message, "ERROR",
                    System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>Người dùng nhấn Enter trong ô RFID Name để đăng ký operator mới.</summary>
        public async Task OnRfidNameEnterAsync(string name)
        {
            try
            {
                if (string.IsNullOrEmpty(_employeeCode) || string.IsNullOrEmpty(name))
                    throw new Exception("ID or name cannot be null.");

                var res = System.Windows.Forms.MessageBox.Show(
                    $"Register operator {name} with ID {_employeeCode}?",
                    "Confirm", System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Question);
                if (res != System.Windows.Forms.DialogResult.Yes) return;

                using var db = _dbFactory.CreateDbContext();
                var dept = db.FT031s.FirstOrDefault(x => x.C000 == "QC")
                           ?? throw new Exception("Department 'QC' not found.");

                if (await db.fT029_Operator_RFIDs.AnyAsync(x => x.C000 == _employeeCode))
                    throw new Exception("ID already exists.");

                await db.fT029_Operator_RFIDs.AddAsync(new FT029_Operator_RFID
                {
                    Id = Guid.NewGuid(),
                    C000 = _employeeCode,
                    C001 = name,
                    C002 = dept.Id,
                    CreatedDate = DateTime.Now,
                    CreatedBy = string.Empty,
                    CreatedMachine = Environment.MachineName,
                    Actived = true
                });
                await db.SaveChangesAsync();
                System.Windows.Forms.MessageBox.Show(
                    $"Operator '{_employeeCode}-{name}' registered successfully.", "OK",
                    System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RFID name enter error");
                System.Windows.Forms.MessageBox.Show(ex.Message, "WARNING",
                    System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
            }
        }

        /// <summary>Gọi từ ExecuteCancel() của Step/FG — CHỈ nơi duy nhất được phép xóa
        /// operator info đã quét (theo yêu cầu: chuyển tab KHÔNG xóa, chỉ Cancel mới xóa).</summary>
        public void Clear()
        {
            _employeeCode = string.Empty;
            _operatorInfo = new FT029_Operator_RFID();
            RfidCardCode = string.Empty;
            RfidName = string.Empty;
            UserName = string.Empty;
        }
    }
}
