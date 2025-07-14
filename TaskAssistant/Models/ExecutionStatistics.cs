namespace TaskAssistant.Models
{
    /// <summary>
    /// ?行??信息
    /// </summary>
    public class ExecutionStatistics
    {
        /// <summary>
        /// ??行次?
        /// </summary>
        public int TotalExecutions { get; set; }

        /// <summary>
        /// 成功?行次?
        /// </summary>
        public int SuccessfulExecutions { get; set; }

        /// <summary>
        /// 失??行次?
        /// </summary>
        public int FailedExecutions { get; set; }

        /// <summary>
        /// 取消?行次?
        /// </summary>
        public int CancelledExecutions { get; set; }

        /// <summary>
        /// 平均?行??（毫秒）
        /// </summary>
        public double AverageExecutionTime { get; set; }

        /// <summary>
        /// 最后?行??
        /// </summary>
        public DateTime? LastExecutionTime { get; set; }

        /// <summary>
        /// 成功率
        /// </summary>
        public double SuccessRate => TotalExecutions > 0 ? (double)SuccessfulExecutions / TotalExecutions * 100 : 0;

        /// <summary>
        /// 失?率
        /// </summary>
        public double FailureRate => TotalExecutions > 0 ? (double)FailedExecutions / TotalExecutions * 100 : 0;
    }
}