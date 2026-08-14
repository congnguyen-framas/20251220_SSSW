using SSSW.UI.WPF.Services;
using System;
using System.Threading.Tasks;

namespace SSSW.UI.WPF.ViewModels
{
    /// <summary>
    /// ViewModel cho dialog nhập tay Employee ID (frmRfidInput). Trước đây tự chứa 1 bản
    /// copy gần-y-hệt logic tra cứu/đăng ký operator (trùng với ShotWeightViewModel VÀ
    /// ShotWeightFGViewModel) — nay chỉ còn là lớp mỏng ủy quyền (delegate) toàn bộ việc
    /// tra cứu DB cho OperatorSessionService dùng chung, rồi copy kết quả về property riêng
    /// của dialog để hiển thị (frmRfidInput.xaml.cs đọc ResultRfidCode/ResultRfidName từ
    /// đây sau khi ShowDialog() == true và trả về cho MainViewModel.OpenRfidInputDialog()).
    /// </summary>
    public class frmRfidInputViewModel : BaseViewModel
    {
        private readonly OperatorSessionService _operatorSession;

        private string _rfidCardCode = string.Empty;
        /// <summary>Mã thẻ/Employee ID người dùng vừa gõ (hiển thị trên UI dialog).</summary>
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
            set { SetProperty(ref _userName, value); }
        }

        public frmRfidInputViewModel(OperatorSessionService operatorSession)
        {
            _operatorSession = operatorSession;

            // Default: xóa các giá trị hiển thị trong VM thay vì xóa WinForms TextBox
            ClearRfidAction = () => RfidCardCode = string.Empty;
        }

        public Action? ClearRfidAction { get; set; }

        public void OnRfidValueChanged()
        {
            // Dùng giá trị bool trả về (không dựa vào IsOperatorSet) — IsOperatorSet có thể vẫn
            // true từ 1 lần quét thành công TRƯỚC ĐÓ (VD trên tab Step) và không phản ánh đúng
            // kết quả của lần tra cứu này trên dialog.
            bool found = _operatorSession.OnRfidValueChanged(RfidCardCode);

            if (!found)
            {
                // Tra cứu thất bại (không tìm thấy/không có quyền) → đồng bộ lại field của dialog
                // này để textbox phản ánh đúng (yêu cầu nhập lại/gõ tên để đăng ký).
                ClearRfidAction?.Invoke();
                RfidName = string.Empty;
                UserName = string.Empty;
                return;
            }

            RfidCardCode = _operatorSession.RfidCardCode;
            RfidName = _operatorSession.RfidName;
            UserName = _operatorSession.UserName;
        }

        /// <summary>Người dùng nhấn Enter trong ô Employee Name để đăng ký operator mới (khi ID chưa tồn tại).</summary>
        public async Task OnRfidNameEnterAsync(string name)
        {
            await _operatorSession.OnRfidNameEnterAsync(name);

            // Đăng ký xong → tra cứu lại ngay để RfidName/UserName phản ánh operator vừa tạo,
            // để dialog có thể trả kết quả về form chính mà không cần người dùng nhấn Enter lại ở ô ID.
            OnRfidValueChanged();
        }
    }
}
