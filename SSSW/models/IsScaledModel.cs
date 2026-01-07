namespace SSSW.models
{
    public class IsScaledModel
    {
        public EnumScaleType ScaleType { get; set; }
        public string ItemStepCode { get; set; }
        public string ItemStepName { get; set; }
        public double? PartWeight { get; set; } = 0;
        public double? RunnerWeight { get; set; } = 0;
        public int? StepIndex { get; set; }
        /// <summary>
        /// Chính là tổng khối lượng của bước hiện tại và các bước trước có liên quan.
        /// </summary>
        public double? ScaleValue { get; set; }
        /// <summary>
        /// Dùng đẻ nhận biết là line cân của receoptacle và Outsoleboard, không lấy khối lượng của bước trước.
        /// </summary>
        public bool IsReceptacle { get; set; } = false;
    }
}
