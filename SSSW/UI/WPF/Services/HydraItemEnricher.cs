// ============================================================================
//  HydraItemEnricher.cs
//  Chuẩn bị dữ liệu Hydra (sp_GetFullStepItemHydraIsRun) trước khi insert FT601 — cùng logic với
//  OMS WorkerShotWeight.GetDataHydra: bổ sung thông tin thiếu từ sp_sssw_getproductInfo, rồi loại các dòng
//  không đủ thông tin (không có FG hợp lệ, thiếu Main code / Size). Hàm thuần — không DB — để test được.
//  Namespace : SSSW.UI.WPF.Services
// ============================================================================
using SSSW.models;

namespace SSSW.UI.WPF.Services
{
    public static class HydraItemEnricher
    {
        /// <summary>Dòng còn thiếu thông tin (FG, tên, step index, main code/name, size) → cần gọi sp_sssw_getproductInfo.</summary>
        public static bool NeedsInfo(HydraItemDetailModel x)
            => string.IsNullOrEmpty(x.FGItemCode) || string.IsNullOrEmpty(x.FGItemName)
               || string.IsNullOrEmpty(x.StepItemName) || x.StepIndex == null
               || string.IsNullOrEmpty(x.MainCode) || string.IsNullOrEmpty(x.MainName) || string.IsNullOrEmpty(x.Size);

        /// <summary>
        /// Mã dùng để tra sp_sssw_getproductInfo: FG nếu đã có, ngược lại mã step
        /// (lấy sản phẩm đầu tiên dùng step đó).
        /// </summary>
        public static List<string> LookupCodes(IEnumerable<HydraItemDetailModel> items)
            => items.Select(x => !string.IsNullOrEmpty(x.FGItemCode) ? x.FGItemCode : x.StepItemCode)
                    .Where(c => !string.IsNullOrEmpty(c)).Select(c => c!)
                    .Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        /// <summary>Chỉ điền vào ô đang trống, không ghi đè dữ liệu Hydra có sẵn.</summary>
        public static void FillMissingInfo(IEnumerable<HydraItemDetailModel> items, IReadOnlyList<ProductInfoStoreModel> rows)
        {
            var cmp = StringComparer.OrdinalIgnoreCase;
            // Thông tin chung theo sản phẩm
            var byProduct = rows.Where(r => !string.IsNullOrEmpty(r.ProductItemCode))
                .GroupBy(r => r.ProductItemCode!, cmp).ToDictionary(g => g.Key, g => g.First(), cmp);
            // Thông tin step theo (sản phẩm, step) vì Sequence phụ thuộc từng sản phẩm
            var byProductStep = rows.Where(r => !string.IsNullOrEmpty(r.ProductItemCode) && !string.IsNullOrEmpty(r.StepFG))
                .GroupBy(r => $"{r.ProductItemCode}|{r.StepFG}", cmp).ToDictionary(g => g.Key, g => g.First(), cmp);
            // Tra theo step khi chưa biết sản phẩm: lấy sản phẩm đầu tiên theo mã để kết quả ổn định
            var byStep = rows.Where(r => !string.IsNullOrEmpty(r.StepFG))
                .GroupBy(r => r.StepFG!, cmp)
                .ToDictionary(g => g.Key, g => g.OrderBy(r => r.ProductItemCode, StringComparer.Ordinal).First(), cmp);

            foreach (var item in items)
            {
                ProductInfoStoreModel? main = null, step = null;
                if (!string.IsNullOrEmpty(item.FGItemCode))
                {
                    byProduct.TryGetValue(item.FGItemCode, out main);
                    if (!string.IsNullOrEmpty(item.StepItemCode))
                        byProductStep.TryGetValue($"{item.FGItemCode}|{item.StepItemCode}", out step);
                }
                else if (!string.IsNullOrEmpty(item.StepItemCode) && byStep.TryGetValue(item.StepItemCode, out var found))
                {
                    main = found;
                    step = found;
                    item.FGItemCode = found.ProductItemCode;
                }

                if (main != null)
                {
                    if (string.IsNullOrEmpty(item.FGItemName)) item.FGItemName = main.ProductName;
                    if (string.IsNullOrEmpty(item.MainCode)) item.MainCode = main.MainCode;
                    if (string.IsNullOrEmpty(item.MainName)) item.MainName = main.MainName;
                    if (string.IsNullOrEmpty(item.Size)) item.Size = main.SizeName;
                }
                if (step != null)
                {
                    if (string.IsNullOrEmpty(item.StepItemName)) item.StepItemName = step.StepName;
                    if (item.StepIndex == null) item.StepIndex = step.Sequence;
                }
            }
        }

        /// <summary>Chỉ giữ dòng có FG hợp lệ (chứa '-'). Chạy SAU FillMissingInfo vì dòng thiếu FG được bổ sung từ mã step.</summary>
        public static List<HydraItemDetailModel> KeepWithFg(IEnumerable<HydraItemDetailModel> items)
            => items.Where(x => !string.IsNullOrEmpty(x.FGItemCode) && x.FGItemCode.Contains('-')).ToList();

        /// <summary>FG thiếu Main code hoặc Size (để ghi log cảnh báo).</summary>
        public static List<string> FgMissingInfo(IEnumerable<HydraItemDetailModel> items)
            => items.Where(x => string.IsNullOrEmpty(x.MainCode) || string.IsNullOrEmpty(x.Size))
                    .Select(x => x.FGItemCode).Where(s => !string.IsNullOrEmpty(s)).Select(s => s!).Distinct().ToList();

        /// <summary>Loại các dòng vẫn thiếu Main code / Size sau khi bổ sung (không đủ thông tin để cân).</summary>
        public static List<HydraItemDetailModel> KeepComplete(IEnumerable<HydraItemDetailModel> items)
            => items.Where(x => !string.IsNullOrEmpty(x.MainCode) && !string.IsNullOrEmpty(x.Size)).ToList();
    }
}
