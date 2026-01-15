using DevExpress.XtraSpreadsheet.Model;
using Microsoft.EntityFrameworkCore;

namespace SSSW.models
{
    [Keyless]
    public class BomWinlineModel
    {
        public int? ParallelSequence { get; set; }
        public string? ItemStepCode { get; set; }
        public string? ItemStepName { get; set; }
        public double? Quantity { get; set; }
        public string? ItemFgCode { get; set; }
        public string? ItemFgName { get; set; }
        public string? SizeCode { get; set; }
        public string? Size { get; set; }

        public int? CategoryCode { get; set; }

        public string? CategoryName { get; set; } = string.Empty;

        public string? Unit { get; set; }
    }
}
