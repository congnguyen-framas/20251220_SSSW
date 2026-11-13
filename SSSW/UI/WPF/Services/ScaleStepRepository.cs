// ============================================================================
//  ScaleStepRepository.cs
//  Truy cập FT600 cho màn cân Step.
//  Namespace : SSSW.UI.WPF.Services
// ============================================================================
using Microsoft.EntityFrameworkCore;
using SSSW.models;
using SSSW.modelss;

namespace SSSW.UI.WPF.Services
{
    public interface IScaleStepRepository
    {
        /// <summary>Bản ghi FT600 mới nhất của từng mã step (key = C002 đã Trim).</summary>
        Task<Dictionary<string, FT600>> GetLatestByStepCodesAsync(IEnumerable<string> stepCodes);
    }

    public class ScaleStepRepository : IScaleStepRepository
    {
        private readonly IDbContextFactory<DbContextDogeWH> _dbFactory;

        public ScaleStepRepository(IDbContextFactory<DbContextDogeWH> dbFactory) => _dbFactory = dbFactory;

        public async Task<Dictionary<string, FT600>> GetLatestByStepCodesAsync(IEnumerable<string> stepCodes)
        {
            // REX: khớp nguyên mã. Còn lại: khớp theo mã chính (bỏ màu & size) → C002 LIKE 'N-VB042313-%'.
            var mains = stepCodes.Select(PartWeightCalculator.StepMainCode).Where(x => x.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (mains.Count == 0) return new();

            var exact = mains.Where(m => m.StartsWith("REX", StringComparison.OrdinalIgnoreCase) || !m.Contains('-')).ToList();
            var prefixes = mains.Except(exact).Select(m => m + "-").ToList();

            using var db = _dbFactory.CreateDbContext();
            var query = db.FT600s.AsNoTracking().Where(x => x.C002 != null &&
                (exact.Contains(x.C002) || prefixes.Any(p => x.C002.StartsWith(p))));
            var rows = await query.OrderByDescending(x => x.CreatedDate).ToListAsync();

            // Key = mã chính; giữ danh sách theo size ở bước chọn (FindWeighedChild) → ở đây lấy bản mới nhất mỗi mã đầy đủ
            return rows.GroupBy(x => x.C002!.Trim(), StringComparer.OrdinalIgnoreCase)
                       .Select(g => g.First())
                       .ToDictionary(x => x.C002!.Trim(), x => x, StringComparer.OrdinalIgnoreCase);
        }
    }
}
