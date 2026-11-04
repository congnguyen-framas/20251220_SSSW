// ============================================================================
//  ComponentQuantityAggregator.cs
//  Gộp số chiếc (PCS) của từng chi tiết trong 1 đôi theo BOM Level 2:
//      Quantity(c) = Σ qty(step cha -> c)   (bỏ dòng self)
//  Hàm thuần — không DB, không UI — để test được. Kết quả lưu ở FT600.C039.
//  Namespace : SSSW.UI.WPF.Services
// ============================================================================
using SSSW.models;

namespace SSSW.UI.WPF.Services
{
    public static class ComponentQuantityAggregator
    {
        /// <param name="level2ByStep">key = mã step cha, value = BOM Level 2 của step đó.</param>
        /// <returns>mã chi tiết -> tổng số chiếc trong 1 đôi.</returns>
        public static Dictionary<string, double> Aggregate(
            IReadOnlyDictionary<string, IReadOnlyList<BomWinlineModel>> level2ByStep)
        {
            var total = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

            foreach (var (parent, rows) in level2ByStep)
            {
                foreach (var row in rows)
                {
                    if (PartWeightCalculator.IsSelfRow(row, parent)) continue;

                    var qty = row.Quantity ?? 0;
                    if (qty <= 0) continue;

                    var code = row.ItemStepCode!.Trim();
                    total[code] = total.GetValueOrDefault(code) + qty;
                }
            }

            return total;
        }

        /// <summary>
        /// Số chiếc trong 1 đôi của chi tiết <paramref name="code"/>: tổng đã gộp nếu là chi tiết của step khác;
        /// nếu không (vd. step cuối) thì lấy Quantity Level 1 quy ra chiếc (PRS = 2 chiếc, PCS giữ nguyên).
        /// </summary>
        public static double PiecesFor(string? code, IReadOnlyDictionary<string, double> aggregated,
            double? level1Quantity, string? level1Unit)
        {
            if (!string.IsNullOrWhiteSpace(code) && aggregated.TryGetValue(code.Trim(), out var total) && total > 0)
                return total;

            var qty = level1Quantity is > 0 ? level1Quantity.Value : 1;
            return string.Equals(level1Unit?.Trim(), "PRS", StringComparison.OrdinalIgnoreCase)
                ? qty * PartWeightCalculator.PiecesPerPair
                : qty;
        }
    }
}
