using Microsoft.EntityFrameworkCore;
using TaskAssistant.Models;

namespace TaskAssistant.Data.Repositories
{
    /// <summary>
    /// ?本???
    /// 提供?本相?的?据?操作
    /// </summary>
    public class ScriptRepository : Repository<ScriptInfo>, IScriptRepository
    {
        #region 构造函?

        /// <summary>
        /// 初始化?本??
        /// </summary>
        /// <param name="context">?据?上下文</param>
        public ScriptRepository(AppDbContext context) : base(context)
        {
        }

        #endregion

        #region ?本特殊查?方法

        /// <summary>
        /// 根据名??取?本
        /// </summary>
        /// <param name="name">?本名?</param>
        /// <returns>?本信息，如果不存在?返回null</returns>
        public async Task<ScriptInfo?> GetByNameAsync(string name)
        {
            return await _dbSet.FirstOrDefaultAsync(s => s.Name == name);
        }

        /// <summary>
        /// ?查?本名?是否已存在
        /// </summary>
        /// <param name="name">?本名?</param>
        /// <param name="excludeId">排除的?本ID（用于更新??查）</param>
        /// <returns>如果名?已存在?返回true，否?返回false</returns>
        public async Task<bool> IsNameExistsAsync(string name, int? excludeId = null)
        {
            var query = _dbSet.Where(s => s.Name == name);
            
            if (excludeId.HasValue)
            {
                query = query.Where(s => s.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        /// <summary>
        /// 根据分??取?本（?量?，不包含Code字段）
        /// </summary>
        /// <param name="category">?本分?</param>
        /// <returns>指定分?的?本集合</returns>
        public async Task<IEnumerable<ScriptInfo>> GetByCategoryAsync(string category)
        {
            return await _dbSet
                .Where(s => s.Category == category && s.IsEnabled)
                .Select(s => new ScriptInfo
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description,
                    Version = s.Version,
                    Author = s.Author,
                    Category = s.Category,
                    Tags = s.Tags,
                    IsEnabled = s.IsEnabled,
                    ExecutionCount = s.ExecutionCount,
                    LastExecuted = s.LastExecuted,
                    CreatedAt = s.CreatedAt,
                    LastModified = s.LastModified
                    // 注意：不包含Code字段，?少?据??
                })
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        /// <summary>
        /// 根据分??取?本（完整版本，包含Code字段）
        /// </summary>
        /// <param name="category">?本分?</param>
        /// <returns>指定分?的完整?本集合</returns>
        public async Task<IEnumerable<ScriptInfo>> GetByCategoryWithCodeAsync(string category)
        {
            return await _dbSet
                .Where(s => s.Category == category && s.IsEnabled)
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        /// <summary>
        /// ?取所有分?
        /// </summary>
        /// <returns>所有分?名?的集合</returns>
        public async Task<IEnumerable<string>> GetAllCategoriesAsync()
        {
            return await _dbSet
                .Where(s => s.IsEnabled)
                .Select(s => s.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }

        /// <summary>
        /// 搜索?本（?量?，不包含Code字段）
        /// </summary>
        /// <param name="keyword">搜索??字</param>
        /// <param name="category">分???（可?）</param>
        /// <param name="isEnabled">是否?用??（可?）</param>
        /// <returns>匹配的?本集合</returns>
        public async Task<IEnumerable<ScriptInfo>> SearchAsync(string keyword, string? category = null, bool? isEnabled = null)
        {
            var query = _dbSet.AsQueryable();

            // ??字搜索
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.ToLower();
                query = query.Where(s => 
                    s.Name.ToLower().Contains(keyword) ||
                    s.Description.ToLower().Contains(keyword) ||
                    s.Author.ToLower().Contains(keyword));
            }

            // 分???
            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(s => s.Category == category);
            }

            // ?用????
            if (isEnabled.HasValue)
            {
                query = query.Where(s => s.IsEnabled == isEnabled.Value);
            }

            return await query
                .Select(s => new ScriptInfo
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description,
                    Version = s.Version,
                    Author = s.Author,
                    Category = s.Category,
                    Tags = s.Tags,
                    IsEnabled = s.IsEnabled,
                    ExecutionCount = s.ExecutionCount,
                    LastExecuted = s.LastExecuted,
                    CreatedAt = s.CreatedAt,
                    LastModified = s.LastModified
                    // 注意：不包含Code字段，?少?据??
                })
                .OrderByDescending(s => s.LastModified)
                .ToListAsync();
        }

        /// <summary>
        /// 搜索?本（完整版本，包含Code字段）
        /// </summary>
        /// <param name="keyword">搜索??字</param>
        /// <param name="category">分???（可?）</param>
        /// <param name="isEnabled">是否?用??（可?）</param>
        /// <returns>匹配的完整?本集合</returns>
        public async Task<IEnumerable<ScriptInfo>> SearchWithCodeAsync(string keyword, string? category = null, bool? isEnabled = null)
        {
            var query = _dbSet.AsQueryable();

            // ??字搜索
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.ToLower();
                query = query.Where(s => 
                    s.Name.ToLower().Contains(keyword) ||
                    s.Description.ToLower().Contains(keyword) ||
                    s.Author.ToLower().Contains(keyword));
            }

            // 分???
            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(s => s.Category == category);
            }

            // ?用????
            if (isEnabled.HasValue)
            {
                query = query.Where(s => s.IsEnabled == isEnabled.Value);
            }

            return await query
                .OrderByDescending(s => s.LastModified)
                .ToListAsync();
        }

        /// <summary>
        /// ?取最近使用的?本（?量?，不包含Code字段）
        /// </summary>
        /// <param name="count">?取?量</param>
        /// <returns>最近使用的?本集合</returns>
        public async Task<IEnumerable<ScriptInfo>> GetRecentlyUsedAsync(int count = 10)
        {
            return await _dbSet
                .Where(s => s.IsEnabled && s.LastExecuted.HasValue)
                .Select(s => new ScriptInfo
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description,
                    Version = s.Version,
                    Author = s.Author,
                    Category = s.Category,
                    Tags = s.Tags,
                    IsEnabled = s.IsEnabled,
                    ExecutionCount = s.ExecutionCount,
                    LastExecuted = s.LastExecuted,
                    CreatedAt = s.CreatedAt,
                    LastModified = s.LastModified
                    // 注意：不包含Code字段，?少?据??
                })
                .OrderByDescending(s => s.LastExecuted)
                .Take(count)
                .ToListAsync();
        }

        /// <summary>
        /// ?取最常用的?本（?量?，不包含Code字段）
        /// </summary>
        /// <param name="count">?取?量</param>
        /// <returns>最常用的?本集合</returns>
        public async Task<IEnumerable<ScriptInfo>> GetMostUsedAsync(int count = 10)
        {
            return await _dbSet
                .Where(s => s.IsEnabled && s.ExecutionCount > 0)
                .Select(s => new ScriptInfo
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description,
                    Version = s.Version,
                    Author = s.Author,
                    Category = s.Category,
                    Tags = s.Tags,
                    IsEnabled = s.IsEnabled,
                    ExecutionCount = s.ExecutionCount,
                    LastExecuted = s.LastExecuted,
                    CreatedAt = s.CreatedAt,
                    LastModified = s.LastModified
                    // 注意：不包含Code字段，?少?据??
                })
                .OrderByDescending(s => s.ExecutionCount)
                .Take(count)
                .ToListAsync();
        }

        /// <summary>
        /// ?取?本列表（?量?，不包含Code字段）
        /// </summary>
        /// <param name="category">分???（可?）</param>
        /// <param name="isEnabled">是否?用??（可?）</param>
        /// <param name="pageIndex">?面索引（?0?始）</param>
        /// <param name="pageSize">?面大小</param>
        /// <returns>?本列表</returns>
        public async Task<IEnumerable<ScriptInfo>> GetScriptListAsync(string? category = null, bool? isEnabled = null, int? pageIndex = null, int? pageSize = null)
        {
            var query = _dbSet.AsQueryable();

            // 分???
            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(s => s.Category == category);
            }

            // ?用????
            if (isEnabled.HasValue)
            {
                query = query.Where(s => s.IsEnabled == isEnabled.Value);
            }

            var selectQuery = query.Select(s => new ScriptInfo
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                Version = s.Version,
                Author = s.Author,
                Category = s.Category,
                Tags = s.Tags,
                IsEnabled = s.IsEnabled,
                ExecutionCount = s.ExecutionCount,
                LastExecuted = s.LastExecuted,
                CreatedAt = s.CreatedAt,
                LastModified = s.LastModified
                // 注意：不包含Code字段，?少?据??
            })
            .OrderByDescending(s => s.LastModified);

            // 分??理
            if (pageIndex.HasValue && pageSize.HasValue)
            {
                return await selectQuery
                    .Skip(pageIndex.Value * pageSize.Value)
                    .Take(pageSize.Value)
                    .ToListAsync();
            }

            return await selectQuery.ToListAsync();
        }

        #endregion

        #region ?本??更新方法

        /// <summary>
        /// 更新?本?行??
        /// </summary>
        /// <param name="scriptId">?本ID</param>
        /// <returns>更新后的?本信息</returns>
        public async Task<ScriptInfo?> UpdateExecutionStatsAsync(int scriptId)
        {
            var script = await GetByIdAsync(scriptId);
            if (script != null)
            {
                script.ExecutionCount++;
                script.LastExecuted = DateTime.Now;
                script.LastModified = DateTime.Now;

                _dbSet.Update(script);
                await SaveChangesAsync();
            }

            return script;
        }

        /// <summary>
        /// 批量?用/禁用?本
        /// </summary>
        /// <param name="scriptIds">?本ID集合</param>
        /// <param name="isEnabled">是否?用</param>
        /// <returns>受影?的行?</returns>
        public async Task<int> BulkUpdateEnabledStatusAsync(IEnumerable<int> scriptIds, bool isEnabled)
        {
            var scripts = await _dbSet
                .Where(s => scriptIds.Contains(s.Id))
                .ToListAsync();

            foreach (var script in scripts)
            {
                script.IsEnabled = isEnabled;
                script.LastModified = DateTime.Now;
            }

            return await SaveChangesAsync();
        }

        /// <summary>
        /// 清理未使用的?本
        /// </summary>
        /// <param name="daysUnused">未使用天?</param>
        /// <returns>被清理的?本?量</returns>
        public async Task<int> CleanupUnusedScriptsAsync(int daysUnused = 90)
        {
            var cutoffDate = DateTime.Now.AddDays(-daysUnused);
            
            var unusedScripts = await _dbSet
                .Where(s => !s.LastExecuted.HasValue || s.LastExecuted < cutoffDate)
                .Where(s => s.ExecutionCount == 0)
                .ToListAsync();

            if (unusedScripts.Any())
            {
                _dbSet.RemoveRange(unusedScripts);
                await SaveChangesAsync();
            }

            return unusedScripts.Count;
        }

        #endregion
    }
}