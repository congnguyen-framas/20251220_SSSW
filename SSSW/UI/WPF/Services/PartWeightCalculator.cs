// ============================================================================
//  PartWeightCalculator.cs
//  Tính Part Weight (C021) của 1 step ép theo BOM Level 2:
//      C021 = C023 − Σ ( Qty(child) × UnitWeight(child) )   (bỏ dòng "self")
//  Hàm thuần — không DB, không UI — để test được.
//  Namespace : SSSW.UI.WPF.Services
// ============================================================================
using SSSW.models;

namespace SSSW.UI.WPF.Services
{
    public class PartWeightResult
    {
        /// <summary>C021 sau khi trừ các chi tiết con; null nếu còn chi tiết con chưa cân.</summary>
        public double? PartWeight { get; init; }

        /// <summary>Mã các chi tiết con chưa có trọng lượng (chưa cân / không tìm thấy).</summary>
        public IReadOnlyList<string> MissingChildren { get; init; } = Array.Empty<string>();

        /// <summary>Tổng khối lượng đã trừ (Σ Qty × UnitWeight).</summary>
        public double TotalDeduction { get; init; }

        public bool IsComplete => PartWeight.HasValue;
    }

    public interface IPartWeightCalculator
    {
        /// <param name="totalPartWeight">C023 của step đang tính.</param>
        /// <param name="stepCode">ItemStepCode của step (để loại dòng self).</param>
        /// <param name="level2">Kết quả sp_getBomWinlineOfItemFG @itemFG = stepCode.</param>
        /// <param name="findWeighed">Tìm FT600 đã cân của chi tiết con theo ItemStepCode (null nếu chưa có).</param>
        PartWeightResult Calculate(double totalPartWeight, string stepCode,
            IReadOnlyList<BomWinlineModel> level2, Func<string, FT600?> findWeighed);
    }

    public class PartWeightCalculator : IPartWeightCalculator
    {
        /// <summary>Quantity ở Level 2 tính theo chiếc (PCS); 1 đôi (PRS) = 2 chiếc.</summary>
        public const double PiecesPerPair = 2;

        public PartWeightResult Calculate(double totalPartWeight, string stepCode,
            IReadOnlyList<BomWinlineModel> level2, Func<string, FT600?> findWeighed)
        {
            double deduction = 0;
            var missing = new List<string>();

            foreach (var child in level2)
            {
                if (IsSelfRow(child, stepCode)) continue;

                var code = child.ItemStepCode!.Trim();
                var qty = child.Quantity ?? 0;
                if (qty <= 0) continue;

                var weighed = findWeighed(code);
                var unitWeight = weighed == null ? null : UnitWeightPerPiece(weighed);
                if (unitWeight == null)
                {
                    missing.Add(code);
                    continue;
                }

                deduction += qty * unitWeight.Value;
            }

            return new PartWeightResult
            {
                PartWeight = missing.Count == 0 ? totalPartWeight - deduction : null,
                MissingChildren = missing,
                TotalDeduction = deduction
            };
        }

        /// <summary>Dòng self: chính phần nhựa của step (mã kết thúc "-0000", đơn vị G).</summary>
        public static bool IsSelfRow(BomWinlineModel row, string stepCode)
        {
            var code = row.ItemStepCode?.Trim();
            if (string.IsNullOrEmpty(code)) return true;
            return string.Equals(code, stepCode?.Trim(), StringComparison.OrdinalIgnoreCase)
                || code.EndsWith("-0000", StringComparison.Ordinal)
                || string.Equals(row.Unit?.Trim(), "G", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Trọng lượng 1 chiếc (g/pcs) của chi tiết đã cân; null nếu chưa có giá trị.
        /// Ưu tiên <c>C036</c> đã lưu (REX: scale / số chiếc; chi tiết khác: C021 / C039); nếu chưa có thì
        /// (không phải REX) tính <c>C021 / C039</c>. Không còn giả định "1 đôi = 2 chiếc".
        /// </summary>
        public static double? UnitWeightPerPiece(FT600 weighed)
        {
            if (weighed.C036 is > 0) return weighed.C036;

            // Hàng PU: Part Weight đã là g/chiếc (N = số chiếc/shot) → không chia thêm C039
            if (weighed.IsPu && weighed.C021 is > 0) return weighed.C021;

            bool isRex = weighed.C002?.Trim().StartsWith("REX", StringComparison.OrdinalIgnoreCase) ?? false;
            if (!isRex && weighed.C021 is > 0 && weighed.C039 is > 0)
                return weighed.C021 / weighed.C039;

            return null;
        }
    }
}
