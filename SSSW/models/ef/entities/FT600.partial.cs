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

        /// <summary>true khi còn chi tiết con chưa cân. UI only.</summary>
        [NotMapped]
        [Browsable(false)]
        public bool Level2HasMissing { get; set; }
    }
}
