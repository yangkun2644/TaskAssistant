using TaskAssistant.Models;

namespace TaskAssistant.Data.Repositories
{
    /// <summary>
    /// 任???接口
    /// 定?任?相?的特殊操作
    /// </summary>
    public interface ITaskRepository : IRepository<TaskInfo>
    {
        #region 任?特殊查?

        /// <summary>
        /// 根据???取任?
        /// </summary>
        /// <param name="status">任???</param>
        /// <returns>指定??的任?集合</returns>
        Task<IEnumerable<TaskInfo>> GetByStatusAsync(string status);

        /// <summary>
        /// ?取待?行的任?
        /// </summary>
        /// <param name="count">?取?量（可?）</param>
        /// <returns>待?行的任?集合</returns>
        Task<IEnumerable<TaskInfo>> GetPendingTasksAsync(int? count = null);

        /// <summary>
        /// ?取正在?行的任?
        /// </summary>
        /// <returns>正在?行的任?集合</returns>
        Task<IEnumerable<TaskInfo>> GetRunningTasksAsync();

        /// <summary>
        /// 根据?本ID?取任?
        /// </summary>
        /// <param name="scriptId">?本ID</param>
        /// <returns>使用指定?本的任?集合</returns>
        Task<IEnumerable<TaskInfo>> GetByScriptIdAsync(int scriptId);

        /// <summary>
        /// ?取?划?行的任?
        /// </summary>
        /// <param name="fromTime">?始??</param>
        /// <param name="toTime">?束??</param>
        /// <returns>在指定??范???划?行的任?集合</returns>
        Task<IEnumerable<TaskInfo>> GetScheduledTasksAsync(DateTime fromTime, DateTime toTime);

        /// <summary>
        /// 搜索任?
        /// </summary>
        /// <param name="keyword">搜索???</param>
        /// <param name="status">????（可?）</param>
        /// <param name="taskType">任??型??（可?）</param>
        /// <param name="isEnabled">是否?用??（可?）</param>
        /// <returns>匹配的任?集合</returns>
        Task<IEnumerable<TaskInfo>> SearchAsync(string keyword, string? status = null, string? taskType = null, bool? isEnabled = null);

        /// <summary>
        /// ?取任???信息
        /// </summary>
        /// <returns>任???信息</returns>
        Task<TaskStatistics> GetStatisticsAsync();

        #endregion

        #region 任???管理

        /// <summary>
        /// 更新任???
        /// </summary>
        /// <param name="taskId">任?ID</param>
        /// <param name="status">新??</param>
        /// <param name="result">?行?果（可?）</param>
        /// <param name="error">??信息（可?）</param>
        /// <returns>更新后的任?信息</returns>
        Task<TaskInfo?> UpdateTaskStatusAsync(int taskId, string status, string? result = null, string? error = null);

        /// <summary>
        /// ??任??始?行
        /// </summary>
        /// <param name="taskId">任?ID</param>
        /// <returns>更新后的任?信息</returns>
        Task<TaskInfo?> MarkTaskStartedAsync(int taskId);

        /// <summary>
        /// ??任?完成
        /// </summary>
        /// <param name="taskId">任?ID</param>
        /// <param name="result">?行?果</param>
        /// <param name="isSuccess">是否成功</param>
        /// <returns>更新后的任?信息</returns>
        Task<TaskInfo?> MarkTaskCompletedAsync(int taskId, string result, bool isSuccess = true);

        /// <summary>
        /// ??任?失?
        /// </summary>
        /// <param name="taskId">任?ID</param>
        /// <param name="error">??信息</param>
        /// <returns>更新后的任?信息</returns>
        Task<TaskInfo?> MarkTaskFailedAsync(int taskId, string error);

        /// <summary>
        /// 重置任???
        /// </summary>
        /// <param name="taskId">任?ID</param>
        /// <returns>重置后的任?信息</returns>
        Task<TaskInfo?> ResetTaskAsync(int taskId);

        #endregion

        #region 批量操作

        /// <summary>
        /// 批量更新任???
        /// </summary>
        /// <param name="taskIds">任?ID集合</param>
        /// <param name="status">新??</param>
        /// <returns>受影?的行?</returns>
        Task<int> BulkUpdateStatusAsync(IEnumerable<int> taskIds, string status);

        /// <summary>
        /// 批量?用/禁用任?
        /// </summary>
        /// <param name="taskIds">任?ID集合</param>
        /// <param name="isEnabled">是否?用</param>
        /// <returns>受影?的行?</returns>
        Task<int> BulkUpdateEnabledStatusAsync(IEnumerable<int> taskIds, bool isEnabled);

        /// <summary>
        /// 清理已完成的任?
        /// </summary>
        /// <param name="daysOld">保留天?</param>
        /// <returns>被清理的任??量</returns>
        Task<int> CleanupCompletedTasksAsync(int daysOld = 30);

        #endregion
    }

    /// <summary>
    /// 任???信息
    /// </summary>
    public class TaskStatistics
    {
        /// <summary>
        /// ?任??
        /// </summary>
        public int TotalTasks { get; set; }

        /// <summary>
        /// 待?行任??
        /// </summary>
        public int PendingTasks { get; set; }

        /// <summary>
        /// ?行中任??
        /// </summary>
        public int RunningTasks { get; set; }

        /// <summary>
        /// 已完成任??
        /// </summary>
        public int CompletedTasks { get; set; }

        /// <summary>
        /// 失?任??
        /// </summary>
        public int FailedTasks { get; set; }

        /// <summary>
        /// ?用任??
        /// </summary>
        public int EnabledTasks { get; set; }

        /// <summary>
        /// 禁用任??
        /// </summary>
        public int DisabledTasks { get; set; }

        /// <summary>
        /// 今日?行任??
        /// </summary>
        public int TodayExecutedTasks { get; set; }

        /// <summary>
        /// 本周?行任??
        /// </summary>
        public int WeekExecutedTasks { get; set; }

        /// <summary>
        /// 成功率（百分比）
        /// </summary>
        public double SuccessRate { get; set; }
    }
}