// ============================================================================
//  BaseViewModel.cs
//  Lớp base cho tất cả ViewModel – cung cấp INotifyPropertyChanged
//  Namespace : SSSW.UI.WPF.ViewModels
// ============================================================================
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SSSW.UI.WPF.ViewModels
{
    /// <summary>
    /// Base class cho tất cả ViewModel trong MVVM pattern.
    /// Implement <see cref="INotifyPropertyChanged"/> để WPF binding tự động
    /// cập nhật UI khi property thay đổi.
    /// </summary>
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        // ── INotifyPropertyChanged ─────────────────────────────────────────
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Thông báo WPF rằng property đã thay đổi.
        /// Gọi từ setter của property — [CallerMemberName] tự động điền tên property.
        /// </summary>
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        /// <summary>
        /// Helper: gán giá trị mới và tự động gọi OnPropertyChanged nếu giá trị thay đổi.
        /// Dùng trong setter để tránh lặp code kiểm tra + notify.
        /// </summary>
        /// <returns>true nếu giá trị thực sự thay đổi.</returns>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
