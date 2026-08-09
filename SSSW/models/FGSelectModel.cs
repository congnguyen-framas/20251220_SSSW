namespace SSSW.models
{
    /// <summary>
    /// Lookup DTO cho "Select FG" trong ShotWeightFGWindow — tương tự StepSelectModel
    /// nhưng chỉ giữ các trường liên quan đến FG (không có step/BOM concept).
    /// </summary>
    public class FgSelectModel
    {
        public string? FGCode { get; set; } = string.Empty;
        public string? FGName { get; set; } = string.Empty;
        public string? Machine { get; set; } = string.Empty;
        public string? Size { get; set; } = string.Empty;
        public string? HydraOrderNo { get; set; } = string.Empty;
        public Guid FT601Id { get; set; }

        /// <summary>Article Pairs Shot (FT601.C013) — dùng làm prsShot cho công thức cân lần 2 (C022).</summary>
        public int? ArticlePairsShot { get; set; }

        /// <summary>Main Code (FT601.C020).</summary>
        public string? MainCode { get; set; }

        /// <summary>Main item name (FT601.C003) — hiển thị cùng MainCode kiểu "MAIN / MAIN CODE" giống grid Total Steps.</summary>
        public string? MainName { get; set; }

        /// <summary>Article (FT601.C006).</summary>
        public string? Article { get; set; }

        /// <summary>Machine Group (FT601.C016).</summary>
        public string? MachineGroup { get; set; }

        /// <summary>Mold Id (FT601.C019).</summary>
        public string? MoldId { get; set; }

        /// <summary>Mold Pairs Shot (FT601.C014).</summary>
        public int? MoldPairsShot { get; set; }

        /// <summary>Category code — từ sp_GetCategorryOfItem, giống cách lấy bên form step.</summary>
        public int? CategoryCode { get; set; }

        /// <summary>Category name — từ sp_GetCategorryOfItem, giống cách lấy bên form step.</summary>
        public string? CategoryName { get; set; }

        /// <summary>Unit — từ sp_GetCategorryOfItem, giống cách lấy bên form step.</summary>
        public string? Unit { get; set; }

        /// <summary>QR code của label đã quét (chỉ có khi thêm sample qua barcode scan).</summary>
        public string? QrCode { get; set; }
    }
}
