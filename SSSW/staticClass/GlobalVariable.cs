using Microsoft.Extensions.Logging;
using ScanAndScale;
using SSSW.modelss;
using SSSW.UI.WPF.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSSW
{
    public class GlobalVariable
    {
        public static string ConStringSSSW { get; set; }

        public static ConfigModel ConfigSystem { get; set; } = new ConfigModel();

        /// <summary>
        /// Shared Barcode/RFID/Scale hardware connections (DeviceConnectionService singleton).
        /// Gán MỘT LẦN bởi MainViewModel ngay khi khởi tạo (trước khi bất kỳ tab Step/FG nào
        /// được resolve) — các ViewModel/Window khác đọc thẳng qua biến toàn cục này thay vì
        /// phải nhận qua constructor DI, để "form khác" (kể cả code không nằm trong DI graph
        /// của Main) vẫn lấy được kết nối RFID/Barcode/Scale đã sẵn sàng.
        /// </summary>
        public static DeviceConnectionService? Devices { get; set; }

        /// <summary>
        /// Invoke contol multi thread.
        /// </summary>
        /// <param name="control"></param>
        /// <param name="action"></param>
        public static void InvokeIfRequired(Control control, Action action)
        {
            if (control == null || control.IsDisposed)
                return; // hoặc throw exception tùy logic

            if (control.InvokeRequired)
                control.BeginInvoke(action);
            else
                action();
        }

        public static string PrefixUpToSecondHyphen(string? s)
        {
            if (string.IsNullOrEmpty(s) || !s.Contains("-")) return string.Empty;
            var parts = s.Split('-');
            // Ghép lại 2 phần đầu, nếu không đủ thì ghép những gì có
            return string.Join("-", parts.Take(2));
        }

        public static string PrefixUpToThirdHyphen(string? s)
        {
            if (string.IsNullOrEmpty(s) || !s.Contains("-")) return string.Empty;
            var parts = s.Split('-');
            // Ghép lại 2 phần đầu, nếu không đủ thì ghép những gì có
            return string.Join("-", parts.Take(3));
        }
    }
}
