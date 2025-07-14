using TaskAssistant.Models;

namespace TaskAssistant.Data.Repositories
{
    /// <summary>
    /// 脚本仓库接口
    /// 定义脚本相关的特殊操作
    /// </summary>
    public interface IScriptRepository : IRepository<ScriptInfo>
    {
        #region 脚本特殊查询

        /// <summary>
        /// 根据名称获取脚本
        /// </summary>
        /// <param name="name">脚本名称</param>
        /// <returns>脚本信息，如果不存在返回null</returns>
        Task<ScriptInfo?> GetByNameAsync(string name);

        /// <summary>
        /// 查询脚本名称是否已存在
        /// </summary>
        /// <param name="name">脚本名称</param>
        /// <param name="excludeId">排除的脚本ID（用于更新时查询）</param>
        /// <returns>如果名称已存在返回true，否则返回false</returns>
        Task<bool> IsNameExistsAsync(string name, int? excludeId = null);

        /// <summary>
        /// 根据分类获取脚本（简化版，不包含Code字段）
        /// </summary>
        /// <param name="category">脚本分类</param>
        /// <returns>指定分类的脚本集合</returns>
        Task<IEnumerable<ScriptInfo>> GetByCategoryAsync(string category);

        /// <summary>
        /// 根据分类获取脚本（完整版本，包含Code字段）
        /// </summary>
        /// <param name="category">脚本分类</param>
        /// <returns>指定分类的完整脚本集合</returns>
        Task<IEnumerable<ScriptInfo>> GetByCategoryWithCodeAsync(string category);

        /// <summary>
        /// 获取所有分类
        /// </summary>
        /// <returns>所有分?名?的集合</returns>
        Task<IEnumerable<string>> GetAllCategoriesAsync();

        /// <summary>
        /// 搜索?本（?量?，不包含Code字段）
        /// </summary>
        /// <param name="keyword">搜索??字</param>
        /// <param name="category">分???（可?）</param>
        /// <param name="isEnabled">是否?用??（可?）</param>
        /// <returns>匹配的?本集合</returns>
        Task<IEnumerable<ScriptInfo>> SearchAsync(string keyword, string? category = null, bool? isEnabled = null);

        /// <summary>
        /// 搜索?本（完整版本，包含Code字段）
        /// </summary>
        /// <param name="keyword">搜索??字</param>
        /// <param name="category">分???（可?）</param>
        /// <param name="isEnabled">是否?用??（可?）</param>
        /// <returns>匹配的完整?本集合</returns>
        Task<IEnumerable<ScriptInfo>> SearchWithCodeAsync(string keyword, string? category = null, bool? isEnabled = null);

        /// <summary>
        /// ?取最近使用的?本（?量?，不包含Code字段）
        /// </summary>
        /// <param name="count">?取?量</param>
        /// <returns>最近使用的?本集合</returns>
        Task<IEnumerable<ScriptInfo>> GetRecentlyUsedAsync(int count = 10);

        /// <summary>
        /// ?取最常用的?本（?量?，不包含Code字段）
        /// </summary>
        /// <param name="count">?取?量</param>
        /// <returns>最常用的?本集合</returns>
        Task<IEnumerable<ScriptInfo>> GetMostUsedAsync(int count = 10);

        /// <summary>
        /// ?取?本列表（?量?，不包含Code字段）
        /// </summary>
        /// <param name="category">分???（可?）</param>
        /// <param name="isEnabled">是否?用??（可?）</param>
        /// <param name="pageIndex">?面索引（?0?始）</param>
        /// <param name="pageSize">?面大小</param>
        /// <returns>?本列表</returns>
        Task<IEnumerable<ScriptInfo>> GetScriptListAsync(string? category = null, bool? isEnabled = null, int? pageIndex = null, int? pageSize = null);

        #endregion

        #region ?本??更新

        /// <summary>
        /// 更新?本?行??
        /// </summary>
        /// <param name="scriptId">?本ID</param>
        /// <returns>更新后的?本信息</returns>
        Task<ScriptInfo?> UpdateExecutionStatsAsync(int scriptId);

        /// <summary>
        /// 批量?用/禁用?本
        /// </summary>
        /// <param name="scriptIds">?本ID集合</param>
        /// <param name="isEnabled">是否?用</param>
        /// <returns>受影?的行?</returns>
        Task<int> BulkUpdateEnabledStatusAsync(IEnumerable<int> scriptIds, bool isEnabled);

        /// <summary>
        /// 清理未使用的?本
        /// </summary>
        /// <param name="daysUnused">未使用天?</param>
        /// <returns>被清理的?本?量</returns>
        Task<int> CleanupUnusedScriptsAsync(int daysUnused = 90);

        #endregion
    }
}