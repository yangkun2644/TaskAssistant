using Microsoft.EntityFrameworkCore;
using TaskAssistant.Models;

namespace TaskAssistant.Data.Repositories
{
    /// <summary>
    /// 任务仓储实现类
    /// 提供任务相关的数据访问操作
    /// </summary>
    public class TaskRepository : Repository<TaskInfo>, ITaskRepository
    {
        #region 构造函数

        /// <summary>
        /// 初始化任务仓储
        /// </summary>
        /// <param name="context">数据库上下文</param>
        public TaskRepository(AppDbContext context) : base(context)
        {
        }

        #endregion

        #region 任务特殊查询实现

        /// <summary>
        /// 根据状态获取任务
        /// </summary>
        /// <param name="status">任务状态</param>
        /// <returns>指定状态的任务集合</returns>
        public async Task<IEnumerable<TaskInfo>> GetByStatusAsync(string status)
        {
            return await _dbSet
                .Include(t => t.Script)
                .Where(t => t.Status == status)
                .OrderBy(t => t.Priority)
                .ThenBy(t => t.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// 获取待执行的任务
        /// </summary>
        /// <param name="count">获取数量（可选）</param>
        /// <returns>待执行的任务集合</returns>
        public async Task<IEnumerable<TaskInfo>> GetPendingTasksAsync(int? count = null)
        {
            IQueryable<TaskInfo> query = _dbSet
                .Include(t => t.Script)
                .Where(t => t.Status == "待执行" && t.IsEnabled)
                .OrderBy(t => t.Priority)
                .ThenBy(t => t.ScheduledTime ?? t.CreatedAt);

            if (count.HasValue)
            {
                query = query.Take(count.Value);
            }

            return await query.ToListAsync();
        }

        /// <summary>
        /// 获取正在执行的任务
        /// </summary>
        /// <returns>正在执行的任务集合</returns>
        public async Task<IEnumerable<TaskInfo>> GetRunningTasksAsync()
        {
            return await _dbSet
                .Include(t => t.Script)
                .Where(t => t.Status == "执行中")
                .OrderBy(t => t.StartedAt)
                .ToListAsync();
        }

        /// <summary>
        /// 根据脚本ID获取任务
        /// </summary>
        /// <param name="scriptId">脚本ID</param>
        /// <returns>使用指定脚本的任务集合</returns>
        public async Task<IEnumerable<TaskInfo>> GetByScriptIdAsync(int scriptId)
        {
            return await _dbSet
                .Include(t => t.Script)
                .Where(t => t.ScriptId == scriptId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// 获取计划执行的任务
        /// </summary>
        /// <param name="fromTime">开始时间</param>
        /// <param name="toTime">结束时间</param>
        /// <returns>在指定时间范围内计划执行的任务集合</returns>
        public async Task<IEnumerable<TaskInfo>> GetScheduledTasksAsync(DateTime fromTime, DateTime toTime)
        {
            return await _dbSet
                .Include(t => t.Script)
                .Where(t => t.ScheduledTime.HasValue && 
                           t.ScheduledTime >= fromTime && 
                           t.ScheduledTime <= toTime &&
                           t.IsEnabled)
                .OrderBy(t => t.ScheduledTime)
                .ToListAsync();
        }

        /// <summary>
        /// 搜索任务
        /// </summary>
        /// <param name="keyword">搜索关键词</param>
        /// <param name="status">状态过滤（可选）</param>
        /// <param name="taskType">任务类型过滤（可选）</param>
        /// <param name="isEnabled">是否启用过滤（可选）</param>
        /// <returns>匹配的任务集合</returns>
        public async Task<IEnumerable<TaskInfo>> SearchAsync(string keyword, string? status = null, string? taskType = null, bool? isEnabled = null)
        {
            var query = _dbSet.Include(t => t.Script).AsQueryable();

            // 关键词搜索
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.ToLower();
                query = query.Where(t => 
                    t.Name.ToLower().Contains(keyword) ||
                    t.Description.ToLower().Contains(keyword) ||
                    (t.Script != null && t.Script.Name.ToLower().Contains(keyword)));
            }

            // 状态过滤
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(t => t.Status == status);
            }

            // 任务类型过滤
            if (!string.IsNullOrWhiteSpace(taskType))
            {
                query = query.Where(t => t.TaskType == taskType);
            }

            // 启用状态过滤
            if (isEnabled.HasValue)
            {
                query = query.Where(t => t.IsEnabled == isEnabled.Value);
            }

            return await query
                .OrderByDescending(t => t.LastModified)
                .ToListAsync();
        }

        /// <summary>
        /// 获取任务统计信息
        /// </summary>
        /// <returns>任务统计信息</returns>
        public async Task<TaskStatistics> GetStatisticsAsync()
        {
            var today = DateTime.Today;
            var weekStart = today.AddDays(-(int)today.DayOfWeek);

            var stats = new TaskStatistics
            {
                TotalTasks = await _dbSet.CountAsync(),
                PendingTasks = await _dbSet.CountAsync(t => t.Status == "待执行"),
                RunningTasks = await _dbSet.CountAsync(t => t.Status == "执行中"),
                CompletedTasks = await _dbSet.CountAsync(t => t.Status == "已完成"),
                FailedTasks = await _dbSet.CountAsync(t => t.Status == "失败"),
                EnabledTasks = await _dbSet.CountAsync(t => t.IsEnabled),
                DisabledTasks = await _dbSet.CountAsync(t => !t.IsEnabled),
                TodayExecutedTasks = await _dbSet.CountAsync(t => t.StartedAt >= today),
                WeekExecutedTasks = await _dbSet.CountAsync(t => t.StartedAt >= weekStart)
            };

            // 计算成功率
            var totalExecuted = stats.CompletedTasks + stats.FailedTasks;
            if (totalExecuted > 0)
            {
                stats.SuccessRate = (double)stats.CompletedTasks / totalExecuted * 100;
            }

            return stats;
        }

        #endregion

        #region 任务状态管理实现

        /// <summary>
        /// 更新任务状态
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <param name="status">新状态</param>
        /// <param name="result">执行结果（可选）</param>
        /// <param name="error">错误信息（可选）</param>
        /// <returns>更新后的任务信息</returns>
        public async Task<TaskInfo?> UpdateTaskStatusAsync(int taskId, string status, string? result = null, string? error = null)
        {
            var task = await GetByIdAsync(taskId);
            if (task != null)
            {
                task.Status = status;
                task.LastModified = DateTime.Now;

                if (!string.IsNullOrWhiteSpace(result))
                {
                    task.LastExecutionResult = result;
                }

                if (!string.IsNullOrWhiteSpace(error))
                {
                    task.LastError = error;
                }

                _dbSet.Update(task);
                await SaveChangesAsync();
            }

            return task;
        }

        /// <summary>
        /// 标记任务开始执行
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <returns>更新后的任务信息</returns>
        public async Task<TaskInfo?> MarkTaskStartedAsync(int taskId)
        {
            var task = await GetByIdAsync(taskId);
            if (task != null)
            {
                task.Status = "执行中";
                task.StartedAt = DateTime.Now;
                task.ExecutionCount++;
                task.LastModified = DateTime.Now;

                _dbSet.Update(task);
                await SaveChangesAsync();
            }

            return task;
        }

        /// <summary>
        /// 标记任务完成
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <param name="result">执行结果</param>
        /// <param name="isSuccess">是否成功</param>
        /// <returns>更新后的任务信息</returns>
        public async Task<TaskInfo?> MarkTaskCompletedAsync(int taskId, string result, bool isSuccess = true)
        {
            var task = await GetByIdAsync(taskId);
            if (task != null)
            {
                task.Status = isSuccess ? "已完成" : "失败";
                task.CompletedAt = DateTime.Now;
                task.LastExecutionResult = result;
                task.LastModified = DateTime.Now;

                if (isSuccess)
                {
                    task.SuccessCount++;
                }
                else
                {
                    task.FailureCount++;
                }

                _dbSet.Update(task);
                await SaveChangesAsync();
            }

            return task;
        }

        /// <summary>
        /// 标记任务失败
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <param name="error">错误信息</param>
        /// <returns>更新后的任务信息</returns>
        public async Task<TaskInfo?> MarkTaskFailedAsync(int taskId, string error)
        {
            var task = await GetByIdAsync(taskId);
            if (task != null)
            {
                task.Status = "失败";
                task.CompletedAt = DateTime.Now;
                task.LastError = error;
                task.FailureCount++;
                task.LastModified = DateTime.Now;

                _dbSet.Update(task);
                await SaveChangesAsync();
            }

            return task;
        }

        /// <summary>
        /// 重置任务状态
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <returns>重置后的任务信息</returns>
        public async Task<TaskInfo?> ResetTaskAsync(int taskId)
        {
            var task = await GetByIdAsync(taskId);
            if (task != null)
            {
                task.Status = "待执行";
                task.StartedAt = null;
                task.CompletedAt = null;
                task.LastExecutionResult = null;
                task.LastError = null;
                task.LastModified = DateTime.Now;

                _dbSet.Update(task);
                await SaveChangesAsync();
            }

            return task;
        }

        #endregion

        #region 批量操作实现

        /// <summary>
        /// 批量更新任务状态
        /// </summary>
        /// <param name="taskIds">任务ID集合</param>
        /// <param name="status">新状态</param>
        /// <returns>受影响的行数</returns>
        public async Task<int> BulkUpdateStatusAsync(IEnumerable<int> taskIds, string status)
        {
            var tasks = await _dbSet
                .Where(t => taskIds.Contains(t.Id))
                .ToListAsync();

            foreach (var task in tasks)
            {
                task.Status = status;
                task.LastModified = DateTime.Now;
            }

            return await SaveChangesAsync();
        }

        /// <summary>
        /// 批量启用/禁用任务
        /// </summary>
        /// <param name="taskIds">任务ID集合</param>
        /// <param name="isEnabled">是否启用</param>
        /// <returns>受影响的行数</returns>
        public async Task<int> BulkUpdateEnabledStatusAsync(IEnumerable<int> taskIds, bool isEnabled)
        {
            var tasks = await _dbSet
                .Where(t => taskIds.Contains(t.Id))
                .ToListAsync();

            foreach (var task in tasks)
            {
                task.IsEnabled = isEnabled;
                task.LastModified = DateTime.Now;
            }

            return await SaveChangesAsync();
        }

        /// <summary>
        /// 清理已完成的任务
        /// </summary>
        /// <param name="daysOld">保留天数</param>
        /// <returns>被清理的任务数量</returns>
        public async Task<int> CleanupCompletedTasksAsync(int daysOld = 30)
        {
            var cutoffDate = DateTime.Now.AddDays(-daysOld);
            
            var oldTasks = await _dbSet
                .Where(t => t.Status == "已完成" && t.CompletedAt < cutoffDate)
                .ToListAsync();

            if (oldTasks.Any())
            {
                _dbSet.RemoveRange(oldTasks);
                await SaveChangesAsync();
            }

            return oldTasks.Count;
        }

        #endregion
    }
}