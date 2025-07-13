using TaskAssistant.Data.Repositories;
using TaskAssistant.Models;

namespace TaskAssistant.Data.Services
{
    /// <summary>
    /// 数据服务接口
    /// 提供统一的数据访问入口，封装仓储层的复杂性
    /// </summary>
    public interface IDataService
    {
        #region 脚本管理

        /// <summary>
        /// 脚本仓储
        /// </summary>
        IScriptRepository Scripts { get; }

        /// <summary>
        /// 保存脚本
        /// </summary>
        /// <param name="script">脚本信息</param>
        /// <returns>保存后的脚本信息</returns>
        Task<ScriptInfo> SaveScriptAsync(ScriptInfo script);

        /// <summary>
        /// 获取脚本列表（轻量级，不包含Code字段）
        /// </summary>
        /// <param name="category">分类过滤（可选）</param>
        /// <param name="isEnabled">启用状态过滤（可选）</param>
        /// <returns>脚本列表</returns>
        Task<IEnumerable<ScriptInfo>> GetScriptsAsync(string? category = null, bool? isEnabled = null);

        /// <summary>
        /// 获取脚本列表（完整版本，包含Code字段）
        /// </summary>
        /// <param name="category">分类过滤（可选）</param>
        /// <param name="isEnabled">启用状态过滤（可选）</param>
        /// <returns>完整脚本列表</returns>
        Task<IEnumerable<ScriptInfo>> GetScriptsWithCodeAsync(string? category = null, bool? isEnabled = null);

        /// <summary>
        /// 删除脚本
        /// </summary>
        /// <param name="scriptId">脚本ID</param>
        /// <returns>是否删除成功</returns>
        Task<bool> DeleteScriptAsync(int scriptId);

        /// <summary>
        /// 执行脚本（更新统计信息）
        /// </summary>
        /// <param name="scriptId">脚本ID</param>
        /// <returns>更新后的脚本信息</returns>
        Task<ScriptInfo?> ExecuteScriptAsync(int scriptId);

        #endregion

        #region 任务管理

        /// <summary>
        /// 任务仓储
        /// </summary>
        ITaskRepository Tasks { get; }

        /// <summary>
        /// 保存任务
        /// </summary>
        /// <param name="task">任务信息</param>
        /// <returns>保存后的任务信息</returns>
        Task<TaskInfo> SaveTaskAsync(TaskInfo task);

        /// <summary>
        /// 获取任务列表
        /// </summary>
        /// <param name="status">状态过滤（可选）</param>
        /// <param name="isEnabled">启用状态过滤（可选）</param>
        /// <returns>任务列表</returns>
        Task<IEnumerable<TaskInfo>> GetTasksAsync(string? status = null, bool? isEnabled = null);

        /// <summary>
        /// 删除任务
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <returns>是否删除成功</returns>
        Task<bool> DeleteTaskAsync(int taskId);

        /// <summary>
        /// 获取任务统计信息
        /// </summary>
        /// <returns>任务统计信息</returns>
        Task<Repositories.TaskStatistics> GetTaskStatisticsAsync();

        #endregion

        #region 数据库管理

        /// <summary>
        /// 初始化数据库
        /// </summary>
        /// <returns>是否初始化成功</returns>
        Task<bool> InitializeDatabaseAsync();

        /// <summary>
        /// 备份数据库
        /// </summary>
        /// <param name="backupPath">备份文件路径</param>
        /// <returns>是否备份成功</returns>
        Task<bool> BackupDatabaseAsync(string backupPath);

        /// <summary>
        /// 还原数据库
        /// </summary>
        /// <param name="backupPath">备份文件路径</param>
        /// <returns>是否还原成功</returns>
        Task<bool> RestoreDatabaseAsync(string backupPath);

        /// <summary>
        /// 清理过期数据
        /// </summary>
        /// <param name="daysToKeep">保留天数</param>
        /// <returns>清理的记录数量</returns>
        Task<int> CleanupOldDataAsync(int daysToKeep = 90);

        #endregion
    }
}