namespace SSSW.models
{
    public class StepSelectModel
    {
        public string StepItemCode { get; set; }
        public string StepItemName { get; set; }

        public string? Size { get; set; } = string.Empty;
        public string? Machine { get; set; } = string.Empty;

        public string? HydraOrderNo { get; set; } = string.Empty;
    }
}
