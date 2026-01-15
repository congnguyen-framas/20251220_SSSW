using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace SSSW.models
{
    public class HydraItemDetailModel
    {
        /// <summary>
        /// phân biệt product và sample, 21/22 - sample.
        /// </summary>
        [Browsable(false)] public string? HydraOrderType { get; set; }

        [NotMapped]
        public string? Location
        {
            get { return HydraOrderType != "21" || HydraOrderType != "22" ? "Production" : "Sample"; }
        }
        [NotMapped]
        public string? Size { get; set; } = string.Empty;
        public string? MainName { get; set; } = string.Empty;

        public string? StepItemCode { get; set; } = string.Empty;
        /// <summary>
        /// Dùng để short các bước chạy.
        /// </summary>
        public int? StepIndex { get; set; } = 0;
        public int? MoldPairShot { get; set; } = 0;
        public string? StepItemName { get; set; } = string.Empty;
        public string? FGItemCode { get; set; } = string.Empty;
        public string? Machine { get; set; } = string.Empty;
        public string? MainCode { get; set; } = string.Empty;
        public string? Artikel { get; set; } = string.Empty;

        public int? ArticlePairShot { get; set; } = 0;

        public string? MachineGroup { get; set; } = string.Empty;

        public string? FGItemName { get; set; } = string.Empty;

        public string? StepIndexHydra { get; set; } = string.Empty;

        /// <summary>
        /// Trang thái cân của step.
        /// Fasle-chưa cân; true-đã cân.
        /// </summary>
        [NotMapped]
        public bool Status { get; set; } = false;

        /// <summary>
        /// Order Hydra number.
        /// </summary>
        public string? OrderHydraNum { get; set; } = string.Empty;
        public string? ColorCode { get; set; } = string.Empty;
        public string? ColorName { get; set; } = string.Empty;
        public string? MoldId { get; set; } = string.Empty;

        public string? OrderStatus { get; set; } = string.Empty;

        public int? MesoYear { get; set; }
        public string? MesoComp { get; set; }
    }
}
