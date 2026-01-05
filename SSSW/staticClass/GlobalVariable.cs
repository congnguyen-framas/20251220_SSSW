using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSSW
{
    public class GlobalVariable
    {
        public static string ConStringDogeWH { get; set; }

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
    }
}
