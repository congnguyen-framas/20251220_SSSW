using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace SSSW.models
{
    public partial class FT600
    {
        [NotMapped]
        [Browsable(false)]
        public bool AllowScale { get; set; } = false;

        [NotMapped]
        public string? StatusText { get; set; } =string.Empty;
        [NotMapped]
        public string? StatusDotColor { get; set; } = string.Empty;
        [NotMapped]
        public string? StatusBarColor { get; set; } = string.Empty;

        /// <summary>Chi tiết BOM Level 2 của step (hiển thị trong Row Details). UI only.</summary>
        [NotMapped]
        [Browsable(false)]
        public IReadOnlyList<Level2DetailItem> Level2Items { get; set; } = Array.Empty<Level2DetailItem>();

        /// <summary>Công thức C021 = C023 − (…) hiển thị dưới bảng chi tiết. UI only.</summary>
        [NotMapped]
        [Browsable(false)]
        public string? Level2Formula { get; set; }

        /// <summary>
        /// true khi dòng được nạp lại từ lần cân trước (FT600 cũ) chứ không cân trong phiên này.
        /// Khi Confirm vẫn được insert lại nhưng đổi FG code/name theo FG của step đang scan/chọn. UI only.
        /// </summary>
        [NotMapped]
        [Browsable(false)]
        public bool IsReusedFromHistory { get; set; }

        /// <summary>Machine Group của hàng PU (FT601.C016 = 'PU', lưu ở FT600.C019).</summary>
        public const string PuMachineGroup = "PU";

        /// <summary>
        /// Hàng PU (không phải REX): cân theo chiếc (PCS), không theo đôi (PRS).
        /// Mold / PPIC / Actual Partitioning đọc ra đã là số chiếc; Unit = PCS-L / PCS-R.
        /// </summary>
        [NotMapped]
        [Browsable(false)]
        public bool IsPu =>
            string.Equals(C019?.Trim(), PuMachineGroup, StringComparison.OrdinalIgnoreCase) &&
            !(C002?.TrimStart().StartsWith("REX", StringComparison.OrdinalIgnoreCase) ?? false);

        /// <summary>
        /// Phía L / R của chi tiết PU theo hậu tố mã step (đoạn sau dấu '-' cuối cùng kết thúc bằng L hoặc R,
        /// vd. "...-L", "...-2801L"). Không nhận ra → null.
        /// </summary>
        public static string? PuSideOf(string? stepCode)
        {
            var code = stepCode?.Trim();
            if (string.IsNullOrEmpty(code)) return null;
            var seg = code[(code.LastIndexOf('-') + 1)..];
            if (seg.Length == 0) return null;
            var last = char.ToUpperInvariant(seg[^1]);
            if (last != 'L' && last != 'R') return null;
            return seg.Length == 1 || char.IsDigit(seg[^2]) || seg[^2] == '_' ? last.ToString() : null;
        }

        /// <summary>Đơn vị hiển thị / lưu của hàng PU: "PCS-L", "PCS-R" (không nhận ra phía → "PCS").</summary>
        public static string PuUnitOf(string? stepCode)
        {
            var side = PuSideOf(stepCode);
            return side == null ? "PCS" : $"PCS-{side}";
        }

        /// <summary>true khi còn chi tiết con chưa cân. UI only.</summary>
        [NotMapped]
        [Browsable(false)]
        public bool Level2HasMissing { get; set; }
    }
}
