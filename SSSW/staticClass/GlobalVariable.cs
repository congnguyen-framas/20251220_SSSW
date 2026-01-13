using Microsoft.Extensions.Logging;
using ScanAndScale.Driver;
using SSSW.modelss;
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
            if (string.IsNullOrEmpty(s)) return string.Empty;
            var parts = s.Split('-');
            // Ghép lại 2 phần đầu, nếu không đủ thì ghép những gì có
            return string.Join("-", parts.Take(2));
        }

    }
}
