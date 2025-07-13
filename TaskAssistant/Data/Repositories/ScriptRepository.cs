using Microsoft.EntityFrameworkCore;
using TaskAssistant.Models;

namespace TaskAssistant.Data.Repositories
{
    /// <summary>
    /// ?��???
    /// ����?����?��?�u?�ާ@
    /// </summary>
    public class ScriptRepository : Repository<ScriptInfo>, IScriptRepository
    {
        #region �۳y��?

        /// <summary>
        /// ��l��?��??
        /// </summary>
        /// <param name="context">?�u?�W�U��</param>
        public ScriptRepository(AppDbContext context) : base(context)
        {
        }

        #endregion

        #region ?���S��d?��k

        /// <summary>
        /// ���u�W??��?��
        /// </summary>
        /// <param name="name">?���W?</param>
        /// <returns>?���H���A�p�G���s�b?��^null</returns>
        public async Task<ScriptInfo?> GetByNameAsync(string name)
        {
            return await _dbSet.FirstOrDefaultAsync(s => s.Name == name);
        }

        /// <summary>
        /// ?�d?���W?�O�_�w�s�b
        /// </summary>
        /// <param name="name">?���W?</param>
        /// <param name="excludeId">�ư���?��ID�]�Τ_��s??�d�^</param>
        /// <returns>�p�G�W?�w�s�b?��^true�A�_?��^false</returns>
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
        /// ���u��??��?���]?�q?�A���]�tCode�r�q�^
        /// </summary>
        /// <param name="category">?����?</param>
        /// <returns>���w��?��?�����X</returns>
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
                    // �`�N�G���]�tCode�r�q�A?��?�u??
                })
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        /// <summary>
        /// ���u��??��?���]���㪩���A�]�tCode�r�q�^
        /// </summary>
        /// <param name="category">?����?</param>
        /// <returns>���w��?������?�����X</returns>
        public async Task<IEnumerable<ScriptInfo>> GetByCategoryWithCodeAsync(string category)
        {
            return await _dbSet
                .Where(s => s.Category == category && s.IsEnabled)
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        /// <summary>
        /// ?���Ҧ���?
        /// </summary>
        /// <returns>�Ҧ���?�W?�����X</returns>
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
        /// �j��?���]?�q?�A���]�tCode�r�q�^
        /// </summary>
        /// <param name="keyword">�j��??�r</param>
        /// <param name="category">��???�]�i?�^</param>
        /// <param name="isEnabled">�O�_?��??�]�i?�^</param>
        /// <returns>�ǰt��?�����X</returns>
        public async Task<IEnumerable<ScriptInfo>> SearchAsync(string keyword, string? category = null, bool? isEnabled = null)
        {
            var query = _dbSet.AsQueryable();

            // ??�r�j��
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.ToLower();
                query = query.Where(s => 
                    s.Name.ToLower().Contains(keyword) ||
                    s.Description.ToLower().Contains(keyword) ||
                    s.Author.ToLower().Contains(keyword));
            }

            // ��???
            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(s => s.Category == category);
            }

            // ?��????
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
                    // �`�N�G���]�tCode�r�q�A?��?�u??
                })
                .OrderByDescending(s => s.LastModified)
                .ToListAsync();
        }

        /// <summary>
        /// �j��?���]���㪩���A�]�tCode�r�q�^
        /// </summary>
        /// <param name="keyword">�j��??�r</param>
        /// <param name="category">��???�]�i?�^</param>
        /// <param name="isEnabled">�O�_?��??�]�i?�^</param>
        /// <returns>�ǰt������?�����X</returns>
        public async Task<IEnumerable<ScriptInfo>> SearchWithCodeAsync(string keyword, string? category = null, bool? isEnabled = null)
        {
            var query = _dbSet.AsQueryable();

            // ??�r�j��
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.ToLower();
                query = query.Where(s => 
                    s.Name.ToLower().Contains(keyword) ||
                    s.Description.ToLower().Contains(keyword) ||
                    s.Author.ToLower().Contains(keyword));
            }

            // ��???
            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(s => s.Category == category);
            }

            // ?��????
            if (isEnabled.HasValue)
            {
                query = query.Where(s => s.IsEnabled == isEnabled.Value);
            }

            return await query
                .OrderByDescending(s => s.LastModified)
                .ToListAsync();
        }

        /// <summary>
        /// ?���̪�ϥΪ�?���]?�q?�A���]�tCode�r�q�^
        /// </summary>
        /// <param name="count">?��?�q</param>
        /// <returns>�̪�ϥΪ�?�����X</returns>
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
                    // �`�N�G���]�tCode�r�q�A?��?�u??
                })
                .OrderByDescending(s => s.LastExecuted)
                .Take(count)
                .ToListAsync();
        }

        /// <summary>
        /// ?���̱`�Ϊ�?���]?�q?�A���]�tCode�r�q�^
        /// </summary>
        /// <param name="count">?��?�q</param>
        /// <returns>�̱`�Ϊ�?�����X</returns>
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
                    // �`�N�G���]�tCode�r�q�A?��?�u??
                })
                .OrderByDescending(s => s.ExecutionCount)
                .Take(count)
                .ToListAsync();
        }

        /// <summary>
        /// ?��?���C��]?�q?�A���]�tCode�r�q�^
        /// </summary>
        /// <param name="category">��???�]�i?�^</param>
        /// <param name="isEnabled">�O�_?��??�]�i?�^</param>
        /// <param name="pageIndex">?�����ޡ]?0?�l�^</param>
        /// <param name="pageSize">?���j�p</param>
        /// <returns>?���C��</returns>
        public async Task<IEnumerable<ScriptInfo>> GetScriptListAsync(string? category = null, bool? isEnabled = null, int? pageIndex = null, int? pageSize = null)
        {
            var query = _dbSet.AsQueryable();

            // ��???
            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(s => s.Category == category);
            }

            // ?��????
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
                // �`�N�G���]�tCode�r�q�A?��?�u??
            })
            .OrderByDescending(s => s.LastModified);

            // ��??�z
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

        #region ?��??��s��k

        /// <summary>
        /// ��s?��?��??
        /// </summary>
        /// <param name="scriptId">?��ID</param>
        /// <returns>��s�Z��?���H��</returns>
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
        /// ��q?��/�T��?��
        /// </summary>
        /// <param name="scriptIds">?��ID���X</param>
        /// <param name="isEnabled">�O�_?��</param>
        /// <returns>���v?����?</returns>
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
        /// �M�z���ϥΪ�?��
        /// </summary>
        /// <param name="daysUnused">���ϥΤ�?</param>
        /// <returns>�Q�M�z��?��?�q</returns>
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