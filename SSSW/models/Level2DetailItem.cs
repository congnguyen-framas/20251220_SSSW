namespace SSSW.models
{
    /// <summary>
    /// Một dòng chi tiết BOM Level 2 hiển thị trong phần mở rộng (Row Details) của TOTAL STEPS.
    /// Chỉ dùng cho UI — không lưu DB.
    /// </summary>
    public class Level2DetailItem
    {
        public string Component { get; init; } = string.Empty;
        public string ComponentCode { get; init; } = string.Empty;

        /// <summary>Số step của chi tiết con ("REX" cho non-injection, "—" nếu chưa có).</summary>
        public string Step { get; init; } = "—";

        public double Qty { get; init; }
        public string? Unit { get; init; }

        /// <summary>Part Weight đã cân của chi tiết con (g/prs), null nếu chưa cân.</summary>
        public double? Weighed { get; init; }

        /// <summary>Trọng lượng 1 chiếc (g/piece), null nếu chưa cân.</summary>
        public double? PerPiece { get; init; }

        /// <summary>Phần bị trừ = Qty × PerPiece (g), null nếu chưa cân.</summary>
        public double? Deduct { get; init; }

        /// <summary>Hàng PU → g/chiếc; ngược lại g/đôi. UI only.</summary>
        public bool IsPu { get; init; }
        public string WeighedText => Weighed is { } v ? $"{v:F2} g/{(IsPu ? "pcs" : "prs")}" : "—";
        public string PerPieceText => PerPiece is { } v ? $"{v:F2} g/pcs" : "—";
        public string DeductText => Deduct is { } v ? $"{v:F2} g" : "—";

        public bool IsWeighed { get; init; }
        public string StatusText => IsWeighed ? "✔ done" : "✖ missing";
    }
}
