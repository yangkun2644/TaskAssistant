using System;

namespace TaskAssistant.Models
{
    /// <summary>
    /// 系统统计信息模型
    /// 包含应用程序运行时的各种统计数据
    /// </summary>
    public class SystemStatistics
    {
        /// <summary>
        /// 脚本总数
        /// </summary>
        public int TotalScripts { get; set; }

        /// <summary>
        /// 今日执行的脚本数量
        /// </summary>
        public int TodayExecutions { get; set; }

        /// <summary>
        /// 任务总数
        /// </summary>
        public int TotalTasks { get; set; }

        /// <summary>
        /// 数据库大小（MB）
        /// </summary>
        public double DatabaseSizeMB { get; set; }

        /// <summary>
        /// 内存使用量（MB）
        /// </summary>
        public double MemoryUsageMB { get; set; }

        /// <summary>
        /// CPU 使用率（百分比）
        /// </summary>
        public double CpuUsagePercent { get; set; }

        /// <summary>
        /// 统计更新时间
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 任务统计信息模型
    /// </summary>
    public class TaskStatistics
    {
        /// <summary>
        /// 总任务数
        /// </summary>
        public int TotalTasks { get; set; }

        /// <summary>
        /// 待执行任务数
        /// </summary>
        public int PendingTasks { get; set; }

        /// <summary>
        /// 正在执行任务数
        /// </summary>
        public int RunningTasks { get; set; }

        /// <summary>
        /// 已完成任务数
        /// </summary>
        public int CompletedTasks { get; set; }

        /// <summary>
        /// 失败任务数
        /// </summary>
        public int FailedTasks { get; set; }

        /// <summary>
        /// 今日执行次数
        /// </summary>
        public int TodayExecutions { get; set; }

        /// <summary>
        /// 成功率（百分比）
        /// </summary>
        public double SuccessRate { get; set; }
    }
}