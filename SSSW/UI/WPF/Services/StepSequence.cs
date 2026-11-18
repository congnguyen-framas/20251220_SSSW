// ============================================================================
//  StepSequence.cs
//  Xác định step được chọn có phải step cuối của FG hay không — luôn theo thứ tự trong BOM Level 1
//  (ParallelSequence), KHÔNG dùng FT601.C010 (số thứ tự từ Hydra có thể lệch với BOM).
//  Hàm thuần để test được. Namespace : SSSW.UI.WPF.Services
// ============================================================================
using SSSW.models;

namespace SSSW.UI.WPF.Services
{
    public static class StepSequence
    {
        /// <summary>
        /// Từ các step chain ứng viên (cùng thứ tự), lấy mỗi component 1 dòng: nhóm theo mã chính (bỏ màu &amp; size),
        /// trong nhóm ưu tiên dòng cùng size với step đang tính. Thứ tự các nhóm giữ như danh sách đầu vào.
        /// </summary>
        public static List<FT600> PickPerComponent(IEnumerable<FT600> candidates, string? rowSize)
            => candidates
                .GroupBy(x => PartWeightCalculator.StepMainCode(x.C002), StringComparer.OrdinalIgnoreCase)
                .Select(g => g.FirstOrDefault(x => x.C008 == rowSize) ?? g.First())
                .ToList();

        /// <summary>ParallelSequence lớn nhất của các step (bỏ REX / mã rỗng); null nếu không có.</summary>
        public static int? LastSequence(IEnumerable<BomWinlineModel> steps)
            => steps.Where(x => !string.IsNullOrWhiteSpace(x.ItemStepCode) &&
                                !x.ItemStepCode!.StartsWith("REX", StringComparison.OrdinalIgnoreCase))
                    .Max(x => x.ParallelSequence);

        /// <summary>
        /// true nếu step <paramref name="stepCode"/> không phải step cuối. Thứ tự lấy từ BOM theo đúng mã step;
        /// chỉ khi mã không có trong BOM mới dùng <paramref name="fallbackSeq"/> (vd. FT601.C010).
        /// </summary>
        public static bool IsNotLastStep(IReadOnlyCollection<BomWinlineModel> steps, string? stepCode, int? fallbackSeq)
        {
            var last = LastSequence(steps);
            if (last == null) return false;

            var code = stepCode?.Trim();
            var seq = steps.FirstOrDefault(x => !string.IsNullOrEmpty(code) &&
                string.Equals(x.ItemStepCode?.Trim(), code, StringComparison.OrdinalIgnoreCase))?.ParallelSequence
                ?? fallbackSeq;

            return seq != null && seq < last;
        }
    }
}
