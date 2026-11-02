// ============================================================================
//  IDialogService.cs
//  Tách hộp thoại thông báo khỏi ViewModel (ViewModel không gọi WinForms MessageBox trực tiếp)
//  Namespace : SSSW.UI.WPF.Services
// ============================================================================
using System.Windows.Forms;

namespace SSSW.UI.WPF.Services
{
    public interface IDialogService
    {
        void Info(string message, string title = "Information");
        void Warn(string message, string title = "Warning");
        void Error(string message, string title = "ERROR");
    }

    /// <summary>Hiển thị bằng System.Windows.Forms.MessageBox (giữ nguyên hành vi cũ).</summary>
    public class MessageBoxDialogService : IDialogService
    {
        public void Info(string message, string title = "Information")
            => MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);

        public void Warn(string message, string title = "Warning")
            => MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);

        public void Error(string message, string title = "ERROR")
            => MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
