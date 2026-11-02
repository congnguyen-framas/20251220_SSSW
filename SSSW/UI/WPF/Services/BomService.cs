// ============================================================================
//  BomService.cs
//  Truy vấn BOM Level 1 (theo FG) / Level 2 (theo step) qua sp_getBomWinlineOfItemFG,
//  cache Level 2 trong phiên để Save không phải gọi DB.
//  Namespace : SSSW.UI.WPF.Services
// ============================================================================
using Microsoft.EntityFrameworkCore;
using SSSW.models;
using SSSW.modelss;
using System.Collections.Concurrent;

namespace SSSW.UI.WPF.Services
{
    public interface IBomService
    {
        /// <summary>sp_getBomWinlineOfItemFG @itemFG = mã FG → các step (Level 1).</summary>
        Task<List<BomWinlineModel>> GetLevel1Async(string itemFgCode);

        /// <summary>sp_getBomWinlineOfItemFG @itemFG = mã step → các chi tiết cấu thành (Level 2). Có cache.</summary>
        Task<IReadOnlyList<BomWinlineModel>> GetLevel2Async(string stepCode);

        void ClearCache();
    }

    public class BomService : IBomService
    {
        private readonly IDbContextFactory<DbContextDogeWH> _dbFactory;
        private readonly ConcurrentDictionary<string, IReadOnlyList<BomWinlineModel>> _level2 =
            new(StringComparer.OrdinalIgnoreCase);

        public BomService(IDbContextFactory<DbContextDogeWH> dbFactory) => _dbFactory = dbFactory;

        public async Task<List<BomWinlineModel>> GetLevel1Async(string itemFgCode)
        {
            using var db = _dbFactory.CreateDbContext();
            return await db.Database
                .SqlQueryRaw<BomWinlineModel>("sp_getBomWinlineOfItemFG @itemFG = {0}", itemFgCode)
                .AsNoTracking().ToListAsync();
        }

        public async Task<IReadOnlyList<BomWinlineModel>> GetLevel2Async(string stepCode)
        {
            var key = stepCode.Trim();
            if (_level2.TryGetValue(key, out var cached)) return cached;

            var rows = await GetLevel1Async(key); // cùng SP, truyền mã step
            _level2[key] = rows;
            return rows;
        }

        public void ClearCache() => _level2.Clear();
    }
}
