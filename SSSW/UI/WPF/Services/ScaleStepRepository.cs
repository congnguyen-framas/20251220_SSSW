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
            var codes = stepCodes.Select(x => x.Trim()).Distinct().ToList();
            if (codes.Count == 0) return new();

            using var db = _dbFactory.CreateDbContext();
            var rows = await db.FT600s.AsNoTracking()
                .Where(x => x.C002 != null && codes.Contains(x.C002))
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();

            return rows.GroupBy(x => x.C002!.Trim())
                       .ToDictionary(g => g.Key, g => g.First());
        }
    }
}
