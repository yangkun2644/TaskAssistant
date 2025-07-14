using TaskAssistant.Models;

namespace TaskAssistant.Services
{
    /// <summary>
    /// 脚本执行服务接口
    /// 提供脚本编译、执行和日志记录功能
    /// </summary>
    public interface IScriptExecutionService
    {
        #region 事件定义

        /// <summary>
        /// 输出事件
        /// 当脚本产生标准输出时触发
        /// </summary>
        event Action<string>? OutputReceived;

        /// <summary>
        /// 错误输出事件
        /// 当脚本产生错误输出时触发
        /// </summary>
        event Action<string>? ErrorReceived;

        /// <summary>
        /// 状态变更事件
        /// 当执行状态发生变化时触发
        /// </summary>
        event Action<string>? StatusChanged;

        #endregion

        #region 执行方法

        /// <summary>
        /// 执行脚本代码
        /// </summary>
        /// <param name="code">脚本代码</param>
        /// <param name="title">执行标题</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <param name="scriptId">关联的脚本ID（可选）</param>
        /// <param name="taskId">关联的任务ID（可选）</param>
        /// <returns>执行结果</returns>
        Task<ScriptExecutionResult> ExecuteAsync(
            string code, 
            string title = "脚本执行", 
            CancellationToken cancellationToken = default,
            int? scriptId = null,
            int? taskId = null);

        /// <summary>
        /// 执行脚本并保存日志到数据库
        /// </summary>
        /// <param name="code">脚本代码</param>
        /// <param name="title">执行标题</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <param name="scriptId">关联的脚本ID（可选）</param>
        /// <param name="taskId">关联的任务ID（可选）</param>
        /// <returns>执行结果和日志ID</returns>
        Task<(ScriptExecutionResult Result, int LogId)> ExecuteAndLogAsync(
            string code, 
            string title = "脚本执行", 
            CancellationToken cancellationToken = default,
            int? scriptId = null,
            int? taskId = null);

        #endregion

        #region 日志管理

        /// <summary>
        /// 保存执行日志到数据库
        /// </summary>
        /// <param name="result">执行结果</param>
        /// <param name="scriptId">关联的脚本ID（可选）</param>
        /// <param name="taskId">关联的任务ID（可选）</param>
        /// <returns>日志ID</returns>
        Task<int> SaveExecutionLogAsync(ScriptExecutionResult result, int? scriptId = null, int? taskId = null);

        /// <summary>
        /// 获取执行日志列表
        /// </summary>
        /// <param name="scriptId">脚本ID过滤（可选）</param>
        /// <param name="taskId">任务ID过滤（可选）</param>
        /// <param name="status">状态过滤（可选）</param>
        /// <param name="pageIndex">页面索引</param>
        /// <param name="pageSize">页面大小</param>
        /// <returns>执行日志列表</returns>
        Task<IEnumerable<ScriptExecutionLog>> GetExecutionLogsAsync(
            int? scriptId = null, 
            int? taskId = null, 
            string? status = null,
            int pageIndex = 0, 
            int pageSize = 50);

        /// <summary>
        /// 获取执行日志详情
        /// </summary>
        /// <param name="logId">日志ID</param>
        /// <returns>执行日志详情</returns>
        Task<ScriptExecutionLog?> GetExecutionLogAsync(int logId);

        /// <summary>
        /// 删除执行日志
        /// </summary>
        /// <param name="logId">日志ID</param>
        /// <returns>是否删除成功</returns>
        Task<bool> DeleteExecutionLogAsync(int logId);

        /// <summary>
        /// 清理过期的执行日志
        /// </summary>
        /// <param name="daysToKeep">保留天数</param>
        /// <returns>清理的记录数量</returns>
        Task<int> CleanupOldLogsAsync(int daysToKeep = 30);

        #endregion

        #region 统计信息

        /// <summary>
        /// 获取执行统计信息
        /// </summary>
        /// <param name="scriptId">脚本ID过滤（可选）</param>
        /// <param name="taskId">任务ID过滤（可选）</param>
        /// <param name="days">统计天数（可选，默认30天）</param>
        /// <returns>执行统计信息</returns>
        Task<ExecutionStatistics> GetExecutionStatisticsAsync(int? scriptId = null, int? taskId = null, int days = 30);

        #endregion
    }

    /// <summary>
    /// 执行统计信息
    /// </summary>
    public class ExecutionStatistics
    {
        /// <summary>
        /// 总执行次数
        /// </summary>
        public int TotalExecutions { get; set; }

        /// <summary>
        /// 成功执行次数
        /// </summary>
        public int SuccessfulExecutions { get; set; }

        /// <summary>
        /// 失败执行次数
        /// </summary>
        public int FailedExecutions { get; set; }

        /// <summary>
        /// 取消执行次数
        /// </summary>
        public int CancelledExecutions { get; set; }

        /// <summary>
        /// 成功率（百分比）
        /// </summary>
        public double SuccessRate => TotalExecutions > 0 
            ? (double)SuccessfulExecutions / TotalExecutions * 100 
            : 0;

        /// <summary>
        /// 平均执行时间（毫秒）
        /// </summary>
        public double AverageExecutionTime { get; set; }

        /// <summary>
        /// 最后执行时间
        /// </summary>
        public DateTime? LastExecutionTime { get; set; }
    }
}