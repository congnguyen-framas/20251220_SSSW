namespace SSSW.models
{
    /// <summary>Một dòng kết quả của <c>sp_sssw_getproductInfo @itemCode = ...</c> (FG / main / size / step của sản phẩm).</summary>
    public class ProductInfoStoreModel
    {
        public string? ProductItemCode { get; set; }
        public string? ProductItemName { get; set; }
        public string? ProductName { get; set; }
        public string? MainCode { get; set; }
        public string? MainName { get; set; }
        public string? SizeCode { get; set; }
        public string? SizeName { get; set; }
        public string? StepName { get; set; }
        public string? StepFG { get; set; }
        public string? StepFGBom { get; set; }
        public int? LineNum { get; set; }
        public int? Sequence { get; set; }
        public double? ShotWeight1PrsGam { get; set; }
        public int? QuantityPrs { get; set; }
        public double? WeightQuantityPrsGam { get; set; }
    }
}
